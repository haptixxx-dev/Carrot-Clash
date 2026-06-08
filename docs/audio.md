# Audio design

Food-themed sound world. The audio reinforces the mechanical identity: everything sounds like a kitchen, a market, or a vegetable fight.

<span class="cc-status partial">Partial</span> The full audio runtime is implemented in code (`AudioManager`, `MusicDirector`, `FootstepController`, `FootstepBank`, `AudioLibrary`, `AmbientZone` under `Assets/_Game/Audio/`) and gameplay systems already emit SFX/music cues by key. What is missing is content: every clip, the `AudioLibrary` / `FootstepBank` data assets, and the `AudioMixer` graph are editor-pending. With no clips wired, `AudioLibrary.Resolve` returns null and every call no-ops gracefully, so the game runs silent until assets land.

::: info Naming note (code vs. this doc)
This doc historically used illustrative clip filenames like `footsteps_stone_01..04`. In the shipped code those are not per-file keys. The runtime resolves a single library key (e.g. `footstep_stone`) and `AudioLibrary` picks a random variant from the clips assigned to that key. The "01..04" variants still exist; they just live under one key. Keys actually emitted by code are listed per section below.
:::

---

## Music architecture

<span class="cc-status built">Implemented</span> `MusicDirector.cs` drives a three-layer mix through `AudioManager`; layer levels are pushed via `SetMusicParameter`. Clips/mixer are editor-pending.

Three layers that blend dynamically via `AudioMixer` exposed parameters:

| Layer | When active | Style | Notes |
|---|---|---|---|
| `Base` | Always (during match) | Upbeat market/kitchen rhythm, percussion-forward, no melody | Never stops; provides steady energy |
| `Intensity` | Last 90s of match OR Zone C contested | Adds a driving melody plus increased tempo | Cross-fade in over 5s |
| `MomentumTier3` | Local player at tier 3 only | Personal "power" layer: brass sting plus driving bass | Heard only by the tier-3 player (2D audio, not spatial) |

**Menu music:** Calm version of the Base layer; no drums. Fades out when Play is pressed.

**Post-match:** Brief 3-note fanfare for win (upbeat), 1-note subdued cue for loss. Then silence, so the XP animation plays in clean audio space and the numbers feel more impactful. (`MusicDirector.StopMatchMusic()` cuts all layers on `MatchEnd`/`PostMatch`.)

**Implementation:** `AudioManager.SetMusicParameter("intensity", 0f/1f)` and `SetMusicParameter("tier3", 0f/1f)`, driven by `MusicDirector`, which subscribes to `GameEvents` (`OnMatchTimerTick`, `OnZoneProgressChanged`, `OnTierChanged`) rather than polling the managers directly.

::: info Code deltas, verified against `MusicDirector.cs`
- **Intensity trigger:** rises in the last 90s (`intensityRampWindow = 90f`) OR while Zone C is contested (capture progress between ~5% and ~95%). Matches the design.
- **Intensity crossfade:** `intensityFadeSpeed = 0.2f`/sec gives a ~5s full fade, matching the doc's "cross-fade in over 5s".
- **Tier 3 layer:** gated on the local player reaching `MomentumTier.OnFire` (enum value 3); fades at `tier3FadeSpeed = 1.5f`/sec. Only the local player hears it.
- The fade is smoothed in `MusicDirector.Update()` via `Mathf.MoveTowards`; `AudioManager` then sets both the layer `AudioSource.volume` and the matching exposed mixer float.
:::

---

## SFX categories

### Footsteps

<span class="cc-status built">Implemented</span> `FootstepController.cs` + `FootstepBank.cs`. Surface clips and the `FootstepBank` mapping asset are editor-pending.

| Surface (`SurfaceType`) | Library key | Sound character |
|---|---|---|
| Stone, Courtyard | `footstep_stone` | Solid, resonant click |
| Wood, Market floor | `footstep_wood` | Hollow thud, slight creak |
| Metal, Catwalk grating | `footstep_metal` | Ringing clank |
| Dirt, Underground Cellar | `footstep_dirt` | Dull, dampened |

