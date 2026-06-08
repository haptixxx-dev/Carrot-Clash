# Core Loop

::: info CODE STATUS
The full core-loop logic is implemented in C# (`CarrotClash` namespace) and verified against this doc. What is **editor-pending** is the content that brings it to life: the gameplay scene, the capture-zone / spawn-point placement, audio cues, and HUD canvases. Numbers below have been reconciled against the shipped code; see the `GameConstants.cs` references throughout. Engineers should start at [/dev/getting-started](/dev/getting-started) and treat `Assets/_Game/CONTRACTS.md` as the frozen API.
:::

## Match state machine

<span class="cc-status built">Implemented</span>. `GameModeManager.cs` owns the state machine; states live in `GameEnums.cs` (`MatchState`).

```
LOBBY → CLASS_SELECT → COUNTDOWN → MATCH_ACTIVE → SUDDEN_DEATH* → MATCH_END → POST_MATCH
```

::: info CODE vs DESIGN
The shipped `MatchState` enum is `Lobby, ClassSelect, Countdown, MatchActive, SuddenDeath, MatchEnd, PostMatch`, exactly the flow above. In single-machine/offline play `GameModeManager.BeginMatchFlow()` starts the coroutine at `ClassSelect`. The `Lobby` hold is a networking concern handled by the dormant `ConnectionManager` / `GameModeNetworkManager`, behind `#if NETCODE_PRESENT`. All durations below come from `GameConstants.cs`.
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

All seven durations/caps are pulled from `GameConstants.cs` (`MatchDuration = 480`, `SuddenDeathDuration = 60`, `CountdownDuration = 5`, `ClassSelectDuration = 30`, `PostMatchDuration = 15`, `ScoreCap = 500`). The doc and code agree.

---

## Match structure

<span class="cc-status built">Implemented</span>, orchestrated by `GameModeManager.MatchFlow()`. <span class="cc-status pending">Editor-pending</span> for spawn-point and zone placement in `Gameplay_Market`.

1. **Class select.** 30s to pick class; server locks selection; duplicate classes allowed
2. **Drop in.** Teams spawn on opposite ends (Spawn A, Spawn B), 60m apart
3. **Objective phase.** Zones A and B immediately contestable; teams earn score from both holds and kills
4. **Zone C unlock.** At `matchTime == 300s` (minute 5), barriers drop with a global audio cue; Zone C scores at 2× rate
5. **Win condition.** First team to 500 points wins outright; at `matchTime == 480s`, highest score wins
6. **Sudden death.** If scores are tied at timer expiry, 60s overtime (Zone C only); next team to score wins

---

## Respawn system

<span class="cc-status built">Implemented</span>. `SpawnManager.cs` handles respawn timing, the contest check, and the safety redirect. <span class="cc-status pending">Editor-pending</span>: `SpawnPoint` placement behind cover, death-camera rig.

- **Respawn time:** 4 seconds flat. No scaling, no penalty for streak.
- **Respawn invulnerability:** 2 seconds after spawn; player can move but cannot be damaged
- **Spawn protection logic:** players spawn at their team's fixed spawn point unless it is being actively contested (within 10m of an enemy). When contested, they respawn at the nearest safe teammate within 30m, or at the fixed spawn with a 1s extra delay
- **Death camera:** spectate the killer or nearest alive teammate for the 4s respawn duration
- **No spawn kill design:** fixed spawns sit behind geometric cover; enemies must actively push to reach them

::: info CODE vs DESIGN
`SpawnManager.ChooseSpawn()` matches the design: (1) first uncontested team spawn, (2) else nearest safe teammate within `SafeTeammateRadius = 30m` (spawned ~2m behind them), (3) else fixed spawn with `extraDelay = 1s` **and** `invuln = 3s` (vs the standard `SpawnInvulnerability = 2s`). The original doc only implied that extended 3s invuln on the contested-fallback path, so flag it for playtesters. The death-camera spectate behaviour is a design target; the camera rig itself is editor-pending.
:::

---

## Team composition

<span class="cc-status built">Implemented</span>. Team caps live in `GameConstants.cs` (`MaxPlayersPerTeam = 4`, `MaxPlayers = 8`); duplicate classes allowed (`ClassSelectController`). The matchmaking floor (3v3) is a networking policy and is editor/backend-pending.

- **Team size:** 3v3 (min viable match) or 4v4 (standard)
- **Duplicate classes:** allowed, no hard lock
- **Mechanical incentive to diversify:** Broccoli's regen aura only works on allies (not self), so a full-damage team is slightly weaker in sustain; Potato's zone anchor ability is near-useless without teammates to protect. These are soft nudges, not enforced rules.
- **Minimum to start:** 2v2 (for testing/private matches); matchmaking requires 3v3+

---

## Score system

<span class="cc-status built">Implemented</span>. Values live in `GameConstants.cs`; scoring is awarded by `CaptureZone.cs` (ticks/captures) and `GameModeManager.HandleKillScore` (kills, tier-aware). The score table below matches the code exactly.

