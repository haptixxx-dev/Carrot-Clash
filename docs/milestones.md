# Build Milestones

Target: a shippable vertical slice. One map, four classes, online multiplayer, all core systems working.

::: info CODE STATUS (2026-06)
The gameplay code for Phases 1-5 now exists: 97 C# scripts under `Assets/_Game/`, all compiling against the frozen API in `Assets/_Game/CONTRACTS.md` (repo root; mirrored at [/dev/architecture](/dev/architecture)). What is *not* done is the editor-authored content: scenes, the player prefab, the map (grey-box plus art plus NavMesh), UI canvases, every audio clip, icons, and VFX. The Netcode for GameObjects package is also not yet installed, so the network layer is dormant behind `NETCODE_PRESENT`.

So the milestone checklists below are marked from the code perspective. A box being checked means *the script that satisfies it exists and is verified*. It does **not** mean the milestone is playable yet, because that needs scenes, prefabs, and art. Engineers should start at [/dev/getting-started](/dev/getting-started) and treat [CONTRACTS.md](/dev/architecture) as the source of truth for the public API.
:::

---

## Phase overview

The duration estimates are the original plan. The code column reflects what is verified in `Assets/_Game/`; the content column is the remaining editor work (scenes, prefabs, art, audio) that gates actual playability.

| Phase | Goal | Code | Content |
|---|---|---|---|
| 1 | Foundation (offline prototype) | <span class="cc-status built">Implemented</span> | <span class="cc-status pending">Editor-pending</span> |
| 2 | All classes + ability system | <span class="cc-status built">Implemented</span> | <span class="cc-status pending">Editor-pending</span> |
| 3 | Full map + game mode | <span class="cc-status partial">Partial</span> | <span class="cc-status pending">Editor-pending</span> |
| 4 | Online multiplayer | <span class="cc-status partial">Partial</span> | <span class="cc-status pending">Editor-pending</span> |
| 5 | Polish (vertical slice) | <span class="cc-status partial">Partial</span> | <span class="cc-status pending">Editor-pending</span> |

Original duration estimate: Phase 1 (2-3 wk), Phase 2 (3-4 wk), Phase 3 (3-4 wk), Phase 4 (3-5 wk), Phase 5 (2-3 wk), so **13-19 weeks total**. The engineering code spine landed ahead of that estimate. The remaining timeline is dominated by content authoring and the NGO integration.

---

## Phase 1: Foundation (offline prototype)

<span class="cc-status built">Implemented</span> (code) &nbsp; <span class="cc-status pending">Editor-pending</span> (scenes/prefab/map)

**Goal:** Prove the core feel. It must be fun alone before adding complexity.

::: tip What's built
Every Phase-1 *system* exists in code and is verified: `PlayerController` orchestrator with `PlayerMovement` (WASD + sprint + crouch + jump on `CharacterController`), `PlayerCamera` (FP mouse-look + FOV surges), `WeaponController` (hitscan + falloff curve + headshots), `MomentumController` (tiers 0-3, decay, transfer), `CaptureZone` + `GameModeManager`, the full `CharacterDataSO` / `WeaponDataSO` / `MomentumConfigSO` data layer, the Carrot kit, and a `TargetDummy`. What is missing is the content. There is no `Boot`/`Gameplay_Market` scene, no player prefab wiring those components together, and no grey-box map. See [/dev/getting-started](/dev/getting-started) for the scene/prefab build order.
:::

**First tasks (start here):**
1. Create `PlayerController` prefab with `CharacterController`, movement, jump, sprint
2. Wire `InputSystem_Actions.inputactions` to player movement and look
3. Implement first-person camera (`PlayerCamera` with mouse look + FOV)
4. Add `WeaponController` with hitscan raycast and hit detection
5. Create `MomentumController` with tiers 0-3, decay coroutine, and `OnTierChanged` event
6. Add placeholder HUD: HP bar + momentum ring (ring = simple filled Image component)
7. Create `CharacterDataSO` for Carrot and `WeaponDataSO` for The Nub
8. Implement `Ability_SprintDash` and `Ability_RadarPulse` for Carrot
9. Create `CaptureZone` with basic trigger-volume capture logic + score tick
10. Create `GameModeManager` with match timer and score tracking
11. Block out the map (grey-box geometry only: Zones A, B, C footprints with spawns)
12. Add a dummy enemy target (static; takes damage; dies; resets after 3s for testing). Done in code as `TargetDummy`.

