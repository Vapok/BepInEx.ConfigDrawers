using System;
using ConfigDrawer.Components;
using ConfigDrawer.Models;
using ConfigDrawer.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ConfigDrawer.Drawers.PrimitiveDrawers;

public static class BoolDrawer
{
    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        var rowObj = CreateRowContainer(parent, entry);
        var valueArea = rowObj.transform.Find("ValueArea");
        var targetParent = valueArea != null ? valueArea : rowObj.transform;

        var currentVal = entry.ConfigEntry.BoxedValue is bool b && b;
        var btnText = currentVal ? "[ ON ]" : "[ OFF ]";
        var borderColor = currentVal ? CyberPalette.ColorGlacialMint : CyberPalette.ColorBorderSubtle;
        var textColor = currentVal ? CyberPalette.ColorGlacialMint : CyberPalette.ColorTextMuted;

        var btn = UiFactory.CreateCyberButton(targetParent, "ToggleBtn", btnText, () =>
        {
            var next = !(entry.ConfigEntry.BoxedValue is bool val && val);
            entry.SetValue(next);
            UpdateDisplay(targetParent, next);
        }, borderColor, textColor, 70f, 24f);

        return rowObj;
    }

    private static void UpdateDisplay(Transform parent, bool state)
    {
        var tmp = parent.GetComponentInChildren<TextMeshProUGUI>();
        var img = parent.GetComponentInChildren<Image>();
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

    private static GameObject CreateRowContainer(Transform parent, SettingEntry entry)
    {
        var row = UiFactory.CreatePanel(parent, $"Row_{entry.Key}", CyberPalette.ColorBorderCard, CyberPalette.ColorCardSurface, 1f);
        var layout = row.AddComponent<LayoutElement>();
        layout.minHeight = 32f;
        layout.preferredHeight = 32f;

        var hover = row.AddComponent<HoverCardHandler>();
        hover.Bind(entry);

        var fill = row.transform.Find("Fill");
        var target = fill != null ? fill : row.transform;

        var label = UiFactory.CreateLabel(target, "Label", entry.DispName, entry.EntryColor, 11f);
        var labelRT = label.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 0f);
        labelRT.anchorMax = new Vector2(0.6f, 1f);
        labelRT.offsetMin = new Vector2(10f, 0f);
        labelRT.offsetMax = Vector2.zero;

        var valueArea = new GameObject("ValueArea", typeof(RectTransform));
        valueArea.transform.SetParent(target, false);
        var valueRT = valueArea.GetComponent<RectTransform>();
        valueRT.anchorMin = new Vector2(0.6f, 0f);
        valueRT.anchorMax = new Vector2(1f, 1f);
        valueRT.offsetMin = Vector2.zero;
        valueRT.offsetMax = new Vector2(-10f, 0f);

        return row;
    }
}
