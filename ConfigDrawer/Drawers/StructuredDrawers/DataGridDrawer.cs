using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
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

        var raw = entry.ConfigEntry?.BoxedValue as string ?? string.Empty;
        var rows = ParseRows(raw);

        GameObject? tableContainer = null;
        TextMeshProUGUI? toggleBtnText = null;
        var isExpanded = false;

        var rowObj = DrawerDispatcher.CreateRowContainer(parent, entry, out var valueArea, () =>
        {
            raw = entry.ConfigEntry?.BoxedValue as string ?? string.Empty;
            rows = ParseRows(raw);
            if (toggleBtnText != null)
            {
                toggleBtnText.text = isExpanded ? $"{rows.Count} Items  v" : $"{rows.Count} Items  >";
            }
            if (tableContainer != null)
            {
                RebuildTableRows(tableContainer.transform, rows, entry, toggleBtnText, () => isExpanded, parent);
            }
        });

        var toggleBtn = UiFactory.CreateCyberButton(valueArea, "ToggleGridBtn", $"{rows.Count} Items  >", () =>
        {
            isExpanded = !isExpanded;
            if (tableContainer != null)
            {
                tableContainer.SetActive(isExpanded);
            }
            if (toggleBtnText != null)
            {
                toggleBtnText.text = isExpanded ? $"{rows.Count} Items  v" : $"{rows.Count} Items  >";
            }
            if (parent is RectTransform pRT)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(pRT);
            }
        }, CyberPalette.ColorCyberTeal, CyberPalette.ColorIceBlueBright, 70f, 22f);

        toggleBtn.transform.SetAsFirstSibling();
        toggleBtnText = toggleBtn.GetComponentInChildren<TextMeshProUGUI>();

        tableContainer = UiFactory.CreatePanel(parent, $"GridTable_{entry.Key}", CyberPalette.ColorBorderSubtle, CyberPalette.ColorVoidBlack, 1f);
        tableContainer.transform.SetSiblingIndex(rowObj.transform.GetSiblingIndex() + 1);

        var tableLayout = tableContainer.AddComponent<VerticalLayoutGroup>();
        tableLayout.spacing = 3f;
        tableLayout.padding = new RectOffset(16, 8, 6, 6);
        tableLayout.childControlWidth = true;
        tableLayout.childControlHeight = true;
        tableLayout.childForceExpandWidth = true;
        tableLayout.childForceExpandHeight = false;

        var tableCsf = tableContainer.AddComponent<ContentSizeFitter>();
        tableCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RebuildTableRows(tableContainer.transform, rows, entry, toggleBtnText, () => isExpanded, parent);
        tableContainer.SetActive(false);

        return rowObj;
    }

    private static List<string> ParseRows(string raw)
    {
        return raw.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                  .Select(r => r.Trim())
                  .Where(r => !string.IsNullOrEmpty(r))
                  .ToList();
    }

    private static void RebuildTableRows(Transform container, List<string> rows, SettingEntry entry, TextMeshProUGUI? toggleBtnText, Func<bool> getExpanded, Transform parentList)
    {
        var fill = container.Find("Fill");
        var target = fill != null ? fill : container;

        foreach (Transform child in target)
        {
            UnityEngine.Object.Destroy(child.gameObject);
        }

        for (int i = 0; i < rows.Count; i++)
        {
            var rowIndex = i;
            RenderDataRow(target, rows, rowIndex, entry, () =>
            {
                entry.SetValue(string.Join(",", rows));
                if (toggleBtnText != null)
                {
                    toggleBtnText.text = getExpanded() ? $"{rows.Count} Items  v" : $"{rows.Count} Items  >";
                }
            }, () =>
            {
                RebuildTableRows(container, rows, entry, toggleBtnText, getExpanded, parentList);
            });
        }

        var addBtn = UiFactory.CreateCyberButton(target, "AddRowBtn", "+ Add Entry", () =>
        {
            rows.Add("Item:1");
            entry.SetValue(string.Join(",", rows));
            RebuildTableRows(container, rows, entry, toggleBtnText, getExpanded, parentList);
            if (toggleBtnText != null)
            {
                toggleBtnText.text = getExpanded() ? $"{rows.Count} Items  v" : $"{rows.Count} Items  >";
            }
            if (parentList is RectTransform pRT)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(pRT);
            }
        }, CyberPalette.ColorGlacialMint, CyberPalette.ColorGlacialMint, -1f, 22f);

        var addLayout = addBtn.GetComponent<LayoutElement>();
        addLayout.flexibleWidth = 1f;

        if (parentList is RectTransform parentRT)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRT);
        }
    }

    private static void RenderDataRow(Transform parent, List<string> rows, int index, SettingEntry entry, Action onModified, Action onRebuild)
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
        }, 130f, 22f);

        UiFactory.CreateInputField(rowObj.transform, "ColAmount", amountPart, newAmount =>
        {
            if (parts.Length > 1)
            {
                parts[1] = newAmount;
            }
            rows[index] = string.Join(":", parts);
            onModified?.Invoke();
        }, 45f, 22f);

        UiFactory.CreateCyberButton(rowObj.transform, "DeleteBtn", "X", () =>
        {
            rows.RemoveAt(index);
            entry.SetValue(string.Join(",", rows));
            onModified?.Invoke();
            onRebuild?.Invoke();
        }, CyberPalette.ColorErrorRed, CyberPalette.ColorErrorRed, 22f, 20f);
    }
}
