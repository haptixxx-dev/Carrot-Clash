# Audio Design

Food-themed sound world. The audio reinforces the mechanical identity: everything sounds like a kitchen, a market, or a vegetable fight.

---

## Music Architecture

Three layers that blend dynamically via `AudioMixer` exposed parameters:

| Layer | When active | Style | Notes |
|---|---|---|---|
| `Base` | Always (during match) | Upbeat market/kitchen rhythm — percussion-forward, no melody | Never stops; provides steady energy |
| `Intensity` | Last 90s of match OR Zone C contested | Adds a driving melody + increased tempo | Cross-fade in over 5s |
| `MomentumTier3` | Local player at tier 3 only | Personal "power" layer — brass sting + driving bass | Heard only by the tier-3 player (2D audio, not spatial) |

**Menu music:** Calm version of the Base layer; no drums. Fades out when Play is pressed.

**Post-match:** Brief 3-note fanfare for win (upbeat), 1-note subdued cue for loss. Then silence — the XP animation plays in clean audio space so numbers feel more impactful.

**Implementation:** `AudioManager.SetMusicParameter("intensity", 0f/1f)` and `SetMusicParameter("tier3", 0f/1f)` — driven by `GameModeManager` and `MomentumController.OnTierChanged` respectively.

---

## SFX Categories

### Footsteps

| Surface | Sound character | Implementation |
|---|---|---|
| Stone (Courtyard) | Solid, resonant click | `footsteps_stone_01..04` — random pick per step |
| Wood (Market floor) | Hollow thud, slight creak | `footsteps_wood_01..04` |
| Metal grating (Catwalk) | Ringing clank | `footsteps_metal_01..03` |
| Underground Cellar | Dull, dampened | `footsteps_dirt_01..03` |

- Surface detection: `Physics.Raycast` downward from player feet; hit collider's `PhysicsMaterial` maps to sound category
- Step rate: driven by animation events (preferred) or distance-threshold fallback
- Carrot Silent Steps passive: `AudioSource.volume = 0` when `speed < sprintSpeed * 0.5f`
- 3D spatial: `AudioRolloffMode.Logarithmic`, max hearing distance 25m

---

### Weapons

| Event | Sound | Notes |
|---|---|---|
| The Nub (SMG) fire | Fast metallic click/crack | Rapid-fire; distinct from AR to avoid confusion |
| Pepper Blaster (Shotgun) fire | Deep, wet boom | Meatiness signals high damage potential |
| The Stem (AR) fire | Crisp 3-round burst | Each burst has a slightly different pitch variant |
| Root Cannon (LMG) fire | Heavy, sustained chug | Lower frequency than SMG; "suppressing fire" feel |
| The Pip (Pistol) fire | Clean, sharp pop | Distinct from primaries; lighter |
| Any weapon reload | Mechanical clunk + click | Class-appropriate: SMG = quick; LMG = heavy drag |
| Empty clip | Dry click × 2 | Plays instead of fire; immediate "reload now" cue |
| Bullet impact (body) | Soft thud | Spatial — heard by nearby players |
| Bullet impact (headshot) | Crisper crack | Different tone; also triggers on-hit sound on HUD |
| Kill confirmation | Satisfying short chime | 2D (not spatial) — only heard by killer |

---

### Abilities

| Ability | Sound | Notes |
|---|---|---|
| Sprint Dash | Whoosh + leaf rustle | Fast; 0.15s total |
| Radar Pulse | Resonant electronic ping → fade out | Slight reverb on the detect ping |
| Spice Burst (launch) | Sizzle + pop | Launch sound when thrown |
| Spice Burst (impact) | Hissing burst + sizzle | AOE impact |
| Spice Burst (on target) | Gurgling slow sound | Played on slowed targets; 3D |
| Heat Trail (deploy) | Searing hiss, continuous | Loops while trail is active; hot exhaust feel |
| Heat Trail (player in trail) | Pain sting + sizzle | Short, repeated every 0.5s of contact |
| Burn Streak (passive trigger) | Short crackle | On kill; brief — not distracting |
| Leaf Shield (deploy) | Rustling whomp | Like a barrier of leaves snapping into place |
| Leaf Shield (hit) | Rustle + crunch | Damage to shield; distinct from armor hit |
| Leaf Shield (destroyed) | Wet crunch + burst | Satisfying destruction sound |
| Spore Cloud (throw) | Soft thud |  |
| Spore Cloud (active) | Sustained hiss + spore rattle | Loops; heard inside the cloud |
| Regen Aura (passive) | Very subtle leaves rustling | Nearly inaudible; present only to reinforce UI heal |
| Starch Armor (activate) | Crunch + hardening sound | Starch snap — popcorn-esque |
| Starch Armor (hit while active) | Dry crunch (different from base hit) | Signals armor absorbing — player can hear the difference |
| Starch Armor (expire) | Soft crumble | Armor dissolving |
| Earthen Slam (windup) | Brief grunt + bass whomp | Telegraphs the slam to nearby enemies |
| Earthen Slam (impact) | Heavy bass thud + crumble | Rumble felt through vibration if controller |

