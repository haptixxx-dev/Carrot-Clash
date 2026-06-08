# Technical Architecture

Unity 6 (pinned **6000.4.10f1**), Universal Render Pipeline, Netcode for GameObjects (NGO — *not yet installed*).

::: info CODE STATUS (2026-06)
The runtime architecture below is **implemented** — 97 C# scripts under `Assets/_Game/` in namespace `CarrotClash`, all compiling and cross-checked. What remains is **editor-authored content**: scenes, the player prefab, the map (grey-box + art + NavMesh), UI canvases, art/VFX/audio clips. Nothing is "playable" until that content is built.

The frozen public API is in [`Assets/_Game/CONTRACTS.md`](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/Assets/_Game/CONTRACTS.md) — treat it as the source of truth over any signature shown here. Engineers should start at the [/dev pages](/dev/getting-started): [Getting Started](/dev/getting-started), [Architecture](/dev/architecture), [Editor Setup](/dev/editor-setup), [Status](/dev/status).

Two notable deltas from the original design are flagged inline below:
- **NGO is not installed** — the network layer compiles only behind the `NETCODE_PRESENT` define and is dormant. Relay/Lobby likewise.
- The **Input asset lacks** Reload / ADS / Ability1 / Ability2 / SwapWeapon actions; the binder falls back to fixed keys (R / RightMouse / Q / E / wheel).
:::

---

## Scene Structure

<span class="cc-status pending">Editor-pending</span> — scene names are wired in code (`GameConstants`, `SceneFlow`); the scene **assets** themselves are not authored yet.

```
Boot               (lightweight — loads config, authenticates, transitions to Menu)
MainMenu           (lobby browser, class preview, settings)
Gameplay_Market    (the map — all gameplay lives here)
GameplayUI         (additive HUD scene)
TutorialScene      (offline tutorial — Phase 5)
```

**Additive loading:** `Gameplay_Market` additively loads a `GameplayUI` scene so HUD is decoupled from map geometry.

::: info CODE DELTA
`GameConstants` defines the canonical scene names: `SceneBoot`, `SceneMainMenu`, `SceneGameplay` (= `"Gameplay_Market"`), `SceneGameplayUI` (= `"GameplayUI"`), and `SceneTutorial` (= `"TutorialScene"`). `SceneFlow.cs` drives transitions. The scene **files** are editor-pending.
:::

---

## Folder Layout (Assets/)

<span class="cc-status built">Implemented</span> — matches the shipped tree (with a few extra folders: `Core/`, `Progression/`, `Editor/`). All runtime code is under one assembly, `CarrotClash.Runtime.asmdef`.

```
Assets/
├── _Game/
│   ├── Core/             ← GameBootstrap, SceneFlow, PlayerSpawner, MatchInitializer
│   ├── Characters/
│   │   ├── Data/          ← CharacterDataSO per class
│   │   ├── Abilities/     ← AbilityDataSO + 16 ability MonoBehaviours (Impl/)
│   │   └── Prefabs/       ← Player prefab variants (editor-pending)
│   ├── Weapons/
│   │   ├── Data/          ← WeaponDataSO per weapon
│   │   └── Prefabs/
│   ├── Gameplay/
│   │   ├── Objectives/    ← CaptureZone, GameModeManager, MatchStats
│   │   ├── Momentum/      ← MomentumController, MomentumConfig SO
│   │   ├── Map/           ← SpawnManager
│   │   └── Bots/          ← IBotBrain, BotController, brains, BotSpawner
│   ├── Network/           ← NGO mirrors (behind #if NETCODE_PRESENT)
│   ├── UI/                ← HUD (11 widgets), Feedback, Menus
│   ├── Audio/             ← AudioManager, library, footsteps, music, ambient
│   ├── Progression/       ← XP, battle pass, challenges, save
│   ├── Editor/            ← data generators + validators (editor-only)
│   └── Shared/            ← GameConstants, enums, events, EffectSystem, settings
├── EasyRoads3D/           (existing)
├── Graphy - Ultimate Stats Monitor/ (existing)
└── SlimUI/                (existing)
```

