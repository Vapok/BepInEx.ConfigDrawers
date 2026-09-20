# BepInEx.ConfigDrawers

<p align="center">
  <img src="https://raw.githubusercontent.com/Vapok/BepInEx.ConfigDrawers/main/icon.png" width="128" height="128" alt="BepInEx.ConfigDrawers Logo">
</p>

<p align="center">
  <strong>Next-Generation In-Game Configuration Manager for BepInEx 5</strong>
</p>

<p align="center">
  <a href="https://github.com/Vapok/BepInEx.ConfigDrawers/releases"><img src="https://img.shields.io/github/v/release/Vapok/BepInEx.ConfigDrawers?include_prereleases&style=flat-square" alt="GitHub Release"></a>
  <a href="https://thunderstore.io/c/valheim/p/Vapok/BepInEx.ConfigDrawers/"><img src="https://img.shields.io/thunderstore/v/Vapok/BepInEx.ConfigDrawers?style=flat-square" alt="Thunderstore Version"></a>
  <a href="https://discord.gg/vapok"><img src="https://img.shields.io/discord/941785535977934898?label=Discord&logo=discord&style=flat-square" alt="Discord"></a>
  <a href="LICENSE.md"><img src="https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square" alt="License"></a>
</p>

---

## Overview

**BepInEx.ConfigDrawers** is a modern, lightweight, high-performance in-game Configuration Manager built from the ground up to replace dated configuration managers.

Featuring a sleek **Cyber-Console UI** with neon ice-blue accents, deep obsidian containers, and crisp monospace typography, `BepInEx.ConfigDrawers` slides out effortlessly from the screen edge on demand. It keeps your game world completely visible while giving you precision control over all your installed mods.

---

## Key Features

- **Smooth Sliding Drawer**: Slides cleanly from the monitor edge. Occupies less than 25% of your screen width, leaving your gameplay and menus visible.
- **Magnetic Docking & Free Float**: Snap to the left rail, snap to the right rail, or detach and drag freely across the screen with interactive window resize handles.
- **Auto-Expanding Multiline Editor**: Long strings, lists, formatting tokens, and JSON payloads expand dynamically as you type without clipping or awkward tiny text boxes.
- **Crisp Monospace Typography**: Built with embedded **Hack** font rendering with Signed Distance Fields (SDF) for ultra-sharp legibility at any resolution.
- **Cyber Tooltips**: Interactive tooltips across all header buttons, dock modes, scale presets, and setting status indicators.
- **Precision Color Picker**: Full HSV spectrum picker with real-time swatch preview, manual Hex/RGB inputs, and preset palette swatches.
- **Smooth Numeric Sliders**: Smooth drag sliders with direct numeric input boxes for surgical precision.
- **First-Class Data Grids & Tables**: Dedicated visual multi-column tables for complex structures like crafting recipes, upgrade costs, and drop tables.
- **Live UI Scaling**: Switch dynamically between **Small**, **Normal**, and **Large** font and layout scales (`[ Size: Norm ]`) to comfortably fit 1080p, 1440p, or 4K monitors.
- **Buffered Input Safety**: No more lost keystrokes or clobbered fields. Full backspace and cursor navigation with clear commit (`Enter` / defocus) and cancel (`Escape`).
- **Interactive Hotkey Rebinding**: Click `[ F1 ]` (or your configured bind) in the header, press any key on your keyboard, and your new toggle key is instantly active.
- **Automatic Legacy Suppression**: Gracefully suppresses older IMGUI `ConfigurationManager.dll` hotkeys to eliminate conflicting dual windows while continuing to render their custom drawers inside the modern drawer.
- **Zero Bloat**: Single-file assembly with embedded resources, zero telemetry, and zero background performance overhead.

---

## Controls & Usage

| Action | Default Input | Description |
| :--- | :--- | :--- |
| **Toggle Drawer** | `F1` | Opens or closes the ConfigDrawers menu. |
| **Close Drawer** | `Escape` | Closes the drawer (or cancels an active text field edit). |
| **Commit Edit** | `Enter` / Defocus | Saves and applies the edited value back to the config file. |
| **Dock Left / Right** | `[ Left ]` / `[ Right ]` | Snaps the drawer to the left or right monitor rail. |
| **Detach / Float** | `[ Float ]` | Detaches the drawer into a free-floating, draggable window. |
| **Resize Drawer** | Drag Edge Handle | Click and drag the inner border handle to resize the drawer width. |
| **Cycle UI Scale** | `[ Size: Norm ]` | Toggles between Small (85%), Normal (95%), and Large (108%) scaling. |
| **Rebind Hotkey** | `[ <Key> ]` | Click the hotkey button in the header and press any key to rebind. |

---

## Mod Compatibility & Ecosystem

`BepInEx.ConfigDrawers` provides 100% ecosystem compatibility:
- **ServerSync Ready**: Automatically reflects locked server settings with dedicated sync status icons and admin-only safeguards.
- **Legacy Drawer Bridge**: Seamlessly bridges legacy IMGUI custom drawers into responsive uGUI containers.
- **Smart TextArea Routing**: Detects legacy `GUILayout.TextArea` single-control drawers and routes them to native auto-expanding uGUI text boxes.
- **ConfigurationManagerAttributes**: Fully supports categories, order indexes, read-only flags, value ranges, and custom drawer delegates.

---

## Installation

### Via Thunderstore / r2modman (Recommended)
1. Install via your mod manager of choice (search for `BepInEx_ConfigDrawers` by **Vapok**).
2. Launch the game and press `F1`.

### Manual Installation
1. Ensure **BepInEx 5.4.x** is installed.
2. Download the latest release from [Releases](https://github.com/Vapok/BepInEx.ConfigDrawers/releases).
3. Place `BepInEx.ConfigDrawers.dll` into your `BepInEx/plugins/` directory.

---

## Building from Source

Requirements:
- .NET SDK (supports .NET Framework 4.8 / MSBuild)
- BepInEx 5.4.x assemblies
- Unity 6 / TextMeshPro assemblies (included in game references)

```bash
git clone https://github.com/Vapok/BepInEx.ConfigDrawers.git
cd BepInEx.ConfigDrawers
dotnet build ConfigDrawers.sln -c Release
```

---

## Author & Community

Created with ❤️ by **Vapok**.

- **Website**: [vapok.io](https://vapok.io)
- **Discord**: [Join the Vapok Gaming Community](https://discord.gg/vapok)
- **GitHub**: [Vapok/BepInEx.ConfigDrawers](https://github.com/Vapok/BepInEx.ConfigDrawers)
- **Thunderstore**: [Vapok Mods](https://thunderstore.io/c/valheim/p/Vapok/)
