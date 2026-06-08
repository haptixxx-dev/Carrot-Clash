using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Broccoli Active 1 — Leaf Shield. Raycasts along the aim ray (capped at the ability range)
    /// and, if it strikes a placement surface, deploys a destructible cover object there. The
    /// spawned prefab carries its own <see cref="DestructibleCover"/> (80 HP) that blocks bullets
    /// and shatters when destroyed. Returns true only when a valid surface was hit so the controller
    /// starts the cooldown; an aim into open sky leaves the ability ready.
    /// </summary>
    public class Ability_LeafShield : AbilityBase
    {
        const int LeafShieldHP = 80;

        public override bool Activate(PlayerController activator)
        {
            if (data == null || data.effectPrefab == null) return false;

            float range = data.range > 0f ? data.range : 3f;
            Ray ray = AimRay;

            if (!Physics.Raycast(ray, out RaycastHit hit, range, PlacementMask, QueryTriggerInteraction.Ignore))
                return false;

            // Orient the cover to stand upright on the hit surface, facing back toward the placer.
            Vector3 placePos = hit.point;
            Vector3 forward = (-ray.direction).Flat();
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            Quaternion rot = Quaternion.LookRotation(forward.normalized, Vector3.up);

            GameObject shield = Instantiate(data.effectPrefab, placePos, rot);

            DestructibleCover cover = shield.GetComponent<DestructibleCover>();
            if (cover == null) cover = shield.AddComponent<DestructibleCover>();
            cover.Initialize(LeafShieldHP);

            // Auto-clean if the cover survives its full lifetime (data.duration; 0 = persists until destroyed).
            if (data.duration > 0f) Destroy(shield, data.duration);

            PlayActivationFeedback();
            return true;
        }
    }
}
