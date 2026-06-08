# Game Feel

The invisible layer that separates a game that "works" from one that feels good to play. Every mechanical event should have a corresponding audio + visual response. This doc specifies all juice.

> Reference bar: Apex Legends for movement feel, Valorant for weapon crispness, Overwatch for ability readability.

::: info CODE STATUS (2026-06)
The feel **logic** is implemented in code and verified against the numbers below — see `PlayerCamera.cs`, `FeedbackController.cs`, `ScreenEffects.cs`, `DamageDirectionIndicator.cs`, `OnFireOverlay.cs`, the ability impls, and `GameConstants.cs`. What every juice hook still needs is **editor-authored content**: the particle/VFX prefabs, decals, hand/weapon animations, and the actual audio clips the code triggers by key. Those are <span class="cc-status pending">Editor-pending</span> across the board, so treat the status callouts in this doc as "the *driver* exists, the *assets* do not."

Engineering source of truth: [/dev/getting-started](/dev/getting-started) and `Assets/_Game/CONTRACTS.md`.
:::

---

## Movement Feel

<span class="cc-status built">Implemented</span> — `PlayerCamera.cs` drives FOV transitions (sprint/ADS/dash), sine-wave camera bob, the landing snap, screen shake, and the death tilt; all bob/FOV numbers below match the code. Honours the "reduce motion" accessibility setting. Hand/weapon model animation is <span class="cc-status pending">Editor-pending</span> (no player prefab/mesh yet).

### Camera Bob
- **Walking:** vertical oscillation, amplitude 0.03m, frequency 1.5Hz
- **Sprinting:** amplitude 0.05m, frequency 2.5Hz; slight horizontal sway (0.02m) 
- **Crouching:** no bob
- **Landing from jump:** single landing bob — 0.08m downward snap, smooth recovery over 0.15s
- **Implementation:** `PlayerCamera.cs` — sine-wave bob driver; landing bob is the `LandBob()` coroutine (0.08m drop easing back over 0.15s with a quadratic falloff). (Note: an `AnimationCurve` was the original plan; the shipped code uses a coroutine ease instead.)

### Sprint
- FOV increases from base (default 90°) to sprint FOV (96°) over 0.2s when sprint starts
- FOV returns to base over 0.3s when sprint ends
- Sprint start: camera tilts very slightly forward (1.5° pitch) — sense of leaning in

::: info Code delta
`GameConstants` matches the design: `DefaultFov` 90, `SprintFov` 96, `AdsFov` 85, `SprintFovTime` 0.2s (used for both the in/out `SmoothDamp`). The sprint **forward-tilt** (1.5° pitch lean) is **not yet applied** in `PlayerCamera.cs` — only the FOV change is wired. Landing bob is a 0.08m downward snap recovering over 0.15s (matches). Sprint bob amplitude/frequency (0.05m / 2.5Hz + 0.02m horizontal sway) and walk bob (0.03m / 1.5Hz) match exactly.
:::

### Jump
- Jump: slight upward head bob (0.04m) on takeoff frame
- Airborne: reduced camera bob
- Landing: see landing bob above

### Crouch
- Camera smoothly lowers by 0.4m over 0.15s (not instant snap)
- Movement becomes silent (footstep volume reduced 80% regardless of class)

---

## Dash (Carrot — Sprint Dash)

<span class="cc-status partial">Partial</span> — `Ability_SprintDash.cs` performs the displacement and calls `PlayerCamera.FovSurge(10°)` for the whoosh (`GameConstants.DashFovSurge = 10`, recovering over 0.2s). The motion blur, speed-line particles, leaf/dust bursts, and the Spore-Cloud-exit clean-air flash are <span class="cc-status pending">Editor-pending</span> VFX. Per design, the camera does **not** shake during the dash, and the code confirms this — no `Shake` call is made.

The most impactful single-movement event in the game. Must feel explosive.

