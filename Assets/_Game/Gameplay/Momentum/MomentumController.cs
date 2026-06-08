using System;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Self-contained momentum tier tracker (Momentum System doc). No networking dependency —
    /// NetworkedMomentumController mirrors the tier as a NetworkVariable and calls into this.
    ///
    /// Charge model: a float accumulator [0..MaxTier]. Whole part = tier. Kills add charge,
    /// death resets to 0 and transfers a fraction to the killer, decay subtracts one tier per
    /// interval, objectives/kills reset the decay timer.
    /// </summary>
    [DisallowMultipleComponent]
    public class MomentumController : MonoBehaviour
    {
        [SerializeField] MomentumConfigSO config;

        float charge;          // 0..3, fractional during transfers
        int tier;              // cached floor(charge), 0..3
        float decayTimer;      // counts down; on 0 lose a tier
        bool decayPaused;      // true during ClassSelect/Countdown
        PlayerController owner;

        public event Action<MomentumTier, MomentumTier> OnTierChanged;  // old, new (local)
        public event Action<float> OnDecayProgress;                     // remaining fraction 0..1

        public int Tier => tier;
        public MomentumTier TierEnum => (MomentumTier)tier;
        public float Charge => charge;
        public bool IsOnFire => tier >= GameConstants.MaxTier;
        public MomentumConfigSO Config => config;

        // Multipliers consumed by movement / abilities / weapons.
        public float SpeedMultiplier => config != null ? config.SpeedMultiplier(tier) : 1f;
        public float CooldownMultiplier => config != null ? config.CooldownMultiplier(tier) : 1f;
        public float DamageMultiplier => config != null ? config.DamageMultiplier(tier) : 1f;

        public void Initialize(PlayerController player, MomentumConfigSO cfg)
        {
            owner = player;
            if (cfg != null) config = cfg;
            charge = 0f;
            tier = 0;
            decayTimer = 0f;
            decayPaused = false;
        }

        public void SetDecayPaused(bool paused) => decayPaused = paused;

        void Update()
        {
            if (tier <= 0 || decayPaused || config == null) return;

            decayTimer -= Time.deltaTime;
            OnDecayProgress?.Invoke(Mathf.Clamp01(decayTimer / config.decayIntervalSeconds));

            if (decayTimer <= 0f)
            {
                // Lose exactly one tier; keep the fractional remainder of the lower tier at 0.
                SetCharge(Mathf.Max(0f, Mathf.Floor(charge) - 1f), resetDecay: tier - 1 > 0);
            }
        }

        /// <summary>Register a kill by this player. <paramref name="isBackstab"/> => Carrot +2 tiers.</summary>
        public void RegisterKill(bool isBackstab = false)
        {
            int gain = isBackstab ? 2 : 1;

            if (tier >= GameConstants.MaxTier)
            {
                // Already capped: killing extends decay instead of overflowing (doc: +8s on tier3->tier3).
                ExtendDecay(GameConstants.OnFireOnFireDecayBonus);
            }
            else
            {
                SetCharge(Mathf.Min(GameConstants.MaxTier, Mathf.Floor(charge) + gain), resetDecay: true);
            }

            // Jalapeño Extended Streak passive: each kill adds to the decay timer.
            if (owner != null && owner.ClassId == ClassId.Jalapeno)
                ExtendDecay(GameConstants.JalapenoDecayExtension);
        }

        /// <summary>Assist credit — partial charge (does not by itself bump a tier unless it crosses a whole number).</summary>
        public void RegisterAssist()
        {
            if (tier >= GameConstants.MaxTier) { ExtendDecay(GameConstants.JalapenoDecayExtension); return; }
            SetCharge(Mathf.Min(GameConstants.MaxTier, charge + GameConstants.AssistChargeFraction), resetDecay: true);
        }

        /// <summary>Broccoli Shared Harvest: receive a fraction of a nearby ally's kill charge.</summary>
        public void AddCharge(float amount)
        {
            if (amount <= 0f) return;
            SetCharge(Mathf.Min(GameConstants.MaxTier, charge + amount), resetDecay: true);
        }

        /// <summary>Objective interaction (standing on a held/contested zone) resets the decay timer without changing tier.</summary>
        public void ResetDecayTimer()
        {
            if (tier > 0 && config != null) decayTimer = config.decayIntervalSeconds;
        }

        void ExtendDecay(float seconds)
        {
            if (config == null) return;
            // Cap at one full interval so it can't snowball unboundedly.
            decayTimer = Mathf.Min(config.decayIntervalSeconds, decayTimer + seconds);
        }

        /// <summary>
        /// On death: reset to tier 0 and return the charge fraction the killer should absorb.
        /// Potato's Stubborn Root halves the standard transfer. The killer's controller applies it.
        /// </summary>
        public float HandleDeath()
        {
            float transferFraction = (owner != null && owner.ClassId == ClassId.Potato)
                ? GameConstants.MomentumTransferPotato
                : (config != null ? config.transferOnDeath : GameConstants.MomentumTransferOnDeath);

            // Doc transfer math: transferred charge = victim tier * fraction, then killer gains +1 tier
            // (rounds up from any positive). We hand the killer a whole tier when transfer > 0.
            float transferred = tier * transferFraction;

            SetCharge(0f, resetDecay: false);
            return transferred;
        }

        /// <summary>Killer absorbs transferred charge. Any positive transfer yields at least +1 tier (doc).</summary>
        public void AbsorbTransfer(float transferredCharge)
        {
            if (transferredCharge <= 0f) return;

            if (tier >= GameConstants.MaxTier)
            {
                ExtendDecay(GameConstants.OnFireOnFireDecayBonus);
                return;
            }
            // "+1 (rounds up from 0.5)" — any positive transfer is at least one tier.
            int gain = Mathf.Max(1, Mathf.FloorToInt(transferredCharge + 0.5f));
            SetCharge(Mathf.Min(GameConstants.MaxTier, Mathf.Floor(charge) + gain), resetDecay: true);
        }

        void SetCharge(float newCharge, bool resetDecay)
        {
            charge = Mathf.Clamp(newCharge, 0f, GameConstants.MaxTier);
            int newTier = Mathf.FloorToInt(charge);

            if (resetDecay && newTier > 0 && config != null)
                decayTimer = config.decayIntervalSeconds;

            if (newTier != tier)
            {
                var old = (MomentumTier)tier;
                tier = newTier;
                var now = (MomentumTier)tier;
                OnTierChanged?.Invoke(old, now);
                if (owner != null) GameEvents.RaiseTierChanged(owner, old, now);

                if (tier == 0) decayTimer = 0f;
            }
        }

        /// <summary>Hard reset (used by NetworkedMomentumController when applying an authoritative tier).</summary>
        public void ForceTier(int newTier)
        {
            SetCharge(Mathf.Clamp(newTier, 0, GameConstants.MaxTier), resetDecay: newTier > 0);
        }
    }
}
