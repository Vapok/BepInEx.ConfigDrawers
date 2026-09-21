using System;
using BepInEx.Configuration;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Configuration;

public static class ConfigDrawerConfig
{
    public static ConfigEntry<KeyboardShortcut> ToggleKeybind { get; private set; } = null!;
    public static ConfigEntry<DockPosition> DefaultDockPosition { get; private set; } = null!;
    public static ConfigEntry<float> DrawerWidth { get; private set; } = null!;
    public static ConfigEntry<float> UiScale { get; private set; } = null!;
    public static ConfigEntry<FontSizeScale> UiFontSize { get; private set; } = null!;
    public static ConfigEntry<float> WindowOpacity { get; private set; } = null!;
    public static ConfigEntry<bool> HideAdvancedByDefault { get; private set; } = null!;
    public static ConfigEntry<bool> AutoSuppressLegacy { get; private set; } = null!;

    public static void Initialize(ConfigFile config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        ToggleKeybind = config.Bind(
            "General",
            "Toggle Keybind",
            new KeyboardShortcut(KeyCode.F1),
            "Key or key combination to toggle ConfigDrawer."
        );

        DefaultDockPosition = config.Bind(
            "Interface",
            "Default Dock Position",
            DockPosition.Left,
            "Default screen rail where ConfigDrawer docks."
        );

        DrawerWidth = config.Bind(
            "Interface",
            "Drawer Width",
            480f,
            new ConfigDescription("Width of the drawer in pixels.", new AcceptableValueRange<float>(360f, 720f))
        );

        UiScale = config.Bind(
            "Interface",
            "UI Scale",
            1.0f,
            new ConfigDescription("UI scale factor.", new AcceptableValueRange<float>(0.75f, 1.75f))
        );

        UiFontSize = config.Bind(
            "Interface",
            "Font Size",
            FontSizeScale.Normal,
            "Font size scale: Small, Normal, Large."
        );

        ConfigDefinition legacyDef = new ConfigDefinition("Interface", "Translucency Opacity");
        float defaultOpacity = 1.0f;
        if (config.ContainsKey(legacyDef))
        {
            ConfigEntry<float> legacyEntry = config.Bind(legacyDef, 1.0f);
            defaultOpacity = legacyEntry.Value;
            config.Remove(legacyDef);
        }

        WindowOpacity = config.Bind(
            "Interface",
            "Window Opacity",
            defaultOpacity,
            new ConfigDescription("Overall opacity of the drawer window.", new AcceptableValueRange<float>(0.20f, 1.0f))
        );

        HideAdvancedByDefault = config.Bind(
            "Interface",
            "Hide Advanced Settings",
            true,
            "Hide advanced settings by default until toggled."
        );

        AutoSuppressLegacy = config.Bind(
            "Compatibility",
            "Auto Suppress Legacy Manager",
            true,
            "Automatically disable hotkeys of older ConfigurationManager versions to prevent duplicate windows."
        );
    }
}
