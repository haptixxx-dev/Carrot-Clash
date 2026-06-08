# Implementation Status

A live dashboard reconciling the [design docs](/gdd) against the actual codebase. The
gameplay spine is **code-complete and verified against the [frozen API contract](/dev/architecture)**:
97 C# scripts under `Assets/_Game/`, all compiling against `CONTRACTS.md`. But this is a
*code drop*. It has never been opened in the editor, so **no scenes, prefabs, art, audio,
NavMesh, or data assets are committed yet.**

::: warning READ THIS FIRST: what "Implemented" means here
"Implemented" means **the C# logic exists and is verified against the contract**, not that
it is playable on screen. Nothing is fully playable today because every system still needs
its scenes, prefabs, art, audio, and NavMesh wired in the Unity editor. Treat the legend below
literally.
:::

## Legend

- <span class="cc-status built">Implemented</span>: code exists and is verified against `CONTRACTS.md`.
- <span class="cc-status partial">Partial</span>: code exists but needs editor wiring, missing input bindings, or a dormant package.
- <span class="cc-status pending">Editor-pending</span>: needs editor-authored content (scenes / prefabs / art / audio / NavMesh).

::: info Source of truth
This page summarises the canonical engineering docs at the repo root:
`README.md`, `DEVENV.md`, `ASSETS.md`, `Assets/_Game/SETUP.md`, `Assets/_Game/CONTRACTS.md`.
Where a number disagrees with the GDD, the code wins and is noted below.
:::

---

## System dashboard

### Player & combat core

| System | Design doc | Code status | Notes |
|---|---|---|---|
| FPS movement (WASD, look, jump, sprint, crouch) | [Characters](/characters) | <span class="cc-status built">Implemented</span> | `PlayerMovement` on `CharacterController` (not Rigidbody, per [milestones](/milestones)). Gravity `-22`. |
| First-person camera (mouse look, FOV, bob) | [Game Feel](/game-feel) | <span class="cc-status built">Implemented</span> | `PlayerCamera`. Default FOV `90`. `cam` is null on bots/remote, so it is null-checked. |
| Hitscan weapons (falloff + headshots) | [Weapons](/weapons) | <span class="cc-status built">Implemented</span> | `WeaponController` + `WeaponDataSO`. Headshot mult via `PlayerHitbox.IsHead`. |
| Health + temporary HP | [Characters](/characters) | <span class="cc-status built">Implemented</span> | `HealthController`; temp-HP layer for Starch Armor / Leaf Shield. |
| Hitboxes (head/body) | [Weapons](/weapons) | <span class="cc-status built">Implemented</span> | `PlayerHitbox` on `Hitbox` layer; head collider flagged in prefab. |
| Input binding | [Tech Architecture](/tech-architecture) | <span class="cc-status partial">Partial</span> | `PlayerInputBinder` works, but the input **asset is missing Reload / ADS / Ability1 / Ability2 / SwapWeapon**. Code falls back to **R / RightMouse / Q / E** (and X / scroll for swap). Cleaner to add the actions. |
| Player prefab | [Tech Architecture](/tech-architecture) | <span class="cc-status pending">Editor-pending</span> | No prefab committed. Build per `SETUP.md` §2c (root + CameraRig + head/body hitboxes, Player layer). |

### Momentum

| System | Design doc | Code status | Notes |
|---|---|---|---|
| Momentum tiers 0-3 (Cold→Warm→Hot→OnFire) | [Momentum System](/momentum-system) | <span class="cc-status built">Implemented</span> | `MomentumController` + `MomentumConfigSO`. `OnTierChanged` event. |
| Decay | [Momentum System](/momentum-system) | <span class="cc-status built">Implemented</span> | Decay interval **12s** (`MomentumDecayInterval`). |
| Tier transfer on kill | [Momentum System](/momentum-system) | <span class="cc-status built">Implemented</span> | Drives `ScoreKillOnFire` (8 vs 5 base) and class passives. |
| Class momentum passives (all 4) | [Momentum System](/momentum-system) | <span class="cc-status built">Implemented</span> | Backstab / ExtendedStreak / SharedHarvest / StubbornRoot; see classes below. |

