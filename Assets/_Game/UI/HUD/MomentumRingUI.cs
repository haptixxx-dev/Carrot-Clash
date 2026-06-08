using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CarrotClash.UI
{
    /// <summary>
    /// Thin ring around the crosshair that visualises momentum tier and decay (UI/UX + Game Feel).
    /// Fill clockwise from 12 o'clock reflects the decay countdown of the current tier. Ring colour
    /// follows the momentum config tier colour (grey → class colour → bright warm → animated fire).
    /// Tier-up flashes white then tweens to the new colour; the ring flickers when &lt; 3s before
    /// decay; on death it plays a shatter VFX and dims. Listens to <see cref="MomentumController"/>
    /// and to <see cref="HealthController.OnDeath"/> for the shatter.
    /// </summary>
    [DisallowMultipleComponent]
    public class MomentumRingUI : MonoBehaviour
    {
        [Header("Ring")]
        [SerializeField] Image ringFill;          // Image type = Filled, Radial360, clockwise, origin Top
        [SerializeField] CanvasGroup ringGroup;   // for dimming / flicker

        [Header("Tier-up flash")]
        [SerializeField] Image flashOverlay;      // white overlay faded in then out
        [SerializeField] float tierUpTweenTime = 0.3f;

        [Header("Death shatter VFX (placeholder)")]
        [SerializeField] GameObject shatterVfxPrefab;   // optional particle prefab spawned in UI space

        [Header("Decay flicker")]
        [SerializeField] float decayFlickerThreshold = 3f;   // seconds before decay
        [SerializeField] float decayFlickerHz = 2f;

        [Header("Fallback tier colours (used if config has none)")]
        [SerializeField] Color coldColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        [SerializeField] Color fireColor = new Color(1f, 0.45f, 0.1f);

        MomentumController momentum;
        HealthController health;

        int currentTier;
        float decayFraction = 1f;          // 0..1 of decay interval remaining (1 = freshly reset)
        float decayIntervalSeconds = GameConstants.MomentumDecayInterval;
        Color targetColor;
        Coroutine tierUpRoutine;
        bool shattered;

        /// <summary>Wire to the local player's momentum + health (health drives the death shatter).</summary>
        public void Bind(MomentumController momentumSource, HealthController healthSource)
        {
            Unbind();
            momentum = momentumSource;
            health = healthSource;

            if (momentum != null)
            {
                momentum.OnTierChanged += HandleTierChanged;
                momentum.OnDecayProgress += HandleDecayProgress;
                if (momentum.Config != null)
                    decayIntervalSeconds = Mathf.Max(0.01f, momentum.Config.decayIntervalSeconds);
                currentTier = momentum.Tier;
            }
            if (health != null)
                health.OnDeath += HandleDeath;

            shattered = false;
            if (ringGroup != null) ringGroup.alpha = 1f;
            if (flashOverlay != null) SetFlashAlpha(0f);
            targetColor = ResolveTierColor(currentTier);
            ApplyColor(targetColor);
            if (ringFill != null) ringFill.fillAmount = currentTier > 0 ? decayFraction : 0f;
        }

        public void Unbind()
        {
            if (momentum != null)
            {
                momentum.OnTierChanged -= HandleTierChanged;
                momentum.OnDecayProgress -= HandleDecayProgress;
            }
            if (health != null)
                health.OnDeath -= HandleDeath;
            momentum = null;
            health = null;
        }

        void OnDisable() => Unbind();
        void OnDestroy() => Unbind();

        void HandleTierChanged(MomentumTier oldTier, MomentumTier newTier)
        {
            int newT = (int)newTier;
            bool wasHigher = (int)oldTier < newT;
            currentTier = newT;
            shattered = false;
            if (ringGroup != null) ringGroup.alpha = 1f;

            targetColor = ResolveTierColor(newT);

            if (newT == 0)
            {
                // dropped to cold via decay (not death): no flash, just settle
                if (ringFill != null) ringFill.fillAmount = 0f;
                ApplyColor(targetColor);
                return;
            }

            if (wasHigher && !SettingsService.ReduceMotion)
            {
                if (tierUpRoutine != null) StopCoroutine(tierUpRoutine);
                tierUpRoutine = StartCoroutine(TierUpFlash(targetColor));
            }
            else
            {
                ApplyColor(targetColor);
            }
        }

        void HandleDecayProgress(float fraction)
        {
            decayFraction = Mathf.Clamp01(fraction);
            if (currentTier > 0 && ringFill != null)
                ringFill.fillAmount = decayFraction;
        }

        void HandleDeath(PlayerController killer)
        {
            currentTier = 0;
            if (ringFill != null) ringFill.fillAmount = 0f;
            Shatter();
        }

        void Shatter()
        {
            shattered = true;
            if (shatterVfxPrefab != null && transform is RectTransform)
                Instantiate(shatterVfxPrefab, transform.position, Quaternion.identity, transform.parent);
            if (ringGroup != null) ringGroup.alpha = 0.2f;
            ApplyColor(coldColor);
        }

        void Update()
        {
            if (shattered || currentTier <= 0) return;

            // Animated fire shimmer on tier 3.
            if (currentTier >= GameConstants.MaxTier && !SettingsService.ReduceMotion && ringFill != null)
            {
                float shimmer = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                ringFill.color = Color.Lerp(targetColor, Color.white, shimmer * 0.25f);
            }

            // Decay-warning flicker: when remaining decay time < threshold.
            if (ringGroup != null)
            {
                float remainingSeconds = decayFraction * decayIntervalSeconds;
                if (remainingSeconds < decayFlickerThreshold && !SettingsService.ReduceMotion)
                {
                    float flicker = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * decayFlickerHz));
                    ringGroup.alpha = flicker;
                }
                else
                {
                    ringGroup.alpha = Mathf.MoveTowards(ringGroup.alpha, 1f, Time.deltaTime * 4f);
                }
            }
        }

        IEnumerator TierUpFlash(Color settleColor)
        {
            if (flashOverlay != null) SetFlashAlpha(1f);
            ApplyColor(Color.white);

            float t = 0f;
            while (t < tierUpTweenTime)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / tierUpTweenTime);
                ApplyColor(Color.Lerp(Color.white, settleColor, u));
                if (flashOverlay != null) SetFlashAlpha(1f - u);
                yield return null;
            }

            ApplyColor(settleColor);
            if (flashOverlay != null) SetFlashAlpha(0f);
            tierUpRoutine = null;
        }

        Color ResolveTierColor(int tier)
        {
            if (tier <= 0) return coldColor;
            if (momentum != null && momentum.Config != null)
            {
                Color c = momentum.Config.TierColor(tier);
                // A zero/clear colour in config means "unset"; fall back to fire for the top tier.
                if (c.a > 0.01f && (c.r + c.g + c.b) > 0.01f) return c;
            }
            return tier >= GameConstants.MaxTier ? fireColor : Color.white;
        }

        void ApplyColor(Color c)
        {
            if (ringFill != null) ringFill.color = c;
        }

        void SetFlashAlpha(float a)
        {
            Color c = flashOverlay.color;
            c.a = a;
            flashOverlay.color = c;
        }
    }
}
