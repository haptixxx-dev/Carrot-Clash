using System;
using UnityEngine;

namespace CarrotClash.Progression
{
    /// <summary>The category of cosmetic a battle pass tier grants (docs/progression.md tier table).</summary>
    public enum BattlePassRewardType
    {
        None,
        CharacterSkin,   // every 5 tiers
        WeaponSkin,      // every 10 tiers
        AbilityVfx,      // every 15 tiers
        VictoryPose,     // every 20 tiers
        Spray,           // scattered filler
        Prestige         // tier 50 capstone
    }

    /// <summary>Describes the reward unlocked at a given battle pass tier. Cosmetic only.</summary>
    public readonly struct BattlePassReward
    {
        public readonly int Tier;
        public readonly BattlePassRewardType Type;
        public readonly string CosmeticId;     // stable id stored in PlayerProfile.ownedCosmetics
        public readonly string DisplayName;
        public readonly bool IsFreeTrack;       // first 10 tiers are the free track

        public BattlePassReward(int tier, BattlePassRewardType type, string cosmeticId,
                                string displayName, bool isFreeTrack)
        {
            Tier = tier;
            Type = type;
            CosmeticId = cosmeticId;
            DisplayName = displayName;
            IsFreeTrack = isFreeTrack;
        }
    }

    /// <summary>
    /// Seasonal battle pass logic: 50 tiers unlocked purely with XP earned during the season.
    /// No purchases here — only XP accrual, tier advancement, and reward lookup. Cosmetics are
    /// recorded onto the supplied <see cref="PlayerProfile"/> and persisted by the caller/XPManager.
    /// </summary>
    public static class BattlePassService
    {
        public const int MaxTier = 50;
        public const int FreeTrackTiers = 10;
        public const int XpPerTier = 1000;     // flat XP per battle pass tier (time, not money)

        /// <summary>Raised when one or more battle pass tiers are unlocked. Argument is the new tier.</summary>
        public static event Action<int> OnTierUnlocked;

        /// <summary>XP required to advance one tier (flat, by design — predictable seasonal pacing).</summary>
        public static int XpForTier => XpPerTier;

        /// <summary>0..1 progress toward the next tier for the given profile.</summary>
        public static float TierFraction(PlayerProfile p)
        {
            if (p == null || p.battlePassTier >= MaxTier) return 1f;
            return Mathf.Clamp01((float)p.battlePassXp / XpPerTier);
        }

        /// <summary>
        /// Add XP to the battle pass track, rolling up tiers and recording any cosmetic rewards onto
        /// the profile. Caps at <see cref="MaxTier"/>. Returns the number of tiers gained.
        /// </summary>
        public static int AddBattlePassXp(PlayerProfile p, int amount)
        {
            if (p == null || amount <= 0 || p.battlePassTier >= MaxTier) return 0;

            int startTier = p.battlePassTier;
            p.battlePassXp += amount;

            while (p.battlePassTier < MaxTier && p.battlePassXp >= XpPerTier)
            {
                p.battlePassXp -= XpPerTier;
                p.battlePassTier++;
                GrantTierReward(p, p.battlePassTier);
                OnTierUnlocked?.Invoke(p.battlePassTier);
            }

            if (p.battlePassTier >= MaxTier) p.battlePassXp = 0; // no overflow past the cap

            return p.battlePassTier - startTier;
        }

        /// <summary>Record the cosmetic for a freshly-unlocked tier onto the profile (idempotent).</summary>
        static void GrantTierReward(PlayerProfile p, int tier)
        {
            BattlePassReward reward = GetReward(tier);
            if (reward.Type == BattlePassRewardType.None || string.IsNullOrEmpty(reward.CosmeticId))
                return;
            if (p.ownedCosmetics == null)
                p.ownedCosmetics = new System.Collections.Generic.List<string>();
            if (!p.ownedCosmetics.Contains(reward.CosmeticId))
                p.ownedCosmetics.Add(reward.CosmeticId);
        }

        /// <summary>
        /// Reward descriptor for a tier (1..50). Cadence per docs: tier 50 is the prestige capstone;
        /// every 20 a victory pose; every 15 an ability VFX; every 10 a weapon skin; every 5 a
        /// character skin; the rest are sprays. First 10 tiers are the free track.
        /// </summary>
        public static BattlePassReward GetReward(int tier)
        {
            if (tier < 1 || tier > MaxTier)
                return new BattlePassReward(tier, BattlePassRewardType.None, null, "Locked", tier <= FreeTrackTiers);

            bool free = tier <= FreeTrackTiers;

            if (tier == MaxTier)
                return new BattlePassReward(tier, BattlePassRewardType.Prestige,
                    "bp_s1_prestige", "Season 1 Champion", free);
            if (tier % 20 == 0)
                return new BattlePassReward(tier, BattlePassRewardType.VictoryPose,
                    $"bp_t{tier}_pose", $"Victory Pose (Tier {tier})", free);
            if (tier % 15 == 0)
                return new BattlePassReward(tier, BattlePassRewardType.AbilityVfx,
                    $"bp_t{tier}_vfx", $"Ability VFX Recolor (Tier {tier})", free);
            if (tier % 10 == 0)
                return new BattlePassReward(tier, BattlePassRewardType.WeaponSkin,
                    $"bp_t{tier}_weaponskin", $"Weapon Skin (Tier {tier})", free);
            if (tier % 5 == 0)
                return new BattlePassReward(tier, BattlePassRewardType.CharacterSkin,
                    $"bp_t{tier}_skin", $"Character Skin (Tier {tier})", free);

            return new BattlePassReward(tier, BattlePassRewardType.Spray,
                $"bp_t{tier}_spray", $"Spray (Tier {tier})", free);
        }
    }
}
