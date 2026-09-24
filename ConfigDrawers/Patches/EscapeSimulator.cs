using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Patches;

internal static class EscapeSimulator
{
    private const string HarmonyId = "vapok.bepinex.configdrawers.escapesimulator";
    private static readonly Harmony HarmonyInstance = new(HarmonyId);

    public static bool IsSimulating { get; private set; }
    public static bool WasEscapedOnOpen { get; set; }
    private static bool _escapeConsumed;
    private static Coroutine? _activeRoutine;

    private static MethodInfo? _getKeyDownKeyCode;
    private static MethodInfo? _getKeyDownString;
    private static MethodInfo? _getKeyKeyCode;
    private static MethodInfo? _getKeyString;
    private static MethodInfo? _newInputWasPressed;

    public static void Initialize()
    {
        try
        {
            Type inputType = typeof(Input);
            MethodInfo keyCodePrefix = AccessTools.Method(typeof(EscapeSimulator), nameof(KeyCodePrefix));
            MethodInfo stringPrefix = AccessTools.Method(typeof(EscapeSimulator), nameof(StringPrefix));

            _getKeyDownKeyCode = AccessTools.Method(inputType, nameof(Input.GetKeyDown), new Type[] { typeof(KeyCode) });
            if (_getKeyDownKeyCode != null)
            {
                HarmonyInstance.Patch(_getKeyDownKeyCode, prefix: new HarmonyMethod(keyCodePrefix));
            }

            _getKeyDownString = AccessTools.Method(inputType, nameof(Input.GetKeyDown), new Type[] { typeof(string) });
            if (_getKeyDownString != null)
            {
                HarmonyInstance.Patch(_getKeyDownString, prefix: new HarmonyMethod(stringPrefix));
            }

            _getKeyKeyCode = AccessTools.Method(inputType, nameof(Input.GetKey), new Type[] { typeof(KeyCode) });
            if (_getKeyKeyCode != null)
            {
                HarmonyInstance.Patch(_getKeyKeyCode, prefix: new HarmonyMethod(keyCodePrefix));
            }

            _getKeyString = AccessTools.Method(inputType, nameof(Input.GetKey), new Type[] { typeof(string) });
            if (_getKeyString != null)
            {
                HarmonyInstance.Patch(_getKeyString, prefix: new HarmonyMethod(stringPrefix));
            }

            Type? buttonControlType = AccessTools.TypeByName("UnityEngine.InputSystem.Controls.ButtonControl");
            if (buttonControlType != null)
            {
                PropertyInfo? wasPressedProp = AccessTools.Property(buttonControlType, "wasPressedThisFrame");
                _newInputWasPressed = wasPressedProp?.GetGetMethod();
                if (_newInputWasPressed != null)
                {
                    MethodInfo newPrefix = AccessTools.Method(typeof(EscapeSimulator), nameof(NewInputPrefix));
                    HarmonyInstance.Patch(_newInputWasPressed, prefix: new HarmonyMethod(newPrefix));
                }
            }

            ConfigDrawers.Log?.LogInfo("[ConfigDrawers] EscapeSimulator initialized with dedicated Harmony instance.");
        }
        catch (Exception ex)
        {
            ConfigDrawers.Log?.LogWarning($"[ConfigDrawers] Escape simulator hooks failed: {ex.Message}");
        }
    }

    private static bool KeyCodePrefix(KeyCode key, ref bool __result)
    {
        if (IsSimulating && !_escapeConsumed && key == KeyCode.Escape)
        {
            _escapeConsumed = true;
            __result = true;
            ConfigDrawers.Log?.LogInfo("[ConfigDrawers] Simulated Escape keydown intercepted via Input.GetKeyDown(KeyCode).");
            return false;
        }
        return true;
    }

    private static bool StringPrefix(string name, ref bool __result)
    {
        if (IsSimulating && !_escapeConsumed && !string.IsNullOrEmpty(name) && string.Equals(name, "escape", StringComparison.OrdinalIgnoreCase))
        {
            _escapeConsumed = true;
            __result = true;
            ConfigDrawers.Log?.LogInfo("[ConfigDrawers] Simulated Escape keydown intercepted via Input.GetKeyDown(string).");
            return false;
        }
        return true;
    }

    private static bool NewInputPrefix(object __instance, ref bool __result)
    {
        if (IsSimulating && !_escapeConsumed && __instance != null)
        {
            if (IsEscapeControl(__instance))
            {
                _escapeConsumed = true;
                __result = true;
                ConfigDrawers.Log?.LogInfo("[ConfigDrawers] Simulated Escape keydown intercepted via InputSystem wasPressedThisFrame.");
                return false;
            }
        }
        return true;
    }

    private static bool IsEscapeControl(object instance)
    {
        Type type = instance.GetType();

        PropertyInfo? keyProp = AccessTools.Property(type, "keyCode");
        if (keyProp != null)
        {
            object? keyVal = keyProp.GetValue(instance, null);
            if (keyVal != null && Convert.ToInt32(keyVal) == 60)
            {
                return true;
            }
        }

        PropertyInfo? nameProp = AccessTools.Property(type, "name");
        string? name = nameProp?.GetValue(instance, null) as string;
        if (!string.IsNullOrEmpty(name) && string.Equals(name, "escape", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        PropertyInfo? dispProp = AccessTools.Property(type, "displayName");
        string? disp = dispProp?.GetValue(instance, null) as string;
        if (!string.IsNullOrEmpty(disp) && string.Equals(disp, "escape", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public static void TriggerSimulatedPress(MonoBehaviour runner, Action? onComplete = null)
    {
        if (runner == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (_activeRoutine != null)
        {
            runner.StopCoroutine(_activeRoutine);
            _activeRoutine = null;
        }

        _activeRoutine = runner.StartCoroutine(SimulateRoutine(onComplete));
    }

    private static IEnumerator SimulateRoutine(Action? onComplete)
    {
        IsSimulating = true;
        _escapeConsumed = false;

        yield return null;
        yield return new WaitForEndOfFrame();

        IsSimulating = false;
        _escapeConsumed = false;
        _activeRoutine = null;

        onComplete?.Invoke();
    }
}
