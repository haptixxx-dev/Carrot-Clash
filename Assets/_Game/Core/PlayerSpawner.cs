using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Instantiates and configures <see cref="PlayerController"/> instances for the local player
    /// and bots (used by MatchInitializer / BotSpawner). Handles prefab instantiation, calling
    /// <see cref="PlayerController.Initialize"/>, placement at a team spawn via
    /// <see cref="SpawnManager.GetInitialSpawn"/>, registration with <see cref="GameModeManager"/>,
    /// and attaching <see cref="PlayerInputBinder"/> for the local player only.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerSpawner : MonoBehaviour
    {
        [Header("Prefab")]
        [Tooltip("Player root prefab with PlayerController + subsystems wired. Per-class visual model comes from CharacterDataSO.playerModelPrefab.")]
        [SerializeField] PlayerController playerPrefab;

        [Header("Shared config")]
        [Tooltip("Fallback momentum config used when none is supplied to SpawnPlayer.")]
        [SerializeField] MomentumConfigSO defaultMomentumConfig;

        int spawnIndexA;
        int spawnIndexB;

        /// <summary>
        /// Spawn one fully-configured player. <paramref name="local"/> players also receive a
        /// <see cref="PlayerInputBinder"/>; bots are flagged via <paramref name="bot"/> and driven
        /// by external AI. Returns the spawned controller (null if no prefab is assigned).
        /// </summary>
        public PlayerController SpawnPlayer(CharacterDataSO data, MomentumConfigSO cfg, Team team, int id, bool local, bool bot)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerSpawner] No player prefab assigned.", this);
                return null;
            }
            if (data == null)
            {
                Debug.LogError("[PlayerSpawner] No CharacterDataSO supplied to SpawnPlayer.", this);
                return null;
            }

            MomentumConfigSO momentumCfg = cfg != null ? cfg : defaultMomentumConfig;

            (Vector3 pos, Quaternion rot) = ResolveSpawn(team);

            PlayerController pc = Instantiate(playerPrefab, pos, rot);
            pc.name = $"Player_{(bot ? "Bot" : (local ? "Local" : "Remote"))}_{id}_{data.classId}";

            pc.Initialize(data, momentumCfg, team, id, local, bot);

            if (GameModeManager.Instance != null)
                GameModeManager.Instance.RegisterPlayer(pc);

            if (local)
            {
                // Attach the input binder if the prefab did not already include one.
                if (pc.GetComponent<PlayerInputBinder>() == null)
                    pc.gameObject.AddComponent<PlayerInputBinder>();
            }

            return pc;
        }

        (Vector3 pos, Quaternion rot) ResolveSpawn(Team team)
        {
            if (SpawnManager.Instance == null)
                return (Vector3.up, Quaternion.identity);

            int index = team == Team.B ? spawnIndexB++ : spawnIndexA++;
            return SpawnManager.Instance.GetInitialSpawn(team, index);
        }
    }
}