Each key holds the `01..04`/`01..03` variants; `AudioLibrary` picks one at random per step.

- **Surface detection:** `Physics.RaycastNonAlloc` straight down from the feet (origin +0.4m, probe length 1.5m); the nearest non-self collider's `PhysicsMaterial` name is resolved to a `SurfaceType` by `FootstepBank` (exact case-insensitive match, then substring containment, then default `Stone`). A designer can name a material `Courtyard_Stone` and it still resolves.
- **Step rate:** speed-driven cadence (not animation events in the shipped build). Interval lerps from `0.55s` at the walk threshold (`0.6 m/s`) down to `0.3s` at full sprint, with a `2.2m` travelled-distance fallback that forces a step regardless of the timer.
- **Carrot Silent Steps passive:** steps are skipped (not just volume 0) when a Carrot moves below `SprintSpeed * SilentStepsSpeedFraction`, with `SilentStepsSpeedFraction = 0.5f`, i.e. under 50% sprint, matching the design.
- **Crouch:** footsteps are attenuated by 80% while crouching (`crouchVolumeMultiplier = 0.2f`). *(New implementation detail, not in the original doc.)*
- **3D spatial:** `AudioRolloffMode.Logarithmic`, max hearing distance `25m` (`GameConstants.AudioMaxDistance`).

---

### Weapons

<span class="cc-status built">Implemented</span> `WeaponController.cs` emits fire/reload/impact SFX by key; clips editor-pending.

| Event | Sound | Notes |
|---|---|---|
| The Nub (SMG) fire | Fast metallic click/crack | Rapid-fire; distinct from AR to avoid confusion |
| Pepper Blaster (Shotgun) fire | Deep, wet boom | Meatiness signals high damage potential |
| The Stem (AR) fire | Crisp 3-round burst | Each burst has a slightly different pitch variant |
| Root Cannon (LMG) fire | Heavy, sustained chug | Lower frequency than SMG; "suppressing fire" feel |
| The Pip (Pistol) fire | Clean, sharp pop | Distinct from primaries; lighter |
| Any weapon reload | Mechanical clunk + click | Class-appropriate: SMG quick, LMG heavy drag |
| Empty clip | Dry click × 2 | Plays instead of fire; immediate "reload now" cue |
| Bullet impact (body) | Soft thud | Spatial; heard by nearby players |
| Bullet impact (headshot) | Crisper crack | Different tone; also triggers on-hit sound on HUD |
| Kill confirmation | Satisfying short chime | 2D (not spatial); only heard by killer |

**Keys the code actually emits:**

- Fire: `weapon_{weaponName}_fire`, e.g. `weapon_The Nub_fire`, `weapon_Pepper Blaster_fire`, `weapon_The Stem_fire`, `weapon_Root Cannon_fire`, `weapon_The Pip_fire` (the `weaponName` string comes straight from each `WeaponDataSO`).
- Reload: `weapon_{weaponName}_reload`.
- Impact: a single `impact_surface` (on a world hit) or `impact_air` (on a clean miss). The pitch-variant feel for fire/impact is intended via library variants.

::: info Code deltas, verified against `WeaponController.cs`
- There are no separate body vs. headshot impact keys, and no dedicated kill-confirmation key wired yet. The headshot flag *is* tracked (`PlayerHitbox.IsHead` drives the headshot damage multiplier and HUD hit-marker tint via `FeedbackController`), but a distinct headshot/kill audio cue is editor-pending. Treat the "body / headshot / kill confirmation" rows above as the design target, not yet-keyed clips.
- The empty-clip "dry click" is a design target; it is not a separately keyed clip in the current build.
:::

---

### Abilities

<span class="cc-status built">Implemented</span> All 16 ability behaviours play their cues. Clips editor-pending.

