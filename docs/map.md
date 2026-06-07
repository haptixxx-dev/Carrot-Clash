# Map Design — The Grand Food Market

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

### Zone A — The Courtyard

| Property | Value |
|---|---|
| Type | Open outdoor |
| Dimensions | ~40m × 30m |
| Capture radius | 6m |
| Capture time (1 player) | 10s |
| Score tick rate | 1 pt/s |
| Key cover | 8 destructible market stalls (80 HP each), 4 produce carts (permanent) |

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

---

## Flanking Routes

| Route | Path | Travel time | Risk level | Notes |
|---|---|---|---|---|
| Alley East | Team A Spawn ↔ Zone B side door | ~6s | Medium | Narrow corridor; encounters likely; passes enemy spawn zone near minute 5 when both teams push Zone C |
| Rooftop Catwalk | Zone A roof ↔ Zone C (one-way drop) | ~8s | High | Exposed overhead; one-way (drop only — can't return this way); Carrot Dash can skip part of the route |
| Underground Cellar | Zone B basement ↔ Zone C underside stairs | ~10s | Low | Slow; sound-dampened (quiet footsteps here regardless of class); ideal for Potato or surprise push |

---

## Spawn Points

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

---

## Audio Landmarks

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

| Asset | Usage |
|---|---|
| EasyRoads3D | Terrain layout and road/path definition between zone exteriors |
| URP PC profile | Primary quality target; Zone A uses real-time directional light + shadows |
| URP Mobile profile | Zone geometry LOD; baked lighting for Zone B interior |
| SlimUI | Capture point UI overlays (team colour, fill bar, percentage) |
