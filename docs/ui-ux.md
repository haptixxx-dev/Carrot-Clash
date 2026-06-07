# UI / UX Specification

Full screen-by-screen spec. Implemented with SlimUI Modern Menu 1 as the menu base; HUD is custom Unity UI (Canvas, Screen Space - Overlay).

---

## Screen Flow

```
[Boot / Loading]
       ↓
  [Main Menu]
  ├── [Play] → [Matchmaking] → [Class Select] → [Match]
  │                                                 ↓
  │                                          [Post-Match] ──→ [Main Menu]
  │                                                 └──→ [Rematch / Class Select]
  ├── [Party] → [Friend List / Invite]
  ├── [Progression] → [Battle Pass] / [Challenges] / [Player Level]
  └── [Settings] → [Video / Audio / Controls / Account]
```

---

## 1. Main Menu

```
┌─────────────────────────────────────────────────────────┐
│  🥕 CARROT CLASH          [Level 14 ▓▓▓▓▓░ 2400/3000]  │
│                                                          │
│                  [ PLAY ]                               │
│                  [ PARTY ]                              │
│                  [ PROGRESSION ]                        │
│                  [ SETTINGS ]                           │
│                  [ QUIT ]                               │
│                                                          │
│  ┌──────────────────┐  ┌──────────────────┐            │
│  │ DAILY CHALLENGES │  │  BATTLE PASS     │            │
│  │ ✓ Get 10 kills   │  │  Season 1 · Tier │            │
│  │ ○ Win a match    │  │  ████░░  12/50   │            │
│  │ ○ Reach Tier 3   │  │  Next: 🥕 skin  │            │
│  └──────────────────┘  └──────────────────┘            │
└─────────────────────────────────────────────────────────┘
```

**Design notes:**
- Daily challenges and battle pass progress visible immediately on login — no clicks needed to see progress
- Level XP bar always visible top-right; satisfying progress for every session
- "PLAY" button is visually dominant — no friction to the core loop
- Background: animated live match footage or stylized food-world environment scene

---

## 2. Matchmaking Screen

```
┌─────────────────────────────────────────────────────────┐
│                  FINDING MATCH                          │
│                  ● ● ●  (animated)                      │
│                                                          │
│              Estimated wait: ~45s                        │
│                                                          │
│   PARTY (2/4)                                           │
│   [You]         Carrot    ████ Level 14                 │
│   [Friend_01]   Jalapeño  ████ Level 8                  │
│   [+ Invite]                                            │
│   [+ Invite]                                            │
│                                                          │
│                         [CANCEL]                        │
└─────────────────────────────────────────────────────────┘
```

---

## 3. Class Select Screen

30-second timer. Auto-selects most-recently-used class if idle.

```
┌─────────────────────────────────────────────────────────┐
│  CLASS SELECT          TEAM A vs TEAM B      [28s ▓░░]  │
│                                                          │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐                  │
│  │CARROT│ │ JAL. │ │BROCC.│ │POTAT.│                   │
│  │  🥕  │ │  🌶  │ │  🥦  │ │  🥔  │                   │
│  │ ★★★  │ │ ★★   │ │ ★★★★ │ │ ★★   │                   │
│  └──────┘ └──────┘ └──────┘ └──────┘                   │
│    Scout   Brawler  Support   Tank                       │
│                                                          │
│  [Selected: CARROT]                                      │
│  HP: 90   Speed: Fast   Weapon: SMG "The Nub"           │
│                                                          │
│  [Sprint Dash] [Radar Pulse]  Passive: Silent Steps     │
│   6s cooldown   18s cooldown                            │
│                                                          │
│  YOUR TEAM          ENEMY TEAM                          │
│  You  → CARROT      ? ? ? ?                             │
│  P2   → JALAPEÑO    ? ? ? ?                             │
│  P3   → ?           ? ? ? ?                             │
│  P4   → ?           ? ? ? ?                             │
│                                                          │
│                    [CONFIRM] ←── auto-confirms at 0     │
└─────────────────────────────────────────────────────────┘
```

