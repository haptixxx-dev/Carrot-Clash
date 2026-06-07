# Weapons

Each class has one fixed primary weapon and access to a shared secondary pistol. Weapons are hitscan (no projectile travel time) to keep feel snappy at the match's pace.

---

## Primary Weapons

### Carrot — SMG "The Nub"

| Stat | Value |
|---|---|
| Damage (body) | 18 |
| Damage (head) | 27 (1.5× multiplier) |
| Fire rate | 750 RPM |
| Effective range | 25m (10% falloff per 5m beyond) |
| Magazine | 30 rounds |
| Reload time | 1.8s |
| Spread (ADS) | Tight |
| Spread (hipfire) | Moderate |

**TTK (body shots, ADS):**
- vs. Carrot (90 HP): 5 shots — 0.40s
- vs. Broccoli (100 HP): 6 shots — 0.48s
- vs. Jalapeño (110 HP): 7 shots — 0.56s
- vs. Potato (140 HP): 8 shots — 0.64s

**Design note:** The Nub rewards consistent aim at close-mid range. Its fast fire rate pairs with Carrot's dash mobility — burst in, empty mag, dash out. Weak beyond 25m; Carrot should not be taking long duels.

---

### Jalapeño — Shotgun "Pepper Blaster"

| Stat | Value |
|---|---|
| Damage (per pellet, body) | 12 |
| Pellets per shot | 8 |
| Max damage (point blank) | 96 |
| Damage (head pellet) | 15 (1.25×) |
| Fire rate | Semi-auto, 100 RPM |
| Effective range | 12m (severe falloff; 50% damage at 15m) |
| Magazine | 6 shells |
| Reload time | 2.6s (full mag swap — no per-shell reload) |

**TTK (all pellets landing, body):**
- vs. Carrot (90 HP): 1 shot — 0.08s (one-shot at ≤8m)
- vs. Broccoli (100 HP): 2 shots — 0.68s
- vs. Jalapeño (110 HP): 2 shots — 0.68s
- vs. Potato (140 HP): 2 shots — 0.68s (at full pellet hits; 3 shots likely in practice)

**Design note:** The Pepper Blaster is the highest-risk, highest-reward weapon. In close range Jalapeño is terrifying; beyond 15m it's nearly useless. This directly supports Jalapeño's role as a room-clearing brawler — get in, slam and burst, get out.

---

### Broccoli — Burst AR "The Stem"

| Stat | Value |
|---|---|
| Damage (body per bullet) | 22 |
| Damage (head per bullet) | 33 (1.5×) |
| Burst size | 3 rounds |
| Burst fire rate | 1200 RPM (within burst) |
| Delay between bursts | 0.85s |
| Effective range | 45m |
| Magazine | 24 rounds (8 bursts) |
| Reload time | 2.2s |

**TTK (body shots, full bursts):**
- vs. Carrot (90 HP): 2 bursts (66 damage) + 1 extra — ~2.0s (support role, not a fragger)
- vs. Broccoli (100 HP): 2 bursts — 2.0s
- vs. Jalapeño (110 HP): 2 bursts — 2.0s
- vs. Potato (140 HP): 3 bursts — 3.0s

**Design note:** The Stem is not a dueling weapon — Broccoli wins firefights through positioning and support, not raw DPS. The burst pattern is forgiving for moderate-skill players (spray control between bursts) but not oppressive. Range advantage compensates for low DPS.

---

### Potato — LMG "Root Cannon"

| Stat | Value |
|---|---|
| Damage (body) | 20 |
| Damage (head) | 26 (1.3×) |
| Fire rate | 400 RPM |
| Effective range | 35m |
| Magazine | 60 rounds |
| Reload time | 3.5s (long — use Starch Armor to cover) |
| Movement penalty while firing | -15% move speed |

**TTK (body shots):**
- vs. Carrot (90 HP): 5 shots — 0.75s
- vs. Broccoli (100 HP): 5 shots — 0.75s
- vs. Jalapeño (110 HP): 6 shots — 0.90s
- vs. Potato (140 HP): 7 shots — 1.05s

**Design note:** The Root Cannon is a sustained-fire suppression weapon. Massive magazine means Potato rarely needs to reload during a push, and the long reload is offset by Starch Armor. The movement penalty reinforces Potato as a stationary anchor — disengage to reload, not to win duels.

---

## Secondary Weapon (All Classes)

### Pistol "The Pip"

| Stat | Value |
|---|---|
| Damage (body) | 28 |
| Damage (head) | 56 (2.0×) |
| Fire rate | Semi-auto, 300 RPM |
| Effective range | 20m |
| Magazine | 12 rounds |
| Reload time | 1.5s |

**Design note:** The Pip is a backup, not a primary alternative. 2× headshot multiplier makes it a legitimate threat at close range for skilled players (3 headshots = 168 damage — kills any class). Switching to pistol is faster than reloading (0.2s swap vs. 1.8–3.5s reload).

---

## Combat Feel Targets

| Parameter | Target value | Reference |
|---|---|---|
| Hitscan confirmation delay | ≤ 1 frame (16ms) | Valorant-level crispness |
| Crosshair spread recovery | 200ms after shot | Tight — rewards burst firing |
| ADS zoom | 1.3× FOV reduction | Not a sniper — just focus |
| ADS time | 0.15s | Fast; no "scoped" delay feel |
| Weapon sway | Subtle only; cancels on ADS | |
| Recoil model | Pattern-based, learnable | Apex-style, not random |
| Kill confirmation | Hit-stop 1 frame + audio crack | Satisfying feedback |

---

## Damage Falloff

All weapons lose damage beyond effective range:

```
[0 → effective_range]    = full damage
[+5m]                    = -10%
[+10m]                   = -25%
[+15m]                   = -45%
[+20m+]                  = -60% (min floor)
```

Shotgun pellets use a steeper curve (50% at +3m beyond effective range).

---

## Ammo System

- **No ammo pickup** — magazines refill automatically after a short delay (3s) post-reload
- Eliminates ammo-scavenging frustration while keeping reload timing as a skill
- Infinite ammo economy keeps pace high — no one is stuck meleeing because they ran dry
