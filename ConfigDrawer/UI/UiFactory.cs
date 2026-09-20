using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.UI;

public static class UiFactory
{
    private static TMP_FontAsset? _cachedFont;

    public static TMP_FontAsset? ResolveFont()
    {
        if (_cachedFont != null)
        {
            return _cachedFont;
        }

        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var font in fonts)
        {
            if (font != null && !string.IsNullOrEmpty(font.name))
            {
                _cachedFont = font;
                return _cachedFont;
            }
        }

        return null;
    }

    public static GameObject CreatePanel(Transform parent, string name, Color borderColor, Color fillColor, float borderWidth = 1f)
    {
        var outerObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        outerObj.transform.SetParent(parent, false);

        var outerImg = outerObj.GetComponent<Image>();
        outerImg.color = borderColor;

        var innerObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        innerObj.transform.SetParent(outerObj.transform, false);

        var innerRT = innerObj.GetComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(borderWidth, borderWidth);
        innerRT.offsetMax = new Vector2(-borderWidth, -borderWidth);

        var innerImg = innerObj.GetComponent<Image>();
        innerImg.color = fillColor;

        return outerObj;
    }

    public static GameObject CreateCyberButton(Transform parent, string name, string labelText, Action onClick, Color borderColor, Color textColor, float width = -1f, float height = 26f)
    {
        var btnObj = CreatePanel(parent, name, borderColor, CyberPalette.ColorVoidBlack, 1f);
        var btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(new UnityAction(onClick));

        var rt = btnObj.GetComponent<RectTransform>();
        var layout = btnObj.AddComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;

        if (width > 0f)
        {
            layout.minWidth = width;
            layout.preferredWidth = width;
        }

        var fillTransform = btnObj.transform.Find("Fill");
        var textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(fillTransform != null ? fillTransform : btnObj.transform, false);

        var textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(6f, 0f);
        textRT.offsetMax = new Vector2(-6f, 0f);

        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.font = ResolveFont();
        tmp.text = labelText;
        tmp.fontSize = 11f;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        return btnObj;
    }

    public static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, Color color, float fontSize = 12f, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var labelObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(parent, false);

        var tmp = labelObj.GetComponent<TextMeshProUGUI>();
        tmp.font = ResolveFont();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        return tmp;
    }

    public static (GameObject Root, TMP_InputField Input) CreateInputField(Transform parent, string name, string initialText, Action<string> onCommit, float width = 120f, float height = 26f)
    {
        var root = CreatePanel(parent, name, CyberPalette.ColorBorderSubtle, CyberPalette.ColorVoidBlack, 1f);
        var layout = root.AddComponent<LayoutElement>();
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.minHeight = height;
        layout.preferredHeight = height;

        var fillTransform = root.transform.Find("Fill");
        var targetParent = fillTransform != null ? fillTransform : root.transform;

        var textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(targetParent, false);

        var textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(6f, 2f);
        textRT.offsetMax = new Vector2(-6f, -2f);

        var textTmp = textObj.GetComponent<TextMeshProUGUI>();
        textTmp.font = ResolveFont();
        textTmp.fontSize = 11f;
        textTmp.color = CyberPalette.ColorTextMain;

        var input = root.AddComponent<TMP_InputField>();
        input.textComponent = textTmp;
        input.text = initialText;
        input.onEndEdit.AddListener(new UnityAction<string>(val => onCommit?.Invoke(val)));

        return (root, input);
    }
}
