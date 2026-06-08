using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Potato Passive — Thick Skin. Marker behaviour. The actual -8% incoming-damage-while-stationary
    /// rule is owned authoritatively by <see cref="PlayerMovement.IncomingDamageMultiplier"/> (gated on
    /// <see cref="ClassId.Potato"/> and time-since-last-move), which <see cref="HealthController"/>
    /// reads in TakeDamage. This component exists so the <see cref="AbilityFactory"/> can attach a
    /// real behaviour for the slot and so future tuning has a home; it has no per-frame logic and
    /// never fires as an active.
    /// </summary>
    [DisallowMultipleComponent]
    public class Passive_ThickSkin : AbilityBase
    {
        // Passive: never activates as an action.
        public override bool Activate(PlayerController activator) => false;
    }
}
