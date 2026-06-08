using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CarrotClash.UI
{
    /// <summary>
    /// One-shot translucent "YOU'RE ON FIRE" full-screen flourish, shown the first time the local
    /// player reaches momentum tier 3 in a session (Momentum Feel doc — 1.5s translucent overlay).
    /// Driven by <see cref="FeedbackController"/>. Animates fade in/hold/out via a CanvasGroup so it
    /// never blocks input. Respects reduce-motion by snapping to a held alpha instead of pulsing.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class OnFireOverlay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] Image backdrop;

        [Header("Timing")]
        [SerializeField] float fadeInTime = 0.2f;
        [SerializeField] float holdTime = 1.1f;
        [SerializeField] float fadeOutTime = 0.2f;
        [SerializeField] float peakAlpha = 0.85f;

        [Header("Style")]
        [SerializeField] string message = "YOU'RE ON FIRE";
        [SerializeField] Color fireColor = new Color(1f, 0.55f, 0.1f, 1f);

        Coroutine routine;

        void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            if (label != null)
            {
                label.text = message;
                label.color = fireColor;
            }
            if (backdrop != null)
            {
                Color c = backdrop.color;
                c.a = 0f;
                backdrop.color = c;
                backdrop.raycastTarget = false;
            }
        }

        /// <summary>Play the overlay once. Re-triggering restarts the animation.</summary>
        public void Show()
        {
            if (!isActiveAndEnabled) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(PlayRoutine());
        }

        IEnumerator PlayRoutine()
        {
            // Reduce-motion: brief static flash with no pulse, then fade out.
            if (SettingsService.ReduceMotion)
            {
                canvasGroup.alpha = peakAlpha;
                yield return new WaitForSecondsRealtime(holdTime);
                canvasGroup.alpha = 0f;
                routine = null;
                yield break;
            }

            float t = 0f;
            while (t < fadeInTime)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, peakAlpha, fadeInTime > 0f ? t / fadeInTime : 1f);
                yield return null;
            }
            canvasGroup.alpha = peakAlpha;

            yield return new WaitForSecondsRealtime(holdTime);

            t = 0f;
            while (t < fadeOutTime)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(peakAlpha, 0f, fadeOutTime > 0f ? t / fadeOutTime : 1f);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            routine = null;
        }
    }
}
