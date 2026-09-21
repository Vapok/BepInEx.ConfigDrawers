# Custom Configuration Drawers

`BepInEx.ConfigDrawers` supports modern uGUI drawers alongside legacy IMGUI drawers.

## Dual-Drawer Compatibility

To ensure your mod works in both modern `BepInEx.ConfigDrawers` and legacy `ConfigurationManager.dll` without crashing, assign both delegates on `ConfigurationManagerAttributes`:

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

- **Zero Coupling**: You do not need to reference `BepInEx.ConfigDrawers.dll`. Copy [`Docs/DropIn/ConfigurationManagerAttributes.cs`](DropIn/ConfigurationManagerAttributes.cs) into your project.
- **Legacy Fallback**: If a player uses legacy `ConfigurationManager.dll`, it executes `CustomDrawer` via IMGUI.
- **Modern Retained UI**: `BepInEx.ConfigDrawers` prioritizes `CustomUguiDrawer`, instantiating native TextMeshPro and uGUI controls without per-frame `OnGUI` execution.

## IUguiDrawerScope API

| Method | Description |
| :--- | :--- |
| `IDisposable Horizontal(spacing)` | Starts a horizontal layout group. Dispose to close. |
| `IDisposable Vertical(spacing)` | Starts a vertical layout group. Dispose to close. |
| `Label(text, width)` | Adds a TextMeshPro label. Set width or pass -1 for auto-fit. |
| `TextField(value, onCommit, width)` | Adds a TextMeshPro input field with an onCommit callback. |
| `Button(text, onClick, width)` | Adds a styled button with an onClick callback. |
| `Slider(value, min, max, onChanged, width)` | Adds a numeric slider with an onChanged callback. |
| `Toggle(value, label, onChanged)` | Adds an ON/OFF toggle switch with an optional label. |
| `Space(pixels)` | Adds horizontal or vertical layout spacing. |
