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
        if (_cachedFont != null && _cachedFont)
        {
            return _cachedFont;
        }

        try
        {
            if (TMP_Settings.defaultFontAsset != null && TMP_Settings.defaultFontAsset.characterTable != null && TMP_Settings.defaultFontAsset.characterTable.Count > 0)
            {
                _cachedFont = TMP_Settings.defaultFontAsset;
                return _cachedFont;
            }
        }
        catch
        {
            // Fallback to searching resources
        }

        try
        {
            var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (allFonts != null && allFonts.Length > 0)
            {
                foreach (var font in allFonts)
                {
                    if (font != null && font && string.Equals(font.name, "Valheim-AveriaSansLibre", StringComparison.OrdinalIgnoreCase))
                    {
                        if (font.characterTable != null && font.characterTable.Count > 0)
                        {
                            _cachedFont = font;
                            return _cachedFont;
                        }
                    }
                }

                foreach (var font in allFonts)
                {
                    if (font != null && font && !string.IsNullOrEmpty(font.name))
                    {
                        var lowerName = font.name.ToLowerInvariant();
                        if ((lowerName.Contains("valheim") || lowerName.Contains("averiasans")) && !lowerName.Contains("norse") && !lowerName.Contains("bold"))
                        {
                            if (font.characterTable != null && font.characterTable.Count > 0)
                            {
                                _cachedFont = font;
                                return _cachedFont;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Fallback to scene instances
        }

        try
        {
            var sceneTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            if (sceneTexts != null && sceneTexts.Length > 0)
            {
                foreach (var text in sceneTexts)
                {
                    if (text != null && text.font != null && text.font.characterTable != null && text.font.characterTable.Count > 0)
                    {
                        _cachedFont = text.font;
                        return _cachedFont;
                    }
                }
            }
        }
        catch
        {
            // Final fallback
        }

        try
        {
            var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (allFonts != null)
            {
                foreach (var font in allFonts)
                {
                    if (font != null && font && font.characterTable != null && font.characterTable.Count > 0)
                    {
                        _cachedFont = font;
                        return _cachedFont;
                    }
                }
            }
        }
        catch
        {
            // Exhausted font search
        }

        return null;
    }

    public static void RefreshAllFonts(GameObject root)
    {
        var font = ResolveFont();
        if (font == null || root == null)
        {
            return;
        }

        var allTexts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in allTexts)
        {
            if (text != null && (text.font == null || text.font.characterTable == null || text.font.characterTable.Count == 0))
            {
                text.font = font;
            }
        }
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

    public static GameObject CreateCyberButton(Transform parent, string name, string labelText, Action onClick, Color borderColor, Color textColor, float width = -1f, float height = 26f, bool enableHover = true)
    {
        var btnObj = CreatePanel(parent, name, borderColor, CyberPalette.ColorVoidBlack, 1f);
        var btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(new UnityAction(onClick));

        var rt = btnObj.GetComponent<RectTransform>();
        var targetWidth = width > 0f ? width : 60f;
        rt.sizeDelta = new Vector2(targetWidth, height);

        var layout = btnObj.AddComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleHeight = 0f;
        layout.minWidth = targetWidth;
        layout.preferredWidth = targetWidth;
        layout.flexibleWidth = 0f;

        var fillTransform = btnObj.transform.Find("Fill");
        var textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(fillTransform != null ? fillTransform : btnObj.transform, false);

        var textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(4f, 0f);
        textRT.offsetMax = new Vector2(-4f, 0f);

        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        var font = ResolveFont();
        if (font != null)
        {
            tmp.font = font;
        }

        tmp.text = labelText;
        tmp.fontSize = 11f;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        if (enableHover)
        {
            var hover = btnObj.AddComponent<CyberHoverHandler>();
            var borderImg = btnObj.GetComponent<Image>();
            var fillImg = fillTransform != null ? fillTransform.GetComponent<Image>() : null;
            hover.Init(borderImg, borderColor, CyberPalette.ColorIceBlueBright, fillImg, CyberPalette.ColorVoidBlack, CyberPalette.ColorCardSurface);
        }

        return btnObj;
    }

    public static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, Color color, float fontSize = 12f, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var labelObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(parent, false);

        var tmp = labelObj.GetComponent<TextMeshProUGUI>();
        var font = ResolveFont();
        if (font != null)
        {
            tmp.font = font;
        }

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        return tmp;
    }

    public static (GameObject Root, TMP_InputField Input) CreateInputField(Transform parent, string name, string initialText, Action<string> onCommit, float width = 120f, float height = 26f, string placeholderText = "")
    {
        var root = CreatePanel(parent, name, CyberPalette.ColorBorderSubtle, CyberPalette.ColorVoidBlack, 1f);
        var targetWidth = width > 0f ? width : 120f;

        var rootRT = root.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(targetWidth, height);

        var layout = root.AddComponent<LayoutElement>();
        layout.minWidth = targetWidth;
        layout.preferredWidth = targetWidth;
        layout.flexibleWidth = width > 0f ? 0f : 1f;
        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleHeight = 0f;

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
        var font = ResolveFont();
        if (font != null)
        {
            textTmp.font = font;
        }

        textTmp.fontSize = 11f;
        textTmp.color = CyberPalette.ColorTextMain;

        var input = root.AddComponent<TMP_InputField>();
        input.textComponent = textTmp;

        if (!string.IsNullOrEmpty(placeholderText))
        {
            var placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderObj.transform.SetParent(targetParent, false);

            var placeholderRT = placeholderObj.GetComponent<RectTransform>();
            placeholderRT.anchorMin = Vector2.zero;
            placeholderRT.anchorMax = Vector2.one;
            placeholderRT.offsetMin = new Vector2(6f, 2f);
            placeholderRT.offsetMax = new Vector2(-6f, -2f);

            var placeholderTmp = placeholderObj.GetComponent<TextMeshProUGUI>();
            if (font != null)
            {
                placeholderTmp.font = font;
            }

            placeholderTmp.fontSize = 11f;
            placeholderTmp.color = CyberPalette.ColorTextMuted;
            placeholderTmp.text = placeholderText;

            input.placeholder = placeholderTmp;
        }

        input.text = initialText;
        input.onEndEdit.AddListener(new UnityAction<string>(val => onCommit?.Invoke(val)));

        return (root, input);
    }
}
