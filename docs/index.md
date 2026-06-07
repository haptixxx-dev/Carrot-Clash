# Carrot Clash — Design Documentation

Fast-paced tactical FPS with food/vegetable-themed hero classes, objective-based team combat, and a momentum/combo system that rewards aggressive play over camping.

---

## Documents

| Doc | Description | Status |
|---|---|---|
| [GDD](gdd.md) | Master GDD — overview, pillars, retention, monetization, accessibility, mobile | ✅ |
| [Core Loop](core-loop.md) | Match state machine, scoring system, feedback loops | ✅ |
| [Momentum System](momentum-system.md) | Kill combo mechanic, tier rules, network sync, UI spec | ✅ |
| [Characters](characters.md) | All 4 classes with stats, abilities, counters, synergies | ✅ |
| [Weapons](weapons.md) | Per-class weapons, damage stats, TTK tables, ammo system | ✅ |
| [Ability Interactions](ability-interactions.md) | How abilities interact, status effect rules, edge cases, anti-abuse | ✅ |
| [Map](map.md) | Grand Food Market — zones, flanking routes, spawn rules, audio | ✅ |
| [Game Feel](game-feel.md) | Screen shake, hit feedback, dash feel, death/respawn, all juice | ✅ |
| [Audio](audio.md) | Music architecture, full SFX spec, ambient per zone, asset list | ✅ |
| [UI / UX](ui-ux.md) | All screens wireframed, HUD spec, mobile layout | ✅ |
| [Progression](progression.md) | XP system, battle pass, daily challenges, FTUE, ranked | ✅ |
| [Tech Architecture](tech-architecture.md) | Unity C# architecture, ScriptableObjects, NGO, bot AI, input | ✅ |
| [Milestones](milestones.md) | 5-phase build plan with checklists and time estimates | ✅ |

---

## Quick Reference

| Property | Value |
|---|---|
| Genre | Team FPS, objective + elimination hybrid |
| Perspective | First-person, fast/tactical |
| Team size | 3v3 or 4v4 |
| Match length | 8 minutes (+ 60s sudden death if tied) |
| Win condition | First to 500 points, or highest at timer end |
| Classes | Carrot, Jalapeño, Broccoli, Potato |
| Map | The Grand Food Market (3 zones) |
| Engine | Unity 6, URP |
| Networking | Unity Netcode for GameObjects + Unity Relay |
| Target platforms | PC (primary), mobile (secondary) |
| Vertical slice estimate | 13–19 weeks |

---

## Design Pillars (Short Form)

1. **Momentum over camping** — kills escalate your power; decay punishes passivity
2. **Class synergy** — 4 roles that combo; no class lock
3. **Readable chaos** — fast and fun but never confusing
4. **Theme-first identity** — food theme is mechanical, not cosmetic
