# Progression & Retention

Systems that bring players back every session. All progression is cosmetic — no stat advantages.

::: info CODE STATUS (2026-06)
The core progression backend is implemented in `Assets/_Game/Progression/` (`XPManager`, `BattlePassService`, `ChallengeSystem`, `ProgressionService`, `SaveSystem`, `ProgressionData`) and is exercised by the main menu controller. Award values below were cross-checked against that code and match the design unless a `::: info` delta box says otherwise. UI canvases, social/party networking, ranked, and the FTUE tutorial scene are still editor-pending or not yet built — see the per-section callouts. Engineers: start at [/dev/getting-started](/dev/getting-started) and treat `Assets/_Game/CONTRACTS.md` as the frozen API.
:::

---

## XP System

<span class="cc-status built">Implemented</span> — `XPManager.cs` (award table, 1–100 level curve, prestige) and `ProgressionService.AwardMatch` are code-complete and verified. The match-end flow that calls `AwardMatch` and the XP-breakdown HUD widget are wired in code; the post-match canvas art is editor-pending.

### Earning XP
Every match awards XP. Losing still gives meaningful XP — critical for retention.

These per-match values are the exact constants in `XPManager`:

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

Challenge rewards (awarded by `ChallengeSystem`, not the per-match table):

| Event | XP |
|---|---|
| Daily challenge completed | 150–200 |
| Weekly challenge completed | 600–1000 (design only) |

::: info Code delta — challenge reward range
The original doc listed dailies at **150–300** and weeklies at **500–1000**. The shipped daily pool (`ChallengeSystem.MakeChallenge`) actually awards **150 or 200** XP per daily. Weeklies are a design concept only — there is **no weekly-challenge code yet** (see the Daily & Weekly section).
:::

**Typical match XP:**
- Active win: ~400–600 XP
- Active loss: ~250–400 XP
- Post-match XP breakdown is animated (numbers tick up) — even a loss feels rewarding

`XPManager.ComputeMatchXp` sums these from the player's `PlayerStats`; `ProgressionService.AwardMatch` then feeds the same total into **both** the global level track and the battle pass track.

### Player Level
- Global level displayed on profile and in match lobby (visible to all)
- Levels 1–100 (`XPManager.MaxLevel = 100`), then "Prestige" (`Prestige_Reset` resets visual level to 1, increments a prestige counter)
- XP per level scales gently: levels 1–25 are fast (band ×1.0), 26–50 moderate (×1.6), 51–100 slower (×2.4), over a linear core ramp (`500 + (level-1)·90`, rounded to the nearest 10)
- No gameplay rewards at any level — purely social signalling

::: info Code delta — prestige spillover
Design intent says prestige resets the visual level "keeping spillover XP". The shipped `Prestige_Reset` resets `xp = 0` (no spillover carried). Flagged for a future pass if carry-over is desired.
:::

---

## Battle Pass (Seasonal)

<span class="cc-status partial">Partial</span> — `BattlePassService.cs` (50 tiers, tier rewards, XP accrual) is code-complete and the main-menu battle-pass card reads it live. The free/paid split and any store/purchase flow are **not implemented** (cosmetic, XP-only). UI canvas and the actual cosmetic assets are editor-pending.

One season = 10 weeks. Battle Pass has 50 tiers (`MaxTier = 50`), unlocked with XP earned during the season. The track is **flat-paced**: `XpPerTier = 1000` XP per tier (`BattlePassService.XpPerTier`).

### Tier Types
The reward cadence in `BattlePassService.GetReward` (later cadence wins where they overlap, e.g. tier 50 is the capstone):

| Tier cadence | Reward type | Example |
|---|---|---|
| Every 5 tiers | Character skin | "Roasted Carrot" skin for Carrot |
| Every 10 tiers | Weapon skin | Charred finish on Pepper Blaster |
| Every 15 tiers | Ability VFX recolor | Jalapeño's Heat Trail as purple flame |
| Every 20 tiers | Victory pose | Carrot breakdance animation |
| Tier 50 | Prestige cosmetic | Holographic "Season 1 Champion" (`bp_s1_prestige`) |
| Other tiers | Spray | Spray paint filler |

### Free vs. Paid track
- **Free track:** the first **10 tiers** (`FreeTrackTiers = 10`; each `BattlePassReward.IsFreeTrack` flag is set for tier ≤ 10) — unlocks early-season content, playable cosmetics, 1 full character skin
- **Paid track (50 tiers, ~$10):** full cosmetic set; no gameplay advantage

::: info Code delta — purchases & pacing
- There is **no purchase / premium-currency code**. `BattlePassService` only accrues XP, rolls up tiers, and records cosmetic ids onto the profile; the "paid track" and "~$10" are design intent for a later store implementation.
- Tier XP is **flat (1000/tier)**, not banded — predictable seasonal pacing by design.
- "Sprays, player cards, titles" collapse to a single `Spray` reward type in code today; player-cards/titles would be added as new `BattlePassRewardType` values.
:::

