using BepInEx;
using BepInEx.Bootstrap;
using ConfigDrawer.Components;
using ConfigDrawer.Configuration;
using ConfigDrawer.Patches;
using UnityEngine;

namespace ConfigDrawer;

[BepInPlugin(ModGuid, ModName, ModVersion)]
public class ConfigDrawer : BaseUnityPlugin
{
    public const string ModGuid = "vapok.mods.configdrawer";
    public const string ModName = "ConfigDrawer";
    public const string ModVersion = "1.0.0";

    public static ConfigDrawer? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        ConfigDrawerConfig.Initialize(Config);

        LegacyManagerSuppressor.CheckAndSuppress(Logger);
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
