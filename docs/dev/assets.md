# Asset & wiring checklist

> This page mirrors [`/ASSETS.md`](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/ASSETS.md) at the repo root, which is the source of truth. Edit the root file first, then reconcile here.

Everything that must be created **inside the Unity editor** to make the committed code playable. The C# is done; this is the content and wiring layer it expects. The checklist below is built from what the scripts actually reference (serialized fields, SFX keys, layer names, scene names), so it is exhaustive and pedantic on purpose.

<span class="cc-status pending">Editor-pending</span> Nearly everything on this page is editor-authored content that does **not** exist in the repo yet. The code that consumes it is implemented and verified.

::: tip How to use this
Each section has an **Owner**; assign a person or discipline. Items are ordered so a clean vertical-slice path emerges: do **P0 (blockers)** first, then P1, then P2 polish. Check the box when the asset exists in the project **and** is wired to its script.
:::

**Priority:** P0 = nothing runs without it · P1 = needed for a full match · P2 = polish/feel.

**Discipline:** ENG = engineer · TA = technical artist · ART = artist · SFX = audio · LD = level design · UX = UI/UX.

::: info Cross-references
Component structure → [/dev/architecture](/dev/architecture) · HUD/menus → [/ui-ux](/ui-ux) · feel/VFX → [/game-feel](/game-feel) · SFX spec → [/audio](/audio) · map → [/map](/map). Wiring order and smoke tests → [/dev/editor-setup](/dev/editor-setup).
:::

---

## A. Project configuration

<span class="cc-status pending">Editor-pending</span> P0. Do first, about 30 min.

| # | Item | Pri | Owner |
|---|---|---|---|
| A1 | **Layers** | P0 | ENG |
| A2 | **Physics collision matrix** | P0 | ENG |
| A3 | **Tags** | P2 | ENG |
| A4 | **Build Settings scenes** | P0 | ENG |
| A5 | **Project quality / URP** | P1 | TA |
| A6 | **Input Actions** | P1 | ENG |

**Details**

- **A1, Layers:** Create 4 layers, EXACT spelling: `Player`, `Environment`, `Ability`, `Hitbox`. Code raycasts these by name (`GameConstants`).
- **A2, Collision matrix:** Player ↔ Environment collide; Hitbox layer on player body colliders; Ability layer for ability volumes (triggers). Disable friendly Player ↔ Player if undesired.
- **A3, Tags:** Optional. None hard-required by code (teams are data-driven). Skip unless your prefabs need them.
- **A4, Build scenes:** Add in order: `Boot`, `MainMenu`, `Gameplay_Market`, `GameplayUI`. Remove `SampleScene`.
- **A5, Quality / URP:** PC and Mobile URP profiles already exist in `Assets/Settings/`. Confirm Quality levels map to them; `SettingsService.ApplyVideoSettings` calls `SetQualityLevel`.
- **A6, Input Actions:** `Assets/InputSystem_Actions.inputactions` is **missing** these actions: **Reload, ADS, Ability1, Ability2, SwapWeapon**. Either add them to the `Player` map (preferred) or rely on `PlayerInputBinder`'s hardcoded fallback (**R / RightMouse / Q / E**).

::: warning Input fallback in effect
Until A6 is done the game still plays via the hardcoded fallback keys. The input asset itself does not yet contain those actions.
:::

---

## B. Data assets

<span class="cc-status partial">Partial</span> P0, mostly one click. The generator and validators are implemented; the generated assets are **not committed**.

| # | Item | Pri | Owner |
|---|---|---|---|
| B1 | **Run the generator** | P0 | ENG |
| B2 | 4× **CharacterDataSO** | P0 | ENG |
| B3 | 5× **WeaponDataSO** | P0 | ENG |
| B4 | 16× **AbilityDataSO** | P0 | ENG/UX |
| B5 | 1× **MomentumConfigSO** | P0 | ENG |
| B6 | **Validate** | P0 | ENG |
| B7 | **AudioLibrary asset** | P1 | SFX |
| B8 | **FootstepBank asset** | P1 | SFX/TA |

**Details**