---

## ScriptableObjects (Data Layer)

<span class="cc-status partial">Partial</span> — all three SO classes exist and compile. Default `.asset` instances are produced by the editor menu **Carrot Clash → Generate Default Data Assets** (`Editor/DataAssetGenerator.cs`) and are **not committed**; an engineer regenerates them locally.

### `CharacterDataSO`

Shipped fields (some added beyond the original sketch — `classId`, `tagline`, `difficulty`, `sprintMultiplier`, `jumpHeight`, `secondaryWeapon`, `accentColor`, `playerModelPrefab`):

```csharp
public class CharacterDataSO : ScriptableObject
{
    public ClassId classId;            // Carrot / Jalapeño / Broccoli / Potato
    public string characterName;
    public string tagline;
    public int difficulty;             // 1–5, class-select rating
    public int baseHP;                 // 90 / 100 / 110 / 140
    public float baseMoveSpeed;        // 7.5 / 6.5 / 6.0 / 5.0 m/s
    public float sprintMultiplier;     // 1.4 default → SprintSpeed
    public float jumpHeight;           // metres (1.4 default)
    public WeaponDataSO primaryWeapon;
    public WeaponDataSO secondaryWeapon; // shared Pistol "The Pip"
    public AbilityDataSO active1, active2, passive, momentumPassive;
    public Color primaryColor;         // momentum VFX tint
    public Color accentColor;
    public GameObject playerModelPrefab;
}
```

### `WeaponDataSO`

::: info CODE DELTA
Falloff is **not** an `AnimationCurve` — it is a stepped table (`FalloffMultiplier(distance)`) reading constants from `GameConstants`: full damage inside `effectiveRange`, then 0.90 / 0.75 / 0.55 / 0.40 at +5 / +10 / +15 / +20 m. Shotguns set `steepFalloff` (50% at +3 m, floors at 0.2). The doc's `falloffCurveStart`/`falloffCurve` fields do not exist. Shotgun spread is `pelletsPerShot` + `spreadAngle`, and burst weapons add `burstFireRateRPM`.
:::

```csharp
public class WeaponDataSO : ScriptableObject
{
    public int damageBody;             // 18 default
    public float headshotMultiplier;   // 1.5
    public int pelletsPerShot;         // 1 (shotgun > 1)
    public float spreadAngle;          // hipfire cone half-angle
    public float fireRateRPM;          // 750 default
    public bool isFullAuto;
    public bool isBurst; public int burstCount; public float burstFireRateRPM; public float burstDelay;
    public int magazineSize;           // 30 default
    public float reloadTime;           // 1.8s default
    public float effectiveRange;       // 25 m default
    public bool steepFalloff;          // shotgun curve
    public float moveSpeedWhileFiring; // Potato LMG = 0.85
    public Vector2[] recoilPattern; public float recoilRecoveryTime; public float cameraKick;

    // Derived: SecondsBetweenShots, BurstSecondsBetweenRounds,
    //          FalloffMultiplier(d), ComputeDamage(d, headshot, momentumMult)
}
```

### `MomentumConfigSO`

::: info CODE DELTA
`decayExtensionOnKill` is **not** a field on the SO — the Jalapeño +5 s extension and other class deviations are applied in code (the momentum-passive behaviours, using `GameConstants.JalapenoDecayExtension` etc.). The SO carries only `decayIntervalSeconds`, `transferOnDeath`, and the `tiers[4]` array; multipliers are read via helper methods (`SpeedMultiplier(tier)`, `CooldownMultiplier(tier)`, `DamageMultiplier(tier)`, `KillScoreValue(victimTier)`).
:::

