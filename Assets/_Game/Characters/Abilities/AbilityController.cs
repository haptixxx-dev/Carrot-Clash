using System;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Manages a player's two active ability slots plus passive and momentum-passive.
    /// Cooldowns respect <see cref="MomentumController.CooldownMultiplier"/> (lower tier cooldowns
    /// at higher momentum). Activation is blocked while staggered or dead.
    /// </summary>
    [DisallowMultipleComponent]
    public class AbilityController : MonoBehaviour
    {
        readonly AbilityBase[] actives = new AbilityBase[2];
        AbilityBase passive;
        AbilityBase momentumPassive;

        readonly float[] cooldownTimers = new float[2];
        readonly float[] cooldownDurations = new float[2];

        PlayerController owner;

        /// <summary>slot, remaining fraction 0..1 (1 = fully on cooldown).</summary>
        public event Action<int, float> OnCooldownChanged;
        /// <summary>slot fired (for HUD flash / SFX).</summary>
        public event Action<int> OnAbilityActivated;

        public AbilityBase GetActive(int slot) => (slot >= 0 && slot < 2) ? actives[slot] : null;
        public bool IsReady(int slot) => (slot >= 0 && slot < 2) && cooldownTimers[slot] <= 0f;
        public float CooldownRemaining(int slot) => (slot >= 0 && slot < 2) ? Mathf.Max(0f, cooldownTimers[slot]) : 0f;
        public float CooldownFraction(int slot)
            => (slot >= 0 && slot < 2 && cooldownDurations[slot] > 0f)
                ? Mathf.Clamp01(cooldownTimers[slot] / cooldownDurations[slot]) : 0f;

        public void Initialize(PlayerController player, CharacterDataSO data)
        {
            owner = player;

            // Concrete ability components are expected on the prefab (or added here by class).
            // We resolve them by their AbilityDataSO assignment so designers can swap kits.
            actives[0] = ResolveAbility(data.active1);
            actives[1] = ResolveAbility(data.active2);
            passive = ResolveAbility(data.passive);
            momentumPassive = ResolveAbility(data.momentumPassive);

            actives[0]?.Bind(player, data.active1);
            actives[1]?.Bind(player, data.active2);
            passive?.Bind(player, data.passive);
            momentumPassive?.Bind(player, data.momentumPassive);

            for (int i = 0; i < 2; i++) { cooldownTimers[i] = 0f; cooldownDurations[i] = 0f; }
        }

        /// <summary>
        /// Find the AbilityBase component implementing the given data asset. Abilities self-declare
        /// which AbilityDataSO they expect via their serialized data; we match on the component type
        /// the data's behaviour maps to. For the MVP, ability components live on the prefab and are
        /// matched by already having that data assigned, or are added by an AbilityFactory.
        /// </summary>
        AbilityBase ResolveAbility(AbilityDataSO config)
        {
            if (config == null) return null;
            var components = GetComponentsInChildren<AbilityBase>(true);
            foreach (var ab in components)
                if (ab.Data == config) return ab;
            // Fall back: let the factory attach the right behaviour for this data.
            return AbilityFactory.Attach(gameObject, config);
        }

        void Update()
        {
            if (owner == null) return;
            for (int i = 0; i < 2; i++)
            {
                if (cooldownTimers[i] > 0f)
                {
                    cooldownTimers[i] = Mathf.Max(0f, cooldownTimers[i] - Time.deltaTime);
                    OnCooldownChanged?.Invoke(i, CooldownFraction(i));
                }
            }
        }

        /// <summary>Attempt to activate slot 0 or 1. Server-validated in networked play.</summary>
        public bool ActivateAbility(int slot)
        {
            if (slot < 0 || slot > 1) return false;
            if (owner.health != null && owner.health.IsDead) return false;
            if (owner.movement != null && owner.movement.IsStaggered) return false;
            if (cooldownTimers[slot] > 0f) return false;

            var ability = actives[slot];
            if (ability == null) return false;

            bool fired = ability.Activate(owner);
            if (!fired) return false;

            float cd = ability.Cooldown * (owner.momentum != null ? owner.momentum.CooldownMultiplier : 1f);
            cooldownDurations[slot] = cd;
            cooldownTimers[slot] = cd;
            OnAbilityActivated?.Invoke(slot);
            OnCooldownChanged?.Invoke(slot, 1f);
            return true;
        }

        public void ResetCooldowns()
        {
            for (int i = 0; i < 2; i++)
            {
                cooldownTimers[i] = 0f;
                OnCooldownChanged?.Invoke(i, 0f);
            }
        }

        void OnDestroy()
        {
            actives[0]?.Unbind();
            actives[1]?.Unbind();
            passive?.Unbind();
            momentumPassive?.Unbind();
        }
    }
}
