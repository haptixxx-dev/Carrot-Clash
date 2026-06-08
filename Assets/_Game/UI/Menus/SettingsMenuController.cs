using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarrotClash.UI
{
    /// <summary>
    /// Settings screen (UI/UX doc "6. Settings"). Four tabs — Video / Audio / Controls / Accessibility —
    /// each binding the matching <see cref="SettingsService"/> properties to uGUI sliders, toggles and
    /// dropdowns. Edits flow straight into the service setters (which persist via PlayerPrefs); video
    /// edits additionally call <see cref="SettingsService.ApplyVideoSettings"/>. The controller also
    /// listens to <see cref="SettingsService.OnSettingsChanged"/> so externally-driven changes keep the
    /// UI in sync, and unsubscribes in <c>OnDisable</c>. Designer wires the serialized widgets.
    /// </summary>
    [DisallowMultipleComponent]
    public class SettingsMenuController : MonoBehaviour
    {
        [Header("Tabs")]
        [SerializeField] Button videoTabButton;
        [SerializeField] Button audioTabButton;
        [SerializeField] Button controlsTabButton;
        [SerializeField] Button accessibilityTabButton;
        [SerializeField] GameObject videoPanel;
        [SerializeField] GameObject audioPanel;
        [SerializeField] GameObject controlsPanel;
        [SerializeField] GameObject accessibilityPanel;

        [Header("Footer")]
        [SerializeField] Button backButton;

        [Header("Video")]
        [SerializeField] Slider fovSlider;                 // 80..110
        [SerializeField] TMP_Text fovValueLabel;
        [SerializeField] TMP_Dropdown qualityDropdown;     // maps to QualitySettings index
        [SerializeField] Toggle vsyncToggle;
        [SerializeField] TMP_Dropdown targetFrameRateDropdown;
        [Tooltip("Frame-rate options shown in the dropdown (index order). -1 means uncapped.")]
        [SerializeField] int[] frameRateOptions = { 30, 60, 90, 120, 144, 240, -1 };

        [Header("Audio")]
        [SerializeField] Slider masterVolumeSlider;        // 0..1
        [SerializeField] Slider musicVolumeSlider;
        [SerializeField] Slider sfxVolumeSlider;
        [SerializeField] Slider voiceVolumeSlider;
        [SerializeField] Toggle spatialAudioToggle;
        [SerializeField] Toggle subtitlesToggle;

        [Header("Controls")]
        [SerializeField] Slider mouseSensitivitySlider;    // X
        [SerializeField] TMP_Text mouseSensitivityValueLabel;
        [SerializeField] Slider mouseSensitivityYSlider;   // Y
        [SerializeField] TMP_Text mouseSensitivityYValueLabel;
        [SerializeField] Slider adsSensMultiplierSlider;
        [SerializeField] TMP_Text adsSensValueLabel;
        [SerializeField] Toggle aimAssistToggle;

        [Header("Accessibility")]
        [SerializeField] TMP_Dropdown colorblindDropdown;  // None / Protanopia / Deuteranopia / Tritanopia
        [SerializeField] Toggle highContrastToggle;
        [SerializeField] Toggle reduceMotionToggle;
        [SerializeField] TMP_Dropdown textSizeDropdown;    // Normal / Large

        // Guard so programmatic widget updates don't echo back into the service setters.
        bool suppressCallbacks;

        void OnEnable()
        {
            WireTabs();
            WireWidgets();

            SettingsService.OnSettingsChanged += RefreshFromSettings;

            RefreshFromSettings();
            ShowTab(videoPanel);
        }

        void OnDisable()
        {
            SettingsService.OnSettingsChanged -= RefreshFromSettings;
            UnwireTabs();
            UnwireWidgets();
        }

        // ----- Wiring -----

        void WireTabs()
        {
            if (videoTabButton != null) videoTabButton.onClick.AddListener(OnVideoTab);
            if (audioTabButton != null) audioTabButton.onClick.AddListener(OnAudioTab);
            if (controlsTabButton != null) controlsTabButton.onClick.AddListener(OnControlsTab);
            if (accessibilityTabButton != null) accessibilityTabButton.onClick.AddListener(OnAccessibilityTab);
            if (backButton != null) backButton.onClick.AddListener(OnBack);
        }

        void UnwireTabs()
        {
            if (videoTabButton != null) videoTabButton.onClick.RemoveListener(OnVideoTab);
            if (audioTabButton != null) audioTabButton.onClick.RemoveListener(OnAudioTab);
            if (controlsTabButton != null) controlsTabButton.onClick.RemoveListener(OnControlsTab);
            if (accessibilityTabButton != null) accessibilityTabButton.onClick.RemoveListener(OnAccessibilityTab);
            if (backButton != null) backButton.onClick.RemoveListener(OnBack);
        }

        void WireWidgets()
        {
            // Video
            if (fovSlider != null) { fovSlider.minValue = 80f; fovSlider.maxValue = 110f; fovSlider.onValueChanged.AddListener(OnFovChanged); }
            if (qualityDropdown != null) qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            if (vsyncToggle != null) vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            if (targetFrameRateDropdown != null) targetFrameRateDropdown.onValueChanged.AddListener(OnTargetFrameRateChanged);

            // Audio
            if (masterVolumeSlider != null) { masterVolumeSlider.minValue = 0f; masterVolumeSlider.maxValue = 1f; masterVolumeSlider.onValueChanged.AddListener(OnMasterVolume); }
            if (musicVolumeSlider != null) { musicVolumeSlider.minValue = 0f; musicVolumeSlider.maxValue = 1f; musicVolumeSlider.onValueChanged.AddListener(OnMusicVolume); }
            if (sfxVolumeSlider != null) { sfxVolumeSlider.minValue = 0f; sfxVolumeSlider.maxValue = 1f; sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolume); }
            if (voiceVolumeSlider != null) { voiceVolumeSlider.minValue = 0f; voiceVolumeSlider.maxValue = 1f; voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolume); }
            if (spatialAudioToggle != null) spatialAudioToggle.onValueChanged.AddListener(OnSpatialAudio);
            if (subtitlesToggle != null) subtitlesToggle.onValueChanged.AddListener(OnSubtitles);

            // Controls
            if (mouseSensitivitySlider != null) { mouseSensitivitySlider.minValue = 0.05f; mouseSensitivitySlider.maxValue = 5f; mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivity); }
            if (mouseSensitivityYSlider != null) { mouseSensitivityYSlider.minValue = 0.05f; mouseSensitivityYSlider.maxValue = 5f; mouseSensitivityYSlider.onValueChanged.AddListener(OnMouseSensitivityY); }
            if (adsSensMultiplierSlider != null) { adsSensMultiplierSlider.minValue = 0.1f; adsSensMultiplierSlider.maxValue = 2f; adsSensMultiplierSlider.onValueChanged.AddListener(OnAdsSens); }
            if (aimAssistToggle != null) aimAssistToggle.onValueChanged.AddListener(OnAimAssist);

            // Accessibility
            if (colorblindDropdown != null) { PopulateColorblindDropdown(); colorblindDropdown.onValueChanged.AddListener(OnColorblind); }
            if (highContrastToggle != null) highContrastToggle.onValueChanged.AddListener(OnHighContrast);
            if (reduceMotionToggle != null) reduceMotionToggle.onValueChanged.AddListener(OnReduceMotion);
            if (textSizeDropdown != null) { PopulateTextSizeDropdown(); textSizeDropdown.onValueChanged.AddListener(OnTextSize); }
        }

        void UnwireWidgets()
        {
            if (fovSlider != null) fovSlider.onValueChanged.RemoveListener(OnFovChanged);
            if (qualityDropdown != null) qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
            if (vsyncToggle != null) vsyncToggle.onValueChanged.RemoveListener(OnVSyncChanged);
            if (targetFrameRateDropdown != null) targetFrameRateDropdown.onValueChanged.RemoveListener(OnTargetFrameRateChanged);

            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolume);
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolume);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolume);
            if (voiceVolumeSlider != null) voiceVolumeSlider.onValueChanged.RemoveListener(OnVoiceVolume);
            if (spatialAudioToggle != null) spatialAudioToggle.onValueChanged.RemoveListener(OnSpatialAudio);
            if (subtitlesToggle != null) subtitlesToggle.onValueChanged.RemoveListener(OnSubtitles);

            if (mouseSensitivitySlider != null) mouseSensitivitySlider.onValueChanged.RemoveListener(OnMouseSensitivity);
            if (mouseSensitivityYSlider != null) mouseSensitivityYSlider.onValueChanged.RemoveListener(OnMouseSensitivityY);
            if (adsSensMultiplierSlider != null) adsSensMultiplierSlider.onValueChanged.RemoveListener(OnAdsSens);
            if (aimAssistToggle != null) aimAssistToggle.onValueChanged.RemoveListener(OnAimAssist);

            if (colorblindDropdown != null) colorblindDropdown.onValueChanged.RemoveListener(OnColorblind);
            if (highContrastToggle != null) highContrastToggle.onValueChanged.RemoveListener(OnHighContrast);
            if (reduceMotionToggle != null) reduceMotionToggle.onValueChanged.RemoveListener(OnReduceMotion);
            if (textSizeDropdown != null) textSizeDropdown.onValueChanged.RemoveListener(OnTextSize);
        }

        // ----- Dropdown population -----

        void PopulateColorblindDropdown()
        {
            colorblindDropdown.ClearOptions();
            colorblindDropdown.AddOptions(new List<string> { "None", "Protanopia", "Deuteranopia", "Tritanopia" });
        }

        void PopulateTextSizeDropdown()
        {
            textSizeDropdown.ClearOptions();
            textSizeDropdown.AddOptions(new List<string> { "Normal", "Large" });
        }

        int FrameRateIndexFor(int targetFps)
        {
            if (frameRateOptions == null) return 0;
            for (int i = 0; i < frameRateOptions.Length; i++)
                if (frameRateOptions[i] == targetFps) return i;
            return 0;
        }

        // ----- Refresh UI from service (no callbacks) -----

        void RefreshFromSettings()
        {
            suppressCallbacks = true;

            // Video
            if (fovSlider != null) fovSlider.value = SettingsService.FieldOfView;
            UpdateFovLabel(SettingsService.FieldOfView);
            if (qualityDropdown != null)
            {
                EnsureQualityOptions();
                qualityDropdown.SetValueWithoutNotify(Mathf.Clamp(SettingsService.QualityLevel, 0, Mathf.Max(0, qualityDropdown.options.Count - 1)));
            }
            if (vsyncToggle != null) vsyncToggle.SetIsOnWithoutNotify(SettingsService.VSync);
            if (targetFrameRateDropdown != null)
            {
                EnsureFrameRateOptions();
                targetFrameRateDropdown.SetValueWithoutNotify(FrameRateIndexFor(SettingsService.TargetFrameRate));
            }

            // Audio
            if (masterVolumeSlider != null) masterVolumeSlider.value = SettingsService.MasterVolume;
            if (musicVolumeSlider != null) musicVolumeSlider.value = SettingsService.MusicVolume;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = SettingsService.SfxVolume;
            if (voiceVolumeSlider != null) voiceVolumeSlider.value = SettingsService.VoiceVolume;
            if (spatialAudioToggle != null) spatialAudioToggle.SetIsOnWithoutNotify(SettingsService.SpatialAudio);
            if (subtitlesToggle != null) subtitlesToggle.SetIsOnWithoutNotify(SettingsService.Subtitles);

            // Controls
            if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = SettingsService.MouseSensitivity;
            UpdateLabel(mouseSensitivityValueLabel, SettingsService.MouseSensitivity);
            if (mouseSensitivityYSlider != null) mouseSensitivityYSlider.value = SettingsService.MouseSensitivityY;
            UpdateLabel(mouseSensitivityYValueLabel, SettingsService.MouseSensitivityY);
            if (adsSensMultiplierSlider != null) adsSensMultiplierSlider.value = SettingsService.AdsSensMultiplier;
            UpdateLabel(adsSensValueLabel, SettingsService.AdsSensMultiplier);
            if (aimAssistToggle != null) aimAssistToggle.SetIsOnWithoutNotify(SettingsService.AimAssist);

            // Accessibility
            if (colorblindDropdown != null) colorblindDropdown.SetValueWithoutNotify((int)SettingsService.Colorblind);
            if (highContrastToggle != null) highContrastToggle.SetIsOnWithoutNotify(SettingsService.HighContrastHud);
            if (reduceMotionToggle != null) reduceMotionToggle.SetIsOnWithoutNotify(SettingsService.ReduceMotion);
            if (textSizeDropdown != null) textSizeDropdown.SetValueWithoutNotify((int)SettingsService.TextScale);

            suppressCallbacks = false;
        }

        void EnsureQualityOptions()
        {
            // Reflect the project's real quality presets so the dropdown indices map 1:1 to QualityLevel.
            string[] names = QualitySettings.names;
            if (qualityDropdown.options.Count == names.Length) return;
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(names));
        }

        void EnsureFrameRateOptions()
        {
            if (frameRateOptions == null || frameRateOptions.Length == 0) return;
            if (targetFrameRateDropdown.options.Count == frameRateOptions.Length) return;
            List<string> labels = new List<string>(frameRateOptions.Length);
            for (int i = 0; i < frameRateOptions.Length; i++)
                labels.Add(frameRateOptions[i] < 0 ? "Uncapped" : frameRateOptions[i].ToString());
            targetFrameRateDropdown.ClearOptions();
            targetFrameRateDropdown.AddOptions(labels);
        }

        // ----- Video callbacks -----

        void OnFovChanged(float value)
        {
            if (suppressCallbacks) return;
            SettingsService.FieldOfView = value;
            UpdateFovLabel(SettingsService.FieldOfView);
            SettingsService.ApplyVideoSettings();
        }

        void OnQualityChanged(int index)
        {
            if (suppressCallbacks) return;
            SettingsService.QualityLevel = index;
            SettingsService.ApplyVideoSettings();
        }

        void OnVSyncChanged(bool on)
        {
            if (suppressCallbacks) return;
            SettingsService.VSync = on;
            SettingsService.ApplyVideoSettings();
        }

        void OnTargetFrameRateChanged(int index)
        {
            if (suppressCallbacks) return;
            if (frameRateOptions != null && index >= 0 && index < frameRateOptions.Length)
                SettingsService.TargetFrameRate = frameRateOptions[index];
            SettingsService.ApplyVideoSettings();
        }

        // ----- Audio callbacks -----

        void OnMasterVolume(float v) { if (!suppressCallbacks) SettingsService.MasterVolume = v; }
        void OnMusicVolume(float v) { if (!suppressCallbacks) SettingsService.MusicVolume = v; }
        void OnSfxVolume(float v) { if (!suppressCallbacks) SettingsService.SfxVolume = v; }
        void OnVoiceVolume(float v) { if (!suppressCallbacks) SettingsService.VoiceVolume = v; }
        void OnSpatialAudio(bool on) { if (!suppressCallbacks) SettingsService.SpatialAudio = on; }
        void OnSubtitles(bool on) { if (!suppressCallbacks) SettingsService.Subtitles = on; }

        // ----- Controls callbacks -----

        void OnMouseSensitivity(float v)
        {
            if (suppressCallbacks) return;
            SettingsService.MouseSensitivity = v;
            UpdateLabel(mouseSensitivityValueLabel, SettingsService.MouseSensitivity);
        }

        void OnMouseSensitivityY(float v)
        {
            if (suppressCallbacks) return;
            SettingsService.MouseSensitivityY = v;
            UpdateLabel(mouseSensitivityYValueLabel, SettingsService.MouseSensitivityY);
        }

        void OnAdsSens(float v)
        {
            if (suppressCallbacks) return;
            SettingsService.AdsSensMultiplier = v;
            UpdateLabel(adsSensValueLabel, SettingsService.AdsSensMultiplier);
        }

        void OnAimAssist(bool on) { if (!suppressCallbacks) SettingsService.AimAssist = on; }

        // ----- Accessibility callbacks -----

        void OnColorblind(int index)
        {
            if (suppressCallbacks) return;
            SettingsService.Colorblind = (ColorblindMode)Mathf.Clamp(index, 0, 3);
        }

        void OnHighContrast(bool on) { if (!suppressCallbacks) SettingsService.HighContrastHud = on; }
        void OnReduceMotion(bool on) { if (!suppressCallbacks) SettingsService.ReduceMotion = on; }

        void OnTextSize(int index)
        {
            if (suppressCallbacks) return;
            SettingsService.TextScale = (TextSize)Mathf.Clamp(index, 0, 1);
        }

        // ----- Labels -----

        void UpdateFovLabel(float fov)
        {
            if (fovValueLabel != null) fovValueLabel.text = Mathf.RoundToInt(fov) + "°";
        }

        static void UpdateLabel(TMP_Text label, float value)
        {
            if (label != null) label.text = value.ToString("0.00");
        }

        // ----- Tabs -----

        void OnVideoTab() { PlayClick(); ShowTab(videoPanel); }
        void OnAudioTab() { PlayClick(); ShowTab(audioPanel); }
        void OnControlsTab() { PlayClick(); ShowTab(controlsPanel); }
        void OnAccessibilityTab() { PlayClick(); ShowTab(accessibilityPanel); }

        void OnBack()
        {
            PlayClick();
            // Ensure video state is applied before leaving, then hide self.
            SettingsService.ApplyVideoSettings();
            gameObject.SetActive(false);
        }

        void ShowTab(GameObject panel)
        {
            if (videoPanel != null) videoPanel.SetActive(panel == videoPanel);
            if (audioPanel != null) audioPanel.SetActive(panel == audioPanel);
            if (controlsPanel != null) controlsPanel.SetActive(panel == controlsPanel);
            if (accessibilityPanel != null) accessibilityPanel.SetActive(panel == accessibilityPanel);
        }

        static void PlayClick()
        {
            CarrotClash.Audio.AudioManager.Instance?.PlayUi("ui_button_confirm");
        }
    }
}
