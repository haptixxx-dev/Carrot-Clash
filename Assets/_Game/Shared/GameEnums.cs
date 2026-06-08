namespace CarrotClash
{
    /// <summary>Team identity. Two teams per match. None used for neutral zones / unassigned.</summary>
    public enum Team
    {
        None = -1,
        A = 0,
        B = 1
    }

    /// <summary>The four playable hero classes.</summary>
    public enum ClassId
    {
        Carrot = 0,
        Jalapeno = 1,
        Broccoli = 2,
        Potato = 3
    }

    /// <summary>Momentum tiers. Index maps directly into <see cref="MomentumConfigSO.tiers"/>.</summary>
    public enum MomentumTier
    {
        Cold = 0,   // default / after death
        Warm = 1,   // 1 kill
        Hot = 2,    // 2 kills
        OnFire = 3  // 3+ kills
    }

    /// <summary>Capture zone identity. C unlocks at minute 5.</summary>
    public enum ZoneId
    {
        A = 0, // Courtyard
        B = 1, // Indoor Market
        C = 2  // Central Stage
    }

    /// <summary>Match state machine states. See Core Loop doc.</summary>
    public enum MatchState
    {
        Lobby = 0,
        ClassSelect = 1,
        Countdown = 2,
        MatchActive = 3,
        SuddenDeath = 4,
        MatchEnd = 5,
        PostMatch = 6
    }

    /// <summary>Two active ability slots per class.</summary>
    public enum AbilitySlot
    {
        Active1 = 0,
        Active2 = 1
    }

    /// <summary>Damage source classification. Drives feedback (hit-marker tint) and effect routing.</summary>
    public enum DamageType
    {
        Bullet = 0,
        Fire = 1,
        Ability = 2,
        Explosion = 3,
        Fall = 4
    }

    /// <summary>Camera-shake profiles. See Game Feel doc screen-shake table.</summary>
    public enum ShakeType
    {
        Random = 0,
        Directional = 1,
        Radial = 2,
        Downward = 3
    }

    /// <summary>Footstep surface categories, mapped from PhysicMaterial names by <see cref="CarrotClash.Audio.FootstepBank"/>.</summary>
    public enum SurfaceType
    {
        Stone = 0,  // Courtyard
        Wood = 1,   // Market floor
        Metal = 2,  // Catwalk grating
        Dirt = 3    // Underground cellar
    }

    /// <summary>Bot brain skill levels. See Tech Architecture bot table.</summary>
    public enum BotDifficulty
    {
        Dummy = 0,
        Easy = 1,
        Medium = 2,
        Hard = 3
    }

    /// <summary>High-level bot finite-state-machine states.</summary>
    public enum BotState
    {
        Idle = 0,
        MoveToObjective = 1,
        Capture = 2,
        FightNearbyEnemy = 3,
        Retreat = 4
    }

    /// <summary>Match outcome from the local player's perspective.</summary>
    public enum MatchResult
    {
        Win = 0,
        Loss = 1,
        Draw = 2
    }
}