### Classes & abilities

Code reality: **16 ability behaviours** = 8 active abilities + 4 class passives + 4 momentum
passives, built on `AbilityBase` / `AbilityController` / `AbilityFactory` driven by
`AbilityDataSO`. All <span class="cc-status built">Implemented</span> in logic; per-ability
VFX/SFX and ability-origin indicators are <span class="cc-status pending">Editor-pending</span>.

| Class | Active 1 | Active 2 | Passive | Momentum passive | Code status |
|---|---|---|---|---|---|
| Carrot | Sprint Dash | Radar Pulse | Silent Steps | Backstab | <span class="cc-status built">Implemented</span> |
| Jalapeño | Spice Burst | Heat Trail | Burn Streak | Extended Streak | <span class="cc-status built">Implemented</span> |
| Broccoli | Leaf Shield | Spore Cloud | Regen Aura | Shared Harvest | <span class="cc-status built">Implemented</span> |
| Potato | Starch Armor | Earthen Slam | Thick Skin | Stubborn Root | <span class="cc-status built">Implemented</span> |

| Supporting system | Design doc | Code status | Notes |
|---|---|---|---|
| Ability slot manager + cooldowns | [Tech Architecture](/tech-architecture) | <span class="cc-status built">Implemented</span> | `AbilityController` respects momentum cooldown multiplier. |
| EffectSystem (slow / fire / knockback / temp-HP) | [Ability Interactions](/ability-interactions) | <span class="cc-status built">Implemented</span> | `EffectSystem` static; fire ticks every `0.1s`, slow `0.35`/`3s`, knockback `6m`/`0.25s`. |
| Character data | [Characters](/characters) | <span class="cc-status partial">Partial</span> | `CharacterDataSO` exists; the 4 assets are **generated, not committed** (editor menu). |
| Class select VFX / origin indicators | [Game Feel](/game-feel) | <span class="cc-status pending">Editor-pending</span> | Particle/indicator art not authored. |

### Objectives & match flow

| System | Design doc | Code status | Notes |
|---|---|---|---|
| Capture zones (A / B / C) | [Map](/map), [Core Loop](/core-loop) | <span class="cc-status built">Implemented</span> | `CaptureZone` (OverlapSphere on Player layer) + score tick. |
| Zone C unlock at 5:00 | [Map](/map) | <span class="cc-status built">Implemented</span> | `IsZoneCUnlocked(elapsed)`; unlock time **300s**. |
| Match state machine | [Core Loop](/core-loop) | <span class="cc-status built">Implemented</span> | `GameModeManager`: Lobby→ClassSelect→Countdown→MatchActive→SuddenDeath→MatchEnd→PostMatch. Match `480s`, sudden death `60s`, cap `500`. |
| Scoring + MVP / Hot Streak | [Core Loop](/core-loop) | <span class="cc-status built">Implemented</span> | `MatchStats` tracks per-player stats, MVP and Hot Streak. |
| Spawn + respawn + safety redirect | [Map](/map) | <span class="cc-status built">Implemented</span> | `SpawnManager` / `PlayerSpawner`. Respawn `4s`, spawn invuln `2s`. |
| Boot / match bootstrap | [Tech Architecture](/tech-architecture) | <span class="cc-status built">Implemented</span> | `GameBootstrap`, `MatchInitializer`, `SceneFlow`. |
| Scenes (Boot / MainMenu / Gameplay_Market / GameplayUI) | [Tech Architecture](/tech-architecture) | <span class="cc-status pending">Editor-pending</span> | No scenes committed. Create + add to Build Settings per `SETUP.md` §2e. |

### HUD, feedback & menus

