using System.Collections.Generic;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Carried between scenes by ClassSelect / lobby UI: the local player's chosen class, the
    /// per-team size, and whether the match is offline (bots fill the rosters and the local
    /// GameModeManager drives the flow). Read by <see cref="MatchInitializer"/> on gameplay load.
    /// </summary>
    public static class MatchConfig
    {
        /// <summary>Class the local player picked in ClassSelect.</summary>
        public static ClassId LocalClass = ClassId.Carrot;

        /// <summary>Players per team (1..MaxPlayersPerTeam). Bots fill any human shortfall offline.</summary>
        public static int TeamSize = GameConstants.MaxPlayersPerTeam;

        /// <summary>True for solo/offline play: bots are spawned and the match flow auto-starts here.</summary>
        public static bool Offline = true;

        /// <summary>Reset to defaults (e.g. on returning to the main menu).</summary>
        public static void Reset()
        {
            LocalClass = ClassId.Carrot;
            TeamSize = GameConstants.MaxPlayersPerTeam;
            Offline = true;
        }
    }

    /// <summary>
    /// Optional contract a HUD component can implement so <see cref="MatchInitializer"/> can bind
    /// it to the local player without depending on a concrete UI type. Kept local to this cluster
    /// so HUD work can land independently.
    /// </summary>
    public interface IHudBindable
    {
        void BindLocalPlayer(PlayerController localPlayer);
    }

    /// <summary>
    /// Gameplay-scene entry point. Reads <see cref="MatchConfig"/>, spawns the local player with the
    /// chosen class, fills the remaining roster with bots offline, binds the HUD to the local player,
    /// and (offline) kicks off <see cref="GameModeManager.BeginMatchFlow"/>. Wires together
    /// <see cref="GameModeManager"/>, <see cref="SpawnManager"/>, <see cref="PlayerSpawner"/>, and a
    /// bot spawner if present.
    /// </summary>
    [DisallowMultipleComponent]
    public class MatchInitializer : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] PlayerSpawner playerSpawner;
        [SerializeField] GameModeManager gameModeManager;
        [SerializeField] SpawnManager spawnManager;

        [Header("Config")]
        [SerializeField] MomentumConfigSO momentumConfig;
        [Tooltip("One CharacterDataSO per class. Looked up by CharacterDataSO.classId.")]
        [SerializeField] List<CharacterDataSO> characterRoster = new List<CharacterDataSO>();

        [Header("Bots (offline)")]
        [Tooltip("Class pool bots draw from to fill rosters offline. Falls back to characterRoster if empty.")]
        [SerializeField] List<CharacterDataSO> botClassPool = new List<CharacterDataSO>();

        [Header("HUD")]
        [Tooltip("HUD root that implements IHudBindable. Lives in the additive GameplayUI scene; may be assigned at runtime.")]
        [SerializeField] MonoBehaviour hudComponent;   // expected to implement IHudBindable

        PlayerController localPlayer;
        int nextPlayerId;

        public PlayerController LocalPlayer => localPlayer;

        void Start()
        {
            ResolveSceneReferences();

            if (playerSpawner == null)
            {
                Debug.LogError("[MatchInitializer] No PlayerSpawner found; cannot start the match.", this);
                return;
            }

            // Local player on Team A.
            CharacterDataSO localData = ResolveClass(MatchConfig.LocalClass);
            localPlayer = playerSpawner.SpawnPlayer(localData, momentumConfig, Team.A, NextId(), local: true, bot: false);

            if (MatchConfig.Offline)
                FillBots();

            BindHud();

            if (MatchConfig.Offline && gameModeManager != null)
                gameModeManager.BeginMatchFlow();
        }

        void ResolveSceneReferences()
        {
            if (gameModeManager == null) gameModeManager = GameModeManager.Instance;
            if (spawnManager == null) spawnManager = SpawnManager.Instance;
            if (playerSpawner == null) playerSpawner = FindFirstObjectByType<PlayerSpawner>();
            if (hudComponent == null)
            {
                // Locate a HUD that opted into binding (may live in the additive UI scene).
                foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                {
                    if (mb is IHudBindable) { hudComponent = mb; break; }
                }
            }
        }

        /// <summary>Fill both teams to <see cref="MatchConfig.TeamSize"/> with bots (local already occupies one A slot).</summary>
        void FillBots()
        {
            int perTeam = Mathf.Clamp(MatchConfig.TeamSize, 1, GameConstants.MaxPlayersPerTeam);

            // Team A: one slot already taken by the local player.
            for (int i = 1; i < perTeam; i++)
                SpawnBot(Team.A, i);

            // Team B: full roster of bots.
            for (int i = 0; i < perTeam; i++)
                SpawnBot(Team.B, i);
        }

        void SpawnBot(Team team, int slot)
        {
            CharacterDataSO data = PickBotClass(slot);
            if (data == null) return;
            playerSpawner.SpawnPlayer(data, momentumConfig, team, NextId(), local: false, bot: true);
        }

        CharacterDataSO PickBotClass(int slot)
        {
            List<CharacterDataSO> pool = botClassPool.Count > 0 ? botClassPool : characterRoster;
            if (pool.Count == 0) return null;
            return pool[slot % pool.Count];
        }

        CharacterDataSO ResolveClass(ClassId id)
        {
            foreach (var c in characterRoster)
                if (c != null && c.classId == id) return c;
            return characterRoster.Count > 0 ? characterRoster[0] : null;
        }

        void BindHud()
        {
            if (localPlayer == null) return;
            if (hudComponent is IHudBindable bindable)
                bindable.BindLocalPlayer(localPlayer);
        }

        int NextId() => nextPlayerId++;
    }
}
