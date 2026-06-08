using System.Collections;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Jalapeño Active 2 — Heat Trail. While active (<see cref="AbilityDataSO.duration"/>, ~3s)
    /// drops a fire <see cref="DamageZone"/> segment at the owner's feet every 0.1s. Each segment
    /// damages enemies standing in it for ~20 DPS (<see cref="AbilityDataSO.magnitude"/>) via
    /// <see cref="EffectSystem.ApplyFire"/>, lingering briefly after it's placed so the trail
    /// persists behind the moving Jalapeño. Segments are independent zones (no double-trigger with
    /// Burn Streak), per the Ability Interactions doc.
    /// </summary>
    public class Ability_HeatTrail : AbilityBase
    {
        const float SegmentInterval = 0.1f;   // doc: spawn at feet every 0.1s
        const float SegmentRadius = 0.7f;      // ~1m-wide trail
        const float SegmentLifetime = 0.6f;    // each puddle lingers so the trail trails behind

        Coroutine routine;

        public override bool Activate(PlayerController activator)
        {
            if (data == null || Owner == null) return false;
            if (routine != null) return false;   // already laying a trail

            PlayActivationFeedback();
            if (!string.IsNullOrEmpty(data.sfxLoop))
                Audio.AudioManager.Instance?.StartLoop(data.sfxLoop, transform.position);

            routine = StartCoroutine(LayTrail());
            return true;
        }

        IEnumerator LayTrail()
        {
            float duration = data.duration > 0f ? data.duration : 3f;
            float dps = data.magnitude > 0f ? data.magnitude : 20f;
            float elapsed = 0f;
            float sinceSegment = SegmentInterval;   // emit one immediately

            while (elapsed < duration)
            {
                if (Owner == null || Owner.health == null || Owner.health.IsDead) break;

                sinceSegment += Time.deltaTime;
                if (sinceSegment >= SegmentInterval)
                {
                    sinceSegment -= SegmentInterval;
                    SpawnSegment(dps);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            StopLoop();
            routine = null;
        }

        /// <summary>Drop one DamageZone puddle at the owner's feet.</summary>
        void SpawnSegment(float dps)
        {
            Vector3 footPos = Owner.transform.position;

            GameObject segment;
            if (data.effectPrefab != null)
            {
                segment = Instantiate(data.effectPrefab, footPos, Quaternion.identity);
            }
            else
            {
                // Placeholder zone: an invisible trigger sphere that still does the damage logic.
                segment = new GameObject("HeatTrailSegment");
                segment.transform.SetPositionAndRotation(footPos, Quaternion.identity);
                SphereCollider col = segment.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = SegmentRadius;
            }

            DamageZone zone = segment.GetComponent<DamageZone>();
            if (zone == null) zone = segment.AddComponent<DamageZone>();
            zone.Configure(dps, SegmentLifetime, Owner.Team, Owner);
        }

        void StopLoop()
        {
            if (data != null && !string.IsNullOrEmpty(data.sfxLoop))
                Audio.AudioManager.Instance?.StopLoop(data.sfxLoop);
        }

        public override void Cancel()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
            StopLoop();
        }

        void OnDisable() => Cancel();
    }
}
