# Getting started (engineers)

Welcome to **Carrot Clash**. This is the landing page for engineers and new contributors.
If you read only one page first, read this one, then follow the links.

::: info Source of truth
These `/dev` pages are VitePress-friendly summaries. The canonical engineering docs live at the
repo root: [`README.md`](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/README.md),
[`DEVENV.md`](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/DEVENV.md),
[`ASSETS.md`](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/ASSETS.md),
[`Assets/_Game/SETUP.md`](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/Assets/_Game/SETUP.md),
and [`Assets/_Game/CONTRACTS.md`](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/Assets/_Game/CONTRACTS.md).
If a `/dev` page ever disagrees with those files, the root docs win.
:::

## What this project is

Carrot Clash is a fast-paced **3v3 / 4v4 team FPS** set in a stylized food-market world. Four vegetable
hero classes fight over a three-zone objective map. The defining mechanic is a **momentum/combo system**.
Kills escalate your power across three tiers, but death transfers that charge to your killer, so every
engagement is a risk-reward bet. The engine is **Unity 6000.4.10f1** (URP). Matches run 8 minutes, first
to 500 points or highest at the timer.

## Current status

<span class="cc-status built">Implemented</span> **Code: complete (vertical-slice scope).**
97 C# scripts under `Assets/_Game/` implement the player core, momentum system, all 4 classes plus 16
abilities, objectives, match flow, the full HUD, menus, bots, audio, and progression. They were built
contract-first and verified by cross-file and per-file review.

<span class="cc-status pending">Editor-pending</span> **Content can't be created from source alone.**
No scenes, player prefab, map (grey-box, art, NavMesh), UI canvases, or audio/art assets exist yet.
Until those are authored in the editor, the project compiles and the data assets generate, but a full
match is **not yet playable**. Everything outstanding is catalogued, with owners, in the
[Asset Checklist](/dev/assets).

## Two intentional install gaps

Both are deliberate and documented. Don't "fix" them without reading the relevant page first.

1. **Netcode for GameObjects is not in the manifest.** The network layer sits behind
   `#if NETCODE_PRESENT` and activates automatically once you add `com.unity.netcode.gameobjects`
   (Relay/Lobby likewise). Offline play and bots work without it. See [Editor Setup & Wiring](/dev/editor-setup).
2. **The input asset lacks Reload/ADS/Ability1/Ability2/SwapWeapon actions.** `PlayerInputBinder`
   falls back to `R / RightMouse / Q / E`. Adding those actions to `InputSystem_Actions` is cleaner
   but optional. See [Code Architecture](/dev/architecture).

## Doc-chain reading order

Read these in order. Each one assumes the previous.

1. [Environment Setup](/dev/environment): set up a blank machine with Unity, Git LFS, IDE, .NET, and Node.
2. [Editor Setup & Wiring](/dev/editor-setup): open the project, install NGO, generate data, wire scenes/prefabs, run the smoke test.
3. [Code Architecture](/dev/architecture): how the code is laid out, the `GameEvents` hub, ScriptableObject data, and the frozen API.
4. [Asset Checklist](/dev/assets): the exhaustive list of editor-authored assets still needed, with owners.
5. [Implementation Status](/dev/status): what's built vs. partial vs. editor-pending, cross-checked against the code.

For game design (mechanics, classes, weapons, map, momentum), start at the [GDD](/gdd).

## New here? Day-1 path

1. **Set up your machine.** Follow [Environment Setup](/dev/environment) (Unity 6000.4.10f1, Git LFS, IDE, .NET, Node).
2. **Open the project.** Import in Unity Hub, wait for it to finish, and confirm the Console shows **0 errors**.
3. **Generate data assets.** Run **`Carrot Clash → Generate Default Data Assets`** from the editor menu if the `.asset` files aren't already in your checkout (the first run bootstraps them; afterwards they're committed).
4. **Read the architecture.** Skim [Code Architecture](/dev/architecture) so you know where things live and how `GameEvents` ties them together.
5. **Pick a task.** Grab an unclaimed item from the [Asset Checklist](/dev/assets) and check [Implementation Status](/dev/status) for context.

::: tip Contributing
Integration branch is **`release`** (treated as main). Branch off it, PR back. **Git LFS is required**.
Cloning without it gives broken asset stubs. Commit Unity `.meta` files alongside their assets, and keep
the C# consistent with the [frozen contracts](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/Assets/_Game/CONTRACTS.md).
:::
