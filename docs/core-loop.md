# Core Loop

::: info CODE STATUS
The full core-loop logic is implemented in C# (`CarrotClash` namespace) and verified against this doc. What is **editor-pending** is the content that brings it to life: the gameplay scene, the capture-zone / spawn-point placement, audio cues, and HUD canvases. Numbers below have been reconciled against the shipped code — see the `GameConstants.cs` references throughout. Engineers should start at [/dev/getting-started](/dev/getting-started) and treat `Assets/_Game/CONTRACTS.md` as the frozen API.
:::

## Match State Machine

<span class="cc-status built">Implemented</span> — `GameModeManager.cs` owns the state machine; states in `GameEnums.cs` (`MatchState`).

```
LOBBY → CLASS_SELECT → COUNTDOWN → MATCH_ACTIVE → SUDDEN_DEATH* → MATCH_END → POST_MATCH
```

::: info CODE vs DESIGN
The shipped `MatchState` enum is `Lobby, ClassSelect, Countdown, MatchActive, SuddenDeath, MatchEnd, PostMatch` — exactly the flow above. In single-machine/offline play `GameModeManager.BeginMatchFlow()` starts the coroutine at `ClassSelect` (the `Lobby` hold is a networking concern handled by the dormant `ConnectionManager` / `GameModeNetworkManager`, behind `#if NETCODE_PRESENT`). All durations below come from `GameConstants.cs`.
:::

| State | Duration | Notes |
|---|---|---|
| `LOBBY` | Until full (max 30s wait) | Host waits; join in progress allowed up to countdown |
| `CLASS_SELECT` | 30s (auto-pick random if idle) | Client-side UI; selection locked server-side at countdown |
| `COUNTDOWN` | 5s | Players visible, no input; audio cue + timer |
| `MATCH_ACTIVE` | 480s (8 min) | All gameplay; Zone C unlocks at 180s remaining (minute 5) |
| `SUDDEN_DEATH` | 60s (only if tied at end) | Zone C only; scoring continues; no respawn delay change |
| `MATCH_END` | Instant | Triggers on score cap (500) or timer expiry |
| `POST_MATCH` | 15s auto-advance, can skip | XP awarded, MVP displayed, rematch vote |

All seven durations/caps are pulled from `GameConstants.cs` (`MatchDuration = 480`, `SuddenDeathDuration = 60`, `CountdownDuration = 5`, `ClassSelectDuration = 30`, `PostMatchDuration = 15`, `ScoreCap = 500`) — the doc and code agree.

---

## Match Structure

<span class="cc-status built">Implemented</span> — orchestrated by `GameModeManager.MatchFlow()`; <span class="cc-status pending">Editor-pending</span> for spawn-point and zone placement in `Gameplay_Market`.

1. **Class select** — 30s to pick class; server locks selection; duplicate classes allowed
2. **Drop in** — teams spawn on opposite ends (Spawn A, Spawn B), 60m apart
3. **Objective phase** — Zones A and B immediately contestable; teams earn score from both holds and kills
4. **Zone C unlock** — at `matchTime == 300s` (minute 5), barriers drop with global audio cue; Zone C scores 2× rate
5. **Win condition** — first team to 500 points wins outright; at `matchTime == 480s`, highest score wins
6. **Sudden death** — if scores are tied at timer expiry, 60s overtime (Zone C only); next team to score wins

---

## Respawn System

<span class="cc-status built">Implemented</span> — `SpawnManager.cs` (respawn timing, contest check, safety redirect). <span class="cc-status pending">Editor-pending</span>: `SpawnPoint` placement behind cover, death-camera rig.

- **Respawn time:** 4 seconds flat (no scaling, no penalty for streak)
- **Respawn invulnerability:** 2 seconds after spawn; player can move but cannot be damaged
- **Spawn protection logic:** players spawn at their team's fixed spawn point unless it is being actively contested (within 10m of an enemy) — in that case, respawn at the nearest safe teammate within 30m, or fixed spawn with a 1s extra delay
- **Death camera:** spectate killer or nearest alive teammate for the 4s respawn duration
- **No spawn kill design:** fixed spawns are behind geometric cover; enemies must actively push to reach them

