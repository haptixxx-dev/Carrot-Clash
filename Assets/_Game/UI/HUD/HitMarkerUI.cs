using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CarrotClash.UI
{
    /// <summary>
    /// Centre-screen hit marker (Game Feel "Hit Confirmation" table). Listens to the local
    /// <see cref="WeaponController.OnHitConfirmed"/> and briefly flashes an X over the crosshair:
    /// <list type="bullet">
    /// <item>White X on a body hit (0.1s).</item>
    /// <item>Larger gold/orange X on a kill (0.2s).</item>
    /// <item>Yellow-tinted X when the hit landed on Starch Armor / temp HP (result.HitTempHp).</item>
    /// </list>
    /// Binds to the local player on spawn; uses a single reusable marker Image (no allocation per hit).
    /// </summary>
    [DisallowMultipleComponent]
    public class HitMarkerUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The X marker graphic, centred on the crosshair. Drives colour, scale and alpha.")]
        [SerializeField] Image marker;
        [SerializeField] CanvasGroup canvasGroup;

        [Header("Colours")]
        [SerializeField] Color bodyColor = Color.white;
        [SerializeField] Color killColor = new Color(1f, 0.7f, 0.15f, 1f);    // gold/orange
        [SerializeField] Color tempHpColor = new Color(1f, 0.92f, 0.3f, 1f);  // yellow (armor)

        [Header("Sizing / timing")]
        [SerializeField] float bodyScale = 1f;
        [SerializeField] float killScale = 1.6f;
        [SerializeField] float bodyShowTime = 0.1f;
        [SerializeField] float killShowTime = 0.2f;

        PlayerController localPlayer;
        RectTransform markerRect;
        Coroutine showRoutine;

        void Awake()
        {
            if (canvasGroup == null && marker != null) canvasGroup = marker.GetComponent<CanvasGroup>();
            if (marker != null) markerRect = marker.rectTransform;
            HideImmediate();
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
            if (pc == null || !pc.IsLocal || pc == localPlayer) return;
            Bind(pc);
        }

        void Bind(PlayerController pc)
        {
            Unbind();
            localPlayer = pc;
            if (localPlayer.weapon != null)
                localPlayer.weapon.OnHitConfirmed += HandleHitConfirmed;
        }

        void Unbind()
        {
            if (localPlayer != null && localPlayer.weapon != null)
                localPlayer.weapon.OnHitConfirmed -= HandleHitConfirmed;
            localPlayer = null;
        }

        void HandleHitConfirmed(DamageResult result, Vector3 hitPoint)
        {
            Color color;
            float scale;
            float showTime;

            if (result.WasLethal)
            {
                color = killColor;
                scale = killScale;
                showTime = killShowTime;
            }
            else if (result.HitTempHp)
            {
                color = tempHpColor;
                scale = bodyScale;
                showTime = bodyShowTime;
            }
            else
            {
                color = bodyColor;
                scale = bodyScale;
                showTime = bodyShowTime;
            }

            Flash(color, scale, showTime);
        }

        void Flash(Color color, float scale, float showTime)
        {
            if (marker == null) return;
            if (showRoutine != null) StopCoroutine(showRoutine);
            showRoutine = StartCoroutine(FlashRoutine(color, scale, showTime));
        }

        IEnumerator FlashRoutine(Color color, float scale, float showTime)
        {
            marker.color = color;
            if (markerRect != null) markerRect.localScale = Vector3.one * scale;
            SetAlpha(1f);

            // Brief hold; use unscaled time so the marker still resolves correctly during hit-stop.
            float t = 0f;
            while (t < showTime)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(1f, 0f, showTime > 0f ? t / showTime : 1f));
                yield return null;
            }
            HideImmediate();
            showRoutine = null;
        }

        void SetAlpha(float a)
        {
            if (canvasGroup != null) { canvasGroup.alpha = a; return; }
            if (marker != null)
            {
                Color c = marker.color;
                c.a = a;
                marker.color = c;
            }
        }

        void HideImmediate()
        {
            SetAlpha(0f);
            if (markerRect != null) markerRect.localScale = Vector3.one * bodyScale;
        }
    }
}
