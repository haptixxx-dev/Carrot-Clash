using System;
using UnityEngine;

namespace CarrotClash
{
    public enum ColorblindMode { None = 0, Protanopia = 1, Deuteranopia = 2, Tritanopia = 3 }
    public enum TextSize { Normal = 0, Large = 1 }

    /// <summary>
    /// Centralised, persisted player settings (PlayerPrefs-backed). Read statically from anywhere
    /// (camera shake gating, FOV, sensitivity, accessibility). Raises <see cref="OnSettingsChanged"/>
    /// so live systems (HUD, audio mixer, camera) can react without polling.
    /// </summary>
    public static class SettingsService
    {
        public static event Action OnSettingsChanged;

        // ----- Video -----
        public static float FieldOfView { get => GetF("fov", GameConstants.DefaultFov); set => SetF("fov", Mathf.Clamp(value, 80f, 110f)); }
        public static int TargetFrameRate { get => GetI("targetFps", 120); set => SetI("targetFps", value); }
        public static bool VSync { get => GetB("vsync", false); set => SetB("vsync", value); }
        public static int QualityLevel { get => GetI("quality", 2); set => SetI("quality", value); }

        // ----- Audio (0..1) -----
        public static float MasterVolume { get => GetF("vol_master", 1f); set => SetF("vol_master", Mathf.Clamp01(value)); }
        public static float MusicVolume { get => GetF("vol_music", 0.8f); set => SetF("vol_music", Mathf.Clamp01(value)); }
        public static float SfxVolume { get => GetF("vol_sfx", 1f); set => SetF("vol_sfx", Mathf.Clamp01(value)); }
        public static float VoiceVolume { get => GetF("vol_voice", 1f); set => SetF("vol_voice", Mathf.Clamp01(value)); }
        public static bool SpatialAudio { get => GetB("spatial", true); set => SetB("spatial", value); }
        public static bool Subtitles { get => GetB("subtitles", false); set => SetB("subtitles", value); }

        // ----- Controls -----
        public static float MouseSensitivity { get => GetF("sens", 1f); set => SetF("sens", Mathf.Max(0.05f, value)); }
        public static float MouseSensitivityY { get => GetF("sens_y", 1f); set => SetF("sens_y", Mathf.Max(0.05f, value)); }
        public static float AdsSensMultiplier { get => GetF("ads_sens", 1f); set => SetF("ads_sens", value); }
        public static bool AimAssist { get => GetB("aim_assist", true); set => SetB("aim_assist", value); }

        // ----- Accessibility -----
        public static bool ReduceMotion { get => GetB("reduce_motion", false); set => SetB("reduce_motion", value); }
        public static bool HighContrastHud { get => GetB("high_contrast", false); set => SetB("high_contrast", value); }
        public static ColorblindMode Colorblind { get => (ColorblindMode)GetI("colorblind", 0); set => SetI("colorblind", (int)value); }
        public static TextSize TextScale { get => (TextSize)GetI("text_size", 0); set => SetI("text_size", (int)value); }

        // ----- Backing helpers -----
        static float GetF(string k, float d) => PlayerPrefs.GetFloat("cc_" + k, d);
        static int GetI(string k, int d) => PlayerPrefs.GetInt("cc_" + k, d);
        static bool GetB(string k, bool d) => PlayerPrefs.GetInt("cc_" + k, d ? 1 : 0) != 0;
        static void SetF(string k, float v) { PlayerPrefs.SetFloat("cc_" + k, v); Dirty(); }
        static void SetI(string k, int v) { PlayerPrefs.SetInt("cc_" + k, v); Dirty(); }
        static void SetB(string k, bool v) { PlayerPrefs.SetInt("cc_" + k, v ? 1 : 0); Dirty(); }

        static void Dirty()
        {
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>Apply video settings that need engine calls (frame rate, vsync, quality).</summary>
        public static void ApplyVideoSettings()
        {
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync ? -1 : TargetFrameRate;
            QualitySettings.SetQualityLevel(Mathf.Clamp(QualityLevel, 0, QualitySettings.names.Length - 1), true);
        }
    }
}