::: info CODE vs DESIGN
`SpawnManager.ChooseSpawn()` matches the design: (1) first uncontested team spawn, (2) else nearest safe teammate within `SafeTeammateRadius = 30m` (spawned ~2m behind them), (3) else fixed spawn with `extraDelay = 1s` **and** `invuln = 3s` (vs the standard `SpawnInvulnerability = 2s`). The extended 3s invuln on the contested-fallback path is a code detail the original doc only implied — call it out for playtesters. The death-camera spectate behaviour is a design target; the camera rig itself is editor-pending.
:::

---

## Team Composition

<span class="cc-status built">Implemented</span> — team caps in `GameConstants.cs` (`MaxPlayersPerTeam = 4`, `MaxPlayers = 8`); duplicate classes allowed (`ClassSelectController`). Matchmaking floor (3v3) is a networking policy and is editor/backend-pending.

- **Team size:** 3v3 (min viable match) or 4v4 (standard)
- **Duplicate classes:** allowed — no hard lock
- **Mechanical incentive to diversify:** Broccoli's regen aura only works on allies (not self), making a full-damage team slightly weaker in sustain; Potato's zone anchor ability is near-useless without teammates to protect — these are soft nudges, not enforced rules
- **Minimum to start:** 2v2 (for testing/private matches); matchmaking requires 3v3+

---

## Score System

<span class="cc-status built">Implemented</span> — values in `GameConstants.cs`; awarded by `CaptureZone.cs` (ticks/captures) and `GameModeManager.HandleKillScore` (kills, tier-aware). The score table below matches the code exactly.

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

<span class="cc-status partial">Partial</span> — the mechanics exist (momentum tiers in `MomentumController`, capture progress in `CaptureZone`, kill feed/HUD widgets in `UI/HUD/`, progression in `Progression/`). The VFX, animated counters, and audio that make them *feel* satisfying are <span class="cc-status pending">Editor-pending</span> (art/audio/canvases not yet authored).

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

<span class="cc-status built">Implemented</span> — `GameModeManager.Tick()` drives the timer, the Zone C unlock cue, and the 1-minute / 30-second warnings. <span class="cc-status pending">Editor-pending</span>: the announcement audio clips (`zone_c_unlock`, `warn_1min`, `warn_30s`) and the HUD flash.

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

<span class="cc-status built">Implemented</span> — `MatchStats.cs` (MVP / Hot Streak / per-player stats), `GameModeManager.EndMatch`, `PostMatchController.cs`. <span class="cc-status pending">Editor-pending</span>: the post-match UI canvas, animated XP breakdown, and rematch-vote networking.

- **Win/Loss/Draw** — draw only possible if sudden death also ends tied (rare). `GameModeManager.SuddenDeath()` ends in `Team.None` (draw) if still tied after 60s.
- **Post-match screen shows:**
  - Final scores (both teams), winner banner
  - MVP card: player with the highest score contribution
  - Hot Streak award: player with the longest unbroken momentum tier 3 duration (`PlayerStats.LongestOnFireStreak`)
  - Personal stats: kills, assists, deaths, objective time, damage dealt
  - XP earned (animated breakdown)
  - Battle pass progress update
  - "Rematch" vote + "Play Again" + "Main Menu"

::: info CODE vs DESIGN
The shipped MVP metric (`PlayerStats.ScoreContribution`) is `Kills × 5 + Assists × 2 + round(ObjectiveTime)` — i.e. it also folds in **assists**, where the original doc said only "kills + objective time". Hot Streak resolution (`MatchStats.ResolveHotStreak`) returns no winner when nobody reached tier 3, so the award can be absent in low-momentum matches.
:::