```csharp
public class MomentumConfigSO : ScriptableObject
{
    public float decayIntervalSeconds;   // 12s default (GameConstants.MomentumDecayInterval)
    public float transferOnDeath;        // 0.5f (50%); Potato overrides to 0.25 in code
    public MomentumTierData[] tiers;     // length 4, array index = tier (0–3)
}

[Serializable]
public struct MomentumTierData
{
    public float moveSpeedBonus;         // 0 / 0.10 / 0.20 / 0.30
    public float cooldownReduction;      // 0 / 0.10 / 0.20 / 0.30
    public float damageBonus;            // 0 / 0    / 0.10 / 0.20
    public int teamScoreKillValue;       // 5 / 5    / 5    / 8 (value to kill a victim AT this tier)
    public Color vfxColor;
}
```

---

## Core MonoBehaviours

<span class="cc-status built">Implemented</span> — every behaviour below exists and is verified. Signatures shown here are illustrative; the **frozen API** lives in [`Assets/_Game/CONTRACTS.md`](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/Assets/_Game/CONTRACTS.md). Where the shipped code names things differently than the original sketch, a CODE DELTA box flags it.

### `PlayerController`
Top-level orchestrator. Holds references, wires input through `PlayerInputBinder` to subsystems. Configured on spawn via `Initialize(data, momentumConfig, team, id, local, bot)`.

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
Manages two active slots + passive + momentum-passive. Cooldowns respect `MomentumController.CooldownMultiplier`. `ActivateAbility` only starts the cooldown if the ability actually fired (returns `true`); abilities expose readiness via `IsReady(slot)`.

```csharp
public class AbilityController : MonoBehaviour
{
    AbilityBase active1, active2;
    float[] cooldownTimers = new float[2];

    public bool IsReady(int slot);     // slot 0 = Active1, 1 = Active2
    public void ActivateAbility(int slot)
    {
        if (cooldownTimers[slot] > 0) return;
        if (!abilities[slot].Activate(owner)) return;   // false = nothing happened, no CD
        cooldownTimers[slot] = abilities[slot].Cooldown * momentum.CooldownMultiplier;
    }
}
```

### `AbilityBase`

::: info CODE DELTA
`Activate` **returns `bool`** (true = fired → controller starts the cooldown; false = no valid target/surface → cooldown untouched), it is not `abstract void`. Abilities are bound on spawn via `Bind(owner, config)` → `OnBind()` (passives subscribe to events there) and `Unbind()`. `Data` is exposed through a property; helpers `AimRay`, `PlacementMask`, `PlayActivationFeedback()`, `SpawnTimed(...)` are provided.
:::

```csharp
public abstract class AbilityBase : MonoBehaviour
{
    public AbilityDataSO Data { get; }
    public float Cooldown { get; }
    protected PlayerController Owner { get; }

    public virtual void Bind(PlayerController owner, AbilityDataSO config);
    protected virtual void OnBind() { }      // passive event hookup
    public virtual void Unbind() { }
    public virtual bool Activate(PlayerController activator) => false;
    public virtual void Cancel() { }
}
```

**Concrete implementations** (16 total — 8 actives, 4 passives, 4 momentum-passives; all built and selected by `AbilityFactory`):

*Active abilities:*
- `Ability_SprintDash` — calls `owner.movement.Dash()`
- `Ability_RadarPulse` — `Physics.OverlapSphere` + reveal effect
- `Ability_SpiceBurst` — applies slow via `EffectSystem.ApplySlow`
- `Ability_HeatTrail` — spawns trail + `DamageZone`
- `Ability_LeafShield` — instantiates `DestructibleCover`
- `Ability_SporeCloud` — spawns particle volume / `VisionObscured` occlusion
- `Ability_StarchArmor` — `EffectSystem.ApplyTemporaryHP(40, 4s)`
- `Ability_EarthenSlam` — `OverlapSphere` + `EffectSystem.ApplyKnockback`

*Passives:* `Passive_SilentSteps`, `Passive_BurnStreak`, `Passive_RegenAura`, `Passive_ThickSkin`
*Momentum-passives:* `MomentumPassive_Backstab`, `MomentumPassive_ExtendedStreak`, `MomentumPassive_SharedHarvest`, `MomentumPassive_StubbornRoot`

### `CaptureZone`

