using System;
using UnityEngine;

namespace CarrotClash.Progression
{
    /// <summary>
    /// Static XP/level service. Owns the canonical <see cref="PlayerProfile"/> in memory, applies
    /// the gentle 1–100 level curve (fast early, slow late), handles level-up and prestige, and
    /// persists through <see cref="SaveSystem"/>. Purely cosmetic — levels grant no gameplay power.
    ///
    /// XP award values come from docs/progression.md.
    /// </summary>
    public static class XPManager
    {
        public const int MaxLevel = 100;

        // ----- Match XP award table (docs/progression.md) -----
        public const int XpWin = 200;
        public const int XpLoss = 100;
        public const int XpPerKill = 15;
        public const int XpPerAssist = 8;
        public const int XpPerCapture = 25;
        public const int XpObjectivePer30s = 10;
        public const int XpHotStreak = 30;       // tier 3 held 60+ seconds
        public const int XpMvp = 50;
        public const float HotStreakSeconds = 60f;

        /// <summary>Raised when the player gains one or more levels. Argument is the new level.</summary>
        public static event Action<int> OnLevelUp;

        /// <summary>Raised whenever XP changes. Args: (currentXpIntoLevel, xpRequiredForNextLevel).</summary>
        public static event Action<int, int> OnXpChanged;

        static PlayerProfile profile;

        /// <summary>The live profile, loaded lazily from disk on first access.</summary>
        public static PlayerProfile Profile
        {
            get
            {
                if (profile == null) profile = SaveSystem.Load();
                return profile;
            }
        }

        public static int Level => Profile.level;
        public static int CurrentXp => Profile.xp;
        public static int Prestige => Profile.prestige;

        /// <summary>XP needed to advance from the current level to the next (0 at max level pre-prestige).</summary>
        public static int XpToNext => XpForLevel(Profile.level);

        /// <summary>Replace the cached profile (used by ChallengeSystem / tests after mutating it).</summary>
        public static void SetProfile(PlayerProfile p)
        {
            profile = p ?? SaveSystem.Load();
        }

        /// <summary>Force a reload from disk (e.g. after an external reset).</summary>
        public static void Reload()
        {
            profile = SaveSystem.Load();
            OnXpChanged?.Invoke(Profile.xp, XpToNext);
        }

        /// <summary>
        /// XP required to go from <paramref name="level"/> to level+1. Gentle curve: bands of
        /// 1–25 (fast), 26–50 (moderate), 51–100 (slow). Returns 0 once at/above max level.
        /// </summary>
        public static int XpForLevel(int level)
        {
            if (level >= MaxLevel) return 0;
            // Base 500 with a quadratic-ish ramp; band multipliers steepen the later tiers.
            float band;
            if (level <= 25) band = 1.0f;
            else if (level <= 50) band = 1.6f;
            else band = 2.4f;

            float cost = 500f + (level - 1) * 90f;   // linear core ramp
            return Mathf.RoundToInt(cost * band / 10f) * 10;   // round to nearest 10 for tidy bars
        }

        /// <summary>
        /// Add raw XP and roll up any level-ups (and prestige at the cap). Persists afterward.
        /// Fires <see cref="OnLevelUp"/> per level crossed and <see cref="OnXpChanged"/> once at the end.
        /// </summary>
        public static void AddXp(int amount)
        {
            if (amount <= 0) return;
            PlayerProfile p = Profile;
            p.xp += amount;

            int needed = XpForLevel(p.level);
            while (needed > 0 && p.xp >= needed)
            {
                p.xp -= needed;
                p.level++;
                if (p.level >= MaxLevel)
                {
                    // Hit the cap: hold at MaxLevel (prestige is an explicit player action elsewhere).
                    p.level = MaxLevel;
                    p.xp = Mathf.Min(p.xp, 0); // no overflow accrual past cap
                    OnLevelUp?.Invoke(p.level);
                    break;
                }
                OnLevelUp?.Invoke(p.level);
                needed = XpForLevel(p.level);
            }

            SaveSystem.Save(p);
            OnXpChanged?.Invoke(p.xp, XpForLevel(p.level));
        }

        /// <summary>
        /// Prestige: only valid at max level. Resets the visual level to 1 (keeping spillover XP),
        /// increments the prestige counter, and persists.
        /// </summary>
        public static bool Prestige_Reset()
        {
            PlayerProfile p = Profile;
            if (p.level < MaxLevel) return false;
            p.prestige++;
            p.level = 1;
            p.xp = 0;
            SaveSystem.Save(p);
            OnLevelUp?.Invoke(p.level);
            OnXpChanged?.Invoke(p.xp, XpForLevel(p.level));
            return true;
        }

        /// <summary>
        /// Compute the XP a player earned this match from their <see cref="PlayerStats"/>.
        /// Mirrors the docs table. <paramref name="win"/> selects participation XP; pass
        /// <paramref name="isMvp"/> for the MVP bonus.
        /// </summary>
        public static int ComputeMatchXp(PlayerStats stats, bool win, bool isMvp = false)
        {
            if (stats == null) return win ? XpWin : XpLoss;

            int xp = win ? XpWin : XpLoss;
            xp += stats.Kills * XpPerKill;
            xp += stats.Assists * XpPerAssist;
            xp += stats.ZoneCaptures * XpPerCapture;
            xp += Mathf.FloorToInt(stats.ObjectiveTime / 30f) * XpObjectivePer30s;
            if (stats.LongestOnFireStreak >= HotStreakSeconds) xp += XpHotStreak;
            if (isMvp) xp += XpMvp;
            return xp;
        }
    }
}
