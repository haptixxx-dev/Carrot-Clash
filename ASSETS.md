# Carrot Clash - Editor-Authored Asset & Wiring Checklist

Everything that must be created **inside the Unity editor** to make the committed code playable. The
C# is done; this is the content + wiring layer it expects. Built from what the scripts actually
reference (serialized fields, SFX keys, layer names, scene names) - so it's exhaustive and pedantic
on purpose.

**How to use this:** each section has an **Owner** column - assign a person/discipline. Items are
ordered so a clean vertical-slice path emerges: do **P0 (blockers)** first, then P1, then P2 polish.
Check the box when the asset exists in the project AND is wired to its script.

Legend - **Priority:** P0 = nothing runs without it · P1 = needed for a full match · P2 = polish/feel.
**Disipline:** ENG = engineer · TA = technical artist · ART = artist · SFX = audio · LD = level design · UX = UI/UX.

> Cross-references: component structure → `docs/tech-architecture.md`; HUD/menus → `docs/ui-ux.md`;
> feel/VFX → `docs/game-feel.md`; SFX spec → `docs/audio.md`; map → `docs/map.md`. Wiring order and
> smoke tests → `Assets/_Game/SETUP.md`.

---

## A. Project configuration (P0 - do first, ~30 min)

| # | Item | Detail / exact values | Pri | Owner |
|---|---|---|---|---|
| A1 | **Layers** | Create 4 layers, EXACT spelling: `Player`, `Environment`, `Ability`, `Hitbox`. Code raycasts these by name (`GameConstants`). | P0 | ENG |
| A2 | **Physics collision matrix** | Player↔Environment collide; Hitbox layer set on player body colliders; Ability layer for ability volumes (triggers). Disable friendly Player↔Player if undesired. | P0 | ENG |
| A3 | **Tags** | Optional but recommended: none hard-required by code (team is data-driven). Skip unless your prefabs need them. | P2 | ENG |
| A4 | **Build Settings scenes** | Add scenes in order: `Boot`, `MainMenu`, `Gameplay_Market`, `GameplayUI`. Remove `SampleScene`. | P0 | ENG |
| A5 | **Project quality / URP** | PC + Mobile URP profiles already exist (`Assets/Settings/`). Confirm Quality levels map to them; `SettingsService.ApplyVideoSettings` calls `SetQualityLevel`. | P1 | TA |
| A6 | **Input Actions** | `Assets/InputSystem_Actions.inputactions` is MISSING actions: **Reload, ADS, Ability1, Ability2, SwapWeapon**. Either add them to the `Player` map (preferred) or rely on `PlayerInputBinder`'s hardcoded fallback (R / RightMouse / Q / E). | P1 | ENG |

---

## B. Data assets (P0 - mostly one click)

| # | Item | Detail | Pri | Owner |
|---|---|---|---|---|
| B1 | **Run the generator** | Menu **`Carrot Clash → Generate Default Data Assets`** - creates all of B2-B5 with doc-exact numbers. | P0 | ENG |
| B2 | 4× **CharacterDataSO** | Carrot / Jalapeño / Broccoli / Potato. Auto-generated. Verify HP/speed vs `docs/characters.md`. | P0 | ENG |
| B3 | 5× **WeaponDataSO** | The Nub, Pepper Blaster, The Stem, Root Cannon, The Pip. Auto-generated. | P0 | ENG |
| B4 | 16× **AbilityDataSO** | Names must match `AbilityFactory` map exactly. Auto-generated. Assign `icon` sprites (→ E-section) + `sfxActivate/sfxImpact/sfxLoop` keys (→ D). | P0 | ENG/UX |
| B5 | 1× **MomentumConfigSO** | 4-tier table (speed 0/.1/.2/.3, cd 0/.1/.2/.3, dmg 0/0/.1/.2, killscore 5/5/5/8, tier colours). Auto-generated. | P0 | ENG |
| B6 | **Validate** | Menu **`Carrot Clash → Validate Data`** → Console clean. | P0 | ENG |
| B7 | **AudioLibrary asset** | `Create → Carrot Clash → Audio Library`. Populate with clips keyed per section D (missing keys safely no-op). | P1 | SFX |
| B8 | **FootstepBank asset** | Maps PhysicMaterial name → SurfaceType. Create + fill for stone/wood/metal/dirt surfaces. | P1 | SFX/TA |

---

## C. Prefabs (P0/P1)

### C1. Player prefab (P0 - the keystone)
Structure per `docs/tech-architecture.md` → "Player Prefab Structure". Build once, make class variants
via data, not separate prefabs.

