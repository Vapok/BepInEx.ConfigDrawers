using System;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;

public static class ColorDrawer
{
    private static readonly string ArrowColorHex = ColorUtility.ToHtmlStringRGB(CyberPalette.ColorGlacialMint);

    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        GameObject? previewObj = null;
        TMP_InputField? inputFieldObj = null;
        TextMeshProUGUI? labelTmp = null;
        GameObject? subpanelObj = null;
        var isExpanded = false;

        void UpdateLabel()
        {
            if (labelTmp != null)
            {
                var arrow = isExpanded ? "▼" : "▶";
                labelTmp.richText = true;
                labelTmp.text = $"<color=#{ArrowColorHex}><b>{arrow}</b></color>  {entry.DispName}";
            }
        }

        var rowObj = DrawerDispatcher.CreateRowContainer(parent, entry, out var valueArea, () =>
        {
            var cur = entry.ConfigEntry.BoxedValue is Color c ? c : Color.white;
            var h = "#" + ColorUtility.ToHtmlStringRGBA(cur);
            if (inputFieldObj != null)
            {
                inputFieldObj.text = h;
            }
            if (previewObj != null)
            {
                var f = previewObj.transform.Find("Fill");
                var img = f?.GetComponent<Image>();
                if (img != null)
                {
                    img.color = cur;
                }
            }
        }, 115f);

        labelTmp = rowObj.transform.Find("Fill/LeftArea/Label")?.GetComponent<TextMeshProUGUI>();
        if (labelTmp != null)
        {
            labelTmp.raycastTarget = false;
        }
        UpdateLabel();

        var currentColor = entry.ConfigEntry.BoxedValue is Color col ? col : Color.white;
        var hex = "#" + ColorUtility.ToHtmlStringRGBA(currentColor);

        previewObj = UiFactory.CreatePanel(valueArea, "Preview", CyberPalette.ColorIceBlueBright, currentColor, 1f);
        var previewRT = previewObj.GetComponent<RectTransform>();
        previewRT.sizeDelta = new Vector2(26f, 22f);
        var previewLayout = previewObj.AddComponent<LayoutElement>();
        previewLayout.minWidth = 26f;
        previewLayout.preferredWidth = 26f;
        previewLayout.flexibleWidth = 0f;
        previewLayout.minHeight = 22f;
        previewLayout.preferredHeight = 22f;
        previewLayout.flexibleHeight = 0f;

        var (inputRoot, inputField) = UiFactory.CreateInputField(valueArea, "HexInput", hex, text =>
        {
            var str = text.StartsWith("#") ? text : "#" + text;
            if (ColorUtility.TryParseHtmlString(str, out var parsed))
            {
                entry.SetValue(parsed);
                var fill = previewObj.transform.Find("Fill");
                var img = fill?.GetComponent<Image>();
                if (img != null)
                {
                    img.color = parsed;
                }
            }
        }, 85f, 22f);

        inputFieldObj = inputField;
        inputField.interactable = entry.CanEdit;

        previewObj.transform.SetAsFirstSibling();
        inputRoot.transform.SetSiblingIndex(1);

        Action toggleAction = () =>
        {
            isExpanded = !isExpanded;
            if (subpanelObj != null)
            {
                subpanelObj.SetActive(isExpanded);
            }
            UpdateLabel();
            if (parent is RectTransform pRT)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(pRT);
            }
        };

        DrawerDispatcher.AttachBarToggle(rowObj, toggleAction);

        var prevClick = previewObj.GetComponent<ClickableBarHandler>() ?? previewObj.AddComponent<ClickableBarHandler>();
        prevClick.OnClick = toggleAction;

        subpanelObj = ColorPickerDrawer.Attach(parent, rowObj, entry, newColor =>
        {
            var f = previewObj.transform.Find("Fill");
            var img = f?.GetComponent<Image>();
            if (img != null)
            {
                img.color = newColor;
            }
            if (inputFieldObj != null)
            {
                inputFieldObj.text = "#" + ColorUtility.ToHtmlStringRGBA(newColor);
            }
        });

        return rowObj;
    }
}
