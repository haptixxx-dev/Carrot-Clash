using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Carrot Passive — Silent Steps. Footstep SFX are muted while Carrot moves below half sprint
    /// speed, letting the scout sneak. The actual muting decision is read by the footstep audio
    /// system via <see cref="PlayerController.ClassId"/>; this component is the canonical home for
    /// the rule and exposes <see cref="ShouldSilence"/> so any caller computes it identically.
    ///
    /// Design (docs/characters.md): silent when <c>speed &lt; sprintSpeed * 0.5</c>.
    /// </summary>
    public class Passive_SilentSteps : AbilityBase
    {
        /// <summary>
        /// True when footsteps should be silenced: the player is moving slowly enough to sneak,
        /// i.e. below <see cref="GameConstants.SilentStepsSpeedFraction"/> of their sprint speed.
        /// </summary>
        /// <param name="speed">Current horizontal movement speed (m/s).</param>
        /// <param name="sprintSpeed">The player's sprint speed (m/s).</param>
        public bool ShouldSilence(float speed, float sprintSpeed)
        {
            if (sprintSpeed <= 0f) return false;
            return speed < sprintSpeed * GameConstants.SilentStepsSpeedFraction;
        }

        /// <summary>Convenience overload that reads the owner's live movement state.</summary>
        public bool ShouldSilence()
        {
            if (Owner == null || Owner.movement == null) return false;
            return ShouldSilence(Owner.movement.HorizontalSpeed, Owner.movement.SprintSpeed);
        }
    }
}
