using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Patches;

[HarmonyPatch]
public static class GuiHorizontalSliderClampPatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        var methods = typeof(GUI).GetMethods(BindingFlags.Public | BindingFlags.Static);
        foreach (var m in methods)
        {
            if (m.Name == nameof(GUI.HorizontalSlider))
            {
                yield return m;
            }
        }
    }

    public static void Postfix(float leftValue, float rightValue, ref float __result)
    {
        var min = Mathf.Min(leftValue, rightValue);
        var max = Mathf.Max(leftValue, rightValue);
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
