using System.Collections;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Potato Active 2 — Earthen Slam. Potato hops and slams the ground, knocking nearby enemies
    /// back and staggering them. Design (characters.md): ~4m radius AoE, 6m knockback, 0.5s stagger,
    /// requires being airborne briefly. For MVP the knockback resolves immediately on activation
    /// (server-authoritative effect), while a short coroutine plays the upward-hop visual via the
    /// movement component so the slam reads correctly. Radial camera shake sells the impact for
    /// nearby local players.
    /// </summary>
    [DisallowMultipleComponent]
    public class Ability_EarthenSlam : AbilityBase
    {
        const float HopJumpFraction = 1f; // reuse the normal jump impulse for the hop visual
        const float ShakeMagnitude = 0.4f;
        const float ShakeDuration = 0.3f;

        Coroutine hopRoutine;

        public override bool Activate(PlayerController activator)
        {
            if (Owner == null || Owner.health == null || Owner.health.IsDead) return false;

            // Resolve the AoE knockback immediately (MVP / authoritative), then play the hop visual.
            DoSlam();

            if (hopRoutine != null) StopCoroutine(hopRoutine);
            hopRoutine = StartCoroutine(HopVisual());

            return true;
        }

        /// <summary>OverlapSphere at the owner's position; knock back + stagger every enemy in radius.</summary>
        void DoSlam()
        {
            Vector3 center = Owner.transform.position;
            float radius = (data != null && data.radius > 0f) ? data.radius : 4f;

            int mask = LayerMask.GetMask(GameConstants.LayerPlayer, GameConstants.LayerHitbox);
            Collider[] hits = Physics.OverlapSphere(center, radius, mask, QueryTriggerInteraction.Collide);

            // De-dup: multiple hitboxes (head/body) can resolve to the same controller.
            PlayerController lastApplied = null;
            foreach (Collider col in hits)
            {
                PlayerController pc = col.GetComponentInParent<PlayerController>();
                if (pc == null || pc == Owner) continue;
                if (!Owner.Team.IsEnemyOf(pc.Team)) continue;
                if (pc.health == null || pc.health.IsDead) continue;
                if (pc == lastApplied) continue;
                lastApplied = pc;

                EffectSystem.ApplyKnockback(pc, center, GameConstants.KnockbackDistance, GameConstants.StaggerDuration);
            }

            // Impact feedback: SFX/VFX from the data, plus a radial shake for the local Potato.
            PlayActivationFeedback();
            if (data != null && !string.IsNullOrEmpty(data.sfxImpact))
                Audio.AudioManager.Instance?.PlaySfx(data.sfxImpact, center);

            if (Owner.IsLocal && Owner.cam != null)
                Owner.cam.Shake(ShakeMagnitude, ShakeDuration, ShakeType.Radial);
        }

        /// <summary>Short upward hop for readability — purely visual; the effect already resolved.</summary>
        IEnumerator HopVisual()
        {
            if (Owner != null && Owner.movement != null && Owner.movement.IsGrounded)
                Owner.movement.Jump();

            yield return null;
            hopRoutine = null;
        }

        public override void Cancel()
        {
            if (hopRoutine != null)
            {
                StopCoroutine(hopRoutine);
                hopRoutine = null;
            }
        }
    }
}