::: info Input asset delta
`InputSystem_Actions.inputactions` currently lacks `Reload`/`ADS`/`Ability1`/`Ability2`/`SwapWeapon` actions. `PlayerInputBinder` detects this and falls back to hard keys **R** (reload), **Right-Mouse** (ADS), **Q** (ability 1), **E** (ability 2). Adding those actions to the asset is editor work; the binder already handles both paths.
:::

**Deliverables checklist** (✅ = code verified; box left unchecked = needs editor content):
- [x] FPS controller (WASD + mouse look + jump + sprint + crouch): `PlayerMovement` + `PlayerCamera`
- [x] Hitscan weapon with hit confirmation (audio + hit marker): `WeaponController`, `HitMarkerUI`, `FeedbackController`
- [x] Momentum system (tiers 0-3, decay, visual feedback on ring HUD): `MomentumController` + `MomentumRingUI`
- [x] Carrot class (Sprint Dash + Radar Pulse): `Ability_SprintDash`, `Ability_RadarPulse`
- [x] One objective zone (capture logic, score tick visible on HUD): `CaptureZone` + `ScoreBarUI`/`ZoneIndicatorUI`
- [x] Basic HUD (HP bar, momentum ring, score counter, ability cooldowns): full `PlayerHUD` (11 widgets)
- [x] Data layer in place (`CharacterDataSO`, `WeaponDataSO`, `MomentumConfigSO`): generated via the editor menu (below)
- [ ] Placeholder map (3-zone blockout, both spawns, flanking route paths marked): **editor-pending** (no scene yet)
- [ ] Player prefab wiring all components + `Gameplay_Market` / `Boot` scenes: **editor-pending**

::: warning Data assets are generated, not committed
`CharacterDataSO`/`WeaponDataSO`/`MomentumConfigSO` instances are produced by the editor menu **Carrot Clash → Generate Default Data Assets** (`DataAssetGenerator`). The code and generators exist; the generated `.asset` files are not in the repo and must be created, then validated by `DataValidator`.
:::

**Done when:** A solo session is playable. Kill the dummy target, watch momentum tier up to 3, let it decay, capture a zone, watch the score count. The game loop reads clearly. *(Gated on the scene/prefab content, not on code.)*

**Key risk:** CharacterController vs. Rigidbody decision. Use `CharacterController` (not Rigidbody) for FPS. It gives deterministic movement without physics jitter and works cleanly with NGO's `ClientNetworkTransform` later. **Resolved:** `PlayerMovement` requires a `CharacterController`; arcade gravity is `-22` (`GameConstants.Gravity`), not `-9.81`.

---

## Phase 2: All classes + ability system

<span class="cc-status built">Implemented</span> (code) &nbsp; <span class="cc-status pending">Editor-pending</span> (data assets, VFX, class-select art)

**Goal:** All four classes playable with full kits. The ability system must support any future class additions.

**Deliverables** (✅ = code verified):
- [x] `AbilityBase` abstract class + `AbilityController` slot manager (+ `AbilityFactory` name→component map)
- [x] Cooldowns respecting `MomentumController.CooldownMultiplier`: handled inside `AbilityController`
- [x] `EffectSystem` static class (ApplySlow, ApplyFire, ApplyKnockback, ApplyTemporaryHP)
- [x] Jalapeño class (Spice Burst, Heat Trail, Burn Streak, Extended Streak)
- [x] Broccoli class (Leaf Shield, Spore Cloud, Regen Aura, Shared Harvest)
- [x] Potato class (Starch Armor, Earthen Slam, Thick Skin, Stubborn Root)
- [x] Carrot class momentum/passive kit (Silent Steps + Backstab): completes all 16 behaviours
- [x] Class select screen logic (`ClassSelectController`): buttons wired; **art editor-pending**
- [x] Momentum class passives wired up for all 4 (Backstab, Extended Streak, Shared Harvest, Stubborn Root)
- [ ] `CharacterDataSO` assets for all 4 classes: generated by editor menu, **not committed**
- [ ] Per-class VFX / ability-origin indicators: **editor-pending** (prefab `effectPrefab`/`activationVfx` slots empty)

