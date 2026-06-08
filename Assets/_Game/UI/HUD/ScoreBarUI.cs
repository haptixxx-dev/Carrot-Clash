using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarrotClash.UI
{
    /// <summary>
    /// Top-center team score bar (UI/UX HUD spec). Shows both team scores as a number + a fill bar
    /// reflecting progress toward the score cap, with the centred match timer between them. Listens
    /// to <see cref="GameEvents.OnScoreChanged"/> and <see cref="GameEvents.OnMatchTimerTick"/>.
    /// The leading team's bar pulses green and the trailing team's amber; the timer turns red under
    /// 60 seconds.
    /// </summary>
    [DisallowMultipleComponent]
    public class ScoreBarUI : MonoBehaviour
    {
        [Header("Team A")]
        [SerializeField] TMP_Text scoreLabelA;
        [SerializeField] Image fillA;          // Image type = Filled, Horizontal

        [Header("Team B")]
        [SerializeField] TMP_Text scoreLabelB;
        [SerializeField] Image fillB;          // Image type = Filled, Horizontal

        [Header("Timer")]
        [SerializeField] TMP_Text timerLabel;  // "5:42"

        [Header("Colours")]
        [SerializeField] Color teamAColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] Color teamBColor = new Color(1f, 0.4f, 0.3f);
        [SerializeField] Color leadPulseColor = new Color(0.3f, 0.9f, 0.4f);
        [SerializeField] Color trailPulseColor = new Color(0.95f, 0.75f, 0.2f);
        [SerializeField] Color timerNormalColor = Color.white;
        [SerializeField] Color timerUrgentColor = new Color(0.95f, 0.2f, 0.2f);

        [SerializeField] float urgentSeconds = 60f;
        [SerializeField] float pulseSpeed = 2.5f;

        int scoreA;
        int scoreB;
        int scoreCap = GameConstants.ScoreCap;
        float remaining;
        bool subscribed;

        /// <summary>Subscribe to the global score/timer events. Seeds from the GameModeManager if present.</summary>
        public void Bind()
        {
            if (subscribed) return;
            GameEvents.OnScoreChanged += HandleScoreChanged;
            GameEvents.OnMatchTimerTick += HandleTimerTick;
            subscribed = true;

            if (GameModeManager.Instance != null)
            {
                scoreCap = Mathf.Max(1, GameModeManager.Instance.ScoreCap);
                scoreA = GameModeManager.Instance.GetScore(Team.A);
                scoreB = GameModeManager.Instance.GetScore(Team.B);
                remaining = GameModeManager.Instance.RemainingTime;
            }
            RefreshScores();
            RefreshTimer();
        }

        void OnDisable() => Unbind();
        void OnDestroy() => Unbind();

        void Unbind()
        {
            if (!subscribed) return;
            GameEvents.OnScoreChanged -= HandleScoreChanged;
            GameEvents.OnMatchTimerTick -= HandleTimerTick;
            subscribed = false;
        }

        void HandleScoreChanged(int teamId, int newTotal)
        {
            if (teamId == (int)Team.A) scoreA = newTotal;
            else if (teamId == (int)Team.B) scoreB = newTotal;
            RefreshScores();
        }

        void HandleTimerTick(float remainingSeconds)
        {
            remaining = Mathf.Max(0f, remainingSeconds);
            RefreshTimer();
        }

        void RefreshScores()
        {
            if (scoreLabelA != null) scoreLabelA.text = scoreA.ToString();
            if (scoreLabelB != null) scoreLabelB.text = scoreB.ToString();
            if (fillA != null) fillA.fillAmount = Mathf.Clamp01((float)scoreA / scoreCap);
            if (fillB != null) fillB.fillAmount = Mathf.Clamp01((float)scoreB / scoreCap);
        }

        void RefreshTimer()
        {
            if (timerLabel == null) return;
            int total = Mathf.CeilToInt(remaining);
            int minutes = total / 60;
            int seconds = total % 60;
            timerLabel.text = $"{minutes}:{seconds:00}";
            timerLabel.color = remaining <= urgentSeconds ? timerUrgentColor : timerNormalColor;
        }

        void Update()
        {
            // Leading team bar pulses green; trailing pulses amber.
            if (SettingsService.ReduceMotion) return;
            if (scoreA == scoreB) return;

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed);
            bool aLeads = scoreA > scoreB;

            if (fillA != null)
            {
                Color target = aLeads ? leadPulseColor : trailPulseColor;
                fillA.color = Color.Lerp(teamAColor, target, pulse * 0.5f);
            }
            if (fillB != null)
            {
                Color target = aLeads ? trailPulseColor : leadPulseColor;
                fillB.color = Color.Lerp(teamBColor, target, pulse * 0.5f);
            }
        }
    }
}