**Design notes:**
- Enemy class selections hidden until match starts (prevents last-second counter-picking)
- Difficulty stars shown — steers new players to lower-difficulty classes
- Ability preview tooltip on hover: shows range, cooldown, effect description
- Team composition shown live — if your team has no support, Broccoli icon subtly highlighted

---

## 4. In-Match HUD

```
┌─────────────────────────────────────────────────────────┐
│  TEAM A [████░░] 312        5:42       TEAM B [████░░] 287 │
│          Zone A: 🔴  Zone B: 🟡  Zone C: 🔒            │
│                                                [KILLFEED]│
│                                          🥕 → 🌶        │
│                                          🥦 → 🥔        │
│                                                          │
│                                                          │
│                          (·)  ← crosshair               │
│                       ◌─────◌  ← momentum ring          │
│                                                          │
│                                                          │
│                                                          │
│  HP ████████░  72/90        🥕      🎯 28 / 30          │
│                          Carrot     ammo / reserve       │
│   [Q Sprint Dash]  [E Radar Pulse]                      │
│    ████████  6.0s   ██░░░░░ 14.2s                       │
└─────────────────────────────────────────────────────────┘
```

### HUD Element Specifications

#### Team Score Bar (Top Center)
- Both team scores displayed as number + fill bar
- Fill bar reflects progress toward 500 cap
- Leading team bar pulses subtly green, trailing team pulses amber
- Match timer centered between scores; turns red at <60s remaining

#### Zone Ownership Indicators (Below Scores)
- Three small icons (A, B, C) with colour:
  - Grey = neutral / not yet captured
  - Your team colour = owned
  - Enemy colour = enemy owned
  - Flashing = being contested
  - 🔒 icon = locked (Zone C pre-minute 5)
- Tap/click on icon shows capture progress tooltip

#### Kill Feed (Top Right)
- Class icon (attacker) → arrow → class icon (victim)
- If tier transfer: small "+1 tier" badge on attacker icon
- Maximum 4 entries visible; oldest fades first
- Duration: 5s per entry
- Your kill: entry flashes gold border for 0.5s

#### Crosshair
- Default: white dot with 4 very short lines (minimal — not a distracting UI)
- Expands on hipfire; contracts on ADS
- Turns red for 0.1s on hit confirmation
- Turns orange/gold on kill confirmation
- Customizable in settings (dot, cross, circle, none)

#### Momentum Ring
- Thin ring around crosshair (12px stroke, radius ~30px)
- Fill: clockwise from 12 o'clock
- Colours: grey (tier 0) → class colour (tier 1) → bright warm (tier 2) → animated fire orange (tier 3)
- Decay countdown: when < 3s before tier decay, ring pulses with a slow flicker
- Tier-up animation: ring flashes white, then fills new colour over 0.3s (tweened)
- Breaking to tier 0 (death): ring shatters particle effect, then dims

#### HP Bar (Bottom Left)
- Horizontal bar; green → yellow → red as HP decreases
- Below 30 HP: bar pulses red + subtle vignette on screen edges
- Starch Armor: yellow segment appears above the base HP bar (temporary HP visualized on top)
- Regen ticking: bar fills with a smooth animation (not instant jumps) so player feels the heal

#### Ability Slots (Bottom Center, flanking the momentum ring)
| Slot | Key | Position |
|---|---|---|
| Active 1 | Q | Bottom-center left |
| Active 2 | E | Bottom-center right |

- Cooldown: fill drains clockwise from top (same convention as ring) in greyscale overlay
- Ready: icon fully lit; brief flash animation
- On activation: icon flashes white, drain begins immediately
- Passive icon shown below ability slots, always lit (no cooldown), greyed if passive has a condition not met