::: info CODE DELTA
The zone is keyed by a `ZoneId` enum (`A`/`B`/`C`), **not** an `int zoneIndex`, and ownership uses the `Team` enum (`Team.None` = neutral), not `-1`. The capture-time field is `captureSeconds` (time for 1 player; A=10 / B=8 / C=12), not `captureRate`; `scoreTickRate` is an `int` (A/B = 1, C = 2). Multi-player capture speedup is tunable: each extra contributor adds `perPlayerSpeedup` (0.5) up to `maxSpeedup` (2.5×). Zone C's `lockUntilMatchTime` is **300 s** (`GameConstants.ZoneCUnlockTime`), not 180. Occupancy uses flat (horizontal) distance for fairness on the elevated C platform.
:::

```csharp
public class CaptureZone : MonoBehaviour
{
    public ZoneId zoneId;          // A / B / C
    public float captureRadius;    // 6m / 4m / 8m (A / B / C)
    public float captureSeconds;   // 1-player capture time: 10 / 8 / 12
    public int scoreTickRate;      // 1 / 1 / 2 per second
    public float lockUntilMatchTime; // 0 / 0 / 300 (seconds from match start)
    public float perPlayerSpeedup; // 0.5 per extra capper
    public float maxSpeedup;       // 2.5× cap

    public Team OwningTeam { get; } // Team.None = neutral
    public float Progress { get; }  // 0–1 toward ProgressTeam
}
```

### `GameModeManager`
Singleton. Owns the match **state machine** (`Lobby → ClassSelect → Countdown → MatchActive → SuddenDeath? → MatchEnd → PostMatch`), match timer, team scores, win condition, Zone C unlock cue, sudden death, and match resolution. Holds `MatchStats` (MVP / hot-streak tracking). Kill scoring is tier-aware (on-fire victim = 8, else 5).

```csharp
public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; }

    float matchDuration = 480f;    // GameConstants.MatchDuration (8 min)
    int scoreCap = 500;            // GameConstants.ScoreCap
    int[] teamScores = new int[GameConstants.TeamCount];

    public MatchState State { get; }
    public float ElapsedTime { get; }
    public float RemainingTime => Mathf.Max(0f, matchDuration - ElapsedTime);
    public MatchStats Stats { get; }

    public int GetScore(Team team);
    public void AddScore(Team team, int amount);          // checks early-win cap
    public void AddKillScore(Team killerTeam, MomentumTier victimTier);
    public void EndMatch(Team winner);                    // Team.None = draw
}
```

---

## Networking (NGO)

<span class="cc-status pending">Editor-pending</span> — **NGO is not installed yet.** All the NetworkBehaviour mirrors below exist in `Assets/_Game/Network/` but compile **only behind the `NETCODE_PRESENT` define** and are dormant. Unity Relay and Lobby are likewise not wired. The non-networked gameplay (everything in the sections above) runs fully offline today via `ConnectionManager`/bots.

::: warning Bring-up order
To activate multiplayer: install **Netcode for GameObjects** + **Unity Transport** (and Relay/Lobby UGS packages), define `NETCODE_PRESENT`, add `NetworkObject`/`NetworkManager` to the prefabs and Boot scene, then the mirrors take over sync. See `CONTRACTS.md` and the [/dev pages](/dev/getting-started) before touching this layer.
:::

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

<span class="cc-status partial">Partial</span> — `PlayerInputBinder` is implemented; it loads `InputSystem_Actions` by name, clones it per-player, and routes the **Player** action map into `PlayerController`.

::: warning CODE DELTA — missing actions, fixed-key fallback
The shipped `InputSystem_Actions` asset **lacks** Reload / ADS / Ability1 / Ability2 / SwapWeapon actions. Until they are added to the asset, `PlayerInputBinder.Update()` polls devices directly with **fixed keys** (not rebindable):

- **Reload** → `R`
- **ADS** → Right Mouse (hold)
- **Ability 1 / 2** → `Q` / `E`
- **Swap weapon** → mouse wheel, or `X` if the asset's Next/Previous are unbound

Add the missing actions to the asset to restore rebindability.
:::

**Actions wired from the asset:**

