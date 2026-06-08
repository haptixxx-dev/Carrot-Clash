# Overview

<span class="cc-status partial">Partial</span> The systems described across this GDD are **code-complete and verified** (97 C# scripts under `Assets/_Game/`, namespace `CarrotClash`) but **not yet playable**: scenes, the player prefab, the map grey-box/art, UI canvases, and all audio/VFX are still editor-pending. Each system page carries its own status callout. Engineers should start at [Getting Started](/dev/getting-started); the frozen API lives in `Assets/_Game/CONTRACTS.md`.

::: info Design vs. code
This doc captures **design intent** and is unchanged in spirit. The only ground-truth deltas worth flagging at the overview level: the four classes ship as `ClassId.Carrot / Jalapeno / Broccoli / Potato` and the momentum meter resolves to four tiers (`Cold → Warm → Hot → OnFire`, max tier 3) in code — matching the design below. Networking (Netcode for GameObjects) is **not installed yet** and sits dormant behind a `NETCODE_PRESENT` compile flag.
:::

## Concept

Carrot Clash is a fast-paced team FPS where squads of vegetable-themed characters fight over objectives in a stylized food-world environment. The game rewards aggression: killing enemies builds a momentum meter that amplifies your movement, abilities, and damage — making hot streaks dangerous and hunting them down even more rewarding.

## Design Pillars

1. **Momentum over camping** — the momentum system mechanically punishes passivity and rewards forward aggression
2. **Class synergy** — four distinct classes that combo well together, encouraging team composition decisions
3. **Readable chaos** — fast-paced but legible; player abilities and map layout keep fights comprehensible
4. **Theme-first identity** — the food/vegetable theme is mechanical, not just cosmetic (abilities, map, sound all reinforce it)

## Inspirations

| Game | What we take from it |
|---|---|
| **Apex Legends** | Class-based hero abilities, fast movement, team synergy |
| **The Finals** | Environmental interactivity, aggressive reward loops |
| **Valorant** | Ability + gunplay balance, objective clarity |
| **Overwatch** | Character readability, role identity |

## Target Audience

- Players who enjoy hero shooters but want faster TTK and less ability dominance
- Casual-to-mid-skill FPS players who like character variety
- Players drawn to stylized, non-realistic aesthetics
