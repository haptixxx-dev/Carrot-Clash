# Developer environment setup

Everything you need to clone, open, build, and contribute to Carrot Clash, starting from a blank machine. Covers Windows, macOS, and Arch Linux. Follow your OS section top to bottom.

::: info SOURCE OF TRUTH
This page mirrors [`/DEVENV.md`](https://github.com/haptixxx-dev/Carrot-Clash/blob/release/DEVENV.md) at the repo root, which is the canonical version. If the two ever disagree, the repo-root file wins. Related: [Editor Setup & Wiring](/dev/editor-setup) and [Code Architecture](/dev/architecture).
:::

::: warning PINNED VERSIONS: DO NOT DEVIATE
- **Unity `6000.4.10f1`** (Unity 6.4). The project will refuse to open cleanly in another version. If Unity Hub offers to "upgrade", **decline**.
- **Node.js 20 LTS**, only needed if you touch the design docs site (VitePress).
- **.NET SDK 8** for IDE IntelliSense / Roslyn. Unity ships its own compiler but IDEs want this.
:::

::: warning GIT LFS IS REQUIRED
**Git LFS must be installed and active before you clone.** Models, audio, video, and textures are stored in LFS (73 binary file types). Cloning without it leaves you with broken ~130-byte pointer stubs instead of real assets: textures go pink, models vanish, `.dll`s won't load. Run `git lfs install` once per machine, then `git lfs pull` after cloning.
:::

---

## 0. TL;DR: what gets installed

| Tool | Why | Required? |
|---|---|---|
| **Unity Hub** | Installs/launches the editor | Required |
| **Unity Editor 6000.4.10f1** + modules | The engine | Required |
| **Git** | Version control | Required |
| **Git LFS** | Large binary assets | **Required** |
| **An IDE** (Rider / VS / VS Code) | Edit C#, IntelliSense, debug | Required (pick one) |
| **.NET SDK 8** | Roslyn/IntelliSense backend | Required for VS Code/Rider |
| **Node.js 20 + npm** | Build the docs site | Only if editing `/docs` |
| **(later) Netcode for GameObjects** | Online multiplayer | When doing netcode (in-editor via Package Manager, see [Editor Setup](/dev/editor-setup)) |

::: tip Netcode is dormant
Netcode for GameObjects (and Relay/Lobby) is **not installed yet**. The network layer lives behind a `NETCODE_PRESENT` compile flag and stays dormant until you add the package. You don't need it to build or run the single-machine project.
:::

---

## 1. Windows

### 1.1 Install a package manager (recommended)

Open **PowerShell as Administrator**. `winget` ships with Windows 10/11. Verify:

```powershell
winget --version
```

### 1.2 Git + Git LFS

```powershell
winget install --id Git.Git -e
winget install --id GitHub.GitLFS -e
```

Then **once per machine**:

```powershell
git lfs install
```

Configure line endings for the cross-platform team. Unity YAML wants LF and `.gitattributes` handles that, but set the safe default anyway:

```powershell
git config --global core.autocrlf input
git config --global core.longpaths true   # Unity asset paths get long
```

### 1.3 Unity Hub + Editor

```powershell
winget install --id Unity.UnityHub -e
```

Open **Unity Hub → Installs → Install Editor → "Archive" / "Download Archive"** and pick **`6000.4.10f1`**. If it's not listed, get the exact installer from <https://unity.com/releases/editor/archive> (search `6000.4.10`).

**Modules to tick during install:**

- **Microsoft Visual Studio Community** *(or skip if using Rider/VS Code)*
- **Windows Build Support (IL2CPP)**
- **Android Build Support** + **OpenJDK** + **Android SDK & NDK Tools** *(mobile is a secondary target, install it)*
- **Documentation** *(optional but handy)*

### 1.4 IDE (pick one)

```powershell
# Option A: Rider (best Unity support, paid/student-free)
winget install --id JetBrains.Rider -e
# Option B: VS Code (free)
winget install --id Microsoft.VisualStudioCode -e
# Option C: Visual Studio Community (free) - installable via Unity Hub module above
```

### 1.5 .NET SDK 8 (for VS Code / Rider IntelliSense)

```powershell
winget install --id Microsoft.DotNet.SDK.8 -e
```

### 1.6 Node.js 20 (only if editing docs)

```powershell
winget install --id OpenJS.NodeJS.LTS -e
```

---

## 2. macOS

### 2.1 Install Homebrew

```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

Follow the post-install note to add `brew` to your PATH (Apple Silicon: `/opt/homebrew/bin`).

### 2.2 Git + Git LFS

macOS ships Git, but install a current one plus LFS:

```bash
brew install git git-lfs
git lfs install
git config --global core.autocrlf input
```

### 2.3 Unity Hub + Editor

```bash
brew install --cask unity-hub
```

Open **Unity Hub → Installs → Install Editor → `6000.4.10f1`** (use the Archive link if not listed: <https://unity.com/releases/editor/archive>).

**Modules to tick:**

- **Mac Build Support (IL2CPP)** *(usually preselected)*
- **iOS Build Support** *(if testing the mobile target on iPhone)*
- **Android Build Support** + **OpenJDK** + **Android SDK & NDK Tools** *(mobile secondary target)*
- **Visual Studio for Mac is discontinued. Use Rider or VS Code below.**

::: tip Apple Silicon (M-series)
Install the **Apple silicon** editor build, not the Intel one.
:::

### 2.4 IDE + .NET SDK

```bash
brew install --cask rider           # recommended for Unity
# or
brew install --cask visual-studio-code
brew install --cask dotnet-sdk      # .NET 8 SDK for IntelliSense
```

### 2.5 Node.js 20 (only if editing docs)

```bash
brew install node@20
brew link --overwrite --force node@20
```

---

## 3. Arch Linux

::: warning Unity on Linux is rougher
Unity on Linux is supported but less polished than Windows/macOS. Use Unity Hub from the AUR. Arch ships rolling/newer packages, so pin Node if needed.
:::

### 3.1 Base tools + an AUR helper

```bash
sudo pacman -Syu --needed base-devel git git-lfs
git lfs install
git config --global core.autocrlf input

# AUR helper (yay) if you don't have one:
sudo pacman -S --needed go
git clone https://aur.archlinux.org/yay.git /tmp/yay && (cd /tmp/yay && makepkg -si)
```

### 3.2 Unity Hub + Editor

```bash
yay -S unityhub
```

Launch `unityhub`, sign in, then **Installs → Install Editor → `6000.4.10f1`** (Archive link if missing: <https://unity.com/releases/editor/archive>, pick the **Linux** build).

**Modules to tick:**

- **Linux Build Support (IL2CPP)** + **Linux Build Support (Mono)**
- **Android Build Support** + **OpenJDK** + **Android SDK & NDK Tools** *(mobile secondary target)*
- **WebGL Build Support** *(optional)*

**Known Linux needs:** if the editor fails to launch, install common deps:

```bash
sudo pacman -S --needed gtk3 nss libgudev libxss alsa-lib   # editor/runtime libs
```

### 3.3 IDE + .NET SDK

```bash
sudo pacman -S --needed dotnet-sdk        # .NET 8 SDK
# IDE - pick one:
yay -S rider                              # JetBrains Rider (best Unity UX)
sudo pacman -S --needed code              # VS Code (OSS build) - or 'visual-studio-code-bin' from AUR for MS build
```

### 3.4 Node.js 20 (only if editing docs)

```bash
sudo pacman -S --needed nodejs npm        # Arch ships current; if you need exactly 20:
# yay -S nvm  &&  nvm install 20  &&  nvm use 20
```

---

## 4. Clone the repo (all platforms)

```bash
# HTTPS
git clone https://github.com/haptixxx-dev/Carrot-Clash.git
# or SSH (set up your key on GitHub first)
git clone git@github.com:haptixxx-dev/Carrot-Clash.git

cd Carrot-Clash
git lfs pull        # pull the actual binary assets (skip and you get pointer stubs)
```

Verify LFS worked. This should print real file sizes, not ~130-byte pointers:

```bash
git lfs ls-files | head
```

---

## 5. Open the project

1. **Unity Hub → Open → Add project from disk →** select the `Carrot-Clash` folder.
2. Hub flags the editor version. Make sure it resolves to **6000.4.10f1**. If Hub offers to "upgrade", **decline** and install the exact version instead.
3. First open is slow: Unity imports all assets and compiles scripts. Wait for the spinner to finish.
4. **Console must show 0 errors.** Warnings about unassigned prefab refs are expected until you wire prefabs (see [Editor Setup & Wiring](/dev/editor-setup)).

Set your IDE: **Edit → Preferences → External Tools → External Script Editor** → pick Rider / VS Code / VS.

---

## 6. Generate gameplay data (one click)

The game's data assets (classes, weapons, abilities, momentum config) are generated from the design docs, then committed to the repo. The first person to run the generator commits the `.asset` files. After the project opens clean:

- **Top menu → `Carrot Clash → Generate Default Data Assets`**
- then **`Carrot Clash → Validate Data`** (check Console)

Full editor wiring (layers, player prefab, scenes, audio) is covered in [Editor Setup & Wiring](/dev/editor-setup). Do that next.

---

## 7. Docs site (optional, only if editing `/docs`)

The design docs are this VitePress site, deployed to GitHub Pages (cc.haptixxx.dev) via CI on push to `main`/`release`.

```bash
npm install            # in repo root, installs vitepress
npm run docs:dev       # local preview at http://localhost:5173
npm run docs:build     # production build into docs/.vitepress/dist
```

---

## 8. Git workflow

- Default/integration branch is **`release`** (this repo treats it as main).
- Branch for your work, PR back. Don't commit straight to `release` unless told.
- **Never commit binary assets without LFS active.** Run `git lfs status` before pushing if unsure.
- Unity meta files **must** be committed alongside their assets (don't `.gitignore` them).

---

## 9. Troubleshooting

| Symptom | Fix |
|---|---|
| Assets look broken / textures pink / models missing | `git lfs install` then `git lfs pull`; you cloned without LFS |
| Hub wants to upgrade the editor version | Decline; install exact `6000.4.10f1` from the Archive |
| Console: hundreds of compile errors on first open | Wrong editor version, or LFS pointers instead of real `.dll`/assets; re-check steps 1.2 / 4 |
| IntelliSense dead in VS Code | Install **.NET SDK 8** + the **C# / Unity** VS Code extensions; reopen the `.sln` Unity generates |
| Network/multiplayer scripts greyed out | Expected; NGO isn't installed yet (see [Editor Setup](/dev/editor-setup)) |
| Reload/ADS/ability keys don't bind | Input asset lacks those actions; `PlayerInputBinder` falls back to R / RightMouse / Q / E |
| `git lfs` command not found (Arch) | `sudo pacman -S git-lfs && git lfs install` |
| Long-path errors on Windows clone | `git config --global core.longpaths true` then re-clone |

---

## 10. Readiness checklist

Run these to confirm your toolchain is ready:

```bash
git --version          # any recent
git lfs version        # prints a version (not "command not found")
dotnet --version       # 8.x
node --version         # v20.x   (only if doing docs)
```

- [ ] Unity Hub lists **6000.4.10f1** under Installs.
- [ ] Project opens with **0 Console errors** after import.
- [ ] `Carrot Clash → Generate Default Data Assets` runs without exceptions.

You're set. Next: [Editor Setup & Wiring](/dev/editor-setup) for editor wiring plus the smoke-test checklist.
