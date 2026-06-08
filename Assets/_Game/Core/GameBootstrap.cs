using UnityEngine;
using CarrotClash.Audio;

namespace CarrotClash
{
    /// <summary>
    /// Entry point for the Boot scene (Tech Architecture scene structure). Applies persisted video
    /// settings, guarantees a persistent <see cref="AudioManager"/> exists, then hands off to the
    /// main menu via <see cref="SceneFlow"/>. Lightweight: no gameplay objects live in Boot.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Persistent prefabs")]
        [Tooltip("Spawned (DontDestroyOnLoad inside AudioManager) if no AudioManager already exists in the scene.")]
        [SerializeField] AudioManager audioManagerPrefab;

        [Header("Flow")]
        [Tooltip("If true, transition to the main menu automatically on Start.")]
        [SerializeField] bool autoLoadMainMenu = true;

        void Start()
        {
            SettingsService.ApplyVideoSettings();
            EnsureAudioManager();

            if (autoLoadMainMenu)
                SceneFlow.LoadMainMenu();
        }

        void EnsureAudioManager()
        {
            if (AudioManager.Instance != null) return;

            if (audioManagerPrefab != null)
            {
                // AudioManager.Awake calls DontDestroyOnLoad on itself.
                Instantiate(audioManagerPrefab);
                return;
            }

            // No prefab assigned: a bare AudioManager still initialises its pool and survives loads,
            // it simply has no mixer/library wired (PlaySfx/PlayUi will no-op until configured).
            var go = new GameObject("AudioManager");
            go.AddComponent<AudioManager>();
        }
    }
}