- **B1, Generator:** Menu **`Carrot Clash → Generate Default Data Assets`** creates all of B2-B5 with doc-exact numbers.
- **B2, Characters:** Carrot / Jalapeño / Broccoli / Potato. Auto-generated. Verify HP/speed against [/characters](/characters).
- **B3, Weapons:** The Nub, Pepper Blaster, The Stem, Root Cannon, The Pip. Auto-generated.
- **B4, Abilities:** Names must match `AbilityFactory` map exactly. Auto-generated. Assign `icon` sprites (→ E3) and `sfxActivate/sfxImpact/sfxLoop` keys (→ D).
- **B5, Momentum config:** 4-tier table (speed `0/.1/.2/.3`, cd `0/.1/.2/.3`, dmg `0/0/.1/.2`, killscore `5/5/5/8`, tier colours). Auto-generated.
- **B6, Validate:** Menu **`Carrot Clash → Validate Data`**, then check the Console is clean.
- **B7, AudioLibrary:** `Create → Carrot Clash → Audio Library`. Populate with clips keyed per section D (missing keys safely no-op).
- **B8, FootstepBank:** Maps PhysicMaterial name → SurfaceType. Create and fill for stone/wood/metal/dirt surfaces.

---

## C. Prefabs

P0 / P1. <span class="cc-status pending">Editor-pending</span>. All prefabs below are unbuilt; the components they host are implemented.

### C1. Player prefab (P0, the keystone)

Structure per [/dev/architecture](/dev/architecture) → "Player Prefab Structure". Build once; make class variants via **data**, not separate prefabs.

| Element | Pri | Owner |
|---|---|---|
| Root GameObject | P0 | ENG |
| `PlayerController` refs | P0 | ENG |
| CameraRig child | P0 | ENG |
| WeaponCamera (optional) | P2 | TA |
| Muzzle transform | P1 | ENG |
| Hitboxes | P0 | ENG |
| Body mesh | P1 | ART |
| Class material/tint | P1 | ART |
| NetworkObject + mirrors | P2 | ENG |

**Requirements**

- **Root GameObject:** On layer **Player**. Components: `CharacterController`, `PlayerController`, `PlayerMovement`, `WeaponController`, `AbilityController`, `HealthController`, `MomentumController`.
- **`PlayerController` refs:** Assign `movement`, `cam`, `weapon`, `abilities`, `health`, `momentum`, `abilityOrigin1`, `abilityOrigin2`, `headTransform`. Drag a `CharacterDataSO` and the `MomentumConfigSO`.
- **CameraRig child:** Child transform (pitch pivot) plus child `Camera` (main FP). Assign both on `PlayerCamera` (`cameraRig`, `mainCamera`). Local player only.
- **WeaponCamera:** Second camera rendering the weapon model on top to avoid clipping. Optional.
- **Muzzle transform:** Assign `muzzle` on `WeaponController` (fire origin plus muzzle flash anchor).
- **Hitboxes:** Head and body colliders with `PlayerHitbox`; tick **Is Head** on head. Set on layer **Hitbox**.
- **Body mesh:** Visible third-person model (remote players). Placeholder capsule is fine for P0.
- **Class material/tint:** Per-class colour (orange/red/green/brown) driven by `primaryColor`.
- **NetworkObject + mirrors:** Only when NGO is installed: `NetworkObject` plus the `Networked*` components from `Assets/_Game/Network/`.

### C2. Ability VFX prefabs

Assigned to `AbilityDataSO.effectPrefab` / `activationVfx`. Placeholder particles are fine for P0; real VFX is P2.

| Prefab | Used by | Pri | Owner |
|---|---|---|---|
| Dash trail / speed lines | Ability_SprintDash | P2 | TA |
| Radar pulse ring | Ability_RadarPulse | P1 | TA |
| Spice grenade + cloud | Ability_SpiceBurst | P1 | TA |
| Heat trail segment | Ability_HeatTrail | P0 | ENG/TA |
| Leaf shield object | Ability_LeafShield | P0 | ENG/TA |
| Spore cloud volume | Ability_SporeCloud | P1 | TA |
| Starch armor VFX | Ability_StarchArmor | P2 | TA |
| Earthen slam decal + burst | Ability_EarthenSlam | P2 | TA |
| Burn aura | Passive_BurnStreak | P2 | TA |
| Generic shatter VFX | MomentumRingUI | P2 | TA |

**Notes**

- **Dash trail:** leaf/dust burst at origin and destination.
- **Radar pulse ring:** expanding ring plus enemy outline.
- **Spice grenade + cloud:** throw arc plus impact mist.
- **Heat trail segment:** **must have a trigger Collider** (DamageZone added by code). Fire particles.
- **Leaf shield object:** destructible cover prefab; needs a collider. `DestructibleCover` is added by code; 1×2 m, readable green.
- **Spore cloud volume:** opaque fog volume, 6 m, vision-block; trigger collider.
- **Starch armor VFX:** yellow outline/burst.
- **Earthen slam:** scorch decal, dirt burst.
- **Burn aura:** fire aura at victim death position.
- **Generic shatter VFX:** ring-fragment particles on death (`shatterVfxPrefab`).

