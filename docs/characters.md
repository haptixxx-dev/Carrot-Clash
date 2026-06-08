# Characters

<span class="cc-status built">Implemented</span> <span class="cc-status pending">Editor-pending</span>

Four food-themed classes. Each has a fixed primary weapon, two active abilities, one passive, and a momentum-specific passive. See [Weapons](weapons.md) for weapon stats and [Momentum System](momentum-system.md) for passive detail.

::: info Code status (verified 2026-06)
All four classes and **all 16 ability behaviours** exist in code under namespace `CarrotClash` (`Assets/_Game/Characters/Abilities/Impl/`), driven by `CharacterDataSO` / `AbilityDataSO` ScriptableObjects. The exact numbers below are baked into `DataAssetGenerator.cs` and `GameConstants.cs` — design and code **agree**. Generate the data assets via the editor menu **Carrot Clash → Generate Default Data Assets** (assets are not committed).

Still editor-pending: the player prefab, class-select/HUD canvases, ability VFX, audio clips, and icons. So the classes are **code-complete but not yet playable** without authored scenes/prefabs/art. Engineers: see [/dev/getting-started](/dev/getting-started) and the frozen API in `Assets/_Game/CONTRACTS.md`.

Minor code refinements not in the original design text:
- **Sprint speed** is derived as `baseMoveSpeed × 1.4` (`sprintMultiplier`), which reproduces every sprint number below exactly.
- **Earthen Slam** knockback adds a 15° upward bounce (`GameConstants.KnockbackElevation`) on top of the 6m horizontal shove.
- Until the input asset gains dedicated bindings, abilities use fallback keys: **Q = Active 1, E = Active 2** (see [/dev/getting-started](/dev/getting-started)).
:::

---

## Class Overview

| Class | Role | HP | Base Speed | Primary | Difficulty |
|---|---|---|---|---|---|
| Carrot | Scout / Flanker | 90 | 7.5 m/s | SMG "The Nub" | ★★★☆☆ |
| Jalapeño | Brawler / DPS | 110 | 6.5 m/s | Shotgun "Pepper Blaster" | ★★☆☆☆ |
| Broccoli | Support / Utility | 100 | 6.0 m/s | Burst AR "The Stem" | ★★★★☆ |
| Potato | Tank / Anchor | 140 | 5.0 m/s | LMG "Root Cannon" | ★★☆☆☆ |

---

## Carrot — Scout / Flanker

<span class="cc-status built">Implemented</span> <span class="cc-status pending">Editor-pending</span> (VFX/SFX/prefab)

> *"Fast, quiet, and always behind you."*

**Fantasy:** The assassin. Sprint Dash in, land a kill, Radar Pulse to find another target, disappear before anyone can respond. Carrot rewards players who study enemy positions and punish overextension.

### Base Stats
| Stat | Value |
|---|---|
| HP | 90 |
| Base move speed | 7.5 m/s |
| Sprint speed | 10.5 m/s |
| Jump height | 1.4m |

### Abilities

| Ability | Type | Cooldown | Range | Effect |
|---|---|---|---|---|
| **Sprint Dash** | Active 1 | 6s | 8m | Instant directional dash; usable mid-air; preserves momentum |
| **Radar Pulse** | Active 2 | 18s | 15m radius | Reveals all enemies within 15m through walls for 2.5s |
| **Silent Steps** | Passive | — | — | Footstep SFX reduced to 0 when moving at < 50% sprint speed |
| **Backstab Momentum** | Momentum passive | — | — | Kills from a 180° rear arc = +2 tier instead of +1 |