Abilities don't hard-code keys. Each ability `data` (the ability `ScriptableObject`) exposes `sfxActivate`, `sfxImpact`, and `sfxLoop` fields, so the activate/impact/loop cues below are authored per-ability in the editor. `AbilityBase` plays `sfxActivate` on cast; AOE impls (`Ability_SpiceBurst`, `Ability_EarthenSlam`, `Ability_SporeCloud`, `Passive_BurnStreak`) play `sfxImpact`; `Ability_HeatTrail` runs `sfxLoop` via `AudioManager.StartLoop`/`StopLoop`. The `EffectSystem` also plays two fixed keys when statuses are applied: `spice_slow_apply` (slow) and `starch_activate` (armour).

| Ability | Sound | Notes |
|---|---|---|
| Sprint Dash | Whoosh + leaf rustle | Fast; 0.15s total |
| Radar Pulse | Resonant electronic ping then fade out | Slight reverb on the detect ping |
| Spice Burst (launch) | Sizzle + pop | Launch sound when thrown |
| Spice Burst (impact) | Hissing burst + sizzle | AOE impact |
| Spice Burst (on target) | Gurgling slow sound | Played on slowed targets; 3D |
| Heat Trail (deploy) | Searing hiss, continuous | Loops while trail is active; hot exhaust feel |
| Heat Trail (player in trail) | Pain sting + sizzle | Short, repeated every 0.5s of contact |
| Burn Streak (passive trigger) | Short crackle | On kill; brief, not distracting |
| Leaf Shield (deploy) | Rustling whomp | Like a barrier of leaves snapping into place |
| Leaf Shield (hit) | Rustle + crunch | Damage to shield; distinct from armor hit |
| Leaf Shield (destroyed) | Wet crunch + burst | Satisfying destruction sound |
| Spore Cloud (throw) | Soft thud |  |
| Spore Cloud (active) | Sustained hiss + spore rattle | Loops; heard inside the cloud |
| Regen Aura (passive) | Very subtle leaves rustling | Nearly inaudible; present only to reinforce UI heal |
| Starch Armor (activate) | Crunch + hardening sound | Starch snap, popcorn-esque |
| Starch Armor (hit while active) | Dry crunch (different from base hit) | Signals armor absorbing; the player can hear the difference |
| Starch Armor (expire) | Soft crumble | Armor dissolving |
| Earthen Slam (windup) | Brief grunt + bass whomp | Telegraphs the slam to nearby enemies |
| Earthen Slam (impact) | Heavy bass thud + crumble | Rumble felt through vibration if controller |

---

### Momentum

<span class="cc-status built">Implemented</span> `FeedbackController.cs` plays tier-up / loss / absorb cues (2D) on `OnTierChanged`. Clips editor-pending.

| Event | Sound | Key | Notes |
|---|---|---|---|
| Tier 0 → 1 (Warm) | Short ascending chime | `momentum_tier1` | Subtle, not distracting |
| Tier 1 → 2 (Hot) | Brighter ascending chime | `momentum_tier2` | Slightly louder; fire crackle undertone |
| Tier 2 → 3 (On Fire) | Power surge: bass hit + fire rush | `momentum_tier3` | Unmistakably tier 3; satisfying |
| Kill transfer / jump to tier 3 | Short rising sting | `momentum_absorb` | Absorbing a victim's charge (also when a kill bumps you straight to tier 3) |
| Tier lost (to decay) | Descending chime | `momentum_lost` | "Power-down" feel |

::: info Code deltas, verified against `FeedbackController.cs`
Enum names match the design (`MomentumTier`: `Cold=0, Warm=1, Hot=2, OnFire=3`). Two design cues are not yet keyed in code:
- **Tier decay warning (< 3s):** the pulsing pre-decay beep is editor-pending; no key is emitted.
- **Tier reset on death:** intentionally silent (the death audio covers it), as designed; nothing to wire.
All momentum cues are routed through `PlayUi` (2D, not spatial).
:::

