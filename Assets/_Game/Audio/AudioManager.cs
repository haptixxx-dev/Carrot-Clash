using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace CarrotClash.Audio
{
    /// <summary>
    /// Pooled audio playback singleton (Audio doc). Plays 3D one-shots and 2D UI cues from a
    /// keyed <see cref="AudioLibrary"/>, manages looping sources (ability loops, ambient), and
    /// drives the music layer mix via exposed AudioMixer parameters. Survives scene loads.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Mixer")]
        [SerializeField] AudioMixer mixer;
        [SerializeField] AudioMixerGroup sfxGroup;
        [SerializeField] AudioMixerGroup uiGroup;
        [SerializeField] AudioMixerGroup musicGroup;
        [SerializeField] AudioMixerGroup ambienceGroup;

        [Header("Library")]
        [SerializeField] AudioLibrary library;

        [Header("Pool")]
        [SerializeField] int poolSize = GameConstants.AudioSourcePoolSize;

        readonly List<AudioSource> pool = new List<AudioSource>();
        readonly Dictionary<string, AudioSource> loops = new Dictionary<string, AudioSource>();

        // Music layers.
        AudioSource musicBase, musicIntensity, musicTier3;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildPool();
            BuildMusicSources();
            SettingsService.OnSettingsChanged += ApplyVolumes;
            ApplyVolumes();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            SettingsService.OnSettingsChanged -= ApplyVolumes;
        }

        void BuildPool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject($"PooledAudio_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.outputAudioMixerGroup = sfxGroup;
                pool.Add(src);
            }
        }

        void BuildMusicSources()
        {
            musicBase = CreateMusicSource("MusicBase");
            musicIntensity = CreateMusicSource("MusicIntensity");
            musicTier3 = CreateMusicSource("MusicTier3");
        }

        AudioSource CreateMusicSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            src.outputAudioMixerGroup = musicGroup;
            return src;
        }

        AudioSource GetFreeSource()
        {
            for (int i = 0; i < pool.Count; i++)
                if (!pool[i].isPlaying) return pool[i];
            // All busy: steal the first (oldest).
            return pool.Count > 0 ? pool[0] : null;
        }

        /// <summary>Play a 3D positional one-shot by key.</summary>
        public void PlaySfx(string key, Vector3 position, float volumeScale = 1f)
        {
            if (library == null) return;
            var clip = library.Resolve(key);
            if (clip == null) return;
            var src = GetFreeSource();
            if (src == null) return;
            src.transform.position = position;
            src.clip = clip;
            src.outputAudioMixerGroup = sfxGroup;
            src.spatialBlend = SettingsService.SpatialAudio ? 1f : 0f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.maxDistance = GameConstants.AudioMaxDistance;
            src.volume = volumeScale;
            src.pitch = 1f;
            src.Play();
        }

        /// <summary>Play a 2D UI / HUD cue by key (no spatialisation).</summary>
        public void PlayUi(string key, float volumeScale = 1f)
        {
            if (library == null) return;
            var clip = library.Resolve(key);
            if (clip == null) return;
            var src = GetFreeSource();
            if (src == null) return;
            src.clip = clip;
            src.outputAudioMixerGroup = uiGroup;
            src.spatialBlend = 0f;
            src.volume = volumeScale;
            src.pitch = 1f;
            src.Play();
        }

        /// <summary>Start a named looping source (ability loop, ambient bed). Idempotent per key.</summary>
        public void StartLoop(string key, Vector3 position, bool spatial = true)
        {
            if (library == null || loops.ContainsKey(key)) return;
            var clip = library.Resolve(key);
            if (clip == null) return;
            var go = new GameObject($"Loop_{key}");
            go.transform.SetParent(transform);
            go.transform.position = position;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.spatialBlend = spatial && SettingsService.SpatialAudio ? 1f : 0f;
            src.outputAudioMixerGroup = spatial ? sfxGroup : ambienceGroup;
            src.maxDistance = GameConstants.AudioMaxDistance;
            src.Play();
            loops[key] = src;
        }

        public void StopLoop(string key)
        {
            if (loops.TryGetValue(key, out var src) && src != null)
            {
                src.Stop();
                Destroy(src.gameObject);
            }
            loops.Remove(key);
        }

        // ----- Music control (driven by GameModeManager / MomentumController) -----
        public void PlayMusic(AudioClip baseLayer, AudioClip intensityLayer, AudioClip tier3Layer)
        {
            SetLayer(musicBase, baseLayer, 1f);
            SetLayer(musicIntensity, intensityLayer, 0f);
            SetLayer(musicTier3, tier3Layer, 0f);
        }

        void SetLayer(AudioSource src, AudioClip clip, float startVol)
        {
            if (src == null) return;
            src.clip = clip;
            src.volume = startVol;
            if (clip != null) src.Play();
        }

        /// <summary>0..1 cross-fade of the intensity layer (last 90s / Zone C contested).</summary>
        public void SetMusicParameter(string param, float value)
        {
            value = Mathf.Clamp01(value);
            switch (param)
            {
                case "intensity": if (musicIntensity != null) musicIntensity.volume = value; break;
                case "tier3": if (musicTier3 != null) musicTier3.volume = value; break;
            }
            if (mixer != null) mixer.SetFloat(param, Mathf.Lerp(-80f, 0f, value));
        }

        public void StopMusic()
        {
            musicBase?.Stop(); musicIntensity?.Stop(); musicTier3?.Stop();
        }

        void ApplyVolumes()
        {
            if (mixer == null) return;
            mixer.SetFloat("MasterVolume", LinearToDb(SettingsService.MasterVolume));
            mixer.SetFloat("MusicVolume", LinearToDb(SettingsService.MusicVolume));
            mixer.SetFloat("SfxVolume", LinearToDb(SettingsService.SfxVolume));
            mixer.SetFloat("VoiceVolume", LinearToDb(SettingsService.VoiceVolume));
        }

        static float LinearToDb(float linear) => linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
    }
}