| Frame | Event |
|---|---|
| Frame 0 (activation) | Ability slot flashes white; whoosh SFX triggers |
| Frame 0–1 | FOV surges: +10° over 1 frame |
| Frame 0–5 | Directional motion blur in dash direction (single-pass blur, strength 0.4) |
| Frame 5 | Movement lands at destination |
| Frame 5–12 | FOV recovers to base over 7 frames |
| Frame 5–12 | Speed lines particle effect fades out |

- Camera does NOT shake during dash (shaking during fast movement = nausea risk)
- Leaf/dust particle burst at dash origin and destination
- If dash exits a Spore Cloud: bright flash of clean air on exit frame

---

## Weapon Feel

<span class="cc-status partial">Partial</span> — `WeaponController.cs` fires hitscan with falloff + headshots, applies recoil/camera-kick, and raises `OnHitConfirmed`; `FeedbackController.cs` turns that into hit-stop + big-hit shake; `CrosshairUI`/`HitMarkerUI` render the crosshair and markers. Muzzle flash, shell-casing ejection, the magazine-pop reload mesh, and iron-sight visuals are <span class="cc-status pending">Editor-pending</span> (VFX/animation/prefab work). All firing/impact SFX are triggered by **key** (`weapon_<name>_fire`, `impact_surface`, `impact_air`) but the clips themselves are unauthored.

::: info Code delta — hit-stop is a time-scale dip, not an animation pause
The doc describes hit-stop as a 1–2 frame *weapon-animation* pause. In code it is implemented as a brief global `Time.timeScale` dip in `FeedbackController.cs`: body hit ≈ 0.02s, kill ≈ 0.035s (≈ 1 and 2 frames at 60fps, so the felt timing matches). Overlapping hits *refresh* rather than stack the dip, and it is skipped entirely under reduce-motion. **Recoil** is partial: the per-weapon `recoilPattern` (Vector2[]) and `cameraKick` exist and the kick is applied as a small upward `Directional` shake (`cameraKick * 0.2`, 0.05s), but full pattern-driven crosshair walk is "kept minimal" in the current `ApplyRecoil()`.
:::

### Firing
- **Hit-stop:** 1 frame pause on weapon animation when bullet connects (not on miss) — makes hits feel impactful vs. shooting into air
- **Kill hit-stop:** 2 frames — slightly more weight than a body hit
- **Recoil:** Pattern-based (learnable). Each weapon has a recorded recoil pattern (array of Vector2 offsets); after fire, crosshair recovers to base over `recoveryTime` seconds
- **Muzzle flash:** 1–2 frame particle burst at weapon barrel; scales with weapon calibre (SMG = small, LMG = large)
- **Shell casing:** ejection particle (visible in first-person; small rotating capsule mesh that falls and fades)
- **Camera kick:** very slight upward camera nudge on each shot, proportional to weapon damage — SMG barely moves, LMG has visible kick

### Hit Confirmation
| Event | Visual | Audio | Duration |
|---|---|---|---|
| Body hit (on target) | White X hit marker, crosshair turns red | Crack + thud | 0.1s |
| Headshot (on target) | Larger orange/gold X marker | Louder crack | 0.15s |
| Kill | Gold marker + ring flash | Chime + kill SFX | 0.2s, then feed |
| Hit on Starch Armor | White X but with different tint (yellow) | Dry crunch | 0.1s |
| Missed shot (wall hit) | No crosshair change | Surface impact SFX only | — |

### Reload
- Weapon lowers slightly during reload (0.1m drop over 0.2s)
- Magazine ejects at mid-reload: visible mesh pops off and falls
- Reload complete: weapon snaps back up with a crisp click

### ADS (Aim Down Sights)
- FOV reduces from base (90°) to ADS FOV (85°) over 0.15s — subtle, not zoomed
- Weapon model moves to center of screen
- Crosshair fades out (replaced by weapon iron sight visual)
- Crosshair reappears 0.15s after leaving ADS