---

### Objectives

<span class="cc-status built">Implemented</span> `CaptureZone.cs` / `GameModeManager.cs` emit zone + match-flow cues. Clips editor-pending.

| Event | Sound | Key | Notes |
|---|---|---|---|
| Zone capture complete | Clean resonant gong | `zone_capture_complete` | 3D spatial from the zone's position |
| Zone C unlock | Kitchen bell + crowd surge | `zone_c_unlock` | Global (2D); loudest in-match event; unmistakable |
| 1-minute warning | Tension cue | `warn_1min` | Fires once at 60s remaining |
| 30-second warning | Tension cue | `warn_30s` | Fires once at 30s remaining |
| Match end (win / draw) | Fanfare / neutral sting | `match_win` / `match_draw` | All gameplay music cut first; sting plays in clean space |

- **Zone C being contested** isn't a clip. It drives the Intensity music layer (see Music architecture); `MusicDirector` listens to `OnZoneProgressChanged` for Zone C.

::: info Code deltas, verified against `CaptureZone.cs` / `GameModeManager.cs`
- Capture **start** (per-team rising/ominous tones) and **per-team** complete variants are design targets; the code currently emits a single `zone_capture_complete` (3D) regardless of team. Per-team variation is editor-pending.
- The doc's "score cap reached" is handled by the match-end keys above (`match_win`/`match_draw`); a separate "draw" outcome key was added in code that the original doc didn't list.
- The countdown-style 60s/30s warnings (`warn_1min`/`warn_30s`) are an implementation addition beyond the original objective table.
:::

---

### UI

<span class="cc-status partial">Partial</span> Menus, post-match, and the challenge system call `PlayUi` with the keys below. Other rows are design targets not yet keyed; all clips editor-pending.

| Event | Sound | Key | Notes |
|---|---|---|---|
| Button hover | Very soft click | `ui_button_hover` | Subtle; doesn't fatigue |
| Button confirm | Slightly heavier click | `ui_button_confirm` | Used across main menu / class select / settings / post-match |
| XP gain animation | Coin-esque rising tone | `ui_xp_tick` | Plays per tick alongside the XP counter |
| Daily challenge complete | Short positive chime | `ui_challenge_complete` | In-HUD; brief |
| Low-health heartbeat | Pulsing thud | (`ScreenEffects` heartbeat key) | 2D; driven by `ScreenEffects` low-HP state |

::: info Code deltas, verified against the menu/HUD scripts
These design rows are not yet keyed in code (editor-pending): **Match found**, **Countdown (5 → 1)**, **Respawn countdown**, **Battle pass tier up**. The battle-pass and match-found cues will be wired when those flows get audio assets. All keyed UI cues route through `PlayUi` (2D).
:::

---

### Ambient (per zone)

<span class="cc-status built">Implemented</span> `AmbientZone.cs`: a trigger volume per zone that starts/stops one library loop for the local player. Loop clips and placed trigger volumes are editor-pending.

| Zone | Ambient layer | Notes |
|---|---|---|
| Zone A Courtyard | Market crowd chatter (quiet), wind, distant cart noise | Warm, busy feel |
| Zone B Indoor Market | Refrigerator hum, dripping water, ambient voice echo | Cooler, enclosed |
| Zone C Central Stage | Distant crowd roar (builds as match timer falls) | Tension amplifier |
| Alley East | Narrower echo; wind rushing through passage | |
| Underground Cellar | Deep rumble, near-silence otherwise | Stealth atmosphere |

Each `AmbientZone` carries an `ambientKey` (e.g. `ambient_courtyard`) and a `spatial` flag. Spatial beds attenuate from the zone centre; non-spatial beds play as a global 2D atmosphere (routed to the Ambience mixer group).

