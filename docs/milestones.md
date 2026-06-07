# Build Milestones

Target: a shippable vertical slice — 1 map, 4 classes, online multiplayer, all core systems working.

---

## Phase Overview

| Phase | Goal | Est. duration | Status |
|---|---|---|---|
| 1 | Foundation (offline prototype) | 2–3 weeks | Not started |
| 2 | All classes + ability system | 3–4 weeks | Not started |
| 3 | Full map + game mode | 3–4 weeks | Not started |
| 4 | Online multiplayer | 3–5 weeks | Not started |
| 5 | Polish (vertical slice) | 2–3 weeks | Not started |
| **Total** | | **13–19 weeks** | |

---

## Phase 1 — Foundation (Offline Prototype)

**Goal:** Prove the core feel. Must be fun alone before adding complexity.

**First tasks (start here):**
1. Create `PlayerController` prefab with `CharacterController`, movement, jump, sprint
2. Wire `InputSystem_Actions.inputactions` to player movement and look
3. Implement first-person camera (`PlayerCamera` with mouse look + FOV)
4. Add `WeaponController` with hitscan raycast and hit detection
5. Create `MomentumController` with tiers 0–3, decay coroutine, and `OnTierChanged` event
6. Add placeholder HUD: HP bar + momentum ring (ring = simple filled Image component)
7. Create `CharacterDataSO` for Carrot and `WeaponDataSO` for The Nub
8. Implement `Ability_SprintDash` and `Ability_RadarPulse` for Carrot
9. Create `CaptureZone` with basic trigger-volume capture logic + score tick
10. Create `GameModeManager` with match timer and score tracking
11. Block out the map (grey-box geometry only — Zones A, B, C footprints with spawns)
12. Add a dummy enemy target (static; takes damage; dies; resets after 3s for testing)

**Deliverables checklist:**
- [ ] FPS controller (WASD + mouse look + jump + sprint + crouch)
- [ ] Hitscan weapon with hit confirmation (audio + hit marker)
- [ ] Momentum system (tiers 0–3, decay, visual feedback on ring HUD)
- [ ] Carrot class (Sprint Dash + Radar Pulse)
- [ ] One objective zone (capture logic, score tick visible on HUD)
- [ ] Basic HUD (HP bar, momentum ring, score counter, ability cooldowns)
- [ ] Placeholder map (3-zone blockout, both spawns, flanking route paths marked)
- [ ] `ScriptableObject` data layer in place (CharacterDataSO, WeaponDataSO, MomentumConfigSO)

**Done when:** Solo session playable — kill dummy target, watch momentum tier up to 3, let it decay, capture a zone, watch score count. Game loop is legible.

**Key risk:** CharacterController vs. Rigidbody decision. Use `CharacterController` (not Rigidbody) for FPS — it gives deterministic movement without physics jitter and works cleanly with NGO's `ClientNetworkTransform` later.

---

## Phase 2 — All Classes + Ability System

**Goal:** All four classes playable with full kits. Ability system must support any future class additions.

**Deliverables:**
- [ ] `AbilityBase` abstract class + `AbilityController` slot manager
- [ ] `CooldownManager` respecting `MomentumController.CooldownMultiplier`
- [ ] `EffectSystem` static class (ApplySlow, ApplyFire, ApplyKnockback, ApplyTemporaryHP)
- [ ] Jalapeño class (Spice Burst, Heat Trail, Burn Streak, Extended Streak)
- [ ] Broccoli class (Leaf Shield, Spore Cloud, Regen Aura, Shared Harvest)
- [ ] Potato class (Starch Armor, Earthen Slam, Thick Skin, Stubborn Root)
- [ ] `CharacterDataSO` assets for all 4 classes
- [ ] Class select screen (basic — 4 buttons, no art yet)
- [ ] Per-class VFX placeholders (ability origin indicator, simple particle)
- [ ] Momentum class passives wired up for all 4

**Done when:** All four classes are selectable, all 8 active abilities function correctly, and all 4 momentum passives produce measurable effects.

**Key risk:** Ability interactions (e.g. Spore Cloud blocking Radar Pulse, Burn Streak + Spice Burst stacking). Define interaction rules now — see `EffectSystem` in [Tech Architecture](tech-architecture.md).

---

## Phase 3 — Full Map + Game Mode

**Goal:** Complete, playable match from start to finish, offline/LAN.

**Deliverables:**
- [ ] Zone A (Courtyard) — art pass, capture point, destructible stalls, 3 lanes
- [ ] Zone B (Indoor Market) — enclosed building, 4 entry points, mezzanine
- [ ] Zone C (Central Stage) — elevated platform, barrier unlock at 300s
- [ ] EasyRoads3D terrain integration (paths between zones)
- [ ] All three flanking routes (Alley East, Rooftop Catwalk, Underground Cellar)
- [ ] `GameModeManager` — full match state machine (CLASS_SELECT → MATCH_ACTIVE → MATCH_END)
- [ ] Zone C lock/unlock logic with audio announcement
- [ ] Post-match screen (scores, MVP, Hot Streak, basic XP counter)
- [ ] Team A and Team B spawn systems with safety redirect
- [ ] Respawn camera (spectate killer or nearest teammate)
- [ ] Zone score UI (three zone ownership indicators on HUD)