::: details Tech notes
- **Sprint Dash** (`Ability_SprintDash`) — `CharacterController.Move()` override for ~0.08s; 8m distance.
- **Radar Pulse** (`Ability_RadarPulse`) — `Physics.OverlapSphere` + `SetOutlineVisible()` on hit `PlayerController`s; 15m radius, 2.5s.
- **Silent Steps** (`Passive_SilentSteps`) — AudioManager mutes the Footsteps source when `speed < sprintSpeed * 0.5f` (`SilentStepsSpeedFraction = 0.5`).
- **Backstab Momentum** (`MomentumPassive_Backstab`) — rear-arc test lives in the spine: `GameExtensions.IsInRearArc` with dot `< -0.1` (`CarrotBackstabDot`), forwarded to `MomentumController.RegisterKill(isBackstab)` for the +2-tier grant.
:::

### Counters & Matchups

| Vs. Class | Carrot's edge | Carrot's weakness |
|---|---|---|
| Jalapeño | Out-range and out-manoeuvre | Never fight head-on close range — Pepper Blaster one-shots |
| Broccoli | Broccoli's AR loses up close; Dash in and out | Spore Cloud blinds Carrot's positional advantage |
| Potato | Potato can't turn fast enough to track Carrot | Starch Armor makes Potato survive what should be a burst kill |

### Synergies

| Ally | Combo |
|---|---|
| Broccoli | Regen aura keeps Carrot topped up during flanks; Spore Cloud sets up Backstab angles |
| Jalapeño | Carrot forces enemies to scatter; Jalapeño presses close behind |
| Potato | Potato holds the front; Carrot circles around the distracted enemy team |

### Tips (for documentation / tutorial text)
- Dash horizontally to juke incoming fire, not just to close distance
- Radar Pulse before entering a building — know the room before you enter it
- Tier 3 Carrot + Backstab passive = +2 tier on your killer if you die — you're still dangerous when hunted

---

## Jalapeño — Brawler / Aggressive DPS

<span class="cc-status built">Implemented</span> <span class="cc-status pending">Editor-pending</span> (VFX/SFX/prefab)

> *"Gets hotter the longer the fight goes."*

**Fantasy:** The room clearer. Walk into a chokepoint, detonate Spice Burst to slow everyone, lay a Heat Trail behind you to cut off retreat, and shotgun anything that survives. High floor, high ceiling — beginners can get kills, experts dominate close range.

### Base Stats
| Stat | Value |
|---|---|
| HP | 110 |
| Base move speed | 6.5 m/s |
| Sprint speed | 9.1 m/s |
| Jump height | 1.2m |

### Abilities

| Ability | Type | Cooldown | Range / Area | Effect |
|---|---|---|---|---|
| **Spice Burst** | Active 1 | 12s | 4m radius, 20m throw | AOE spice grenade; slows enemies 35% for 3s, deals 15 damage |
| **Heat Trail** | Active 2 | 20s | 15m line, 1m wide, 3s duration | Drops fire behind Jalapeño while moving; 20 DPS to any enemy in trail |
| **Burn Streak** | Passive | — | 5m radius | Each kill ignites victim; nearby enemies take 10 DPS for 2s |
| **Extended Streak** | Momentum passive | — | — | Each kill adds +5s to momentum decay timer |

::: details Tech notes
- **Spice Burst** (`Ability_SpiceBurst`) — `Physics.OverlapSphere` at impact; `EffectSystem` slow (`SlowAmount = 0.35`, `SlowDuration = 3s`) plus 15 direct impact damage.
- **Heat Trail** (`Ability_HeatTrail`) — fire segments at the feet; each is a `DamageZone` trigger; 20 DPS, 1m wide (0.5m radius), 3s.
- **Burn Streak** (`Passive_BurnStreak`) — on kill, spawn a fire aura at the death position; 10 DPS over 2s, 5m radius.
- **Extended Streak** (`MomentumPassive_ExtendedStreak`) — adds `JalapenoDecayExtension = +5s` per kill in `MomentumController`.
:::

### Counters & Matchups

