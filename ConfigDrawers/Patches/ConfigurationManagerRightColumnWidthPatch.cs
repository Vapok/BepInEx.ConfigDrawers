using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace BepInEx.ConfigDrawers.Patches;

[HarmonyPatch]
internal static class ConfigurationManagerRightColumnWidthPatch
{
    private const int MinimumRightColumnWidth = 200;
    private const int TargetRightColumnWidth = 350;

    [HarmonyPrepare]
    internal static bool Prepare()
    {
        return AccessTools.TypeByName("ConfigurationManager.ConfigurationManager") != null;
    }

    [HarmonyTargetMethods]
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        List<MethodBase> targets = new();
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (Assembly assembly in assemblies)
        {
            try
            {
                Type? cmType = assembly.GetType("ConfigurationManager.ConfigurationManager");
                if (cmType != null)
                {
                    MethodInfo? prop = AccessTools.PropertyGetter(cmType, "RightColumnWidth");
                    if (prop != null)
                    {
                        targets.Add(prop);
                    }
                }
            }
            catch (Exception ex)
            {
                ConfigDrawers.Log?.LogDebug($"[ConfigDrawers] Dynamic assembly inspection skipped {assembly.FullName}: {ex.Message}");
            }
        }
        return targets;
    }

    [HarmonyPostfix]
    private static void Postfix(ref int __result)
    {
        if (__result <= 0 || __result < MinimumRightColumnWidth)
        {
            __result = TargetRightColumnWidth;
        }
    }
}
