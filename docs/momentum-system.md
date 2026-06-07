# Momentum System

The defining mechanic of Carrot Clash. Kills charge a per-player momentum meter that escalates your power — but death resets it and transfers half your charge to your killer.

---

## Tiers

| Tier | Trigger | Move speed | Ability cooldown | Damage bonus | Kill score value | Visual |
|---|---|---|---|---|---|---|
| 0 — Cold | Default / after death | Baseline | Baseline | — | +5 | No effect |
| 1 — Warm | 1 kill without dying | +10% | -10% | — | +5 | Faint class-colour glow on model |
| 2 — Hot | 2 kills without dying | +20% | -20% | +10% | +5 | Particle trail (class-themed) |
| 3 — On Fire | 3+ kills without dying | +30% | -30% | +20% | +8 | Full fire VFX, screen edge glow, audio rush layer |

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

---

## Decay Detail

- Decay timer starts at 12 seconds on gaining a tier
- Timer resets on:
  - A kill (immediately stops decay, restarts timer)
  - Standing on a contested or owned capture zone
  - Jalapeño's passive adds 5s to timer on each kill
- Decay does not accelerate — losing tier 2 → 1 takes another full 12s
- Decay is paused during `CLASS_SELECT` and `COUNTDOWN` states

---

## Class-Specific Passives

| Class | Passive | Effect |
|---|---|---|
| Carrot | Backstab Momentum | Kills from behind (180° arc behind target) count as +2 tiers |
| Jalapeño | Extended Streak | Each kill extends decay timer by +5s (stacks: 2 kills = +10s) |
| Broccoli | Shared Harvest | Nearby ally kills (within 10m) give Broccoli 10% of their tier charge |
| Potato | Stubborn Root | On death, only 25% transfers to killer instead of 50% |

---

## Network Sync

- `MomentumTier` stored as `NetworkVariable<int>` on `NetworkedMomentumController`
- All tier changes are **server-authoritative**: client sends `ServerRpc RegisterKill()`, server validates and updates tier
- Visual effects (glow, particles, audio) driven **client-side** from `OnValueChanged` callback on the NetworkVariable — keeps visuals instant without trust issues
- Decay runs server-side only; clients get tier updates via NetworkVariable

---

## UI Specification

### Momentum Ring (HUD)
- Position: center-bottom, surrounding crosshair
- Shape: thin circular ring (12px stroke)
- Fill: clockwise from top; full ring = tier 3
- Colour per tier: Cold = grey, Warm = class primary colour, Hot = bright warm, On Fire = animated orange/white pulse
- Tier-up animation: ring flashes white then settles to new colour over 0.3s
- Decay animation: ring slowly shrinks; at < 3 seconds remaining, pulses red

### Kill Feed Entry
- Format: `[Killer class icon] → [Victim class icon]`
- If tier transfer occurred: small arrow with tier number shown between icons
- Duration: 5 seconds; fades out

### On Fire Full-Screen Notification (first time only per session)
- First time reaching tier 3: large "🔥 YOU'RE ON FIRE" overlay fades in and out over 1.5s
- Subsequent tier 3 reaches: audio cue only (no full-screen overlay — would get annoying)

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