| Vs. Class | Jalapeño's edge | Jalapeño's weakness |
|---|---|---|
| Carrot | Heat Trail stops Carrot from retreating after a dash | Carrot at range is impossible — don't chase |
| Broccoli | Broccoli's low damage loses in any close-range exchange | Spore Cloud negates Jalapeño's ability to aim in tight spaces |
| Potato | Two Jalapeños can overwhelm a Potato | 1v1 vs. Potato: Starch Armor absorbs one full shotgun burst — use Slam to break it |

### Synergies

| Ally | Combo |
|---|---|
| Carrot | Carrot Radar Pulse + Jalapeño Spice Burst into a revealed cluster = easy multi-slow |
| Potato | Potato Earthen Slam scatters enemies into Jalapeño's close range |
| Broccoli | Broccoli Spore Cloud lets Jalapeño approach unseen |

---

## Broccoli — Support / Utility

<span class="cc-status built">Implemented</span> <span class="cc-status pending">Editor-pending</span> (cover/fog prefabs, VFX/SFX)

> *"Nobody picks Broccoli. Until they see what Broccoli can do."*

**Fantasy:** The force multiplier. Broccoli does nothing flashy alone, but with good positioning it keeps the team alive and in fights longer than the enemy expects. Mechanically demanding — requires spatial awareness of allies at all times.

### Base Stats
| Stat | Value |
|---|---|
| HP | 100 |
| Base move speed | 6.0 m/s |
| Sprint speed | 8.4 m/s |
| Jump height | 1.2m |

### Abilities

| Ability | Type | Cooldown | Range / Area | Effect |
|---|---|---|---|---|
| **Leaf Shield** | Active 1 | 15s | 3m range from placement | Deploys a 1×2m destructible cover object; 80 HP, blocks bullets |
| **Spore Cloud** | Active 2 | 18s | 6m radius, 4s duration | Throwable; creates a vision-blocking fog cloud at impact |
| **Regen Aura** | Passive | — | 8m radius | Allies (not Broccoli) within 8m regenerate 2 HP/s continuously |
| **Shared Harvest** | Momentum passive | — | 10m radius | Nearby ally kills give Broccoli 10% of kill charge |

::: details Tech notes
- **Leaf Shield** (`Ability_LeafShield`) — instantiate the `DestructibleCover` prefab at the crosshair surface hit; 80 HP via `HealthController`; placement range 3m.
- **Spore Cloud** (`Ability_SporeCloud`) — fog particle volume; enemies inside it have `OutlineVisible = false` (`VisionObscured`); 6m radius, 4s, 20m throw.
- **Regen Aura** (`Passive_RegenAura`) — `Physics.OverlapSphere` every `RegenAuraTick = 1s`; `health.Heal(2)` on each ally in the `RegenAuraRadius = 8m` (excludes Broccoli).
- **Shared Harvest** (`MomentumPassive_SharedHarvest`) — subscribes to the kill event, distance-checks `BroccoliHarvestRadius = 10m`, grants `BroccoliHarvestShare = 0.1` of the charge.
:::

### Counters & Matchups

| Vs. Class | Broccoli's edge | Broccoli's weakness |
|---|---|---|
| Carrot | Spore Cloud negates Carrot's positioning intel (Radar Pulse reveals through it, but Spore also blinds Carrot mid-dive) | Carrot's Dash into close range is hard to survive — Leaf Shield as panic cover |
| Jalapeño | Spore Cloud prevents Jalapeño from aiming Heat Trail precisely | Literally cannot survive a point-blank shotgun — stay out of 10m |
| Potato | Broccoli Regen Aura makes Potato nearly unkillable if stacked — powerful combo | No damage-dealing combo with Potato; dependent on team for kills |

### Synergies

| Ally | Combo |
|---|---|
| Potato | Regen Aura on Potato makes them a near-unkillable objective anchor; Spore Cloud over a Potato zone-hold forces enemies to rush blind |
| Carrot | Spore Cloud + Carrot Dash combo: cloud goes in, Carrot dashes through from an unexpected angle, enemies can't track |
| All | Leaf Shield on a capture zone makes holding it significantly easier — 80 HP of free cover that respawns on cooldown |

