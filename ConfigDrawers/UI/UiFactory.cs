using System.IO;
using System;
using BepInEx.ConfigDrawers.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.UI;

public static class UiFactory
{
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

    public static TMP_FontAsset? ResolveFont()
    {
        return UIFonts.GetPrimaryFont();
    }

    public static TMP_FontAsset? ResolveTerminalFont()
    {
        return UIFonts.GetTerminalFont();
    }

    public static void RefreshAllFonts(GameObject root)
    {
        UIFonts.RefreshAllFonts(root);
    }

    public static GameObject CreatePanel(Transform parent, string name, Color borderColor, Color fillColor, float borderWidth = 1f)
    {
        GameObject outerObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        outerObj.transform.SetParent(parent, false);

        Image outerImg = outerObj.GetComponent<Image>();
        outerImg.color = borderColor;

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(outerObj.transform, false);

        float bw = Mathf.Max(borderWidth, 0.5f);
        fillObj.GetComponent<RectTransform>()
            .SetAnchor(Vector2.zero, Vector2.one)
            .SetOffsets(new Vector2(bw, bw), new Vector2(-bw, -bw));

        Image fillImg = fillObj.GetComponent<Image>();
        fillImg.color = fillColor;

        return outerObj;
    }

    public static GameObject CreateCyberButton(
        Transform parent,
        string name,
        string labelText,
        Action onClick,
        Color borderColor,
        Color textColor,
        float width = -1f,
        float height = 24f,
        bool enableHover = true)
    {
        GameObject btnObj = CreatePanel(parent, name, borderColor, CyberPalette.ColorVoidBlack, 1f);

        float targetWidth = width > 0f ? width : 50f;
        btnObj.GetComponent<RectTransform>().SetSizeDelta(new Vector2(targetWidth, height));

        LayoutElement layout = btnObj.AddComponent<LayoutElement>();
        layout.SetDimensions(targetWidth, height, targetWidth, height, 0f, 0f);

        Transform? fillTransform = btnObj.transform.Find("Fill");
        Transform targetParent = fillTransform != null ? fillTransform : btnObj.transform;

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.SetActive(false);
        textObj.transform.SetParent(targetParent, false);

        textObj.GetComponent<RectTransform>()
            .SetAnchor(Vector2.zero, Vector2.one)
            .SetOffsets(new Vector2(3f, 0f), new Vector2(-3f, 0f));

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset? font = ResolveFont();
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
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        textObj.SetActive(true);

        if (enableHover)
        {
            CyberHoverHandler hover = btnObj.AddComponent<CyberHoverHandler>();
            Image borderImg = btnObj.GetComponent<Image>();
            Image? fillImg = fillTransform != null ? fillTransform.GetComponent<Image>() : null;
            bool isRed = borderColor == CyberPalette.ColorErrorRed;
            Color hoverBorder = isRed ? new Color(1f, 0.45f, 0.45f, 1f) : CyberPalette.ColorIceBlueBright;
            Color pressedBorder = isRed ? new Color(1f, 0.85f, 0.85f, 1f) : Color.white;
            Color hoverFill = isRed ? new Color(0.18f, 0.04f, 0.04f, 1f) : CyberPalette.ColorCardSurface;
            Color pressedFill = isRed ? new Color(0.35f, 0.08f, 0.08f, 1f) : new Color(0.12f, 0.22f, 0.32f, 1f);
            Color hoverText = isRed ? Color.white : CyberPalette.ColorIceBlueBright;
            Color pressedText = Color.white;
            hover.Init(borderImg, borderColor, hoverBorder, fillImg, CyberPalette.ColorVoidBlack, hoverFill, pressedBorder, pressedFill, tmp, textColor, hoverText, pressedText);
        }

        Button btn = btnObj.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(new UnityAction(onClick));

        return btnObj;
    }

