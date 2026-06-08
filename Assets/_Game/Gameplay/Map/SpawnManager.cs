using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>A fixed team spawn point. Several per team behind cover (Map doc).</summary>
    public class SpawnPoint : MonoBehaviour
    {
        public Team team = Team.A;
        void OnDrawGizmos()
        {
            Gizmos.color = team == Team.A ? Color.cyan : new Color(1f, 0.5f, 0.4f);
            Gizmos.DrawCube(transform.position + Vector3.up, Vector3.one * 0.5f);
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * 2f);
        }
    }

    /// <summary>
    /// Handles respawn timing and spawn-point selection with the safety redirect rule (Core Loop /
    /// Map docs): if a fixed spawn is contested (enemy within 10m), redirect to the nearest safe
    /// teammate within 30m, else fixed spawn with extra delay + extended invulnerability.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpawnManager : MonoBehaviour
    {
        public static SpawnManager Instance { get; private set; }

        [SerializeField] List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

        static readonly Collider[] enemyBuffer = new Collider[16];

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (spawnPoints.Count == 0)
                spawnPoints.AddRange(FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None));
            GameEvents.OnPlayerDied += HandleDeath;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            GameEvents.OnPlayerDied -= HandleDeath;
        }

        void HandleDeath(PlayerController player)
        {
            if (player == null || player.IsBot && false) { /* bots respawn too */ }
            StartCoroutine(RespawnRoutine(player));
        }

        IEnumerator RespawnRoutine(PlayerController player)
        {
            yield return new WaitForSeconds(GameConstants.RespawnTime);
            if (player == null) yield break;

            float extraDelay;
            float invuln;
            var (pos, rot) = ChooseSpawn(player, out extraDelay, out invuln);

            if (extraDelay > 0f) yield return new WaitForSeconds(extraDelay);
            if (player == null) yield break;

            player.Respawn(pos, rot);
            StartCoroutine(InvulnRoutine(player, invuln));
        }

        IEnumerator InvulnRoutine(PlayerController player, float duration)
        {
            if (player == null || player.health == null) yield break;
            player.health.Invulnerable = true;
            yield return new WaitForSeconds(duration);
            if (player != null && player.health != null) player.health.Invulnerable = false;
        }

        (Vector3 pos, Quaternion rot) ChooseSpawn(PlayerController player, out float extraDelay, out float invuln)
        {
            extraDelay = 0f;
            invuln = GameConstants.SpawnInvulnerability;

            // 1. Try an uncontested fixed spawn for the player's team.
            SpawnPoint best = null;
            foreach (var sp in spawnPoints)
            {
                if (sp == null || sp.team != player.Team) continue;
                if (!IsContested(sp.transform.position, player.Team))
                {
                    best = sp;
                    break;
                }
            }

            if (best != null)
                return (best.transform.position, best.transform.rotation);

            // 2. All fixed spawns contested → nearest safe teammate within 30m.
            PlayerController safeAlly = FindSafeTeammate(player);
            if (safeAlly != null)
            {
                Vector3 offset = safeAlly.transform.position - safeAlly.transform.forward * 2f;
                return (offset, safeAlly.transform.rotation);
            }

            // 3. Fallback: fixed spawn anyway with extra delay + extended invuln.
            foreach (var sp in spawnPoints)
                if (sp != null && sp.team == player.Team) { best = sp; break; }

            extraDelay = 1f;
            invuln = 3f;
            return best != null ? (best.transform.position, best.transform.rotation)
                                : (Vector3.up, Quaternion.identity);
        }

        bool IsContested(Vector3 position, Team friendlyTeam)
        {
            int mask = LayerMask.GetMask(GameConstants.LayerPlayer);
            int count = Physics.OverlapSphereNonAlloc(position, GameConstants.SpawnContestRadius, enemyBuffer, mask);
            for (int i = 0; i < count; i++)
            {
                var pc = enemyBuffer[i].GetComponentInParent<PlayerController>();
                if (pc != null && pc.Team.IsEnemyOf(friendlyTeam) && !pc.health.IsDead)
                    return true;
            }
            return false;
        }

        PlayerController FindSafeTeammate(PlayerController player)
        {
            PlayerController best = null;
            float bestDist = GameConstants.SafeTeammateRadius;
            var all = GameModeManager.Instance != null ? GameModeManager.Instance.Players : null;
            if (all == null) return null;

            foreach (var mate in all)
            {
                if (mate == null || mate == player) continue;
                if (mate.Team != player.Team || mate.health.IsDead) continue;
                if (IsContested(mate.transform.position, player.Team)) continue;
                float d = Vector3.Distance(mate.transform.position, player.transform.position);
                if (d <= bestDist) { bestDist = d; best = mate; }
            }
            return best;
        }

        public (Vector3, Quaternion) GetInitialSpawn(Team team, int index)
        {
            var teamSpawns = new List<SpawnPoint>();
            foreach (var sp in spawnPoints)
                if (sp != null && sp.team == team) teamSpawns.Add(sp);
            if (teamSpawns.Count == 0) return (Vector3.up, Quaternion.identity);
            var chosen = teamSpawns[index % teamSpawns.Count];
            return (chosen.transform.position, chosen.transform.rotation);
        }
    }
}