| Element | Requirement | Pri | Owner |
|---|---|---|---|
| Root GameObject | On layer **Player**. Components: `CharacterController`, `PlayerController`, `PlayerMovement`, `WeaponController`, `AbilityController`, `HealthController`, `MomentumController`. | P0 | ENG |
| `PlayerController` refs | Assign `movement`, `cam`, `weapon`, `abilities`, `health`, `momentum`, `abilityOrigin1`, `abilityOrigin2`, `headTransform`. Drag a `CharacterDataSO` + the `MomentumConfigSO`. | P0 | ENG |
| CameraRig child | Child transform (pitch pivot) + child `Camera` (main FP). Assign both on `PlayerCamera` (`cameraRig`, `mainCamera`). Local player only. | P0 | ENG |
| WeaponCamera (optional) | Second camera rendering weapon model on top to avoid clipping. | P2 | TA |
| Muzzle transform | Assign `muzzle` on `WeaponController` (fire origin + muzzle flash anchor). | P1 | ENG |
| Hitboxes | Head + body colliders with `PlayerHitbox`; tick **Is Head** on head. Set on layer **Hitbox**. | P0 | ENG |
| Body mesh | Visible third-person model (remote players). Placeholder capsule OK for P0. | P1 | ART |
| Class material/tint | Per-class colour (orange/red/green/brown) driven by `primaryColor`. | P1 | ART |
| NetworkObject + mirrors | (only when NGO installed) `NetworkObject` + the `Networked*` components from `Assets/_Game/Network/`. | P2 | ENG |

### C2. Ability VFX prefabs
Assigned to `AbilityDataSO.effectPrefab` / `activationVfx`. Placeholder particles fine for P0; real VFX P2.

| Prefab | Used by | Notes | Pri | Owner |
|---|---|---|---|---|
| Dash trail / speed lines | Ability_SprintDash | leaf/dust burst at origin+dest | P2 | TA |
| Radar pulse ring | Ability_RadarPulse | expanding ring + enemy outline | P1 | TA |
| Spice grenade + cloud | Ability_SpiceBurst | throw arc + impact mist | P1 | TA |
| Heat trail segment | Ability_HeatTrail | **must have a trigger Collider** (DamageZone added by code). Fire particles. | P0 | ENG/TA |
| Leaf shield object | Ability_LeafShield | destructible cover prefab; needs collider + `DestructibleCover` is added by code; 1×2m, readable green | P0 | ENG/TA |
| Spore cloud volume | Ability_SporeCloud | opaque fog volume, 6m, vision-block; trigger collider | P1 | TA |
| Starch armor VFX | Ability_StarchArmor | yellow outline/burst | P2 | TA |
| Earthen slam decal + burst | Ability_EarthenSlam | scorch decal, dirt burst | P2 | TA |
| Burn aura | Passive_BurnStreak | fire aura at victim death pos | P2 | TA |
| Generic shatter VFX | MomentumRingUI (`shatterVfxPrefab`) | ring-fragment particles on death | P2 | TA |

### C3. System prefabs / scene singletons
| Prefab/Object | Components | Lives in | Pri | Owner |
|---|---|---|---|---|
| AudioManager | `AudioManager` + assigned `AudioMixer`, mixer groups, `AudioLibrary` | Boot (DontDestroyOnLoad) | P0 | ENG/SFX |
| GameModeManager | `GameModeManager` (+ zone refs) | Gameplay_Market | P0 | ENG |
| SpawnManager | `SpawnManager` + `SpawnPoint`s | Gameplay_Market | P0 | ENG/LD |
| MatchInitializer | `MatchInitializer` (wires spawner/HUD/bots) | Gameplay_Market | P0 | ENG |
| PlayerSpawner | `PlayerSpawner` (+ player prefab ref) | Gameplay_Market | P0 | ENG |
| BotSpawner | `BotSpawner` (+ prefab, data list, momentum cfg) | Gameplay_Market | P1 | ENG |

### C4. Target dummy (P0 - earliest testability)
`TargetDummy` on a collider (layer Player or Hitbox) with `PlayerHitbox`. Dies + resets after 3s. Lets
you smoke-test shooting + momentum before the map exists. | P0 | ENG |

---

## D. Audio assets (P1 - keys; clips can land incrementally)

All resolved by key through `AudioLibrary`. **Missing keys silently no-op**, so the game runs without
audio - fill over time. Add each clip as a variant under its key in the AudioLibrary asset (B7).