---

## Ability Feel

<span class="cc-status partial">Partial</span> — all 16 ability behaviours exist (`Assets/_Game/Characters/Abilities/Impl/`) and each calls `PlayActivationFeedback()` + a data-driven SFX key, with `AbilitySlotUI.cs` running the cooldown wipe. The **gameplay** effect of every ability is implemented; the **VFX** (particles, outlines, fog volumes, shield mesh) and the audio clips are <span class="cc-status pending">Editor-pending</span>. Status callouts below flag where the *effect logic* is fully built vs where only the activation hook exists.

### All Abilities
- 1-frame "activation micro-pause": weapon fire input ignored for 1 frame at ability activation — creates a clear "moment" before the ability effect
- Ability slot: white flash on activation, then cooldown drain begins with a clockwise wipe overlay
- First-person hand animation plays when ability activates (even if no hand is modelled — camera nudge substitutes)

### Sprint Dash (Carrot)
- FOV surge (documented above)
- Particles at origin and destination
- Hair/leaf particles in wake (class-themed)

### Radar Pulse
<span class="cc-status partial">Partial</span> — `Ability_RadarPulse.cs` scans a 15m sphere and reveals enemies for 2.5s via an emissive carrot-orange tint that auto-restores (a placeholder "outline" until a real outline shader/VFX is authored).
- Expanding ring particle at player position (visible to all nearby)
- Revealed enemies: orange outline effect appears on their model, fades after 2.5s
- Sound: clean ping with slight reverb

### Spice Burst
<span class="cc-status partial">Partial</span> — `Ability_SpiceBurst.cs` applies the slow via `EffectSystem.ApplySlow` (`SlowAmount` -35%, `SlowDuration` 3s — matches design); the throw-arc preview, cloud particle, and heat-haze shimmer are VFX pending.
- Throw arc shown briefly as a dotted line (0.3s preview only — like a grenade indicator)
- Impact: expanding spice cloud particle for 0.3s, then dissipates into mist
- Slowed enemies: shimmer effect on their model (heat-haze shader on outline)

### Heat Trail
<span class="cc-status partial">Partial</span> — `Ability_HeatTrail.cs` + `DamageZone.cs` apply fire DoT (`EffectSystem.ApplyFire`, ticking every `FireTickInterval` 0.1s) to enemies in the trail; fire particles and the victim's "on fire" camera shake are VFX/feel hooks still to be wired to assets.
- Fire particles spawn at Jalapeño's feet while active — bright, readable
- When enemy enters trail: camera shake for that enemy (0.1 magnitude, rapid-interval — "I'm on fire" feel)
- Trail fades with particle alpha fadeout over 0.5s after duration ends

### Leaf Shield
<span class="cc-status partial">Partial</span> — `Ability_LeafShield.cs` + `DestructibleCover.cs` spawn a HP-bearing shield that blocks shots and is destroyed when depleted; the readable leaf mesh, crack/darken states, and shatter particles are <span class="cc-status pending">Editor-pending</span> art.
- Deploys with a rustling snap particle burst
- Shield object is clearly readable: bright green leaf texture, outlined in white
- Takes visible damage: mesh darkens and acquires cracks as HP decreases
- Destruction: mesh shatters into leaf particles (satisfying)

### Spore Cloud
<span class="cc-status partial">Partial</span> — `Ability_SporeCloud.cs` + `VisionObscured.cs` create the vision-blocking volume; the fog rendering, inside-view post-process, and spore drift are VFX pending.
- Cloud is opaque from enemy perspective (grey-green fog volume)
- From inside: desaturated, slightly blurred edges (post-process vignette)
- Subtle particle drift in cloud (spores floating)
- Dissipates: alpha fadeout over 0.5s at duration end

