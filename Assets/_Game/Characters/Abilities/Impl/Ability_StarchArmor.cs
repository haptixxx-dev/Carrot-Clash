using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Potato Active 1 — Starch Armor. Grants the owner a slab of temporary HP that absorbs damage
    /// (including fire) before base HP, for a short window. Survivability button: pop it before a
    /// shotgun burst or while holding an objective. Temp HP is applied through
    /// <see cref="EffectSystem.ApplyTemporaryHP"/> so ordering/stacking stays authoritative in one place.
    /// </summary>
    [DisallowMultipleComponent]
    public class Ability_StarchArmor : AbilityBase
    {
        public override bool Activate(PlayerController activator)
        {
            if (Owner == null || Owner.health == null || Owner.health.IsDead) return false;

            EffectSystem.ApplyTemporaryHP(Owner, GameConstants.StarchArmorAmount, GameConstants.StarchArmorDuration);

            // Designer-configured activation SFX/VFX (yellow outline VFX is optional placeholder art).
            PlayActivationFeedback();

            return true;
        }
    }
}
