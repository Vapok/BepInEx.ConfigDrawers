# Custom Configuration Drawers

`BepInEx.ConfigDrawers` supports modern uGUI drawers alongside legacy IMGUI drawers.

## Dual-Drawer Compatibility

To give players using legacy `ConfigurationManager.dll` a custom layout while taking full advantage of modern uGUI in `BepInEx.ConfigDrawers`, you can define both delegates on `ConfigurationManagerAttributes`:

```csharp
var desc = new ConfigDescription("Drop Configuration", null, new ConfigurationManagerAttributes
{
    // Legacy IMGUI fallback for original ConfigurationManager.dll
    CustomDrawer = cfg =>
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Creature:");
        cfg.BoxedValue = GUILayout.TextField((string)cfg.BoxedValue);
        GUILayout.EndHorizontal();
    },

    // Native uGUI for BepInEx.ConfigDrawers
    CustomUguiDrawer = ui =>
    {
        using (ui.Horizontal(spacing: 6f))
        {
            ui.Label("Creature:", width: 80f);
            ui.TextField((string)myEntry.Value, val => myEntry.Value = val, width: 140f);
            ui.Button("Reset", () => myEntry.Value = (string)myEntry.DefaultValue);
        }
    }
});
```

## How It Works

- **Zero Coupling**: You do not need to reference `BepInEx.ConfigDrawers.dll`. Copy [`ConfigurationManagerAttributes.cs`](https://raw.githubusercontent.com/Vapok/BepInEx.ConfigDrawers/main/Docs/DropIn/ConfigurationManagerAttributes.cs) into your project.
- **Legacy Fallback**: If a player uses legacy `ConfigurationManager.dll`, it executes `CustomDrawer` via IMGUI. If only `CustomUguiDrawer` is provided, legacy managers simply fall back to their standard textbox or slider editors without errors.
- **Modern Retained UI**: `BepInEx.ConfigDrawers` prioritizes `CustomUguiDrawer`, instantiating native TextMeshPro and uGUI controls without per-frame `OnGUI` execution. If only `CustomDrawer` is provided, `BepInEx.ConfigDrawers` seamlessly runs it through `LegacyImguiBridge`.

## IUguiDrawerScope API

| Method | Description |
| :--- | :--- |
| `IDisposable Horizontal(spacing)` | Starts a horizontal layout group. Dispose to close. |
| `IDisposable Vertical(spacing)` | Starts a vertical layout group. Dispose to close. |
| `IDisposable Box(backgroundColor, padding, spacing)` | Starts an enclosed padded box container with optional background tint. |
| `IDisposable Row(backgroundColor, padding, spacing)` | Starts a full-width row container with optional background tint. |
| `Label(text, width)` | Adds a TextMeshPro label. Set width or pass -1 for auto-fit. |
| `TextField(value, onCommit, width)` | Adds a TextMeshPro input field with an onCommit callback. |
| `Button(text, onClick, width, tooltip)` | Adds a styled button with an onClick callback and optional hover tooltip. |
| `Slider(value, min, max, onChanged, width, format)` | Adds a numeric slider with an onChanged callback and optional format. |
| `Toggle(value, label, onChanged)` | Adds an ON/OFF toggle switch with an optional label. |
| `Space(pixels)` | Adds horizontal or vertical layout spacing. |
| `Separator(height, color)` | Adds a subtle horizontal divider line. |
