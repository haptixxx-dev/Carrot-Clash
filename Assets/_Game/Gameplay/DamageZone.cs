using System.Collections.Generic;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// A trigger volume that damages enemy players standing inside it (Jalapeño's Heat Trail
    /// segments, and any other fire-based ground hazard). Damage is routed through
    /// <see cref="EffectSystem.ApplyFire"/> so it stacks as managed fire ticks (hitting Starch
    /// Armor temp HP first) rather than dealing raw damage every physics frame.
    ///
    /// Configure via <see cref="Configure"/> after instantiation. The zone self-destructs once its
    /// lifetime elapses. Only enemies of <see cref="ownerTeam"/> are affected; the instigator's own
    /// team is ignored. Requires a trigger collider on the same GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class DamageZone : MonoBehaviour
    {
        float dps;
        float expireTime;
        Team ownerTeam = Team.None;
        PlayerController instigator;
        bool configured;

        // Per-victim cooldown so this zone only feeds ONE managed fire source per victim at a time.
        // EffectSystem.ApplyFire appends a fresh source each call with no de-duplication, so we must
        // not re-apply every physics frame — we add a fire window of FireTickInterval and only refresh
        // it once the previous window has elapsed, yielding a steady ~dps stream (not 5x over-damage).
        readonly Dictionary<PlayerController, float> nextApplyTime = new Dictionary<PlayerController, float>();

        /// <summary>
        /// Set up the zone. <paramref name="dps"/> is dealt to each enemy inside via short fire
        /// bursts; <paramref name="lifetime"/> destroys the zone afterward. Enemies are those whose
        /// team opposes <paramref name="ownerTeam"/>.
        /// </summary>
        public void Configure(float damagePerSecond, float lifetime, Team owningTeam, PlayerController source)
        {
            dps = damagePerSecond;
            expireTime = Time.time + Mathf.Max(0f, lifetime);
            ownerTeam = owningTeam;
            instigator = source;
            configured = true;

            // Ensure the collider acts as a trigger so it never physically pushes players.
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            if (lifetime > 0f) Destroy(gameObject, lifetime);
        }

        void OnTriggerStay(Collider other)
        {
            if (!configured) return;
            if (Time.time >= expireTime) return;

            PlayerController victim = other.GetComponentInParent<PlayerController>();
            if (victim == null || victim == instigator) return;
            if (victim.health == null || victim.health.IsDead) return;
            if (!ownerTeam.IsEnemyOf(victim.Team)) return;

            // OnTriggerStay fires every physics frame (~50Hz), but EffectSystem.ApplyFire appends a
            // new managed source each call with no merging. Adding one every frame would stack ~5
            // overlapping FireTickInterval windows and deal ~5x the intended dps. Gate so this zone
            // only refreshes a victim's fire source once the previous window has fully elapsed.
            if (nextApplyTime.TryGetValue(victim, out float next) && Time.time < next) return;
            nextApplyTime[victim] = Time.time + GameConstants.FireTickInterval;

            EffectSystem.ApplyFire(victim, dps, GameConstants.FireTickInterval, instigator);
        }

        void OnTriggerExit(Collider other)
        {
            if (nextApplyTime.Count == 0) return;
            PlayerController victim = other.GetComponentInParent<PlayerController>();
            if (victim != null) nextApplyTime.Remove(victim);
        }
    }
}