### Starch Armor
<span class="cc-status partial">Partial</span> — `Ability_StarchArmor.cs` grants temp HP via `EffectSystem.ApplyTemporaryHP` (`StarchArmorAmount` 40, `StarchArmorDuration` 4s — matches design); `HealthBarUI` shows the yellow temp-HP segment. The yellow outline + crumble particles are VFX pending.
- Activation: yellow particle burst around Potato + the HP bar yellow segment appears
- While active: subtle yellow outline on Potato model (other players can see it)
- Taking hits while armored: yellow crumble particles fly off at hit point
- Expiry: particles crumble off + yellow outline fades

### Earthen Slam
<span class="cc-status partial">Partial</span> — `Ability_EarthenSlam.cs` does an `OverlapSphere` (≈4m AoE radius from the SO) and applies `EffectSystem.ApplyKnockback` (`KnockbackDistance` 6m, `StaggerDuration` 0.5s, `KnockbackElevation` 15° bounce — all match design). The self radial shake (0.4 mag / 0.3s) and the victim launch shake (0.25 / 0.2s) are wired in code. Crater decal + dirt particles are <span class="cc-status pending">Editor-pending</span>.
- Windup: Potato crouches slightly + camera shake for Potato (0.05, 0.2s — telegraphs to them too)
- Airborne: brief camera tilt forward (~2°)
- Impact: heavy radial camera shake for everyone within ~8m (0.4 magnitude, 0.3s) + dirt/veggie particle burst
- Knockback victims: brief screen shake for them on launch (0.25, 0.2s)
- Impact crater visual: temporary scorch mark decal on ground (~1s fade)

::: info Code delta — two radii
The **damage/knockback AoE** is ~4m (the `OverlapSphere` in `DoSlam()`, driven by `AbilityDataSO.radius`). The **camera-shake reach** of ~8m in the screen-shake table is a separate, larger "feel" radius (so nearby players feel the rumble without being knocked back). In the current code the 0.4 self-shake fires for the casting Potato only; the per-bystander radial shake at 8m / distant rumble at <10m are spec'd here but not yet distance-gated in code.
:::

---

## Momentum Feel

<span class="cc-status built">Implemented</span> — `MomentumController.cs` drives tiers/decay/transfer; `FeedbackController.cs` plays the per-tier shake + SFX on `OnTierChanged`; `MomentumRingUI.cs` renders the ring and decay-warning pulse; `OnFireOverlay.cs` shows the first-tier-3 flourish. VFX (ring trail particles, screen-edge glow) and the actual chime/sting clips are <span class="cc-status pending">Editor-pending</span>.

::: info Code deltas
Per-tier feel matches the design intent and is graded in code: tier 1 (Warm) shake 0.05, tier 2 (Hot) 0.07, tier 3 (On Fire) 0.10 (the screen-shake table only lists the tier-3 0.10 — the lower two come from `FeedbackController`). Each plays `momentum_tier1/2/3`; reaching tier 3 also fires `momentum_absorb` (covers the kill-jump-to-tier-3 case) and, the first time per session, the **YOU'RE ON FIRE** overlay (total ≈1.5s: 0.2 in / 1.1 hold / 0.2 out). Dropping to tier 0 plays `momentum_lost`. Tuning numbers from `GameConstants`: `MomentumDecayInterval` 12s, `MomentumTransferOnDeath` 50% (Potato override `MomentumTransferPotato` 25%), `MaxTier` 3.
:::

### Tier-Up
| Tier | Visual | Audio | Camera |
|---|---|---|---|
| → Tier 1 (Warm) | Ring fills to class colour | Short ascending chime | Subtle 0.05 camera bump |
| → Tier 2 (Hot) | Ring brightens, trail particles begin | Brighter chime + crackle | Slightly stronger bump |
| → Tier 3 (On Fire) | Ring animates, screen edge glow pulses, full VFX | Power surge | 0.1 magnitude quick burst shake |

- First-ever Tier 3 in session: full-screen brief overlay "YOU'RE ON FIRE" (1.5s, translucent)

