using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Carrot Momentum Passive — Backstab Momentum. A kill landed from the victim's 180° rear arc
    /// grants +2 momentum tiers instead of the usual +1.
    ///
    /// The arc test and tier bonus are already wired into the spine: <see cref="PlayerController"/>'s
    /// death handler calls <see cref="GameExtensions.IsInRearArc"/> with
    /// <see cref="GameConstants.CarrotBackstabDot"/> and forwards the result into
    /// <see cref="MomentumController.RegisterKill"/> (isBackstab). This component therefore carries no
    /// per-frame logic — it exists for the <see cref="AbilityFactory"/> mapping, for HUD/tutorial
    /// hooks that want to surface the passive, and as the canonical home for the tuning value.
    /// </summary>
    public class MomentumPassive_Backstab : AbilityBase
    {
        /// <summary>
        /// Dot-product threshold between the victim's forward and the (victim → killer) direction.
        /// A dot below this counts as a rear-arc (backstab) kill. Mirrors the value the spine uses
        /// in <see cref="PlayerController"/>; surfaced here for tooling and documentation.
        /// </summary>
        public float RearArcDotThreshold => GameConstants.CarrotBackstabDot;

        /// <summary>Tier bonus granted by a successful backstab kill (the spine grants +2).</summary>
        public int BackstabTierBonus => 2;

        /// <summary>
        /// Pure helper mirroring the spine's backstab test, so UI/tutorial/bot code can predict a
        /// backstab without duplicating the dot-product convention.
        /// </summary>
        /// <param name="victimForward">The victim's facing direction.</param>
        /// <param name="victimToKiller">Vector from victim position to killer position.</param>
        public bool IsBackstab(Vector3 victimForward, Vector3 victimToKiller)
        {
            return GameExtensions.IsInRearArc(victimForward, victimToKiller, RearArcDotThreshold);
        }
    }
}
