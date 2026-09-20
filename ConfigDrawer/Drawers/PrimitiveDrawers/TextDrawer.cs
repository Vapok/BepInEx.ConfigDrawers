using System;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;

public static class TextDrawer
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

        GameObject inputRoot = null!;
        TMP_InputField inputField = null!;
        (inputRoot, inputField) = UiFactory.CreateInputField(targetParent, "Input", entry.EditBuffer, text =>
        {
            entry.UpdateBuffer(text);
            entry.CommitBuffer();
            if (inputRoot != null)
            {
                UpdateVisuals(inputRoot, entry);
            }
        }, 140f, 24f);

        inputField.onValueChanged.AddListener(val =>
        {
            entry.UpdateBuffer(val);
            UpdateVisuals(inputRoot, entry);
        });

        return rowObj;
    }

    private static void UpdateVisuals(GameObject inputRoot, SettingEntry entry)
    {
        var img = inputRoot.GetComponent<Image>();
        if (img == null)
        {
            return;
        }

        if (!entry.IsValid)
        {
            img.color = CyberPalette.ColorErrorRed;
        }
        else if (entry.IsDirty)
        {
            img.color = CyberPalette.ColorWarningAmber;
        }
        else
        {
            img.color = CyberPalette.ColorBorderSubtle;
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
