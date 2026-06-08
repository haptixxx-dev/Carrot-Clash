using System.Collections.Generic;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Jalapeño Active 1 — Spice Burst. Lobs a spice grenade toward where the player is aiming
    /// (up to <see cref="AbilityDataSO.range"/>, ~20m). On impact it detonates: every enemy within
    /// <see cref="AbilityDataSO.radius"/> (~4m) is slowed (35% for 3s, per the Ability Interactions
    /// doc) and takes direct ability damage equal to <see cref="AbilityDataSO.magnitudeSecondary"/>
    /// (15). Slow and damage apply independently of any knockback (see doc interaction matrix).
    /// </summary>
    public class Ability_SpiceBurst : AbilityBase
    {
        // Enemies/environment the impact AOE can land on (player capsules + hitboxes).
        static readonly int OverlapMask =
            LayerMask.GetMask(GameConstants.LayerPlayer, GameConstants.LayerHitbox);

        readonly Collider[] overlapBuffer = new Collider[32];
        readonly HashSet<PlayerController> hitThisDetonation = new HashSet<PlayerController>();

        public override bool Activate(PlayerController activator)
        {
            if (data == null || Owner == null) return false;

            float range = data.range > 0f ? data.range : 20f;
            Vector3 impact = ResolveImpactPoint(range);

            Detonate(impact);
            PlayActivationFeedback();
            return true;
        }

        /// <summary>Trace the aim ray to find the detonation point, clamped to throw range.</summary>
        Vector3 ResolveImpactPoint(float range)
        {
            Ray ray = AimRay;
            if (Physics.Raycast(ray, out RaycastHit hit, range, PlacementMask, QueryTriggerInteraction.Ignore))
                return hit.point;
            return ray.origin + ray.direction * range;
        }

        /// <summary>Run the AOE: slow + direct ability damage to each unique enemy in the radius.</summary>
        void Detonate(Vector3 center)
        {
            if (data.effectPrefab != null)
                SpawnTimed(data.effectPrefab, center, Quaternion.identity, 2f);
            if (!string.IsNullOrEmpty(data.sfxImpact))
                Audio.AudioManager.Instance?.PlaySfx(data.sfxImpact, center);

            float radius = data.radius > 0f ? data.radius : 4f;
            int directDamage = data.magnitudeSecondary > 0f ? Mathf.RoundToInt(data.magnitudeSecondary) : 15;

            hitThisDetonation.Clear();
            int count = Physics.OverlapSphereNonAlloc(center, radius, overlapBuffer, OverlapMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider col = overlapBuffer[i];
                if (col == null) continue;

                PlayerController enemy = col.GetComponentInParent<PlayerController>();
                if (enemy == null || enemy == Owner) continue;
                if (enemy.health == null || enemy.health.IsDead) continue;
                if (!Owner.Team.IsEnemyOf(enemy.Team)) continue;
                if (!hitThisDetonation.Add(enemy)) continue;   // each player resolved once

                EffectSystem.ApplySlow(enemy, GameConstants.SlowAmount, GameConstants.SlowDuration);

                Vector3 dir = (enemy.transform.position - center).normalized;
                var info = new DamageInfo(directDamage, DamageType.Ability, Owner,
                                          enemy.transform.position, dir, false);
                enemy.ApplyDamage(info);
            }
        }
    }
}
