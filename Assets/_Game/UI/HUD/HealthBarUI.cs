using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarrotClash.UI
{
    /// <summary>
    /// Bottom-left HP bar. Listens to <see cref="HealthController.OnHealthChanged"/> and
    /// <see cref="HealthController.OnTempHpChanged"/>. The fill lerps green→yellow→red as HP
    /// drops, pulses red below the low-health threshold, and shows a yellow temp-HP (Starch Armor)
    /// segment layered above the base bar. The fill is animated smoothly so regen ticks feel like
    /// healing rather than instant jumps (UI/UX HUD spec).
    /// </summary>
    [DisallowMultipleComponent]
    public class HealthBarUI : MonoBehaviour
    {
        [Header("Base HP fill")]
        [SerializeField] Image fillImage;            // Image type = Filled, Horizontal
        [SerializeField] TMP_Text valueLabel;        // "72/90"

        [Header("Temp HP (Starch Armor) segment")]
        [SerializeField] Image tempFillImage;        // Image type = Filled, Horizontal; drawn above base
        [SerializeField] GameObject tempRoot;        // shown only while temp HP > 0

        [Header("Low-health pulse")]
        [SerializeField] CanvasGroup lowHealthVignette;   // optional screen-edge vignette
        [SerializeField] float pulseSpeed = 4f;

        [Header("Colours")]
        [SerializeField] Color fullColor = new Color(0.30f, 0.85f, 0.30f);
        [SerializeField] Color midColor = new Color(0.95f, 0.85f, 0.20f);
        [SerializeField] Color lowColor = new Color(0.90f, 0.20f, 0.20f);
        [SerializeField] Color tempColor = new Color(0.95f, 0.80f, 0.20f);

        [Tooltip("How fast the fill chases the target fraction (units of fraction per second).")]
        [SerializeField] float fillLerpSpeed = 3f;

        HealthController health;
        int currentHP;
        int maxHP = 1;
        int tempHP;
        float displayedFraction;
        float tempReferenceMax = 1f;   // temp HP is shown relative to max HP for a stable scale
        bool isLow;

        /// <summary>Wire this widget to a player's health subsystem. Safe to call repeatedly (rebinds).</summary>
        public void Bind(HealthController source)
        {
            Unbind();
            health = source;
            if (health == null) return;

            health.OnHealthChanged += HandleHealthChanged;
            health.OnTempHpChanged += HandleTempHpChanged;

            // Seed from current state so the bar is correct immediately on bind.
            HandleHealthChanged(health.CurrentHP, health.MaxHP);
            HandleTempHpChanged(health.TempHP);
            displayedFraction = maxHP > 0 ? (float)currentHP / maxHP : 0f;
            ApplyVisuals(true);
        }

        public void Unbind()
        {
            if (health == null) return;
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnTempHpChanged -= HandleTempHpChanged;
            health = null;
        }

        void OnDisable() => Unbind();
        void OnDestroy() => Unbind();

        void HandleHealthChanged(int cur, int max)
        {
            currentHP = cur;
            maxHP = Mathf.Max(1, max);
            tempReferenceMax = maxHP;
            isLow = currentHP > 0 && currentHP <= GameConstants.LowHealthThreshold;
            UpdateLabel();
        }

        void HandleTempHpChanged(int temp)
        {
            tempHP = Mathf.Max(0, temp);
            if (tempRoot != null) tempRoot.SetActive(tempHP > 0);
            if (tempFillImage != null)
            {
                tempFillImage.color = tempColor;
                tempFillImage.fillAmount = Mathf.Clamp01(tempHP / Mathf.Max(1f, tempReferenceMax));
            }
            UpdateLabel();
        }

        void UpdateLabel()
        {
            if (valueLabel == null) return;
            valueLabel.text = tempHP > 0
                ? $"{currentHP}/{maxHP} (+{tempHP})"
                : $"{currentHP}/{maxHP}";
        }

        void Update()
        {
            float target = maxHP > 0 ? (float)currentHP / maxHP : 0f;
            displayedFraction = Mathf.MoveTowards(displayedFraction, target, fillLerpSpeed * Time.deltaTime);
            ApplyVisuals(false);
        }

        void ApplyVisuals(bool snap)
        {
            if (snap)
            {
                float t = maxHP > 0 ? (float)currentHP / maxHP : 0f;
                displayedFraction = t;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = displayedFraction;

                // green (full) -> yellow (mid) -> red (low)
                Color baseColor = displayedFraction > 0.5f
                    ? Color.Lerp(midColor, fullColor, (displayedFraction - 0.5f) * 2f)
                    : Color.Lerp(lowColor, midColor, displayedFraction * 2f);

                if (isLow && !SettingsService.ReduceMotion)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed);
                    baseColor = Color.Lerp(baseColor, lowColor, pulse * 0.6f);
                }

                fillImage.color = baseColor;
            }

            if (lowHealthVignette != null)
            {
                float targetAlpha = (isLow && !SettingsService.ReduceMotion)
                    ? 0.25f + 0.25f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * pulseSpeed * 0.5f))
                    : 0f;
                lowHealthVignette.alpha = Mathf.MoveTowards(lowHealthVignette.alpha, targetAlpha, Time.deltaTime * 2f);
            }
        }
    }
}
