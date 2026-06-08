# Editor setup & wiring

Editor wiring and smoke-test guide for the gameplay code drop.

::: info Source of truth
This page mirrors [`Assets/_Game/SETUP.md`](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/Assets/_Game/SETUP.md) in the repo. If the two ever disagree, the in-repo `SETUP.md` wins. Update it first, then reconcile here.
:::

The gameplay code was written **without a Unity editor** (generated against the frozen API in [`CONTRACTS.md`](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/Assets/_Game/CONTRACTS.md)), so **nothing has been opened in the editor yet**. The 97 C# scripts compile and are verified, but there are **no scenes, prefabs, art, or audio clips** yet. Your job: install the missing packages, wire the scenes and prefabs, then confirm it compiles and runs.

::: warning Engine version
Open the project in **Unity 6000.4.10f1** (Unity 6.4), URP. Use exactly this version. It is pinned.
:::

Status legend used below:

- <span class="cc-status built">Implemented</span>: code exists and is verified
- <span class="cc-status partial">Partial</span>: code exists but needs editor wiring/assets
- <span class="cc-status pending">Editor-pending</span>: needs editor-authored content (scenes/prefabs/art/audio)

---

## 1. Install packages

<span class="cc-status partial">Partial</span>: network code is written but the packages are not installed.

Two systems are referenced in code but **not yet in `Packages/manifest.json`**. The code is written so the project **still compiles without them**. The network layer stays dormant behind a `#if NETCODE_PRESENT` flag. Install only when you're ready for multiplayer.

**Window → Package Manager → "+" → Add package by name:**

| Package | Name to paste | Needed for |
|---|---|---|
| Netcode for GameObjects | `com.unity.netcode.gameobjects` | Online multiplayer (`Assets/_Game/Network/` lights up automatically once installed) |
| Relay *(optional)* | `com.unity.services.relay` | NAT traversal / join codes |
| Lobby *(optional)* | `com.unity.services.lobby` | Room browser |
| Authentication *(optional)* | `com.unity.services.authentication` | Required by Relay/Lobby |

Already present and used: **Input System, URP, AI Navigation** (NavMesh, for bots), **TextMeshPro, uGUI**.

::: tip Offline works with zero installs
You can do **everything except online play** with no package installs at all. Offline plus bots works as-is.
:::

---

## 2. First-time editor setup (do this once)

### a. Layers

<span class="cc-status pending">Editor-pending</span>

Code raycasts against named layers. Go to **Edit → Project Settings → Tags and Layers** and create these layers if missing (exact spelling):

- `Player`
- `Environment`
- `Ability`
- `Hitbox`

Set physics collisions sanely (Player vs Environment, Hitbox on player bodies). The capture/spawn logic uses `OverlapSphere` on the `Player` layer, so make sure player prefabs are on it.

### b. Generate data assets (one click)

<span class="cc-status built">Implemented</span>: generator menu exists; the assets it produces are committed once generated.

The four classes, five weapons, sixteen abilities, and momentum config are generated from the docs (so the numbers match the GDD) and committed to the repo.

1. **Top menu → `Carrot Clash → Generate Default Data Assets`.**
2. Then **`Carrot Clash → Validate Data`** and check the Console for warnings.

This creates the `CharacterDataSO` / `WeaponDataSO` / `AbilityDataSO` / `MomentumConfigSO` assets under `Assets/_Game/.../Data/`.

### c. Player prefab

<span class="cc-status pending">Editor-pending</span>: all component scripts exist; the prefab itself must be assembled.

There is no player prefab yet. Build one to match the "Player Prefab Structure" in [/tech-architecture](/tech-architecture):

- Root with `CharacterController` + `PlayerController` + `PlayerMovement` + `WeaponController` + `AbilityController` + `HealthController` + `MomentumController`.
- A `CameraRig` child with the main `Camera`; assign it on `PlayerCamera` (local player only).
- Head and body colliders with `PlayerHitbox` (tick **Is Head** on the head one).
- Put the root on the **Player** layer.
- Assign `abilityOrigin1` / `abilityOrigin2` and `headTransform` transforms.
- Drag the Carrot `CharacterDataSO` and the `MomentumConfigSO` onto `PlayerController`.

::: tip Bots & remote players
`cam` is **null** on bots and remote players. The code already null-checks it, so a prefab without a camera assigned is fine for non-local instances.
:::

### d. Audio

