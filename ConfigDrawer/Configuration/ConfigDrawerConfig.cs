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
    public static ConfigEntry<float> TranslucencyOpacity { get; private set; } = null!;
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
            440f,
            new ConfigDescription("Width of the drawer in pixels.", new AcceptableValueRange<float>(360f, 720f))
        );

        UiScale = config.Bind(
            "Interface",
            "UI Scale",
            1.0f,
            new ConfigDescription("UI scale factor.", new AcceptableValueRange<float>(0.75f, 1.75f))
        );

        TranslucencyOpacity = config.Bind(
            "Interface",
            "Translucency Opacity",
            0.95f,
            new ConfigDescription("Background surface opacity.", new AcceptableValueRange<float>(0.5f, 1.0f))
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
