# Core Loop

## Match State Machine

```
LOBBY → CLASS_SELECT → COUNTDOWN → MATCH_ACTIVE → SUDDEN_DEATH* → MATCH_END → POST_MATCH
```

| State | Duration | Notes |
|---|---|---|
| `LOBBY` | Until full (max 30s wait) | Host waits; join in progress allowed up to countdown |
| `CLASS_SELECT` | 30s (auto-pick random if idle) | Client-side UI; selection locked server-side at countdown |
| `COUNTDOWN` | 5s | Players visible, no input; audio cue + timer |
| `MATCH_ACTIVE` | 480s (8 min) | All gameplay; Zone C unlocks at 180s remaining (minute 5) |
| `SUDDEN_DEATH` | 60s (only if tied at end) | Zone C only; scoring continues; no respawn delay change |
| `MATCH_END` | Instant | Triggers on score cap (500) or timer expiry |
| `POST_MATCH` | 15s auto-advance, can skip | XP awarded, MVP displayed, rematch vote |

---

## Match Structure

1. **Class select** — 30s to pick class; server locks selection; duplicate classes allowed
2. **Drop in** — teams spawn on opposite ends (Spawn A, Spawn B), 60m apart
3. **Objective phase** — Zones A and B immediately contestable; teams earn score from both holds and kills
4. **Zone C unlock** — at `matchTime == 300s` (minute 5), barriers drop with global audio cue; Zone C scores 2× rate
5. **Win condition** — first team to 500 points wins outright; at `matchTime == 480s`, highest score wins
6. **Sudden death** — if scores are tied at timer expiry, 60s overtime (Zone C only); next team to score wins

---

## Respawn System

- **Respawn time:** 4 seconds flat (no scaling, no penalty for streak)
- **Respawn invulnerability:** 2 seconds after spawn; player can move but cannot be damaged
- **Spawn protection logic:** players spawn at their team's fixed spawn point unless it is being actively contested (within 10m of an enemy) — in that case, respawn at the nearest safe teammate within 30m, or fixed spawn with a 1s extra delay
- **Death camera:** spectate killer or nearest alive teammate for the 4s respawn duration
- **No spawn kill design:** fixed spawns are behind geometric cover; enemies must actively push to reach them

---

## Team Composition

- **Team size:** 3v3 (min viable match) or 4v4 (standard)
- **Duplicate classes:** allowed — no hard lock
- **Mechanical incentive to diversify:** Broccoli's regen aura only works on allies (not self), making a full-damage team slightly weaker in sustain; Potato's zone anchor ability is near-useless without teammates to protect — these are soft nudges, not enforced rules
- **Minimum to start:** 2v2 (for testing/private matches); matchmaking requires 3v3+

---

## Score System

| Event | Score | Notes |
|---|---|---|
| Zone tick (A or B, held per second) | +1 | Requires full capture (100% progress) |
| Zone tick (C, held per second) | +2 | Only active after minute 5 |
| Zone captured (neutral → team) | +10 | One-time on capture completion |
| Zone captured (contested → team) | +15 | One-time; harder to achieve |
| Enemy eliminated | +5 | Per kill; credited to killing team |
| Enemy eliminated (On Fire, tier 3) | +8 | Replaces +5, not additive |
| Assist | +2 | Last 5s of damage contribution before kill |

### Score Balance Math
At 4v4 with 50% zone control:
- Team holding 1 zone (Zone A only) for full 8 minutes: 480 pts from ticks alone
- Hitting 500 requires meaningful kills or capturing a second zone
- A 4-kill hot streak adds 32 pts; a 10-kill match adds ~80 pts from kills alone
- In practice most matches end between 420–480, so the cap hits in high-performance matches only

### Score Cap Calibration
- 500 feels achievable (creates match-ending tension) without being hit in 4 minutes by dominant teams
- If playtesting shows matches ending too fast (< 5 min): raise cap to 600 or lower Zone C tick rate
- If matches rarely end early: lower cap to 450 or increase kills' score contribution

---

## Feedback Loops (Addiction Architecture)

These are deliberately designed to create the "one more game" pull:

### Within a Match
- **Momentum tier VFX** — visible power escalation is physically satisfying; players want to maintain it
- **Zone capture progress bar** — near-captures are more motivating than finishing far behind; by design, captures take 6–12s so near-misses happen constantly
- **Kill feed** — every kill is a visible event; watching your team string kills creates vicarious momentum
- **Zone C unlock announcement** — guaranteed drama spike at minute 5; even a losing team gets a comeback window

### Between Matches
- **Post-match XP counter** — animated, always non-zero; even losses give visible reward
- **Battle pass tier progress** — visible how close you are to next tier reward
- **Daily challenge completion** — "2/3 challenges done today" visible in main menu; strong "finish the set" drive
- **Rank delta** — in ranked: SR gain/loss displayed prominently; loss streaks motivate recovery; win streaks feel like achievement
- **Rematch vote** — reduces friction to another session; team of 4 friends can stay together indefinitely

---

## Match Timer

| Time | Event |
|---|---|
| 0:00 | Match start; Zones A and B contestable |
| 5:00 | Zone C unlocks (3:00 remaining — middle of match) |
| 7:00 | "1 minute remaining" audio announcement |
| 7:30 | "30 seconds" audio + HUD flash |
| 8:00 | Match end (or sudden death if tied) |
| 9:00 | Sudden death end |

---

## End State

- **Win/Loss/Draw** — draw only possible if sudden death also ends tied (rare)
- **Post-match screen shows:**
  - Final scores (both teams), winner banner
  - MVP card: player with highest total score contribution (kills + objective time)
  - Hot Streak award: player with the longest unbroken momentum tier 3 duration
  - Personal stats: kills, assists, deaths, objective time, damage dealt
  - XP earned (animated breakdown)
  - Battle pass progress update
  - "Rematch" vote + "Play Again" + "Main Menu"
