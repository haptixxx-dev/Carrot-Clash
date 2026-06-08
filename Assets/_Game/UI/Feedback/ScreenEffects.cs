using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using CarrotClash.Audio;

namespace CarrotClash.UI
{
    /// <summary>
    /// Full-screen post-feedback for the local player (Game Feel "Damage / Death" + UI/UX HP-bar spec):
    /// <list type="bullet">
    /// <item>Low-HP red vignette that pulses with a heartbeat below <see cref="GameConstants.LowHealthThreshold"/> HP.</item>
    /// <item>Death audio muffle hook (optional AudioMixer low-pass) for ~0.5s on death.</item>
    /// <item>Respawn fade-to-black on death and fade-in on (re)spawn.</item>
    /// </list>
    /// Uses two full-screen uGUI <see cref="Image"/> overlays (vignette + fade) so it works on the HUD
    /// canvas without any post-process volume dependency. Polls the bound <see cref="HealthController"/>
    /// for HP (HealthFraction) rather than reacting per-hit, so the vignette tracks healing too.
    /// Honours <see cref="SettingsService.ReduceMotion"/> (no pulsing — a steady tint instead).
    /// </summary>
    [DisallowMultipleComponent]
    public class ScreenEffects : MonoBehaviour
    {
        [Header("Overlays (full-screen uGUI Images)")]
        [Tooltip("Red edge vignette overlay. Should stretch to fill the canvas; tinted/alpha-driven here.")]
        [SerializeField] Image vignette;
        [Tooltip("Solid black overlay used for respawn fade-to-black / fade-in.")]
        [SerializeField] Image fade;

        [Header("Low-HP vignette")]
        [SerializeField] Color vignetteColor = new Color(0.7f, 0f, 0f, 1f);
        [Tooltip("HP fraction at or below which the low-HP vignette + heartbeat begin. Defaults to 30HP at full health.")]
        [SerializeField] float lowHealthFraction = 0.33f;
        [SerializeField] float heartbeatRate = 1.2f;        // pulses per second
        [SerializeField] float vignetteMaxAlpha = 0.45f;
        [SerializeField] float vignetteMinAlpha = 0.12f;
        [SerializeField] string heartbeatSfxKey = "";        // optional; left blank by default

        [Header("Respawn fade")]
        [SerializeField] float fadeToBlackTime = 0.2f;
        [SerializeField] float fadeInTime = 0.3f;

        [Header("Death muffle (optional)")]
        [Tooltip("Optional AudioMixer with an exposed low-pass cutoff parameter to muffle gameplay on death.")]
        [SerializeField] AudioMixer muffleMixer;
        [Tooltip("Exposed mixer parameter name controlling the low-pass cutoff frequency (Hz).")]
        [SerializeField] string lowPassParam = "GameplayLowpass";
        [SerializeField] float muffleCutoffHz = 700f;
        [SerializeField] float normalCutoffHz = 22000f;
        [SerializeField] float muffleDuration = 0.5f;

        PlayerController localPlayer;
        HealthController health;

        float heartbeatPhase;
        bool heartbeatBeatPending = true;
        Coroutine fadeRoutine;
        Coroutine muffleRoutine;

        void Awake()
        {
            // Start clean: both overlays transparent.
            SetImageAlpha(vignette, 0f, vignetteColor);
            if (fade != null) { fade.color = WithAlpha(Color.black, 0f); fade.raycastTarget = false; }
            if (vignette != null) vignette.raycastTarget = false;
            RestoreAudio();
        }

        void OnEnable()
        {
            GameEvents.OnPlayerSpawned += HandlePlayerSpawned;
            if (localPlayer == null && GameModeManager.Instance != null)
                TryBindFromExistingPlayers();
        }

        void OnDisable()
        {
            GameEvents.OnPlayerSpawned -= HandlePlayerSpawned;
            Unbind();
            RestoreAudio();
            if (fadeRoutine != null) { StopCoroutine(fadeRoutine); fadeRoutine = null; }
            if (muffleRoutine != null) { StopCoroutine(muffleRoutine); muffleRoutine = null; }
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

            bool isRespawn = (pc == localPlayer);
            if (!isRespawn) Bind(pc);

            // (Re)spawn: clear any death vignette and fade in from black.
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeInRoutine());
            RestoreAudio();
        }

        void Bind(PlayerController pc)
        {
            Unbind();
            localPlayer = pc;
            health = pc.health;
            if (health != null) health.OnDeath += HandleDeath;
        }

