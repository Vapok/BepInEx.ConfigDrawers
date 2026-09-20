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

        var container = UiFactory.CreatePanel(parent, $"Grid_{entry.Key}", CyberPalette.ColorBorderCard, CyberPalette.ColorVoidBlack, 1f);
        var layout = container.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 3f;
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var csf = container.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var raw = entry.ConfigEntry?.BoxedValue as string ?? string.Empty;
        var rows = raw.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(r => r.Trim())
                      .Where(r => !string.IsNullOrEmpty(r))
                      .ToList();

        var headerRow = UiFactory.CreateLabel(container.transform, "Header", $"// {entry.DispName} [ DATA GRID: {rows.Count} ITEMS ] //", CyberPalette.ColorIceBlueBright, 10.5f);
        var headerLayout = headerRow.gameObject.AddComponent<LayoutElement>();
        headerLayout.minHeight = 20f;
        headerLayout.preferredHeight = 20f;
        headerLayout.flexibleHeight = 0f;

        for (int i = 0; i < rows.Count; i++)
        {
            var rowIndex = i;
            RenderDataRow(container.transform, rows, rowIndex, entry, () =>
            {
                // Refresh parent
                entry.SetValue(string.Join(",", rows));
            });
        }

        var addBtn = UiFactory.CreateCyberButton(container.transform, "AddRowBtn", "[ + ADD ENTRY ]", () =>
        {
            rows.Add("Item:1");
            entry.SetValue(string.Join(",", rows));
        }, CyberPalette.ColorGlacialMint, CyberPalette.ColorGlacialMint, -1f, 22f);
        var addLayout = addBtn.GetComponent<LayoutElement>();
        addLayout.flexibleWidth = 1f;

        return container;
    }

    private static void RenderDataRow(Transform parent, List<string> rows, int index, SettingEntry entry, Action onModified)
    {
        var rowStr = rows[index];
        var parts = rowStr.Split(':');
        var namePart = parts.Length > 0 ? parts[0] : "";
        var amountPart = parts.Length > 1 ? parts[1] : "1";

        var rowObj = new GameObject($"Row_{index}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowObj.transform.SetParent(parent, false);

        var hlg = rowObj.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        var le = rowObj.AddComponent<LayoutElement>();
        le.minHeight = 22f;
        le.preferredHeight = 22f;
        le.flexibleHeight = 0f;
        le.flexibleWidth = 1f;

        UiFactory.CreateInputField(rowObj.transform, "ColName", namePart, newName =>
        {
            parts[0] = newName;
            rows[index] = string.Join(":", parts);
            onModified?.Invoke();
        }, 150f, 22f);

        UiFactory.CreateInputField(rowObj.transform, "ColAmount", amountPart, newAmount =>
        {
            if (parts.Length > 1)
            {
                parts[1] = newAmount;
            }
            rows[index] = string.Join(":", parts);
            onModified?.Invoke();
        }, 50f, 22f);

        UiFactory.CreateCyberButton(rowObj.transform, "DeleteBtn", "[ X ]", () =>
        {
            rows.RemoveAt(index);
            onModified?.Invoke();
        }, CyberPalette.ColorErrorRed, CyberPalette.ColorErrorRed, 24f, 20f);
    }
}
