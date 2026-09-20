using System;
using System.Linq;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;

public static class EnumDrawer
{
    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        string[] names;
        Array values;

        if (entry.AcceptableValuesList != null)
        {
            names = entry.AcceptableValuesList.Select(v => v?.ToString() ?? string.Empty).ToArray();
            values = entry.AcceptableValuesList;
        }
        else
        {
            names = Enum.GetNames(entry.SettingType);
            values = Enum.GetValues(entry.SettingType);
        }

        var maxLen = names.Length > 0 ? names.Max(n => n.Length) : 6;
        var btnWidth = Mathf.Clamp((maxLen * 6.8f) + 28f, 105f, 150f);

        GameObject? btnObj = null;

        var rowObj = DrawerDispatcher.CreateRowContainer(parent, entry, out var valueArea, () =>
        {
            if (btnObj != null)
            {
                var cur = entry.ConfigEntry.BoxedValue?.ToString() ?? "None";
                var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = $"{cur}  ▼";
                }
            }
        }, btnWidth);

        var currentVal = entry.ConfigEntry.BoxedValue;
        var labelText = $"{currentVal?.ToString() ?? "None"}  ▼";

        btnObj = UiFactory.CreateCyberButton(valueArea, "EnumDropdownBtn", labelText, () =>
        {
            if (!entry.CanEdit || btnObj == null)
            {
                return;
            }

            var currentIndex = Array.IndexOf(values, entry.ConfigEntry.BoxedValue);

            CyberDropdownOverlay.Show(btnObj.GetComponent<RectTransform>(), names, currentIndex, selectedIndex =>
            {
                if (selectedIndex >= 0 && selectedIndex < values.Length)
                {
                    var chosenVal = values.GetValue(selectedIndex);
                    if (chosenVal != null)
                    {
                        entry.SetValue(chosenVal);
                        var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                        if (tmp != null)
                        {
                            tmp.text = $"{chosenVal}  ▼";
                        }
                    }
                }
            });
        }, CyberPalette.ColorCyberTeal, CyberPalette.ColorIceBlueBright, btnWidth, 22f);

        var btnTmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnTmp != null)
        {
            btnTmp.overflowMode = TextOverflowModes.Ellipsis;
        }

        var btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = entry.CanEdit;
        }

        return rowObj;
    }
}
