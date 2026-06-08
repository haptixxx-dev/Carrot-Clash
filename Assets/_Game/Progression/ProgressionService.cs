using System.Collections.Generic;

namespace CarrotClash.Progression
{
    /// <summary>
    /// Thin read-mostly facade over the progression subsystem so menus/HUD can display level, XP,
    /// battle pass, and daily challenge state without depending on the internals of
    /// <see cref="XPManager"/>, <see cref="BattlePassService"/>, or <see cref="SaveSystem"/>.
    ///
    /// Match-end ingestion lives here too (<see cref="AwardMatch"/>) so callers funnel through one
    /// place rather than poking the individual services.
    /// </summary>
    public static class ProgressionService
    {
        /// <summary>The current player profile (loaded lazily by <see cref="XPManager"/>).</summary>
        public static PlayerProfile Profile => XPManager.Profile;

        // ----- Level / XP -----
        public static int Level => XPManager.Level;
        public static int Prestige => XPManager.Prestige;
        public static int CurrentXp => XPManager.CurrentXp;
        public static int XpToNextLevel => XPManager.XpToNext;

        /// <summary>0..1 progress toward the next level (1 at max level).</summary>
        public static float LevelFraction
        {
            get
            {
                int toNext = XPManager.XpToNext;
                if (toNext <= 0) return 1f;
                return UnityEngine.Mathf.Clamp01((float)XPManager.CurrentXp / toNext);
            }
        }

        // ----- Battle pass -----
        public static int BattlePassTier => Profile.battlePassTier;
        public static int BattlePassXp => Profile.battlePassXp;
        public static int BattlePassXpToNext => BattlePassService.XpForTier;
        public static float BattlePassFraction => BattlePassService.TierFraction(Profile);

        /// <summary>Reward descriptor for a given battle pass tier (1..50).</summary>
        public static BattlePassReward GetBattlePassReward(int tier) => BattlePassService.GetReward(tier);

        // ----- Cosmetics -----
        public static IReadOnlyList<string> OwnedCosmetics => Profile.ownedCosmetics;
        public static bool OwnsCosmetic(string id) =>
            !string.IsNullOrEmpty(id) && Profile.ownedCosmetics != null && Profile.ownedCosmetics.Contains(id);

        // ----- Daily challenges -----
        public static IReadOnlyList<DailyChallenge> DailyChallenges => Profile.dailyChallenges;

        /// <summary>
        /// Award progression for a finished match: computes match XP from the player's stats, adds it
        /// to both the global level track and the battle pass track, and persists. Returns total XP.
        /// </summary>
        public static int AwardMatch(PlayerStats stats, bool win, bool isMvp = false)
        {
            int xp = XPManager.ComputeMatchXp(stats, win, isMvp);
            XPManager.AddXp(xp);                          // saves via SaveSystem internally
            BattlePassService.AddBattlePassXp(Profile, xp);
            SaveSystem.Save(Profile);
            return xp;
        }
    }
}
