# Ability Interactions

Rules for how abilities behave when they collide. Defined explicitly to prevent ambiguity during implementation.

<span class="cc-status built">Implemented</span> The 16 ability behaviours, the central `EffectSystem` (slow / fire / knockback / temp-HP), `HealthController` damage ordering, and the `DamageZone` / `VisionObscured` helpers all exist and are verified in code. What's **Editor-pending** is the content these rules act on — ability prefabs/VFX, the player prefab, scenes — and a couple of interactions are written into the design here but not yet wired into a consuming system (flagged inline below).

::: info Code ground truth
The tuning numbers in this doc are now enforced by `Assets/_Game/Shared/GameConstants.cs`. Where the doc and code disagree, the deltas are called out in `::: info` boxes. The interaction logic lives in `Assets/_Game/Shared/EffectSystem.cs`, `Assets/_Game/Characters/HealthController.cs`, and the per-ability classes under `Assets/_Game/Characters/Abilities/Impl/`. The frozen API is in `Assets/_Game/CONTRACTS.md`; engineers should start at [/dev/getting-started](/dev/getting-started).
:::

---

## Interaction Matrix

<span class="cc-status built">Implemented</span> <span class="cc-status partial">Partial</span> — most rows are enforced by code; the two reveal-vs-cloud rows are not yet wired (see the delta box under the matrix).