::: info Ability count delta
The docs frame this as "8 active abilities + 4 momentum passives". The code ships **16 ability behaviours total**: per class one `Active1`, one `Active2`, one always-on `Passive`, and one `MomentumPassive` (= 8 actives + 4 passives + 4 momentum passives). The exact class names are frozen in [CONTRACTS.md](/dev/architecture) and matched by `AbilityFactory`. Note two passives (`Passive_SilentSteps`, `Passive_ThickSkin`) are thin marker components read directly by `PlayerMovement`/`AudioManager` via `ClassId`, by design.
:::

**Done when:** All four classes are selectable, all 8 active abilities function correctly, and all 4 momentum passives produce measurable effects. *(Code path verified; gated on data assets plus a player prefab to attach them to.)*

**Key risk:** Ability interactions, e.g. Spore Cloud blocking Radar Pulse, or Burn Streak + Spice Burst stacking. Define the interaction rules now. See `EffectSystem` in [Tech Architecture](tech-architecture.md) and the dedicated [Ability Interactions](ability-interactions.md) page. **Resolved in code:** interaction rules live in `EffectSystem` plus the relevant `Ability_*`/`Passive_*` impls.

---

## Phase 3: Full map + game mode

<span class="cc-status built">Implemented</span> (match logic) &nbsp; <span class="cc-status pending">Editor-pending</span> (the entire map + scenes)

**Goal:** A complete, playable match from start to finish, offline/LAN.

The match logic is code-complete; the map is not authored. The map deliverables below are pure editor content and stay unchecked.

**Match-logic deliverables** (✅ = code verified):
- [x] `GameModeManager`: full match state machine, score cap + timer + win detection
- [x] Zone C lock/unlock logic with audio announcement hook (`zone_c_unlock` SFX key, `OnZoneCUnlocked`)
- [x] Post-match screen (scores, MVP, Hot Streak, XP counter): `PostMatchController` + `MatchStats` (`ResolveMvp`/`ResolveHotStreak`)
- [x] Team A / Team B spawn systems with safety redirect: `SpawnManager` (auto-respawn on `OnPlayerDied`, `SpawnContestRadius` redirect)
- [x] Zone score UI (three zone ownership indicators on HUD): `ZoneIndicatorUI` / `ScoreBarUI`

**Map / scene deliverables** (all <span class="cc-status pending">Editor-pending</span>):
- [ ] Zone A (Courtyard): art pass, capture point, destructible stalls, 3 lanes
- [ ] Zone B (Indoor Market): enclosed building, 4 entry points, mezzanine
- [ ] Zone C (Central Stage): elevated platform, barrier unlock at 300s
- [ ] EasyRoads3D terrain integration (paths between zones)
- [ ] All three flanking routes (Alley East, Rooftop Catwalk, Underground Cellar)
- [ ] Respawn camera (spectate killer or nearest teammate): *not in code yet*; `PlayerCamera.DeathTilt()` exists, full spectate is pending

::: info Match state machine delta
The doc shortened the flow to `CLASS_SELECT → MATCH_ACTIVE → MATCH_END`. The shipped `MatchState` enum is fuller: `Lobby → ClassSelect → Countdown → MatchActive → SuddenDeath → MatchEnd → PostMatch`. Timings are pulled from `GameConstants`: `ClassSelectDuration=30`, `CountdownDuration=5`, `MatchDuration=480` (8 min), `SuddenDeathDuration=60`, `ZoneCUnlockTime=300` (minute 5), `PostMatchDuration=15`. Sudden death is wired (`GameModeManager.SuddenDeath()`).
:::

**Done when:** A full 8-minute match plays out correctly. Zone C unlocks at minute 5, the match ends on score cap (`ScoreCap=500`) or timer, and the post-match screen appears with accurate stats. *(Logic verified; gated on the map + scenes.)*

---

## Phase 4: Online multiplayer

<span class="cc-status partial">Partial</span>: mirror components written, but **NGO is not installed yet** (code dormant behind `NETCODE_PRESENT`)

**Goal:** 3v3 or 4v4 playable over the internet.

::: warning Netcode package not installed
The network mirror layer exists in `Assets/_Game/Network/`, but **every file is wrapped in `#if NETCODE_PRESENT ... #endif`**, and Netcode for GameObjects is **not** in the project, so the directory compiles to nothing today. Unity Relay and Lobby are likewise not integrated. Installing NGO and defining `NETCODE_PRESENT` is the gate for this phase. The mirror pattern is described in [CONTRACTS.md](/dev/architecture): a `Networked*` component holds a reference to the local gameplay component and push/pulls `NetworkVariable`s, while gameplay logic stays in the offline spine.
:::

