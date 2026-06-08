using System.Collections.Generic;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>Accumulated per-player statistics for the post-match screen, MVP, and XP.</summary>
    public class PlayerStats
    {
        public int PlayerId;
        public Team Team;
        public ClassId Class;
        public string DisplayName = "Player";
        public bool IsLocal;
        public bool IsBot;

        public int Kills;
        public int Assists;
        public int Deaths;
        public int DamageDealt;
        public float ObjectiveTime;           // seconds spent on a held/contested zone
        public int ZoneCaptures;
        public float LongestOnFireStreak;     // seconds at tier 3 unbroken (Hot Streak award)

        /// <summary>MVP metric: kills + objective contribution.</summary>
        public int ScoreContribution => Kills * GameConstants.ScoreKill
                                        + Assists * GameConstants.ScoreAssist
                                        + Mathf.RoundToInt(ObjectiveTime);
    }

    /// <summary>
    /// Tracks per-player statistics across a match by subscribing to GameEvents. Lives on the
    /// GameModeManager. Provides MVP / Hot Streak resolution for the post-match screen.
    /// </summary>
    public class MatchStats
    {
        readonly Dictionary<int, PlayerStats> stats = new Dictionary<int, PlayerStats>(GameConstants.MaxPlayers);
        readonly Dictionary<int, float> onFireSince = new Dictionary<int, float>();

        public IReadOnlyDictionary<int, PlayerStats> All => stats;

        public PlayerStats GetOrCreate(PlayerController p)
        {
            if (p == null) return null;
            if (!stats.TryGetValue(p.PlayerId, out var s))
            {
                s = new PlayerStats
                {
                    PlayerId = p.PlayerId,
                    Team = p.Team,
                    Class = p.ClassId,
                    IsLocal = p.IsLocal,
                    IsBot = p.IsBot,
                };
                stats[p.PlayerId] = s;
            }
            return s;
        }

        public void Subscribe()
        {
            GameEvents.OnKill += HandleKill;
            GameEvents.OnAssist += HandleAssist;
            GameEvents.OnDamageDealt += HandleDamage;
            GameEvents.OnTierChanged += HandleTier;
            GameEvents.OnZoneCaptured += HandleCapture;
        }

        public void Unsubscribe()
        {
            GameEvents.OnKill -= HandleKill;
            GameEvents.OnAssist -= HandleAssist;
            GameEvents.OnDamageDealt -= HandleDamage;
            GameEvents.OnTierChanged -= HandleTier;
            GameEvents.OnZoneCaptured -= HandleCapture;
        }

        void HandleKill(KillEvent e)
        {
            if (e.Killer != null) GetOrCreate(e.Killer).Kills++;
            if (e.Victim != null) GetOrCreate(e.Victim).Deaths++;
        }

        void HandleAssist(PlayerController p, int amount)
        {
            if (p != null) GetOrCreate(p).Assists += amount;
        }

        void HandleDamage(PlayerController victim, DamageInfo info, DamageResult result)
        {
            if (info.Instigator != null && result.DamageApplied > 0)
                GetOrCreate(info.Instigator).DamageDealt += result.DamageApplied;
        }

        void HandleTier(PlayerController p, MomentumTier oldT, MomentumTier newT)
        {
            if (p == null) return;
            var s = GetOrCreate(p);
            if (newT == MomentumTier.OnFire && oldT != MomentumTier.OnFire)
            {
                onFireSince[p.PlayerId] = Time.time;
            }
            else if (oldT == MomentumTier.OnFire && newT != MomentumTier.OnFire)
            {
                if (onFireSince.TryGetValue(p.PlayerId, out float since))
                {
                    float dur = Time.time - since;
                    if (dur > s.LongestOnFireStreak) s.LongestOnFireStreak = dur;
                    onFireSince.Remove(p.PlayerId);
                }
            }
        }

        void HandleCapture(ZoneCaptureEvent e)
        {
            // Capture credit is team-wide; objective time accrual is handled by AddObjectiveTime.
        }

        /// <summary>Called per-frame by GameModeManager to accrue objective time for occupants.</summary>
        public void AddObjectiveTime(PlayerController p, float dt)
        {
            if (p != null) GetOrCreate(p).ObjectiveTime += dt;
        }

        public PlayerStats ResolveMvp()
        {
            PlayerStats best = null;
            foreach (var s in stats.Values)
                if (best == null || s.ScoreContribution > best.ScoreContribution) best = s;
            return best;
        }

        public PlayerStats ResolveHotStreak()
        {
            PlayerStats best = null;
            foreach (var s in stats.Values)
                if (best == null || s.LongestOnFireStreak > best.LongestOnFireStreak) best = s;
            return best != null && best.LongestOnFireStreak > 0f ? best : null;
        }

        public void Clear() { stats.Clear(); onFireSince.Clear(); }
    }
}
