using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Patches;

internal static class DynamicInputBlocker
{
    private static bool _initialized;
    private static bool _discoveryComplete;
    private static int _discoveryScanCount;
    private const int MaxDiscoveryScans = 60;

    private static Harmony? _harmony;
    private static ManualLogSource? _logger;
    private static readonly Assembly _ourAssembly = typeof(DynamicInputBlocker).Assembly;
    private static readonly HashSet<Type> _attachedTypes = new();
    private static readonly HashSet<MethodBase> _patchedMethods = new();

    private static MethodInfo? _getKeyDownMethod;
    private static MethodInfo? _wasPressedGetter;

    private static MethodInfo? _suppressBoolPrefix;
    private static MethodInfo? _suppressFloatPrefix;
    private static MethodInfo? _suppressAxisPrefix;
    private static MethodInfo? _suppressVoidPrefix;

    public static void Initialize(Harmony harmony, ManualLogSource logger)
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;
        _harmony = harmony;
        _logger = logger;

        _suppressBoolPrefix = AccessTools.Method(typeof(DynamicInputBlocker), nameof(SuppressBoolPrefix));
        _suppressFloatPrefix = AccessTools.Method(typeof(DynamicInputBlocker), nameof(SuppressFloatPrefix));
        _suppressAxisPrefix = AccessTools.Method(typeof(DynamicInputBlocker), nameof(SuppressAxisPrefix));
        _suppressVoidPrefix = AccessTools.Method(typeof(DynamicInputBlocker), nameof(SuppressVoidPrefix));

