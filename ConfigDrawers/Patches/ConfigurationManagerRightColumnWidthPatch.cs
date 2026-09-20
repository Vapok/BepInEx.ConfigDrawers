using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace BepInEx.ConfigDrawers.Patches;

[HarmonyPatch]
public static class ConfigurationManagerRightColumnWidthPatch
{
    public static bool Prepare()
    {
        return AccessTools.TypeByName("ConfigurationManager.ConfigurationManager") != null;
    }

    public static IEnumerable<MethodBase> TargetMethods()
    {
        var targets = new List<MethodBase>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var cmType = assembly.GetType("ConfigurationManager.ConfigurationManager");
                if (cmType != null)
                {
                    var prop = AccessTools.PropertyGetter(cmType, "RightColumnWidth");
                    if (prop != null)
                    {
                        targets.Add(prop);
                    }
                }
            }
            catch
            {
                // Soft skip restricted dynamic assemblies
            }
        }
        return targets;
    }

    public static void Postfix(ref int __result)
    {
        if (__result <= 0 || __result < 200)
        {
            __result = 350;
        }
    }
}