    public static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, Color color, float fontSize = 11f, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        GameObject labelObj = new GameObject(name, typeof(RectTransform));
        labelObj.SetActive(false);
        labelObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset? font = ResolveFont();
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
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        labelObj.SetActive(true);
        return tmp;
    }

    public static (GameObject Root, TMP_InputField Input) CreateInputField(
        Transform parent,
        string name,
        string initialText,
        Action<string> onCommit,
        float width = 120f,
        float height = 24f,
        string placeholderText = "",
        bool multiline = false)
    {
        GameObject root = CreatePanel(parent, name, CyberPalette.ColorInputGroove, CyberPalette.ColorInputWell, 1f);
        float targetWidth = width > 0f ? width : 120f;

        root.GetComponent<RectTransform>().SetSizeDelta(new Vector2(targetWidth, height));

        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.minWidth = targetWidth;
        layout.preferredWidth = targetWidth;
        layout.flexibleWidth = width > 0f ? 0f : 1f;
        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleHeight = 0f;

        Transform? fillTransform = root.transform.Find("Fill");
        Transform targetParent = fillTransform != null ? fillTransform : root.transform;

        GameObject bottomLine = new GameObject("BottomAccent", typeof(RectTransform), typeof(Image));
        bottomLine.transform.SetParent(targetParent, false);
        bottomLine.GetComponent<RectTransform>()
            .SetAnchor(new Vector2(0f, 0f), new Vector2(1f, 0f))
            .SetPivot(new Vector2(0.5f, 0f))
            .SetSizeDelta(new Vector2(0f, 1.5f))
            .SetAnchoredPosition(Vector2.zero);

        Image blImg = bottomLine.GetComponent<Image>();
        blImg.color = CyberPalette.ColorInputAccent;
        blImg.raycastTarget = false;

        float scaledFontSize = GetScaledFontSize(10f);

        GameObject textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(targetParent, false);
        RectTransform taRT = textArea.GetComponent<RectTransform>()
            .SetAnchor(Vector2.zero, Vector2.one)
            .SetOffsets(
                multiline ? new Vector2(6f, 6f) : new Vector2(8f, 0f),
                multiline ? new Vector2(-6f, -6f) : new Vector2(-8f, 0f)
            );

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.SetActive(false);
        textObj.transform.SetParent(textArea.transform, false);
        textObj.GetComponent<RectTransform>()
            .SetAnchor(Vector2.zero, Vector2.one)
            .SetOffsets(Vector2.zero, Vector2.zero);

        TMP_FontAsset? standardFont = ResolveFont();

        TextMeshProUGUI textTmp = textObj.AddComponent<TextMeshProUGUI>();
        if (standardFont != null)
        {
            textTmp.font = standardFont;
            if (standardFont.material != null)
            {
                textTmp.fontSharedMaterial = standardFont.material;
            }
        }

        textTmp.fontSize = scaledFontSize;
        textTmp.color = CyberPalette.ColorTextMain;
        textTmp.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
        textTmp.textWrappingMode = multiline ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        textTmp.richText = false;
        textTmp.overflowMode = TextOverflowModes.Overflow;
        textTmp.raycastTarget = false;
        textObj.SetActive(true);

        GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform));
        placeholderObj.SetActive(false);
        placeholderObj.transform.SetParent(textArea.transform, false);
        placeholderObj.GetComponent<RectTransform>()
            .SetAnchor(Vector2.zero, Vector2.one)
            .SetOffsets(Vector2.zero, Vector2.zero);

        TextMeshProUGUI placeholderTmp = placeholderObj.AddComponent<TextMeshProUGUI>();
        if (standardFont != null)
        {
            placeholderTmp.font = standardFont;
            if (standardFont.material != null)
            {
                placeholderTmp.fontSharedMaterial = standardFont.material;
            }
        }

        placeholderTmp.fontSize = scaledFontSize;
        placeholderTmp.color = new Color(0.35f, 0.48f, 0.58f, 0.55f);
        placeholderTmp.text = placeholderText ?? string.Empty;
        placeholderTmp.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
        placeholderTmp.textWrappingMode = multiline ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        placeholderTmp.richText = false;
        placeholderTmp.raycastTarget = false;
        placeholderObj.SetActive(true);

        TMP_InputField input = root.AddComponent<TMP_InputField>();
        input.textComponent = textTmp;
        input.textViewport = taRT;
        input.targetGraphic = fillTransform != null ? fillTransform.GetComponent<Image>() : root.GetComponent<Image>();
        input.placeholder = placeholderTmp;

        input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
        input.navigation = new Navigation { mode = Navigation.Mode.None };
        input.transition = Selectable.Transition.None;

        input.customCaretColor = false;
        input.caretWidth = 2;
        input.caretBlinkRate = 0.85f;
        input.selectionColor = new Color(0.12f, 0.50f, 0.75f, 0.45f);

        Image borderImg = root.GetComponent<Image>();
        Image? fillImg = fillTransform != null ? fillTransform.GetComponent<Image>() : null;

        CyberHoverHandler hover = root.AddComponent<CyberHoverHandler>();
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

        GameObject iconObj = new GameObject("TextPadIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(leftArea, false);

        RectTransform iconRT = iconObj.GetComponent<RectTransform>()
            .SetAnchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f))
            .SetPivot(new Vector2(0f, 0.5f))
            .SetSizeDelta(new Vector2(13f, 13f));

        label.ForceMeshUpdate();
        iconRT.anchoredPosition = new Vector2(label.preferredWidth + 6f, 0f);

        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = IconFactory.GetTextPadIcon();
        iconImg.color = CyberPalette.ColorTextMuted;
        iconImg.raycastTarget = false;

        return iconRT;
    }
}