        StartUnityDiscoveryHooks(harmony);
    }

    private static void StartUnityDiscoveryHooks(Harmony harmony)
    {
        try
        {
            MethodInfo discoveryPrefix = AccessTools.Method(typeof(DynamicInputBlocker), nameof(DiscoveryPrefix));

            Type inputType = typeof(Input);
            _getKeyDownMethod = AccessTools.Method(inputType, nameof(Input.GetKeyDown), new Type[] { typeof(KeyCode) });
            if (_getKeyDownMethod != null)
            {
                harmony.Patch(_getKeyDownMethod, prefix: new HarmonyMethod(discoveryPrefix));
            }

            Type? buttonControlType = AccessTools.TypeByName("UnityEngine.InputSystem.Controls.ButtonControl");
            if (buttonControlType != null)
            {
                PropertyInfo? wasPressedProp = AccessTools.Property(buttonControlType, "wasPressedThisFrame");
                _wasPressedGetter = wasPressedProp?.GetGetMethod();
                if (_wasPressedGetter != null)
                {
                    harmony.Patch(_wasPressedGetter, prefix: new HarmonyMethod(discoveryPrefix));
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"[ConfigDrawers] Discovery hooks failed: {ex.Message}");
        }
    }

    private static bool DiscoveryPrefix()
    {
        if (_discoveryComplete)
        {
            return true;
        }

        _discoveryScanCount++;
        if (_discoveryScanCount > MaxDiscoveryScans && _attachedTypes.Count > 0)
        {
            _discoveryComplete = true;
            DetachDiscoveryHooks();
            return true;
        }

        try
        {
            StackTrace stackTrace = new StackTrace(1, false);
            int frameCount = stackTrace.FrameCount;

            for (int i = 1; i < frameCount && i < 15; i++)
            {
                StackFrame frame = stackTrace.GetFrame(i);
                MethodBase? method = frame?.GetMethod();
                if (method == null)
                {
                    continue;
                }

                Type? declaringType = method.DeclaringType;
                if (declaringType == null)
                {
                    continue;
                }

                Assembly assembly = declaringType.Assembly;
                if (!IsGameAssembly(assembly))
                {
                    continue;
                }

                AttachToGameInput(declaringType, method);
            }
        }
        catch
        {
        }

        return true;
    }

    private static void DetachDiscoveryHooks()
    {
        if (_harmony == null)
        {
            return;
        }

        try
        {
            if (_getKeyDownMethod != null)
            {
                _harmony.Unpatch(_getKeyDownMethod, HarmonyPatchType.Prefix, _harmony.Id);
                _getKeyDownMethod = null;
            }
            if (_wasPressedGetter != null)
            {
                _harmony.Unpatch(_wasPressedGetter, HarmonyPatchType.Prefix, _harmony.Id);
                _wasPressedGetter = null;
            }
        }
        catch
        {
        }
    }

    private static bool IsGameAssembly(Assembly assembly)
    {
        if (assembly == _ourAssembly)
        {
            return false;
        }

        string name = assembly.GetName().Name;
        if (name.StartsWith("UnityEngine")
            || name.StartsWith("Unity.")
            || name.StartsWith("System")
            || name.StartsWith("mscorlib")
            || name.StartsWith("Mono.")
            || name.StartsWith("BepInEx")
            || name.StartsWith("0Harmony"))
        {
            return false;
        }

        return true;
    }

    private static void AttachToGameInput(Type gameType, MethodBase triggerMethod)
    {
        if (_harmony == null || _attachedTypes.Contains(gameType))
        {
            return;
        }

        _attachedTypes.Add(gameType);

        int attachedCount = 0;
        MethodInfo[] methods = gameType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];
            if (method.IsGenericMethod || method.IsAbstract || _patchedMethods.Contains(method))
            {
                continue;
            }

            string name = method.Name;
            if (name.Contains("Mouse") || name.Contains("Cursor") || name.Contains("Pointer") || name.Contains("Scroll") || name.Contains("Wheel"))
            {
                continue;
            }

            Type returnType = method.ReturnType;
            ParameterInfo[] parameters = method.GetParameters();

            if (returnType == typeof(bool))
            {
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int) && name.Contains("Button"))
                {
                    continue;
                }

                if (name.StartsWith("Get") || name.Contains("Key") || name.Contains("Button") || name.Contains("Input") || name.Contains("Press"))
                {
                    if (PatchMethod(method, _suppressBoolPrefix))
                    {
                        attachedCount++;
                    }
                }
            }
            else if (returnType == typeof(float))
            {
                if (name.StartsWith("Get") && name.Contains("Axis") && !name.Contains("Modifier"))
                {
                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                    {
                        if (PatchMethod(method, _suppressAxisPrefix))
                        {
                            attachedCount++;
                        }
                    }
                    else
                    {
                        if (PatchMethod(method, _suppressFloatPrefix))
                        {
                            attachedCount++;
                        }
                    }
                }
            }
            else if (returnType == typeof(void))
            {
                if (name.Contains("Keyboard") || name.Contains("TakeInput") || name.Contains("ProcessInput"))
                {
                    if (PatchMethod(method, _suppressVoidPrefix))
                    {
                        attachedCount++;
                    }
                }
            }
        }

        if (triggerMethod is MethodInfo triggerMethodInfo && !_patchedMethods.Contains(triggerMethodInfo))
        {
            string trigName = triggerMethodInfo.Name;
            if (!trigName.Contains("Mouse") && !trigName.Contains("Scroll") && !trigName.Contains("Wheel"))
            {
                if (triggerMethodInfo.ReturnType == typeof(bool))
                {
                    if (PatchMethod(triggerMethodInfo, _suppressBoolPrefix))
                    {
                        attachedCount++;
                    }
                }
                else if (triggerMethodInfo.ReturnType == typeof(void))
                {
                    if (PatchMethod(triggerMethodInfo, _suppressVoidPrefix))
                    {
                        attachedCount++;
                    }
                }
            }
        }

        if (attachedCount > 0)
        {
            _discoveryComplete = true;
            DetachDiscoveryHooks();
            _logger?.LogInfo($"[ConfigDrawers] Attached keyboard suppression to {attachedCount} methods on {gameType.Name}.");
        }
    }

    private static bool PatchMethod(MethodInfo targetMethod, MethodInfo? prefix)
    {
        if (_harmony == null || prefix == null || _patchedMethods.Contains(targetMethod))
        {
            return false;
        }

        try
        {
            _patchedMethods.Add(targetMethod);
            _harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefix));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool SuppressBoolPrefix(ref bool __result)
    {
        if (ConfigDrawers.IsOpen)
        {
            __result = false;
            return false;
        }
        return true;
    }

    private static bool SuppressFloatPrefix(ref float __result)
    {
        if (ConfigDrawers.IsOpen)
        {
            __result = 0f;
            return false;
        }
        return true;
    }

    private static bool SuppressAxisPrefix(string name, ref float __result)
    {
        if (ConfigDrawers.IsOpen)
        {
            if (!string.IsNullOrEmpty(name) && (name.Contains("Mouse") || name.Contains("Scroll") || name.Contains("Wheel") || name.Contains("Cursor")))
            {
                return true;
            }

            __result = 0f;
            return false;
        }
        return true;
    }

    private static bool SuppressVoidPrefix()
    {
        if (ConfigDrawers.IsOpen)
        {
            return false;
        }
        return true;
    }
}
