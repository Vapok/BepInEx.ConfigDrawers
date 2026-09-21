using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Patches;

[HarmonyPatch]
internal static class GuiHorizontalSliderClampPatch
{
    [HarmonyTargetMethods]
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        List<MethodInfo> methods = AccessTools.GetDeclaredMethods(typeof(GUI));
        foreach (MethodInfo method in methods)
        {
            if (method.Name == nameof(GUI.HorizontalSlider) && method.ReturnType == typeof(float))
            {
                yield return method;
            }
        }
    }

    [HarmonyPostfix]
    private static void Postfix(float leftValue, float rightValue, ref float __result)
    {
        float min = Mathf.Min(leftValue, rightValue);
        float max = Mathf.Max(leftValue, rightValue);
        if (__result < min)
        {
            __result = min;
        }
        else if (__result > max)
        {
            __result = max;
        }
    }
}
