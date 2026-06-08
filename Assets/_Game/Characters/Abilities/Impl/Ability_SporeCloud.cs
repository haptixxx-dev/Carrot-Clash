using System.Collections.Generic;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Broccoli Active 2 — Spore Cloud. Throws along the aim ray to the first surface (or the end of
    /// range) and spawns a vision-blocking fog volume there for <see cref="AbilityDataSO.duration"/>
    /// (4s) over <see cref="AbilityDataSO.radius"/> (6m). Players inside the cloud are marked
    /// not-revealable via a <see cref="VisionObscured"/> component so reveal systems (Radar Pulse,
    /// outlines, blips) ignore them. Always returns true — the throw commits on use.
    /// </summary>
    public class Ability_SporeCloud : AbilityBase
    {
        public override bool Activate(PlayerController activator)
        {
            if (data == null) return false;

            float range = data.range > 0f ? data.range : 20f;
            float radius = data.radius > 0f ? data.radius : 6f;
            float duration = data.duration > 0f ? data.duration : 4f;

            Ray ray = AimRay;
            Vector3 impact = Physics.Raycast(ray, out RaycastHit hit, range, PlacementMask, QueryTriggerInteraction.Ignore)
                ? hit.point
                : ray.GetPoint(range);

            GameObject volume;
            if (data.effectPrefab != null)
            {
                volume = Instantiate(data.effectPrefab, impact, Quaternion.identity);
            }
            else
            {
                // Placeholder volume when no fog prefab is assigned — logic still runs.
                volume = new GameObject("SporeCloudVolume");
                volume.transform.position = impact;
            }

            SporeCloudVolume cloud = volume.GetComponent<SporeCloudVolume>();
            if (cloud == null) cloud = volume.AddComponent<SporeCloudVolume>();
            cloud.Initialize(radius, duration);

            if (!string.IsNullOrEmpty(data.sfxImpact))
                Audio.AudioManager.Instance?.PlaySfx(data.sfxImpact, impact);

            PlayActivationFeedback();
            return true;
        }
    }

    /// <summary>
    /// Runtime controller for a deployed Spore Cloud. Periodically scans for players inside its
    /// radius and keeps a <see cref="VisionObscured"/> marker on each one for as long as they remain
    /// covered, then clears its markers and despawns when the duration elapses.
    /// </summary>
    [DisallowMultipleComponent]
    public class SporeCloudVolume : MonoBehaviour
    {
        const float RescanInterval = 0.25f;

        float radius = 6f;
        float expireTime;
        float rescanAccumulator;

        readonly List<VisionObscured> covered = new List<VisionObscured>(8);
        readonly List<VisionObscured> scanBuffer = new List<VisionObscured>(8);
        static readonly Collider[] overlapBuffer = new Collider[32];

        /// <summary>Configure radius and lifetime. Call immediately after instantiation.</summary>
        public void Initialize(float cloudRadius, float duration)
        {
            radius = Mathf.Max(0.5f, cloudRadius);
            expireTime = Time.time + Mathf.Max(0.1f, duration);
            rescanAccumulator = RescanInterval; // scan on the first Update
        }

        void Update()
        {
            if (Time.time >= expireTime)
            {
                ClearAll();
                Destroy(gameObject);
                return;
            }

            rescanAccumulator += Time.deltaTime;
            if (rescanAccumulator < RescanInterval) return;
            rescanAccumulator = 0f;

            Rescan();
        }

        void Rescan()
        {
            // Diff the covered set instead of remove-all-then-readd. VisionObscured.RemoveSource()
            // self-destructs the component the instant its count hits 0, and Unity does NOT cancel a
            // scheduled Destroy if AddSource bumps the count back up the same frame — so a blanket
            // release/re-add would flicker IsObscured and reallocate the marker every tick for a
            // continuously-covered player. Instead we build this tick's set, then only RemoveSource
            // for players who left and only AddSource for players who newly entered.
            scanBuffer.Clear();

            int mask = LayerMask.GetMask(GameConstants.LayerPlayer, GameConstants.LayerHitbox);
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, overlapBuffer, mask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                PlayerController pc = overlapBuffer[i].GetComponentInParent<PlayerController>();
                if (pc == null || pc.health == null || pc.health.IsDead) continue;
                if (scanBuffer.Exists(v => v != null && v.gameObject == pc.gameObject)) continue;

                VisionObscured marker = pc.GetComponent<VisionObscured>();
                if (marker == null) marker = pc.gameObject.AddComponent<VisionObscured>();

                // Only bump the source count for players this volume was NOT already covering.
                if (!covered.Contains(marker))
                    marker.AddSource();

                scanBuffer.Add(marker);
            }

            // Release the count for players we covered last tick but no longer cover (or whose marker
            // was destroyed externally). Players still present are left untouched — zero churn.
            for (int i = 0; i < covered.Count; i++)
            {
                VisionObscured prev = covered[i];
                if (prev != null && !scanBuffer.Contains(prev))
                    prev.RemoveSource();
            }

            covered.Clear();
            covered.AddRange(scanBuffer);
        }

        void ClearAll()
        {
            for (int i = 0; i < covered.Count; i++)
                if (covered[i] != null) covered[i].RemoveSource();
            covered.Clear();
        }

        void OnDestroy()
        {
            ClearAll();
        }
    }
}
