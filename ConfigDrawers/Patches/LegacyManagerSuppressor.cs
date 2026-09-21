using System;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using BepInEx.ConfigDrawers.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Patches;

internal static class LegacyManagerSuppressor
{
    private static bool _suppressed;
    private const int DefaultLegacyColumnWidth = 350;

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

            MonoBehaviour[] components = Chainloader.ManagerObject.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour comp in components)
            {
                if (comp == null)
                {
                    continue;
                }

                Type type = comp.GetType();
                if (type.FullName == "ConfigurationManager.ConfigurationManager" && type.Assembly != typeof(ConfigDrawers).Assembly)
                {
                    DisableLegacyHotkeyAndWindow(comp, type, logger);
                    SetLegacyRightColumnWidth(comp, type, DefaultLegacyColumnWidth);
                    _suppressed = true;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Legacy suppressor check encountered: {ex.Message}");
        }
    }

    private static void DisableLegacyHotkeyAndWindow(MonoBehaviour component, Type type, ManualLogSource logger)
    {
        if (TrySetProperty(component, type, "OverrideHotkey", true))
        {
            logger.LogInfo("Successfully suppressed legacy ConfigurationManager hotkey listener.");
        }

        TrySetProperty(component, type, "DisplayingWindow", false);
    }

    private static void SetLegacyRightColumnWidth(MonoBehaviour component, Type type, int width)
    {
        TrySetProperty(component, type, "RightColumnWidth", width);
    }

    private static bool TrySetProperty(object target, Type type, string propertyName, object value)
    {
        try
        {
            PropertyInfo? property = AccessTools.Property(type, propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, value, null);
                return true;
            }
        }
        catch (Exception ex)
        {
            ConfigDrawers.Log?.LogDebug($"[ConfigDrawers] TrySetProperty failed for {propertyName}: {ex.Message}");
        }

        return false;
    }
}
