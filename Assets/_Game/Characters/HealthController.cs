using System;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Owns a player's HP, temporary HP (Starch Armor), damage ordering, regen gating, and death.
    /// Server-authoritative in networked play; the mirror NetworkedHealthController writes HP here
    /// and lets this component own the local feedback. In offline/bot play this is the source of truth.
    ///
    /// Damage ordering (Ability Interactions doc): TempHP depletes before base HP for ALL damage types.
    /// Regen (Broccoli) restores base HP only and is skipped while TempHP &gt; 0.
    /// </summary>
    [DisallowMultipleComponent]
    public class HealthController : MonoBehaviour
    {
        [Header("Runtime (read-only)")]
        [SerializeField] int currentHP;
        [SerializeField] int maxHP = 90;
        [SerializeField] int tempHP;

        float tempHpExpireTime = -1f;
        float lastDamageTime = -999f;
        bool isDead;
        bool invulnerable;

        // Assist bookkeeping: instigator -> last-damage timestamp. Small fixed cap; players are few.
        readonly System.Collections.Generic.Dictionary<PlayerController, float> recentDamagers
            = new System.Collections.Generic.Dictionary<PlayerController, float>(8);

        PlayerController owner;

        // ----- Events (local; networking raises the global GameEvents) -----
        public event Action<int, int> OnHealthChanged;        // current, max
        public event Action<int> OnTempHpChanged;             // temp amount
        public event Action<DamageInfo, DamageResult> OnDamaged;
        public event Action<PlayerController> OnDeath;        // argument = killer (may be null)

        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;
        public int TempHP => tempHP;
        public bool IsDead => isDead;
        public bool IsFullHealth => currentHP >= maxHP && tempHP <= 0;
        public bool Invulnerable { get => invulnerable; set => invulnerable = value; }
        public float HealthFraction => maxHP > 0 ? (float)currentHP / maxHP : 0f;

        public void Initialize(PlayerController player, int maximumHP)
        {
            owner = player;
            maxHP = Mathf.Max(1, maximumHP);
            currentHP = maxHP;
            tempHP = 0;
            tempHpExpireTime = -1f;
            isDead = false;
            invulnerable = false;
            recentDamagers.Clear();
            OnHealthChanged?.Invoke(currentHP, maxHP);
            OnTempHpChanged?.Invoke(tempHP);
        }

        void Update()
        {
            if (tempHP > 0 && Time.time >= tempHpExpireTime)
            {
                tempHP = 0;
                OnTempHpChanged?.Invoke(tempHP);
            }
        }

        /// <summary>Apply damage. Returns the resolved result for feedback. Honours invulnerability, temp HP, Thick Skin.</summary>
        public DamageResult TakeDamage(in DamageInfo info)
        {
            if (isDead || invulnerable || info.Amount <= 0)
                return DamageResult.None;

            int incoming = info.Amount;

            // Potato Thick Skin: -8% while stationary > 1s. Movement owns the flag; query it.
            if (owner != null && owner.movement != null)
                incoming = Mathf.Max(1, Mathf.RoundToInt(incoming * owner.movement.IncomingDamageMultiplier));

            bool hitTemp = tempHP > 0;
            int remaining = incoming;

            if (tempHP > 0)
            {
                int absorbed = Mathf.Min(tempHP, remaining);
                tempHP -= absorbed;
                remaining -= absorbed;
                OnTempHpChanged?.Invoke(tempHP);
            }

            if (remaining > 0)
            {
                currentHP = Mathf.Max(0, currentHP - remaining);
                OnHealthChanged?.Invoke(currentHP, maxHP);
            }

            lastDamageTime = Time.time;
            if (info.Instigator != null && info.Instigator != owner)
                recentDamagers[info.Instigator] = Time.time;

            bool lethal = currentHP <= 0;
            var result = new DamageResult(incoming, hitTemp, lethal, info.IsHeadshot);

            OnDamaged?.Invoke(info, result);
            GameEvents.RaiseDamageDealt(owner, info, result);

            if (lethal)
                Die(info.Instigator, info.Type);

            return result;
        }

        /// <summary>Restore base HP (Broccoli Regen Aura). No effect while Starch Armor is active or when dead/full.</summary>
        public void Heal(int amount)
        {
            if (isDead || amount <= 0 || tempHP > 0) return;   // regen ignores temp HP
            if (currentHP >= maxHP) return;
            currentHP = Mathf.Min(maxHP, currentHP + amount);
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }

        /// <summary>Starch Armor. Adds a temporary HP buffer consumed before base HP, expiring after duration.</summary>
        public void AddTemporaryHP(int amount, float durationSeconds)
        {
            if (isDead || amount <= 0) return;
            tempHP += amount;
            tempHpExpireTime = Time.time + durationSeconds;
            OnTempHpChanged?.Invoke(tempHP);
        }

        /// <summary>Full restore on respawn.</summary>
        public void Revive()
        {
            isDead = false;
            currentHP = maxHP;
            tempHP = 0;
            tempHpExpireTime = -1f;
            recentDamagers.Clear();
            OnHealthChanged?.Invoke(currentHP, maxHP);
            OnTempHpChanged?.Invoke(tempHP);
        }

        void Die(PlayerController killer, DamageType killingBlowType)
        {
            if (isDead) return;
            isDead = true;
            currentHP = 0;
            tempHP = 0;
            OnTempHpChanged?.Invoke(tempHP);

            // Momentum transfer + tier reset is driven by PlayerController, which has full context
            // (backstab arc, tiers). It listens to OnDeath and assembles the KillEvent.
            OnDeath?.Invoke(killer);
        }

        /// <summary>Players who damaged this one within the assist window (excludes the killer, filled by caller).</summary>
        public void CollectAssisters(System.Collections.Generic.List<PlayerController> buffer, PlayerController excludeKiller)
        {
            buffer.Clear();
            float cutoff = Time.time - GameConstants.AssistWindow;
            foreach (var kv in recentDamagers)
            {
                if (kv.Key == null || kv.Key == excludeKiller) continue;
                if (kv.Value >= cutoff) buffer.Add(kv.Key);
            }
        }
    }
}
