# Game Feel

The invisible layer that separates a game that "works" from one that feels good to play. Every mechanical event should have a corresponding audio + visual response. This doc specifies all juice.

> Reference bar: Apex Legends for movement feel, Valorant for weapon crispness, Overwatch for ability readability.

---

## Movement Feel

### Camera Bob
- **Walking:** vertical oscillation, amplitude 0.03m, frequency 1.5Hz
- **Sprinting:** amplitude 0.05m, frequency 2.5Hz; slight horizontal sway (0.02m) 
- **Crouching:** no bob
- **Landing from jump:** single landing bob — 0.08m downward snap over 1 frame, smooth recovery over 0.15s (spring-interpolated)
- **Implementation:** `CameraShake` component with sine wave driver; landing bob uses `AnimationCurve` lerp

### Sprint
- FOV increases from base (default 90°) to sprint FOV (96°) over 0.2s when sprint starts
- FOV returns to base over 0.3s when sprint ends
- Sprint start: camera tilts very slightly forward (1.5° pitch) — sense of leaning in

### Jump
- Jump: slight upward head bob (0.04m) on takeoff frame
- Airborne: reduced camera bob
- Landing: see landing bob above

### Crouch
- Camera smoothly lowers by 0.4m over 0.15s (not instant snap)
- Movement becomes silent (footstep volume reduced 80% regardless of class)

---

## Dash (Carrot — Sprint Dash)

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

### All Abilities
- 1-frame "activation micro-pause": weapon fire input ignored for 1 frame at ability activation — creates a clear "moment" before the ability effect
- Ability slot: white flash on activation, then cooldown drain begins with a clockwise wipe overlay
- First-person hand animation plays when ability activates (even if no hand is modelled — camera nudge substitutes)

### Sprint Dash (Carrot)
- FOV surge (documented above)
- Particles at origin and destination
- Hair/leaf particles in wake (class-themed)

### Radar Pulse
- Expanding ring particle at player position (visible to all nearby)
- Revealed enemies: orange outline effect appears on their model, fades after 2.5s
- Sound: clean ping with slight reverb

### Spice Burst
- Throw arc shown briefly as a dotted line (0.3s preview only — like a grenade indicator)
- Impact: expanding spice cloud particle for 0.3s, then dissipates into mist
- Slowed enemies: shimmer effect on their model (heat-haze shader on outline)

### Heat Trail
- Fire particles spawn at Jalapeño's feet while active — bright, readable
- When enemy enters trail: camera shake for that enemy (0.1 magnitude, rapid-interval — "I'm on fire" feel)
- Trail fades with particle alpha fadeout over 0.5s after duration ends

### Leaf Shield
- Deploys with a rustling snap particle burst
- Shield object is clearly readable: bright green leaf texture, outlined in white
- Takes visible damage: mesh darkens and acquires cracks as HP decreases
- Destruction: mesh shatters into leaf particles (satisfying)

### Spore Cloud
- Cloud is opaque from enemy perspective (grey-green fog volume)
- From inside: desaturated, slightly blurred edges (post-process vignette)
- Subtle particle drift in cloud (spores floating)
- Dissipates: alpha fadeout over 0.5s at duration end

### Starch Armor
- Activation: yellow particle burst around Potato + the HP bar yellow segment appears
- While active: subtle yellow outline on Potato model (other players can see it)
- Taking hits while armored: yellow crumble particles fly off at hit point
- Expiry: particles crumble off + yellow outline fades

### Earthen Slam
- Windup: Potato crouches slightly + camera shake for Potato (0.05, 0.2s — telegraphs to them too)
- Airborne: brief camera tilt forward (~2°)
- Impact: heavy radial camera shake for everyone within 8m (0.4 magnitude, 0.3s) + dirt/veggie particle burst
- Knockback victims: brief screen shake for them on launch (0.25, 0.2s)
- Impact crater visual: temporary scorch mark decal on ground (~1s fade)

---

## Momentum Feel

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

---

## Screen Shake Summary

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

**Implementation:** `CameraShake` component; `ShakeType` enum (Directional, Radial, Random); `IEnumerator Shake(magnitude, duration, type)`. Player can disable screen shake in accessibility settings.

---

## Polish Checklist (Phase 5)

- [ ] All abilities have a distinct, themed SFX (no placeholder)
- [ ] Hit-stop implemented for all weapon types
- [ ] Momentum tier-up has unique feel for each tier
- [ ] Death camera tilt working + respawn fade implemented
- [ ] Damage directional indicator functional
- [ ] Zone C unlock is a "wow" moment — bell, camera bump, barriers, HUD flash
- [ ] Post-match XP counter animates on screen with correct timing
- [ ] Screen shake toggleable in accessibility settings
- [ ] All screen shakes tested at max magnitude to confirm no nausea risk
- [ ] 5 playtests of "first 10 minutes" to verify FTUE feel lands
