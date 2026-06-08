using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Match orchestrator (Core Loop doc). Owns the state machine, match timer, team scores,
    /// win condition, Zone C unlock, sudden death, and match-end resolution. Singleton; in
    /// networked play GameModeNetworkManager mirrors timer/scores and calls into this on the host.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameModeManager : MonoBehaviour
    {
        public static GameModeManager Instance { get; private set; }

        [Header("Config")]
        [SerializeField] float matchDuration = GameConstants.MatchDuration;
        [SerializeField] int scoreCap = GameConstants.ScoreCap;
        [SerializeField] bool autoStart = true;     // false in networked play (host starts)
        [SerializeField] List<CaptureZone> zones = new List<CaptureZone>();

        readonly int[] teamScores = new int[GameConstants.TeamCount];
        readonly List<PlayerController> players = new List<PlayerController>(GameConstants.MaxPlayers);

        MatchState state = MatchState.Lobby;
        float elapsedTime;
        float stateTimer;
        bool zoneCUnlockedAnnounced;
        bool oneMinuteWarned, thirtySecWarned;

        public MatchStats Stats { get; } = new MatchStats();

        public MatchState State => state;
        public float ElapsedTime => elapsedTime;
        public float RemainingTime => Mathf.Max(0f, matchDuration - elapsedTime);
        public int ScoreCap => scoreCap;
        public IReadOnlyList<PlayerController> Players => players;

        public int GetScore(Team team) => team == Team.None ? 0 : teamScores[(int)team];

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Stats.Subscribe();
            GameEvents.OnKill += HandleKillScore;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Stats.Unsubscribe();
            GameEvents.OnKill -= HandleKillScore;
        }

        /// <summary>Award team score for a kill, using the victim's tier for the on-fire bonus.</summary>
        void HandleKillScore(KillEvent e)
        {
            if (e.Killer == null || e.Killer == e.Victim) return;
            if (!e.Killer.Team.IsEnemyOf(e.Victim != null ? e.Victim.Team : Team.None)) return;
            AddKillScore(e.Killer.Team, e.VictimTier);
        }

        void Start()
        {
            if (autoStart) BeginMatchFlow();
        }

        // ----- Registration -----
        public void RegisterPlayer(PlayerController p)
        {
            if (p != null && !players.Contains(p)) { players.Add(p); Stats.GetOrCreate(p); }
        }
        public void UnregisterPlayer(PlayerController p) => players.Remove(p);
        public void RegisterZone(CaptureZone z) { if (z != null && !zones.Contains(z)) zones.Add(z); }

        // ----- State machine -----
        public void BeginMatchFlow() => StartCoroutine(MatchFlow());

        IEnumerator MatchFlow()
        {
            SetState(MatchState.ClassSelect);
            PauseMomentumDecay(true);
            yield return Wait(GameConstants.ClassSelectDuration);

            SetState(MatchState.Countdown);
            yield return Wait(GameConstants.CountdownDuration);

            SetState(MatchState.MatchActive);
            PauseMomentumDecay(false);

            while (state == MatchState.MatchActive)
            {
                Tick(Time.deltaTime);
                yield return null;
            }
        }

        void Tick(float dt)
        {
            elapsedTime += dt;
            GameEvents.RaiseMatchTimerTick(RemainingTime);

            // Zone C unlock announce (zones self-unlock; this is the global cue).
            if (!zoneCUnlockedAnnounced && GameConstants.IsZoneCUnlocked(elapsedTime))
            {
                zoneCUnlockedAnnounced = true;
                CarrotClash.Audio.AudioManager.Instance?.PlayUi("zone_c_unlock");
            }

            // Timer warnings (Core Loop match-timer table).
            if (!oneMinuteWarned && RemainingTime <= 60f) { oneMinuteWarned = true; CarrotClash.Audio.AudioManager.Instance?.PlayUi("warn_1min"); }
            if (!thirtySecWarned && RemainingTime <= 30f) { thirtySecWarned = true; CarrotClash.Audio.AudioManager.Instance?.PlayUi("warn_30s"); }

            // Accrue objective time for players on a held/contested zone.
            AccrueObjectiveTime(dt);

            // Win by timer.
            if (elapsedTime >= matchDuration)
                ResolveTimerEnd();
        }

        void AccrueObjectiveTime(float dt)
        {
            foreach (var z in zones)
            {
                if (z == null || !z.IsUnlocked) continue;
                foreach (var p in players)
                {
                    if (p == null || p.health == null || p.health.IsDead) continue;
                    if (GameExtensions.FlatDistance(p.transform.position, z.transform.position) <= z.captureRadius)
                        Stats.AddObjectiveTime(p, dt);
                }
            }
        }

        /// <summary>Add team score; checks the early-win cap. Server-side authoritative.</summary>
        public void AddScore(Team team, int amount)
        {
            if (team == Team.None || amount == 0) return;
            int idx = (int)team;
            teamScores[idx] = Mathf.Max(0, teamScores[idx] + amount);
            GameEvents.RaiseScoreChanged(idx, teamScores[idx]);

            if (state == MatchState.MatchActive && teamScores[idx] >= scoreCap)
                EndMatch(team);
        }

        /// <summary>Credit a kill's team score (called by the kill-scoring listener with tier-aware value).</summary>
        public void AddKillScore(Team killerTeam, MomentumTier victimTier)
        {
            int value = victimTier == MomentumTier.OnFire ? GameConstants.ScoreKillOnFire : GameConstants.ScoreKill;
            AddScore(killerTeam, value);
        }

        void ResolveTimerEnd()
        {
            int a = teamScores[(int)Team.A];
            int b = teamScores[(int)Team.B];
            if (a == b)
            {
                StartCoroutine(SuddenDeath());
            }
            else
            {
                EndMatch(a > b ? Team.A : Team.B);
            }
        }

        IEnumerator SuddenDeath()
        {
            SetState(MatchState.SuddenDeath);
            stateTimer = GameConstants.SuddenDeathDuration;
            int startA = teamScores[(int)Team.A];
            int startB = teamScores[(int)Team.B];

            while (stateTimer > 0f && state == MatchState.SuddenDeath)
            {
                stateTimer -= Time.deltaTime;
                GameEvents.RaiseMatchTimerTick(stateTimer);
                AccrueObjectiveTime(Time.deltaTime);

                // Next team to score wins immediately.
                if (teamScores[(int)Team.A] > startA) { EndMatch(Team.A); yield break; }
                if (teamScores[(int)Team.B] > startB) { EndMatch(Team.B); yield break; }
                yield return null;
            }

            // Still tied → draw.
            if (state == MatchState.SuddenDeath)
            {
                int a = teamScores[(int)Team.A], b = teamScores[(int)Team.B];
                EndMatch(a == b ? Team.None : (a > b ? Team.A : Team.B));
            }
        }

        public void EndMatch(Team winner)
        {
            if (state == MatchState.MatchEnd || state == MatchState.PostMatch) return;
            SetState(MatchState.MatchEnd);
            CarrotClash.Audio.AudioManager.Instance?.PlayUi(winner == Team.None ? "match_draw" : "match_win");
            GameEvents.RaiseMatchEnded(winner);
            StartCoroutine(PostMatch());
        }

        IEnumerator PostMatch()
        {
            SetState(MatchState.PostMatch);
            yield return Wait(GameConstants.PostMatchDuration);
            // Match flow ends here; MainMenu transition driven by UI / SceneFlow.
        }

        void SetState(MatchState newState)
        {
            if (state == newState) return;
            var old = state;
            state = newState;
            GameEvents.RaiseMatchStateChanged(old, newState);
        }

        void PauseMomentumDecay(bool paused)
        {
            foreach (var p in players) p?.momentum?.SetDecayPaused(paused);
        }

        IEnumerator Wait(float seconds)
        {
            stateTimer = seconds;
            while (stateTimer > 0f)
            {
                stateTimer -= Time.deltaTime;
                yield return null;
            }
        }

        public float StateTimeRemaining => stateTimer;
    }
}
