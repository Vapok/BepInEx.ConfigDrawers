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
    public const string ModVersion = "1.0.0";

    public static ConfigDrawers? Instance { get; private set; }
    public static BepInEx.Logging.ManualLogSource? Log => Instance?.Logger;

    private void Awake()
    {
        Instance = this;
        ConfigDrawerConfig.Initialize(Config);

        var harmony = new Harmony(ModGuid);
        harmony.PatchAll(typeof(ConfigDrawers).Assembly);

        LegacyManagerSuppressor.CheckAndSuppress(Logger);
        UI.UiFactory.ResolveFont();
        InitializeWindow();
        AttachCompatibilityShim();
    }

    private void InitializeWindow()
    {
        var windowObj = new GameObject("ConfigDrawer_Window", typeof(ConfigDrawerWindow));
        DontDestroyOnLoad(windowObj);
    }

    private void AttachCompatibilityShim()
    {
        try
        {
            if (Chainloader.ManagerObject != null)
            {
                var shim = Chainloader.ManagerObject.GetComponent<ConfigurationManager.ConfigurationManager>();
                if (shim == null)
                {
                    Chainloader.ManagerObject.AddComponent<ConfigurationManager.ConfigurationManager>();
                }
            }
        }
        catch
        {
            // Soft failure ignore non-standard host environments
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