**Done when:** A full 8-minute match plays out correctly — Zone C unlocks at minute 5, match ends on score cap or timer, post-match screen appears with accurate stats.

---

## Phase 4 — Online Multiplayer

**Goal:** 3v3 or 4v4 playable over the internet.

**Deliverables:**
- [ ] Unity Netcode for GameObjects (NGO) integration
- [ ] Unity Relay integration (NAT traversal, no port forwarding required)
- [ ] Unity Lobby (create/browse/join rooms)
- [ ] `NetworkedPlayerController` (position sync with `ClientNetworkTransform`)
- [ ] `NetworkedHealthController` (`NetworkVariable<int>` HP, `ServerRpc TakeDamage`)
- [ ] `NetworkedMomentumController` (`NetworkVariable<int>` Tier)
- [ ] `NetworkedAbilityController` (`ServerRpc ActivateAbility`)
- [ ] `CaptureZoneNetwork` (synced capture progress + ownership)
- [ ] `GameModeNetworkManager` (synced scores + timer)
- [ ] Server-side lag compensation for hitscan (position history buffer, 200ms max)
- [ ] Spawn invulnerability enforced server-side
- [ ] Reconnect handling (player slot held for 30s; bot fills if no reconnect)
- [ ] Basic anti-cheat: server validates kill distance, damage values, ability cooldowns

**Done when:** Two people on separate networks play a full match with correct score sync, momentum sync, and win detection. Target: playable at 50ms RTT without noticeable rubber-banding.

**Key risks:**
1. Host-client model means host has 0ms advantage — document this; accept for MVP, fix with dedicated server in Season 1
2. NGO's `CharacterController` sync requires `NetworkRigidbody` substitute — use `ClientNetworkTransform` with interpolation
3. Ability VFX must play on all clients — use `ClientRpc` to trigger visual-only effects

---

## Phase 5 — Polish (Vertical Slice)

**Goal:** Build ready for external playtesting / demo. Someone unfamiliar can pick it up and play.

**Deliverables:**
- [ ] Full SFX pass (footsteps surface-responsive, weapon fire, ability SFX, UI, ambient per zone)
- [ ] Full VFX polish (all ability effects, momentum tier visuals, muzzle flash, hit particles)
- [ ] Main menu (SlimUI Modern Menu 1 as base; Play, Party, Settings, Quit)
- [ ] Settings screen (resolution, fullscreen, audio sliders, mouse sensitivity, keybind display)
- [ ] Tutorial match (forced first launch; teaches movement + momentum + objective)
- [ ] First-time UX overlays (3 real-match onboarding tooltips)
- [ ] XP system (earn XP, level up, basic progression visible on post-match screen)
- [ ] Daily challenges (3 visible in main menu; basic rotation of 10 challenge types)
- [ ] Battle pass UI stub (show tiers, current progress — no purchases yet)
- [ ] Graphy integration (dev-build only performance overlay)
- [ ] Performance pass (profile on minimum spec PC + test device for mobile)
- [ ] Bug fix sprint (minimum: no crashes, no score desync, no ability soft-locks)
- [ ] 5 full internal playtests (track: TTK feel, momentum clarity, Zone C drama, first-match clarity)

**Done when:** 3 people unfamiliar with the game play a full match, understand what momentum is, and say "one more game" unprompted.

---

## Asset Inventory

| Asset | Status | Phase needed |
|---|---|---|
| FPS controller | Build | 1 |
| WeaponController (hitscan) | Build | 1 |
| MomentumController | Build | 1 |
| AbilityBase + AbilityController | Build | 2 |
| EffectSystem | Build | 2 |
| CaptureZone + GameModeManager | Build | 1 (basic) → 3 (full) |
| NGO integration | Integrate | 4 |
| Unity Relay + Lobby | Integrate | 4 |
| Lag compensation | Build | 4 |
| EasyRoads3D | In project | 3 |
| Graphy | In project | 5 |
| SlimUI / Modern Menu 1 | In project | 5 |
| URP (PC + Mobile profiles) | Configured | 1 |
| InputSystem_Actions | In project | 1 |

---

## Definition of "Vertical Slice"

A vertical slice is not a demo with missing features hidden. It must:
- Represent the final game's quality and feel in every system it contains
- Be playable by someone with no prior knowledge
- Contain: 1 map, 4 classes, 1 mode, online multiplayer, progression stub, full SFX/VFX
- Not contain: ranked mode, battle pass purchasing, additional maps, additional classes

When someone plays the vertical slice, they should feel like they're playing the actual game — just a limited version of it.
