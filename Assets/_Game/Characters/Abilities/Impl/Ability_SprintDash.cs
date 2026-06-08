using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Carrot Active 1 — Sprint Dash. An instant directional displacement in the player's current
    /// movement/look direction, usable mid-air, that preserves momentum. The dash itself is a single
    /// <see cref="PlayerMovement.Dash"/> call (collision-respecting <c>CharacterController.Move</c>);
    /// the camera punches its FOV outward for the juke "whoosh" feel.
    ///
    /// Design (docs/characters.md): 6s cooldown, 8m range. The cooldown/range numbers are driven at
    /// runtime by the bound <see cref="AbilityDataSO"/> so designers can tune without code changes.
    /// </summary>
    public class Ability_SprintDash : AbilityBase
    {
        public override bool Activate(PlayerController activator)
        {
            if (Owner == null || Owner.movement == null) return false;

            Vector3 dir = ResolveDashDirection();

            // Distance comes from the SO (range). PlayerMovement.Dash flattens + normalises the
            // direction and falls back to forward if it is degenerate, so a near-zero dir is safe.
            Owner.movement.Dash(dir, Data != null ? Data.range : 0f);

            // FOV surge for the dash whoosh (camera is null on remote/bot players — null-check).
            Owner.cam?.FovSurge(GameConstants.DashFovSurge);

            PlayActivationFeedback();
            return true;
        }

        /// <summary>
        /// Dash heading: the player's current planar move heading if they are actively moving,
        /// otherwise the camera look direction (so a standing dash carries you where you are aiming).
        /// </summary>
        Vector3 ResolveDashDirection()
        {
            // HorizontalSpeed > 0 means the player is committing to a movement direction; use the
            // velocity heading via the owner's facing combined with aim so strafing dashes feel right.
            Vector3 aimFlat = AimRay.direction.Flat();
            Vector3 forwardFlat = Owner.transform.forward.Flat();

            // Prefer the look/aim heading when it is meaningful; fall back to body forward.
            Vector3 dir = aimFlat.sqrMagnitude > 0.01f ? aimFlat : forwardFlat;
            return dir;
        }
    }
}