### D1. Exact keys referenced in code (literal - must match)
| Key | Trigger | Pri | Owner |
|---|---|---|---|
| `weapon_{WeaponName}_fire` | per weapon: `weapon_The Nub_fire`, `weapon_Pepper Blaster_fire`, `weapon_The Stem_fire`, `weapon_Root Cannon_fire`, `weapon_The Pip_fire` | P1 | SFX |
| `weapon_{WeaponName}_reload` | same 5 weapon names | P1 | SFX |
| `impact_surface` / `impact_air` | bullet hit wall / miss | P2 | SFX |
| `momentum_tier1` / `tier2` / `tier3` | tier-up chimes (ascending family) | P1 | SFX |
| `momentum_lost` | tier decayed | P2 | SFX |
| `momentum_absorb` | killed a high-tier player | P2 | SFX |
| `spice_slow_apply` | Spice Burst slow lands | P2 | SFX |
| `starch_activate` | Starch Armor on | P2 | SFX |
| `zone_capture_complete` | zone captured | P1 | SFX |
| `zone_c_unlock` | minute-5 unlock (kitchen bell) | P1 | SFX |
| `warn_1min` / `warn_30s` | timer warnings | P2 | SFX |
| `ui_button_hover` / `ui_button_confirm` | menu | P1 | UX/SFX |
| `ui_xp_tick` | post-match XP count-up | P2 | SFX |
| `ui_challenge_complete` | daily done | P2 | SFX |
| `footstep_{surface}` | `footstep_stone/wood/metal/dirt` | P1 | SFX |

> Ability `sfxActivate/Impact/Loop` keys are designer-chosen on each `AbilityDataSO` - pick names and
> add matching clips (e.g. `dash_whoosh`, `heat_trail_loop`). 18 ability cues total per `docs/audio.md`.

### D2. Music + ambience
| Asset | Detail | Pri | Owner |
|---|---|---|---|
| Music: Base / Intensity / Tier3 stems | looping; assigned on `MusicDirector` | P1 | SFX |
| AudioMixer | exposed params `MasterVolume`, `MusicVolume`, `SfxVolume`, `VoiceVolume`, `intensity`, `tier3`; groups: SFX, UI, Music, Ambience | P1 | SFX/ENG |
| Ambient loops × 5 zones | Courtyard / Indoor / Stage / Alley / Cellar; assigned on `AmbientZone` trigger volumes | P2 | SFX |

Full target inventory (~90 files) is in `docs/audio.md`.

---

## E. UI assets & scene wiring (P1 - lots of serialized refs)

The HUD/menu **scripts exist**; they need a Canvas hierarchy with every serialized `Image`/`TMP_Text`/
`RectTransform`/`CanvasGroup`/`Button` assigned. Build the prefabs/canvases and drag refs.

### E1. GameplayUI canvas (HUD) - Screen Space Overlay
| Widget (script) | Serialized refs it needs | Pri | Owner |
|---|---|---|---|
| `PlayerHUD` (root) | refs to all widgets below | P1 | UX |
| `HealthBarUI` | `fillImage`, `tempFillImage`, HP `TMP_Text`, low-HP pulse target | P1 | UX |
| `MomentumRingUI` | `ringFill` Image, `ringGroup` CanvasGroup, `shatterVfxPrefab` | P1 | UX |
| `AbilitySlotUI` ×2 | `iconImage`, `cooldownOverlay`, `cooldownLabel`, key label | P1 | UX |
| `AmmoUI` | `ammoLabel`, `reloadSpinner` RectTransform | P1 | UX |
| `ScoreBarUI` | `fillA`, `fillB`, score + timer `TMP_Text` | P1 | UX |
| `ZoneIndicatorUI` | 3 zone icon Images (A/B/C) + lock icon | P1 | UX |
| `CrosshairUI` | `dot`, `lineTop/Bottom/Left/Right` RectTransforms | P1 | UX |
| `KillFeedUI` + `KillFeedEntry` | `entryParent`, entry prefab (`killerIcon`, `victimIcon`, `border`) | P1 | UX |
| `HitMarkerUI` | `marker` Image | P2 | UX |
| `DamageDirectionIndicator` | `arcTemplate` RectTransform, `arrow` Image | P2 | UX |
| `FeedbackController` / `ScreenEffects` / `OnFireOverlay` | `lowHealthVignette`, `fade`, `flashOverlay`, `vignette` CanvasGroups/Images | P2 | UX |

### E2. Menu screens (SlimUI base where useful)
| Screen (script) | Needs | Pri | Owner |
|---|---|---|---|
| `MainMenuController` | Play/Party/Progression/Settings/Quit buttons, level/XP labels, daily + battle-pass summary labels | P1 | UX |
| `ClassSelectController` | 4 class buttons, stat/ability labels (`classNameLabel`, `active1Label`, `active2Label`...), countdown, CharacterDataSO array | P1 | UX |
| `PostMatchController` | winner banner, MVP/Hot-Streak cards, scoreboard rows, animated XP labels, battle-pass bar | P1 | UX |
| `SettingsMenuController` | tab buttons + panels; sliders/toggles/dropdowns for EVERY `SettingsService` field (FOV, sens, volumes, colourblind dropdown, reduce-motion, text size) | P1 | UX |