<span class="cc-status partial">Partial</span>: `AudioManager` + library code is built; clips are editor-pending.

- Create an `AudioManager` GameObject (add the `AudioManager` component) in the **Boot** scene.
- Create an `AudioLibrary` asset (`Create → Carrot Clash → Audio Library`), and add clips keyed per the list at the bottom of [`CONTRACTS.md`](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/Assets/_Game/CONTRACTS.md). Missing keys just no-op, so it's safe to leave gaps early.
- Optional: an `AudioMixer` with exposed params `MasterVolume`, `MusicVolume`, `SfxVolume`, `VoiceVolume`.

### e. Scenes

<span class="cc-status pending">Editor-pending</span>: all scenes must be authored.

Create/confirm these scenes, named exactly: `Boot`, `MainMenu`, `Gameplay_Market`, plus `GameplayUI` (loaded additively). Add them to **Build Settings** in that order. Put `GameBootstrap` in `Boot`.

### f. Input (heads-up)

<span class="cc-status partial">Partial</span>: works via fallback keys; the input asset is incomplete.

::: warning Missing input actions
`Assets/InputSystem_Actions.inputactions` currently only has **Move / Look / Attack / Sprint / Jump / Crouch**. It is **missing Reload / ADS / Ability1 / Ability2 / SwapWeapon**.

`PlayerInputBinder` has a keyboard/mouse fallback for those (**R / RightMouse / Q / E**), so it works, but it's cleaner to add those actions to the asset.
:::

---

## 3. Manual smoke checks (in order)

Tick these off. If one fails, stop and report which.

- [ ] **Compiles.** Open project → Console has **0 errors** (warnings about missing prefab refs are fine).
- [ ] **Data generates.** `Carrot Clash → Generate Default Data Assets` runs, assets appear, `Validate Data` is clean.
- [ ] **Player moves.** Drop the player prefab in `Gameplay_Market`, press Play: WASD, mouse look, jump, sprint, and crouch all work.
- [ ] **Shooting works.** Add a `TargetDummy` (on a Player-layer collider with `PlayerHitbox`): fire hits it, hit marker shows, it dies and resets after ~3s.
- [ ] **Momentum tiers.** Kill 3 dummies without dying → ring fills tier 1→2→3, "ON FIRE" overlay first time, then watch it decay after ~12s idle.
- [ ] **Headshots.** Shooting the head hitbox does more damage than the body (1.5× on most weapons).
- [ ] **Abilities fire.** Press Q / E as Carrot → Sprint Dash moves you, Radar Pulse pings. Cooldown overlay drains on the HUD slots.
- [ ] **Capture zone.** Stand in a `CaptureZone` → progress ring fills, score ticks up on the HUD score bar.
- [ ] **Match flow.** With a `GameModeManager` + `MatchInitializer` in the scene: ClassSelect → Countdown → MatchActive runs; Zone C unlock fires at 5:00; match ends on score cap or timer; post-match screen shows.
- [ ] **Bots.** `BotSpawner` fills empty slots → bots walk to objectives and shoot. (Bake a NavMesh first: **AI → bake**.)
- [ ] **Settings persist.** Change FOV / sensitivity / volume in the settings menu → quit → relaunch → values stuck.
- [ ] **Reduce Motion.** Toggle it in Accessibility → screen shake and camera bob stop.
- [ ] *(only if NGO installed)* **Two clients.** Build + run two instances, host + join → both see synced score, momentum, kills.

---

## 4. Architecture in 30 seconds

For the full picture see [/dev/architecture](/dev/architecture) and [/tech-architecture](/tech-architecture). In brief:

- [`CONTRACTS.md`](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/Assets/_Game/CONTRACTS.md) is the **frozen public API**. If you change a spine signature, update that file.
- Everything talks through the static `GameEvents` hub (kills, score, tiers, zones); UI, audio, and progression just subscribe.
- Data lives on ScriptableObjects (step 2b). Code never hardcodes class/weapon stats.
- `PlayerController` is the orchestrator; subsystems hang off it. `cam` is **null** on bots/remote players, and the code already null-checks it.
- Network files mirror the offline logic and only compile with `NETCODE_PRESENT`. **Offline logic is the source of truth.**

::: details Where to go next
- [/dev/environment](/dev/environment) (dev environment setup)
- [/dev/assets](/dev/assets) (the editor-pending asset checklist)
- [/dev/status](/dev/status) (implementation status across all systems)
:::

Once the checklist passes, we ship.