### C3. System prefabs / scene singletons

| Prefab/Object | Lives in | Pri | Owner |
|---|---|---|---|
| AudioManager | Boot (DontDestroyOnLoad) | P0 | ENG/SFX |
| GameModeManager | Gameplay_Market | P0 | ENG |
| SpawnManager | Gameplay_Market | P0 | ENG/LD |
| MatchInitializer | Gameplay_Market | P0 | ENG |
| PlayerSpawner | Gameplay_Market | P0 | ENG |
| BotSpawner | Gameplay_Market | P1 | ENG |

**Components**

- **AudioManager:** `AudioManager` plus assigned `AudioMixer`, mixer groups, `AudioLibrary`.
- **GameModeManager:** `GameModeManager` (plus zone refs).
- **SpawnManager:** `SpawnManager` plus `SpawnPoint`s.
- **MatchInitializer:** `MatchInitializer` (wires spawner/HUD/bots).
- **PlayerSpawner:** `PlayerSpawner` (plus player prefab ref).
- **BotSpawner:** `BotSpawner` (plus prefab, data list, momentum cfg).

### C4. Target dummy (P0, earliest testability)

`TargetDummy` on a collider (layer Player or Hitbox) with `PlayerHitbox`. Dies and resets after 3 s. Lets you smoke-test shooting and momentum before the map exists. **P0 · ENG.**

---

## D. Audio assets

<span class="cc-status partial">Partial</span> P1, the keys; clips can land incrementally. All resolved by key through `AudioLibrary`. **Missing keys silently no-op**, so the game runs without audio; fill over time. Add each clip as a variant under its key in the AudioLibrary asset (B7).

### D1. Exact keys referenced in code (literal, must match)

**Weapon (per weapon name)**

| Key | Trigger | Pri | Owner |
|---|---|---|---|
| `weapon_{WeaponName}_fire` | per weapon fire | P1 | SFX |
| `weapon_{WeaponName}_reload` | per weapon reload | P1 | SFX |
| `impact_surface` / `impact_air` | bullet hit wall / miss | P2 | SFX |

The 5 weapon names: `weapon_The Nub_fire`, `weapon_Pepper Blaster_fire`, `weapon_The Stem_fire`, `weapon_Root Cannon_fire`, `weapon_The Pip_fire` (and the matching `_reload` keys).

**Momentum & abilities**

| Key | Trigger | Pri | Owner |
|---|---|---|---|
| `momentum_tier1` / `tier2` / `tier3` | tier-up chimes (ascending) | P1 | SFX |
| `momentum_lost` | tier decayed | P2 | SFX |
| `momentum_absorb` | killed a high-tier player | P2 | SFX |
| `spice_slow_apply` | Spice Burst slow lands | P2 | SFX |
| `starch_activate` | Starch Armor on | P2 | SFX |

**Match & UI**

| Key | Trigger | Pri | Owner |
|---|---|---|---|
| `zone_capture_complete` | zone captured | P1 | SFX |
| `zone_c_unlock` | minute-5 unlock (kitchen bell) | P1 | SFX |
| `warn_1min` / `warn_30s` | timer warnings | P2 | SFX |
| `ui_button_hover` / `ui_button_confirm` | menu | P1 | UX/SFX |
| `ui_xp_tick` | post-match XP count-up | P2 | SFX |
| `ui_challenge_complete` | daily done | P2 | SFX |
| `footstep_{surface}` | `footstep_stone/wood/metal/dirt` | P1 | SFX |

::: info Ability cue keys
Ability `sfxActivate/Impact/Loop` keys are designer-chosen on each `AbilityDataSO`. Pick names and add matching clips (e.g. `dash_whoosh`, `heat_trail_loop`). 18 ability cues total per [/audio](/audio).
:::

### D2. Music + ambience

| Asset | Pri | Owner |
|---|---|---|
| Music: Base / Intensity / Tier3 stems | P1 | SFX |
| AudioMixer | P1 | SFX/ENG |
| Ambient loops × 5 zones | P2 | SFX |

**Details**

- **Music stems:** looping; assigned on `MusicDirector`.
- **AudioMixer:** exposed params `MasterVolume`, `MusicVolume`, `SfxVolume`, `VoiceVolume`, `intensity`, `tier3`; groups: SFX, UI, Music, Ambience.
- **Ambient loops:** Courtyard / Indoor / Stage / Alley / Cellar; assigned on `AmbientZone` trigger volumes.

The full target inventory (~90 files) is in [/audio](/audio).

---

## E. UI assets & scene wiring