**Mirror code written** (compiles only once NGO is present):
- [x] `NetworkedPlayerController` (position-sync mirror)
- [x] `NetworkedHealthController` (HP mirror)
- [x] `NetworkedMomentumController` (tier mirror)
- [x] `NetworkedAbilityController` (ability-activation mirror)
- [x] `CaptureZoneNetwork` (capture progress + ownership mirror)
- [x] `GameModeNetworkManager` (score + timer mirror)
- [x] `ConnectionManager` (connection lifecycle scaffold)

**Not yet done:**
- [ ] Install Unity Netcode for GameObjects (NGO) + define `NETCODE_PRESENT`
- [ ] Unity Relay integration (NAT traversal, no port forwarding required)
- [ ] Unity Lobby (create/browse/join rooms)
- [ ] `ClientNetworkTransform` on the player + interpolation tuning
- [ ] Server-side lag compensation for hitscan (position history buffer, `MaxLagCompensation=0.2s` cap)
- [ ] Spawn invulnerability enforced server-side (`SpawnInvulnerability=2s`)
- [ ] Reconnect handling (player slot held; bot fills if no reconnect)
- [ ] Basic anti-cheat: server validates kill distance, damage values, ability cooldowns

**Done when:** Two people on separate networks play a full match with correct score sync, momentum sync, and win detection. Target: playable at 50ms RTT without noticeable rubber-banding.

**Key risks:**
1. Host-client model means the host has 0ms advantage. Document this; accept it for MVP, fix it with a dedicated server in Season 1.
2. NGO's `CharacterController` sync requires a `NetworkRigidbody` substitute. Use `ClientNetworkTransform` with interpolation.
3. Ability VFX must play on all clients. Use `ClientRpc` to trigger visual-only effects.

---

## Phase 5: Polish (vertical slice)

<span class="cc-status partial">Partial</span>: audio/menu/progression *systems* coded; all clips, art, tutorial content, and the perf/playtest passes are pending

**Goal:** A build ready for external playtesting / demo. Someone unfamiliar can pick it up and play.

**System code that exists** (✅ = code verified):
- [x] Audio system: pooled `AudioManager` + `AudioLibrary` + `FootstepController`/`FootstepBank` (surface-responsive) + `MusicDirector` + `AmbientZone`
- [x] VFX hooks: momentum tier visuals (`MomentumRingUI` + tier colours), muzzle/hit feedback (`FeedbackController`, `ScreenEffects`, `HitMarkerUI`): *particle/clip assets pending*
- [x] Main menu (`MainMenuController`) and Settings (`SettingsMenuController` + `SettingsService`, PlayerPrefs-backed)
- [x] XP system (`XPManager` / `ProgressionService`: earn XP, level up, shown on post-match)
- [x] Daily challenges (`ChallengeSystem`: 3 visible per day, reroll support)
- [x] Battle pass UI stub (`BattlePassService`: tiers + progress, no purchases)
- [x] Save layer (`SaveSystem` / `ProgressionData`)

**Content / passes still pending** (<span class="cc-status pending">Editor-pending</span> or QA work):
- [ ] Full SFX clip pass (footsteps, weapon fire, ability SFX, UI, ambient per zone): *code reads them via the [SFX key conventions](/dev/architecture); the clips themselves are unauthored*
- [ ] Full VFX polish (all ability effects, muzzle flash, hit particles as real prefabs)
- [ ] SlimUI Modern Menu 1 art base wired into the menu controllers
- [ ] Settings screen art (resolution, fullscreen, sliders, mouse sensitivity, keybind display)
- [ ] Tutorial match (forced first launch): `SceneTutorial` const exists; **no tutorial scene/logic yet**
- [ ] First-time UX overlays (3 real-match onboarding tooltips)
- [ ] Graphy integration (dev-build only performance overlay)
- [ ] Performance pass (minimum-spec PC + mobile test device)
- [ ] Bug fix sprint (no crashes, no score desync, no ability soft-locks)
- [ ] 5 full internal playtests (track: TTK feel, momentum clarity, Zone C drama, first-match clarity)