| Event | Score | Notes |
|---|---|---|
| Zone tick (A or B, held per second) | +1 | Requires full capture (100% progress) |
| Zone tick (C, held per second) | +2 | Only active after minute 5 |
| Zone captured (neutral → team) | +10 | One-time on capture completion |
| Zone captured (contested → team) | +15 | One-time; harder to achieve |
| Enemy eliminated | +5 | Per kill; credited to killing team |
| Enemy eliminated (On Fire, tier 3) | +8 | Replaces +5, not additive |
| Assist | +2 | Last 5s of damage contribution before kill |

### Score balance math
At 4v4 with 50% zone control:
- Team holding 1 zone (Zone A only) for the full 8 minutes: 480 pts from ticks alone
- Hitting 500 requires meaningful kills or capturing a second zone
- A 4-kill hot streak adds 32 pts; a 10-kill match adds ~80 pts from kills alone
- In practice most matches end between 420-480, so the cap only hits in high-performance matches

### Score cap calibration
- 500 feels achievable (it creates match-ending tension) without being hit in 4 minutes by dominant teams
- If playtesting shows matches ending too fast (< 5 min): raise cap to 600 or lower Zone C tick rate
- If matches rarely end early: lower cap to 450 or increase kills' score contribution

---

## Feedback loops (addiction architecture)

<span class="cc-status partial">Partial</span>. The mechanics exist: momentum tiers in `MomentumController`, capture progress in `CaptureZone`, kill feed/HUD widgets in `UI/HUD/`, progression in `Progression/`. The VFX, animated counters, and audio that make them *feel* satisfying are <span class="cc-status pending">Editor-pending</span> (art/audio/canvases not yet authored).

These are designed to create the "one more game" pull.

### Within a match
- **Momentum tier VFX.** Visible power escalation is physically satisfying; players want to maintain it.
- **Zone capture progress bar.** Near-captures motivate more than finishing far behind. Captures take 6-12s by design, so near-misses happen constantly.
- **Kill feed.** Every kill is a visible event; watching your team string kills creates vicarious momentum.
- **Zone C unlock announcement.** A guaranteed drama spike at minute 5 that even gives a losing team a comeback window.

### Between matches
- **Post-match XP counter.** Animated and always non-zero; even losses give visible reward.
- **Battle pass tier progress.** Shows how close you are to the next tier reward.
- **Daily challenge completion.** "2/3 challenges done today" shows in the main menu, which drives the "finish the set" itch.
- **Rank delta.** In ranked, SR gain/loss is displayed prominently. Loss streaks motivate recovery; win streaks feel like achievement.
- **Rematch vote.** Reduces friction to another session; a team of 4 friends can stay together indefinitely.

---

## Match timer

<span class="cc-status built">Implemented</span>. `GameModeManager.Tick()` drives the timer, the Zone C unlock cue, and the 1-minute / 30-second warnings. <span class="cc-status pending">Editor-pending</span>: the announcement audio clips (`zone_c_unlock`, `warn_1min`, `warn_30s`) and the HUD flash.

| Time | Event |
|---|---|
| 0:00 | Match start; Zones A and B contestable |
| 5:00 | Zone C unlocks (3:00 remaining, middle of match) |
| 7:00 | "1 minute remaining" audio announcement |
| 7:30 | "30 seconds" audio + HUD flash |
| 8:00 | Match end (or sudden death if tied) |
| 9:00 | Sudden death end |

---

## End state

<span class="cc-status built">Implemented</span>. `MatchStats.cs` handles MVP / Hot Streak / per-player stats; `GameModeManager.EndMatch` and `PostMatchController.cs` drive the rest. <span class="cc-status pending">Editor-pending</span>: the post-match UI canvas, animated XP breakdown, and rematch-vote networking.

- **Win/Loss/Draw.** A draw is only possible if sudden death also ends tied (rare). `GameModeManager.SuddenDeath()` ends in `Team.None` (draw) if still tied after 60s.
- **Post-match screen shows:**
  - Final scores (both teams), winner banner
  - MVP card: player with the highest score contribution
  - Hot Streak award: player with the longest unbroken momentum tier 3 duration (`PlayerStats.LongestOnFireStreak`)
  - Personal stats: kills, assists, deaths, objective time, damage dealt
  - XP earned (animated breakdown)
  - Battle pass progress update
  - "Rematch" vote + "Play Again" + "Main Menu"

::: info CODE vs DESIGN
The shipped MVP metric (`PlayerStats.ScoreContribution`) is `Kills × 5 + Assists × 2 + round(ObjectiveTime)`, so it also folds in **assists**, where the original doc said only "kills + objective time". Hot Streak resolution (`MatchStats.ResolveHotStreak`) returns no winner when nobody reached tier 3, so the award can be absent in low-momentum matches.
:::
