# 🥕 Carrot Clash

A fast-paced **3v3 / 4v4 team FPS** set in a stylized food-market world. Four vegetable hero classes
fight over objectives on a three-zone map. The defining mechanic is a **momentum/combo system**: kills
escalate your power across three tiers, but death transfers that charge to your killer - so every
engagement is a risk-reward bet.

- **Engine:** Unity **6000.4.10f1** (Unity 6.4), URP
- **Networking:** Unity Netcode for GameObjects (+ Relay) - *install pending, see below*
- **Targets:** PC (primary), mobile (secondary)
- **Match:** 8 minutes · first to 500 points, or highest at the timer
- **Design docs:** <https://cc.haptixxx.dev> (built from `/docs`)

---

## Start here (read in this order)

| Doc | What it's for | Who |
|---|---|---|
| **[DEVENV.md](DEVENV.md)** | Set up a blank machine: Unity, Git LFS, IDE, .NET, Node. Windows / macOS / Arch Linux. | Everyone, first |
| **[Assets/_Game/SETUP.md](Assets/_Game/SETUP.md)** | Open the project, install NGO, wire scenes/prefabs, run the smoke-test checklist. | Engineers |
| **[ASSETS.md](ASSETS.md)** | Exhaustive list of every editor-authored asset still needed, with owners for dividing among the team. | Leads / whole team |
| **[Assets/_Game/CONTRACTS.md](Assets/_Game/CONTRACTS.md)** | The frozen public C# API. Update it if you change a spine signature. | Engineers |
| **[docs/](docs/)** | Full game design document (GDD, characters, weapons, momentum, map, etc.). | Everyone |

---

## Project status

**Code: complete (vertical-slice scope).** 97 C# scripts under `Assets/_Game/` implementing player
core, momentum, all 4 classes + 16 abilities, objectives + match flow, full HUD, menus, bots, audio,
progression, and network mirrors. Built contract-first and verified by cross-file + per-file review.

**Not yet done - editor-authored content** (can't be created from source alone): scenes, the player
prefab, the map, audio clips, UI canvases, and a NavMesh. All catalogued with owners in
**[ASSETS.md](ASSETS.md)**. Until those exist, the project compiles and the data assets generate, but
a full match isn't playable yet.

### Two known install gaps (intentional)
1. **Netcode for GameObjects is not in the manifest.** The network layer is behind `#if NETCODE_PRESENT`
   and activates automatically once you add `com.unity.netcode.gameobjects`. Offline + bots work without it.
2. **The input asset lacks Reload/ADS/Ability bindings.** `PlayerInputBinder` falls back to
   `R / RightMouse / Q / E`. Adding the actions to `InputSystem_Actions` is cleaner.

---

## Code layout (`Assets/_Game/`)

```
Shared/        Enums, constants, events hub, damage structs, effect system, settings
Characters/    PlayerController + movement/camera/weapon/health/momentum, abilities (16 impls)
Weapons/       WeaponController + WeaponDataSO
Gameplay/      Objectives (capture zones, game mode, stats), momentum, map/spawns, bots
UI/            HUD widgets, combat feedback, menus
Audio/         Pooled AudioManager, library, footsteps, music, ambient
Progression/   XP, battle pass, daily challenges, save
Network/       NGO mirror components (compile only with NETCODE_PRESENT)
Editor/        Data-asset generators + validators
```

Everything communicates through the static **`GameEvents`** hub; gameplay numbers live on
**ScriptableObjects** (generated via the `Carrot Clash` editor menu), never hardcoded.

---

## Contributing

- Integration branch is **`release`** (treated as main here). Branch off it, PR back.
- **Git LFS is required** - see [DEVENV.md](DEVENV.md). Cloning without it gives broken asset stubs.
- Commit Unity `.meta` files alongside their assets.
- Keep the C# consistent with [CONTRACTS.md](Assets/_Game/CONTRACTS.md); update that doc when the API changes.

---

## Quick first run

```bash
git clone https://github.com/haptixxx-dev/Carrot-Clash.git
cd Carrot-Clash
git lfs pull
```
Then: open in Unity Hub (**6000.4.10f1**) → wait for import → Console should be **0 errors** →
**`Carrot Clash → Generate Default Data Assets`** → follow **[SETUP.md](Assets/_Game/SETUP.md)**.