---

## Potato — Tank / Anchor

<span class="cc-status built">Implemented</span> <span class="cc-status pending">Editor-pending</span> (VFX/SFX/prefab)

> *"Hard to move. Harder to kill."*

**Fantasy:** The immovable object. Plant yourself on an objective, absorb everything the enemy throws, and scatter anyone who gets too close. Potato wins through presence — the enemy has to deal with you or lose the zone.

### Base Stats
| Stat | Value |
|---|---|
| HP | 140 |
| Base move speed | 5.0 m/s |
| Sprint speed | 7.0 m/s |
| Jump height | 1.0m |

### Abilities

| Ability | Type | Cooldown | Range / Area | Effect |
|---|---|---|---|---|
| **Starch Armor** | Active 1 | 20s | Self | +40 temporary HP for 4s; absorbs before base HP |
| **Earthen Slam** | Active 2 | 16s | 4m radius, ground AoE | Jump slam; knocks enemies back 6m and staggers them for 0.5s |
| **Thick Skin** | Passive | — | Self | -8% incoming damage while stationary for > 1 second |
| **Stubborn Root** | Momentum passive | — | Self | On death, only 25% of momentum transfers to killer |

::: details Tech notes
- **Starch Armor** (`Ability_StarchArmor`) — `health.AddTemporaryHP(40, 4f)` (`StarchArmorAmount`/`StarchArmorDuration`); distinct yellow HP-bar section.
- **Earthen Slam** (`Ability_EarthenSlam`) — `Physics.OverlapSphere` at the owner; `EffectSystem.ApplyKnockback` (`KnockbackDistance = 6m`, `StaggerDuration = 0.5s`, plus a 15° `KnockbackElevation` bounce); plays a short upward hop visual.
- **Thick Skin** (`Passive_ThickSkin`) — applies a 0.08 damage-reduction multiplier in `TakeDamage()` after >1s stationary.
- **Stubborn Root** (`MomentumPassive_StubbornRoot`) — overrides the default 0.5 transfer with `MomentumTransferPotato = 0.25` on death.
:::

### Counters & Matchups

| Vs. Class | Potato's edge | Potato's weakness |
|---|---|---|
| Carrot | Carrot can't burst down 140 HP + Starch Armor before being slammed | Carrot's dash away from Earthen Slam is instant — rarely lands on mobile targets |
| Jalapeño | Starch Armor absorbs the first shotgun blast — buys time to slam | Jalapeño Spice Burst slows Potato to a crawl and wins the timer game |
| Broccoli | Potato is safe from Broccoli's burst AR in sustained fire — outlasts | Broccoli Spore Cloud + regen means the enemy Broccoli heals teammates faster than Potato kills them |

### Synergies

| Ally | Combo |
|---|---|
| Broccoli | The definitive anchor pair — Broccoli regen keeps Potato alive indefinitely on a held zone |
| Jalapeño | Earthen Slam scatters enemies, Jalapeño cleans up the scattered targets in close range |
| Carrot | Potato soaks attention; Carrot flanks the distracted back line |

---

## Team Composition Guide

| Composition | Strengths | Weaknesses |
|---|---|---|
| Carrot + Jalapeño + Broccoli + Potato | Balanced; covers all ranges; strong objective hold | No pure damage spike; requires coordination |
| 2× Carrot + 2× Jalapeño | Aggressive; huge momentum potential; terrifying in Zone C rushes | No sustain; will collapse without kills; Broccoli aura missed |
| 2× Potato + 2× Broccoli | Nearly impossible to dislodge from an objective | Almost no kill pressure; will lose on score if enemy ignores them and caps other zones |
| 3× Jalapeño + 1× Broccoli | Room-clearing nightmare in Zone B | Completely useless at range; Zone A courtyard is a death zone |