### E3. Icons & 2D art
| Asset | Detail | Pri | Owner |
|---|---|---|---|
| 4 class icons | 🥕🌶🥦🥔 - kill feed, class select, scoreboard | P1 | ART |
| 16 ability icons | one per `AbilityDataSO.icon` | P1 | ART |
| 3 zone icons + lock | A/B/C ownership states | P1 | ART |
| Crosshair sprites | dot/cross/circle options | P2 | UX |
| Hit-marker sprites | white X / gold X / yellow-tint | P2 | UX |
| Logo / menu background | main menu | P2 | ART |

---

## F. Level - The Grand Food Market (P1 - biggest art/LD lift)

Per `docs/map.md`. Grey-box first (P0-ish for playability), art pass later.

| # | Item | Detail | Pri | Owner |
|---|---|---|---|---|
| F1 | **Grey-box geometry** | 3 zones + 2 spawns + 3 flank routes. Blockout only. | P1 | LD |
| F2 | Zone A - Courtyard | ~40×30m open, 3 lanes, 8 destructible stalls + 4 carts. `CaptureZone` (radius 6, 10s, tick 1, lock 0). | P1 | LD |
| F3 | Zone B - Indoor Market | ~30×25m enclosed, 4 entries, mezzanine. `CaptureZone` (radius 4, 8s, tick 1, lock 0). | P1 | LD |
| F4 | Zone C - Central Stage | ~20m circular raised 3m. `CaptureZone` (radius 8, 12s, tick 2, **lock 300**). Physical barrier objects that lower at unlock. | P1 | LD |
| F5 | Flank routes | Alley East, Rooftop Catwalk (one-way drop), Underground Cellar. | P1 | LD |
| F6 | Spawn points | 3 per team behind cover, `SpawnPoint` (team A/B), 60m apart. | P0 | LD |
| F7 | **NavMesh bake** | AI → bake on finished blockout; NavMesh links for catwalk drop + cellar stairs. Bots need this. | P1 | LD/ENG |
| F8 | Surface PhysicMaterials | stone/wood/metal/dirt assigned to surfaces → footsteps (matches FootstepBank). | P2 | TA |
| F9 | Destructible stalls | 80 HP cover (`DestructibleCover` or equivalent). | P2 | TA |
| F10 | Lighting + ambient zones | per-zone palette (warm/cool/neutral) + `AmbientZone` trigger volumes. | P2 | TA |
| F11 | Art pass | awnings, crates, stage dressing, food-market theme. | P2 | ART |

---

## G. Scenes (P0 - assemble the above)

| Scene | Must contain | Pri | Owner |
|---|---|---|---|
| `Boot` | `GameBootstrap`, AudioManager prefab. Applies settings → loads MainMenu. | P0 | ENG |
| `MainMenu` | `MainMenuController` canvas + SlimUI base. | P1 | UX |
| `Gameplay_Market` | The map (F), GameModeManager, SpawnManager, MatchInitializer, PlayerSpawner, BotSpawner. | P0 | ENG/LD |
| `GameplayUI` | HUD canvas (E1), loaded additively by `SceneFlow.LoadGameplay`. | P1 | UX |
| `TutorialScene` (optional) | subset of Zone A, scripted dummy, FTUE steps. | P2 | LD/ENG |

---

## H. Build / platform (P2 - before external playtest)

| # | Item | Pri | Owner |
|---|---|---|---|
| H1 | PC build profile (IL2CPP), 60fps target | P2 | ENG |
| H2 | Mobile build (Android first), 30fps, touch HUD layout (`docs/ui-ux.md` mobile) | P2 | ENG |
| H3 | Graphy overlay in dev builds only | P2 | ENG |
| H4 | (NGO) Relay/Lobby + ConnectionManager test across 2 networks | P2 | ENG |

---

## Suggested division (5-person team)

- **Engineer 1 (gameplay):** A1-A6, B1-B6, C1 player prefab, C3 system objects, C4 dummy, G Boot/Gameplay scenes. *(unblocks everyone)*
- **Engineer 2 (net/build):** Network mirror wiring + NGO install, H1-H4, input-action additions (A6).
- **Technical Artist:** C2 ability VFX, F7-F10 (NavMesh, materials, destructibles, lighting), B8 FootstepBank.
- **UI/UX:** E1 HUD, E2 menus, E3 icons coordination, MainMenu/GameplayUI scenes.
- **Level Designer + Artist:** F1-F6 + F11 map; **Audio** (can be contractor): B7, D1-D2.

**Critical path to a playable test:** A1-A4 → B1 → C1 + C4 → minimal Gameplay scene → shoot a dummy,
watch momentum. Everything else layers on from there.
