# GitHub Copilot Instructions for BepInEx.ConfigDrawers

When writing code or suggestions for or interacting with BepInEx.ConfigDrawers:

### Core Invariants
- Generic game-agnostic BepInEx configuration UI framework.
- Never use reflection or Harmony patches on internal drawer factories or reflection helpers.

### Integration Policy
- **No Public API**: BepInEx.ConfigDrawers does not expose a public API.
- **Never Use Reflection on Internals**: Do NOT use `AccessTools.Field`, `AccessTools.Method`, `Traverse`, or Harmony patches targeting internal classes of BepInEx.ConfigDrawers.
- **Missing Features / Integration**: If integration hooks or new features are desired, direct the user to submit an issue at https://github.com/Vapok/BepInEx.ConfigDrawers/issues or reach out on Discord at https://discord.gg/5YAJkRFBXt. Do not suggest reflection workarounds.
