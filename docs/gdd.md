# Game Design Document — Carrot Clash

**Version:** 0.2 | **Engine:** Unity 6 (URP) | **Target:** PC primary, mobile secondary

---

## 1. Executive Summary

Carrot Clash is a 3v3 / 4v4 team FPS set in a stylized food-themed world. Players choose from four vegetable-inspired hero classes and fight to control objectives on a three-zone map. The defining mechanic is a **momentum/combo system**: kills escalate your power across three tiers, but death transfers that charge to your killer — making every engagement a risk-reward calculation.

Matches run 8 minutes. Short session length + high TTK variance (from momentum) creates the same "one more game" pull as Apex and The Finals.

---

## 2. Design Pillars

| # | Pillar | What it means in practice |
|---|---|---|
| 1 | **Momentum over camping** | Decay punishes passivity; buffs reward kills — the game physically pushes you forward |
| 2 | **Class synergy** | 4 classes with complementary kits; team composition matters but isn't mandatory |
| 3 | **Readable chaos** | Bright colour language per class; abilities are clearly telegraphed; chaos is fun, not frustrating |
| 4 | **Theme-first identity** | Food theme isn't cosmetic — it drives ability names, audio design, map geography, and SFX |

---

## 3. Core Systems (Summary)

| System | Doc | Status |
|---|---|---|
| Objective + Elimination hybrid mode | [Core Loop](core-loop.md) | Designed |
| Momentum/combo tier system | [Momentum System](momentum-system.md) | Designed |
| 4 hero classes with ability kits | [Characters](characters.md) | Designed |
| Class-specific weapons | [Weapons](weapons.md) | Designed |
| Three-zone map (Grand Food Market) | [Map](map.md) | Designed |
| XP, progression, seasonal retention | [Progression](progression.md) | Designed |
| Unity C# architecture | [Tech Architecture](tech-architecture.md) | Designed |
| Build milestones (5 phases) | [Milestones](milestones.md) | Designed |

---

## 4. Gameplay Synopsis

**Pre-match:** Players select a class in a 30-second lobby. No class lock — duplicates allowed.

**Match start:** Teams spawn 60m apart. Zones A and B are immediately contested. Zone C (Central Stage) is locked behind a barrier until minute 5.

**Mid-match:** Teams earn points from objective time + kills. Kill momentum escalates per-player power. Tier 3 "On Fire" players are visually distinct targets — killing them transfers their momentum charge to you.

**Late-game:** Zone C unlocks, scoring at 2x rate. Most teams pivot entirely to contesting it. If neither team holds Zone C, Zones A and B become the deciding factor. Score caps at 500 for an early win; otherwise highest score at 8:00 wins.

**Post-match:** MVP card (top score contribution), Hot Streak award (longest momentum chain), XP earned, rank delta. Designed to reward watching the screen stay up — kills, highlights, and XP rewards play out with delay to maximize dopamine drip.

---

## 5. Player Psychology / Retention Hooks

These are not cosmetic additions — they are load-bearing engagement mechanics:

