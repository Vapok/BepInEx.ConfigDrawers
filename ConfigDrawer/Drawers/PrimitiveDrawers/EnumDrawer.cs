using System;
using ConfigDrawer.Components;
using ConfigDrawer.Models;
using ConfigDrawer.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ConfigDrawer.Drawers.PrimitiveDrawers;

public static class EnumDrawer
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

        var currentVal = entry.ConfigEntry.BoxedValue;
        var labelText = currentVal?.ToString() ?? "None";

        var btn = UiFactory.CreateCyberButton(targetParent, "EnumBtn", $"[ {labelText} ]", () =>
        {
            CycleNext(entry, targetParent);
        }, CyberPalette.ColorCyberTeal, CyberPalette.ColorIceBlueBright, 130f, 24f);

        return rowObj;
    }

    private static void CycleNext(SettingEntry entry, Transform container)
    {
        var values = Enum.GetValues(entry.SettingType);
        if (values.Length == 0)
        {
            return;
        }

        var currentIndex = Array.IndexOf(values, entry.ConfigEntry.BoxedValue);
        var nextIndex = (currentIndex + 1) % values.Length;
        var nextVal = values.GetValue(nextIndex);
        if (nextVal != null)
        {
            entry.SetValue(nextVal);
            var tmp = container.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = $"[ {nextVal} ]";
            }
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
        labelRT.anchorMax = new Vector2(0.55f, 1f);
        labelRT.offsetMin = new Vector2(10f, 0f);
        labelRT.offsetMax = Vector2.zero;

        var valueArea = new GameObject("ValueArea", typeof(RectTransform));
        valueArea.transform.SetParent(target, false);
        var valueRT = valueArea.GetComponent<RectTransform>();
        valueRT.anchorMin = new Vector2(0.55f, 0f);
        valueRT.anchorMax = new Vector2(1f, 1f);
        valueRT.offsetMin = Vector2.zero;
        valueRT.offsetMax = new Vector2(-10f, 0f);

        return row;
    }
}
