# ConfigDrawer & Vapok.Common V2 Roadmap

## Vision: Native Fluent uGUI Custom Drawers

### 1. Context & Motivation
- **The Legacy Heritage**: BepInEx `ConfigurationManager` and mods utilizing `ConfigurationManagerAttributes.CustomDrawer` (like `Vapok.Common`'s `ItemManager`, `PieceManager`, and Jotunn) were historically designed around Unity's Immediate Mode GUI (`GUILayout`).
- **The Current V1 Bridge**: `ConfigDrawer` v1 bridges legacy IMGUI into uGUI through `LegacyImguiBridge` using DPI canvas scaling, viewport group clipping, and reflection-based dynamic layout measurement. While this provides 100% backwards compatibility for existing mods, IMGUI remains a foreign subsystem running inside uGUI.
- **The V2 Goal**: Provide a modern, native uGUI drawer API that offers the procedural ease of `GUILayout` with the crisp typography, theme responsiveness, and performance of native uGUI.

---

### 2. Architecture & Design Concept

#### Dual-Delegate Support in ConfigurationManagerAttributes
Introduce an optional native uGUI drawer delegate alongside the legacy IMGUI delegate:
```csharp
public class ConfigurationManagerAttributes
{
    // Legacy IMGUI delegate (v1 compatibility)
    public Action<ConfigEntryBase>? CustomDrawer;

    // Modern native uGUI delegate (v2 fluent builder)
    public Action<IUguiDrawerScope>? CustomUguiDrawer;
}
```

#### Dispatcher Resolution Priority
In `DrawerDispatcher.cs`:
1. Check if `entry.CustomUguiDrawer != null` -> Execute native uGUI fluent builder.
2. Else if `entry.CustomDrawer != null` -> Execute legacy `LegacyImguiBridge` (IMGUI).
3. Else -> Fallback to native primitive drawers (DataGrid, Slider, Enum, Text, etc.).

---

### 3. Proposed Fluent uGUI Builder API (`IUguiDrawerScope`)

The builder gives mod authors a procedural layout experience mirroring `GUILayout` without requiring manual GameObject parenting or RectTransform math:

```csharp
public interface IUguiDrawerScope
{
    Transform Container { get; }
    
    // Rows & Layout Groups
    IDisposable Horizontal(float spacing = 4f);
    IDisposable Vertical(float spacing = 4f);
    
    // Widgets
    void Label(string text, float width = -1f);
    void TextField(string value, Action<string> onCommit, float width = -1f);
    void Button(string text, Action onClick, float width = 50f);
    void Slider(float value, float min, float max, Action<float> onChanged, float width = 120f);
    void Toggle(bool value, string label, Action<bool> onChanged);
    void Space(float pixels);
}
```

#### Example: Modernizing `ItemManager.drawDropsConfigTable` in V2
```csharp
private static void DrawDropsConfigTableUgui(IUguiDrawerScope ui, ConfigEntryBase cfg)
{
    var drops = new SerializedDrop((string)cfg.BoxedValue).Drops;

    using (ui.Vertical(spacing: 6f))
    {
        foreach (var drop in drops)
        {
            using (ui.Horizontal(spacing: 4f))
            {
                ui.TextField(drop.creature, val => { drop.creature = val; UpdateConfig(); }, width: 140f);
                ui.Button("X", () => RemoveDrop(drop), width: 24f);
                ui.Button("+", () => AddDrop(), width: 24f);
            }
            
            using (ui.Horizontal(spacing: 4f))
            {
                ui.Label("Chance:");
                ui.Slider(drop.chance, 0f, 1f, val => { drop.chance = val; UpdateConfig(); }, width: 100f);
                ui.Label($"{(drop.chance * 100):0.#}%");
            }
            
            using (ui.Horizontal(spacing: 4f))
            {
                ui.Toggle(drop.levelMultiplier, "Level scaling drop amount", val => { drop.levelMultiplier = val; UpdateConfig(); });
            }
        }
    }
}
```

---

### 4. Benefits of V2 Native Drawers
1. **Zero IMGUI Overhead**: No `OnGUI` execution passes, no group clipping math, no texture allocation per frame.
2. **Cohesive Theming**: Uses the active `CyberPalette` font sizes, colors, and hover states natively.
3. **Rock-Solid Event Handling**: Fully integrated into uGUI's `EventSystem` with standard navigation and scroll view isolation.
4. **Complete Backwards Compatibility**: Mods that haven't updated to `CustomUguiDrawer` continue running seamlessly through the `LegacyImguiBridge`.
