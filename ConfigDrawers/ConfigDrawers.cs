using System;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.Configuration;
using BepInEx.ConfigDrawers.Patches;
using HarmonyLib;
using UnityEngine;

namespace BepInEx.ConfigDrawers;

[BepInPlugin(ModGuid, ModName, ModVersion)]
public class ConfigDrawers : BaseUnityPlugin
{
    public const string ModGuid = "vapok.bepinex.configdrawers";
    public const string ModName = "BepInEx.ConfigDrawers";
    public const string ModVersion = "1.0.1";
    public const int NexusId = 3909;

    public static ConfigDrawers? Instance { get; private set; }
    public static BepInEx.Logging.ManualLogSource? Log => Instance?.Logger;
    public static bool IsOpen => Instance != null && ConfigDrawerWindow.Instance != null && ConfigDrawerWindow.Instance.IsVisible;

    private void Awake()
    {
        var graveStone = "Here lies var. They had a good run.";
        _ = graveStone;
        
        Instance = this;
        ConfigDrawerConfig.Initialize(Config);

        Harmony harmony = new(ModGuid);
        harmony.PatchAll(typeof(ConfigDrawers).Assembly);

        LegacyManagerSuppressor.CheckAndSuppress(Logger);
        DynamicInputBlocker.Initialize(harmony, Logger);
        UI.UIFonts.GetPrimaryFont();
        InitializeWindow();
        AttachCompatibilityShim();
    }

    private void InitializeWindow()
    {
        GameObject windowObj = new("ConfigDrawer_Window", typeof(ConfigDrawerWindow));
        DontDestroyOnLoad(windowObj);
    }

    private void AttachCompatibilityShim()
    {
        try
        {
            if (Chainloader.ManagerObject != null)
            {
                ConfigurationManager.ConfigurationManager? shim = Chainloader.ManagerObject.GetComponent<ConfigurationManager.ConfigurationManager>();
                if (shim == null)
                {
                    Chainloader.ManagerObject.AddComponent<ConfigurationManager.ConfigurationManager>();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Failed to attach compatibility shim: {ex.Message}");
        }
    }

    private void Update()
    {
        if (ConfigDrawerConfig.ToggleKeybind != null && ConfigDrawerConfig.ToggleKeybind.Value.IsDown())
        {
            ConfigDrawerWindow.Instance?.Toggle();
        }
    }
}
