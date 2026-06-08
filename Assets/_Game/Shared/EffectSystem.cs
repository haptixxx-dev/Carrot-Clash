using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Static façade for applying status effects to players. Centralises the rules from the
    /// Ability Interactions doc so abilities don't each re-implement ordering/stacking. Effects map
    /// onto existing subsystems (movement for slow/knockback, health for fire/temp-HP) rather than
    /// adding bespoke components, keeping state in one authoritative place per concern.
    /// </summary>
    public static class EffectSystem
    {
        /// <summary>Spice Burst slow. Reapplication resets duration; no compounding (movement enforces the floor).</summary>
        public static void ApplySlow(PlayerController target, float amount, float duration)
        {
            if (target == null || target.movement == null || target.health.IsDead) return;
            target.movement.ApplySlow(amount, duration);
            Audio.AudioManager.Instance?.PlaySfx("spice_slow_apply", target.transform.position);
        }

        /// <summary>
        /// Fire damage over time (Heat Trail / Burn Streak). Routed through a managed ticker on the
        /// target so multiple sources stack as separate ticks. Fire hits temp HP first (Starch protects).
        /// </summary>
        public static void ApplyFire(PlayerController target, float dps, float duration, PlayerController instigator)
        {
            if (target == null || target.health.IsDead) return;
            var ticker = target.GetComponent<FireDamageTicker>();
            if (ticker == null) ticker = target.gameObject.AddComponent<FireDamageTicker>();
            ticker.AddSource(dps, duration, instigator);
        }

        /// <summary>Earthen Slam knockback. Computes impulse away from <paramref name="sourcePosition"/> with upward bounce.</summary>
        public static void ApplyKnockback(PlayerController target, Vector3 sourcePosition, float distance, float stagger)
        {
            if (target == null || target.movement == null || target.health.IsDead) return;

            Vector3 away = (target.transform.position - sourcePosition).Flat();
            if (away.sqrMagnitude < 0.01f) away = -target.transform.forward.Flat();
            away.Normalize();

            // Add upward elevation component (doc: ~15° bounce).
            Vector3 dir = Quaternion.AngleAxis(-GameConstants.KnockbackElevation, Vector3.Cross(away, Vector3.up)) * away;

            // Slow carries through impulse proportionally — a slowed target travels less far.
            float effectiveDistance = distance;
            // velocity = distance / duration; movement decays the impulse over KnockbackDuration.
            float speed = effectiveDistance / GameConstants.KnockbackDuration;
            target.movement.ApplyKnockback(dir * speed, stagger);

            if (target.IsLocal && target.cam != null)
                target.cam.Shake(0.25f, 0.2f, ShakeType.Directional, dir);
        }

        /// <summary>Starch Armor temporary HP.</summary>
        public static void ApplyTemporaryHP(PlayerController target, int amount, float duration)
        {
            if (target == null || target.health == null) return;
            target.health.AddTemporaryHP(amount, duration);
            Audio.AudioManager.Instance?.PlaySfx("starch_activate", target.transform.position);
        }
    }

    /// <summary>
    /// Per-target managed fire ticker. Tracks N concurrent fire sources, each ticking DPS at
    /// <see cref="GameConstants.FireTickInterval"/> until its own duration expires. Self-destructs
    /// when no sources remain. Fire damage is dealt to temp HP first (handled by HealthController).
    /// </summary>
    [DisallowMultipleComponent]
    public class FireDamageTicker : MonoBehaviour
    {
        struct Source { public float dps; public float expireTime; public PlayerController instigator; }

        readonly System.Collections.Generic.List<Source> sources = new System.Collections.Generic.List<Source>(4);
        HealthController health;
        float tickAccumulator;

        void Awake() => health = GetComponent<HealthController>();

        public void AddSource(float dps, float duration, PlayerController instigator)
        {
            sources.Add(new Source { dps = dps, expireTime = Time.time + duration, instigator = instigator });
        }

        void Update()
        {
            if (sources.Count == 0 || health == null || health.IsDead) { sources.Clear(); enabled = false; Destroy(this); return; }

            // Expire finished sources.
            for (int i = sources.Count - 1; i >= 0; i--)
                if (Time.time >= sources[i].expireTime) sources.RemoveAt(i);

            if (sources.Count == 0) { Destroy(this); return; }

            tickAccumulator += Time.deltaTime;
            if (tickAccumulator < GameConstants.FireTickInterval) return;
            tickAccumulator -= GameConstants.FireTickInterval;

            foreach (var s in sources)
            {
                int dmg = Mathf.Max(1, Mathf.RoundToInt(s.dps * GameConstants.FireTickInterval));
                var info = new DamageInfo(dmg, DamageType.Fire, s.instigator,
                                          transform.position, Vector3.zero, false);
                health.TakeDamage(info);
                if (health.IsDead) break;
            }
        }
    }
}
