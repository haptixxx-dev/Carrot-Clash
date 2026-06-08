using UnityEngine;

namespace CarrotClash.Audio
{
    /// <summary>
    /// Orchestrates the three-layer dynamic music mix (Audio doc — Music Architecture):
    /// <list type="bullet">
    /// <item><b>Base</b> — always playing during the match.</item>
    /// <item><b>Intensity</b> — crossfades in over the last 90s, or while Zone C is contested.</item>
    /// <item><b>Tier3</b> — the local player's personal "power" layer, on only at momentum tier 3.</item>
    /// </list>
    /// Layer levels are pushed to <see cref="AudioManager.SetMusicParameter"/>; this component owns
    /// the *targets* and smooths the actual mixer values toward them for soft crossfades.
    /// </summary>
    [DisallowMultipleComponent]
    public class MusicDirector : MonoBehaviour
    {
        [Header("Music layer clips")]
        [SerializeField] AudioClip baseLayer;
        [SerializeField] AudioClip intensityLayer;
        [SerializeField] AudioClip tier3Layer;

        [Header("Timing")]
        [Tooltip("Seconds remaining at which the intensity layer begins to rise.")]
        [SerializeField] float intensityRampWindow = 90f;
        [Tooltip("Crossfade speed for the intensity layer (units per second toward target).")]
        [SerializeField] float intensityFadeSpeed = 0.2f;   // ~5s full crossfade (Audio doc)
        [Tooltip("Crossfade speed for the personal tier-3 layer.")]
        [SerializeField] float tier3FadeSpeed = 1.5f;

        [Header("Behaviour")]
        [Tooltip("Start the music automatically when the match enters its active state.")]
        [SerializeField] bool autoStartOnMatchActive = true;

        // Local player tracking (for the personal tier-3 layer).
        PlayerController localPlayer;

        // Layer targets (0..1) and smoothed current values.
        float intensityTarget;
        float intensityCurrent;
        float tier3Target;
        float tier3Current;

        // Drivers for the intensity layer (logical OR of contributing conditions).
        bool timerIntensity;       // last 90s of the match
        bool zoneCContested;       // Zone C currently being fought over

        bool musicStarted;

        void OnEnable()
        {
            GameEvents.OnPlayerSpawned += HandlePlayerSpawned;
            GameEvents.OnMatchStateChanged += HandleMatchStateChanged;
            GameEvents.OnMatchTimerTick += HandleTimerTick;
            GameEvents.OnTierChanged += HandleTierChanged;
            GameEvents.OnZoneCaptured += HandleZoneCaptured;
            GameEvents.OnZoneProgressChanged += HandleZoneProgress;
            GameEvents.OnMatchEnded += HandleMatchEnded;
        }

        void OnDisable()
        {
            GameEvents.OnPlayerSpawned -= HandlePlayerSpawned;
            GameEvents.OnMatchStateChanged -= HandleMatchStateChanged;
            GameEvents.OnMatchTimerTick -= HandleTimerTick;
            GameEvents.OnTierChanged -= HandleTierChanged;
            GameEvents.OnZoneCaptured -= HandleZoneCaptured;
            GameEvents.OnZoneProgressChanged -= HandleZoneProgress;
            GameEvents.OnMatchEnded -= HandleMatchEnded;
        }

        void Update()
        {
            AudioManager am = AudioManager.Instance;
            if (am == null || !musicStarted) return;

            float desiredIntensity = (timerIntensity || zoneCContested) ? 1f : 0f;
            intensityTarget = desiredIntensity;

            if (!Mathf.Approximately(intensityCurrent, intensityTarget))
            {
                intensityCurrent = Mathf.MoveTowards(intensityCurrent, intensityTarget,
                    intensityFadeSpeed * Time.deltaTime);
                am.SetMusicParameter("intensity", intensityCurrent);
            }

            if (!Mathf.Approximately(tier3Current, tier3Target))
            {
                tier3Current = Mathf.MoveTowards(tier3Current, tier3Target,
                    tier3FadeSpeed * Time.deltaTime);
                am.SetMusicParameter("tier3", tier3Current);
            }
        }

        // ----- Match flow -----

        void HandleMatchStateChanged(MatchState oldState, MatchState newState)
        {
            if (autoStartOnMatchActive && newState == MatchState.MatchActive && !musicStarted)
                StartMatchMusic();

            if (newState == MatchState.MatchEnd || newState == MatchState.PostMatch)
                StopMatchMusic();
        }

        void HandleMatchEnded(Team winner) => StopMatchMusic();

        /// <summary>Begin the layered match track. Base on, intensity + tier3 silent until cued.</summary>
        public void StartMatchMusic()
        {
            AudioManager am = AudioManager.Instance;
            if (am == null) return;

            am.PlayMusic(baseLayer, intensityLayer, tier3Layer);

            intensityTarget = 0f;
            intensityCurrent = 0f;
            tier3Target = 0f;
            tier3Current = 0f;
            timerIntensity = false;
            zoneCContested = false;
            am.SetMusicParameter("intensity", 0f);
            am.SetMusicParameter("tier3", 0f);

            musicStarted = true;
        }

        /// <summary>Stop all layers — Post-match plays in clean audio space (Audio doc).</summary>
        public void StopMatchMusic()
        {
            if (!musicStarted) return;
            AudioManager.Instance?.StopMusic();
            musicStarted = false;
            intensityCurrent = 0f;
            tier3Current = 0f;
        }

        void HandleTimerTick(float remainingSeconds)
        {
            timerIntensity = remainingSeconds <= intensityRampWindow;
        }

        // ----- Local player / tier-3 personal layer -----

        void HandlePlayerSpawned(PlayerController player)
        {
            if (player != null && player.IsLocal)
                localPlayer = player;
        }

        void HandleTierChanged(PlayerController player, MomentumTier oldTier, MomentumTier newTier)
        {
            // Only the local player hears their personal tier-3 layer.
            if (player == null || !player.IsLocal) return;
            if (localPlayer == null) localPlayer = player;
            tier3Target = newTier == MomentumTier.OnFire ? 1f : 0f;
        }

        // ----- Zone C contested -> intensity -----

        void HandleZoneProgress(ZoneId zone, float progress)
        {
            if (zone != ZoneId.C) return;
            // Mid-capture progress (neither neutral nor fully owned) signals an active contest.
            zoneCContested = progress > 0.05f && progress < 0.95f;
        }

        void HandleZoneCaptured(ZoneCaptureEvent e)
        {
            // Once C resolves to an owner, the contest is over for music purposes.
            if (e.Zone == ZoneId.C) zoneCContested = false;
        }
    }
}
