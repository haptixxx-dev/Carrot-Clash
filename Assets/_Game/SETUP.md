# Carrot Clash — Setup & Verification (Intern Guide)

This is the gameplay code drop. It was written without a Unity editor (LLM-generated against a frozen
API contract in `CONTRACTS.md`), so **nothing has been opened in the editor yet**. Your job: install
the missing packages, wire up the scenes/prefabs, and confirm it compiles + runs.

Engine: **Unity 6000.4.10f1** (Unity 6.4), URP. Open the project in exactly this version.

---

## 1. Install packages

Two systems are referenced in code but **not yet in `Packages/manifest.json`**. The code is written
so the project **still compiles without them** (network code is behind a `#if NETCODE_PRESENT` flag).
Install when you're ready for multiplayer.

**Window → Package Manager → "+" → Add package by name:**

| Package | Name to paste | Needed for |
|---|---|---|
| Netcode for GameObjects | `com.unity.netcode.gameobjects` | Online multiplayer (the `Assets/_Game/Network/` files light up automatically once installed) |
| Relay *(optional)* | `com.unity.services.relay` | NAT traversal / join codes |
| Lobby *(optional)* | `com.unity.services.lobby` | Room browser |
| Authentication *(optional)* | `com.unity.services.authentication` | Required by Relay/Lobby |

Already present and used: Input System, URP, AI Navigation (NavMesh, for bots), TextMeshPro, uGUI.

> You can do **everything except online play** with zero package installs. Offline + bots works as-is.

---

## 2. First-time editor setup (do this once)

### a. Layers
Code raycasts against named layers. **Edit → Project Settings → Tags and Layers** — create these
layers if missing (exact spelling):
- `Player`
- `Environment`
- `Ability`
- `Hitbox`

Set physics collisions sanely (Player vs Environment, Hitbox on player bodies). The capture/spawn
logic uses `OverlapSphere` on the `Player` layer — make sure player prefabs are on it.

### b. Generate data assets (one click)
The four classes, five weapons, sixteen abilities, and momentum config are **not committed as assets** —
they're generated from the docs so the numbers always match the GDD.

**Top menu → `Carrot Clash → Generate Default Data Assets`.**
Then **`Carrot Clash → Validate Data`** and check the Console for warnings.

This creates the `CharacterDataSO` / `WeaponDataSO` / `AbilityDataSO` / `MomentumConfigSO` assets under
`Assets/_Game/.../Data/`.

### c. Player prefab
There is no player prefab yet — build one to match the structure in `docs/tech-architecture.md`
("Player Prefab Structure"):
- Root with `CharacterController` + `PlayerController` + `PlayerMovement` + `WeaponController` +
  `AbilityController` + `HealthController` + `MomentumController`.
- A `CameraRig` child with the main `Camera`; assign it on `PlayerCamera` (local player only).
- Head + body colliders with `PlayerHitbox` (tick **Is Head** on the head one).
- Put the root on the **Player** layer.
- Assign `abilityOrigin1/2` and `headTransform` transforms.
- Drag the Carrot `CharacterDataSO` + the `MomentumConfigSO` onto `PlayerController`.

### d. Audio
- Create an `AudioManager` GameObject (add `AudioManager` component) in the Boot scene.
- Create an `AudioLibrary` asset (`Create → Carrot Clash → Audio Library`), add clips keyed per the
  list at the bottom of `CONTRACTS.md` (missing keys just no-op — safe to leave gaps early).
- Optional: an `AudioMixer` with exposed params `MasterVolume`, `MusicVolume`, `SfxVolume`, `VoiceVolume`.

### e. Scenes
Create/confirm three scenes named exactly: `Boot`, `MainMenu`, `Gameplay_Market`, plus `GameplayUI`
(loaded additively). Add them to **Build Settings** in that order. Put `GameBootstrap` in `Boot`.

### f. Input (heads-up)
`Assets/InputSystem_Actions.inputactions` currently only has Move/Look/Attack/Sprint/Jump/Crouch.
It's **missing Reload / ADS / Ability1 / Ability2 / SwapWeapon**. `PlayerInputBinder` has a keyboard/mouse
fallback for those (R / RightMouse / Q / E), so it works — but cleaner to add those actions to the asset.

---

## 3. Manual smoke checks (in order)

Tick these off. If one fails, stop and report which.

- [ ] **Compiles.** Open project → Console has **0 errors** (warnings about missing prefab refs are fine).
- [ ] **Data generates.** `Carrot Clash → Generate Default Data Assets` runs, assets appear, `Validate Data` is clean.
- [ ] **Player moves.** Drop the player prefab in `Gameplay_Market`, press Play: WASD + mouse look + jump + sprint + crouch all work.
- [ ] **Shooting works.** Add a `TargetDummy` (on a Player-layer collider with `PlayerHitbox`): fire hits it, hit marker shows, it dies and resets after ~3s.
- [ ] **Momentum tiers.** Kill 3 dummies without dying → ring fills tier 1→2→3, "ON FIRE" overlay first time, then watch it decay after ~12s idle.
- [ ] **Headshots.** Shooting the head hitbox does more damage than the body (1.5× on most weapons).
- [ ] **Abilities fire.** Press Q / E as Carrot → Sprint Dash moves you, Radar Pulse pings. Cooldown overlay drains on the HUD slots.
- [ ] **Capture zone.** Stand in a `CaptureZone` → progress ring fills, score ticks up on the HUD score bar.
- [ ] **Match flow.** With a `GameModeManager` + `MatchInitializer` in the scene: ClassSelect → Countdown → MatchActive runs; Zone C unlock fires at 5:00; match ends on score cap or timer; post-match screen shows.
- [ ] **Bots.** `BotSpawner` fills empty slots → bots walk to objectives and shoot. (Bake a NavMesh first: **AI → bake**.)
- [ ] **Settings persist.** Change FOV / sensitivity / volume in the settings menu → quit → relaunch → values stuck.
- [ ] **Reduce Motion.** Toggle it in Accessibility → screen shake + camera bob stop.
- [ ] *(only if NGO installed)* **Two clients.** Build + run two instances, host + join → both see synced score, momentum, kills.

---

## 4. Architecture in 30 seconds

- `Assets/_Game/CONTRACTS.md` = the frozen public API. **If you change a spine signature, update that file.**
- Everything talks through the static `GameEvents` hub (kills, score, tiers, zones) — UI/audio/progression just subscribe.
- Data lives on ScriptableObjects (step 2b). Code never hardcodes class/weapon stats.
- `PlayerController` is the orchestrator; subsystems hang off it. `cam` is **null** on bots/remote players — code already null-checks it.
- Network files mirror the offline logic and only compile with `NETCODE_PRESENT`. Offline logic is the source of truth.

Questions → ping the author. Once the checklist passes, we ship.