**Zone transitions:** on `OnTriggerExit` the loop's stop is deferred by `crossfadeSeconds = 1.5f`; re-entering (or entering an overlapping zone) within that grace cancels the pending stop, so walking a boundary never hard-cuts the bed. This delivers the ~1.5s smooth transition from the original design.

---

## Technical implementation

<span class="cc-status built">Implemented</span> `AudioManager.cs` (singleton, `DontDestroyOnLoad`). The `AudioMixer` graph and an `AudioLibrary` asset must be wired in the editor; until then it runs silent.

- `AudioManager` singleton using pooled `AudioSource` components, pool size `32` (`GameConstants.AudioSourcePoolSize`). When all sources are busy it steals the oldest rather than dropping the cue.
- Category buses via Unity `AudioMixer`. The code wires four serialized groups: **Music**, **SFX**, **UI**, **Ambience**. 2D one-shots go to the UI group, 3D one-shots and spatial loops to the SFX group, non-spatial loops to the Ambience group. (The finer per-category SFX sub-buses for Footsteps / Weapons / Abilities are an editor-side mixer-graph detail, not separate code groups.)
- Sound lookup: gameplay code calls `PlaySfx(key, pos)` (3D) / `PlayUi(key)` (2D) / `StartLoop(key, pos, spatial)`. `AudioLibrary.Resolve(key)` returns a random variant for that key, or `null` (caller no-ops) when unwired.
- In-world sounds: `spatialBlend = 1.0` (full 3D), `AudioRolloffMode.Logarithmic`, `maxDistance = 25m`. Spatialisation respects the `SpatialAudio` setting; when disabled, even 3D cues collapse to 2D (`spatialBlend = 0`).
- UI / confirmation sounds: `spatialBlend = 0.0` (2D, no rolloff).
- Music: one looping `AudioSource` per layer (`MusicBase` / `MusicIntensity` / `MusicTier3`); crossfades are done by lerping layer volume plus the matching exposed mixer float (see `SetMusicParameter`), not snapshot transitions.
- Volume: master/music/SFX/voice volumes come from `SettingsService` and are pushed to exposed mixer floats (`MasterVolume`, `MusicVolume`, `SfxVolume`, `VoiceVolume`) on settings change, converted linear to dB.
- Footstep surface detection: `PhysicsMaterial` name resolves to a `SurfaceType` enum, then to a `footstep_{surface}` key, via the `FootstepBank` ScriptableObject.
- Audio occlusion (optional, Phase 5): raycast between source and listener, attenuate through walls. Not implemented yet.

::: info Mixer-group delta
The original doc listed Footsteps/Weapons/Abilities/UI/Ambient as buses under SFX. In code the serialized groups are **Music / SFX / UI / Ambience**; footsteps, weapons and abilities all currently route through the single **SFX** group. Splitting SFX into the finer sub-buses is left to the editor-authored `AudioMixer` asset.
:::

---

## Audio assets needed (to produce or source)

<span class="cc-status pending">Editor-pending</span> The runtime is done; this is the remaining content checklist. Once produced, clips drop into the `AudioLibrary` (under the keys documented above) and `FootstepBank` assets, with no code changes required.

| Category | Estimated files | Format | Notes |
|---|---|---|---|
| Music (stems) | 3 layers × 1-2 loops | WAV / OGG, 44.1kHz | Commission or license |
| Footsteps | 4 surfaces × 4 variants | Short WAV | Free SFX banks exist (Freesound) |
| Weapon fire | 5 weapons × 2-3 variants | WAV | Kitchen-themed reimagining |
| Reload | 5 weapons × 1 | WAV | |
| Ability SFX | ~18 unique cues | WAV | Food-themed design priority |
| Momentum tiers | 4 tier-up, 1 tier-down, 1 warning | WAV | Designed as a set; ascending family |
| Objective cues | ~8 unique | WAV | |
| UI SFX | ~12 unique | WAV | Short, punchy |
| Ambient layers | 5 zones | OGG loop | Low-memory ambient loops |
| **Total** | **~90 files** | | |
