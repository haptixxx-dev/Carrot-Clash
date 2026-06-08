using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Static configuration for one weapon. All combat weapons are hitscan (Weapons doc).
    /// One asset per weapon: The Nub, Pepper Blaster, The Stem, Root Cannon, The Pip.
    /// </summary>
    [CreateAssetMenu(menuName = "Carrot Clash/Weapon Data", fileName = "WeaponData")]
    public class WeaponDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName = "The Nub";

        [Header("Damage")]
        [Tooltip("Body damage per bullet (per pellet for shotguns).")]
        public int damageBody = 18;
        [Tooltip("Multiplier applied to body damage on a headshot.")]
        public float headshotMultiplier = 1.5f;

        [Header("Shotgun (optional)")]
        [Tooltip("Pellets fired per trigger pull. 1 for non-shotguns.")]
        public int pelletsPerShot = 1;
        [Tooltip("Hipfire cone half-angle in degrees for pellet spread / bloom.")]
        public float spreadAngle = 1.5f;

        [Header("Fire")]
        [Tooltip("Rounds per minute. Used to derive seconds-between-shots.")]
        public float fireRateRPM = 750f;
        public bool isFullAuto = true;

        [Header("Burst (Broccoli's Stem)")]
        public bool isBurst = false;
        public int burstCount = 3;
        [Tooltip("RPM within a burst (between the burst's own rounds).")]
        public float burstFireRateRPM = 1200f;
        [Tooltip("Delay between completed bursts, seconds.")]
        public float burstDelay = 0.85f;

        [Header("Ammo")]
        public int magazineSize = 30;
        public float reloadTime = 1.8f;

        [Header("Range / Falloff")]
        [Tooltip("Distance (m) up to which full damage applies.")]
        public float effectiveRange = 25f;
        [Tooltip("Shotgun pellets use a steeper falloff curve (50% at +3m).")]
        public bool steepFalloff = false;

        [Header("Movement")]
        [Tooltip("Move-speed multiplier while firing (Potato LMG = 0.85).")]
        [Range(0f, 1f)] public float moveSpeedWhileFiring = 1f;

        [Header("Feel")]
        [Tooltip("Pattern-based recoil offsets, applied in order then looping.")]
        public Vector2[] recoilPattern = new Vector2[0];
        public float recoilRecoveryTime = 0.2f;
        [Tooltip("Camera kick magnitude per shot (proportional to calibre).")]
        public float cameraKick = 0.2f;

        // ----- Derived helpers -----

        /// <summary>Seconds between shots from RPM.</summary>
        public float SecondsBetweenShots => fireRateRPM > 0f ? 60f / fireRateRPM : 0.1f;

        /// <summary>Seconds between rounds inside a burst.</summary>
        public float BurstSecondsBetweenRounds => burstFireRateRPM > 0f ? 60f / burstFireRateRPM : 0.05f;

        /// <summary>
        /// Damage multiplier from distance falloff (Weapons doc curve). 1.0 inside effective range.
        /// Steep curve (shotgun) halves at +3m and floors at 0.2.
        /// </summary>
        public float FalloffMultiplier(float distance)
        {
            if (distance <= effectiveRange) return 1f;
            float over = distance - effectiveRange;

            if (steepFalloff)
            {
                // 50% at +3m, linear down to a 0.2 floor by +9m.
                float t = Mathf.Clamp01(over / (GameConstants.ShotgunFalloffHalfRange * 3f));
                return Mathf.Lerp(1f, 0.2f, t);
            }

            if (over <= 5f) return GameConstants.FalloffStep5m;
            if (over <= 10f) return GameConstants.FalloffStep10m;
            if (over <= 15f) return GameConstants.FalloffStep15m;
            return GameConstants.FalloffStep20m;
        }

        /// <summary>Final damage for a hit at <paramref name="distance"/>, accounting for headshot + falloff.</summary>
        public int ComputeDamage(float distance, bool headshot, float momentumDamageMultiplier)
        {
            float dmg = damageBody;
            if (headshot) dmg *= headshotMultiplier;
            dmg *= FalloffMultiplier(distance);
            dmg *= momentumDamageMultiplier;
            return Mathf.Max(1, Mathf.RoundToInt(dmg));
        }
    }
}
