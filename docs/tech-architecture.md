# Technical Architecture

Unity 6, Universal Render Pipeline, Netcode for GameObjects (NGO).

---

## Scene Structure

```
Boot               (lightweight — loads config, authenticates, transitions to Menu)
MainMenu           (lobby browser, class preview, settings)
Gameplay_Market    (the map — all gameplay lives here)
```

**Additive loading:** `Gameplay_Market` additively loads a `GameplayUI` scene so HUD is decoupled from map geometry.

---

## Folder Layout (Assets/)

```
Assets/
├── _Game/
│   ├── Characters/
│   │   ├── Data/          ← CharacterDataSO per class
│   │   ├── Abilities/     ← AbilityDataSO + MonoBehaviour implementations
│   │   └── Prefabs/       ← Player prefab variants per class
│   ├── Weapons/
│   │   ├── Data/          ← WeaponDataSO per weapon
│   │   └── Prefabs/
│   ├── Gameplay/
│   │   ├── Objectives/    ← CaptureZone, GameModeManager
│   │   ├── Momentum/      ← MomentumController, MomentumConfig SO
│   │   └── Map/           ← Zone unlock logic, spawn managers
│   ├── Network/           ← NGO-specific components
│   ├── UI/                ← HUD, menus, post-match
│   ├── Audio/             ← AudioManager, SFX banks
│   └── Shared/            ← Events, extensions, constants
├── EasyRoads3D/           (existing)
├── Graphy - Ultimate Stats Monitor/ (existing)
└── SlimUI/                (existing)
```

---

## ScriptableObjects (Data Layer)

### `CharacterDataSO`
```csharp
public class CharacterDataSO : ScriptableObject
{
    public string characterName;
    public int baseHP;           // 90 / 100 / 110 / 140
    public float baseMoveSpeed;  // 7.5 / 6.5 / 6.0 / 5.0 m/s
    public WeaponDataSO primaryWeapon;
    public AbilityDataSO active1;
    public AbilityDataSO active2;
    public AbilityDataSO passive;
    public AbilityDataSO momentumPassive;
    public Color primaryColor;   // used for momentum VFX tint
}
```

### `WeaponDataSO`
```csharp
public class WeaponDataSO : ScriptableObject
{
    public int damageBody;
    public float headshotMultiplier;
    public float fireRateRPM;
    public int magazineSize;
    public float reloadTime;
    public float effectiveRange;
    public float falloffCurveStart; // metres where falloff begins
    public AnimationCurve falloffCurve;
    public bool isBurst;
    public int burstCount;
    public float burstDelay;
}
```

### `MomentumConfigSO`
```csharp
public class MomentumConfigSO : ScriptableObject
{
    public float decayIntervalSeconds;   // 12s default
    public float decayExtensionOnKill;   // 0s (Jalapeño passive adds 5s)
    public float transferOnDeath;        // 0.5f (50%)
    public MomentumTierData[] tiers;     // array index = tier (0–3)
}

[Serializable]
public struct MomentumTierData
{
    public float moveSpeedBonus;         // 0 / 0.10 / 0.20 / 0.30
    public float cooldownReduction;      // 0 / 0.10 / 0.20 / 0.30
    public float damageBonus;            // 0 / 0    / 0.10 / 0.20
    public int teamScoreKillValue;       // 5 / 5    / 5    / 8
    public Color vfxColor;
}
```

---

## Core MonoBehaviours

### `PlayerController`
Top-level orchestrator. Holds references, wires input events to subsystems.

```csharp
public class PlayerController : MonoBehaviour
{
    [Header("Subsystems")]
    public PlayerMovement movement;
    public PlayerCamera cam;
    public WeaponController weapon;
    public AbilityController abilities;
    public HealthController health;
    public MomentumController momentum;
    public PlayerHUD hud; // null on remote players

    void Awake()
    {
        // bind InputSystem events
        // subscribe health.OnDeath → momentum.HandleDeath
        // subscribe momentum.OnTierChanged → hud.UpdateMomentumRing
    }
}
```

### `PlayerMovement`
Wraps `CharacterController`. Applies momentum speed bonus from `MomentumController`.

```csharp
public class PlayerMovement : MonoBehaviour
{
    CharacterController cc;
    float baseSpeed;       // from CharacterDataSO
    float currentSpeed;    // baseSpeed * momentum.SpeedMultiplier

    public void Dash(Vector3 direction, float force) { ... }
    // Called by AbilityController when Sprint Dash is activated
}
```

### `MomentumController`
Fully self-contained tier tracking. No dependency on networking — sync is handled by `NetworkedMomentumController`.

```csharp
public class MomentumController : MonoBehaviour
{
    public event Action<int> OnTierChanged;

    int tier;                    // 0–3
    float decayTimer;
    MomentumConfigSO config;

    public void RegisterKill(bool isFlanking = false)
    public void RegisterAssist()
    public void HandleDeath(MomentumController killer)
    public float SpeedMultiplier => config.tiers[tier].moveSpeedBonus + 1f;
    public float CooldownMultiplier => 1f - config.tiers[tier].cooldownReduction;
    public float DamageMultiplier => config.tiers[tier].damageBonus + 1f;

    // Decay coroutine runs while tier > 0
    IEnumerator DecayLoop() { ... }
}
```