<span class="cc-status partial">Partial</span> P1, lots of serialized refs. The HUD/menu **scripts exist**; they need a Canvas hierarchy with every serialized `Image` / `TMP_Text` / `RectTransform` / `CanvasGroup` / `Button` assigned. Build the prefabs/canvases and drag refs.

### E1. GameplayUI canvas (HUD), Screen Space Overlay

| Widget (script) | Pri | Owner |
|---|---|---|
| `PlayerHUD` (root) | P1 | UX |
| `HealthBarUI` | P1 | UX |
| `MomentumRingUI` | P1 | UX |
| `AbilitySlotUI` ×2 | P1 | UX |
| `AmmoUI` | P1 | UX |
| `ScoreBarUI` | P1 | UX |
| `ZoneIndicatorUI` | P1 | UX |
| `CrosshairUI` | P1 | UX |
| `KillFeedUI` + `KillFeedEntry` | P1 | UX |
| `HitMarkerUI` | P2 | UX |
| `DamageDirectionIndicator` | P2 | UX |
| `FeedbackController` / `ScreenEffects` / `OnFireOverlay` | P2 | UX |

**Serialized refs each widget needs**

- **`PlayerHUD` (root):** refs to all widgets below.
- **`HealthBarUI`:** `fillImage`, `tempFillImage`, HP `TMP_Text`, low-HP pulse target.
- **`MomentumRingUI`:** `ringFill` Image, `ringGroup` CanvasGroup, `shatterVfxPrefab`.
- **`AbilitySlotUI` ×2:** `iconImage`, `cooldownOverlay`, `cooldownLabel`, key label.
- **`AmmoUI`:** `ammoLabel`, `reloadSpinner` RectTransform.
- **`ScoreBarUI`:** `fillA`, `fillB`, score and timer `TMP_Text`.
- **`ZoneIndicatorUI`:** 3 zone icon Images (A/B/C) plus lock icon.
- **`CrosshairUI`:** `dot`, `lineTop/Bottom/Left/Right` RectTransforms.
- **`KillFeedUI` + `KillFeedEntry`:** `entryParent`, entry prefab (`killerIcon`, `victimIcon`, `border`).
- **`HitMarkerUI`:** `marker` Image.
- **`DamageDirectionIndicator`:** `arcTemplate` RectTransform, `arrow` Image.
- **`FeedbackController` / `ScreenEffects` / `OnFireOverlay`:** `lowHealthVignette`, `fade`, `flashOverlay`, `vignette` CanvasGroups/Images.

### E2. Menu screens (SlimUI base where useful)

| Screen (script) | Pri | Owner |
|---|---|---|
| `MainMenuController` | P1 | UX |
| `ClassSelectController` | P1 | UX |
| `PostMatchController` | P1 | UX |
| `SettingsMenuController` | P1 | UX |

**What each needs**

- **`MainMenuController`:** Play/Party/Progression/Settings/Quit buttons, level/XP labels, daily and battle-pass summary labels.
- **`ClassSelectController`:** 4 class buttons, stat/ability labels (`classNameLabel`, `active1Label`, `active2Label`…), countdown, CharacterDataSO array.
- **`PostMatchController`:** winner banner, MVP/Hot-Streak cards, scoreboard rows, animated XP labels, battle-pass bar.
- **`SettingsMenuController`:** tab buttons plus panels; sliders/toggles/dropdowns for **every** `SettingsService` field (FOV, sens, volumes, colourblind dropdown, reduce-motion, text size).

### E3. Icons & 2D art

| Asset | Pri | Owner |
|---|---|---|
| 4 class icons | P1 | ART |
| 16 ability icons | P1 | ART |
| 3 zone icons + lock | P1 | ART |
| Crosshair sprites | P2 | UX |
| Hit-marker sprites | P2 | UX |
| Logo / menu background | P2 | ART |

**Details**

- **4 class icons:** 🥕🌶🥦🥔 for kill feed, class select, scoreboard.
- **16 ability icons:** one per `AbilityDataSO.icon`.
- **3 zone icons + lock:** A/B/C ownership states.
- **Crosshair sprites:** dot/cross/circle options.
- **Hit-marker sprites:** white X / gold X / yellow-tint.
- **Logo / menu background:** main menu.

---

## F. Level: The Grand Food Market

<span class="cc-status pending">Editor-pending</span> P1, the biggest art/LD lift. Per [/map](/map). Grey-box first (P0-ish for playability), art pass later.

