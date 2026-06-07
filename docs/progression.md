# Progression & Retention

Systems that bring players back every session. All progression is cosmetic — no stat advantages.

---

## XP System

### Earning XP
Every match awards XP. Losing still gives meaningful XP — critical for retention.

| Event | XP |
|---|---|
| Match participation (win) | 200 |
| Match participation (loss) | 100 |
| Kill | 15 |
| Assist | 8 |
| Objective capture | 25 |
| Objective time held (per 30s) | 10 |
| Hot Streak (tier 3 for 60+ seconds) | 30 bonus |
| MVP award | 50 bonus |
| Daily challenge completed | 150–300 |
| Weekly challenge completed | 500–1000 |

**Typical match XP:**
- Active win: ~400–600 XP
- Active loss: ~250–400 XP
- Post-match XP breakdown is animated (numbers tick up) — even a loss feels rewarding

### Player Level
- Global level displayed on profile and in match lobby (visible to all)
- Levels 1–100, then "Prestige" (resets visual level, awards unique icon)
- XP per level scales gently: levels 1–25 are fast, 26–50 moderate, 51–100 slower
- No gameplay rewards at any level — purely social signalling

---

## Battle Pass (Seasonal)

One season = 10 weeks. Battle Pass has 50 tiers, unlocked with XP earned during the season.

### Tier Types

| Tier | Reward type | Example |
|---|---|---|
| Every 5 tiers | Character skin | "Roasted Carrot" skin for Carrot |
| Every 10 tiers | Weapon skin | Charred finish on Pepper Blaster |
| Every 15 tiers | Ability VFX recolor | Jalapeño's Heat Trail as purple flame |
| Every 20 tiers | Victory pose | Carrot breakdance animation |
| Tier 50 | Prestige cosmetic | Holographic "Season 1 Champion" icon + skin |
| Scattered | Sprays, player cards, titles | "On Fire" title, spray paint |

### Free vs. Paid track
- **Free track (10 tiers):** unlocks early-season content, playable cosmetics, 1 full character skin
- **Paid track (50 tiers, ~$10):** full cosmetic set; no gameplay advantage

### FOMO Mechanics (Ethical)
- Season cosmetics never return to the store after the season ends — creates genuine exclusivity
- "Last week of season" banner in main menu — encourages logging in during season close
- Battle Pass does NOT expire during the season — time investment, not money urgency

---

## Daily & Weekly Challenges

### Daily Challenges (3 per day, resets at midnight UTC)
Generated from a pool, randomized per player so not all players chase same behaviour.

| Example Challenge | XP Reward |
|---|---|
| Get 10 kills in a single match | 200 |
| Capture 3 objectives | 150 |
| Reach Momentum Tier 3 in a match | 150 |
| Win a match with your team | 150 |
| Deal 500 damage with Jalapeño | 200 |
| Perform 5 assists | 150 |
| Hold an objective for 60 seconds | 150 |

**Reroll:** 1 free reroll per day per challenge. Paid reroll with premium currency (soft money gate, not hard).

### Weekly Challenges (5 per week, resets Monday)
Higher XP, requires sustained effort.

| Example Challenge | XP Reward |
|---|---|
| Win 10 matches | 1000 |
| Earn Hot Streak award 5 times | 800 |
| Get 50 kills with any class | 700 |
| Capture 20 objectives total | 700 |
| Play 3 matches with each class | 600 |

---

## First Session Experience (FTUE)

Retention is won or lost in the first 10 minutes. The FTUE must deliver the momentum system's feeling before the player closes the game.

### Tutorial Match (forced, skippable after first completion)
1. Load into a solo practice map (subset of Zone A)
2. **Step 1:** Shoot a target dummy — introduces weapon feel and hitscan feedback
3. **Step 2:** Kill 3 dummies in sequence — momentum tier 1 → 2 → 3 triggered, VFX shown
4. **Step 3:** "You're On Fire! Now don't die." — tier decay demonstrated
5. **Step 4:** Capture a zone — objective UI introduced
6. **Step 5:** Use Ability 1 (Dash for Carrot) — ability system introduced
7. **Exit:** "Now play a real match" CTA, battle pass intro screen

### Post-Tutorial Onboarding (first 3 real matches)
- Tooltip overlays for first zone contest ("Hold this zone to score!")
- First kill gives animated "First Blood" callout
- First tier 3 triggers a unique one-time "YOU'RE ON FIRE" full-screen pop
- Progress bar toward first battle pass reward visible immediately

---

## Social Systems

### Party System
- Invite friends from main menu or post-match screen
- Party leader queues for the group
- Party composition shown in class select lobby

### Post-Match Share
- "Hot Streak" replay clip (10s, auto-generated from kill-chain) with "Share" button
- Generates a short video clip using Unity Recorder (dev) or a screenshot with stats overlaid
- Share target: clipboard image or direct social media intent URL

### Recent Players
- Post-match lobby shows all 8 players; one-click to friend-request or mute

---

## Ranked Mode (Post-MVP, Season 2+)

Not in vertical slice scope — design now so the foundation supports it.

### Rank Tiers
`Seedling → Sprout → Veggie → Ripe → Elite → Champion`

- Placement matches (5) assign initial rank
- Win/lose SR (Skill Rating) per match
- Season rank resets to calibration (soft reset — seeded from previous SR)
- Separate from cosmetic progression — ranked rewards are season-exclusive cosmetics

### Ranked Queue Rules
- 4v4 only (not 3v3 — more predictable balance)
- Party size limit: full 4-stack or solo only (no 2+2 mixed to reduce carry dynamics)
- Dodge penalty: -20 SR, 5-minute queue ban