#### Ammo (Bottom Right)
- `28 / 30` format (current mag / mag size; pistol not shown unless active)
- Below 30% remaining: turns orange
- At 0: flashes red + "RELOAD" text
- Auto-reload indicator: small spinning icon during reload

---

## 5. Post-Match Screen

```
┌─────────────────────────────────────────────────────────┐
│                  TEAM A WINS  🏆                        │
│                  312  vs  287                           │
│                                                          │
│  ┌──────────────────────────────────────────────────┐  │
│  │  MVP    [Carrot] You         18 kills  420 pts   │  │
│  │  🔥 HOT STREAK  [Jalapeño] Friend_01  94s on fire│  │
│  └──────────────────────────────────────────────────┘  │
│                                                          │
│  SCOREBOARD                                              │
│  Player      Class   K   A   D   Obj Time   Score       │
│  You         🥕      18   4   3    142s      312         │
│  Friend_01   🌶       9   6   5     88s      198         │
│  P3          🥦       3  14   7    201s      187         │
│  P4          🥔       6   3   4    198s      176         │
│  ──────────────────────────────────────                 │
│  Enemy1      🌶      11   3   6    115s      231         │
│  ...                                                     │
│                                                          │
│  XP EARNED              Level 14 → [=====░] 200/3000   │
│  Win bonus:    +200     ████████████████████░░░         │
│  18 Kills:     +270     Gaining 570 XP...  ← animated  │
│  4 Assists:     +32                                      │
│  Obj time:     +48                                       │
│  Hot Streak:    +30                                      │
│  ─────────────                                          │
│  Total:        +570                                      │
│                                                          │
│  Battle Pass: Tier 12 → ██████████░░ 650/1000          │
│                                                          │
│  [🔁 REMATCH  2/4 voted]  [▶ PLAY AGAIN]  [🏠 MENU]   │
└─────────────────────────────────────────────────────────┘
```

**Design notes:**
- XP gains animate with a count-up over ~3 seconds — every number ticking up is a dopamine beat
- Battle pass tier progress shown even if tier wasn't gained — "you're 650/1000 toward the next tier" is always motivating
- "2/4 voted rematch" count updates live — social pressure to vote rematch
- Highlight clip button appears after 2s for the Hot Streak award holder (auto-generated 10s clip)

---

## 6. Settings Screen

### Video
- Resolution, fullscreen / windowed / borderless
- Quality preset (Low / Medium / High / Ultra)
- VSync, target frame rate
- FOV slider (80–110°)

### Audio
- Master, Music, SFX, Voice (separate sliders)
- Spatial audio on/off
- Subtitles (voice callout subtitles, on/off)

### Controls
- Mouse sensitivity (X/Y independent)
- ADS sensitivity multiplier
- Key bindings (displayed, click to rebind)
- Controller support (dead zones, stick sensitivity)

### Accessibility
- Colourblind mode (Protanopia / Deuteranopia / Tritanopia presets) — alters class colour language
- High contrast HUD mode
- Reduce motion (disables screen shake, camera bob, momentum VFX animations)
- Text size (Normal / Large)
- Aim assist (toggle, for controller)

---

## 7. Mobile HUD Adjustments

On mobile (portrait or landscape), the HUD reorganises for thumb reach:

```
┌──────────────────────────────────────┐
│  [A 312]    5:42    [B 287]          │
│  Zone indicators (small row)         │
│                  ·  ← crosshair      │
│               ◌─────◌               │
│                                      │
│  HP ████  72/90    ammo 28/30       │
│                                      │
│  [Joystick]    [Jump][Crouch]       │
│                [Q Ability 1]        │
│                [E Ability 2]        │
│                   [Fire] [ADS]      │
└──────────────────────────────────────┘
```

- Virtual joystick: left thumb zone (move)
- Fire button: right thumb zone
- Ability buttons: right-side cluster above fire
- Crosshair + ring: screen center, always visible
- Touch-to-ADS: available as alternative to dedicated button in settings