---

### Momentum

| Event | Sound | Notes |
|---|---|---|
| Tier 0 → 1 (Warm) | Short ascending chime | Subtle, not distracting |
| Tier 1 → 2 (Hot) | Brighter ascending chime | Slightly louder; fire crackle undertone |
| Tier 2 → 3 (On Fire) | Power surge — bass hit + fire rush | Unmistakably tier 3; satisfying |
| Tier decay warning (< 3s) | Soft pulsing beep | 2 pulses per second; low urgency |
| Tier lost to decay | Descending chime | "Power-down" feel |
| Tier reset on death | Silent — death sound covers it | No separate tone; integrated into death audio |
| Kill transfer received | Short rising sting | When you kill a tier-X player and absorb their charge |

---

### Objectives

| Event | Sound | Notes |
|---|---|---|
| Zone capture started (your team) | Rising tone + marker ping | 2D; UI-adjacent |
| Zone capture started (enemy) | Lower, ominous tone | Warning cue |
| Zone capture complete (your team) | Clean resonant gong | Victory feel; 3D spatial from zone location |
| Zone capture complete (enemy) | Discordant shorter gong | Loses clearly |
| Zone C unlock | Kitchen bell × 1 + crowd surge | Global; loudest in-match audio event; unmistakable |
| Zone C being contested | Intensity music layer fades in | Audio signals you're near the deciding moment |
| Score cap reached (win) | Match-end fanfare cue | All gameplay sounds cut; win sting plays |

---

### UI

| Event | Sound | Notes |
|---|---|---|
| Button hover | Very soft click | Subtle; doesn't fatigue |
| Button confirm | Slightly heavier click | |
| Match found | Pinging notification | Distinct; wakes up if tabbed |
| Countdown (5 → 1) | Ticking + final burst | Builds anticipation |
| Respawn countdown | Soft descending ticks | Not anxiety-inducing; just informational |
| XP gain animation | Coin-esque rising tone sequence | Animates with XP counter |
| Battle pass tier up | Distinct fanfare sting | Memorable; reward moment |
| Daily challenge complete | Short positive chime | In-HUD; brief |

---

### Ambient (Per Zone)

| Zone | Ambient layer | Notes |
|---|---|---|
| Zone A Courtyard | Market crowd chatter (quiet), wind, distant cart noise | Warm, busy feel |
| Zone B Indoor Market | Refrigerator hum, dripping water, ambient voice echo | Cooler, enclosed |
| Zone C Central Stage | Distant crowd roar (builds as match timer falls) | Tension amplifier |
| Alley East | Narrower echo; wind rushing through passage | |
| Underground Cellar | Deep rumble, near-silence otherwise | Stealth atmosphere |

**Zone transitions:** Ambient layers cross-fade over 1.5s as player moves between zones (trigger volumes at zone thresholds). No hard audio cuts.

---

## Technical Implementation

- `AudioManager` singleton using pooled `AudioSource` components (pool size: 32)
- Category buses via Unity `AudioMixer`: Master → Music, SFX (→ Footsteps, Weapons, Abilities, UI, Ambient)
- All in-world sounds: `AudioSource.spatialBlend = 1.0` (full 3D)
- All UI / confirmation sounds: `spatialBlend = 0.0` (2D, no rolloff)
- Music: single `AudioSource` per layer, looping; crossfade via `AudioMixer` snapshot transitions
- Footstep surface detection: `PhysicsMaterial` name → enum → sound bank lookup in `FootstepBank` ScriptableObject
- Audio occlusion (optional Phase 5): `Physics.Raycast` between AudioSource and listener; attenuate if wall between them

---

## Audio Assets Needed (to produce or source)

| Category | Estimated files | Format | Notes |
|---|---|---|---|
| Music (stems) | 3 layers × 1-2 loops | WAV / OGG, 44.1kHz | Commission or license |
| Footsteps | 4 surfaces × 4 variants | Short WAV | Free SFX banks exist (Freesound) |
| Weapon fire | 5 weapons × 2-3 variants | WAV | Kitchen-themed reimagining |
| Reload | 5 weapons × 1 | WAV | |
| Ability SFX | ~18 unique cues | WAV | Food-themed design priority |
| Momentum tiers | 4 tier-up, 1 tier-down, 1 warning | WAV | Designed as a set — ascending family |
| Objective cues | ~8 unique | WAV | |
| UI SFX | ~12 unique | WAV | Short, punchy |
| Ambient layers | 5 zones | OGG loop | Low-memory ambient loops |
| **Total** | **~90 files** | | |
