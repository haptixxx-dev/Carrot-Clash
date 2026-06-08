using System.Collections.Generic;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Broccoli Passive — Regen Aura. Every <see cref="GameConstants.RegenAuraTick"/> seconds, heals
    /// every living ally (same team, excluding Broccoli itself) within
    /// <see cref="GameConstants.RegenAuraRadius"/> for <see cref="GameConstants.RegenAuraHeal"/> HP
    /// via <c>ally.health.Heal</c>. Healing is gated inside HealthController (no effect on full HP or
    /// while Starch Armor temp HP is active), so this just applies the tick. Passive: never activates.
    /// </summary>
    public class Passive_RegenAura : AbilityBase
    {
        static readonly Collider[] overlapBuffer = new Collider[32];
        static readonly HashSet<PlayerController> healedThisTick = new HashSet<PlayerController>();

        float tickAccumulator;

        void Update()
        {
            if (Owner == null) return;
            // Don't heal from a dead Broccoli's corpse.
            if (Owner.health != null && Owner.health.IsDead) return;

            tickAccumulator += Time.deltaTime;
            if (tickAccumulator < GameConstants.RegenAuraTick) return;
            tickAccumulator -= GameConstants.RegenAuraTick;

            HealAlliesInRange();
        }

        void HealAlliesInRange()
        {
            int mask = LayerMask.GetMask(GameConstants.LayerPlayer, GameConstants.LayerHitbox);
            int count = Physics.OverlapSphereNonAlloc(Owner.transform.position, GameConstants.RegenAuraRadius,
                overlapBuffer, mask, QueryTriggerInteraction.Collide);

            // A player has multiple colliders (body + hitboxes on different layers); de-dupe so each
            // ally heals exactly once per tick.
            healedThisTick.Clear();
            for (int i = 0; i < count; i++)
            {
                PlayerController pc = overlapBuffer[i].GetComponentInParent<PlayerController>();
                if (pc == null || pc == Owner) continue;          // not self
                if (pc.Team != Owner.Team) continue;              // allies only
                if (pc.health == null || pc.health.IsDead) continue; // not dead
                if (!healedThisTick.Add(pc)) continue;            // once per ally per tick

                pc.health.Heal(GameConstants.RegenAuraHeal);
            }
        }
    }
}
