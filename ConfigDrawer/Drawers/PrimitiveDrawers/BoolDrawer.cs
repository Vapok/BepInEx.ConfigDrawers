using System;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;

public static class BoolDrawer
{
    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        GameObject? btnObj = null;

        var rowObj = DrawerDispatcher.CreateRowContainer(parent, entry, out var valueArea, () =>
        {
            var cur = entry.ConfigEntry.BoxedValue is bool b && b;
            if (btnObj != null)
            {
                UpdateDisplay(btnObj, cur);
            }
        });

        var currentVal = entry.ConfigEntry.BoxedValue is bool val && val;
        var btnText = currentVal ? "[ ON ]" : "[ OFF ]";
        var borderColor = currentVal ? CyberPalette.ColorGlacialMint : CyberPalette.ColorBorderSubtle;
        var textColor = currentVal ? CyberPalette.ColorGlacialMint : CyberPalette.ColorTextMuted;

        btnObj = UiFactory.CreateCyberButton(valueArea, "ToggleBtn", btnText, () =>
        {
            var next = !(entry.ConfigEntry.BoxedValue is bool v && v);
            entry.SetValue(next);
            if (btnObj != null)
            {
                UpdateDisplay(btnObj, next);
            }
        }, borderColor, textColor, 60f, 22f);

        btnObj.transform.SetAsFirstSibling();

        return rowObj;
    }

    private static void UpdateDisplay(GameObject btnObj, bool state)
    {
        var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        var img = btnObj.GetComponent<Image>();
        if (tmp != null)
        {
            tmp.text = state ? "[ ON ]" : "[ OFF ]";
            tmp.color = state ? CyberPalette.ColorGlacialMint : CyberPalette.ColorTextMuted;
        }

        if (img != null)
        {
            img.color = state ? CyberPalette.ColorGlacialMint : CyberPalette.ColorBorderSubtle;
        }
    }
}
