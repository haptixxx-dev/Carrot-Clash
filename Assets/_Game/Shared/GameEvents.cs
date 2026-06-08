using System;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Carries the full context of an elimination so listeners (momentum, scoring, kill feed,
    /// challenges, audio) can each react without coupling to one another.
    /// </summary>
    public readonly struct KillEvent
    {
        public readonly PlayerController Killer;     // may be null (environment / suicide)
        public readonly PlayerController Victim;
        public readonly DamageType KillingBlowType;
        public readonly bool IsBackstab;             // Carrot rear-arc kill
        public readonly MomentumTier VictimTier;     // victim tier at moment of death
        public readonly bool IsHeadshot;

        public KillEvent(PlayerController killer, PlayerController victim, DamageType killingBlowType,
                         bool isBackstab, MomentumTier victimTier, bool isHeadshot)
        {
            Killer = killer;
            Victim = victim;
            KillingBlowType = killingBlowType;
            IsBackstab = isBackstab;
            VictimTier = victimTier;
            IsHeadshot = isHeadshot;
        }
    }

    /// <summary>Fired when a zone changes ownership (one-time capture event, distinct from per-second ticks).</summary>
    public readonly struct ZoneCaptureEvent
    {
        public readonly ZoneId Zone;
        public readonly Team NewOwner;
        public readonly Team PreviousOwner;
        public readonly bool WasContested;   // contested -> team (worth more) vs neutral -> team

        public ZoneCaptureEvent(ZoneId zone, Team newOwner, Team previousOwner, bool wasContested)
        {
            Zone = zone;
            NewOwner = newOwner;
            PreviousOwner = previousOwner;
            WasContested = wasContested;
        }
    }

    /// <summary>
    /// Global, statically-accessible event hub. Decouples cross-cutting systems (UI, audio,
    /// challenges, analytics) from gameplay producers. Gameplay code raises; presentation listens.
    ///
    /// IMPORTANT: handlers must unsubscribe in OnDisable/OnDestroy — these are static and survive
    /// scene reloads. <see cref="Clear"/> is called on match teardown as a safety net.
    /// </summary>
    public static class GameEvents
    {
        // ----- Combat -----
        public static event Action<KillEvent> OnKill;
        public static event Action<PlayerController, DamageInfo, DamageResult> OnDamageDealt;
        public static event Action<PlayerController> OnPlayerSpawned;
        public static event Action<PlayerController> OnPlayerDied;
        public static event Action<PlayerController, MomentumTier, MomentumTier> OnTierChanged; // player, old, new
        public static event Action<PlayerController, int> OnAssist;   // player credited, victim count irrelevant here

        // ----- Objectives -----
        public static event Action<ZoneCaptureEvent> OnZoneCaptured;
        public static event Action<ZoneId, float> OnZoneProgressChanged; // zone, 0..1
        public static event Action OnZoneCUnlocked;

        // ----- Match flow -----
        public static event Action<MatchState, MatchState> OnMatchStateChanged; // old, new
        public static event Action<int, int> OnScoreChanged;          // teamId, newTotal
        public static event Action<float> OnMatchTimerTick;           // remaining seconds
        public static event Action<Team> OnMatchEnded;                // winning team (None = draw)

        // ----- Raisers (null-safe) -----
        public static void RaiseKill(in KillEvent e) => OnKill?.Invoke(e);
        public static void RaiseDamageDealt(PlayerController victim, in DamageInfo info, in DamageResult result)
            => OnDamageDealt?.Invoke(victim, info, result);
        public static void RaisePlayerSpawned(PlayerController p) => OnPlayerSpawned?.Invoke(p);
        public static void RaisePlayerDied(PlayerController p) => OnPlayerDied?.Invoke(p);
        public static void RaiseTierChanged(PlayerController p, MomentumTier oldT, MomentumTier newT)
            => OnTierChanged?.Invoke(p, oldT, newT);
        public static void RaiseAssist(PlayerController p, int amount) => OnAssist?.Invoke(p, amount);

        public static void RaiseZoneCaptured(in ZoneCaptureEvent e) => OnZoneCaptured?.Invoke(e);
        public static void RaiseZoneProgress(ZoneId zone, float progress) => OnZoneProgressChanged?.Invoke(zone, progress);
        public static void RaiseZoneCUnlocked() => OnZoneCUnlocked?.Invoke();

        public static void RaiseMatchStateChanged(MatchState oldS, MatchState newS)
            => OnMatchStateChanged?.Invoke(oldS, newS);
        public static void RaiseScoreChanged(int teamId, int total) => OnScoreChanged?.Invoke(teamId, total);
        public static void RaiseMatchTimerTick(float remaining) => OnMatchTimerTick?.Invoke(remaining);
        public static void RaiseMatchEnded(Team winner) => OnMatchEnded?.Invoke(winner);

        /// <summary>Detach all listeners. Call on match teardown to prevent leaks across reloads.</summary>
        public static void Clear()
        {
            OnKill = null;
            OnDamageDealt = null;
            OnPlayerSpawned = null;
            OnPlayerDied = null;
            OnTierChanged = null;
            OnAssist = null;
            OnZoneCaptured = null;
            OnZoneProgressChanged = null;
            OnZoneCUnlocked = null;
            OnMatchStateChanged = null;
            OnScoreChanged = null;
            OnMatchTimerTick = null;
            OnMatchEnded = null;
        }
    }
}
