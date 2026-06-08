# Code Architecture

<span class="cc-status built">Implemented</span> — 97 C# scripts under `Assets/_Game/`, verified to compile against the frozen API. There are **no scenes, prefabs, art, or audio clips yet** — that content is editor-pending. This page describes the code spine; the editor wiring lives in [Editor Setup](/dev/editor-setup) and the current build state in [Implementation Status](/dev/status).

::: info Source of truth
The canonical API is [`Assets/_Game/CONTRACTS.md`](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/Assets/_Game/CONTRACTS.md). This page summarises and explains it — if the two ever disagree, **CONTRACTS.md wins**. Setup steps live in [`Assets/_Game/SETUP.md`](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/Assets/_Game/SETUP.md).
:::

---

## Namespace & conventions

All gameplay code lives in the **`CarrotClash`** namespace, with two sub-namespaces:

- `CarrotClash.Audio` — the pooled audio manager + library
- `CarrotClash.Net` — the network mirror layer (compiles only with `NETCODE_PRESENT`)
- `CarrotClash.EditorTools` — editor menu generators/validators (its own asmdef)

Style is deliberately conservative: explicit types (no `var` for fields), 4-space indent, doc-comments on public types, and **no `FindObjectsOfType` per frame**. Engine is pinned to **Unity 6000.4.10f1** (URP 17.4, Input System 1.19).

---

## Folder layout

Everything ships under `Assets/_Game/`. Each folder is a single responsibility; leaf systems (UI, audio, bots, progression, network) only ever talk to the spine through events and the frozen contract.

| Folder | Responsibility |
|---|---|
| `Shared/` | Enums, `GameConstants`, `GameExtensions`, `DamageInfo`/`DamageResult`, the static **`GameEvents`** hub, `EffectSystem`, `SettingsService` |
| `Core/` | Bootstrap & flow: `GameBootstrap`, `SceneFlow`, `MatchInitializer`, `PlayerSpawner` |
| `Characters/` | `PlayerController` orchestrator + subsystems, `CharacterDataSO`, the ability framework |
| `Characters/Abilities/` | `AbilityBase`, `AbilityController`, `AbilityDataSO`, `AbilityFactory` |
| `Characters/Abilities/Impl/` | The 16 concrete ability behaviours |
| `Characters/Data/` | Generated `CharacterDataSO` assets (editor-pending) |
| `Weapons/` | `WeaponController`, `WeaponDataSO`, hitscan + falloff + headshots, hitboxes |
| `Weapons/Data/` | Generated `WeaponDataSO` assets (editor-pending) |
| `Gameplay/Momentum/` | `MomentumController`, `MomentumConfigSO` (tier decay/transfer, class passives) |
| `Gameplay/Objectives/` | `CaptureZone`, `GameModeManager`, `MatchStats`, `SpawnManager` |
| `Gameplay/Bots/` | Bot FSM brain + `BotSpawner` |
| `Gameplay/Map/` | Map-side helpers/markers (grey-box + NavMesh are editor-pending) |
| `UI/HUD/` | The 11 HUD widgets + `PlayerHUD` |
| `UI/Feedback/` | Combat feedback / screen juice |
| `UI/Menus/` | Main / class-select / post-match / settings menus |
| `Audio/` | `AudioManager` (pooled), `AudioLibrary`, footsteps, music director, ambient |
| `Progression/` | XP, battle pass, challenges, save system |
| `Network/` | NGO mirror components, all behind `#if NETCODE_PRESENT` |
| `Editor/` | `DataAssetGenerator`, `DataValidator`, ability catalog (menu tools) |

---

## Event-driven design: the `GameEvents` hub

<span class="cc-status built">Implemented</span>

The spine never reaches up to UI/audio/progression — instead, the gameplay systems **raise** strongly-typed events on the static `GameEvents` class, and leaf systems **subscribe**. This keeps the HUD, audio, bots, stats, and network mirrors fully decoupled from the simulation.

::: warning Subscription hygiene
Subscribe with `+=` and **always** unsubscribe in `OnDisable`/`OnDestroy`. `GameEvents` is static — a missed unsubscribe leaks across scene loads. `GameEvents.Clear()` detaches everything (used on hard teardown).
:::

Key events (see CONTRACTS.md for exact signatures):

- `OnKill(KillEvent)` — killer, victim, killing-blow type, backstab flag, victim tier, headshot flag
- `OnDamageDealt(PlayerController victim, DamageInfo, DamageResult)`
- `OnPlayerSpawned(PlayerController)` / `OnPlayerDied(PlayerController)`
- `OnTierChanged(PlayerController, old, new)` — momentum tier transitions
- `OnAssist(PlayerController, int amount)`
- `OnZoneCaptured(ZoneCaptureEvent)` / `OnZoneProgressChanged(ZoneId, float)` / `OnZoneCUnlocked()`
- `OnMatchStateChanged(old, new)` — match state machine
- `OnScoreChanged(int teamId, int newTotal)`
- `OnMatchTimerTick(float remainingSeconds)`
- `OnMatchEnded(Team winner)` — `Team.None` means draw

