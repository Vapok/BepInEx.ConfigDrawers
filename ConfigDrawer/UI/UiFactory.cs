using System;
using BepInEx.ConfigDrawers.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.UI;

public static class UiFactory
{
#pragma warning disable CS0618
    private static TMP_FontAsset? _cachedFont;

    public static float GetFontScaleFactor()
    {
        return ConfigDrawerConfig.UiFontSize?.Value switch
        {
            FontSizeScale.Small => 0.85f,
            FontSizeScale.Large => 1.08f,
            _ => 0.95f
        };
    }

    public static float GetScaledFontSize(float baseSize)
    {
        return Mathf.Round(baseSize * GetFontScaleFactor());
    }

    private static TMP_FontAsset? _cachedTerminalFont;

    public static TMP_FontAsset? ResolveTerminalFont()
    {
        if (_cachedTerminalFont != null && _cachedTerminalFont)
        {
            return _cachedTerminalFont;
        }

        try
        {
            var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (allFonts != null && allFonts.Length > 0)
            {
                foreach (var f in allFonts)
                {
                    if (f != null && f && !string.IsNullOrEmpty(f.name))
                    {
                        var ln = f.name.ToLowerInvariant();
                        if (ln.Contains("mono") || ln.Contains("console") || ln.Contains("code"))
                        {
                            if (f.characterTable != null && f.characterTable.Count > 0)
                            {
                                _cachedTerminalFont = f;
                                return _cachedTerminalFont;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Soft fallback
        }

        _cachedTerminalFont = ResolveFont();
        return _cachedTerminalFont;
    }

    public static TMP_FontAsset? ResolveFont()
    {
        if (_cachedFont != null && _cachedFont)
        {
            return _cachedFont;
        }

        try
        {
            var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (allFonts != null && allFonts.Length > 0)
            {
                // Priority 1: Exact Valheim-AveriaSansLibre
                foreach (var f in allFonts)
                {
                    if (f != null && f && string.Equals(f.name, "Valheim-AveriaSansLibre", StringComparison.OrdinalIgnoreCase))
                    {
                        if (f.characterTable != null && f.characterTable.Count > 0)
                        {
                            SetCachedFont(f);
                            return _cachedFont;
                        }
                    }
                }

                // Priority 2: Valheim UI fonts excluding Norse, bold, and pixel prstart
                foreach (var f in allFonts)
                {
                    if (f != null && f && !string.IsNullOrEmpty(f.name))
                    {
                        var ln = f.name.ToLowerInvariant();
                        if ((ln.Contains("valheim") || ln.Contains("averia")) && !ln.Contains("norse") && !ln.Contains("bold") && !ln.Contains("prstart"))
                        {
                            if (f.characterTable != null && f.characterTable.Count > 0)
                            {
                                SetCachedFont(f);
                                return _cachedFont;
                            }
                        }
                    }
                }

                // Priority 3: Clean standard fonts excluding pixel and norse
                foreach (var f in allFonts)
                {
                    if (f != null && f && !string.IsNullOrEmpty(f.name))
                    {
                        var ln = f.name.ToLowerInvariant();
                        if (!ln.Contains("prstart") && !ln.Contains("norse"))
                        {
                            if (f.characterTable != null && f.characterTable.Count > 0)
                            {
                                SetCachedFont(f);
                                return _cachedFont;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Soft fallback
        }

        return null;
    }

    private static void SetCachedFont(TMP_FontAsset font)
    {
        _cachedFont = font;
        try
        {
            if (TMP_Settings.defaultFontAsset == null)
            {
                TMP_Settings.defaultFontAsset = font;
            }
        }
        catch
        {
            // Soft fallback
        }
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
            if (text != null)
            {
                text.font = font;
                if (font.material != null)
                {
                    text.fontSharedMaterial = font.material;
                }
            }
        }
    }

    public static GameObject CreatePanel(Transform parent, string name, Color borderColor, Color fillColor, float borderWidth = 1f)
    {
        var outerObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        outerObj.transform.SetParent(parent, false);

        var outerImg = outerObj.GetComponent<Image>();
        outerImg.color = borderColor;

        var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(outerObj.transform, false);

        var fillRT = fillObj.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        var bw = Mathf.Max(borderWidth, 0.5f);
        fillRT.offsetMin = new Vector2(bw, bw);
        fillRT.offsetMax = new Vector2(-bw, -bw);

        var fillImg = fillObj.GetComponent<Image>();
        fillImg.color = fillColor;

        return outerObj;
    }

    public static GameObject CreateCyberButton(Transform parent, string name, string labelText, Action onClick, Color borderColor, Color textColor, float width = -1f, float height = 24f, bool enableHover = true)
    {
        var btnObj = CreatePanel(parent, name, borderColor, CyberPalette.ColorVoidBlack, 1f);

        var rt = btnObj.GetComponent<RectTransform>();
        var targetWidth = width > 0f ? width : 50f;
        rt.sizeDelta = new Vector2(targetWidth, height);

        var layout = btnObj.AddComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleHeight = 0f;
        layout.minWidth = targetWidth;
        layout.preferredWidth = targetWidth;
        layout.flexibleWidth = 0f;

        var fillTransform = btnObj.transform.Find("Fill");
        var textObj = new GameObject("Text", typeof(RectTransform));
        textObj.SetActive(false);
        textObj.transform.SetParent(fillTransform != null ? fillTransform : btnObj.transform, false);

        var textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(3f, 0f);
        textRT.offsetMax = new Vector2(-3f, 0f);

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        var font = ResolveFont();
        if (font != null)
        {
            tmp.font = font;
            if (font.material != null)
            {
                tmp.fontSharedMaterial = font.material;
            }
        }

        tmp.text = labelText;
        tmp.fontSize = GetScaledFontSize(10.5f);
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        textObj.SetActive(true);

        if (enableHover)
        {
            var hover = btnObj.AddComponent<CyberHoverHandler>();
            var borderImg = btnObj.GetComponent<Image>();
            var fillImg = fillTransform != null ? fillTransform.GetComponent<Image>() : null;
            var isRed = borderColor == CyberPalette.ColorErrorRed;
            var hoverBorder = isRed ? new Color(1f, 0.45f, 0.45f, 1f) : CyberPalette.ColorIceBlueBright;
            var pressedBorder = isRed ? new Color(1f, 0.85f, 0.85f, 1f) : Color.white;
            var hoverFill = isRed ? new Color(0.18f, 0.04f, 0.04f, 1f) : CyberPalette.ColorCardSurface;
            var pressedFill = isRed ? new Color(0.35f, 0.08f, 0.08f, 1f) : new Color(0.12f, 0.22f, 0.32f, 1f);
            var hoverText = isRed ? Color.white : CyberPalette.ColorIceBlueBright;
            var pressedText = Color.white;
            hover.Init(borderImg, borderColor, hoverBorder, fillImg, CyberPalette.ColorVoidBlack, hoverFill, pressedBorder, pressedFill, tmp, textColor, hoverText, pressedText);
        }

        var btn = btnObj.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(new UnityAction(onClick));

        return btnObj;
    }

    public static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, Color color, float fontSize = 11f, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var labelObj = new GameObject(name, typeof(RectTransform));
        labelObj.SetActive(false);
        labelObj.transform.SetParent(parent, false);

        var tmp = labelObj.AddComponent<TextMeshProUGUI>();
        var font = ResolveFont();
        if (font != null)
        {
            tmp.font = font;
            if (font.material != null)
            {
                tmp.fontSharedMaterial = font.material;
            }
        }

        tmp.text = text;
        tmp.fontSize = GetScaledFontSize(fontSize);
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        labelObj.SetActive(true);
        return tmp;
    }

    public static (GameObject Root, TMP_InputField Input) CreateInputField(Transform parent, string name, string initialText, Action<string> onCommit, float width = 120f, float height = 24f, string placeholderText = "", bool multiline = false)
    {
        var root = CreatePanel(parent, name, CyberPalette.ColorInputGroove, CyberPalette.ColorInputWell, 1f);
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

        var bottomLine = new GameObject("BottomAccent", typeof(RectTransform), typeof(Image));
        bottomLine.transform.SetParent(targetParent, false);
        var blRT = bottomLine.GetComponent<RectTransform>();
        blRT.anchorMin = new Vector2(0f, 0f);
        blRT.anchorMax = new Vector2(1f, 0f);
        blRT.pivot = new Vector2(0.5f, 0f);
        blRT.sizeDelta = new Vector2(0f, 1.5f);
        blRT.anchoredPosition = Vector2.zero;
        var blImg = bottomLine.GetComponent<Image>();
        blImg.color = CyberPalette.ColorInputAccent;
        blImg.raycastTarget = false;

        var textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(targetParent, false);
        var taRT = textArea.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero;
        taRT.anchorMax = Vector2.one;
        taRT.offsetMin = multiline ? new Vector2(6f, 6f) : new Vector2(6f, 2f);
        taRT.offsetMax = multiline ? new Vector2(-6f, -6f) : new Vector2(-6f, -2f);

        var textObj = new GameObject("Text", typeof(RectTransform));
        textObj.SetActive(false);
        textObj.transform.SetParent(textArea.transform, false);
        var textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        var standardFont = ResolveFont();

        var textTmp = textObj.AddComponent<TextMeshProUGUI>();
        if (standardFont != null)
        {
            textTmp.font = standardFont;
            if (standardFont.material != null)
            {
                textTmp.fontSharedMaterial = standardFont.material;
            }
        }

        textTmp.fontSize = GetScaledFontSize(10f);
        textTmp.color = CyberPalette.ColorTextMain;
        textTmp.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
        textTmp.enableWordWrapping = multiline;
        textTmp.richText = false;
        textTmp.overflowMode = TextOverflowModes.Overflow;
        textTmp.raycastTarget = false;
        textObj.SetActive(true);

        var placeholderObj = new GameObject("Placeholder", typeof(RectTransform));
        placeholderObj.SetActive(false);
        placeholderObj.transform.SetParent(textArea.transform, false);
        var phRT = placeholderObj.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = Vector2.zero;
        phRT.offsetMax = Vector2.zero;

        var placeholderTmp = placeholderObj.AddComponent<TextMeshProUGUI>();
        if (standardFont != null)
        {
            placeholderTmp.font = standardFont;
            if (standardFont.material != null)
            {
                placeholderTmp.fontSharedMaterial = standardFont.material;
            }
        }

        placeholderTmp.fontSize = GetScaledFontSize(10f);
        placeholderTmp.color = new Color(0.35f, 0.48f, 0.58f, 0.55f);
        placeholderTmp.text = placeholderText ?? string.Empty;
        placeholderTmp.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
        placeholderTmp.enableWordWrapping = multiline;
        placeholderTmp.richText = false;
        placeholderTmp.raycastTarget = false;
        placeholderObj.SetActive(true);

        var input = root.AddComponent<TMP_InputField>();
        input.textComponent = textTmp;
        input.textViewport = taRT;
        input.targetGraphic = fillTransform != null ? fillTransform.GetComponent<Image>() : root.GetComponent<Image>();
        input.placeholder = placeholderTmp;

        input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
        input.navigation = new Navigation { mode = Navigation.Mode.None };

        input.customCaretColor = true;
        input.caretColor = CyberPalette.ColorIceBlueBright;
        input.caretWidth = 2;
        input.caretBlinkRate = 0.85f;
        input.selectionColor = new Color(0.12f, 0.50f, 0.75f, 0.45f);

        var borderImg = root.GetComponent<Image>();
        var fillImg = fillTransform != null ? fillTransform.GetComponent<Image>() : null;

        var hover = root.AddComponent<CyberHoverHandler>();
        hover.Init(borderImg, CyberPalette.ColorInputGroove, new Color(0.22f, 0.38f, 0.50f, 0.9f), fillImg, CyberPalette.ColorInputWell, CyberPalette.ColorInputWell);

        input.onSelect.AddListener(_ =>
        {
            if (borderImg != null) borderImg.color = CyberPalette.ColorIceBlueBright;
            if (blImg != null) blImg.color = CyberPalette.ColorIceBlueBright;
        });
        input.onDeselect.AddListener(_ =>
        {
            if (borderImg != null) borderImg.color = CyberPalette.ColorInputGroove;
            if (blImg != null) blImg.color = CyberPalette.ColorInputAccent;
        });

        input.text = initialText;
        input.onEndEdit.AddListener(new UnityAction<string>(val => onCommit?.Invoke(val)));

        return (root, input);
    }

    public static RectTransform? AttachTextPadIcon(Transform leftArea, TextMeshProUGUI label)
    {
        if (leftArea == null || label == null)
        {
            return null;
        }

        var iconObj = new GameObject("TextPadIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(leftArea, false);

        var iconRT = iconObj.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0f, 0.5f);
        iconRT.anchorMax = new Vector2(0f, 0.5f);
        iconRT.pivot = new Vector2(0f, 0.5f);
        iconRT.sizeDelta = new Vector2(13f, 13f);

        label.ForceMeshUpdate();
        iconRT.anchoredPosition = new Vector2(label.preferredWidth + 6f, 0f);

        var iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = IconFactory.GetTextPadIcon();
        iconImg.color = CyberPalette.ColorTextMuted;
        iconImg.raycastTarget = false;

        return iconRT;
    }
}
