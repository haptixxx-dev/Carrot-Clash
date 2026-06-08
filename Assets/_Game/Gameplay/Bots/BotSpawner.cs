using System.Collections.Generic;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Fills empty match slots with AI players. Spawns up to <see cref="botsPerTeam"/> bots per team
    /// from a shared player prefab, assigning a class from <see cref="characters"/> (round-robin),
    /// initialising each via <see cref="PlayerController.Initialize"/> with <c>bot:true</c>, registering
    /// them with <see cref="GameModeManager"/>, placing them at a team spawn from
    /// <see cref="SpawnManager"/>, and attaching a <see cref="BotController"/> with the configured
    /// difficulty. Used for Phase-1 solo testing and to top up under-filled lobbies.
    /// </summary>
    [DisallowMultipleComponent]
    public class BotSpawner : MonoBehaviour
    {
        [Header("Prefab / data")]
        [Tooltip("The standard player prefab (must carry PlayerController + subsystems).")]
        [SerializeField] GameObject playerPrefab;
        [Tooltip("Class data pool; assigned round-robin to spawned bots.")]
        [SerializeField] List<CharacterDataSO> characters = new List<CharacterDataSO>();
        [SerializeField] MomentumConfigSO momentumConfig;

        [Header("Fill rules")]
        [SerializeField] BotDifficulty difficulty = BotDifficulty.Medium;
        [Tooltip("Bots to add to each team (clamped to free slots up to MaxPlayersPerTeam).")]
        [SerializeField] int botsPerTeam = 3;
        [Tooltip("Spawn automatically on Start (handy for solo testing).")]
        [SerializeField] bool spawnOnStart = true;

        int nextPlayerId = 1000;     // bot ids live in a high range to avoid clashing with humans
        int classCursor;

        readonly List<PlayerController> spawned = new List<PlayerController>(GameConstants.MaxPlayers);

        public IReadOnlyList<PlayerController> Spawned => spawned;

        void Start()
        {
            if (spawnOnStart) FillEmptySlots();
        }

        /// <summary>Top up both teams up to their bot count, respecting the per-team cap and existing players.</summary>
        public void FillEmptySlots()
        {
            FillTeam(Team.A);
            FillTeam(Team.B);
        }

        void FillTeam(Team team)
        {
            if (playerPrefab == null || momentumConfig == null || characters.Count == 0)
            {
                Debug.LogWarning("[BotSpawner] Missing prefab / momentum config / character data; cannot spawn bots.", this);
                return;
            }

            int existing = CountTeam(team);
            int free = Mathf.Max(0, GameConstants.MaxPlayersPerTeam - existing);
            int toSpawn = Mathf.Min(botsPerTeam, free);

            for (int i = 0; i < toSpawn; i++)
            {
                int slotIndex = existing + i;
                SpawnBot(team, slotIndex);
            }
        }

        /// <summary>Spawn a single configured bot for a team at the given spawn index.</summary>
        public PlayerController SpawnBot(Team team, int spawnIndex)
        {
            if (playerPrefab == null || momentumConfig == null || characters.Count == 0)
                return null;

            CharacterDataSO data = characters[classCursor % characters.Count];
            classCursor++;

            Vector3 pos = Vector3.up;
            Quaternion rot = Quaternion.identity;
            if (SpawnManager.Instance != null)
            {
                var (p, r) = SpawnManager.Instance.GetInitialSpawn(team, spawnIndex);
                pos = p;
                rot = r;
            }

            GameObject go = Instantiate(playerPrefab, pos, rot);
            go.name = $"Bot_{team}_{spawnIndex}_{data.characterName}";

            PlayerController pc = go.GetComponent<PlayerController>();
            if (pc == null)
            {
                Debug.LogError("[BotSpawner] Player prefab has no PlayerController; destroying instance.", this);
                Destroy(go);
                return null;
            }

            int id = nextPlayerId++;
            pc.Initialize(data, momentumConfig, team, id, local: false, bot: true);

            GameModeManager.Instance?.RegisterPlayer(pc);

            BotController bot = go.GetComponent<BotController>();
            if (bot == null) bot = go.AddComponent<BotController>();
            bot.Configure(pc, difficulty);

            spawned.Add(pc);
            return pc;
        }

        int CountTeam(Team team)
        {
            int n = 0;
            IReadOnlyList<PlayerController> players = GameModeManager.Instance != null
                ? GameModeManager.Instance.Players
                : null;
            if (players != null)
            {
                for (int i = 0; i < players.Count; i++)
                    if (players[i] != null && players[i].Team == team) n++;
            }
            return n;
        }
    }
}
