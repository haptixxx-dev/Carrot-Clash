# Map Design — The Grand Food Market

<span class="cc-status pending">Editor-pending</span> The map geometry, grey-box, art, and NavMesh are all editor-authored content and do not exist yet. **However, every gameplay system this map drives is implemented in code** (`Assets/_Game/`): capture zones, the Zone C unlock, spawn selection with the safety redirect, destructible cover, vision-obscuring volumes, and per-zone ambient audio. This page reconciles the design with the shipped code and flags the deltas. See [/dev/getting-started](/dev/getting-started) and `Assets/_Game/CONTRACTS.md` for the engineering source of truth.

::: info Map components → code map
- Capture points → `Gameplay/Objectives/CaptureZone.cs` (`ZoneId` from `Shared/GameEnums.cs`)
- Zone C unlock timer → `GameConstants.ZoneCUnlockTime` + `GameModeManager`
- Spawns + safety redirect → `Gameplay/Map/SpawnManager.cs` (`SpawnPoint` markers)
- Destructible stalls/shields → `Gameplay/DestructibleCover.cs`
- Spore-cloud sightline denial → `Gameplay/VisionObscured.cs`
- Per-zone ambience → `Audio/AmbientZone.cs`

What's missing is purely **scene content**: someone must place the `CaptureZone`, `SpawnPoint`, `AmbientZone`, and cover prefabs in the `Gameplay_Market` scene and bake the NavMesh.
:::

---

## Theme

An oversized open-air market fused with a commercial kitchen. Think a farmer's market after a food fight — bright awnings, crates of produce stacked as cover, vendor stalls that can be blown apart, and an elevated central stage used for cooking demonstrations (now used for the match's deciding zone).