| System | Design doc | Code status | Notes |
|---|---|---|---|
| HUD (11 widgets) | [UI / UX](/ui-ux) | <span class="cc-status built">Implemented</span> | `PlayerHUD` + Health, Momentum ring, Score bar, Ammo, Crosshair, Hit marker, Kill feed (+entry), Ability slots, Zone indicators. |
| Combat feedback / juice | [Game Feel](/game-feel) | <span class="cc-status built">Implemented</span> | `FeedbackController`, `ScreenEffects` (shake types), `OnFireOverlay`, `DamageDirectionIndicator`. |
| Menus (main / class-select / post-match / settings) | [UI / UX](/ui-ux) | <span class="cc-status built">Implemented</span> | `MainMenuController`, `ClassSelectController`, `PostMatchController`, `SettingsMenuController` + `SettingsService` (persists). |
| Reduce Motion / colorblind / text size | [UI / UX](/ui-ux) | <span class="cc-status built">Implemented</span> | `SettingsService` exposes accessibility modes; feedback respects Reduce Motion. |
| UI canvases / TMP layouts / icons | [UI / UX](/ui-ux) | <span class="cc-status pending">Editor-pending</span> | No canvases or icon/VFX assets authored. |

### Bots

| System | Design doc | Code status | Notes |
|---|---|---|---|
| Bot brain (FSM) | [Tech Architecture](/tech-architecture) | <span class="cc-status built">Implemented</span> | `CombatBotBrain` + `DummyBrain` via `IBotBrain`; states Idle/MoveToObjective/Capture/FightNearbyEnemy/Retreat. |
| Bot controller + spawner | [Tech Architecture](/tech-architecture) | <span class="cc-status built">Implemented</span> | `BotController`, `BotSpawner` fills empty slots. |
| NavMesh | [Map](/map) | <span class="cc-status pending">Editor-pending</span> | AI Navigation package present, but the **NavMesh must be baked** on the (not-yet-built) map before bots path. |

### Audio

| System | Design doc | Code status | Notes |
|---|---|---|---|
| Pooled audio manager + library | [Audio](/audio) | <span class="cc-status built">Implemented</span> | `AudioManager` (pooled sources, max dist `25`) + `AudioLibrary` keyed clips. |
| Footsteps (surface-responsive) | [Audio](/audio) | <span class="cc-status built">Implemented</span> | `FootstepController` + `FootstepBank` per `SurfaceType`. Silent Steps mutes under 50% sprint speed. |
| Music director + ambient zones | [Audio](/audio) | <span class="cc-status built">Implemented</span> | `MusicDirector`, `AmbientZone`. |
| Audio clips / mixer | [Audio](/audio) | <span class="cc-status pending">Editor-pending</span> | **No clips authored.** Missing keys safely no-op. Optional `AudioMixer` exposed params per `SETUP.md`. |

### Progression

| System | Design doc | Code status | Notes |
|---|---|---|---|
| XP + levels | [Progression](/progression) | <span class="cc-status built">Implemented</span> | `XPManager`, `ProgressionService`, `ProgressionData`. |
| Battle pass (stub) | [Progression](/progression) | <span class="cc-status built">Implemented</span> | `BattlePassService` tracks tiers and progress, no purchasing (per vertical-slice scope). |
| Daily challenges | [Progression](/progression) | <span class="cc-status built">Implemented</span> | `ChallengeSystem`. |
| Save / load | [Progression](/progression) | <span class="cc-status built">Implemented</span> | `SaveSystem` persists progression. |
| Battle pass / challenge UI art | [Progression](/progression) | <span class="cc-status pending">Editor-pending</span> | UI canvases + icons not authored. |

### Networking

