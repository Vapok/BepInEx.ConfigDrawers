# Changelog

All notable changes to **BepInEx.ConfigDrawers** are documented here.

---

## [1.0.2] - Toggle Visuals & Vector Inputs

- Fixed an issue where toggle buttons (On/Off) did not update their active color immediately when clicked without restarting the game.
- Expanded X, Y, and Z vector input boxes so five-digit numbers are fully visible without clipping.

## [1.0.1] - Window Opacity Fix

- Fixed an issue where the Window Opacity slider was not wired up to the window background.

## [1.0.0] - Initial Release

- **Slide-out Drawer Interface**: A configuration panel that docks to the edge of your screen.
- **Text & Numeric Editing**: Text boxes allow continuous typing and editing without focus loss or premature resets.
- **Flexible Docking**: Slides out from the left edge by default. Can be dragged freely or snapped to the left or right screen edge.
- **Window Opacity**: Adjustable opacity slider to view game elements behind the settings menu.
- **In-Menu Keybinding**: Rebind the menu toggle key directly from the interface.
- **Setting Tooltips**: Hover over settings to view descriptions, defaults, and acceptable ranges.
- **Custom Data Editors**: Dedicated visual editors for list values, tables, and structured data strings.
- **Lightweight**: Standalone BepInEx plugin with no telemetry or external network calls.
