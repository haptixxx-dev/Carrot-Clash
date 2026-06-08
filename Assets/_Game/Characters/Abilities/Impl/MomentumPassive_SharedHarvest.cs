using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Broccoli Momentum Passive — Shared Harvest. When a nearby ally (same team, not Broccoli
    /// itself) scores a kill within <see cref="GameConstants.BroccoliHarvestRadius"/> of Broccoli,
    /// Broccoli gains a fraction (<see cref="GameConstants.BroccoliHarvestShare"/>) of kill charge via
    /// <c>Owner.momentum.AddCharge</c>. Subscribes to <see cref="GameEvents.OnKill"/> in OnBind and
    /// unsubscribes in Unbind / OnDestroy.
    /// </summary>
    public class MomentumPassive_SharedHarvest : AbilityBase
    {
        bool subscribed;

        protected override void OnBind()
        {
            if (subscribed) return;
            GameEvents.OnKill += HandleKill;
            subscribed = true;
        }

        public override void Unbind()
        {
            Unsubscribe();
            base.Unbind();
        }

        void OnDestroy()
        {
            Unsubscribe();
        }

        void Unsubscribe()
        {
            if (!subscribed) return;
            GameEvents.OnKill -= HandleKill;
            subscribed = false;
        }

        void HandleKill(KillEvent e)
        {
            if (Owner == null || Owner.momentum == null) return;
            if (Owner.health != null && Owner.health.IsDead) return;

            PlayerController killer = e.Killer;
            if (killer == null || killer == Owner) return;        // ally kills only, not Broccoli's own
            if (killer.Team != Owner.Team) return;                // same team

            float distance = GameExtensions.FlatDistance(killer.transform.position, Owner.transform.position);
            if (distance > GameConstants.BroccoliHarvestRadius) return;

            Owner.momentum.AddCharge(GameConstants.BroccoliHarvestShare);
        }
    }
}
