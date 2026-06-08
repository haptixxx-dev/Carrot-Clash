using System;
using System.Collections.Generic;

namespace CarrotClash.Progression
{
    /// <summary>
    /// The kinds of behaviour a daily challenge can ask for. The pool is sampled (without
    /// duplicates) to build the player's three dailies. All progression is cosmetic — these only
    /// gate XP, never gameplay power.
    /// </summary>
    public enum ChallengeType
    {
        Kills,
        Captures,
        ReachTier3,
        WinMatch,
        DamageWithClass,
        Assists,
        HoldObjective
    }

    /// <summary>
    /// Serializable root of all player progression. Persisted as JSON via <see cref="SaveSystem"/>.
    /// Cosmetic-only: level/prestige are social signalling, battle pass is cosmetic unlocks.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public int level = 1;
        public int xp;                 // XP accumulated toward the next level (resets on level-up)
        public int prestige;           // number of times the player has hit max level and reset
        public int battlePassTier;     // 0..50
        public int battlePassXp;       // XP accumulated toward the next battle pass tier
        public List<string> ownedCosmetics = new List<string>();
        public long lastDailyResetUtcTicks;       // UTC ticks of the last midnight roll-over applied
        public List<DailyChallenge> dailyChallenges = new List<DailyChallenge>();
        public int dailyRerollsUsed;              // free reroll budget consumed for the current day
    }

    /// <summary>
    /// A single daily challenge instance. <see cref="progress"/> advances toward <see cref="target"/>;
    /// when it meets the target the challenge is <see cref="completed"/> and its XP is awarded once.
    /// </summary>
    [Serializable]
    public class DailyChallenge
    {
        public string id;
        public string description;
        public int target = 1;
        public int progress;
        public int xpReward = 150;
        public bool completed;
        public ChallengeType type;

        /// <summary>0..1 fraction toward completion, clamped.</summary>
        public float Fraction => target <= 0 ? 1f : UnityEngine.Mathf.Clamp01((float)progress / target);
    }
}