| Action | Binding | Handler |
|---|---|---|
| Move | WASD / Left stick | `PlayerController.InputMove()` |
| Look | Mouse delta / Right stick | `PlayerController.InputLook()` |
| Sprint | Left Shift / Left stick click | `PlayerController.InputSprint()` |
| Jump | Space / South button | `PlayerController.InputJump()` |
| Crouch | Left Ctrl / Right stick click | `PlayerController.InputCrouch()` |
| Fire (Attack) | Left Mouse / Right Trigger | `PlayerController.InputFire()` |
| Swap (Next/Previous) | Wheel / D-pad | `PlayerController.InputSwapWeapon()` |

**Fixed-key fallback (asset lacks these actions):**

| Action | Key | Handler |
|---|---|---|
| ADS | Right Mouse | `PlayerController.InputAds()` |
| Reload | R | `PlayerController.InputReload()` |
| Ability 1 | Q | `PlayerController.InputAbility(0)` |
| Ability 2 | E | `PlayerController.InputAbility(1)` |

---

## Player Prefab Structure

<span class="cc-status pending">Editor-pending</span> — every component that hangs on this prefab is coded; the **prefab asset itself is not authored yet** (and `[NetworkObject]` only applies once NGO is installed). `PlayerSpawner` / `BotSpawner` instantiate it and call `PlayerController.Initialize(...)`.

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

<span class="cc-status built">Implemented</span> — `EffectSystem` (static façade) centralises the Ability-Interactions rules so each ability doesn't re-implement ordering/stacking.

::: info CODE DELTA
The model is **not** a per-effect `SlowEffect` component. Effects map onto existing subsystems: slow/knockback go through `PlayerMovement` (which enforces a single floor / decaying impulse), temp-HP through `HealthController`, and fire through a managed `FireDamageTicker` so multiple sources stack as independent ticks (fire hits temp HP first — Starch protects). Signatures carry an `instigator` (for kill credit) and knockback takes a **source position + distance + stagger**, not a raw force vector.
:::

```csharp
public static class EffectSystem
{
    public static void ApplySlow(PlayerController target, float amount, float duration);
    public static void ApplyFire(PlayerController target, float dps, float duration, PlayerController instigator);
    public static void ApplyKnockback(PlayerController target, Vector3 sourcePosition, float distance, float stagger);
    public static void ApplyTemporaryHP(PlayerController target, int amount, float duration);
}
```

---

## Audio Architecture

<span class="cc-status partial">Partial</span> — code is built (`AudioManager` pool of `AudioSourcePoolSize` = 32, keyed `AudioLibrary`, `FootstepController` + `FootstepBank`, `MusicDirector` 3-layer mix, `AmbientZone`); **all audio clips and the mixer asset are editor-pending.**

- `AudioManager` singleton: pools AudioSource components, plays one-shots (`PlaySfx`) and 2D UI cues (`PlayUi`), manages named loops, survives scene loads. Default 3D max distance = 25 m.
- SFX categories: `Footsteps`, `Weapons`, `Abilities`, `UI`, `Ambience`, `Music`
- Music uses layered stems (`MusicDirector`): `Base` always playing, `Intensity` fades in with match tension, a tier-3 personal layer on `OnFire`
- 3D spatial audio for all in-world sounds; UI sounds are 2D

---

## Performance Targets

<span class="cc-status pending">Editor-pending</span> — design targets; not yet measured (no scenes/map to profile). `Graphy` is in the project for dev-build profiling.

| Platform | Target FPS | Render resolution | Notes |
|---|---|---|---|
| PC (mid-range) | 120 FPS | Native | Graphy profiler in dev builds |
| PC (low-end) | 60 FPS | 1080p | Low URP quality level |
| Mobile (primary test) | 30 FPS stable | 720p | Mobile URP profile already configured |

**Critical:** Max 8 players per match keeps CPU simulation cost manageable. Avoid per-frame `FindObjectsOfType` — use events and cached references throughout.

---

## Bot AI (Tutorial & Testing)

