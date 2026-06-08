using System.Collections.Generic;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Jalapeño Passive — Burn Streak. When the owner gets a kill, the victim's body ignites: every
    /// enemy within 5m of the death position takes 10 DPS for 2s (Characters doc). Implemented as a
    /// burst of <see cref="EffectSystem.ApplyFire"/> on each nearby enemy found via OverlapSphere —
    /// no persistent zone needed, since the aura is a one-shot ignition at the kill location.
    /// </summary>
    public class Passive_BurnStreak : AbilityBase
    {
        const float AuraRadius = 5f;
        const float AuraDps = 10f;
        const float AuraDuration = 2f;

        // Player + hitbox colliders for the nearby-enemy scan.
        static readonly int OverlapMask =
            LayerMask.GetMask(GameConstants.LayerPlayer, GameConstants.LayerHitbox);

        readonly Collider[] overlapBuffer = new Collider[32];
        readonly HashSet<PlayerController> ignitedThisKill = new HashSet<PlayerController>();
        bool subscribed;

        protected override void OnBind()
        {
            if (subscribed) return;
            GameEvents.OnKill += HandleKill;
            subscribed = true;
        }

        public override void Unbind()
        {
            if (!subscribed) return;
            GameEvents.OnKill -= HandleKill;
            subscribed = false;
        }

        void OnDestroy() => Unbind();

        void HandleKill(KillEvent e)
        {
            if (Owner == null || e.Killer != Owner || e.Victim == null) return;

            Vector3 center = e.Victim.transform.position;

            if (data != null && data.effectPrefab != null)
                SpawnTimed(data.effectPrefab, center, Quaternion.identity, AuraDuration);
            if (data != null && !string.IsNullOrEmpty(data.sfxImpact))
                Audio.AudioManager.Instance?.PlaySfx(data.sfxImpact, center);

            ignitedThisKill.Clear();
            int count = Physics.OverlapSphereNonAlloc(center, AuraRadius, overlapBuffer, OverlapMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider col = overlapBuffer[i];
                if (col == null) continue;

                PlayerController enemy = col.GetComponentInParent<PlayerController>();
                if (enemy == null || enemy == Owner) continue;
                if (enemy.health == null || enemy.health.IsDead) continue;
                if (!Owner.Team.IsEnemyOf(enemy.Team)) continue;
                if (!ignitedThisKill.Add(enemy)) continue;

                EffectSystem.ApplyFire(enemy, AuraDps, AuraDuration, Owner);
            }
        }
    }
}
