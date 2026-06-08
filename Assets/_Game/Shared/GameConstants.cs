using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Central tuning constants pulled directly from the design docs. Anything a designer would
    /// reasonably want to tweak per-asset lives on a ScriptableObject instead; these are the
    /// match-wide invariants and feel timings that the whole codebase agrees on.
    /// </summary>
    public static class GameConstants
    {
        // ----- Match (Core Loop doc) -----
        public const float MatchDuration = 480f;       // 8 minutes
        public const float SuddenDeathDuration = 60f;
        public const float CountdownDuration = 5f;
        public const float ClassSelectDuration = 30f;
        public const float PostMatchDuration = 15f;
        public const int ScoreCap = 500;
        public const float ZoneCUnlockTime = 300f;     // minute 5, measured from match start

        // ----- Teams -----
        public const int TeamCount = 2;
        public const int MaxPlayersPerTeam = 4;
        public const int MaxPlayers = TeamCount * MaxPlayersPerTeam;

        // ----- Respawn (Core Loop doc) -----
        public const float RespawnTime = 4f;
        public const float SpawnInvulnerability = 2f;
        public const float SpawnContestRadius = 10f;   // enemy within this of a spawn => redirect
        public const float SafeTeammateRadius = 30f;

        // ----- Scoring (Core Loop doc score table) -----
        public const int ScoreZoneTickAB = 1;          // per second, full capture
        public const int ScoreZoneTickC = 2;           // per second, after unlock
        public const int ScoreCaptureNeutral = 10;     // one-time, neutral -> team
        public const int ScoreCaptureContested = 15;   // one-time, contested -> team
        public const int ScoreKill = 5;
        public const int ScoreKillOnFire = 8;          // replaces ScoreKill vs a tier-3 victim
        public const int ScoreAssist = 2;

        // ----- Combat (Weapons doc) -----
        public const float AssistWindow = 5f;          // last 5s of damage counts as an assist
        public const float AmmoRefillDelay = 3f;       // reserve refills this long after a reload
        public const float WeaponSwapTime = 0.2f;
        public const float CrosshairRecovery = 0.2f;   // spread recovers 200ms after a shot
        public const float AdsTime = 0.15f;
        public const float MaxLagCompensation = 0.2f;  // 200ms hitscan rewind cap

        // Damage falloff curve (Weapons doc). Multiplier applied to body damage beyond effective range.
        public const float FalloffStep5m = 0.90f;      // +5m  beyond effective range
        public const float FalloffStep10m = 0.75f;     // +10m
        public const float FalloffStep15m = 0.55f;     // +15m
        public const float FalloffStep20m = 0.40f;     // +20m and beyond (floor)
        public const float ShotgunFalloffHalfRange = 3f; // shotgun pellets: 50% at +3m beyond effective

        // ----- Status effects (Ability Interactions doc) -----
        public const float SlowAmount = 0.35f;         // Spice Burst: -35% move speed
        public const float SlowDuration = 3f;
        public const float KnockbackDistance = 6f;     // Earthen Slam displacement
        public const float KnockbackDuration = 0.25f;
        public const float KnockbackElevation = 15f;   // degrees of upward bounce
        public const float StaggerDuration = 0.5f;     // can't fire / use abilities
        public const float FireTickInterval = 0.1f;    // DamageZone OnTriggerStay cadence
        public const int StarchArmorAmount = 40;
        public const float StarchArmorDuration = 4f;
        public const float RegenAuraTick = 1f;         // heal cadence
        public const int RegenAuraHeal = 2;            // HP per tick
        public const float RegenAuraRadius = 8f;
        public const int LowHealthThreshold = 30;      // vignette + heartbeat below this

        // ----- Momentum (Momentum System doc) -----
        public const float MomentumDecayInterval = 12f;
        public const float MomentumTransferOnDeath = 0.5f;   // 50% of charge to killer
        public const float MomentumTransferPotato = 0.25f;   // Potato Stubborn Root override
        public const float JalapenoDecayExtension = 5f;      // Extended Streak per kill
        public const float BroccoliHarvestShare = 0.1f;      // Shared Harvest fraction
        public const float BroccoliHarvestRadius = 10f;
        public const float OnFireOnFireDecayBonus = 8f;      // tier-3 kills tier-3 while capped
        public const float AssistChargeFraction = 0.25f;
        public const int MaxTier = 3;

        // ----- Movement / camera feel (Game Feel doc) -----
        public const float DefaultFov = 90f;
        public const float SprintFov = 96f;
        public const float AdsFov = 85f;
        public const float SprintFovTime = 0.2f;
        public const float DashFovSurge = 10f;
        public const float CrouchHeightDelta = 0.4f;
        public const float CrouchTransitionTime = 0.15f;
        public const float Gravity = -22f;             // tuned arcade gravity, not -9.81
        public const float TerminalVelocity = -50f;

        // ----- Carrot-specific (Characters doc) -----
        public const float SilentStepsSpeedFraction = 0.5f;  // < 50% sprint => silent
        public const float CarrotBackstabDot = -0.1f;        // dot < this => rear arc

        // ----- Audio -----
        public const int AudioSourcePoolSize = 32;
        public const float AudioMaxDistance = 25f;

        // ----- Layers (set up in project; names canonical here) -----
        public const string LayerPlayer = "Player";
        public const string LayerEnvironment = "Environment";
        public const string LayerAbility = "Ability";
        public const string LayerHitbox = "Hitbox";

        // ----- Scene names -----
        public const string SceneBoot = "Boot";
        public const string SceneMainMenu = "MainMenu";
        public const string SceneGameplay = "Gameplay_Market";
        public const string SceneGameplayUI = "GameplayUI";
        public const string SceneTutorial = "TutorialScene";

        /// <summary>Returns true once the C zone is contestable for the given match-elapsed time.</summary>
        public static bool IsZoneCUnlocked(float elapsedTime) => elapsedTime >= ZoneCUnlockTime;

        /// <summary>The opposing team, or None if given None.</summary>
        public static Team Opponent(Team t) => t switch
        {
            Team.A => Team.B,
            Team.B => Team.A,
            _ => Team.None
        };
    }
}