### 5.1 Variable Reward Loops
- Kill streaks are variable (you don't know when tier 3 hits feel amazing)
- Post-match XP breakdown shows rewards with animated counters — even a loss gives visible progress
- Daily challenges randomize which class/objective they target — always something fresh to chase

### 5.2 Near-Miss Engineering
- Score cap at 500; most matches end 420–480 — making every match feel like it could have gone either way
- Tiebreaker zone (Zone C) almost always determines the winner — creates a clear dramatic moment
- Momentum transfer means your death directly empowered the enemy — personal stake, not random

### 5.3 Social Hooks
- Post-match screen shows your team's combined momentum uptime — shared accomplishment
- "Hot Streak" award shows kill-chain replay clip (short 10s highlight, shareable)
- Party system: invite friends directly from post-match screen

### 5.4 Session Length Design
- 8-minute matches = minimal time commitment, no friction to "just one more"
- Instant respawn = no dead time, no sitting out — every second is gameplay
- Class select is 30 seconds max — not long enough to overthink, fast enough to feel reactive

---

## 6. Art Direction

**Style:** Stylized, vibrant, slightly exaggerated. Think Overwatch's readability + Fortnite's colour saturation — but grounded in a food/market aesthetic rather than cartoony.

**Colour language (class identification):**
- Carrot: Orange primary, yellow accents
- Jalapeño: Red primary, deep green accents
- Broccoli: Green primary, white accents
- Potato: Brown/tan primary, earthy grey accents

**Map palette:** Warm yellows and oranges in the Courtyard, cool blues and greens inside the Market, neutral warm whites on the Central Stage. Colour shifts help spatial awareness.

**Momentum visual language:**
- Tier 0: No effect
- Tier 1: Faint colour-matching outline glow on player model
- Tier 2: Particle trail (leaves, embers — class-appropriate)
- Tier 3: Full character VFX, screen edge glow in player POV, audible heartbeat SFX

---

## 7. Audio Direction

- **Music:** Energetic, kitchen/market-themed base track. Intensity layers up with match tension (under 2 minutes, Zone C contested). Momentum tier 3 triggers a personal audio "rush" layer.
- **Footsteps:** Surface-responsive (wood market floor, stone courtyard, metal grating on catwalks). Carrot's silent step passive reduces these to near-zero in FOV.
- **Abilities:** Each ability has a food-themed SFX motif (Spice Burst = sizzle + burst, Leaf Shield = rustling + thud, Earthen Slam = bass thud + crumble).
- **UI:** Minimal, punchy. Momentum tier-up is a satisfying "power-up" chime. Kill confirmed is a short distinct crack. Zone captured is a clear resonant tone.

---

## 8. UI / UX Spec

### HUD Elements
| Element | Position | Notes |
|---|---|---|
| HP bar | Bottom-left | Simple numeric + bar; flashes red below 30HP |
| Momentum ring | Bottom-center | Ring around crosshair fills with tier colour; cracks on decay |
| Ability slots | Bottom-center | Two active slots with cooldown overlay; passive shown greyed |
| Team score | Top-center | Both team scores + match timer prominent |
| Zone ownership | Top-center below scores | Three zone indicators; colours show team ownership |
| Kill feed | Top-right | Class icon + victim class icon; momentum transfer shown with arrow |
| Ammo | Bottom-right | Current magazine / reserve |

### Screens
- **Main menu** (SlimUI base): Play, Party, Progression, Settings
- **Class select:** Full-body class preview, ability tooltip, team composition summary
- **Post-match:** Score card, awards, XP breakdown, highlight replay button, "Play Again" + "Return to Menu"

---

## 9. Monetization (Post-MVP)

Not in vertical slice scope — design now to avoid painting into a corner.

- **Cosmetics only** — no pay-to-win, no stat-affecting purchases
- **Battle Pass** (seasonal, 50 tiers): character skins, weapon skins, ability VFX recolors, sprays, victory poses
- **Direct store**: individual cosmetic items; no loot boxes (avoids gambling regulation risk)
- **Free-to-play**: base game free; battle pass is the primary revenue driver
- All four base classes free; any future classes also free (Fortnite/Apex model)

---

## 10. Technical Overview

See [Tech Architecture](tech-architecture.md) for full detail.

- **Engine:** Unity 6, Universal Render Pipeline
- **Networking:** Unity Netcode for GameObjects + Unity Relay
- **Input:** Unity Input System (`InputSystem_Actions` asset)
- **Target FPS:** 60 locked (PC), 30 stable (mobile)
- **Max players per match:** 8 (4v4)
- **Server model:** Host-client for MVP (one player hosts via Relay); dedicated server post-MVP

---

## 11. Accessibility

Carrot Clash targets a broad audience. Accessibility features are not optional extras — they expand the player base and cost little to implement early.

| Feature | Detail |
|---|---|
| Colourblind modes | Protanopia / Deuteranopia / Tritanopia presets — alters class colour identifiers and momentum ring colours |
| Reduce motion | Disables screen shake, camera bob, heavy VFX animations (momentum tier effects simplified) |
| High contrast HUD | Increases contrast on HP bar, momentum ring, zone indicators |
| Text size | Normal / Large text globally |
| Subtitles | In-game voice callout text (e.g. "Enemy spotted" caption) — Phase 5 scope |
| Controller aim assist | Rotational aim assist for gamepad input (toggle) |
| Remappable controls | All bindings remappable on PC; controller layout presets for console-style mapping |

All options in Settings → Accessibility.

---

## 12. Mobile-Specific Design

URP mobile profile is already configured. Additional mobile considerations:

| Concern | Solution |
|---|---|
| Touch controls | Virtual joystick (left), fire/ADS/ability buttons (right); see [UI/UX](ui-ux.md) mobile layout |
| Match length | 8-minute matches are ideal for mobile sessions — no modification needed |
| Battery / thermal | Adaptive Performance asset already in project — use for dynamic quality scaling |
| Smaller screen | HUD scales proportionally; critical elements (HP, momentum ring) remain central |
| Network tolerance | NGO lag compensation max 200ms handles typical mobile network variance |
| Frame target | 30 FPS stable preferred over 60 FPS unstable on mobile |