### Tier Decay Warning
- Momentum ring pulses at 2Hz (fast flicker) when < 3s before decay
- Subtle audio beep × 2 per second
- Designed to create urgency without being distracting

### Tier Reset on Death
- Ring shatters: particle system with ring fragments flying outward (camera death point)
- No separate audio — death SFX covers the reset

### Receiving Tier Transfer (killing a high-momentum player)
- "Power absorbed" short rising sting
- Quick fill animation on your ring (shoots up to new tier rapidly, then settles)
- Kill feed shows tier badge on your entry

---

## Damage / Death Feel

<span class="cc-status built">Implemented</span> — `DamageDirectionIndicator.cs` (pooled directional arcs), `ScreenEffects.cs` (low-HP vignette + heartbeat, death audio muffle, respawn fade), `PlayerCamera.DeathTilt()`, and `SpawnManager.cs` (respawn timing + invuln) cover this section. The fade/vignette use full-screen uGUI images (no post-process dependency). Heartbeat SFX key is wired but left blank pending a clip.

::: info Code deltas
- **Per-hit red vignette flash:** the design calls for a brief red edge-flash *per hit*. Code currently implements the **directional damage arc** (`DamageDirectionIndicator`) plus a **persistent** low-HP vignette (`ScreenEffects`); there is no separate per-hit flash yet.
- **Low-HP threshold:** `GameConstants.LowHealthThreshold = 30` (HP). `ScreenEffects` defaults to a `lowHealthFraction` of 0.33 (≈33 HP at 100 max) — close to the doc's "below 30 HP" but driven by a serialized fraction, so tune it on the prefab to land on 30 exactly.
- **Big-hit shake:** matches — hits > 40 damage trigger a 0.15 / 0.1s directional shake (`bigHitDamageThreshold = 40`).
- **Death/respawn timing:** `RespawnTime` 4s, `SpawnInvulnerability` 2s, death tilt 90° over 0.5s, audio muffle 0.5s, fade-to-black 0.2s / fade-in 0.3s — all match.
:::

### Receiving Damage
- **Screen vignette:** red edge glow flashes proportional to damage (brief, 0.1–0.2s per hit)
- **Directional indicator:** small arc appears on HUD in the direction of incoming damage (like Apex/COD damage indicator) — points toward damage source
- **Camera shake (large hits):** hits > 40 damage cause brief shake (0.15 magnitude, 0.1s)
- **Below 30 HP:** persistent low-pulse red vignette + heartbeat audio

### Death
| Frame | Event |
|---|---|
| Death frame | All gameplay sounds muffled (low-pass filter applied) for 0.5s |
| Death frame | Camera tilts 90° to the side over 0.5s (ragdoll-view feel without actual ragdoll) |
| 0–4s | Death camera: spectate killer (default) or nearest teammate |
| 4s | Respawn fade-to-black (0.2s) |
| 4.2s | Spawn, fade-in from black (0.3s) |
| 4.5s | 2s spawn invulnerability begins; white outline on player own model |

### Killing Someone
- 2-frame hit-stop (weapon animation)
- Kill chime (2D, only you hear it)
- Kill feed entry with your class icon and victim class icon
- "+5 pts" (or "+8 pts" at tier 3) floating text in HUD space for 0.8s

---

## Environmental Feel

<span class="cc-status partial">Partial</span> — `CaptureZone.cs` + `GameModeManager.cs` run capture progress, ownership, and the Zone C unlock at `ZoneCUnlockTime` (300s = minute 5); `ZoneIndicatorUI.cs` flips the C lock icon on `OnZoneCUnlocked`. The animated ground ring, the barrier objects + hydraulic/steam particles, the kitchen-bell moment, and the global camera bump are <span class="cc-status pending">Editor-pending</span> (no scene geometry yet).