| # | Item | Pri | Owner |
|---|---|---|---|
| F1 | **Grey-box geometry** | P1 | LD |
| F2 | Zone A - Courtyard | P1 | LD |
| F3 | Zone B - Indoor Market | P1 | LD |
| F4 | Zone C - Central Stage | P1 | LD |
| F5 | Flank routes | P1 | LD |
| F6 | Spawn points | P0 | LD |
| F7 | **NavMesh bake** | P1 | LD/ENG |
| F8 | Surface PhysicMaterials | P2 | TA |
| F9 | Destructible stalls | P2 | TA |
| F10 | Lighting + ambient zones | P2 | TA |
| F11 | Art pass | P2 | ART |

**Details**

- **F1, Grey-box geometry:** 3 zones, 2 spawns, 3 flank routes. Blockout only.
- **F2, Zone A (Courtyard):** ~40×30 m open, 3 lanes, 8 destructible stalls plus 4 carts. `CaptureZone` (radius 6, 10 s, tick 1, lock 0).
- **F3, Zone B (Indoor Market):** ~30×25 m enclosed, 4 entries, mezzanine. `CaptureZone` (radius 4, 8 s, tick 1, lock 0).
- **F4, Zone C (Central Stage):** ~20 m circular raised 3 m. `CaptureZone` (radius 8, 12 s, tick 2, **lock 300**). Physical barrier objects that lower at unlock.
- **F5, Flank routes:** Alley East, Rooftop Catwalk (one-way drop), Underground Cellar.
- **F6, Spawn points:** 3 per team behind cover, `SpawnPoint` (team A/B), 60 m apart.
- **F7, NavMesh bake:** AI → bake on finished blockout; NavMesh links for the catwalk drop and cellar stairs. Bots need this.
- **F8, Surface PhysicMaterials:** stone/wood/metal/dirt assigned to surfaces for footsteps (matches FootstepBank).
- **F9, Destructible stalls:** 80 HP cover (`DestructibleCover` or equivalent).
- **F10, Lighting + ambient zones:** per-zone palette (warm/cool/neutral) plus `AmbientZone` trigger volumes.
- **F11, Art pass:** awnings, crates, stage dressing, food-market theme.

---

## G. Scenes

<span class="cc-status pending">Editor-pending</span> P0, assemble the above. No scenes exist in the repo yet.

| Scene | Pri | Owner |
|---|---|---|
| `Boot` | P0 | ENG |
| `MainMenu` | P1 | UX |
| `Gameplay_Market` | P0 | ENG/LD |
| `GameplayUI` | P1 | UX |
| `TutorialScene` (optional) | P2 | LD/ENG |

**Must contain**

- **`Boot`:** `GameBootstrap`, AudioManager prefab. Applies settings, then loads MainMenu.
- **`MainMenu`:** `MainMenuController` canvas plus SlimUI base.
- **`Gameplay_Market`:** the map (F), GameModeManager, SpawnManager, MatchInitializer, PlayerSpawner, BotSpawner.
- **`GameplayUI`:** HUD canvas (E1), loaded additively by `SceneFlow.LoadGameplay`.
- **`TutorialScene`:** subset of Zone A, scripted dummy, FTUE steps. Optional.

---

## H. Build / platform

<span class="cc-status pending">Editor-pending</span> P2, before external playtest.

| # | Item | Pri | Owner |
|---|---|---|---|
| H1 | PC build profile (IL2CPP), 60 fps target | P2 | ENG |
| H2 | Mobile build (Android first), 30 fps, touch HUD layout (see [/ui-ux](/ui-ux) mobile) | P2 | ENG |
| H3 | Graphy overlay in dev builds only | P2 | ENG |
| H4 | (NGO) Relay/Lobby + ConnectionManager test across 2 networks | P2 | ENG |

::: warning Netcode dormant
The network layer is implemented behind `NETCODE_PRESENT`, but Netcode for GameObjects is **not installed yet**. H4 (and the C1 NetworkObject mirrors) is blocked until that package plus Relay/Lobby land.
:::

---

## Suggested division (5-person team)

- **Engineer 1 (gameplay):** A1-A6, B1-B6, C1 player prefab, C3 system objects, C4 dummy, G Boot/Gameplay scenes. *(unblocks everyone)*
- **Engineer 2 (net/build):** Network mirror wiring plus NGO install, H1-H4, input-action additions (A6).
- **Technical Artist:** C2 ability VFX, F7-F10 (NavMesh, materials, destructibles, lighting), B8 FootstepBank.
- **UI/UX:** E1 HUD, E2 menus, E3 icons coordination, MainMenu/GameplayUI scenes.
- **Level Designer + Artist:** F1-F6 plus F11 map; **Audio** (can be contractor): B7, D1-D2.

::: tip Critical path to a playable test
A1-A4 → B1 → C1 + C4 → minimal Gameplay scene → shoot a dummy, watch momentum. Everything else layers on from there.
:::
