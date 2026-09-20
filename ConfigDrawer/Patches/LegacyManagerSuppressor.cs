using System;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using BepInEx.ConfigDrawers.Configuration;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Patches;

public static class LegacyManagerSuppressor
{
    private static bool _suppressed;

    public static void CheckAndSuppress(ManualLogSource logger)
    {
        if (_suppressed || !ConfigDrawerConfig.AutoSuppressLegacy.Value)
        {
            return;
        }

        try
        {
            if (Chainloader.ManagerObject == null)
            {
                return;
            }

            var components = Chainloader.ManagerObject.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp == null)
                {
                    continue;
                }

                var type = comp.GetType();
                if (type.FullName == "ConfigurationManager.ConfigurationManager" && comp.GetType().Assembly != typeof(ConfigDrawers).Assembly)
                {
                    SuppressComponent(comp, type, logger);
                    _suppressed = true;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Legacy suppressor check encountered: {ex.Message}");
        }
    }

    private static void SuppressComponent(MonoBehaviour component, Type type, ManualLogSource logger)
    {
        try
        {
            var overrideProp = type.GetProperty("OverrideHotkey", BindingFlags.Instance | BindingFlags.Public);
            if (overrideProp != null && overrideProp.CanWrite)
            {
                overrideProp.SetValue(component, true, null);
                logger.LogInfo("Successfully suppressed legacy ConfigurationManager hotkey listener.");
            }

            var displayingProp = type.GetProperty("DisplayingWindow", BindingFlags.Instance | BindingFlags.Public);
            if (displayingProp != null && displayingProp.CanWrite)
            {
                displayingProp.SetValue(component, false, null);
            }

            var rightColProp = type.GetProperty("RightColumnWidth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (rightColProp != null && rightColProp.CanWrite)
            {
                rightColProp.SetValue(component, 350, null);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Failed to suppress legacy component: {ex.Message}");
        }
    }
}
