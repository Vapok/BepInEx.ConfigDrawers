using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.StructuredDrawers;

public static class DataGridDrawer
{
    public static bool CanDraw(SettingEntry entry)
    {
        if (entry == null || entry.ConfigEntry == null || entry.SettingType != typeof(string))
        {
            return false;
        }

        var text = entry.ConfigEntry?.BoxedValue as string;
        if (text == null || text.Length == 0)
        {
            return false;
        }

        return text.Contains(":") && (text.Contains(",") || text.Contains("\n"));
    }

    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        var container = UiFactory.CreatePanel(parent, $"Grid_{entry.Key}", CyberPalette.ColorBorderCard, CyberPalette.ColorCardSurface, 1f);
        var layout = container.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        var csf = container.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var headerRow = UiFactory.CreateLabel(container.transform, "Header", $"{entry.DispName} [ DATA GRID ]", CyberPalette.ColorIceBlueBright, 11f);

        var raw = entry.ConfigEntry?.BoxedValue as string ?? string.Empty;
        var rows = raw.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(r => r.Trim())
                      .Where(r => !string.IsNullOrEmpty(r))
                      .ToList();

        for (int i = 0; i < rows.Count; i++)
        {
            var rowIndex = i;
            RenderDataRow(container.transform, rows, rowIndex, entry);
        }

        UiFactory.CreateCyberButton(container.transform, "AddRowBtn", "[ + ADD ROW ]", () =>
        {
            rows.Add("Item:1");
            entry.SetValue(string.Join(",", rows));
            // Re-render
        }, CyberPalette.ColorGlacialMint, CyberPalette.ColorGlacialMint, 120f, 22f);

        return container;
    }

    private static void RenderDataRow(Transform parent, List<string> rows, int index, SettingEntry entry)
    {
        var rowStr = rows[index];
        var parts = rowStr.Split(':');
        var namePart = parts.Length > 0 ? parts[0] : "";
        var amountPart = parts.Length > 1 ? parts[1] : "1";

        var rowObj = new GameObject($"Row_{index}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowObj.transform.SetParent(parent, false);

        var hlg = rowObj.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        var le = rowObj.AddComponent<LayoutElement>();
        le.minHeight = 24f;
        le.preferredHeight = 24f;

        UiFactory.CreateInputField(rowObj.transform, "ColName", namePart, newName =>
        {
            parts[0] = newName;
            rows[index] = string.Join(":", parts);
            entry.SetValue(string.Join(",", rows));
        }, 180f, 24f);

        UiFactory.CreateInputField(rowObj.transform, "ColAmount", amountPart, newAmount =>
        {
            if (parts.Length > 1)
            {
                parts[1] = newAmount;
            }
            rows[index] = string.Join(":", parts);
            entry.SetValue(string.Join(",", rows));
        }, 60f, 24f);

        UiFactory.CreateCyberButton(rowObj.transform, "DeleteBtn", "[ X ]", () =>
        {
            rows.RemoveAt(index);
            entry.SetValue(string.Join(",", rows));
        }, CyberPalette.ColorErrorRed, CyberPalette.ColorErrorRed, 30f, 22f);
    }
}
