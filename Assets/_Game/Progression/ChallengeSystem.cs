using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarrotClash.Progression
{
    /// <summary>
    /// Owns the player's three daily challenges. Generates them from a pool, rolls over at UTC
    /// midnight (tracked on the profile), advances progress by listening to gameplay events for the
    /// local player, and awards XP through <see cref="XPManager"/> on completion. Cosmetic-only.
    ///
    /// Place this on a persistent object (e.g. the main-menu / app-root) so it survives match loads.
    /// </summary>
    [DisallowMultipleComponent]
    public class ChallengeSystem : MonoBehaviour
    {
        public const int DailyCount = 3;
        public const int FreeRerollsPerDay = DailyCount; // one free reroll per challenge per day

        /// <summary>Raised when a daily challenge transitions to completed (XP already granted).</summary>
        public static event Action<DailyChallenge> OnChallengeCompleted;

        /// <summary>Raised whenever the daily set changes (reset, reroll, or progress tick).</summary>
        public static event Action OnChallengesChanged;

        // Within-match counters for "single match" style challenges (reset each match).
        int matchKills;
        int matchAssists;
        int matchCaptures;
        int matchDamageWithClass;
        float matchObjectiveTime;
        bool reachedTier3ThisMatch;
        ClassId localClass = ClassId.Carrot;

        bool subscribed;

        PlayerProfile Profile => XPManager.Profile;

        /// <summary>The current daily set (never null; refreshes on enable).</summary>
        public IReadOnlyList<DailyChallenge> Dailies => Profile.dailyChallenges;

        void OnEnable()
        {
            RefreshDailyReset();
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void OnDestroy()
        {
            Unsubscribe();
        }

        void Subscribe()
        {
            if (subscribed) return;
            subscribed = true;
            GameEvents.OnKill += HandleKill;
            GameEvents.OnAssist += HandleAssist;
            GameEvents.OnZoneCaptured += HandleZoneCaptured;
            GameEvents.OnTierChanged += HandleTierChanged;
            GameEvents.OnDamageDealt += HandleDamage;
            GameEvents.OnMatchStateChanged += HandleMatchStateChanged;
            GameEvents.OnMatchEnded += HandleMatchEnded;
        }

        void Unsubscribe()
        {
            if (!subscribed) return;
            subscribed = false;
            GameEvents.OnKill -= HandleKill;
            GameEvents.OnAssist -= HandleAssist;
            GameEvents.OnZoneCaptured -= HandleZoneCaptured;
            GameEvents.OnTierChanged -= HandleTierChanged;
            GameEvents.OnDamageDealt -= HandleDamage;
            GameEvents.OnMatchStateChanged -= HandleMatchStateChanged;
            GameEvents.OnMatchEnded -= HandleMatchEnded;
        }

        // ----- Daily reset / generation -----

        /// <summary>Roll the daily set over if the stored reset day is before today's UTC midnight.</summary>
        public void RefreshDailyReset()
        {
            PlayerProfile p = Profile;
            DateTime todayUtcMidnight = DateTime.UtcNow.Date; // midnight UTC of today
            DateTime lastReset = p.lastDailyResetUtcTicks > 0
                ? new DateTime(p.lastDailyResetUtcTicks, DateTimeKind.Utc).Date
                : DateTime.MinValue;

            if (lastReset < todayUtcMidnight || p.dailyChallenges == null || p.dailyChallenges.Count != DailyCount)
            {
                GenerateDailies(p);
                p.lastDailyResetUtcTicks = todayUtcMidnight.Ticks;
                p.dailyRerollsUsed = 0;
                SaveAndNotify();
            }
        }

        /// <summary>Build a fresh set of <see cref="DailyCount"/> distinct challenges into the profile.</summary>
        void GenerateDailies(PlayerProfile p)
        {
            ChallengeType[] pool =
            {
                ChallengeType.Kills, ChallengeType.Captures, ChallengeType.ReachTier3,
                ChallengeType.WinMatch, ChallengeType.DamageWithClass, ChallengeType.Assists,
                ChallengeType.HoldObjective
            };

            // Fisher–Yates partial shuffle for distinct picks.
            for (int i = 0; i < pool.Length; i++)
            {
                int j = UnityEngine.Random.Range(i, pool.Length);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            p.dailyChallenges = new List<DailyChallenge>(DailyCount);
            for (int i = 0; i < DailyCount; i++)
                p.dailyChallenges.Add(MakeChallenge(pool[i]));
        }

        /// <summary>Create a single challenge instance for a given type, using docs reward values.</summary>
        static DailyChallenge MakeChallenge(ChallengeType type)
        {
            string guid = Guid.NewGuid().ToString("N").Substring(0, 8);
            switch (type)
            {
                case ChallengeType.Kills:
                    return new DailyChallenge { id = $"kills_{guid}", type = type, target = 10, xpReward = 200,
                        description = "Get 10 kills in a single match" };
                case ChallengeType.Captures:
                    return new DailyChallenge { id = $"caps_{guid}", type = type, target = 3, xpReward = 150,
                        description = "Capture 3 objectives" };
                case ChallengeType.ReachTier3:
                    return new DailyChallenge { id = $"tier3_{guid}", type = type, target = 1, xpReward = 150,
                        description = "Reach Momentum Tier 3 in a match" };
                case ChallengeType.WinMatch:
                    return new DailyChallenge { id = $"win_{guid}", type = type, target = 1, xpReward = 150,
                        description = "Win a match with your team" };
                case ChallengeType.DamageWithClass:
                    return new DailyChallenge { id = $"dmg_{guid}", type = type, target = 500, xpReward = 200,
                        description = "Deal 500 damage in a single match" };
                case ChallengeType.Assists:
                    return new DailyChallenge { id = $"assist_{guid}", type = type, target = 5, xpReward = 150,
                        description = "Perform 5 assists" };
                case ChallengeType.HoldObjective:
                    return new DailyChallenge { id = $"hold_{guid}", type = type, target = 60, xpReward = 150,
                        description = "Hold an objective for 60 seconds" };
                default:
                    return new DailyChallenge { id = $"misc_{guid}", type = ChallengeType.Kills, target = 5, xpReward = 100,
                        description = "Get 5 kills" };
            }
        }

        /// <summary>
        /// Reroll the challenge at <paramref name="index"/> into a new distinct type, if a free
        /// reroll is available for the day. Returns true if a reroll happened.
        /// </summary>
        public bool Reroll(int index)
        {
            PlayerProfile p = Profile;
            if (p.dailyChallenges == null || index < 0 || index >= p.dailyChallenges.Count) return false;
            if (p.dailyRerollsUsed >= FreeRerollsPerDay) return false;
            if (p.dailyChallenges[index].completed) return false;

            ChallengeType newType = PickDistinctType(p, index);
            p.dailyChallenges[index] = MakeChallenge(newType);
            p.dailyRerollsUsed++;
            SaveAndNotify();
            return true;
        }

        ChallengeType PickDistinctType(PlayerProfile p, int excludeIndex)
        {
            HashSet<ChallengeType> inUse = new HashSet<ChallengeType>();
            for (int i = 0; i < p.dailyChallenges.Count; i++)
                if (i != excludeIndex) inUse.Add(p.dailyChallenges[i].type);

            Array values = Enum.GetValues(typeof(ChallengeType));
            List<ChallengeType> candidates = new List<ChallengeType>();
            foreach (ChallengeType t in values)
                if (!inUse.Contains(t)) candidates.Add(t);

            if (candidates.Count == 0) return p.dailyChallenges[excludeIndex].type;
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        // ----- Event handlers (local player only) -----

        void HandleMatchStateChanged(MatchState oldState, MatchState newState)
        {
            // Reset per-match counters when a fresh match begins (countdown precedes MatchActive).
            if (newState == MatchState.Countdown)
                ResetMatchCounters();
        }

        void ResetMatchCounters()
        {
            matchKills = 0;
            matchAssists = 0;
            matchCaptures = 0;
            matchDamageWithClass = 0;
            matchObjectiveTime = 0f;
            reachedTier3ThisMatch = false;
        }

        bool IsLocal(PlayerController p) => p != null && p.IsLocal && !p.IsBot;

        void HandleKill(KillEvent e)
        {
            if (!IsLocal(e.Killer)) return;
            matchKills++;
            Advance(ChallengeType.Kills, matchKills, absolute: true);
        }

        void HandleAssist(PlayerController p, int amount)
        {
            if (!IsLocal(p)) return;
            matchAssists += amount;
            Advance(ChallengeType.Assists, matchAssists, absolute: true);
        }

        void HandleZoneCaptured(ZoneCaptureEvent e)
        {
            PlayerController local = ResolveLocalPlayer();
            if (local == null) return;
            // Credit a capture if the local player's team became the new owner.
            if (e.NewOwner == local.Team && e.NewOwner != Team.None && e.NewOwner != e.PreviousOwner)
            {
                matchCaptures++;
                Advance(ChallengeType.Captures, matchCaptures, absolute: true);
            }
        }

        void HandleTierChanged(PlayerController p, MomentumTier oldT, MomentumTier newT)
        {
            if (!IsLocal(p)) return;
            if (newT == MomentumTier.OnFire && !reachedTier3ThisMatch)
            {
                reachedTier3ThisMatch = true;
                Advance(ChallengeType.ReachTier3, 1, absolute: true);
            }
        }

        void HandleDamage(PlayerController victim, DamageInfo info, DamageResult result)
        {
            if (!IsLocal(info.Instigator) || result.DamageApplied <= 0) return;
            localClass = info.Instigator.ClassId;
            matchDamageWithClass += result.DamageApplied;
            Advance(ChallengeType.DamageWithClass, matchDamageWithClass, absolute: true);
        }

        void HandleMatchEnded(Team winner)
        {
            PlayerController local = ResolveLocalPlayer();

            // Win challenge.
            if (local != null && winner != Team.None && winner == local.Team)
                Advance(ChallengeType.WinMatch, 1, absolute: true);

            // Hold-objective uses accumulated match stats (no per-event for objective time).
            PlayerStats localStats = ResolveLocalStats(local);
            if (localStats != null)
            {
                matchObjectiveTime = localStats.ObjectiveTime;
                Advance(ChallengeType.HoldObjective, Mathf.FloorToInt(matchObjectiveTime), absolute: true);
            }
        }

        PlayerController ResolveLocalPlayer()
        {
            GameModeManager gm = GameModeManager.Instance;
            if (gm == null) return null;
            IReadOnlyList<PlayerController> players = gm.Players;
            for (int i = 0; i < players.Count; i++)
                if (IsLocal(players[i])) return players[i];
            return null;
        }

        PlayerStats ResolveLocalStats(PlayerController local)
        {
            GameModeManager gm = GameModeManager.Instance;
            if (gm == null || gm.Stats == null) return null;
            if (local != null && gm.Stats.All.TryGetValue(local.PlayerId, out PlayerStats s)) return s;
            foreach (PlayerStats st in gm.Stats.All.Values)
                if (st.IsLocal) return st;
            return null;
        }

        // ----- Progress advance / completion -----

        /// <summary>
        /// Advance every active challenge of <paramref name="type"/>. When <paramref name="absolute"/>
        /// is true, <paramref name="value"/> is the new progress (for single-match counters); otherwise
        /// it is an increment.
        /// </summary>
        void Advance(ChallengeType type, int value, bool absolute)
        {
            PlayerProfile p = Profile;
            if (p.dailyChallenges == null) return;

            bool changed = false;
            for (int i = 0; i < p.dailyChallenges.Count; i++)
            {
                DailyChallenge c = p.dailyChallenges[i];
                if (c.completed || c.type != type) continue;

                int newProgress = absolute ? Mathf.Max(c.progress, value) : c.progress + value;
                newProgress = Mathf.Min(newProgress, c.target);
                if (newProgress != c.progress)
                {
                    c.progress = newProgress;
                    changed = true;
                }

                if (!c.completed && c.progress >= c.target)
                {
                    c.completed = true;
                    changed = true;
                    XPManager.AddXp(c.xpReward);
                    CarrotClash.Audio.AudioManager.Instance?.PlayUi("ui_challenge_complete");
                    OnChallengeCompleted?.Invoke(c);
                }
            }

            if (changed) SaveAndNotify();
        }

        void SaveAndNotify()
        {
            SaveSystem.Save(Profile);
            OnChallengesChanged?.Invoke();
        }
    }
}