| System | Design doc | Code status | Notes |
|---|---|---|---|
| Networked player / health / momentum / ability | [Tech Architecture](/tech-architecture) | <span class="cc-status partial">Partial</span> | All four mirrors written behind `#if NETCODE_PRESENT` and dormant. |
| Networked capture zone / game mode | [Tech Architecture](/tech-architecture) | <span class="cc-status partial">Partial</span> | `CaptureZoneNetwork`, `GameModeNetworkManager`, `ConnectionManager` sit behind the same gate. |
| Netcode for GameObjects (NGO) | [Milestones](/milestones) Phase 4 | <span class="cc-status pending">Editor-pending</span> | **Package NOT installed.** Network layer is dormant until `com.unity.netcode.gameobjects` is added, at which point the `Network/` files light up. |
| Unity Relay / Lobby / Authentication | [Milestones](/milestones) Phase 4 | <span class="cc-status pending">Editor-pending</span> | Not installed. Relay (join codes / NAT) and Lobby (room browser) are optional installs. |
| Lag compensation | [Milestones](/milestones) Phase 4 | <span class="cc-status pending">Editor-pending</span> | Server-side hitscan history buffer not yet built (Phase 4 work). |

::: tip Offline-first
Everything except online play works with **zero package installs**. Offline + bots is the
source of truth, and the network files only compile once NGO is present. See
[Editor Setup](/dev/editor-setup) and `SETUP.md` §1.
:::

### Map & world

| System | Design doc | Code status | Notes |
|---|---|---|---|
| Destructible cover | [Map](/map) | <span class="cc-status built">Implemented</span> | `DestructibleCover`. |
| Damage zones / vision blockers | [Map](/map) | <span class="cc-status built">Implemented</span> | `DamageZone`, `VisionObscured` (Spore Cloud / Radar Pulse interplay). |
| The Grand Food Market (grey-box, art, NavMesh) | [Map](/map) | <span class="cc-status pending">Editor-pending</span> | No geometry committed. Zones A/B/C, lanes, flanking routes, and both spawns all need blockout → art → bake. |

---

## Data & engine notes

- **Data assets** (4 `CharacterDataSO`, 5 `WeaponDataSO`, 16 `AbilityDataSO`, `MomentumConfigSO`)
  are **generated from the docs**, not committed. Run `Carrot Clash → Generate Default Data Assets`,
  then `Carrot Clash → Validate Data` (see [Editor Setup](/dev/editor-setup)). This keeps the
  numbers matching the GDD.
- **Engine** pinned to **Unity 6000.4.10f1**, URP 17.4, Input System 1.19.
- **Namespace** `CarrotClash` (`.Audio`, `.Net`, `.EditorTools` sub-namespaces). The public
  surface is frozen in `Assets/_Game/CONTRACTS.md`; see [Code Architecture](/dev/architecture).

---

## What "done" looks like

Per [Milestones - Definition of "Vertical Slice"](/milestones), the slice is done when it:

- Represents the final game's quality and feel in **every system it contains**.
- Is playable by **someone with no prior knowledge**.
- **Contains:** 1 map, 4 classes, 1 mode, online multiplayer, a progression stub, full SFX/VFX.
- **Does not contain:** ranked mode, battle-pass purchasing, extra maps, extra classes.

The acceptance test (Phase 5): **3 people unfamiliar with the game play a full match,
understand what momentum is, and say "one more game" unprompted.**

::: warning Gap to "done"
The **gameplay logic is finished and verified**, but the slice is **not playable yet**. To
cross the line, the remaining work is editor-authored, not code:

1. **Content:** build all scenes, the player prefab, the Grand Food Market (grey-box → art →
   baked NavMesh), UI canvases, every audio clip, and all icons/VFX.
2. **Input:** add Reload / ADS / Ability1 / Ability2 / SwapWeapon actions to
   `InputSystem_Actions` (today they run on keyboard/mouse fallbacks).
3. **Networking:** install NGO (+ optional Relay/Lobby/Authentication), wire the dormant
   `Network/` mirrors, and build Phase-4 lag compensation.

See the [Asset Checklist](/dev/assets) for the full editor task list.
:::