### `AbilityController`
Manages two active slots + passive. Cooldowns respect `MomentumController.CooldownMultiplier`.

```csharp
public class AbilityController : MonoBehaviour
{
    AbilityBase active1, active2;
    float[] cooldownTimers = new float[2];

    public void ActivateAbility(int slot) // slot 0 or 1
    {
        if (cooldownTimers[slot] > 0) return;
        abilities[slot].Activate(owner);
        cooldownTimers[slot] = abilities[slot].Data.cooldown
                               * momentum.CooldownMultiplier;
    }
}
```

### `AbilityBase`
Abstract base for all ability implementations.

```csharp
public abstract class AbilityBase : MonoBehaviour
{
    public AbilityDataSO Data;
    protected PlayerController owner;

    public abstract void Activate(PlayerController activator);
    public virtual void Cancel() { }
}
```

**Concrete implementations:**
- `Ability_SprintDash : AbilityBase` — calls `owner.movement.Dash()`
- `Ability_RadarPulse : AbilityBase` — Physics.OverlapSphere + reveal effect
- `Ability_SpiceBurst : AbilityBase` — SphereCast + `SlowEffect.Apply()`
- `Ability_HeatTrail : AbilityBase` — spawns trail prefab, DamageZone component
- `Ability_LeafShield : AbilityBase` — instantiates destroyable cover prefab
- `Ability_SporeCloud : AbilityBase` — spawns particle volume, RenderFeature occlusion
- `Ability_StarchArmor : AbilityBase` — calls `owner.health.AddTemporaryHP(40, 4f)`
- `Ability_EarthenSlam : AbilityBase` — OverlapSphere + `KnockbackEffect.Apply()`

### `CaptureZone`
Drives objective scoring. Zone C has a `lockUntilMatchTime` field.

```csharp
public class CaptureZone : MonoBehaviour
{
    public int zoneIndex;          // 0, 1, 2
    public float captureRadius;    // 6m / 4m / 8m (A / B / C)
    public float captureRate;      // seconds to capture with 1 player; more players = faster
    public float scoreTickRate;    // 1 / 1 / 2 per second
    public float lockUntilMatchTime; // 0 / 0 / 180 (seconds from match start)

    int owningTeam;                // -1 = neutral
    float captureProgress;         // 0–1

    void Update()
    {
        if (GameModeManager.ElapsedTime < lockUntilMatchTime) return;
        // count players per team in radius
        // advance or reverse captureProgress
        // on full capture: fire OnZoneCaptured event, start scoring
    }
}
```

### `GameModeManager`
Singleton. Owns match timer, score totals, win condition.

```csharp
public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance;

    float matchDuration = 480f;    // 8 min
    int scoreCap = 500;
    int[] teamScores = new int[2];

    public float ElapsedTime { get; private set; }
    public float RemainingTime => matchDuration - ElapsedTime;

    public void AddScore(int teamId, int amount)  // also checks win condition
    public void EndMatch(int winningTeam)
}
```

---

## Networking (NGO)

### Topology
- **MVP:** Host-client via Unity Relay (one player hosts, others connect through relay)
- **Post-MVP:** Dedicated server via Unity Gaming Services (UGS)

### NetworkBehaviours
```
NetworkedPlayerController   — syncs position (ClientTransform) + rotation
NetworkedHealthController   — NetworkVariable<int> HP; ServerRpc TakeDamage()
NetworkedMomentumController — NetworkVariable<int> Tier; ServerRpc RegisterKill()
NetworkedAbilityController  — ServerRpc ActivateAbility(int slot)
CaptureZoneNetwork          — NetworkVariable<int> OwningTeam; NetworkVariable<float> CaptureProgress
GameModeNetworkManager      — NetworkVariable<int[]> TeamScores; NetworkVariable<float> MatchTimer
```

### Authority Model
- **Server-authoritative** for: damage, score, zone capture, win condition, momentum tier
- **Client-predicted** for: movement, camera rotation, ability visual effects
- **Client-side** only: HUD updates, audio, local VFX

### Latency Compensation
- Hitscan uses server-side lag compensation (rewinds hitbox positions by RTT/2)
- Max compensated lag: 200ms (above this, shots registered as misses server-side)
- Use `Physics.Raycast` on server with rewound transform snapshots (store last 200ms of position history per player)

---

## Input System

`InputSystem_Actions.inputactions` (already in project).

**Player Action Map:**

