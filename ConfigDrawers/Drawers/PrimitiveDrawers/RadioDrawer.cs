using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;

public static class RadioDrawer
{
    private const int MinRadioItems = 2;
    private const int MaxRadioItems = 4;

    public static bool ShouldUseRadio(SettingEntry entry)
    {
        if (entry == null)
        {
            return false;
        }

        if (entry.AcceptableValuesList != null)
        {
            var len = entry.AcceptableValuesList.Length;
            if (len < MinRadioItems || len > MaxRadioItems)
            {
                return false;
            }

            var maxAllowedLen = len switch
            {
                2 => 7,
                3 => 5,
                _ => 4
            };

            return entry.AcceptableValuesList.All(v =>
            {
                var s = v?.ToString();
                return s != null && s.Length > 0 && s.Length <= maxAllowedLen;
            });
        }

        if (entry.SettingType.IsEnum && !entry.SettingType.IsDefined(typeof(FlagsAttribute), false))
        {
            var names = Enum.GetNames(entry.SettingType);
            if (names.Length < MinRadioItems || names.Length > MaxRadioItems)
            {
                return false;
            }

            var maxAllowedLen = names.Length switch
            {
                2 => 7,
                3 => 5,
                _ => 4
            };

            return names.All(n => !string.IsNullOrEmpty(n) && n.Length <= maxAllowedLen);
        }

        return false;
    }

    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        var options = GetOptions(entry);
        var btnWidth = options.Count switch
        {
            2 => 54f,
            3 => 44f,
            _ => 36f
        };

        var totalWidth = (options.Count * btnWidth) + ((options.Count - 1) * 4f);
        var buttonMap = new List<(object Value, GameObject Button, TextMeshProUGUI? Text, Image Border, Image Fill)>();

        void RefreshStates(object current)
        {
            foreach (var item in buttonMap)
            {
                bool isSelected = Equals(item.Value, current) || string.Equals(item.Value?.ToString(), current?.ToString(), StringComparison.OrdinalIgnoreCase);
                Color border = isSelected ? CyberPalette.ColorIceBlue : CyberPalette.ColorBorderSubtle;
                Color text = isSelected ? CyberPalette.ColorIceBlueBright : CyberPalette.ColorTextMuted;
                Color fill = isSelected ? new Color(0.06f, 0.16f, 0.24f, 1f) : CyberPalette.ColorCardSurface;
                Color hoverBorder = isSelected ? Color.white : CyberPalette.ColorIceBlueBright;
                Color hoverText = isSelected ? Color.white : CyberPalette.ColorIceBlueBright;

                if (item.Text != null) { item.Text.color = text; }
                item.Border.color = border;
                item.Fill.color = fill;

                CyberHoverHandler hover = item.Button.GetComponent<CyberHoverHandler>();
                if (hover != null)
                {
                    hover.SetNormalColors(border, text, normalFill: fill, hoverBorder: hoverBorder, hoverText: hoverText);
                }
            }
        }

        var rowObj = DrawerDispatcher.CreateRowContainer(parent, entry, out var valueArea, () =>
        {
            RefreshStates(entry.ConfigEntry.BoxedValue);
        }, totalWidth);

        var groupObj = new GameObject("RadioGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        groupObj.transform.SetParent(valueArea, false);

        var groupRT = groupObj.GetComponent<RectTransform>();
        groupRT.sizeDelta = new Vector2(totalWidth, 22f);

        var groupLE = groupObj.GetComponent<LayoutElement>();
        groupLE.minWidth = totalWidth;
        groupLE.preferredWidth = totalWidth;
        groupLE.flexibleWidth = 0f;
        groupLE.minHeight = 22f;
        groupLE.preferredHeight = 22f;
        groupLE.flexibleHeight = 0f;

        var hlg = groupObj.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4f;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        foreach (var opt in options)
        {
            var optName = opt.ToString() ?? string.Empty;
            var optVal = opt;

            var btn = UiFactory.CreateCyberButton(groupObj.transform, $"Radio_{optName}", optName, () =>
            {
                if (!entry.CanEdit)
                {
                    return;
                }

                entry.SetValue(optVal);
                RefreshStates(optVal);
            }, CyberPalette.ColorBorderSubtle, CyberPalette.ColorTextMuted, btnWidth, 22f);

            var borderImg = btn.GetComponent<Image>();
            var fillImg = (btn.transform.Find("Fill") ?? btn.transform).GetComponent<Image>();
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();

            if (txt != null)
            {
                txt.enableAutoSizing = true;
                txt.fontSizeMin = 7f;
                txt.fontSizeMax = 9.5f;
                txt.overflowMode = TextOverflowModes.Ellipsis;
                txt.textWrappingMode = TextWrappingModes.NoWrap;
            }

            buttonMap.Add((optVal, btn, txt, borderImg, fillImg));
        }

        RefreshStates(entry.ConfigEntry.BoxedValue);

        groupObj.transform.SetAsFirstSibling();

        return rowObj;
    }

    private static List<object> GetOptions(SettingEntry entry)
    {
        if (entry.AcceptableValuesList != null)
        {
            return entry.AcceptableValuesList.ToList();
        }

        if (entry.SettingType.IsEnum)
        {
            return Enum.GetValues(entry.SettingType).Cast<object>().ToList();
        }

        return new List<object>();
    }
}
