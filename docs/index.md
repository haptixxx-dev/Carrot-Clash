# Carrot Clash design documentation

Fast-paced tactical FPS with food/vegetable-themed hero classes and objective-based team combat. A momentum/combo system rewards aggressive play and punishes camping.

::: info BUILD STATUS: code-complete, editor-pending
The gameplay spine is <span class="cc-status built">Implemented</span> in code. 97 C# scripts under `Assets/_Game/` cover player core, the momentum tier system, all 4 classes plus 16 ability behaviours, the match state machine and scoring, spawns, the full HUD, menus, bots, audio, and progression. All of it is verified against this design doc.

What is **not** done is editor-authored content: no scenes, player prefab, map (grey-box/art/NavMesh), UI canvases, audio clips, icons or VFX exist yet. Multiplayer is dormant. **Netcode for GameObjects and Relay/Lobby are not installed** (the network layer compiles behind `#if NETCODE_PRESENT`). The game is therefore not yet playable end-to-end.

Engineers should start at **[/dev/getting-started](dev/getting-started.md)**. The frozen API lives in `Assets/_Game/CONTRACTS.md`.
:::

---

## Documents

Design docs are all complete. The **Code** column reflects how far the verified C# implementation has gone against each spec (cross-checked against `Assets/_Game/`).

| Doc | Description | Doc | Code |
|---|---|---|---|
| [GDD](gdd.md) | Master GDD: overview, pillars, retention, monetization, accessibility, mobile | ✅ | <span class="cc-status partial">Partial</span> |
| [Core Loop](core-loop.md) | Match state machine, scoring system, feedback loops | ✅ | <span class="cc-status built">Implemented</span> |
| [Momentum System](momentum-system.md) | Kill combo mechanic, tier rules, network sync, UI spec | ✅ | <span class="cc-status built">Implemented</span> |
| [Characters](characters.md) | All 4 classes with stats, abilities, counters, synergies | ✅ | <span class="cc-status built">Implemented</span> |
| [Weapons](weapons.md) | Per-class weapons, damage stats, TTK tables, ammo system | ✅ | <span class="cc-status built">Implemented</span> |
| [Ability Interactions](ability-interactions.md) | How abilities interact, status effect rules, edge cases, anti-abuse | ✅ | <span class="cc-status built">Implemented</span> |
| [Map](map.md) | Grand Food Market: zones, flanking routes, spawn rules, audio | ✅ | <span class="cc-status pending">Editor-pending</span> |
| [Game Feel](game-feel.md) | Screen shake, hit feedback, dash feel, death/respawn, all juice | ✅ | <span class="cc-status built">Implemented</span> |
| [Audio](audio.md) | Music architecture, full SFX spec, ambient per zone, asset list | ✅ | <span class="cc-status partial">Partial</span> |
| [UI / UX](ui-ux.md) | All screens wireframed, HUD spec, mobile layout | ✅ | <span class="cc-status partial">Partial</span> |
| [Progression](progression.md) | XP system, battle pass, daily challenges, FTUE, ranked | ✅ | <span class="cc-status built">Implemented</span> |
| [Tech Architecture](tech-architecture.md) | Unity C# architecture, ScriptableObjects, NGO, bot AI, input | ✅ | <span class="cc-status partial">Partial</span> |
| [Milestones](milestones.md) | 5-phase build plan with checklists and time estimates | ✅ | <span class="cc-status built">Implemented</span> |

Code-status key: <span class="cc-status built">Implemented</span> = verified in code · <span class="cc-status partial">Partial</span> = code exists, needs editor wiring/assets · <span class="cc-status pending">Editor-pending</span> = needs editor-authored content (scenes/prefabs/art/audio).

**Engineers:** see the [/dev pages](dev/getting-started.md) for setup, architecture and the live build status, backed by `Assets/_Game/CONTRACTS.md`.

---

## Quick reference

| Property | Value |
|---|---|
| Genre | Team FPS, objective + elimination hybrid |
| Perspective | First-person, fast/tactical |
| Team size | 3v3 or 4v4 |
| Match length | 8 minutes (+ 60s sudden death if tied) |
| Win condition | First to 500 points, or highest at timer end |
| Classes | Carrot, Jalapeño, Broccoli, Potato |
| Map | The Grand Food Market (3 zones) |
| Engine | Unity 6 (6000.4.10f1), URP 17.4 |
| Networking | Unity Netcode for GameObjects + Unity Relay (planned, not installed yet) |
| Target platforms | PC (primary), mobile (secondary) |
| Vertical slice estimate | 13-19 weeks |

::: info CODE vs DESIGN: verified deltas
The shipped match invariants in `Assets/_Game/Shared/GameConstants.cs` match this table exactly: `MatchDuration=480` (8 min), `SuddenDeathDuration=60`, `ScoreCap=500`, `MaxPlayersPerTeam=4`. Zone C unlocks at `ZoneCUnlockTime=300` (minute 5). Class ids in code are `Carrot, Jalapeno, Broccoli, Potato`.

Two deltas to flag:
- **Networking is not live.** NGO and Relay are designed and the network mirrors are written, but the packages are not installed, so that code compiles only behind `#if NETCODE_PRESENT`. Current play is single-player vs bots.
- **Input actions incomplete.** The `InputSystem_Actions` asset lacks Reload/ADS/Ability1/Ability2/SwapWeapon. `PlayerInputBinder` falls back to **R / RightMouse / Q / E** until those actions are authored.
:::

---

## Design pillars (short form)

1. **Momentum over camping:** kills escalate your power; decay punishes passivity
2. **Class synergy:** 4 roles that combo, with no class lock
3. **Readable chaos:** high action density that stays legible
4. **Theme-first identity:** the food theme is mechanical, not cosmetic
