using System;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;

public static class EnumDrawer
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
            if (btnObj != null)
            {
                var cur = entry.ConfigEntry.BoxedValue?.ToString() ?? "None";
                var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = $"[ {cur} ]";
                }
            }
        });

        var currentVal = entry.ConfigEntry.BoxedValue;
        var labelText = currentVal?.ToString() ?? "None";

        btnObj = UiFactory.CreateCyberButton(valueArea, "EnumBtn", $"[ {labelText} ]", () =>
        {
            CycleNext(entry, btnObj);
        }, CyberPalette.ColorCyberTeal, CyberPalette.ColorIceBlueBright, 110f, 22f);

        btnObj.transform.SetAsFirstSibling();

        return rowObj;
    }

    private static void CycleNext(SettingEntry entry, GameObject? btnObj)
    {
        var values = Enum.GetValues(entry.SettingType);
        if (values.Length == 0 || btnObj == null)
        {
            return;
        }

        var currentIndex = Array.IndexOf(values, entry.ConfigEntry.BoxedValue);
        var nextIndex = (currentIndex + 1) % values.Length;
        var nextVal = values.GetValue(nextIndex);
        if (nextVal != null)
        {
            entry.SetValue(nextVal);
            var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = $"[ {nextVal} ]";
            }
        }
    }
}
