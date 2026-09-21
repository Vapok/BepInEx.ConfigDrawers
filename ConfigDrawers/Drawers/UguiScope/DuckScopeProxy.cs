using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Drawers.UguiScope;

public static class DuckScopeProxy
{
    private static readonly Dictionary<Type, Type> _proxyTypeCache = new();
    private static ModuleBuilder? _moduleBuilder;
    private static readonly object _lock = new();

    private static ModuleBuilder GetModuleBuilder()
    {
        if (_moduleBuilder != null)
        {
            return _moduleBuilder;
        }

        AssemblyName assemblyName = new AssemblyName("BepInEx.ConfigDrawers.DynamicProxies");
        AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        _moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
        return _moduleBuilder;
    }

    public static object GetOrCreateProxy(Type targetInterfaceType, UguiDrawerScope scope)
    {
        if (targetInterfaceType == null)
        {
            throw new ArgumentNullException(nameof(targetInterfaceType));
        }

        if (scope == null)
        {
            throw new ArgumentNullException(nameof(scope));
        }

        if (targetInterfaceType.IsAssignableFrom(typeof(UguiDrawerScope)))
        {
            return scope;
        }

        Type proxyType;
        lock (_lock)
        {
            if (!_proxyTypeCache.TryGetValue(targetInterfaceType, out proxyType!))
            {
                proxyType = GenerateProxyType(targetInterfaceType);
                _proxyTypeCache[targetInterfaceType] = proxyType;
            }
        }

        return Activator.CreateInstance(proxyType, scope);
    }

    public static void InvokeScopeDelegate(Delegate del, UguiDrawerScope scope, ConfigEntryBase? configEntry)
    {
        if (del == null || scope == null)
        {
            return;
        }

        ParameterInfo[] parameters = del.Method.GetParameters();
        if (parameters.Length == 0)
        {
            del.DynamicInvoke();
            return;
        }

        object firstArg;
        Type firstParamType = parameters[0].ParameterType;

        if (firstParamType == typeof(Transform))
        {
            firstArg = scope.Container;
        }
        else if (firstParamType.IsInterface && !firstParamType.IsAssignableFrom(typeof(UguiDrawerScope)))
        {
            firstArg = GetOrCreateProxy(firstParamType, scope);
        }
        else
        {
            firstArg = scope;
        }

        if (parameters.Length == 1)
        {
            del.DynamicInvoke(firstArg);
        }
        else if (parameters.Length == 2)
        {
            del.DynamicInvoke(firstArg, configEntry);
        }
        else
        {
            del.DynamicInvoke(firstArg);
        }
    }

    private static Type GenerateProxyType(Type interfaceType)
    {
        ModuleBuilder mb = GetModuleBuilder();
        string typeName = $"Proxy_{interfaceType.Namespace?.Replace('.', '_')}_{interfaceType.Name}_{Guid.NewGuid():N}";
        TypeBuilder tb = mb.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class, typeof(object), new[] { interfaceType });

        FieldBuilder targetField = tb.DefineField("_target", typeof(UguiDrawerScope), FieldAttributes.Private | FieldAttributes.InitOnly);

        ConstructorBuilder ctor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(UguiDrawerScope) });
        ILGenerator ctorIl = ctor.GetILGenerator();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Ldarg_1);
        ctorIl.Emit(OpCodes.Stfld, targetField);
        ctorIl.Emit(OpCodes.Ret);

        List<Type> allInterfaces = new List<Type> { interfaceType };
        allInterfaces.AddRange(interfaceType.GetInterfaces());

        HashSet<string> implementedMethods = new HashSet<string>();

        foreach (Type iface in allInterfaces)
        {
            foreach (PropertyInfo prop in iface.GetProperties())
            {
                MethodInfo? getMethod = prop.GetGetMethod();
                if (getMethod != null && !implementedMethods.Contains(getMethod.Name))
                {
                    implementedMethods.Add(getMethod.Name);
                    MethodBuilder mbProp = tb.DefineMethod(getMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual, getMethod.ReturnType, Type.EmptyTypes);
                    ILGenerator pil = mbProp.GetILGenerator();
                    pil.Emit(OpCodes.Ldarg_0);
                    pil.Emit(OpCodes.Ldfld, targetField);

                    PropertyInfo? targetProp = typeof(UguiDrawerScope).GetProperty(prop.Name);
                    if (targetProp != null && targetProp.GetGetMethod() != null)
                    {
                        pil.Emit(OpCodes.Callvirt, targetProp.GetGetMethod()!);
                    }
                    else
                    {
                        EmitDefaultValue(pil, getMethod.ReturnType);
                    }
                    pil.Emit(OpCodes.Ret);
                    tb.DefineMethodOverride(mbProp, getMethod);
                }
            }

            foreach (MethodInfo method in iface.GetMethods())
            {
                if (method.IsSpecialName || implementedMethods.Contains(method.Name))
                {
                    continue;
                }

                ParameterInfo[] pars = method.GetParameters();
                Type[] paramTypes = pars.Select(p => p.ParameterType).ToArray();

                MethodBuilder mbMethod = tb.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.Virtual, method.ReturnType, paramTypes);
                ILGenerator mil = mbMethod.GetILGenerator();
                mil.Emit(OpCodes.Ldarg_0);
                mil.Emit(OpCodes.Ldfld, targetField);

                for (int i = 0; i < pars.Length; i++)
                {
                    mil.Emit(OpCodes.Ldarg, i + 1);
                }

                MethodInfo? targetMethod = typeof(UguiDrawerScope).GetMethod(method.Name, paramTypes);
                if (targetMethod != null)
                {
                    mil.Emit(OpCodes.Callvirt, targetMethod);
                }
                else
                {
                    mil.Emit(OpCodes.Pop);
                    EmitDefaultValue(mil, method.ReturnType);
                }

                mil.Emit(OpCodes.Ret);
                tb.DefineMethodOverride(mbMethod, method);
            }
        }

        return tb.CreateType();
    }

    private static void EmitDefaultValue(ILGenerator il, Type returnType)
    {
        if (returnType == typeof(void))
        {
            return;
        }

        if (returnType.IsValueType)
        {
            LocalBuilder local = il.DeclareLocal(returnType);
            il.Emit(OpCodes.Ldloca_S, local);
            il.Emit(OpCodes.Initobj, returnType);
            il.Emit(OpCodes.Ldloc, local);
        }
        else
        {
            il.Emit(OpCodes.Ldnull);
        }
    }
}