The environment is mechanical: cover objects are themed to the class colours (orange crates = Carrot territory, red pepper barrels = Jalapeño's domain), helping with visual orientation. Every part of the map exists to support a specific combat scenario.

---

## Layout Overview

```
        [Team A Spawn]
              |
       [Alley East] ←── flanking route
              |
        ┌─────────────────────────┐
        │     ZONE A              │
        │     The Courtyard       │
        │   (open, long sight     │
        │    lines, stalls)       │
        │  [Capture Point A]      │
        └────────────┬────────────┘
                     │
         [Rooftop Catwalk] ←── flanking route (Zone A to Zone C, one-way drop)
                     │
        ┌────────────▼────────────┐
        │     ZONE B              │
        │  The Indoor Market      │
        │  (tight corridors,      │
        │   multiple entries)     │
        │  [Capture Point B]      │
        └────────────┬────────────┘
                     │
       [Underground Cellar] ←── flanking route
                     │
        ┌────────────▼────────────┐
        │     ZONE C              │◄── LOCKED until minute 5
        │   The Central Stage     │
        │  (elevated platform,    │
        │   mixed sightlines)     │
        │  [Capture Point C]      │
        └─────────────────────────┘
              |
       [Alley East south] ←── connects to Spawn B
              |
        [Team B Spawn]
```

---

## Zone Breakdown

<span class="cc-status built">Implemented</span> Capture behaviour for all three zones lives in `CaptureZone.cs`. The per-zone defaults below (`captureRadius`, `captureSeconds`, `scoreTickRate`, `lockUntilMatchTime`) are serialized fields on that component and match the numbers in this doc exactly. They become live once a designer drops a configured `CaptureZone` into the gameplay scene.

::: info Code adds capture nuance not in the design tables
The shipped `CaptureZone` implements two behaviours the original flat tables don't spell out:

- **Multi-capper speedup** — each extra teammate inside the radius adds `perPlayerSpeedup` (0.5 = +50% of base rate, diminishing), clamped to `maxSpeedup` (2.5×). So "Capture time (1 player)" below is the *solo* time; a stacked team captures faster, up to 2.5× quicker.
- **Contest = freeze, then chip-down** — while both teams stand in the zone, progress freezes (and the eventual capture is flagged *contested*, worth +15 instead of +10). A contesting enemy must first chip an owner's progress back to 0 before their own capture begins.
- **Flat (horizontal) distance** is used for occupancy so the raised Zone C platform is judged fairly regardless of height.

Capture-completion score: neutral→team = `10`, contested→team = `15` (`GameConstants`).
:::

### Zone A — The Courtyard

| Property | Value |
|---|---|
| Type | Open outdoor |
| Dimensions | ~40m × 30m |
| Capture radius | 6m |
| Capture time (1 player) | 10s |
| Score tick rate | 1 pt/s |
| Key cover | 8 destructible market stalls (80 HP each), 4 produce carts (permanent) |

> Destructible stalls map to `DestructibleCover.cs`, whose default HP pool is `80` — matches the design. The component shatters (self-destructs + plays `impact_surface` SFX + optional VFX) at 0 HP.

**Sightlines:** 3 main lanes separated by stall rows. Removing a stall opens a lane entirely — Jalapeño shutting down a lane with Heat Trail is a key zone A tactic.

**Class affinity:** Carrot (mobility between lanes), Jalapeño (Heat Trail lane denial)

**Capture point placement:** Center of the courtyard; capturer is visible from all 3 lanes — no fully safe capture position, but cover is close.

**Spawn proximity (Team A):** ~15s walk; ~8s at full sprint. First contest expected at ~0:10.

---

### Zone B — The Indoor Market

| Property | Value |
|---|---|
| Type | Enclosed building |
| Dimensions | ~30m × 25m, 1 floor (partial mezzanine) |
| Capture radius | 4m (smaller — tight space) |
| Capture time (1 player) | 8s (faster — compensates for harder hold) |
| Score tick rate | 1 pt/s |
| Key cover | Shelving rows (permanent), counter tops (permanent), 2 entry doors (each 3m wide) |

**Entry points:** 4 entries total — front (Zone A side), back (Zone C side), Alley East side door, mezzanine balcony drop. Broccoli Leaf Shield at the front door is the most impactful ability placement in the game.

**Class affinity:** Potato (zone hold), Broccoli (Leaf Shield entry denial + aura sustain)

**Capture point placement:** Behind the main counter — partially sheltered, but accessible from front entry, back entry, and mezzanine. No single dominant hold angle.

**Sightlines:** Max 12m clear sight; rest is cornered. Favours close-range classes heavily.

---

### Zone C — The Central Stage

| Property | Value |
|---|---|
| Type | Elevated open platform |
| Dimensions | ~20m diameter circular platform, raised 3m |
| Capture radius | 8m (large — the whole platform) |
| Capture time (1 player) | 12s |
| Score tick rate | 2 pts/s |
| Key cover | 6 low produce barrels around the edge (permanent), 1 central column (permanent) |
| Unlock time | 300s into match (5 minutes) |

**Access routes:**
- Main ramp from Zone B (wide, exposed)
- Rooftop Catwalk drop (one-way; fast route from Zone A)
- Underground Cellar stairs (slow, concealed)

**Class affinity:** Mixed — open platform rewards Carrot mobility and Jalapeño AOE, but Potato + Broccoli can anchor the central column nearly indefinitely.

**Tactical note:** The platform is visible from Zone A's rooftop line of sight — Carrot with Radar Pulse can see who's contesting Zone C from Zone A, enabling info plays.

**Barrier unlock:** Physical barrier objects lower at second 300; global audio announcement ("The Stage is open!") plays for all players; HUD flashes Zone C icon.

> Code: a `CaptureZone` with `lockUntilMatchTime = 300` (`GameConstants.ZoneCUnlockTime`) stays inert until `GameModeManager.ElapsedTime` crosses 300s, then fires `GameEvents.RaiseZoneCUnlocked()` (HUD + audio listen for it). `scoreTickRate` is `2` here vs `1` for A/B — confirmed in both doc and code. The physical barrier mesh and the unlock SFX/announcement are editor-pending; the event that triggers them is wired.

> Info-play note (Carrot from Zone A): `Ability_RadarPulse` ships with a 15m scan radius and 2.5s reveal, so the rooftop-sightline call-out above is supported by the actual ability values.

---

## Flanking Routes

| Route | Path | Travel time | Risk level | Notes |
|---|---|---|---|---|
| Alley East | Team A Spawn ↔ Zone B side door | ~6s | Medium | Narrow corridor; encounters likely; passes enemy spawn zone near minute 5 when both teams push Zone C |
| Rooftop Catwalk | Zone A roof ↔ Zone C (one-way drop) | ~8s | High | Exposed overhead; one-way (drop only — can't return this way); Carrot Dash can skip part of the route |
| Underground Cellar | Zone B basement ↔ Zone C underside stairs | ~10s | Low | Slow; sound-dampened (quiet footsteps here regardless of class); ideal for Potato or surprise push |

::: details Code note — footsteps & sightline denial
The cellar's stealth feel rides on the footstep system: `SurfaceType.Dirt` (`GameEnums.cs`) is the cellar's surface category, resolved per-collider by `FootstepBank`. The route's "quiet regardless of class" claim is a **design intent for the surface/material setup** — there's no route-based code that further dampens footsteps; Carrot's separate `Passive_SilentSteps` is the class-level stealth.

Sightline denial along these routes (e.g. Broccoli's Spore Cloud) is backed by `VisionObscured.cs`: standing in a cloud suppresses reveals (Radar Pulse, blips, outlines) via a reference count so overlapping clouds behave correctly.
:::

---

## Spawn Points

<span class="cc-status partial">Partial</span> Spawn *logic* is fully implemented in `SpawnManager.cs` (respawn timing, fixed-spawn selection, the contest check, the safe-teammate fallback, and invulnerability windows). It's marked Partial because it needs `SpawnPoint` marker objects placed in the gameplay scene (the staggered positions behind cover described below) before it does anything — with zero spawn points it falls back to world origin.

### Team A Spawn
- 3 staggered spawn positions behind a stone archway (prevents spawn kill line)
- 2s spawn invulnerability on entry
- Direct sight into Zone A's right lane
- Nearest enemy position at match start: ~40m

### Team B Spawn
- Mirror layout — 3 positions behind a produce warehouse wall
- Direct sight into Zone A's left lane
- Same 2s invulnerability

### Spawn Safety Rule
If a spawn position has an enemy within 10m, the spawning player is redirected to the nearest safe teammate position. If no safe position exists, spawn delay extends by 1s and the player spawns at the safe fixed position with 3s invulnerability.

The shipped `SpawnManager.ChooseSpawn` runs this exact cascade:

- **Respawn delay** — `RespawnTime = 4s` before spawn selection begins.
- **Try a fixed spawn** — first uncontested team spawn point, where "contested" = an enemy within `SpawnContestRadius = 10m`.
- **Else nearest safe teammate** — only teammates within `SafeTeammateRadius = 30m` (and themselves uncontested) qualify; the player drops in just behind that ally.
- **Else fallback** — a fixed spawn anyway, with `+1s` extra delay and `3s` invulnerability.
- **Default invulnerability** otherwise is `SpawnInvulnerability = 2s` (matches the 2s on-entry invuln above).

::: info Delta vs. design
The doc says "nearest safe teammate position" without bounding it — the code caps that search at **30m** (`SafeTeammateRadius`). This is a code-only constant; it tightens the rule so you can't be flung across the map. Everything else (10m contest radius, +1s/3s fallback, 2s normal invuln) matches the design verbatim.
:::

---

## Audio Landmarks

<span class="cc-status partial">Partial</span> The mechanism is built: `AmbientZone.cs` is a trigger volume that starts/stops a per-zone ambient loop through the pooled `AudioManager`, with a ~1.5s crossfade grace so crossing a boundary doesn't hard-cut the bed. Drop one `AmbientZone` per zone (with the right `ambientKey`) and the table below works. It's Partial because the actual ambient *clips* are editor-pending — see [/dev/assets](/dev/assets).

Spatial audio helps players build a mental map of the level — each zone has a distinct ambient layer:

| Zone | Ambient sound |
|---|---|
| Zone A Courtyard | Open air; distant market noise, wind, distant crowd chatter |
| Zone B Indoor Market | Muffled; refrigerator hum, dripping water, echo on gunshots |
| Zone C Central Stage | Reverberant; elevated; distant crowd roar builds as match timer drops |
| Alley East | Tight echo; footsteps louder than anywhere else on the map |
| Underground Cellar | Deep, bass-heavy; near-silent footsteps (stealth route feel) |

**Zone C unlock audio:** A dramatic kitchen bell rings once, crowd noise surges, HUD SFX plays — clear signal that stakes have escalated.

---

## Existing Assets Integration

<span class="cc-status pending">Editor-pending</span> These third-party packages are the intended building blocks for the scene; the geometry, lighting, and UI overlays that use them are all editor-authored and not built yet. See [/dev/assets](/dev/assets) for the current asset inventory and import status.

| Asset | Usage |
|---|---|
| EasyRoads3D | Terrain layout and road/path definition between zone exteriors |
| URP PC profile | Primary quality target; Zone A uses real-time directional light + shadows |
| URP Mobile profile | Zone geometry LOD; baked lighting for Zone B interior |
| SlimUI | Capture point UI overlays (team colour, fill bar, percentage) |
