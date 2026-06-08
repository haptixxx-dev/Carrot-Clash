using System.Collections;
using UnityEngine;
using CarrotClash.Audio;

namespace CarrotClash.UI
{
    /// <summary>
    /// Central "juice" router for the local player (Game Feel doc). Binds the local
    /// <see cref="PlayerController"/> when it spawns, then translates combat events into
    /// hit-stop, camera shake, and momentum SFX. Kept separate from the individual HUD widgets
    /// so the feel logic lives in one place and can be reasoned about / tuned holistically.
    ///
    /// Hit-stop: a single-frame <see cref="Time.timeScale"/> dip on hit confirmation. Guarded so
    /// that rapid-fire hits do not stack the dip (each new request just refreshes the timer rather
    /// than nesting coroutines and crawling the game to a halt).
    /// </summary>
    [DisallowMultipleComponent]
    public class FeedbackController : MonoBehaviour
    {
        [Header("Hit-stop")]
        [Tooltip("Time scale held during a hit-stop dip (1 = none).")]
        [SerializeField] float hitStopScale = 0.02f;
        [Tooltip("Unscaled duration of a body-hit dip (~1 frame at 60fps).")]
        [SerializeField] float hitStopBodyDuration = 0.02f;
        [Tooltip("Unscaled duration of a kill dip (~2 frames at 60fps) — more weight than a body hit.")]
        [SerializeField] float hitStopKillDuration = 0.035f;

        [Header("Camera shake — big events (Game Feel screen-shake table)")]
        [SerializeField] float bigHitShakeMagnitude = 0.15f;
        [SerializeField] float bigHitShakeDuration = 0.1f;
        [SerializeField] int bigHitDamageThreshold = 40;
        [SerializeField] float tier3ShakeMagnitude = 0.10f;
        [SerializeField] float tier3ShakeDuration = 0.2f;
        [SerializeField] float tier1ShakeMagnitude = 0.05f;
        [SerializeField] float tier2ShakeMagnitude = 0.07f;
        [SerializeField] float tierShakeDuration = 0.12f;

        [Header("On-Fire overlay")]
        [Tooltip("Optional 'YOU'RE ON FIRE' overlay shown the first time the local player hits tier 3 this session.")]
        [SerializeField] OnFireOverlay onFireOverlay;

        PlayerController localPlayer;
        Coroutine hitStopRoutine;
        float hitStopReleaseTime;          // unscaled time at which the current dip should end
        bool firstOnFireShown;             // session-wide: only the first tier-3 triggers the overlay

        void OnEnable()
        {
            GameEvents.OnPlayerSpawned += HandlePlayerSpawned;
            GameEvents.OnTierChanged += HandleTierChanged;

            // Late-bind if the local player already exists (e.g. UI loaded after gameplay).
            if (localPlayer == null && GameModeManager.Instance != null)
                TryBindFromExistingPlayers();
        }

        void OnDisable()
        {
            GameEvents.OnPlayerSpawned -= HandlePlayerSpawned;
            GameEvents.OnTierChanged -= HandleTierChanged;
            UnbindLocal();

            // Safety: never leave the game frozen if this component is torn down mid-dip.
            if (hitStopRoutine != null)
            {
                StopCoroutine(hitStopRoutine);
                hitStopRoutine = null;
            }
            Time.timeScale = 1f;
        }

        void TryBindFromExistingPlayers()
        {
            var players = GameModeManager.Instance.Players;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null && players[i].IsLocal)
                {
                    Bind(players[i]);
                    return;
                }
            }
        }

        void HandlePlayerSpawned(PlayerController pc)
        {
            if (pc == null || !pc.IsLocal) return;
            if (pc == localPlayer) return;     // respawn of the already-bound player
            Bind(pc);
        }

        void Bind(PlayerController pc)
        {
            UnbindLocal();
            localPlayer = pc;
            if (localPlayer.weapon != null)
                localPlayer.weapon.OnHitConfirmed += HandleHitConfirmed;
        }

        void UnbindLocal()
        {
            if (localPlayer != null && localPlayer.weapon != null)
                localPlayer.weapon.OnHitConfirmed -= HandleHitConfirmed;
            localPlayer = null;
        }

        // ----- Hit confirmation: hit-stop + big-event shake -----
        void HandleHitConfirmed(DamageResult result, Vector3 hitPoint)
        {
            float duration = result.WasLethal ? hitStopKillDuration : hitStopBodyDuration;
            RequestHitStop(duration);

            // Big hit (> threshold damage) shakes from the hit direction toward the target.
            if (result.DamageApplied > bigHitDamageThreshold && localPlayer != null && localPlayer.cam != null)
            {
                Vector3 dir = (hitPoint - localPlayer.transform.position);
                localPlayer.cam.Shake(bigHitShakeMagnitude, bigHitShakeDuration, ShakeType.Directional, dir);
            }
        }

        /// <summary>
        /// Request a hit-stop. Refreshes the release time so overlapping hits extend (not stack) the dip,
        /// keeping at most one coroutine alive. Skipped entirely under reduce-motion.
        /// </summary>
        void RequestHitStop(float duration)
        {
            if (SettingsService.ReduceMotion) return;

            float candidateRelease = Time.unscaledTime + duration;
            if (candidateRelease > hitStopReleaseTime) hitStopReleaseTime = candidateRelease;

            if (hitStopRoutine == null)
                hitStopRoutine = StartCoroutine(HitStopRoutine());
        }

        IEnumerator HitStopRoutine()
        {
            Time.timeScale = hitStopScale;
            while (Time.unscaledTime < hitStopReleaseTime)
                yield return null;
            Time.timeScale = 1f;
            hitStopRoutine = null;
        }

        // ----- Momentum tier changes (local player only): shake, SFX, on-fire overlay -----
        void HandleTierChanged(PlayerController pc, MomentumTier oldTier, MomentumTier newTier)
        {
            if (pc == null || !pc.IsLocal) return;

            bool gained = (int)newTier > (int)oldTier;

            if (gained)
            {
                // Tier-up shake + ascending momentum SFX per tier (Momentum Feel table).
                switch (newTier)
                {
                    case MomentumTier.Warm:
                        ShakeLocal(tier1ShakeMagnitude, tierShakeDuration, ShakeType.Random);
                        PlayUi("momentum_tier1");
                        break;
                    case MomentumTier.Hot:
                        ShakeLocal(tier2ShakeMagnitude, tierShakeDuration, ShakeType.Random);
                        PlayUi("momentum_tier2");
                        break;
                    case MomentumTier.OnFire:
                        ShakeLocal(tier3ShakeMagnitude, tier3ShakeDuration, ShakeType.Random);
                        PlayUi("momentum_tier3");
                        // Power-absorbed sting also plays when a kill bumps you straight to tier 3.
                        PlayUi("momentum_absorb");
                        if (!firstOnFireShown)
                        {
                            firstOnFireShown = true;
                            if (onFireOverlay != null) onFireOverlay.Show();
                        }
                        break;
                }
            }
            else if (newTier == MomentumTier.Cold)
            {
                // Dropped to tier 0: death-reset or full decay. Death SFX covers death; decay plays the lost cue.
                PlayUi("momentum_lost");
            }
        }

        void ShakeLocal(float magnitude, float duration, ShakeType type)
        {
            if (localPlayer != null && localPlayer.cam != null)
                localPlayer.cam.Shake(magnitude, duration, type);
        }

        static void PlayUi(string key)
        {
            AudioManager.Instance?.PlayUi(key);
        }
    }
}
