using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarrotClash.UI
{
    /// <summary>
    /// One ability slot widget (Bottom-center, flanking the momentum ring). Represents a single
    /// <see cref="AbilitySlot"/> (Active1 = Q, Active2 = E). Listens to the shared
    /// <see cref="AbilityController.OnCooldownChanged"/> and <see cref="AbilityController.OnAbilityActivated"/>
    /// events and filters by its own slot index. Shows a radial greyscale cooldown overlay that
    /// drains clockwise from the top, the ability icon + key label, a remaining-seconds readout,
    /// and a brief white flash on activation and on becoming ready (UI/UX + Game Feel).
    /// </summary>
    [DisallowMultipleComponent]
    public class AbilitySlotUI : MonoBehaviour
    {
        [Header("Slot identity")]
        [SerializeField] int slotIndex = 0;            // 0 = Active1 (Q), 1 = Active2 (E)
        [SerializeField] string keyLabelText = "Q";

        [Header("Visuals")]
        [SerializeField] Image iconImage;
        [SerializeField] Image cooldownOverlay;        // Image type = Filled, Radial360, clockwise, origin Top
        [SerializeField] Image flashOverlay;           // white flash
        [SerializeField] TMP_Text keyLabel;            // "Q" / "E"
        [SerializeField] TMP_Text cooldownLabel;       // "6.0s" while on cooldown, blank when ready

        [Header("Colours")]
        [SerializeField] Color readyTint = Color.white;
        [SerializeField] Color cooldownTint = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] float flashTime = 0.18f;

        AbilityController abilities;
        float cooldownFraction;
        float cooldownDuration;
        bool wasReady = true;
        Coroutine flashRoutine;

        /// <summary>The slot this widget reflects (set by the orchestrator if not authored in-scene).</summary>
        public int SlotIndex => slotIndex;

        /// <summary>Wire to the player's ability controller and seed the icon + key label.</summary>
        public void Bind(AbilityController source, AbilityDataSO slotData, int slot, string keyLabelOverride = null)
        {
            Unbind();
            abilities = source;
            slotIndex = slot;
            if (!string.IsNullOrEmpty(keyLabelOverride)) keyLabelText = keyLabelOverride;

            if (keyLabel != null) keyLabel.text = keyLabelText;
            if (iconImage != null && slotData != null && slotData.icon != null)
                iconImage.sprite = slotData.icon;

            if (slotData != null) cooldownDuration = Mathf.Max(0.01f, slotData.cooldown);

            if (abilities != null)
            {
                abilities.OnCooldownChanged += HandleCooldownChanged;
                abilities.OnAbilityActivated += HandleActivated;

                // Seed current cooldown state.
                cooldownFraction = abilities.CooldownFraction(slotIndex);
                wasReady = abilities.IsReady(slotIndex);
            }

            ApplyVisuals();
        }

        public void Unbind()
        {
            if (abilities != null)
            {
                abilities.OnCooldownChanged -= HandleCooldownChanged;
                abilities.OnAbilityActivated -= HandleActivated;
            }
            abilities = null;
        }

        void OnDisable() => Unbind();
        void OnDestroy() => Unbind();

        void HandleCooldownChanged(int slot, float fraction)
        {
            if (slot != slotIndex) return;
            cooldownFraction = Mathf.Clamp01(fraction);

            bool ready = cooldownFraction <= 0f;
            if (ready && !wasReady)
                PlayFlash();   // ready flash when cooldown completes
            wasReady = ready;

            ApplyVisuals();
        }

        void HandleActivated(int slot)
        {
            if (slot != slotIndex) return;
            wasReady = false;
            PlayFlash();       // activation flash; drain begins immediately via cooldown events
        }

        void Update()
        {
            if (cooldownLabel == null) return;
            if (cooldownFraction > 0f && abilities != null)
            {
                float remaining = abilities.CooldownRemaining(slotIndex);
                cooldownLabel.text = remaining >= 0.05f ? remaining.ToString("0.0") : string.Empty;
            }
        }

        void ApplyVisuals()
        {
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = cooldownFraction;
                cooldownOverlay.enabled = cooldownFraction > 0f;
            }
            if (iconImage != null)
                iconImage.color = cooldownFraction > 0f ? cooldownTint : readyTint;
            if (cooldownLabel != null && cooldownFraction <= 0f)
                cooldownLabel.text = string.Empty;
        }

        void PlayFlash()
        {
            if (flashOverlay == null || SettingsService.ReduceMotion) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            float t = 0f;
            while (t < flashTime)
            {
                t += Time.deltaTime;
                float a = 1f - Mathf.Clamp01(t / flashTime);
                SetFlashAlpha(a);
                yield return null;
            }
            SetFlashAlpha(0f);
            flashRoutine = null;
        }

        void SetFlashAlpha(float a)
        {
            Color c = flashOverlay.color;
            c.a = a;
            flashOverlay.color = c;
        }
    }
}