### FOMO Mechanics (Ethical)
- Season cosmetics never return to the store after the season ends — creates genuine exclusivity
- "Last week of season" banner in main menu — encourages logging in during season close
- Battle Pass does NOT expire during the season — time investment, not money urgency

---

## Daily & Weekly Challenges

<span class="cc-status partial">Partial</span> — Dailies are code-complete in `ChallengeSystem.cs` (generation, UTC-midnight reset, live progress from gameplay events, XP grant, free reroll) and feed the main-menu challenges card. Weeklies are design-only (no code). UI canvas is editor-pending.

### Daily Challenges (3 per day, resets at midnight UTC)
`ChallengeSystem.DailyCount = 3`. Generated from a 7-type pool, randomized per player (Fisher–Yates) so not all players chase the same behaviour. Roll-over is tracked on the profile (`lastDailyResetUtcTicks`) and refreshes at UTC midnight.

These are the exact daily templates and rewards in `ChallengeSystem.MakeChallenge`:

| Challenge | Target | XP Reward |
|---|---|---|
| Get 10 kills in a single match | 10 | 200 |
| Capture 3 objectives | 3 | 150 |
| Reach Momentum Tier 3 in a match | 1 | 150 |
| Win a match with your team | 1 | 150 |
| Deal 500 damage in a single match | 500 | 200 |
| Perform 5 assists | 5 | 150 |
| Hold an objective for 60 seconds | 60 | 150 |

::: info Code delta — "Deal 500 damage" is class-agnostic
The doc previously said "Deal 500 damage **with Jalapeño**". The shipped challenge is **"Deal 500 damage in a single match"** with any class (it records whichever class the local player dealt damage with). A per-class variant would need a target-class field on the challenge.
:::

**Reroll:** `FreeRerollsPerDay = 3` (one free reroll per challenge per day; consumed via `Reroll(index)`, which swaps in a distinct unused type). Completed challenges cannot be rerolled. There is **no paid reroll** in code — the "premium currency" reroll is a future soft-gate, not yet built.

### Weekly Challenges (design only — not yet built)
<span class="cc-status pending">Editor-pending</span> — No weekly-challenge system exists in code yet; only the daily set is implemented. Higher XP, requires sustained effort. Target reward values for the future implementation:

| Example Challenge | XP Reward |
|---|---|
| Win 10 matches | 1000 |
| Earn Hot Streak award 5 times | 800 |
| Get 50 kills with any class | 700 |
| Capture 20 objectives total | 700 |
| Play 3 matches with each class | 600 |

---

## First Session Experience (FTUE)

<span class="cc-status pending">Editor-pending</span> — No tutorial/FTUE code or scene exists yet. The momentum, ability, objective, and combat-feedback systems the tutorial would showcase are all implemented; the guided tutorial scene, step scripting, and onboarding overlays are pending.

Retention is won or lost in the first 10 minutes. The FTUE must deliver the momentum system's feeling before the player closes the game.

### Tutorial Match (forced, skippable after first completion)
1. Load into a solo practice map (subset of Zone A)
2. **Step 1:** Shoot a target dummy — introduces weapon feel and hitscan feedback
3. **Step 2:** Kill 3 dummies in sequence — momentum tier 1 → 2 → 3 triggered, VFX shown
4. **Step 3:** "You're On Fire! Now don't die." — tier decay demonstrated
5. **Step 4:** Capture a zone — objective UI introduced
6. **Step 5:** Use Ability 1 (Dash for Carrot) — ability system introduced
7. **Exit:** "Now play a real match" CTA, battle pass intro screen

::: tip Building blocks already exist
`TargetDummy` (step 1–2 targets), the momentum tier system + tier VFX hooks (step 2–3), `CaptureZone` + HUD objective widget (step 4), and the ability behaviours (step 5) are all implemented — the FTUE is mostly scene authoring + step orchestration on top of shipped systems.
:::

### Post-Tutorial Onboarding (first 3 real matches)
- Tooltip overlays for first zone contest ("Hold this zone to score!")
- First kill gives animated "First Blood" callout
- First tier 3 triggers a unique one-time "YOU'RE ON FIRE" full-screen pop
- Progress bar toward first battle pass reward visible immediately

---

## Social Systems

<span class="cc-status pending">Editor-pending</span> — A party button/panel stub exists in `MainMenuController`, but there is no party/matchmaking networking behind it. Networking depends on Netcode for GameObjects, which is **not installed yet** (the network layer is dormant behind `#if NETCODE_PRESENT`). Share/recent-players are unbuilt.

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

<span class="cc-status pending">Editor-pending</span> — No ranked code exists. Out of vertical-slice scope by design; documented here so the foundation (match state machine, `MatchStats`, MVP/Hot-Streak awards) can support it later.

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

---

## Persistence

<span class="cc-status built">Implemented</span> — `SaveSystem.cs` persists the full `PlayerProfile` (level, XP, prestige, battle-pass tier/XP, owned cosmetics, daily challenges + reroll budget) as JSON to `Application.persistentDataPath`, with a PlayerPrefs fallback (e.g. WebGL) and corrupt-save recovery to a fresh profile. Single-player local save today; cloud save would arrive with the online backend.
