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

**BepInEx.ConfigDrawers** is a modern, lightweight, high-performance in-game Configuration Manager built from the ground up to replace outdated configuration managers. 

Designed with a sleek **Cyber-Console UI** inspired by [Vapok.io](https://vapok.io), `BepInEx.ConfigDrawers` slides out effortlessly as a side drawer when you press your hotkey, keeping your game screen visible and making mod tweaking enjoyable again.

---

## Key Features

- **Smooth Sliding Drawer**: Slides out from the screen edge on demand. Occupies less than 25% of your screen width, leaving your game world completely visible.
- **Magnetic Docking & Free Float**: Default to the left monitor rail, snap to the right rail, or detach and drag freely across your screen to keep whatever you are tuning in clear view.
- **No More Clobbered Text Fields**: Input fields use a modern buffered editing pattern. You can backspace, type decimals, and adjust values without your inputs resetting or jumping.
- **Interactive In-Game Rebinding**: Want to change the hotkey from `F1` to `F10` or `Pause`? Click `[ BIND ]`, press your key, and you're done.
- **First-Class Data Grids & Tables**: Dedicated visual multi-column tables for crafting recipes, upgrading costs, and creature drop tables.
- **Dual-Mode JSON Editor**: Inspect custom JSON configurations using clean visual property cards or switch to a monospace code editor with live syntax checking.
- **Contextual Hover Cards**: Hover over any setting to view its full description, default value, acceptable limits, and ServerSync lock status.
- **Live Translucency Mode**: Toggle background opacity (`100%`, `80%`, `60%`) to look directly through the menu at your game environment.
- **Automatic Legacy Takeover**: Gracefully disables hotkeys on older `ConfigurationManager.dll` versions to eliminate dual-window conflicts.
- **Zero Bloat & Telemetry**: Completely standalone, ultra-lightweight, zero telemetry, and zero background tracking.

---

## Controls & Usage

| Action | Default Input | Description |
| :--- | :--- | :--- |
| **Toggle Menu** | `F1` | Opens or closes the BepInEx.ConfigDrawers menu. |
| **Close Menu** | `Escape` | Closes the drawer (or reverts an active text box edit). |
| **Commit Edit** | `Enter` | Saves and applies the current input field value. |
| **Dock Left / Right** | `[ L ]` / `[ R ]` | Snaps the drawer to the left or right monitor rail. |
| **Detach / Float** | `[ ⧉ ]` | Detaches the drawer into a movable floating window. |
| **Rebind Hotkey** | Header Button | Click `[ BIND: F1 ]` and press any key to set a new toggle bind. |

---

## Compatibility

`BepInEx.ConfigDrawers` provides 100% ecosystem compatibility:
- **ServerSync Ready**: Automatically reflects locked server settings for non-admin players with clear visual indicators.
- **Legacy Drawer Bridge**: Seamlessly renders custom drawers provided by third-party BepInEx mods.
- **Universal Engine**: Built on pure Unity and BepInEx 5—works out of the box with Valheim and other Unity BepInEx titles.

---

## Installation

### Via Thunderstore / r2modman (Recommended)
1. Install via your mod manager of choice.
2. Launch the game and press `F1`.

### Manual Installation
1. Ensure **BepInEx 5.4** is installed.
2. Extract `BepInEx.ConfigDrawers.dll` into your `BepInEx/plugins/` directory.

---

## Author & Support

Created with ❤️ by **Vapok**.

- **Website**: [vapok.io](https://vapok.io)
- **Discord**: [Join the Vapok Gaming Community](https://discord.gg/vapok)
- **GitHub**: [Vapok/BepInEx.ConfigDrawers](https://github.com/Vapok/BepInEx.ConfigDrawers)
