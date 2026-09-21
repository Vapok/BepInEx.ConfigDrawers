# BepInEx.ConfigDrawers

<p align="center">
  <img src="https://raw.githubusercontent.com/Vapok/BepInEx.ConfigDrawers/main/icon.png" width="128" height="128" alt="BepInEx.ConfigDrawers Logo">
</p>

<p align="center">
  <strong>In-game configuration manager for BepInEx 5 plugins.</strong>
</p>

<p align="center">
  <a href="https://github.com/Vapok/BepInEx.ConfigDrawers/releases"><img src="https://img.shields.io/github/v/release/Vapok/BepInEx.ConfigDrawers?include_prereleases&style=flat-square" alt="GitHub Release"></a>
  <a href="https://thunderstore.io/c/valheim/p/Vapok/BepInEx.ConfigDrawers/"><img src="https://img.shields.io/thunderstore/v/Vapok/BepInEx.ConfigDrawers?style=flat-square" alt="Thunderstore Version"></a>
  <a href="https://discord.gg/vapok"><img src="https://img.shields.io/discord/941785535977934898?label=Discord&logo=discord&style=flat-square" alt="Discord"></a>
  <a href="LICENSE.md"><img src="https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square" alt="License"></a>
</p>

---

## Overview

**BepInEx.ConfigDrawers** is an in-game configuration manager for BepInEx 5 plugins built on Unity uGUI. It provides a collapsible drawer interface that docks to either side of your screen or floats as a moveable window, allowing you to edit plugin configurations in real time without obscuring gameplay.

---

## Features

- **Docking & Floating Window**: Dock to the left or right screen rail, or detach into a free-floating draggable window with adjustable width.
- **Search & Filtering**: Real-time search filter across plugin names, sections, setting keys, and descriptions.
- **Native Setting Drawers**: Built-in drawer editors for primitives (`bool`, `int`, `float`, `string`, `enum`), vectors (`Vector2`, `Vector3`, `Vector4`), and key shortcuts.
- **Multiline Text Editor**: Auto-expanding editor for long strings, tokenized format strings, and JSON configurations.
- **Color Spectrum Picker**: HSV color wheel with hex/RGB inputs and palette swatch presets.
- **Data Grids & Tables**: Multi-column table views for complex structured settings like recipes and drop lists.
- **UI Scaling**: Configurable scale presets (Small, Normal, Large) for different screen resolutions.
- **Hotkey Rebinding**: Click the hotkey button in the header and press any key to rebind the menu toggle shortcut in-game.
- **ServerSync Integration**: Automatically identifies server-enforced configurations and displays synchronization status indicators.
- **Legacy IMGUI Compatibility**: Automatically suppresses conflicting legacy `ConfigurationManager.dll` hotkeys while continuing to render legacy custom drawer delegates inside the modern drawer.
- **Custom uGUI & IMGUI Drawers**: Native procedural builder API (`CustomUguiDrawer`) for mod configuration interfaces with legacy IMGUI fallback. See [Custom Drawers Guide](Docs/CUSTOM_DRAWERS.md).

---

## Controls & Keybinds

| Action | Default Input | Description |
| :--- | :--- | :--- |
| **Toggle Drawer** | `F1` | Opens or closes the configuration drawer. |
| **Close / Cancel** | `Escape` | Closes the drawer or cancels the active input field edit. |
| **Commit Edit** | `Enter` / Defocus | Commits the input change and saves the configuration. |
| **Dock Left / Right** | `[ Left ]` / `[ Right ]` | Snaps the drawer to the left or right monitor rail. |
| **Float Window** | `[ Float ]` | Detaches the drawer into a free-floating window. |
| **Resize Width** | Drag Rail Handle | Drag the inner border handle to adjust the drawer width. |
| **Cycle UI Scale** | `[ Size: Norm ]` | Cycles between Small (85%), Normal (95%), and Large (108%) UI scaling. |
| **Rebind Toggle Key** | Click Hotkey Button | Click the hotkey button in the header, then press the desired keyboard key. |

---

## Configuration Settings

Settings are stored in `BepInEx/config/vapok.bepinex.configdrawers.cfg`:

| Section | Key | Default | Description |
| :--- | :--- | :--- | :--- |
| `General` | `ToggleKeybind` | `F1` | Keyboard shortcut to open and close the drawer. |
| `General` | `DefaultDockPosition` | `Right` | Default dock position on startup (`Left`, `Right`, or `Float`). |
| `General` | `DrawerWidth` | `380` | Width of the drawer in pixels when docked. |
| `General` | `UiFontSize` | `Normal` | Font and layout scale (`Small`, `Normal`, or `Large`). |
| `Compatibility` | `AutoSuppressLegacy` | `true` | Suppresses legacy ConfigurationManager window hotkeys to avoid duplicate windows. |

---

## Installation

### Thunderstore / Mod Manager (Recommended)
1. Install `BepInEx.ConfigDrawers` via r2modman or Thunderstore Mod Manager.
2. Launch the game and press `F1` to open the configuration drawer.

### Manual Installation
1. Ensure **BepInEx 5.4.x** is installed.
2. Download the latest release package from [Releases](https://github.com/Vapok/BepInEx.ConfigDrawers/releases).
3. Extract `BepInEx.ConfigDrawers.dll` into your `BepInEx/plugins/` directory.

---

## Building from Source

Requirements:
- .NET SDK (supporting .NET Framework 4.8 / MSBuild)
- BepInEx 5.4.x core libraries
- Unity / TextMeshPro assemblies

```bash
git clone https://github.com/Vapok/BepInEx.ConfigDrawers.git
cd BepInEx.ConfigDrawers
dotnet build ConfigDrawers.sln -c Release
```

---

## Author & Community

Maintained by **Vapok**.

- **Website**: [vapok.io](https://vapok.io)
- **Discord**: [Vapok Gaming Community](https://discord.gg/vapok)
- **GitHub**: [Vapok/BepInEx.ConfigDrawers](https://github.com/Vapok/BepInEx.ConfigDrawers)
- **Thunderstore**: [Vapok Mods](https://thunderstore.io/c/valheim/p/Vapok/)