<span class="cc-status built">Implemented</span> — `IBotBrain`, `BotController`, `DummyBrain`, `CombatBotBrain`, and `BotSpawner` all exist and run. (NavMesh pathing falls back to direct steering when no baked NavMesh / `NavMeshAgent` is present — and the map + NavMesh are editor-pending.)

Needed for two purposes: Phase 1 solo testing (shoot something that moves) and Phase 5 tutorial.

### Architecture

`BotController` replaces `PlayerController`'s input source with an AI-driven `IBotBrain` implementation. The bot uses the same `PlayerMovement`, `WeaponController`, `AbilityController`, and `HealthController` as a real player — only the input layer differs.

::: info CODE DELTA
`IBotBrain` adds a `Tick(PlayerController self)` method, called once per frame **before** the getters are read (decision logic lives in `Tick`; the getters just surface the cached per-frame output). `BotSpawner` instantiates the shared player prefab, calls `Initialize(... bot:true)`, and attaches a `BotController` configured with a `BotDifficulty`.
:::

```csharp
public interface IBotBrain
{
    void Tick(PlayerController self);   // advance decision logic this frame
    Vector2 GetMoveInput();
    Vector2 GetLookInput();
    bool GetFireInput();
    bool GetAbility1Input();
    bool GetAbility2Input();
}

public class BotController : MonoBehaviour
{
    IBotBrain brain;
    PlayerController player; // Tick(brain) then feed output into player subsystems each Update()
}
```

### Bot Difficulty Levels

`BotDifficulty` enum: `Dummy / Easy / Medium / Hard`. `Dummy` uses `DummyBrain`; the other three use `CombatBotBrain(accuracy, reaction)` with the tuning below. The combat brain enables offensive ability use once `aimAccuracy ≥ 0.6` (i.e. Medium/Hard) and retreats below 30% HP.

| Level | Aim accuracy | Reaction time |
|---|---|---|
| Dummy (Phase 1) | 0% (never fires) | — |
| Easy | 40% | 600ms |
| Medium | 65% | 300ms |
| Hard | 85% | 150ms |

| Level | Ability usage | Movement |
|---|---|---|
| Dummy | Never | Wanders randomly |
| Easy | None | Walks toward objective |
| Medium | Contextual | Patrols zone, pushes on capture |
| Hard | Smart | Flanks, uses abilities appropriately |

### Tutorial Bot
- Fixed script (not AI-driven): walks forward, stops, demonstrates taking damage
- Used only in `TutorialScene` to demonstrate momentum system
- Offline-only — no networking needed

### Bot Pathfinding
- Unity NavMesh: bake on completed map (Phase 3)
- NavMesh links for: Rooftop Catwalk drop, Underground Cellar stairs
- Bots use `NavMeshAgent` to navigate; override velocity when `PlayerMovement.Dash()` is called by AI

### Bot Objectives
Simple finite state machine:

```
IDLE → MOVE_TO_OBJECTIVE → CAPTURE → FIGHT_NEARBY_ENEMY → RETREAT (< 30% HP)
```

- Transition IDLE → MOVE_TO_OBJECTIVE: always; bots always pursue nearest not-yet-owned zone
- Transition → FIGHT: `Physics.OverlapSphere(8m)` finds enemy; bot engages (with difficulty-scaled aim error + reaction delay)
- Transition → RETREAT: HP < 30%; heads back toward team spawn (`SpawnManager.GetInitialSpawn`), still returning fire at point-blank threats

---

## For Engineers

This page is the design-level overview. For build/run instructions, the frozen API, editor-content checklists, and current status, see:

- [`Assets/_Game/CONTRACTS.md`](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/Assets/_Game/CONTRACTS.md) — **frozen public API** (authoritative over signatures here)
- [/dev/getting-started](/dev/getting-started) · [/dev/architecture](/dev/architecture) · [/dev/editor-setup](/dev/editor-setup) · [/dev/environment](/dev/environment) · [/dev/status](/dev/status)
- Repo-root canonical docs: `README.md`, `DEVENV.md`, `ASSETS.md`, `Assets/_Game/SETUP.md`
