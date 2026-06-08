using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CarrotClash.UI
{
    /// <summary>
    /// A single pooled kill-feed row: killer icon, arrow, victim icon, an optional "+1 tier" transfer
    /// badge, and a border that flashes gold for the local player's kills. Owned and recycled by
    /// <see cref="KillFeedUI"/> — it holds no game state, only presentation.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class KillFeedEntry : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] Image killerIcon;
        [SerializeField] Image victimIcon;
        [SerializeField] Image arrow;
        [Tooltip("'+1 tier' badge overlaid on the killer icon; hidden unless this was a tier transfer.")]
        [SerializeField] GameObject tierBadge;
        [Tooltip("Border graphic flashed gold when the local player scored the kill.")]
        [SerializeField] Image border;

        Coroutine routine;

        /// <summary>True while this entry is visible (or fading) — used by the feed to manage the cap.</summary>
        public bool IsShowing { get; private set; }

        void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>Populate and play this entry. All visual config is passed in by the feed.</summary>
        public void Show(Sprite killer, Sprite victim, bool showTierBadge, bool localKill,
                         Color goldBorder, float lifetime, float fadeTime, float goldFlashTime)
        {
            if (killerIcon != null) killerIcon.sprite = killer;
            if (victimIcon != null) victimIcon.sprite = victim;
            if (tierBadge != null) tierBadge.SetActive(showTierBadge);

            if (border != null)
            {
                if (localKill)
                {
                    border.enabled = true;
                    Color c = goldBorder;
                    c.a = 1f;
                    border.color = c;
                }
                else
                {
                    border.enabled = false;
                }
            }

            canvasGroup.alpha = 1f;
            gameObject.SetActive(true);
            IsShowing = true;

            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(LifetimeRoutine(localKill, lifetime, fadeTime, goldFlashTime));
        }

        IEnumerator LifetimeRoutine(bool localKill, float lifetime, float fadeTime, float goldFlashTime)
        {
            // Gold border flash (local kill only), then fade the border out while the row holds.
            if (localKill && border != null && border.enabled)
            {
                float bt = 0f;
                Color start = border.color;
                while (bt < goldFlashTime)
                {
                    bt += Time.unscaledDeltaTime;
                    Color c = start;
                    c.a = Mathf.Lerp(1f, 0f, goldFlashTime > 0f ? bt / goldFlashTime : 1f);
                    border.color = c;
                    yield return null;
                }
                border.enabled = false;
            }

            float hold = Mathf.Max(0f, lifetime - fadeTime);
            yield return new WaitForSecondsRealtime(hold);

            float t = 0f;
            while (t < fadeTime)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeTime > 0f ? t / fadeTime : 1f);
                yield return null;
            }

            HideImmediate();
        }

        /// <summary>Stop any animation and hide immediately (used when recycled by the feed).</summary>
        public void HideImmediate()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
            IsShowing = false;
            canvasGroup.alpha = 0f;
            if (border != null) border.enabled = false;
            gameObject.SetActive(false);
        }
    }
}
