using System;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
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

        GameObject? previewObj = null;
        TMP_InputField? inputFieldObj = null;

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
        });

        var currentColor = entry.ConfigEntry.BoxedValue is Color col ? col : Color.white;
        var hex = "#" + ColorUtility.ToHtmlStringRGBA(currentColor);

        previewObj = UiFactory.CreatePanel(valueArea, "Preview", CyberPalette.ColorIceBlue, currentColor, 1f);
        var previewRT = previewObj.GetComponent<RectTransform>();
        previewRT.sizeDelta = new Vector2(22f, 22f);
        var previewLayout = previewObj.AddComponent<LayoutElement>();
        previewLayout.minWidth = 22f;
        previewLayout.preferredWidth = 22f;
        previewLayout.flexibleWidth = 0f;
        previewLayout.minHeight = 22f;
        previewLayout.preferredHeight = 22f;
        previewLayout.flexibleHeight = 0f;

        var (inputRoot, inputField) = UiFactory.CreateInputField(valueArea, "HexInput", hex, text =>
        {
            if (ColorUtility.TryParseHtmlString(text.StartsWith("#") ? text : "#" + text, out var parsed))
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

        previewObj.transform.SetAsFirstSibling();
        inputRoot.transform.SetSiblingIndex(1);

        return rowObj;
    }
}
