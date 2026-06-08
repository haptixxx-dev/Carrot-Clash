using System;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>Per-tier multipliers and presentation. Array index == tier (0..3).</summary>
    [Serializable]
    public struct MomentumTierData
    {
        [Tooltip("Additive move-speed bonus (0 / 0.10 / 0.20 / 0.30).")]
        public float moveSpeedBonus;
        [Tooltip("Cooldown reduction fraction (0 / 0.10 / 0.20 / 0.30).")]
        public float cooldownReduction;
        [Tooltip("Additive damage bonus (0 / 0 / 0.10 / 0.20).")]
        public float damageBonus;
        [Tooltip("Team score awarded for killing a victim AT this tier (5 / 5 / 5 / 8).")]
        public int teamScoreKillValue;
        public Color vfxColor;
    }

    /// <summary>
    /// Tunable momentum-system configuration (Momentum System doc). One shared asset for the match;
    /// per-class deviations (Potato transfer, Jalapeño decay) are handled in code via class passives.
    /// </summary>
    [CreateAssetMenu(menuName = "Carrot Clash/Momentum Config", fileName = "MomentumConfig")]
    public class MomentumConfigSO : ScriptableObject
    {
        [Tooltip("Seconds without a kill/objective before losing one tier.")]
        public float decayIntervalSeconds = GameConstants.MomentumDecayInterval;

        [Tooltip("Default fraction of charge transferred to killer on death (Potato overrides to 0.25).")]
        [Range(0f, 1f)] public float transferOnDeath = GameConstants.MomentumTransferOnDeath;

        [Tooltip("Index = tier 0..3.")]
        public MomentumTierData[] tiers = new MomentumTierData[4];

        void OnValidate()
        {
            if (tiers == null || tiers.Length != 4)
            {
                var resized = new MomentumTierData[4];
                if (tiers != null)
                    Array.Copy(tiers, resized, Mathf.Min(tiers.Length, 4));
                tiers = resized;
            }
        }

        MomentumTierData Get(int tier) => tiers[Mathf.Clamp(tier, 0, GameConstants.MaxTier)];

        public float SpeedMultiplier(int tier) => 1f + Get(tier).moveSpeedBonus;
        public float CooldownMultiplier(int tier) => 1f - Get(tier).cooldownReduction;
        public float DamageMultiplier(int tier) => 1f + Get(tier).damageBonus;
        public int KillScoreValue(int victimTier) => Get(victimTier).teamScoreKillValue;
        public Color TierColor(int tier) => Get(tier).vfxColor;
    }
}