| Action | Binding | Handler |
|---|---|---|
| Move | WASD / Left stick | `PlayerMovement.SetMoveInput()` |
| Look | Mouse delta / Right stick | `PlayerCamera.SetLookInput()` |
| Sprint | Left Shift / Left stick click | `PlayerMovement.SetSprint()` |
| Jump | Space / South button | `PlayerMovement.Jump()` |
| Crouch | Left Ctrl / Right stick click | `PlayerMovement.SetCrouch()` |
| Fire | Left Mouse / Right Trigger | `WeaponController.Fire()` |
| ADS | Right Mouse / Left Trigger | `WeaponController.SetADS()` |
| Reload | R / West button | `WeaponController.Reload()` |
| Ability 1 | Q / Left Bumper | `AbilityController.Activate(0)` |
| Ability 2 | E / Right Bumper | `AbilityController.Activate(1)` |

---

## Player Prefab Structure

```
[NetworkObject] PlayerRoot
├── [CharacterController] PlayerCapsule
│   └── PlayerMovement
│   └── PlayerController
│   └── NetworkedPlayerController
├── CameraRig
│   ├── [Camera] MainCamera            ← first-person view
│   └── [Camera] WeaponCamera          ← renders weapon model on top (prevents clip)
├── WeaponRoot
│   └── [WeaponModel] + MuzzleFlash
├── PlayerBody                         ← third-person visible mesh (remote players only)
│   └── [SkinnedMeshRenderer]
└── AbilityOrigins                     ← spawn points for ability effects
    ├── AbilityOrigin_1
    └── AbilityOrigin_2
```

---

## Ability VFX / Effects System

Status effects use a component-add model: applying a slow adds a `SlowEffect` MonoBehaviour to the target. It modifies movement speed and removes itself on expiry.

```csharp
public static class EffectSystem
{
    public static void ApplySlow(PlayerController target, float amount, float duration)
    public static void ApplyFire(PlayerController target, float dps, float duration)
    public static void ApplyKnockback(PlayerController target, Vector3 force)
    public static void ApplyTemporaryHP(PlayerController target, int amount, float duration)
}
```

---

## Audio Architecture

- `AudioManager` singleton: pools AudioSource components, plays oneshots and managed loops
- SFX categories: `Footsteps`, `Weapons`, `Abilities`, `UI`, `Ambience`, `Music`
- Music uses layered stems: `Base` always playing, `Intensity` fades in with match tension, `MomentumTier3` personal layer on `OnFire`
- 3D spatial audio for all in-world sounds; UI sounds are 2D

---

## Performance Targets

| Platform | Target FPS | Render resolution | Notes |
|---|---|---|---|
| PC (mid-range) | 120 FPS | Native | Graphy profiler in dev builds |
| PC (low-end) | 60 FPS | 1080p | Low URP quality level |
| Mobile (primary test) | 30 FPS stable | 720p | Mobile URP profile already configured |

**Critical:** Max 8 players per match keeps CPU simulation cost manageable. Avoid per-frame `FindObjectsOfType` — use events and cached references throughout.

---

## Bot AI (Tutorial & Testing)

Needed for two purposes: Phase 1 solo testing (shoot something that moves) and Phase 5 tutorial.

### Architecture

`BotController` replaces `PlayerController`'s input source with an AI-driven `IBotBrain` implementation. The bot uses the same `PlayerMovement`, `WeaponController`, `AbilityController`, and `HealthController` as a real player — only the input layer differs.

```csharp
public interface IBotBrain
{
    Vector2 GetMoveInput();
    Vector2 GetLookInput();
    bool GetFireInput();
    bool GetAbility1Input();
    bool GetAbility2Input();
}

public class BotController : MonoBehaviour
{
    IBotBrain brain;
    PlayerController player; // feeds brain output into player subsystems each Update()
}
```

### Bot Difficulty Levels

| Level | Aim accuracy | Reaction time | Ability usage | Movement |
|---|---|---|---|---|
| Dummy (Phase 1) | 0% (never fires) | — | Never | Wanders randomly |
| Easy | 40% shot accuracy | 600ms | Random | Walks toward objective |
| Medium | 65% accuracy | 300ms | Contextual | Patrols zone, pushes on capture |
| Hard | 85% accuracy | 150ms | Smart | Flanks, uses abilities appropriately |

### Tutorial Bot
- Fixed script (not AI-driven): walks forward, stops, demonstrates taking damage
- Used only in `TutorialScene` to demonstrate momentum system
- Does not need `NetworkBehaviour` — tutorial is offline-only

### Bot Pathfinding
- Unity NavMesh: bake on completed map (Phase 3)
- NavMesh links for: Rooftop Catwalk drop, Underground Cellar stairs
- Bots use `NavMeshAgent` to navigate; override velocity when `PlayerMovement.Dash()` is called by AI

### Bot Objectives
Simple finite state machine:

```
IDLE → MOVE_TO_OBJECTIVE → CAPTURE → FIGHT_NEARBY_ENEMY → RETREAT (< 30% HP)
```

- Transition IDLE → MOVE_TO_OBJECTIVE: always; bots always pursue nearest uncaptured zone
- Transition → FIGHT: `Physics.OverlapSphere(8m)` finds enemy; bot engages
- Transition → RETREAT: HP < 30%; moves toward Broccoli ally or back to spawn
