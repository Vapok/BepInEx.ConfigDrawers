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

        GameObject rowObj = DrawerDispatcher.CreateRowContainer(parent, entry, out Transform valueArea, () =>
        {
            bool cur = entry.ConfigEntry.BoxedValue is bool b && b;
            if (btnObj != null)
            {
                UpdateDisplay(btnObj, cur);
            }
        });

        bool currentVal = entry.ConfigEntry.BoxedValue is bool val && val;
        string btnText = currentVal ? "ON" : "OFF";
        Color borderColor = currentVal ? CyberPalette.ColorGlacialMint : CyberPalette.ColorBorderSubtle;
        Color textColor = currentVal ? CyberPalette.ColorGlacialMint : CyberPalette.ColorTextMuted;

        btnObj = UiFactory.CreateCyberButton(valueArea, "ToggleBtn", btnText, () =>
        {
            bool next = !(entry.ConfigEntry.BoxedValue is bool v && v);
            entry.SetValue(next);
            if (btnObj != null)
            {
                UpdateDisplay(btnObj, next);
            }
        }, borderColor, textColor, 44f, 22f);

        UpdateDisplay(btnObj, currentVal);

        btnObj.transform.SetAsFirstSibling();

        return rowObj;
    }

    private static void UpdateDisplay(GameObject btnObj, bool state)
    {
        TextMeshProUGUI tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        Image img = btnObj.GetComponent<Image>();
        CyberHoverHandler hover = btnObj.GetComponent<CyberHoverHandler>();

        Color borderColor = state ? CyberPalette.ColorGlacialMint : CyberPalette.ColorBorderSubtle;
        Color textColor = state ? CyberPalette.ColorGlacialMint : CyberPalette.ColorTextMuted;
        Color hoverBorder = state ? Color.Lerp(CyberPalette.ColorGlacialMint, Color.white, 0.35f) : CyberPalette.ColorIceBlueBright;
        Color hoverText = state ? Color.white : CyberPalette.ColorIceBlueBright;

        if (tmp != null)
        {
            tmp.text = state ? "ON" : "OFF";
            tmp.color = textColor;
        }

        if (img != null)
        {
            img.color = borderColor;
        }

        if (hover != null)
        {
            hover.SetNormalColors(borderColor, textColor, hoverBorder: hoverBorder, hoverText: hoverText);
        }
    }
}