### Zone Capture
- Capture zone: animated ring on the ground around the capture point
  - Neutral: slow white pulse
  - Capturing (your team): fills clockwise in your team colour
  - Contested: alternates between both team colours rapidly
  - Captured: full team colour, slower pulse
- Zone C barrier descent: physical barrier objects lower with a hydraulic sound + dust particles

### Zone C Unlock
Biggest audio-visual moment in the match outside of winning:
- Barriers lower over 1.5s with hissing steam particles and creak sound
- Kitchen bell rings once (loud, distinct)
- All players' HUD Zone C icon pulses bright
- Brief camera bump (0.05 magnitude) felt by all players globally (like the earth shook slightly)

::: info Code delta
On unlock, `GameModeManager` raises `GameEvents.OnZoneCUnlocked` and plays the `zone_c_unlock` UI cue; the HUD lock icon updates. The **kitchen bell**, **barrier descent**, and the **global 0.05 camera bump** are not yet wired in code — they depend on the (editor-pending) bell clip, barrier prefabs, and a global-shake broadcast.
:::

---

## Screen Shake Summary

<span class="cc-status built">Implemented</span> — `PlayerCamera.Shake(magnitude, duration, type, direction)` with a falloff envelope; honours the reduce-motion / "disable screen shake" accessibility setting (it early-outs).

| Event | Magnitude | Duration | Type |
|---|---|---|---|
| Receiving damage >40 | 0.15 | 0.1s | Directional (from source) |
| Earthen Slam (in blast radius) | 0.40 | 0.3s | Radial |
| Earthen Slam (Potato landing, self) | 0.10 | 0.15s | Downward |
| Spice Burst impact (in radius) | 0.10 | 0.1s | Radial |
| Momentum Tier 3 achieved | 0.10 | 0.2s | Random |
| Zone C unlock (everyone) | 0.05 | 0.4s | Random — subtle global event |
| Death (received fatal hit) | — | — | Camera tilt, not shake |
| Nearby Earthen Slam (< 10m, not in radius) | 0.08 | 0.15s | Radial (distant rumble) |

**Implementation:** the shake lives on `PlayerCamera` (not a separate `CameraShake` component). `ShakeType` enum is `Random, Directional, Radial, Downward` (the Earthen-Slam self-landing uses `Downward`; the original doc listed three types). `void Shake(float magnitude, float duration, ShakeType type, Vector3 direction)` runs a coroutine with linear magnitude falloff. Player can disable screen shake via the accessibility "reduce motion" setting (`SettingsService.ReduceMotion`), which the method respects.

---

## Polish Checklist (Phase 5)

Code-side feel is wired (the *drivers* exist); items still open are content (clips/VFX) or tuning/playtest passes that need a running scene.

- [ ] All abilities have a distinct, themed SFX — *each ability triggers a key; clips unauthored (editor-pending)*
- [x] Hit-stop implemented for all weapon types — *`FeedbackController` time-scale dip, all weapons via `OnHitConfirmed`*
- [x] Momentum tier-up has unique feel for each tier — *graded shake + per-tier SFX key in `FeedbackController`*
- [x] Death camera tilt working + respawn fade implemented — *`PlayerCamera.DeathTilt` + `ScreenEffects` fade*
- [x] Damage directional indicator functional — *`DamageDirectionIndicator` (pooled arcs)*
- [ ] Zone C unlock is a "wow" moment — *SFX cue + HUD icon wired; bell, global camera bump, barriers still editor-pending*
- [ ] Post-match XP counter animates on screen with correct timing — *`PostMatchController` exists; verify timing in scene*
- [x] Screen shake toggleable in accessibility settings — *`PlayerCamera.Shake` honours `SettingsService.ReduceMotion`*
- [ ] All screen shakes tested at max magnitude to confirm no nausea risk — *needs a playable scene*
- [ ] 5 playtests of "first 10 minutes" to verify FTUE feel lands — *needs scenes/prefabs/art*
