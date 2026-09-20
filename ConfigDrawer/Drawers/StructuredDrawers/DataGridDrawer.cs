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

        if (entry.CustomDrawer != null)
        {
            return false;
        }

        var keyLower = entry.Key.ToLowerInvariant();
        var descLower = entry.Description.ToLowerInvariant();
        var val = entry.ConfigEntry?.BoxedValue as string ?? string.Empty;

        if (val.Contains("{0}") || val.Contains("{1}") || keyLower.EndsWith("string") || keyLower.Contains("format") || keyLower.Contains("template"))
        {
            return false;
        }

        var hasColon = val.Contains(":");
        var isDelimited = val.Contains(",") || val.Contains("\n") || val.Contains(";");

        if (hasColon && isDelimited)
        {
            return true;
        }

        if (descLower.Contains("item:qty") || descLower.Contains("prefab:amount") || descLower.Contains("item:amount"))
        {
            return true;
        }

        if ((keyLower.Contains("recipe") || keyLower.Contains("requirement") || (keyLower.Contains("cost") && !keyLower.Contains("string"))) && hasColon)
        {
            return true;
        }

        return false;
    }

    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        var raw = entry.ConfigEntry?.BoxedValue as string ?? string.Empty;
        var rows = ParseRows(raw);

        GameObject? subpanelObj = null;
        TextMeshProUGUI? labelTmp = null;
        var isExpanded = false;

        var arrowColorHex = ColorUtility.ToHtmlStringRGB(CyberPalette.ColorGlacialMint);
        var countColorHex = ColorUtility.ToHtmlStringRGB(CyberPalette.ColorTextMuted);
        void UpdateLeftLabel()
        {
            if (labelTmp != null)
            {
                var arrow = isExpanded ? "▼" : "▶";
                var itemWord = rows.Count == 1 ? "item" : "items";
                var countText = $"({rows.Count} {itemWord})";
                labelTmp.richText = true;
                labelTmp.text = $"<color=#{arrowColorHex}><b>{arrow}</b></color>  {entry.DispName}  <color=#{countColorHex}>{countText}</color>";
            }
        }

        var rowObj = DrawerDispatcher.CreateRowContainer(parent, entry, out var valueArea, () =>
        {
            raw = entry.ConfigEntry?.BoxedValue as string ?? string.Empty;
            rows = ParseRows(raw);
            UpdateLeftLabel();
            if (subpanelObj != null)
            {
                RebuildSubpanel(subpanelObj.transform, rows, entry, parent as RectTransform, UpdateLeftLabel);
            }
        });

        var leftRT = rowObj.transform.Find("Fill/LeftArea")?.GetComponent<RectTransform>();
        if (leftRT != null)
        {
            leftRT.offsetMax = new Vector2(-80f, 0f);
        }

        labelTmp = rowObj.transform.Find("Fill/LeftArea/Label")?.GetComponent<TextMeshProUGUI>();
        if (labelTmp != null)
        {
            labelTmp.raycastTarget = false;
        }
        UpdateLeftLabel();

        Action toggleAction = () =>
        {
            isExpanded = !isExpanded;
            if (subpanelObj != null)
            {
                subpanelObj.SetActive(isExpanded);
            }
            UpdateLeftLabel();
            if (parent is RectTransform pRT)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(pRT);
            }
        };

        DrawerDispatcher.AttachBarToggle(rowObj, toggleAction);

        subpanelObj = new GameObject($"SubGrid_{entry.Key}", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        subpanelObj.transform.SetParent(parent, false);
        subpanelObj.transform.SetSiblingIndex(rowObj.transform.GetSiblingIndex() + 1);

        var bgImg = subpanelObj.GetComponent<Image>();
        bgImg.color = CyberPalette.ColorVoidBlack;

        var vlg = subpanelObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 3f;
        vlg.padding = new RectOffset(16, 12, 6, 6);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = subpanelObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var le = subpanelObj.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        RebuildSubpanel(subpanelObj.transform, rows, entry, parent as RectTransform, UpdateLeftLabel);
        subpanelObj.SetActive(false);

        return rowObj;
    }

    private static List<string> ParseRows(string raw)
    {
        return raw.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                  .Select(r => r.Trim())
                  .Where(r => !string.IsNullOrEmpty(r))
                  .ToList();
    }

    private static void RebuildSubpanel(Transform subpanel, List<string> rows, SettingEntry entry, RectTransform? parentListRT, Action? onCountChanged)
    {
        foreach (Transform child in subpanel)
        {
            UnityEngine.Object.Destroy(child.gameObject);
        }

        var headerRow = new GameObject("HeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        headerRow.transform.SetParent(subpanel, false);
        var hHlg = headerRow.GetComponent<HorizontalLayoutGroup>();
        hHlg.spacing = 4f;
        hHlg.childAlignment = TextAnchor.MiddleLeft;
        hHlg.childControlWidth = false;
        hHlg.childControlHeight = true;
        hHlg.childForceExpandWidth = false;
        hHlg.childForceExpandHeight = false;

        var hLe = headerRow.AddComponent<LayoutElement>();
        hLe.minHeight = 16f;
        hLe.preferredHeight = 16f;
        hLe.flexibleHeight = 0f;

        var itemLabel = UiFactory.CreateLabel(headerRow.transform, "HItem", "ITEM / PREFAB", CyberPalette.ColorTextMuted, 9f);
        itemLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 16f);

        var qtyLabel = UiFactory.CreateLabel(headerRow.transform, "HQty", "QTY", CyberPalette.ColorTextMuted, 9f);
        qtyLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(45f, 16f);

        for (int i = 0; i < rows.Count; i++)
        {
            var rowIndex = i;
            RenderDataRow(subpanel, rows, rowIndex, entry, () =>
            {
                entry.SetValue(string.Join(",", rows));
            }, () =>
            {
                onCountChanged?.Invoke();
                RebuildSubpanel(subpanel, rows, entry, parentListRT, onCountChanged);
            });
        }

        if (entry.CanEdit)
        {
            var addBtn = UiFactory.CreateCyberButton(subpanel, "AddRowBtn", "+ Add Item", () =>
            {
                rows.Add("Item:1");
                entry.SetValue(string.Join(",", rows));
                onCountChanged?.Invoke();
                RebuildSubpanel(subpanel, rows, entry, parentListRT, onCountChanged);
                if (parentListRT != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentListRT);
                }
            }, CyberPalette.ColorGlacialMint, CyberPalette.ColorGlacialMint, 90f, 20f);

            var addLe = addBtn.GetComponent<LayoutElement>();
            addLe.minHeight = 20f;
            addLe.preferredHeight = 20f;
            addLe.flexibleHeight = 0f;
        }

        if (parentListRT != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentListRT);
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

        var (_, inName) = UiFactory.CreateInputField(rowObj.transform, "ColName", namePart, newName =>
        {
            parts[0] = newName;
            rows[index] = string.Join(":", parts);
            onModified?.Invoke();
        }, 120f, 22f);
        inName.interactable = entry.CanEdit;

        var (_, inQty) = UiFactory.CreateInputField(rowObj.transform, "ColAmount", amountPart, newAmount =>
        {
            if (parts.Length > 1)
            {
                parts[1] = newAmount;
            }
            rows[index] = string.Join(":", parts);
            onModified?.Invoke();
        }, 45f, 22f);
        inQty.interactable = entry.CanEdit;

        if (entry.CanEdit)
        {
            UiFactory.CreateCyberButton(rowObj.transform, "DeleteBtn", "X", () =>
            {
                rows.RemoveAt(index);
                entry.SetValue(string.Join(",", rows));
                onModified?.Invoke();
                onRebuild?.Invoke();
            }, CyberPalette.ColorErrorRed, CyberPalette.ColorErrorRed, 22f, 20f);
        }
    }
}
