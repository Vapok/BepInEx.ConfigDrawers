using System;
using System.Globalization;
using BepInEx.Configuration;
using ConfigDrawer.Components;
using ConfigDrawer.Models;
using ConfigDrawer.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ConfigDrawer.Drawers.PrimitiveDrawers;

public static class NumberDrawer
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

        var (inputRoot, inputField) = UiFactory.CreateInputField(targetParent, "Input", entry.EditBuffer, text =>
        {
            entry.UpdateBuffer(text);
            entry.CommitBuffer();
        }, 80f, 24f);

        inputField.onValueChanged.AddListener(val =>
        {
            entry.UpdateBuffer(val);
        });

        return rowObj;
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