        void Unbind()
        {
            if (health != null) health.OnDeath -= HandleDeath;
            health = null;
            localPlayer = null;
        }

        void HandleDeath(PlayerController killer)
        {
            // Muffle gameplay audio briefly, then fade to black for the respawn handoff.
            if (muffleRoutine != null) StopCoroutine(muffleRoutine);
            muffleRoutine = StartCoroutine(MuffleRoutine());

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeToBlackRoutine());

            // Drop the low-HP vignette so it doesn't linger over the black screen.
            SetImageAlpha(vignette, 0f, vignetteColor);
        }

        void Update()
        {
            UpdateLowHealthVignette();
        }

        void UpdateLowHealthVignette()
        {
            if (health == null || vignette == null) return;
            // While a fade is in progress (death/respawn), don't fight it with the vignette.
            if (fadeRoutine != null) return;
            if (health.IsDead) return;

            float frac = health.HealthFraction;
            if (frac > lowHealthFraction || frac <= 0f)
            {
                SetImageAlpha(vignette, 0f, vignetteColor);
                heartbeatBeatPending = true;
                return;
            }

            // Intensity scales as HP drops toward zero within the low band.
            float intensity = Mathf.InverseLerp(lowHealthFraction, 0f, frac); // 0..1
            float baseAlpha = Mathf.Lerp(vignetteMinAlpha, vignetteMaxAlpha, intensity);

            float alpha;
            if (SettingsService.ReduceMotion)
            {
                alpha = baseAlpha;
            }
            else
            {
                heartbeatPhase += Time.deltaTime * heartbeatRate;
                if (heartbeatPhase >= 1f) heartbeatPhase -= 1f;
                // Sharp double-thump shape: a strong pulse curve.
                float pulse = Mathf.Pow(Mathf.Sin(heartbeatPhase * Mathf.PI), 4f);
                alpha = Mathf.Lerp(vignetteMinAlpha * 0.5f, vignetteMaxAlpha, intensity * pulse);

                // Optional heartbeat SFX at the top of each beat.
                if (!string.IsNullOrEmpty(heartbeatSfxKey))
                {
                    if (pulse > 0.85f && heartbeatBeatPending)
                    {
                        AudioManager.Instance?.PlayUi(heartbeatSfxKey);
                        heartbeatBeatPending = false;
                    }
                    else if (pulse < 0.2f)
                    {
                        heartbeatBeatPending = true;
                    }
                }
            }

            SetImageAlpha(vignette, alpha, vignetteColor);
        }

        // ----- Respawn fades -----
        IEnumerator FadeToBlackRoutine()
        {
            if (fade == null) { fadeRoutine = null; yield break; }
            float t = 0f;
            float startA = fade.color.a;
            while (t < fadeToBlackTime)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(startA, 1f, fadeToBlackTime > 0f ? t / fadeToBlackTime : 1f);
                fade.color = WithAlpha(Color.black, a);
                yield return null;
            }
            fade.color = WithAlpha(Color.black, 1f);
            fadeRoutine = null;
        }

        IEnumerator FadeInRoutine()
        {
            SetImageAlpha(vignette, 0f, vignetteColor);
            if (fade == null) { fadeRoutine = null; yield break; }
            float t = 0f;
            float startA = fade.color.a;
            while (t < fadeInTime)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(startA, 0f, fadeInTime > 0f ? t / fadeInTime : 1f);
                fade.color = WithAlpha(Color.black, a);
                yield return null;
            }
            fade.color = WithAlpha(Color.black, 0f);
            fadeRoutine = null;
        }

        // ----- Death audio muffle -----
        IEnumerator MuffleRoutine()
        {
            ApplyMuffle(muffleCutoffHz);
            yield return new WaitForSecondsRealtime(muffleDuration);
            RestoreAudio();
            muffleRoutine = null;
        }

        void ApplyMuffle(float cutoff)
        {
            if (muffleMixer != null && !string.IsNullOrEmpty(lowPassParam))
                muffleMixer.SetFloat(lowPassParam, cutoff);
        }

        void RestoreAudio()
        {
            if (muffleMixer != null && !string.IsNullOrEmpty(lowPassParam))
                muffleMixer.SetFloat(lowPassParam, normalCutoffHz);
        }

        // ----- Helpers -----
        static void SetImageAlpha(Image img, float a, Color baseColor)
        {
            if (img == null) return;
            img.color = WithAlpha(baseColor, a);
        }

        static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
