using System.Collections.Generic;
using System.Text;
using CarrotClash.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarrotClash.UI
{
    /// <summary>
    /// Main menu screen (UI/UX doc "1. Main Menu"). Hosts the primary navigation buttons, shows the
    /// player level + XP bar (<see cref="XPManager"/>), the daily-challenge summary
    /// (<see cref="ChallengeSystem"/>), and the battle-pass progress (<see cref="ProgressionService"/>).
    /// Play loads gameplay via <see cref="SceneFlow"/>. Button presses route SFX through the
    /// <see cref="CarrotClash.Audio.AudioManager"/>. Designer wires the serialized references on the
    /// prefab; all behaviour lives here.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuController : MonoBehaviour
    {
        [Header("Navigation Buttons")]
        [SerializeField] Button playButton;
        [SerializeField] Button partyButton;
        [SerializeField] Button progressionButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button quitButton;

        [Header("Panels (toggled by nav)")]
        [SerializeField] GameObject partyPanel;
        [SerializeField] GameObject progressionPanel;
        [SerializeField] GameObject settingsPanel;

        [Header("Player Level / XP")]
        [SerializeField] TMP_Text levelLabel;
        [SerializeField] TMP_Text xpLabel;
        [SerializeField] Slider xpBar;

        [Header("Daily Challenges Card")]
        [Tooltip("ChallengeSystem in the scene (persistent app-root). Used to read live daily progress.")]
        [SerializeField] ChallengeSystem challengeSystem;
        [SerializeField] TMP_Text challengeSummaryLabel;
        [SerializeField] TMP_Text challengeListLabel;

        [Header("Battle Pass Card")]
        [SerializeField] TMP_Text battlePassTierLabel;
        [SerializeField] Slider battlePassBar;
        [SerializeField] TMP_Text battlePassNextRewardLabel;

        readonly StringBuilder builder = new StringBuilder(160);

        void OnEnable()
        {
            WireButton(playButton, OnPlay);
            WireButton(partyButton, OnParty);
            WireButton(progressionButton, OnProgression);
            WireButton(settingsButton, OnSettings);
            WireButton(quitButton, OnQuit);

            XPManager.OnXpChanged += HandleXpChanged;
            XPManager.OnLevelUp += HandleLevelUp;
            BattlePassService.OnTierUnlocked += HandleTierUnlocked;
            ChallengeSystem.OnChallengesChanged += RefreshChallenges;
            ChallengeSystem.OnChallengeCompleted += HandleChallengeCompleted;

            ShowPanel(null);
            RefreshAll();
        }

        void OnDisable()
        {
            UnwireButton(playButton, OnPlay);
            UnwireButton(partyButton, OnParty);
            UnwireButton(progressionButton, OnProgression);
            UnwireButton(settingsButton, OnSettings);
            UnwireButton(quitButton, OnQuit);

            XPManager.OnXpChanged -= HandleXpChanged;
            XPManager.OnLevelUp -= HandleLevelUp;
            BattlePassService.OnTierUnlocked -= HandleTierUnlocked;
            ChallengeSystem.OnChallengesChanged -= RefreshChallenges;
            ChallengeSystem.OnChallengeCompleted -= HandleChallengeCompleted;
        }

        void WireButton(Button b, UnityEngine.Events.UnityAction action)
        {
            if (b != null) b.onClick.AddListener(action);
        }

        void UnwireButton(Button b, UnityEngine.Events.UnityAction action)
        {
            if (b != null) b.onClick.RemoveListener(action);
        }

        // ----- Refresh -----

        void RefreshAll()
        {
            RefreshXp();
            RefreshChallenges();
            RefreshBattlePass();
        }

        void HandleXpChanged(int currentXpIntoLevel, int xpRequiredForNextLevel)
        {
            ApplyXp(ProgressionService.Level, ProgressionService.Prestige, currentXpIntoLevel, xpRequiredForNextLevel);
        }

        void HandleLevelUp(int newLevel)
        {
            // OnXpChanged fires alongside level-ups; refresh defensively in case only level moved.
            RefreshXp();
        }

        void RefreshXp()
        {
            ApplyXp(ProgressionService.Level, ProgressionService.Prestige,
                ProgressionService.CurrentXp, ProgressionService.XpToNextLevel);
        }

        void ApplyXp(int level, int prestige, int xpIntoLevel, int xpForNext)
        {
            if (levelLabel != null)
            {
                builder.Clear();
                builder.Append("Level ").Append(level);
                if (prestige > 0) builder.Append("  ★").Append(prestige);
                levelLabel.text = builder.ToString();
            }

            if (xpLabel != null)
            {
                xpLabel.text = xpForNext > 0
                    ? xpIntoLevel + " / " + xpForNext
                    : "MAX";
            }

            if (xpBar != null)
            {
                xpBar.minValue = 0f;
                xpBar.maxValue = 1f;
                xpBar.value = xpForNext > 0 ? Mathf.Clamp01((float)xpIntoLevel / xpForNext) : 1f;
            }
        }

        void RefreshChallenges()
        {
            IReadOnlyList<DailyChallenge> dailies = challengeSystem != null
                ? challengeSystem.Dailies
                : ProgressionService.DailyChallenges;

            int completed = 0;
            int total = dailies != null ? dailies.Count : 0;
            if (dailies != null)
            {
                for (int i = 0; i < dailies.Count; i++)
                    if (dailies[i] != null && dailies[i].completed) completed++;
            }

            if (challengeSummaryLabel != null)
                challengeSummaryLabel.text = "DAILY CHALLENGES  " + completed + " / " + total;

            if (challengeListLabel != null)
            {
                builder.Clear();
                if (dailies != null)
                {
                    for (int i = 0; i < dailies.Count; i++)
                    {
                        DailyChallenge c = dailies[i];
                        if (c == null) continue;
                        builder.Append(c.completed ? "✓ " : "○ ");
                        builder.Append(c.description);
                        builder.Append("  +").Append(c.xpReward).Append(" XP\n");
                    }
                }
                challengeListLabel.text = builder.ToString().TrimEnd('\n');
            }
        }

        void HandleChallengeCompleted(DailyChallenge challenge)
        {
            // XP already granted by ChallengeSystem; just refresh the cards.
            RefreshChallenges();
            RefreshXp();
            RefreshBattlePass();
        }

        void HandleTierUnlocked(int newTier)
        {
            RefreshBattlePass();
        }

        void RefreshBattlePass()
        {
            int tier = ProgressionService.BattlePassTier;

            if (battlePassTierLabel != null)
                battlePassTierLabel.text = "Season 1 · Tier " + tier;

            if (battlePassBar != null)
            {
                battlePassBar.minValue = 0f;
                battlePassBar.maxValue = 1f;
                battlePassBar.value = ProgressionService.BattlePassFraction;
            }

            if (battlePassNextRewardLabel != null)
            {
                int nextTier = Mathf.Min(tier + 1, BattlePassService.MaxTier);
                BattlePassReward next = ProgressionService.GetBattlePassReward(nextTier);
                battlePassNextRewardLabel.text = "Next: " + next.DisplayName;
            }
        }

        // ----- Button handlers -----

        void OnPlay()
        {
            PlayConfirm();
            if (SceneFlow.IsLoading) return;
            SceneFlow.LoadGameplay();
        }

        void OnParty()
        {
            PlayConfirm();
            ShowPanel(partyPanel);
        }

        void OnProgression()
        {
            PlayConfirm();
            ShowPanel(progressionPanel);
        }

        void OnSettings()
        {
            PlayConfirm();
            ShowPanel(settingsPanel);
        }

        void OnQuit()
        {
            PlayConfirm();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void ShowPanel(GameObject panel)
        {
            if (partyPanel != null) partyPanel.SetActive(panel == partyPanel);
            if (progressionPanel != null) progressionPanel.SetActive(panel == progressionPanel);
            if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);
        }

        static void PlayConfirm()
        {
            CarrotClash.Audio.AudioManager.Instance?.PlayUi("ui_button_confirm");
        }
    }
}
