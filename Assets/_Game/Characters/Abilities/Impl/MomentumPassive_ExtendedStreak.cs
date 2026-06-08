using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Jalapeño Momentum Passive — Extended Streak. Each kill adds +5s to the momentum decay timer,
    /// letting Jalapeño hold a hot streak longer. The decay extension itself is applied authoritatively
    /// inside <see cref="MomentumController.RegisterKill"/> (keyed on <see cref="ClassId.Jalapeno"/>),
    /// so this component is a marker: it exists for the <see cref="AbilityFactory"/> mapping, HUD
    /// tooltips, and future per-instance tuning. It has no active effect of its own.
    /// </summary>
    public class MomentumPassive_ExtendedStreak : AbilityBase
    {
        /// <summary>The decay extension granted per kill (mirrors the value applied in MomentumController).</summary>
        public float DecayExtensionSeconds => GameConstants.JalapenoDecayExtension;

        // No Activate / event subscription: momentum-passives are read by MomentumController directly.
    }
}
