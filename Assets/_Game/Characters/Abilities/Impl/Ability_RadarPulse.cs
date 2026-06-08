using System.Collections.Generic;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Carrot Active 2 — Radar Pulse. Sweeps a sphere on the Player layer and reveals every enemy
    /// inside it through walls for a fixed duration. Reveal is implemented by attaching a private
    /// <see cref="RevealController"/> to each revealed enemy, which applies a placeholder outline
    /// (emissive tint, rendered on top) and removes itself when the timer elapses.
    ///
    /// Design (docs/characters.md): 18s cooldown, 15m radius, 2.5s reveal. Values come from the
    /// bound <see cref="AbilityDataSO"/> at runtime (range/radius = scan radius, duration = reveal).
    /// </summary>
    public class Ability_RadarPulse : AbilityBase
    {
        // Reused buffer to avoid per-activation allocation; OverlapSphereNonAlloc fills it.
        static readonly Collider[] HitBuffer = new Collider[GameConstants.MaxPlayers * 2];

        public override bool Activate(PlayerController activator)
        {
            if (Owner == null) return false;

            float scanRadius = ResolveScanRadius();
            float revealDuration = Data != null && Data.duration > 0f ? Data.duration : 2.5f;
            int playerMask = LayerMask.GetMask(GameConstants.LayerPlayer);

            int count = Physics.OverlapSphereNonAlloc(
                Owner.transform.position, scanRadius, HitBuffer, playerMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Collider col = HitBuffer[i];
                if (col == null) continue;

                PlayerController other = col.GetComponentInParent<PlayerController>();
                if (other == null || other == Owner) continue;

                // Only reveal enemies of the caster's team (dead players are skipped — no point
                // outlining a corpse that is about to respawn).
                if (!Owner.Team.IsEnemyOf(other.Team)) continue;
                if (other.health != null && other.health.IsDead) continue;

                RevealController.Reveal(other.gameObject, revealDuration);
            }

            PlayActivationFeedback();
            return true;
        }

        /// <summary>Scan radius: prefer the SO radius, fall back to range, then to the doc default 15m.</summary>
        float ResolveScanRadius()
        {
            if (Data == null) return 15f;
            if (Data.radius > 0f) return Data.radius;
            if (Data.range > 0f) return Data.range;
            return 15f;
        }

        /// <summary>
        /// Lightweight per-enemy reveal marker. Applies a placeholder "outline" by tinting every
        /// renderer's material emission and forcing it to render on top, then restores the originals
        /// when the timer expires (or another pulse refreshes the duration).
        /// </summary>
        sealed class RevealController : MonoBehaviour
        {
            static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
            static readonly Color RevealTint = new Color(1f, 0.55f, 0.1f, 1f); // carrot orange

            float expireTime;
            readonly List<Renderer> tracked = new List<Renderer>();
            readonly List<Color> originalEmission = new List<Color>();
            readonly List<bool> hadEmission = new List<bool>();
            bool applied;

            /// <summary>Reveal a target (or refresh its existing reveal) for <paramref name="duration"/> seconds.</summary>
            public static void Reveal(GameObject target, float duration)
            {
                if (target == null) return;
                RevealController rc = target.GetComponent<RevealController>();
                if (rc == null) rc = target.AddComponent<RevealController>();
                rc.Begin(duration);
            }

            void Begin(float duration)
            {
                expireTime = Time.time + duration;
                if (applied) return;

                target_ApplyOutline();
                applied = true;
            }

            void target_ApplyOutline()
            {
                tracked.Clear();
                originalEmission.Clear();
                hadEmission.Clear();

                Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                {
                    if (r == null) continue;
                    Material mat = r.material; // instance — safe to mutate, restored on clear
                    tracked.Add(r);

                    bool wasOn = mat.IsKeywordEnabled("_EMISSION");
                    hadEmission.Add(wasOn);
                    originalEmission.Add(mat.HasProperty(EmissionColorId) ? mat.GetColor(EmissionColorId) : Color.black);

                    if (mat.HasProperty(EmissionColorId))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor(EmissionColorId, RevealTint);
                    }
                }
            }

            void Update()
            {
                if (Time.time >= expireTime)
                {
                    RestoreAndRemove();
                }
            }

            void RestoreAndRemove()
            {
                for (int i = 0; i < tracked.Count; i++)
                {
                    Renderer r = tracked[i];
                    if (r == null) continue;
                    Material mat = r.material;
                    if (mat.HasProperty(EmissionColorId))
                    {
                        mat.SetColor(EmissionColorId, originalEmission[i]);
                        if (!hadEmission[i]) mat.DisableKeyword("_EMISSION");
                    }
                }
                tracked.Clear();
                originalEmission.Clear();
                hadEmission.Clear();
                applied = false;
                Destroy(this);
            }

            void OnDestroy()
            {
                // Defensive restore if destroyed externally (e.g. player despawn) before timer.
                if (!applied) return;
                for (int i = 0; i < tracked.Count; i++)
                {
                    Renderer r = tracked[i];
                    if (r == null) continue;
                    Material mat = r.material;
                    if (mat.HasProperty(EmissionColorId))
                        mat.SetColor(EmissionColorId, originalEmission[i]);
                }
            }
        }
    }
}