::: info Challenge-pool delta
The doc says "basic rotation of **10** challenge types". The shipped `ChallengeType` enum exposes **7** types (`Kills`, `Captures`, `ReachTier3`, `WinMatch`, `DamageWithClass`, `Assists`, `HoldObjective`); `ChallengeSystem` draws **3** distinct daily challenges from that pool (`DailyCount=3`). The "3 visible" count matches the design; the pool size is 7, not 10. Adding the remaining types is a content/tuning task.
:::

**Done when:** 3 people unfamiliar with the game play a full match, understand what momentum is, and say "one more game" unprompted.

---

## Asset inventory

Code-owned systems (the "Build" rows) are now written. Third-party packages and editor content (the "Integrate"/"In project"/"Configured" rows) are tracked separately on [/dev/status](/dev/status).

**Code we own:**

| Asset | Status | Phase |
|---|---|---|
| FPS controller (`PlayerMovement`+`PlayerCamera`) | <span class="cc-status built">Implemented</span> | 1 |
| `WeaponController` (hitscan + falloff) | <span class="cc-status built">Implemented</span> | 1 |
| `MomentumController` | <span class="cc-status built">Implemented</span> | 1 |
| `AbilityBase` + `AbilityController` + 16 impls | <span class="cc-status built">Implemented</span> | 2 |
| `EffectSystem` | <span class="cc-status built">Implemented</span> | 2 |
| `CaptureZone` + `GameModeManager` + `MatchStats` | <span class="cc-status built">Implemented</span> | 1→3 |
| `SpawnManager` (respawn + redirect) | <span class="cc-status built">Implemented</span> | 3 |
| Bots (`CombatBotBrain` FSM + `BotSpawner`) | <span class="cc-status built">Implemented</span> | 3 |
| Audio (`AudioManager` + footsteps + music) | <span class="cc-status built">Implemented</span> | 5 |
| Progression (XP/battlepass/challenges/save) | <span class="cc-status built">Implemented</span> | 5 |
| Network mirrors (`Assets/_Game/Network/`) | <span class="cc-status partial">Partial</span> (behind `NETCODE_PRESENT`) | 4 |
| Lag compensation | <span class="cc-status pending">Pending</span> | 4 |

**Packages / config / engine:**

| Asset | Status | Phase |
|---|---|---|
| Unity engine | Pinned **6000.4.10f1** | - |
| Netcode for GameObjects | Not installed | 4 |
| Unity Relay + Lobby | Not integrated | 4 |
| EasyRoads3D | In project | 3 |
| Graphy | In project | 5 |
| SlimUI / Modern Menu 1 | In project | 5 |
| URP 17.4 (PC + Mobile profiles) | Configured | 1 |
| `InputSystem_Actions` (InputSystem 1.19) | In project (missing 5 actions; see Phase 1) | 1 |

---

## Definition of "vertical slice"

A vertical slice is not a demo with missing features hidden. It must:
- Represent the final game's quality and feel in every system it contains
- Be playable by someone with no prior knowledge
- Contain: 1 map, 4 classes, 1 mode, online multiplayer, progression stub, full SFX/VFX
- Not contain: ranked mode, battle pass purchasing, additional maps, additional classes

When someone plays the vertical slice, they should feel like they're playing the actual game, just a limited version of it.

---

## For engineers

The milestone checklists above are the *design* view. For the implementation view, see the `/dev` pages. They are the VitePress-friendly summaries of the canonical engineering docs at the repo root (`README.md`, `DEVENV.md`, `ASSETS.md`, `Assets/_Game/SETUP.md`, `Assets/_Game/CONTRACTS.md`), which remain the source of truth:

- [/dev/getting-started](/dev/getting-started): clone, open in Unity 6000.4.10f1, generate data assets, build the scenes/prefab
- [/dev/architecture](/dev/architecture): code map + the frozen public API (mirrors `CONTRACTS.md`)
- [/dev/status](/dev/status): live build status: what's coded vs. what's editor-pending
- [/dev/assets](/dev/assets): the editor-authored content checklist (scenes, prefab, map, audio, art)
- [/dev/environment](/dev/environment) and [/dev/editor-setup](/dev/editor-setup): toolchain + project setup

Do not contradict the root engineering docs; if a number here disagrees with `GameConstants.cs`, the code wins.