Each event has a matching `Raise…` method (`RaiseKill(in KillEvent)`, `RaiseScoreChanged(teamId, total)`, etc.). Only the owning system raises; everyone else listens.

```
                          ┌──────────────────────────────┐
   WeaponController ──┐    │      GameEvents (static)     │    ┌──> PlayerHUD / widgets
   HealthController ──┼──> │  OnKill / OnDamageDealt /    │ ──>├──> AudioManager
   MomentumController─┤    │  OnTierChanged / OnScore… /  │    ├──> MatchStats
   CaptureZone ───────┤    │  OnZoneCaptured / OnMatch…   │    ├──> ProgressionService
   GameModeManager ───┘    └──────────────────────────────┘    └──> Network mirrors
        (raisers)                                                       (subscribers)
```

---

## ScriptableObject data layer

<span class="cc-status partial">Partial</span> — the code reads SOs everywhere; the **asset instances are generated via an editor menu and are not committed**.

Code never hardcodes class/weapon/ability stats. All tuning lives on ScriptableObjects:

- **`CharacterDataSO`** — one per class: HP, move speed, sprint multiplier, jump height, primary/secondary `WeaponDataSO`, the four `AbilityDataSO`s (active1/active2/passive/momentumPassive), colours, model prefab.
- **`WeaponDataSO`** — hitscan stats: body damage, headshot multiplier, pellets/spread, fire rate, burst params, mag size, reload time, effective range + falloff, recoil pattern, camera kick. Exposes `ComputeDamage(distance, headshot, momentumMultiplier)` and `FalloffMultiplier(dist)`.
- **`AbilityDataSO`** — cooldown, duration, range, radius, magnitude(s), VFX/SFX keys, and an `AbilityKind` (`Active` / `Passive` / `MomentumPassive`).
- **`MomentumConfigSO`** — decay interval, on-death transfer, and a 4-entry `MomentumTierData[]` (speed/cooldown/damage bonuses + per-tier score value + VFX colour).

Generate them once via the editor menu **`Carrot Clash → Generate Default Data Assets`**, then **`Carrot Clash → Validate Data`** (the `Editor/` generators keep the numbers matched to the GDD). See [Editor Setup](/dev/editor-setup).

---

## `PlayerController`: the orchestrator

<span class="cc-status built">Implemented</span>

`PlayerController` owns a player entity and wires its subsystems together. It does almost no logic itself — it routes input and exposes the subsystem references:

| Subsystem | Role |
|---|---|
| `PlayerMovement` | `CharacterController`-based locomotion: sprint/crouch/jump, slow, knockback/stagger, dash |
| `PlayerCamera` | First-person camera rig, shake, FOV surge, death tilt — **local player only** |
| `WeaponController` | Hitscan firing, ADS, reload, weapon swap; raises ammo/fired/hit events |
| `AbilityController` | Holds the two active abilities + cooldown tracking |
| `HealthController` | HP, temp-HP, invulnerability, death + assist tracking |
| `MomentumController` | Tier charge/decay/transfer, derived speed/cooldown/damage multipliers |

It also exposes transforms used by abilities/hitscan: `abilityOrigin1`, `abilityOrigin2`, `headTransform`.

Lifecycle: `Initialize(CharacterDataSO, MomentumConfigSO, Team, id, local, bot=false)` sets it up; input arrives via the routing methods (`InputMove`, `InputLook`, `InputFire`, `InputAbility(slot)`, `InputReload`, `InputAds`, `InputSwapWeapon`, …); `ApplyDamage(in DamageInfo)` and `Respawn(pos, rot)` handle combat lifecycle.

::: warning `cam` is null on bots and remote players
`PlayerController.cam` (the `PlayerCamera`) is **only assigned for the local human player**. On bots and on remote networked players it is `null`. Any code touching the camera **must null-check** — the spine already does. Bots are distinguished by `IsBot`, locality by `IsLocal`.
:::

Input itself comes from `PlayerInputBinder`. Note the input asset currently **lacks Reload/ADS/Ability1/Ability2/SwapWeapon** actions, so the binder falls back to **R / RightMouse / Q / E** — see [Implementation Status](/dev/status).

---

## The 16-ability `AbilityFactory` pattern

<span class="cc-status built">Implemented</span>

Every ability is a `MonoBehaviour` subclassing **`AbilityBase`**. There are 16 — four per class — split across `Active`, `Passive`, and `MomentumPassive` kinds. They all live in `Assets/_Game/Characters/Abilities/Impl/` in the `CarrotClash` namespace.

`AbilityBase` defines the contract:

