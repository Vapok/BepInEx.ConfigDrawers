using System;
using BepInEx.Configuration;
using BepInEx.ConfigDrawers.Drawers.JsonDrawer;
using BepInEx.ConfigDrawers.Drawers.LegacyBridge;
using BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;
using BepInEx.ConfigDrawers.Drawers.StructuredDrawers;
using BepInEx.ConfigDrawers.Models;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Drawers;

public static class DrawerDispatcher
{
    public static GameObject DrawSetting(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        if (entry.CustomDrawer != null)
        {
            var bridgeObj = new GameObject($"CustomDrawer_{entry.Key}");
            bridgeObj.transform.SetParent(parent, false);
            var bridge = bridgeObj.AddComponent<LegacyImguiBridge>();
            bridge.Bind(entry);
            return bridgeObj;
        }

        if (DataGridDrawer.CanDraw(entry))
        {
            return DataGridDrawer.Draw(parent, entry);
        }

        if (JsonEditorDrawer.CanDraw(entry))
        {
            return JsonEditorDrawer.Draw(parent, entry);
        }

        if (entry.SettingType == typeof(bool))
        {
            return BoolDrawer.Draw(parent, entry);
        }

        if (entry.SettingType.IsEnum)
        {
            return EnumDrawer.Draw(parent, entry);
        }

        if (entry.SettingType == typeof(KeyboardShortcut))
        {
            return ShortcutDrawer.Draw(parent, entry);
        }

        if (entry.SettingType == typeof(Color))
        {
            return ColorDrawer.Draw(parent, entry);
        }

        if (IsNumericType(entry.SettingType))
        {
            return NumberDrawer.Draw(parent, entry);
        }

        return TextDrawer.Draw(parent, entry);
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) ||
               type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(long) ||
               type == typeof(short) ||
               type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(uint) ||
               type == typeof(ulong) ||
               type == typeof(ushort);
    }
}
