using System.Collections.Generic;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// A capture objective (Core Loop + Map docs). Counts players of each team inside the radius,
    /// advances/reverses capture progress, awards one-time capture score and per-second tick score,
    /// and resets contesting players' momentum decay. Zone C stays locked until its unlock time.
    ///
    /// Authoritative on the server in networked play; CaptureZoneNetwork mirrors progress/ownership.
    /// </summary>
    [DisallowMultipleComponent]
    public class CaptureZone : MonoBehaviour
    {
        [Header("Identity")]
        public ZoneId zoneId = ZoneId.A;

        [Header("Geometry / rates (Map doc)")]
        public float captureRadius = 6f;     // A=6 B=4 C=8
        public float captureSeconds = 10f;   // time for 1 player to capture; A=10 B=8 C=12
        public int scoreTickRate = 1;        // per second; C=2
        [Tooltip("Match-elapsed seconds before this zone is contestable (C = 300).")]
        public float lockUntilMatchTime = 0f;

        [Header("Tuning")]
        [Tooltip("Each extra contributor adds this fraction of base rate (diminishing).")]
        public float perPlayerSpeedup = 0.5f;
        [Tooltip("Max effective capture-speed multiplier from stacking players.")]
        public float maxSpeedup = 2.5f;

        // ----- Runtime state -----
        Team owningTeam = Team.None;
        float progress;          // 0..1 toward the leading team
        Team progressTeam = Team.None;  // which team the current progress belongs to
        float tickAccumulator;
        bool unlocked;
        bool contestedDuringCapture;

        readonly List<PlayerController> inside = new List<PlayerController>(8);
        static readonly Collider[] overlapBuffer = new Collider[32];

        public Team OwningTeam => owningTeam;
        public float Progress => progress;
        public Team ProgressTeam => progressTeam;
        public bool IsUnlocked => unlocked;
        public bool IsLocked => !unlocked;

        void Start()
        {
            unlocked = lockUntilMatchTime <= 0f;
        }

        void Update()
        {
            if (GameModeManager.Instance == null) return;
            float elapsed = GameModeManager.Instance.ElapsedTime;

            // Handle unlock transition (Zone C).
            if (!unlocked)
            {
                if (elapsed >= lockUntilMatchTime)
                {
                    unlocked = true;
                    GameEvents.RaiseZoneCUnlocked();
                }
                else return;
            }

            CountOccupants(out int teamA, out int teamB);
            UpdateProgress(teamA, teamB);
            UpdateScoring();
            ResetContestorsDecay();
        }

        void CountOccupants(out int teamA, out int teamB)
        {
            inside.Clear();
            teamA = 0; teamB = 0;
            int mask = LayerMask.GetMask(GameConstants.LayerPlayer);
            int count = Physics.OverlapSphereNonAlloc(transform.position, captureRadius, overlapBuffer, mask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var pc = overlapBuffer[i].GetComponentInParent<PlayerController>();
                if (pc == null || pc.health == null || pc.health.IsDead) continue;
                if (inside.Contains(pc)) continue;
                // Use flat distance for fairness on the elevated Zone C platform.
                if (GameExtensions.FlatDistance(pc.transform.position, transform.position) > captureRadius) continue;
                inside.Add(pc);
                if (pc.Team == Team.A) teamA++;
                else if (pc.Team == Team.B) teamB++;
            }
        }

        void UpdateProgress(int teamA, int teamB)
        {
            bool aPresent = teamA > 0, bPresent = teamB > 0;

            // Contested: both teams present → progress frozen, flag for capture-value bonus.
            if (aPresent && bPresent)
            {
                contestedDuringCapture = true;
                return;
            }

            Team capper = aPresent ? Team.A : (bPresent ? Team.B : Team.None);
            if (capper == Team.None) return;  // nobody present → progress holds

            int contributors = capper == Team.A ? teamA : teamB;
            float speed = Mathf.Min(maxSpeedup, 1f + (contributors - 1) * perPlayerSpeedup);
            float delta = (speed / captureSeconds) * Time.deltaTime;

            if (capper == owningTeam)
            {
                // Already own it — keep progress full.
                progress = 1f;
                progressTeam = capper;
                return;
            }

            if (progressTeam == capper || progressTeam == Team.None)
            {
                progressTeam = capper;
                progress = Mathf.MoveTowards(progress, 1f, delta);
            }
            else
            {
                // Opposite team chipping the existing progress down first.
                progress = Mathf.MoveTowards(progress, 0f, delta);
                if (progress <= 0f) progressTeam = capper;
            }

            GameEvents.RaiseZoneProgress(zoneId, progress);

            if (progress >= 1f && owningTeam != capper)
                CompleteCapture(capper);
        }

        void CompleteCapture(Team newOwner)
        {
            Team previous = owningTeam;
            owningTeam = newOwner;
            progress = 1f;
            progressTeam = newOwner;

            bool wasContested = contestedDuringCapture || previous == GameConstants.Opponent(newOwner);
            contestedDuringCapture = false;

            int score = wasContested ? GameConstants.ScoreCaptureContested : GameConstants.ScoreCaptureNeutral;
            GameModeManager.Instance?.AddScore(newOwner, score);

            GameEvents.RaiseZoneCaptured(new ZoneCaptureEvent(zoneId, newOwner, previous, wasContested));
            CarrotClash.Audio.AudioManager.Instance?.PlaySfx("zone_capture_complete", transform.position);
        }

        void UpdateScoring()
        {
            if (owningTeam == Team.None || progress < 1f) return;

            tickAccumulator += Time.deltaTime;
            if (tickAccumulator >= 1f)
            {
                tickAccumulator -= 1f;
                GameModeManager.Instance?.AddScore(owningTeam, scoreTickRate);
            }
        }

        void ResetContestorsDecay()
        {
            // Standing on a contested or owned zone resets momentum decay (doc).
            foreach (var pc in inside)
                pc.momentum?.ResetDecayTimer();
        }

        public int CountTeam(Team team)
        {
            int n = 0;
            foreach (var pc in inside) if (pc.Team == team) n++;
            return n;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, captureRadius);
        }
    }
}