- `Bind(owner, config)` → calls `OnBind()` — **passives subscribe to `GameEvents` here**
- `Unbind()` — unsubscribe here
- `Activate(activator)` → returns `true` if it fired (actives only)
- `Cancel()`
- Helpers: `PlayActivationFeedback()`, `static SpawnTimed(prefab, pos, rot, lifetime)`, `static PlacementMask`

**`AbilityFactory.Attach(playerObject, config)`** is the glue: it maps `AbilityDataSO.abilityName` → the concrete component type and adds it to the player object. The 16 class names are **frozen** (referenced by name in the factory map):

| Class | Active 1 | Active 2 | Passive | Momentum passive |
|---|---|---|---|---|
| Carrot | `Ability_SprintDash` | `Ability_RadarPulse` | `Passive_SilentSteps` | `MomentumPassive_Backstab` |
| Jalapeño | `Ability_SpiceBurst` | `Ability_HeatTrail` | `Passive_BurnStreak` | `MomentumPassive_ExtendedStreak` |
| Broccoli | `Ability_LeafShield` | `Ability_SporeCloud` | `Passive_RegenAura` | `MomentumPassive_SharedHarvest` |
| Potato | `Ability_StarchArmor` | `Ability_EarthenSlam` | `Passive_ThickSkin` | `MomentumPassive_StubbornRoot` |

::: details Marker passives
A few passives (`Passive_SilentSteps`, `Passive_ThickSkin`) are also read directly by `PlayerMovement`/`AudioManager` via a `PlayerController.ClassId` check. Their behaviour component is a thin marker that exists for the factory + future tuning — it is a real component, just with minimal `Activate` logic.
:::

`AbilityController` exposes per-slot state to the HUD: `IsReady(slot)`, `CooldownFraction(slot)`, `ActivateAbility(slot)`, plus `OnCooldownChanged(slot, fraction)` / `OnAbilityActivated(slot)` events.

---

## `EffectSystem`

<span class="cc-status built">Implemented</span>

`EffectSystem` is a small static façade that abilities and weapons call to apply standard status effects to a target, so the effect logic isn't duplicated:

- `ApplySlow(target, amount, duration)`
- `ApplyFire(target, dps, duration, instigator)`
- `ApplyKnockback(target, sourcePosition, distance, stagger)`
- `ApplyTemporaryHP(target, amount, duration)`

Internally these route into `PlayerMovement` (slow/knockback), `HealthController` (temp-HP), and a fire damage-over-time loop using the instigator for kill attribution.

---

## Network mirror layer (`#if NETCODE_PRESENT`)

<span class="cc-status pending">Editor-pending</span> — **Netcode for GameObjects is not installed yet**, so this entire layer is dormant. Relay/Lobby likewise are not installed.

The `CarrotClash.Net` asmdef and every file in `Assets/_Game/Network/` are wrapped in `#if NETCODE_PRESENT … #endif`, a define that only exists once the NGO package is added. **The project compiles fully without it** — offline + bots is the default mode.

The design is a **mirror** pattern, not a rewrite: each `Networked*` component holds a reference to the local gameplay component and pushes/pulls `NetworkVariable`s. Gameplay logic stays in the offline spine — the offline simulation is the source of truth and the mirrors only replicate it.

- `NetworkedPlayerController`, `NetworkedHealthController`, `NetworkedMomentumController`, `NetworkedAbilityController` — mirror the player subsystems
- `CaptureZoneNetwork`, `GameModeNetworkManager` — mirror objective/match state
- `ConnectionManager` — host/join entry point

To light it up: install `com.unity.netcode.gameobjects` (and optionally Relay/Lobby/Authentication). See [Environment Setup](/dev/environment) and [Implementation Status](/dev/status).

---

## `CONTRACTS.md` is the frozen API

::: warning Do not drift from the contract
Every leaf file (abilities, UI, bots, network, audio, progression, feedback) compiles against **exactly** the signatures in `CONTRACTS.md`. Do not invent members not listed there. **If you change a spine signature, update `CONTRACTS.md` in the same commit.**
:::

The contract enumerates every public type and member of the spine: enums, `GameConstants`, `GameExtensions`, the damage/kill/zone structs, `GameEvents`, all the player subsystems, the SO data layer, the ability framework, `EffectSystem`, the objective/match systems, settings, audio, and the networking note. It is the first thing to read before touching gameplay code.

➡️ **Full API:** [`Assets/_Game/CONTRACTS.md` on GitHub](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/Assets/_Game/CONTRACTS.md)

## See also

- [Getting Started](/dev/getting-started) — clone, open, run
- [Environment Setup](/dev/environment) — packages, Unity version
- [Editor Setup & Wiring](/dev/editor-setup) — scenes, prefabs, data assets
- [Asset Checklist](/dev/assets) — editor-pending content
- [Implementation Status](/dev/status) — what's built vs. pending
- [Tech Architecture](/tech-architecture) — the original design-side architecture doc
