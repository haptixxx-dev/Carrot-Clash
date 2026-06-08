using UnityEngine;
using UnityEngine.UI;

namespace CarrotClash.UI
{
    /// <summary>
    /// Centre-screen crosshair: a dot plus four short lines (UI/UX HUD spec + Game Feel). The lines
    /// expand when hip-firing and contract toward the dot when aiming down sights (polled from the
    /// local <see cref="WeaponController.IsAiming"/>). It flashes red on a confirmed hit
    /// (<see cref="WeaponController.OnHitConfirmed"/>) and gold on a kill
    /// (<see cref="GameEvents.OnKill"/> for the local player). During ADS the crosshair fades out so
    /// the iron sight reads instead.
    /// </summary>
    [DisallowMultipleComponent]
    public class CrosshairUI : MonoBehaviour
    {
        [Header("Parts")]
        [SerializeField] Image dot;
        [SerializeField] RectTransform lineTop;
        [SerializeField] RectTransform lineBottom;
        [SerializeField] RectTransform lineLeft;
        [SerializeField] RectTransform lineRight;
        [SerializeField] CanvasGroup group;        // for the ADS fade

        [Header("Spread (line offset from centre, px)")]
        [SerializeField] float hipGap = 16f;
        [SerializeField] float adsGap = 6f;
        [SerializeField] float gapLerpSpeed = 12f;

        [Header("Hit / kill feedback")]
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color hitColor = new Color(0.95f, 0.2f, 0.2f);
        [SerializeField] Color killColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] float hitFlashTime = 0.1f;
        [SerializeField] float killFlashTime = 0.2f;

        [Header("ADS fade")]
        [SerializeField] float adsAlpha = 0.15f;
        [SerializeField] float fadeLerpSpeed = 10f;

        WeaponController weapon;
        PlayerController localPlayer;
        bool subscribedKill;

        float currentGap;
        float flashTimer;
        float flashDuration;
        Color flashColor;

        Image lineTopImage;
        Image lineBottomImage;
        Image lineLeftImage;
        Image lineRightImage;

        void Awake() => CacheLineImages();

        void CacheLineImages()
        {
            if (lineTop != null) lineTopImage = lineTop.GetComponent<Image>();
            if (lineBottom != null) lineBottomImage = lineBottom.GetComponent<Image>();
            if (lineLeft != null) lineLeftImage = lineLeft.GetComponent<Image>();
            if (lineRight != null) lineRightImage = lineRight.GetComponent<Image>();
        }

        /// <summary>Wire to the local player. Polls its weapon for ADS state and listens for hits/kills.</summary>
        public void Bind(PlayerController player)
        {
            Unbind();
            CacheLineImages();
            localPlayer = player;
            weapon = player != null ? player.weapon : null;

            if (weapon != null)
                weapon.OnHitConfirmed += HandleHitConfirmed;

            GameEvents.OnKill += HandleKill;
            subscribedKill = true;

            currentGap = hipGap;
            ApplyGap();
            ApplyColor(normalColor);
        }

        public void Unbind()
        {
            if (weapon != null)
                weapon.OnHitConfirmed -= HandleHitConfirmed;
            if (subscribedKill)
            {
                GameEvents.OnKill -= HandleKill;
                subscribedKill = false;
            }
            weapon = null;
            localPlayer = null;
        }

        void OnDisable() => Unbind();
        void OnDestroy() => Unbind();

        void HandleHitConfirmed(DamageResult result, Vector3 hitPoint)
        {
            // Kill flash is driven by OnKill; here we show the per-hit red confirm.
            if (result.WasLethal) return;
            TriggerFlash(hitColor, hitFlashTime);
        }

        void HandleKill(KillEvent e)
        {
            if (localPlayer == null || e.Killer != localPlayer || e.Killer == e.Victim) return;
            TriggerFlash(killColor, killFlashTime);
        }

        void TriggerFlash(Color color, float duration)
        {
            flashColor = color;
            flashDuration = Mathf.Max(0.01f, duration);
            flashTimer = flashDuration;
        }

        void Update()
        {
            bool aiming = weapon != null && weapon.IsAiming;

            // Expand on hipfire, contract on ADS.
            float targetGap = aiming ? adsGap : hipGap;
            currentGap = Mathf.MoveTowards(currentGap, targetGap, gapLerpSpeed * Time.deltaTime * 10f);
            ApplyGap();

            // Hit / kill colour flash decays back to white.
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                float u = Mathf.Clamp01(flashTimer / flashDuration);
                ApplyColor(Color.Lerp(normalColor, flashColor, u));
            }
            else
            {
                ApplyColor(normalColor);
            }

            // Fade out during ADS so the iron sight reads.
            if (group != null)
            {
                float targetAlpha = aiming ? adsAlpha : 1f;
                group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, fadeLerpSpeed * Time.deltaTime);
            }
        }

        void ApplyGap()
        {
            if (lineTop != null) lineTop.anchoredPosition = new Vector2(0f, currentGap);
            if (lineBottom != null) lineBottom.anchoredPosition = new Vector2(0f, -currentGap);
            if (lineLeft != null) lineLeft.anchoredPosition = new Vector2(-currentGap, 0f);
            if (lineRight != null) lineRight.anchoredPosition = new Vector2(currentGap, 0f);
        }

        void ApplyColor(Color c)
        {
            if (dot != null) dot.color = c;
            if (lineTopImage != null) lineTopImage.color = c;
            if (lineBottomImage != null) lineBottomImage.color = c;
            if (lineLeftImage != null) lineLeftImage.color = c;
            if (lineRightImage != null) lineRightImage.color = c;
        }
    }
}