| Ability A | Ability B | Result | Notes |
|---|---|---|---|
| Radar Pulse | Spore Cloud | **Cloud wins (design)** — Pulse plays sound but should reveal nothing inside cloud radius | Intentional asymmetry: intel beats stealth, but stealth beats recon. *Not yet enforced — see delta box.* |
| Radar Pulse | Leaf Shield | Shield does NOT block Pulse | Pulse is energy-based, not line-of-sight |
| Spice Burst (slow) | Earthen Slam (knockback) | Both apply independently | Slowed enemies still fly back — funny + fair |
| Spice Burst (slow) | Sprint Dash | Dash is instant; slow reapplies after dash completes | Dash does not consume or break the slow |
| Heat Trail (fire) | Burn Streak (fire aura) | Both apply independently; separate damage ticks | They are separate `DamageZone` instances; no double-trigger |
| Heat Trail | Sprint Dash | < 0.1s traversal = 50% damage (grazing rule) | Full speed dash crosses ~1m trail in ~0.05s |
| Spice Burst | Leaf Shield | Spice Burst deals AOE damage to shield (15 dmg) | Shield has 80 HP (`Ability_LeafShield.LeafShieldHP`) so won't one-shot unless near-depleted |
| Earthen Slam | Leaf Shield | **Design:** slam shoves the shield object (doesn't destroy it). **Code today:** slam only affects enemy players; the shield is a static cover, so it's unaffected | See edge-case note — shield isn't a rigidbody yet |
| Starch Armor | Regen Aura | Regen only restores base HP; Temp HP depletes first | `HealthController.Heal()` checks `tempHP > 0`; skips if so |
| Starch Armor | Spice Burst | Burst damage hits Starch Armor first | Standard damage ordering: TempHP → BaseHP |
| Starch Armor | Earthen Slam | Starch Armor does NOT prevent knockback | Knockback is a movement impulse + stagger; the HP buffer is irrelevant to it |
| Regen Aura (Broccoli A) | Regen Aura (Broccoli B) | **Design intent:** 2 HP/s, not 4. **Code today:** each Broccoli's `Passive_RegenAura` ticks independently, so two auras heal 4 HP/s | No cross-source dedup exists yet — see delta box |
| Spore Cloud | Heat Trail | Fire visible through cloud | Particle emitters are unaffected by vision fog; fire "leaks" through cloud intentionally (a hint to enemies) |
| Spore Cloud | Burn Streak passive | Fire aura at kill position is visible through cloud | Same as above |
| Radar Pulse | Starch Armor | Reveals player regardless of Starch Armor state | Armor affects HP; Pulse is detection only |
| Silent Steps (Carrot) | Spore Cloud | Cloud does not silence; Carrot's passive is independent | Separate systems; no interaction |
| Shared Harvest (Broccoli passive) | Kill by ally in cloud | Harvest counts regardless of cloud; it's a kill event listener (`MomentumPassive_SharedHarvest` subscribes to `GameEvents.OnKill`, range 10m) | No line-of-sight check on harvest |

::: info Code-vs-design deltas in the matrix
Two rows describe behaviour the code does **not** yet enforce; the supporting plumbing exists but no consumer hooks it up:

- **Cloud beats Radar Pulse.** `Ability_SporeCloud` does tag covered players with a `VisionObscured` marker (`IsObscured == true`), but `Ability_RadarPulse` does **not** read that marker — it reveals every enemy in its scan radius. To make "Cloud wins" real, the reveal pass needs an `if (other.GetComponent<VisionObscured>()?.IsObscured == true) continue;` guard. Same gap applies to any future minimap/blip reveal.
- **Stacked Regen Auras.** There is no `hasRegenSource` flag on `HealthController`; each `Passive_RegenAura` heals independently every `RegenAuraTick` (1s) for `RegenAuraHeal` (2). Two Broccolis therefore heal a shared ally **4 HP/s**, not 2. If 2 HP/s is the intent, the dedup belongs in `HealthController.Heal` (e.g. a per-second heal cap), not in the passive.
:::

---

## Status Effect Rules

<span class="cc-status built">Implemented</span> All four status effects are centralised in `EffectSystem` and applied to existing subsystems (`PlayerMovement` for slow/knockback/stagger, `HealthController` + `FireDamageTicker` for fire/temp-HP). Numbers below are pulled from `GameConstants.cs`.

### Slow (from Spice Burst)
- Reduces move speed by **35%** (`GameConstants.SlowAmount = 0.35`) for **3.0s** (`SlowDuration`)
- Multiple Spice Bursts on the same target: duration resets to 3.0s (no double slow — `PlayerMovement.ApplySlow` sets `slowMultiplier = 1 - amount` outright, so the floor is 65% of base, not 65% of 65%)
- Slow applies after the momentum multiplier, on top of base speed. The code's full chain is `speed = baseSpeed * sprintMult * momentumMult * slowMultiplier * firingMult` (then ×0.6 if crouched). Design intent (slow on base, before momentum) is preserved; the multipliers compose either way
- Slow does NOT cancel the sprint toggle — the resulting sprint speed is still multiplied by `slowMultiplier`, so a sprinting slowed player is 35% slower than a normal sprint
- Slow DOES affect Earthen Slam approach speed (important: Potato using Slam after being slowed is significantly weaker)

### Knockback (from Earthen Slam)
- Force: **6m** displacement over **0.25s** (`KnockbackDistance` / `KnockbackDuration`)
- Implemented as a decaying `externalImpulse` on `PlayerMovement` (velocity = distance / duration, lerped to zero over `KnockbackDuration`) fed through `CharacterController.Move`, **not** a Rigidbody impulse — so it respects collisions and never clips through walls
- Stagger: **0.5s** (`StaggerDuration`) during which the player cannot fire or use abilities

  ::: info Stagger is harsher than the original design
  The doc said "movement still allowed — mashing is the counterplay." The shipped code does the opposite: while staggered, `PlayerMovement` forces `speed = 0` (directed movement and jump are disabled) and only the knockback impulse carries the player. The counterplay is now "wait out the 0.5s," not "mash to escape." Re-decide whether to soften this back to the design or update the rationale.
  :::
- Does not affect Broccoli Leaf Shield's deployment timer (shield stays deployed, just moves)
- Knockback direction is away from Potato's position at slam impact; slightly upward (15° elevation, `KnockbackElevation`) to create visible "bounce"

### Fire (from Heat Trail / Burn Streak passive)
- Routed through `EffectSystem.ApplyFire`, which adds a managed source to a per-target `FireDamageTicker`. The ticker deals `round(dps * FireTickInterval)` (min 1) every **0.1s** (`FireTickInterval`) until each source's duration expires, then self-destructs
- `DamageZone` (Heat Trail's puddle segments) gates re-application so each zone only feeds one fire source per victim per tick window — `OnTriggerStay` fires ~50Hz but won't 5× the DPS
- Tuning: Heat Trail ≈ **20 DPS** (`AbilityDataSO.magnitude`), Burn Streak passive = **10 DPS for 2s within 5m** of the kill (`Passive_BurnStreak`). Overlapping ≈ **30 DPS**, as designed
- Multiple fire sources stack as separate managed ticks (no merging) — exactly the "separate `DamageZone` instances, no double-trigger" rule
- Fire damage type (`DamageType.Fire`): dealt to TempHP first (Starch Armor protects against fire)
- Fire does NOT slow
- Fire does NOT reveal (no indicator that someone is on fire, but their scream SFX is audible)

### Temporary HP (from Starch Armor)
- Grants **40** temp HP (`StarchArmorAmount`) for **4s** (`StarchArmorDuration`)

  ::: info Number changed from the original docs
  Earlier prose referenced an "80 HP" Starch buffer in a couple of edge cases. The shipped value is **40** temp HP. The 80 HP figure that survives in the doc is the **Leaf Shield** cover object (`LeafShieldHP = 80`), which is a separate thing. Don't conflate the two.
  :::
- Displayed as a yellow segment on the HP bar, above base HP
- Deducted before base HP for all incoming damage, including fire (`HealthController.TakeDamage` drains `tempHP` first)
- Does NOT regen (`HealthController.Heal` early-returns while `tempHP > 0`)
- Expires after 4s regardless of remaining amount (`tempHpExpireTime` checked each `Update`)
- Stacks additively if re-applied (`AddTemporaryHP` adds to the pool and refreshes the timer)
- Visual: HP bar yellow segment shrinks as temp HP depletes; on expiry, yellow flashes out

---

## Edge Cases

<span class="cc-status partial">Partial</span> The resolutions below match the shipped code except where noted — the Spore-Cloud-vs-reveal "blindness" relies on the still-unwired `VisionObscured` consumer, and Earthen Slam deals no damage in code (see notes).

| Scenario | Resolution |
|---|---|
| Carrot Dashes into a Spore Cloud to escape Jalapeño | Cloud marks the player obscured on the next rescan (≤0.25s, `SporeCloudVolume.RescanInterval`). *Reveal suppression itself is pending a `VisionObscured` consumer.* |
| Potato Slams while Broccoli's Leaf Shield is directly underfoot | **Design:** slam blast destroys the 80 HP shield. **Code today:** `Ability_EarthenSlam` only applies knockback + stagger to *players* — it deals no damage and does not touch the shield object, so the shield survives (and isn't a physics rigidbody, so it doesn't move either). Wire shield damage/knockback if this case matters. |
| Jalapeño drops Heat Trail directly on a capture point | Trail does not affect capture progress — damage (`DamageZone`) and objective (`CaptureZone`) are separate systems |
| Two Carrot Radar Pulses fired simultaneously at the same target | Reveal duration just resets to **2.5s** — each Pulse calls `RevealController.Begin`, which sets `expireTime = now + duration`; a second pulse refreshes it rather than extending it. *(The original doc's "12s longer reveal" was a typo.)* |
| Broccoli's Spore Cloud is thrown into Zone C during sudden death | Cloud fully valid — no restriction; zone capture still continues inside cloud (based on player presence, not visibility) |
| Jalapeño fires Spice Burst at a target already at the edge of Earthen Slam range | Both effects apply: slow + knockback. The knockback impulse is computed once at slam time; the slow then reduces the target's *own* directed speed, so a slowed target is staggered and drifts less under its own power. |
| Carrot activates Sprint Dash while Spore Cloud is around them | Dash moves them out of cloud instantly; on the cloud's next rescan they're released from the obscured set |
| Player dies while inside Broccoli's Regen Aura | Death interrupts regen; `Passive_RegenAura` skips dead allies, so a dead/respawning player is never healed |

---

## Anti-Abuse Rules

<span class="cc-status pending">Editor-pending</span> These are server-authoritative rules. The intent is realised in the network mirrors under `Assets/_Game/Network/` (e.g. `NetworkedAbilityController` already validates cooldown/dead/staggered server-side before running an activation), but the **whole layer is dormant behind the `NETCODE_PRESENT` define** — Netcode for GameObjects (and Relay/Lobby) is not installed yet. In offline/bot play today, `HealthController`, `EffectSystem`, and the ability classes are the local source of truth. See [/dev/architecture](/dev/architecture) and `CONTRACTS.md`.

| Rule | Reason |
|---|---|
| Ability activation requires the real `AbilityController` cooldown to be ready (server re-checks) | Client cannot fire ability ahead of cooldown |
| Knockback magnitude is server-computed | Client cannot exaggerate knockback force |
| Damage zones tick on server; visual effects on client | Prevents client from firing extra DamageZone ticks |
| Starch Armor temp HP authoritative on server | Client cannot fake extra HP to survive lethal hits |
| Radar Pulse reveal state synced server → clients | Reveal cannot be forged client-side |

::: warning Reveal sync not built yet
There is no per-player `NetworkVariable<bool>` reveal mirror in code. Today reveal is applied locally by `Ability_RadarPulse.RevealController` (an emissive-tint outline). When NGO lands, the reveal flag must become server-authoritative — and that same server pass is where the Spore-Cloud `VisionObscured` suppression should be applied (see the matrix delta box).
:::
