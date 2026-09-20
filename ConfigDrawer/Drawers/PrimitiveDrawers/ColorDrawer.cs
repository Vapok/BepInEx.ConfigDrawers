using System;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;

public static class ColorDrawer
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

        var currentColor = entry.ConfigEntry.BoxedValue is Color c ? c : Color.white;
        var hex = "#" + ColorUtility.ToHtmlStringRGBA(currentColor);

        var preview = UiFactory.CreatePanel(targetParent, "Preview", CyberPalette.ColorIceBlue, currentColor, 1f);
        var previewLayout = preview.AddComponent<LayoutElement>();
        previewLayout.minWidth = 24f;
        previewLayout.preferredWidth = 24f;
        previewLayout.minHeight = 24f;
        previewLayout.preferredHeight = 24f;

        var (inputRoot, _) = UiFactory.CreateInputField(targetParent, "HexInput", hex, text =>
        {
            if (ColorUtility.TryParseHtmlString(text.StartsWith("#") ? text : "#" + text, out var parsed))
            {
                entry.SetValue(parsed);
                var fill = preview.transform.Find("Fill");
                var img = fill?.GetComponent<Image>();
                if (img != null)
                {
                    img.color = parsed;
                }
            }
        }, 90f, 24f);

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

        var valueArea = new GameObject("ValueArea", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        valueArea.transform.SetParent(target, false);
        var valueRT = valueArea.GetComponent<RectTransform>();
        valueRT.anchorMin = new Vector2(0.55f, 0f);
        valueRT.anchorMax = new Vector2(1f, 1f);
        valueRT.offsetMin = Vector2.zero;
        valueRT.offsetMax = new Vector2(-10f, 0f);

        var hlg = valueArea.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        return row;
    }
}
