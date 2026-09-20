using System;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.JsonDrawer;

public static class JsonEditorDrawer
{
    public static bool CanDraw(SettingEntry entry)
    {
        if (entry == null || entry.ConfigEntry == null || entry.SettingType != typeof(string))
        {
            return false;
        }

        var text = entry.ConfigEntry?.BoxedValue as string;
        if (text == null || text.Trim().Length == 0)
        {
            return false;
        }

        var trimmed = text.Trim();
        return (trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
               (trimmed.StartsWith("[") && trimmed.EndsWith("]"));
    }

    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        var panel = UiFactory.CreatePanel(parent, $"Json_{entry.Key}", CyberPalette.ColorBorderCard, CyberPalette.ColorCardSurface, 1f);
        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        var csf = panel.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var headerRow = new GameObject("HeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        headerRow.transform.SetParent(panel.transform, false);
        var hlg = headerRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        UiFactory.CreateLabel(headerRow.transform, "Title", $"{entry.DispName} [ JSON OBJECT ]", CyberPalette.ColorIceBlueBright, 11f);

        var (codeRoot, inputField) = UiFactory.CreateInputField(panel.transform, "CodeInput", entry.EditBuffer, text =>
        {
            entry.UpdateBuffer(text);
            entry.CommitBuffer();
        }, -1f, 90f);

        inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
        inputField.onValueChanged.AddListener(val =>
        {
            entry.UpdateBuffer(val);
        });

        return panel;
    }
}
