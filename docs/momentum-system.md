# Momentum System

<span class="cc-status built">Implemented</span> — the tier model, decay, death-transfer, class passives, and HUD ring/overlay all exist and are verified in code. Tier VFX (model glow, particle trails, fire) are <span class="cc-status pending">Editor-pending</span> (no prefabs/particle assets authored yet). Network mirror is dormant — see [Network Sync](#network-sync).

The defining mechanic of Carrot Clash. Kills charge a per-player momentum meter that escalates your power — but death resets it and transfers half your charge to your killer.

::: info Code map
The spine is `Assets/_Game/Gameplay/Momentum/MomentumController.cs` (charge accumulator + decay + transfer), tuned by `MomentumConfigSO.cs` (per-tier multipliers) and `GameConstants.cs` (match-wide invariants). Class passives live in `Assets/_Game/Characters/Abilities/Impl/MomentumPassive_*.cs`. Default data values are emitted by the editor menu **Carrot Clash → Generate Default Data Assets** (`Editor/DataAssetGenerator.cs`). Every number below was cross-checked against that code — they all match the design.
:::

---

## Tiers

<span class="cc-status built">Implemented</span> — multipliers read from `MomentumConfigSO` per tier; the generated config matches this table exactly.

**Combat stats per tier** (multipliers applied to movement / cooldowns / damage):

| Tier | Trigger | Move speed | Ability cooldown | Damage bonus |
|---|---|---|---|---|
| 0 — Cold | Default / after death | Baseline | Baseline | — |
| 1 — Warm | 1 kill without dying | +10% | -10% | — |
| 2 — Hot | 2 kills without dying | +20% | -20% | +10% |
| 3 — On Fire | 3+ kills without dying | +30% | -30% | +20% |

**Score & presentation per tier:**

| Tier | Kill score value | Visual |
|---|---|---|
| 0 — Cold | +5 | No effect |
| 1 — Warm | +5 | Faint class-colour glow on model |
| 2 — Hot | +5 | Particle trail (class-themed) |
| 3 — On Fire | +8 | Full fire VFX, screen edge glow, audio rush layer |

::: info Code note — "kill score value" semantics
The `teamScoreKillValue` on each tier is the score awarded to the **killer's team for killing a victim at that tier** (`MomentumConfigSO.KillScoreValue(victimTier)`), not a bonus for the killer's own tier. So killing a tier-3 victim is worth +8; everything else is +5 (`GameConstants.ScoreKill = 5`, `ScoreKillOnFire = 8`). This matches the "always worth targeting" design intent in [Design Intent](#design-intent).
:::

**Numeric example (Carrot base speed 7.5 m/s):**
- Tier 0: 7.5 m/s
- Tier 1: 8.25 m/s
- Tier 2: 9.0 m/s
- Tier 3: 9.75 m/s

**Numeric example (Sprint Dash cooldown 6s):**
- Tier 0: 6.0s
- Tier 1: 5.4s
- Tier 2: 4.8s
- Tier 3: 4.2s

---

## Rules

<span class="cc-status built">Implemented</span> — kill/death/assist/decay are all in `MomentumController`. The charge model is a float accumulator `[0..3]` where the whole part is the tier; kills add whole tiers, death resets to 0 and returns a transfer fraction, decay subtracts one tier per interval.

| Rule | Detail |
|---|---|
| Kill charges tier | Each kill = +1 tier (capped at 3) |
| Death resets tier | Back to tier 0 immediately |
| Transfer on death | 50% of killer charge transferred: if you die at tier 2, killer gains 1 tier (50% of 2 = 1.0 → floor to 1) |
| Decay | -1 tier every 12 seconds without a kill or objective interaction (standing on a capture zone resets decay timer) |
| Assist credit | Assist = 25% charge; killing blow + assist = kill credit takes precedence |

### Transfer Math
| Dead player tier | Charge transferred | Killer tier gain |
|---|---|---|
| 1 | 0.5 | +1 (rounds up from 0.5) |
| 2 | 1.0 | +1 |
| 3 | 1.5 | +1 (killer can reach tier 3 faster than normal) |

If killer is already tier 3, transfer is stored as a "next tier fill" — killing a tier 3 player while already at tier 3 extends your decay timer by 8 seconds instead.

::: info Code note — transfer rounding & the tier-3 case
`MomentumController.HandleDeath()` returns `victimTier * transferFraction` (0.5 default, 0.25 for Potato — `GameConstants.MomentumTransferPotato`). The killer's `AbsorbTransfer()` then grants `Mathf.Max(1, FloorToInt(transferred + 0.5f))` tiers, so **any positive transfer is at least +1 tier** — exactly the "rounds up from 0.5" rule above. When the killer is already at tier 3, `AbsorbTransfer` extends decay by `GameConstants.OnFireOnFireDecayBonus = 8s` instead of overflowing, matching the design. The decay extension is also capped at one full interval so it can't snowball.
:::

---

## Decay Detail

<span class="cc-status built">Implemented</span> — decay runs in `MomentumController.Update()`; the interval is `GameConstants.MomentumDecayInterval = 12s` (also the default on `MomentumConfigSO.decayIntervalSeconds`). `OnDecayProgress` drives the HUD ring fill.

- Decay timer starts at 12 seconds on gaining a tier
- Timer resets on:
  - A kill (immediately stops decay, restarts timer)
  - Standing on a contested or owned capture zone
  - Jalapeño's passive adds 5s to timer on each kill
- Decay does not accelerate — losing tier 2 → 1 takes another full 12s
- Decay is paused during `CLASS_SELECT` and `COUNTDOWN` states

---

## Class-Specific Passives

<span class="cc-status built">Implemented</span> — all four passives are wired. The numeric effects are owned authoritatively by `MomentumController` (backstab tier bonus, Jalapeño decay extension, Potato transfer override) and `MomentumPassive_SharedHarvest` (Broccoli, subscribes to `GameEvents.OnKill`). The `MomentumPassive_*` components are real `AbilityBase` behaviours so the `AbilityFactory` can attach them per class.

| Class | Passive | Effect |
|---|---|---|
| Carrot | Backstab Momentum | Kills from behind (180° rear arc) count as +2 tiers |
| Jalapeño | Extended Streak | Each kill extends decay timer by +5s (stacks: 2 kills = +10s) |
| Broccoli | Shared Harvest | Nearby ally kills (within 10m) give Broccoli 10% of a tier's charge |
| Potato | Stubborn Root | On death, only 25% transfers to killer instead of 50% |

::: info Code note — where each passive lives
- **Carrot Backstab** uses `GameConstants.CarrotBackstabDot = -0.1` for the rear-arc dot test; `PlayerController`'s death handler computes the arc and forwards `isBackstab` into `RegisterKill`, which grants +2 tiers.
- **Jalapeño Extended Streak** adds `GameConstants.JalapenoDecayExtension = 5s` inside `RegisterKill` whenever the owner is `ClassId.Jalapeno`.
- **Broccoli Shared Harvest** adds `GameConstants.BroccoliHarvestShare = 0.1` charge when a same-team ally kills within `BroccoliHarvestRadius = 10m`.
- **Potato Stubborn Root** is a marker component; the 25% override lives in `HandleDeath` via `GameConstants.MomentumTransferPotato = 0.25`.
:::

---

## Network Sync

<span class="cc-status pending">Editor-pending</span> — `NetworkedMomentumController` exists and implements the design below, but **Netcode for GameObjects is not installed yet**, so the whole class compiles only behind `#if NETCODE_PRESENT` and is dormant. Until NGO is added (see [/dev/getting-started](dev/getting-started.md)), momentum runs locally via the standalone `MomentumController`. The design intent is preserved; nothing here contradicts `Assets/_Game/CONTRACTS.md`.

- Authoritative `MomentumTier` stored as `NetworkVariable<int>` (`netTier`) on `NetworkedMomentumController`, write-permission Server, read-permission Everyone
- All tier changes are **server-authoritative**: client calls `RegisterKillServerRpc(bool backstab)`, the server runs the real `MomentumController` logic and writes `netTier`
- Clients reconcile their local `MomentumController` to the authoritative tier from the `netTier.OnValueChanged` callback (`ForceTier`), so speed / cooldown / damage multipliers and VFX stay in sync without the client owning the value
- Decay runs server-side on the authoritative controller; clients receive tier updates via the NetworkVariable

::: info Code note — RPC name
The actual entry point is `RegisterKillServerRpc(bool backstab)` (NGO RPC naming convention), not a bare `RegisterKill()`. It carries the backstab flag so the server can apply Carrot's +2 authoritatively.
:::

---

## UI Specification

<span class="cc-status partial">Partial</span> — the HUD ring (`MomentumRingUI`), kill feed (`KillFeedUI`) and on-fire overlay (`OnFireOverlay`) are implemented and driven by `FeedbackController`/`PlayerHUD`, but the canvases, icons and particle/VFX assets they reference are <span class="cc-status pending">Editor-pending</span>.

### Momentum Ring (HUD)
- Position: center-bottom, surrounding crosshair
- Shape: thin circular ring (12px stroke)
- Fill: clockwise from top; full ring = tier 3
- Colour per tier: Cold = grey, Warm = class primary colour, Hot = bright warm, On Fire = animated orange/white pulse
- Tier-up animation: ring flashes white then settles to new colour over 0.3s
- Decay animation: ring slowly shrinks; at < 3 seconds remaining, pulses red

### Kill Feed Entry
- Format: `[Killer class icon] → [Victim class icon]`
- If tier transfer occurred: small "+1 tier" transfer badge on the killer icon
- Duration: 5 seconds; fades out

::: info Code note
`KillFeedUI` uses `entryLifetime = 5f` then a 0.5s fade, max 4 entries visible (oldest recycled). The transfer badge shows when the kill was a backstab **or** the victim held momentum (`VictimTier > 0`). Local-player kills get a gold border flash.
:::

### On Fire Full-Screen Notification (first time only per session)
- First time reaching tier 3: large "YOU'RE ON FIRE" overlay fades in and out over ~1.5s
- Subsequent tier 3 reaches: audio cue only (no full-screen overlay — would get annoying)

::: info Code note
Implemented as `OnFireOverlay` (fade-in 0.2s + hold 1.1s + fade-out 0.2s = 1.5s, peak alpha 0.85, non-blocking CanvasGroup). The "first time per session" gate lives in `FeedbackController.firstOnFireShown`, set on the first `Cold/Warm/Hot → OnFire` transition. Every tier-3 reach still plays the `momentum_tier3` SFX + screen shake; the overlay is suppressed after the first. Respects the reduce-motion accessibility setting.
:::

---

## Design Intent

The momentum transfer on death is the core tension driver — it answers "why should I hunt that player?"

- A tier 3 player is always worth targeting: +8 kill score instead of +5, plus you gain momentum
- Decay prevents camping at tier 3: you must keep engaging or you fall off
- Decay reset on objectives: players who hold objectives maintain tier naturally — objectives and kills are both valid momentum strategies, preventing one dominant playstyle
- Tier 0 after death feels like a fresh start, not a punishment — you respawn in 4s and can be back at tier 1 in your first kill

### The Hunt Loop
```
player reaches tier 3
  → becomes visually distinct (enemy can see their fire VFX)
  → becomes a high-value target
  → enemy team prioritizes killing them
  → killing tier 3 = huge momentum swing + 8 score
  → dead player respawns at tier 0 and wants to rebuild
  → creates constant back-and-forth rather than snowball dominance
```

This is the difference from games where kill streaks just end the match — here, a hot player is a magnet for counterplay, not an unstoppable force.
