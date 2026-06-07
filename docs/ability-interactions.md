# Ability Interactions

Rules for how abilities behave when they collide. Defined explicitly to prevent ambiguity during implementation.

---

## Interaction Matrix

| Ability A | Ability B | Result | Notes |
|---|---|---|---|
| Radar Pulse | Spore Cloud | **Cloud wins** — Pulse plays sound but reveals nothing inside cloud radius | Intentional asymmetry: intel beats stealth, but stealth beats recon |
| Radar Pulse | Leaf Shield | Shield does NOT block Pulse | Pulse is energy-based, not line-of-sight |
| Spice Burst (slow) | Earthen Slam (knockback) | Both apply independently | Slowed enemies still fly back — funny + fair |
| Spice Burst (slow) | Sprint Dash | Dash is instant; slow reapplies after dash completes | Dash does not consume or break the slow |
| Heat Trail (fire) | Burn Streak (fire aura) | Both apply independently; separate damage ticks | They are separate `DamageZone` instances; no double-trigger |
| Heat Trail | Sprint Dash | < 0.1s traversal = 50% damage (grazing rule) | Full speed dash crosses ~1m trail in ~0.05s |
| Spice Burst | Leaf Shield | Spice Burst deals AOE damage to shield (15 dmg) | Shield has 80 HP so won't one-shot unless near-depleted |
| Earthen Slam | Leaf Shield | Slam knocks back shield object (not destroys it) | Shield treated as dynamic rigidbody during knockback; settles ~3m away |
| Starch Armor | Regen Aura | Regen only restores base HP; Temp HP depletes first | `HealthController.Heal()` checks `tempHP > 0`; skips if so |
| Starch Armor | Spice Burst | Burst damage hits Starch Armor first | Standard damage ordering: TempHP → BaseHP |
| Starch Armor | Earthen Slam | Starch Armor does NOT prevent knockback | Knockback is physics-based; HP buffer is irrelevant |
| Regen Aura (Broccoli A) | Regen Aura (Broccoli B) | Same player in range of 2 Broccolies: 2 HP/s, not 4 | `HealthController` tracks `hasRegenSource` bool; only one tick per second regardless of source count |
| Spore Cloud | Heat Trail | Fire visible through cloud | Particle emitters are unaffected by vision fog; fire "leaks" through cloud intentionally (a hint to enemies) |
| Spore Cloud | Burn Streak passive | Fire aura at kill position is visible through cloud | Same as above |
| Radar Pulse | Starch Armor | Reveals player regardless of Starch Armor state | Armor affects HP; Pulse is detection only |
| Silent Steps (Carrot) | Spore Cloud | Cloud does not silence; Carrot's passive is independent | Separate systems; no interaction |
| Shared Harvest (Broccoli passive) | Kill by ally in cloud | Harvest counts regardless of cloud; it's a kill event listener | No line-of-sight check on harvest |

---

## Status Effect Rules

### Slow (from Spice Burst)
- Reduces move speed by **35%** for **3.0s**
- Multiple Spice Bursts on the same target: duration resets to 3.0s (no double slow — movement floor is 65% of base, not 65% of 65%)
- Slow applies to base speed, before momentum multiplier: `effectiveSpeed = (baseSpeed * momentumMultiplier) * (1 - slowAmount)`
- Slow does NOT affect sprint toggle — the sprint speed is also reduced by 35%
- Slow DOES affect Earthen Slam approach speed (important: Potato using Slam after being slowed is significantly weaker)

### Knockback (from Earthen Slam)
- Force: **6m** displacement over **0.25s**
- Applies `AddForce(direction * force, ForceMode.Impulse)` to `CharacterController` via temporary Rigidbody mode
- Stagger: 0.5s during which player cannot fire or use abilities (movement still allowed — mashing is the counterplay)
- Does not affect Broccoli Leaf Shield's deployment timer (shield stays deployed, just moves)
- Knockback direction is away from Potato's position at slam impact; slightly upward (15° elevation) to create visible "bounce"

### Fire (from Heat Trail / Burn Streak passive)
- `DamageZone` component: `OnTriggerStay` deals damage every 0.1s; stored as `dps * deltaTime` accumulation
- Multiple fire sources: stacks as separate ticks (Heat Trail + Burn Streak fire = ~30 DPS total when overlapping)
- Fire damage type: dealt to TempHP first (Starch Armor protects against fire)
- Fire does NOT slow
- Fire does NOT reveal (no indicator that someone is on fire, but their scream SFX is audible)

### Temporary HP (from Starch Armor)
- Displayed as a yellow segment on the HP bar, above base HP
- Deducted before base HP in all incoming damage
- Does NOT regen (Broccoli's regen ignores it)
- Expires after 4s regardless of remaining amount
- Visual: HP bar yellow segment shrinks as temp HP depletes; on expiry, yellow flashes out

---

## Edge Cases

| Scenario | Resolution |
|---|---|
| Carrot Dashes into a Spore Cloud to escape Jalapeño | Cloud's vision block takes effect immediately on entry — no "preview" period |
| Potato Slams while Broccoli's Leaf Shield is directly underfoot | Shield is destroyed (slam AOE hits shield object for full blast, 80 damage = 0 HP) |
| Jalapeño drops Heat Trail directly on a capture point | Trail does not affect capture progress — damage and objective are separate systems |
| Two Carrot Radar Pulses fired simultaneously at the same target | Reveal duration resets (12s longer reveal — each Pulse sets `revealTimer = 2.5s`, second one just resets it) |
| Broccoli's Spore Cloud is thrown into Zone C during sudden death | Cloud fully valid — no restriction; zone capture still continues inside cloud (based on player presence, not visibility) |
| Jalapeño fires Spice Burst at a target already at the edge of Earthen Slam range | Both effects apply: slow + knockback. Slowed enemy travels 4m instead of 6m (speed reduction carries through physics impulse proportionally) |
| Carrot activates Sprint Dash while Spore Cloud is around them | Dash moves them out of cloud instantly — they exit blindness on exit |
| Player dies while inside Broccoli's Regen Aura | Death interrupts regen; regen aura has no effect on dead/respawning player |

---

## Anti-Abuse Rules

These are enforced server-side:

| Rule | Reason |
|---|---|
| Ability activation requires `cooldownTimer <= 0` | Client cannot fire ability ahead of cooldown |
| Knockback magnitude is server-computed | Client cannot exaggerate knockback force |
| Damage zones tick on server; visual effects on client | Prevents client from firing extra DamageZone ticks |
| Starch Armor temp HP authoritative on server | Client cannot fake extra HP to survive lethal hits |
| Radar Pulse reveal state synced via `NetworkVariable<bool>` per player | Reveal cannot be forged client-side |
