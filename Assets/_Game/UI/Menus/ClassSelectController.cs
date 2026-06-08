using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarrotClash.UI
{
    /// <summary>
    /// Class-select screen (UI/UX doc "3. Class Select"). Four class buttons (Carrot / Jalapeño /
    /// Broccoli / Potato), a stats + ability preview for the highlighted class, a 30s countdown that
    /// auto-confirms (<see cref="GameConstants.ClassSelectDuration"/>), and a team-composition preview.
    /// Raises <see cref="OnClassConfirmed"/> when the player confirms (or the timer hits zero).
    /// Enemy picks stay hidden until match start (design note: no last-second counter-picking).
    /// The CharacterDataSO list is resolved via a serialized array wired by the designer.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClassSelectController : MonoBehaviour
    {
        /// <summary>Fired once with the locked-in class when the player confirms or the timer expires.</summary>
        public event Action<ClassId> OnClassConfirmed;

        const string LastClassPrefKey = "cc_last_class";

        [Header("Character Data (designer-wired, one per class)")]
        [SerializeField] CharacterDataSO[] characters = new CharacterDataSO[4];

        [Header("Class Buttons (index matches ClassId order)")]
        [SerializeField] Button[] classButtons = new Button[4];
        [Tooltip("Optional per-button selection highlight, toggled to show the active pick.")]
        [SerializeField] GameObject[] selectionHighlights = new GameObject[4];

        [Header("Selected Class Preview")]
        [SerializeField] TMP_Text classNameLabel;
        [SerializeField] TMP_Text taglineLabel;
        [SerializeField] TMP_Text difficultyLabel;
        [SerializeField] TMP_Text statsLabel;
        [SerializeField] TMP_Text active1Label;
        [SerializeField] TMP_Text active2Label;
        [SerializeField] TMP_Text passiveLabel;

        [Header("Countdown")]
        [SerializeField] TMP_Text countdownLabel;
        [SerializeField] Slider countdownBar;

        [Header("Team Composition Preview")]
        [SerializeField] TMP_Text yourTeamLabel;
        [SerializeField] TMP_Text enemyTeamLabel;
        [Tooltip("Optional Broccoli (support) icon highlight when your team has no support pick.")]
        [SerializeField] GameObject supportNeededHint;
        [Tooltip("Placeholder teammate class picks for the live composition preview.")]
        [SerializeField] ClassId[] teammatePicks = new ClassId[3];

        [Header("Confirm")]
        [SerializeField] Button confirmButton;

        readonly StringBuilder builder = new StringBuilder(160);

        ClassId selectedClass = ClassId.Carrot;
        float timeRemaining;
        bool confirmed;

        /// <summary>The class currently highlighted (not yet confirmed).</summary>
        public ClassId SelectedClass => selectedClass;

        void OnEnable()
        {
            confirmed = false;
            timeRemaining = GameConstants.ClassSelectDuration;

            for (int i = 0; i < classButtons.Length; i++)
            {
                if (classButtons[i] == null) continue;
                int captured = i;
                classButtons[i].onClick.AddListener(() => SelectClass((ClassId)captured));
            }
            if (confirmButton != null) confirmButton.onClick.AddListener(ConfirmSelection);

            // Default to the most-recently-used class (design note: auto-selects MRU if idle).
            selectedClass = (ClassId)Mathf.Clamp(PlayerPrefs.GetInt(LastClassPrefKey, 0), 0, 3);

            RefreshPreview();
            RefreshTeamComposition();
            RefreshCountdown();
        }

        void OnDisable()
        {
            for (int i = 0; i < classButtons.Length; i++)
                if (classButtons[i] != null) classButtons[i].onClick.RemoveAllListeners();
            if (confirmButton != null) confirmButton.onClick.RemoveAllListeners();
        }

        void Update()
        {
            if (confirmed) return;

            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                RefreshCountdown();
                ConfirmSelection();       // auto-confirm at 0
                return;
            }
            RefreshCountdown();
        }

        CharacterDataSO DataFor(ClassId id)
        {
            for (int i = 0; i < characters.Length; i++)
                if (characters[i] != null && characters[i].classId == id) return characters[i];
            // Fall back to index mapping if classId wasn't set on the asset.
            int idx = (int)id;
            return (idx >= 0 && idx < characters.Length) ? characters[idx] : null;
        }

        void SelectClass(ClassId id)
        {
            if (confirmed) return;
            selectedClass = id;
            CarrotClash.Audio.AudioManager.Instance?.PlayUi("ui_button_hover");
            RefreshPreview();
            RefreshTeamComposition();
        }

        void RefreshPreview()
        {
            for (int i = 0; i < selectionHighlights.Length; i++)
                if (selectionHighlights[i] != null)
                    selectionHighlights[i].SetActive(i == (int)selectedClass);

            CharacterDataSO data = DataFor(selectedClass);
            if (data == null)
            {
                if (classNameLabel != null) classNameLabel.text = selectedClass.ToString().ToUpperInvariant();
                if (taglineLabel != null) taglineLabel.text = string.Empty;
                if (difficultyLabel != null) difficultyLabel.text = string.Empty;
                if (statsLabel != null) statsLabel.text = string.Empty;
                SetAbilityLabel(active1Label, null);
                SetAbilityLabel(active2Label, null);
                if (passiveLabel != null) passiveLabel.text = "Passive: —";
                return;
            }

            if (classNameLabel != null)
                classNameLabel.text = (string.IsNullOrEmpty(data.characterName)
                    ? selectedClass.ToString()
                    : data.characterName).ToUpperInvariant();
            if (taglineLabel != null) taglineLabel.text = data.tagline;
            if (difficultyLabel != null) difficultyLabel.text = Stars(data.difficulty);

            if (statsLabel != null)
            {
                builder.Clear();
                builder.Append("HP ").Append(data.baseHP);
                builder.Append("   Speed ").Append(data.baseMoveSpeed.ToString("0.#")).Append(" m/s");
                if (data.primaryWeapon != null)
                    builder.Append("   Weapon ").Append(data.primaryWeapon.weaponName);
                statsLabel.text = builder.ToString();
            }

            SetAbilityLabel(active1Label, data.active1);
            SetAbilityLabel(active2Label, data.active2);
            if (passiveLabel != null)
                passiveLabel.text = data.passive != null
                    ? "Passive: " + data.passive.abilityName
                    : "Passive: —";
        }

        void SetAbilityLabel(TMP_Text label, AbilityDataSO ability)
        {
            if (label == null) return;
            if (ability == null) { label.text = "—"; return; }
            builder.Clear();
            builder.Append(ability.abilityName);
            if (ability.cooldown > 0f)
                builder.Append("  (").Append(ability.cooldown.ToString("0.#")).Append("s cd)");
            label.text = builder.ToString();
        }

        void RefreshTeamComposition()
        {
            bool teamHasSupport = selectedClass == ClassId.Broccoli;

            if (yourTeamLabel != null)
            {
                builder.Clear();
                builder.Append("You  → ").Append(selectedClass.ToString().ToUpperInvariant()).Append('\n');
                for (int i = 0; i < teammatePicks.Length; i++)
                {
                    ClassId pick = teammatePicks[i];
                    if (pick == ClassId.Broccoli) teamHasSupport = true;
                    builder.Append('P').Append(i + 2).Append("   → ").Append(pick.ToString()).Append('\n');
                }
                yourTeamLabel.text = builder.ToString().TrimEnd('\n');
            }

            // Subtly highlight the support class when the team lacks one (design note).
            if (supportNeededHint != null) supportNeededHint.SetActive(!teamHasSupport);

            // Enemy picks hidden until match start (no last-second counter-picking).
            if (enemyTeamLabel != null) enemyTeamLabel.text = "? ? ? ?";
        }

        void RefreshCountdown()
        {
            if (countdownLabel != null) countdownLabel.text = Mathf.CeilToInt(timeRemaining) + "s";
            if (countdownBar != null)
            {
                countdownBar.minValue = 0f;
                countdownBar.maxValue = 1f;
                countdownBar.value = GameConstants.ClassSelectDuration > 0f
                    ? Mathf.Clamp01(timeRemaining / GameConstants.ClassSelectDuration)
                    : 0f;
            }
        }

        void ConfirmSelection()
        {
            if (confirmed) return;
            confirmed = true;
            PlayerPrefs.SetInt(LastClassPrefKey, (int)selectedClass);
            PlayerPrefs.Save();
            CarrotClash.Audio.AudioManager.Instance?.PlayUi("ui_button_confirm");
            OnClassConfirmed?.Invoke(selectedClass);
        }

        static string Stars(int difficulty)
        {
            difficulty = Mathf.Clamp(difficulty, 1, 5);
            return new string('★', difficulty) + new string('☆', 5 - difficulty);
        }
    }
}
