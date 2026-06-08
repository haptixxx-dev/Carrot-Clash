using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Potato Momentum Passive — Stubborn Root. Marker behaviour. The 25% (instead of the default 50%)
    /// momentum-transfer-on-death is owned authoritatively by <see cref="MomentumController.HandleDeath"/>,
    /// which checks <see cref="ClassId.Potato"/> and uses <c>GameConstants.MomentumTransferPotato</c>.
    /// This component exists purely so the <see cref="AbilityFactory"/> can attach a real behaviour for
    /// the slot and so future tuning has a home; it has no per-frame logic and never fires as an active.
    /// </summary>
    [DisallowMultipleComponent]
    public class MomentumPassive_StubbornRoot : AbilityBase
    {
        // Momentum passive: never activates as an action.
        public override bool Activate(PlayerController activator) => false;
    }
}
