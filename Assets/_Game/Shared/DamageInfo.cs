using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Immutable description of a single damage application. Passed through
    /// <see cref="HealthController.TakeDamage"/> so every system (feedback, momentum, scoring,
    /// kill feed) can reason about who hit whom, how, and from where.
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly int Amount;
        public readonly DamageType Type;
        public readonly PlayerController Instigator;  // may be null (environment / fall)
        public readonly Vector3 HitPoint;
        public readonly Vector3 HitDirection;         // normalized, from instigator toward victim
        public readonly bool IsHeadshot;

        public DamageInfo(int amount, DamageType type, PlayerController instigator,
                          Vector3 hitPoint, Vector3 hitDirection, bool isHeadshot = false)
        {
            Amount = amount;
            Type = type;
            Instigator = instigator;
            HitPoint = hitPoint;
            HitDirection = hitDirection.sqrMagnitude > 0.0001f ? hitDirection.normalized : Vector3.forward;
            IsHeadshot = isHeadshot;
        }

        /// <summary>Convenience for environment / non-directional damage.</summary>
        public static DamageInfo Environmental(int amount, DamageType type)
            => new DamageInfo(amount, type, null, Vector3.zero, Vector3.zero, false);
    }

    /// <summary>
    /// Result returned from a damage application so callers (weapons, abilities) can drive feedback
    /// without re-querying the victim.
    /// </summary>
    public readonly struct DamageResult
    {
        public readonly int DamageApplied;     // actual HP removed (after temp-HP / falloff caps)
        public readonly bool HitTempHp;        // damage landed on Starch Armor
        public readonly bool WasLethal;        // this hit killed the victim
        public readonly bool WasHeadshot;

        public DamageResult(int damageApplied, bool hitTempHp, bool wasLethal, bool wasHeadshot)
        {
            DamageApplied = damageApplied;
            HitTempHp = hitTempHp;
            WasLethal = wasLethal;
            WasHeadshot = wasHeadshot;
        }

        public static readonly DamageResult None = new DamageResult(0, false, false, false);
    }
}
