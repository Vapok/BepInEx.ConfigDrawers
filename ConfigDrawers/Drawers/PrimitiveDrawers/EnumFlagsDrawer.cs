using System;
using System.Linq;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;

public static class EnumFlagsDrawer
{
    private const float MinButtonWidth = 120f;
    private const float MaxButtonWidth = 160f;
    private const float ButtonHeight = 22f;
    private const float CharWidthFactor = 8f;
    private const float PaddingOffset = 36f;

    public static bool IsEnumFlags(SettingEntry entry)
    {
        if (entry == null)
        {
            return false;
        }

        Type settingType = entry.SettingType;
        Type actualType = Nullable.GetUnderlyingType(settingType) ?? settingType;

        return actualType.IsEnum && actualType.IsDefined(typeof(FlagsAttribute), false);
    }

    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        Type enumType = Nullable.GetUnderlyingType(entry.SettingType) ?? entry.SettingType;
        string[] names;
        Array values;

        if (entry.AcceptableValuesList != null)
        {
            names = entry.AcceptableValuesList.Select(v => v?.ToString() ?? string.Empty).ToArray();
            values = entry.AcceptableValuesList;
        }
        else
        {
            names = Enum.GetNames(enumType);
            values = Enum.GetValues(enumType);
        }

        int maxLen = names.Length > 0 ? names.Max(n => n.Length) : 6;
        float btnWidth = Mathf.Clamp((maxLen * CharWidthFactor) + PaddingOffset, MinButtonWidth, MaxButtonWidth);

        GameObject? btnObj = null;

        GameObject rowObj = DrawerDispatcher.CreateRowContainer(parent, entry, out Transform valueArea, () =>
        {
            if (btnObj != null)
            {
                UpdateLabel(btnObj, entry);
            }
        }, btnWidth);

        string labelText = $"{GetButtonLabel(entry)}  ▼";

        btnObj = UiFactory.CreateCyberButton(valueArea, "EnumFlagsDropdownBtn", labelText, () =>
        {
            if (!entry.CanEdit || btnObj == null)
            {
                return;
            }

            RectTransform btnRT = btnObj.GetComponent<RectTransform>();

            CyberDropdownOverlay.ShowMulti(btnRT, names, idx =>
            {
                if (idx < 0 || idx >= values.Length)
                {
                    return false;
                }

                object? itemVal = values.GetValue(idx);
                if (itemVal == null)
                {
                    return false;
                }

                ulong itemMask = ToUInt64(itemVal);
                ulong currentMask = ToUInt64(entry.ConfigEntry.BoxedValue);

                if (itemMask == 0UL)
                {
                    return currentMask == 0UL;
                }

                return (currentMask & itemMask) == itemMask;
            },
            idx =>
            {
                if (idx < 0 || idx >= values.Length)
                {
                    return;
                }

                object? itemVal = values.GetValue(idx);
                if (itemVal == null)
                {
                    return;
                }

                ulong itemMask = ToUInt64(itemVal);
                ulong currentMask = ToUInt64(entry.ConfigEntry.BoxedValue);

                if (itemMask == 0UL)
                {
                    currentMask = 0UL;
                }
                else
                {
                    if ((currentMask & itemMask) == itemMask)
                    {
                        currentMask &= ~itemMask;
                    }
                    else
                    {
                        currentMask |= itemMask;
                    }
                }

                object newBoxedVal = ToEnumValue(enumType, currentMask);
                entry.SetValue(newBoxedVal);

                if (btnObj != null)
                {
                    UpdateLabel(btnObj, entry);
                }
            });
        }, CyberPalette.ColorCyberTeal, CyberPalette.ColorIceBlueBright, btnWidth, ButtonHeight);

        TextMeshProUGUI? btnTmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnTmp != null)
        {
            btnTmp.overflowMode = TextOverflowModes.Ellipsis;
        }

        Button? btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = entry.CanEdit;
        }

        return rowObj;
    }

    private static void UpdateLabel(GameObject btnObj, SettingEntry entry)
    {
        TextMeshProUGUI? tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = $"{GetButtonLabel(entry)}  ▼";
        }
    }

    public static string GetButtonLabel(SettingEntry entry)
    {
        object? cur = entry.ConfigEntry.BoxedValue;
        if (cur == null)
        {
            return "None";
        }

        string str = cur.ToString();
        if (string.IsNullOrEmpty(str) || str == "0")
        {
            return "None";
        }

        return str;
    }

    public static ulong ToUInt64(object? enumValue)
    {
        if (enumValue == null)
        {
            return 0UL;
        }

        Type type = enumValue.GetType();
        Type underlying = type.IsEnum ? Enum.GetUnderlyingType(type) : type;
        TypeCode code = Type.GetTypeCode(underlying);

        return code switch
        {
            TypeCode.SByte => (ulong)(byte)(sbyte)enumValue,
            TypeCode.Byte => (ulong)(byte)enumValue,
            TypeCode.Int16 => (ulong)(ushort)(short)enumValue,
            TypeCode.UInt16 => (ulong)(ushort)enumValue,
            TypeCode.Int32 => (ulong)(uint)(int)enumValue,
            TypeCode.UInt32 => (ulong)(uint)enumValue,
            TypeCode.Int64 => (ulong)(long)enumValue,
            TypeCode.UInt64 => (ulong)enumValue,
            _ => Convert.ToUInt64(enumValue)
        };
    }

    public static object ToEnumValue(Type enumType, ulong mask)
    {
        Type underlying = Enum.GetUnderlyingType(enumType);
        TypeCode code = Type.GetTypeCode(underlying);

        return code switch
        {
            TypeCode.SByte => Enum.ToObject(enumType, (sbyte)(byte)mask),
            TypeCode.Byte => Enum.ToObject(enumType, (byte)mask),
            TypeCode.Int16 => Enum.ToObject(enumType, (short)(ushort)mask),
            TypeCode.UInt16 => Enum.ToObject(enumType, (ushort)mask),
            TypeCode.Int32 => Enum.ToObject(enumType, (int)(uint)mask),
            TypeCode.UInt32 => Enum.ToObject(enumType, (uint)mask),
            TypeCode.Int64 => Enum.ToObject(enumType, (long)mask),
            TypeCode.UInt64 => Enum.ToObject(enumType, mask),
            _ => Enum.ToObject(enumType, mask)
        };
    }
}
