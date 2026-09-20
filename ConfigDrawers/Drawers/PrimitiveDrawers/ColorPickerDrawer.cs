using System;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;

public static class ColorPickerDrawer
{
    private static Texture2D? _spectrumTexture;

    public static GameObject Attach(Transform parent, GameObject rowObj, SettingEntry entry, Action<Color> onRowColorChanged)
    {
        if (parent == null || rowObj == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        var currentColor = entry.ConfigEntry.BoxedValue is Color col ? col : Color.white;
        var subpanelObj = new GameObject($"ColorPicker_{entry.Key}", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        subpanelObj.transform.SetParent(parent, false);
        subpanelObj.transform.SetSiblingIndex(rowObj.transform.GetSiblingIndex() + 1);

        var bgImg = subpanelObj.GetComponent<Image>();
        bgImg.color = CyberPalette.ColorVoidBlack;

        var vlg = subpanelObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(16, 16, 8, 10);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = subpanelObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var subpanelLE = subpanelObj.AddComponent<LayoutElement>();
        subpanelLE.flexibleWidth = 1f;

        var spectrumPanel = UiFactory.CreatePanel(subpanelObj.transform, "SpectrumPanel", CyberPalette.ColorBorderSubtle, CyberPalette.ColorVoidBlack, 1f);
        var spectrumRT = spectrumPanel.GetComponent<RectTransform>();
        spectrumRT.sizeDelta = new Vector2(300f, 75f);
        var specLE = spectrumPanel.AddComponent<LayoutElement>();
        specLE.minHeight = 75f;
        specLE.preferredHeight = 75f;
        specLE.flexibleHeight = 0f;
        specLE.minWidth = 300f;
        specLE.preferredWidth = 300f;
        specLE.flexibleWidth = 0f;

        var specFill = spectrumPanel.transform.Find("Fill") ?? spectrumPanel.transform;

        var rawImageObj = new GameObject("SpectrumRawImage", typeof(RectTransform), typeof(RawImage), typeof(ColorSpectrumPicker));
        rawImageObj.transform.SetParent(specFill, false);
        var rawRT = rawImageObj.GetComponent<RectTransform>();
        rawRT.anchorMin = Vector2.zero;
        rawRT.anchorMax = Vector2.one;
        rawRT.offsetMin = Vector2.zero;
        rawRT.offsetMax = Vector2.zero;

        var rawImg = rawImageObj.GetComponent<RawImage>();
        rawImg.texture = GetOrCreateSpectrumTexture();

        var crosshairObj = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
        crosshairObj.transform.SetParent(rawImageObj.transform, false);
        var chRT = crosshairObj.GetComponent<RectTransform>();
        chRT.sizeDelta = new Vector2(10f, 10f);
        chRT.pivot = new Vector2(0.5f, 0.5f);
        var chImg = crosshairObj.GetComponent<Image>();
        chImg.color = Color.white;
        chImg.raycastTarget = false;

        var chInnerObj = new GameObject("CrosshairInner", typeof(RectTransform), typeof(Image));
        chInnerObj.transform.SetParent(crosshairObj.transform, false);
        var chInnerRT = chInnerObj.GetComponent<RectTransform>();
        chInnerRT.anchorMin = Vector2.zero;
        chInnerRT.anchorMax = Vector2.one;
        chInnerRT.offsetMin = new Vector2(1.5f, 1.5f);
        chInnerRT.offsetMax = new Vector2(-1.5f, -1.5f);
        var chInnerImg = chInnerObj.GetComponent<Image>();
        chInnerImg.color = Color.black;
        chInnerImg.raycastTarget = false;

        var picker = rawImageObj.GetComponent<ColorSpectrumPicker>();

        var presetRow = new GameObject("PresetRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        presetRow.transform.SetParent(subpanelObj.transform, false);
        var pHLG = presetRow.GetComponent<HorizontalLayoutGroup>();
        pHLG.spacing = 5f;
        pHLG.childAlignment = TextAnchor.MiddleLeft;
        pHLG.childControlWidth = false;
        pHLG.childControlHeight = false;
        pHLG.childForceExpandWidth = false;
        pHLG.childForceExpandHeight = false;
        var pLE = presetRow.AddComponent<LayoutElement>();
        pLE.minHeight = 20f;
        pLE.preferredHeight = 20f;
        pLE.flexibleHeight = 0f;

        var curSwatchObj = UiFactory.CreatePanel(presetRow.transform, "CurrentSwatch", CyberPalette.ColorIceBlueBright, currentColor, 1f);
        var csRT = curSwatchObj.GetComponent<RectTransform>();
        csRT.sizeDelta = new Vector2(34f, 20f);
        var csLE = curSwatchObj.AddComponent<LayoutElement>();
        csLE.minWidth = 34f;
        csLE.preferredWidth = 34f;
        csLE.flexibleWidth = 0f;
        csLE.minHeight = 20f;
        csLE.preferredHeight = 20f;
        csLE.flexibleHeight = 0f;
        var swatchFill = curSwatchObj.transform.Find("Fill")?.GetComponent<Image>();

        TMP_InputField? inR = null;
        TMP_InputField? inG = null;
        TMP_InputField? inB = null;
        TMP_InputField? inA = null;
        TMP_InputField? inHex = null;

        var isUpdating = false;

        void UpdateCrosshair(Color c)
        {
            Color.RGBToHSV(c, out var h, out var s, out var v);
            var u = h;
            var vCoord = s < 0.999f ? 0.5f + (1f - s) * 0.5f : v * 0.5f;

            var rect = rawRT.rect;
            var posX = rect.xMin + u * rect.width;
            var posY = rect.yMin + vCoord * rect.height;
            chRT.anchoredPosition = new Vector2(posX, posY);
        }

        void ApplyColor(Color col, bool updateCrosshair = true, bool updateInputs = true)
        {
            if (isUpdating)
            {
                return;
            }

            isUpdating = true;
            currentColor = col;
            entry.SetValue(col);

            if (swatchFill != null)
            {
                swatchFill.color = col;
            }

            if (updateCrosshair)
            {
                UpdateCrosshair(col);
            }

            if (updateInputs)
            {
                var rByte = Mathf.RoundToInt(col.r * 255f);
                var gByte = Mathf.RoundToInt(col.g * 255f);
                var bByte = Mathf.RoundToInt(col.b * 255f);
                var aByte = Mathf.RoundToInt(col.a * 255f);

                if (inR != null) inR.text = rByte.ToString();
                if (inG != null) inG.text = gByte.ToString();
                if (inB != null) inB.text = bByte.ToString();
                if (inA != null) inA.text = aByte.ToString();
                if (inHex != null) inHex.text = ColorUtility.ToHtmlStringRGBA(col);
            }

            onRowColorChanged.Invoke(col);
            isUpdating = false;
        }

        picker.OnPick = (u, v) =>
        {
            if (!entry.CanEdit)
            {
                return;
            }

            Color picked;
            if (v < 0.5f)
            {
                picked = Color.HSVToRGB(u, 1.0f, v / 0.5f);
            }
            else
            {
                picked = Color.HSVToRGB(u, 1.0f - ((v - 0.5f) / 0.5f), 1.0f);
            }

            picked.a = currentColor.a;

            var rect = rawRT.rect;
            var posX = rect.xMin + u * rect.width;
            var posY = rect.yMin + v * rect.height;
            chRT.anchoredPosition = new Vector2(posX, posY);

            ApplyColor(picked, updateCrosshair: false, updateInputs: true);
        };

        var presets = new[]
        {
            Color.white,
            Color.black,
            Color.red,
            Color.green,
            new Color(0f, 0.5f, 1f, 1f),
            Color.yellow,
            Color.cyan,
            Color.magenta,
            new Color(1f, 0.5f, 0f, 1f)
        };

        foreach (var pCol in presets)
        {
            var btn = UiFactory.CreateCyberButton(presetRow.transform, "Preset", "", () =>
            {
                if (entry.CanEdit)
                {
                    ApplyColor(pCol);
                }
            }, CyberPalette.ColorBorderSubtle, Color.clear, 18f, 18f);

            var bFill = btn.transform.Find("Fill")?.GetComponent<Image>();
            if (bFill != null)
            {
                bFill.color = pCol;
            }
        }

        var inputRow = new GameObject("InputRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        inputRow.transform.SetParent(subpanelObj.transform, false);
        var inHLG = inputRow.GetComponent<HorizontalLayoutGroup>();
        inHLG.spacing = 6f;
        inHLG.childAlignment = TextAnchor.MiddleLeft;
        inHLG.childControlWidth = false;
        inHLG.childControlHeight = false;
        inHLG.childForceExpandWidth = false;
        inHLG.childForceExpandHeight = false;

        var inLE = inputRow.AddComponent<LayoutElement>();
        inLE.minHeight = 22f;
        inLE.preferredHeight = 22f;
        inLE.flexibleHeight = 0f;

        void OnRgbChanged()
        {
            if (isUpdating) return;
            var r = byte.TryParse(inR?.text, out var rb) ? rb / 255f : currentColor.r;
            var g = byte.TryParse(inG?.text, out var gb) ? gb / 255f : currentColor.g;
            var b = byte.TryParse(inB?.text, out var bb) ? bb / 255f : currentColor.b;
            var a = byte.TryParse(inA?.text, out var ab) ? ab / 255f : currentColor.a;
            var nextColor = new Color(r, g, b, a);
            ApplyColor(nextColor, updateCrosshair: true, updateInputs: false);
            if (inHex != null)
            {
                inHex.text = ColorUtility.ToHtmlStringRGBA(nextColor);
            }
        }

        void OnHexChanged(string hexStr)
        {
            if (isUpdating) return;
            var str = hexStr.StartsWith("#") ? hexStr : "#" + hexStr;
            if (ColorUtility.TryParseHtmlString(str, out var parsed))
            {
                ApplyColor(parsed, updateCrosshair: true, updateInputs: false);
                var rb = Mathf.RoundToInt(parsed.r * 255f);
                var gb = Mathf.RoundToInt(parsed.g * 255f);
                var bb = Mathf.RoundToInt(parsed.b * 255f);
                var ab = Mathf.RoundToInt(parsed.a * 255f);
                if (inR != null) inR.text = rb.ToString();
                if (inG != null) inG.text = gb.ToString();
                if (inB != null) inB.text = bb.ToString();
                if (inA != null) inA.text = ab.ToString();
            }
        }

        CreateChannelBox(inputRow.transform, "R", new Color(1f, 0.45f, 0.45f, 1f), Mathf.RoundToInt(currentColor.r * 255f).ToString(), _ => OnRgbChanged(), _ => OnRgbChanged(), out inR);
        CreateChannelBox(inputRow.transform, "G", new Color(0.45f, 1f, 0.55f, 1f), Mathf.RoundToInt(currentColor.g * 255f).ToString(), _ => OnRgbChanged(), _ => OnRgbChanged(), out inG);
        CreateChannelBox(inputRow.transform, "B", new Color(0.45f, 0.75f, 1f, 1f), Mathf.RoundToInt(currentColor.b * 255f).ToString(), _ => OnRgbChanged(), _ => OnRgbChanged(), out inB);
        CreateChannelBox(inputRow.transform, "A", CyberPalette.ColorTextMuted, Mathf.RoundToInt(currentColor.a * 255f).ToString(), _ => OnRgbChanged(), _ => OnRgbChanged(), out inA);
        CreateHexBox(inputRow.transform, ColorUtility.ToHtmlStringRGBA(currentColor), OnHexChanged, OnHexChanged, out inHex);

        inR.interactable = entry.CanEdit;
        inG.interactable = entry.CanEdit;
        inB.interactable = entry.CanEdit;
        inA.interactable = entry.CanEdit;
        inHex.interactable = entry.CanEdit;

        subpanelObj.SetActive(false);
        UpdateCrosshair(currentColor);

        return subpanelObj;
    }

    private static GameObject CreateChannelBox(Transform parent, string labelText, Color labelColor, string initialValue, Action<string> onCommit, Action<string> onLiveChange, out TMP_InputField inputField)
    {
        var boxObj = UiFactory.CreatePanel(parent, $"Box_{labelText}", CyberPalette.ColorInputGroove, CyberPalette.ColorInputWell, 1f);
        var boxRT = boxObj.GetComponent<RectTransform>();
        boxRT.sizeDelta = new Vector2(46f, 22f);

        var le = boxObj.AddComponent<LayoutElement>();
        le.minWidth = 46f;
        le.preferredWidth = 46f;
        le.flexibleWidth = 0f;
        le.minHeight = 22f;
        le.preferredHeight = 22f;
        le.flexibleHeight = 0f;

        var fill = boxObj.transform.Find("Fill") ?? boxObj.transform;

        var hlg = fill.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 1f;
        hlg.padding = new RectOffset(3, 3, 1, 1);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        var lbl = UiFactory.CreateLabel(fill, "Lbl", labelText, labelColor, 9.5f, TextAlignmentOptions.MidlineLeft);
        var lblRT = lbl.GetComponent<RectTransform>();
        lblRT.sizeDelta = new Vector2(10f, 20f);
        var lblLE = lbl.gameObject.AddComponent<LayoutElement>();
        lblLE.minWidth = 10f;
        lblLE.preferredWidth = 10f;
        lblLE.flexibleWidth = 0f;

        var textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(fill, false);
        var textRT = textObj.GetComponent<RectTransform>();
        textRT.sizeDelta = new Vector2(28f, 20f);
        var textLE = textObj.AddComponent<LayoutElement>();
        textLE.minWidth = 28f;
        textLE.preferredWidth = 28f;
        textLE.flexibleWidth = 0f;

        var textTmp = textObj.AddComponent<TextMeshProUGUI>();
        var monoFont = UiFactory.ResolveTerminalFont() ?? UiFactory.ResolveFont();
        if (monoFont != null)
        {
            textTmp.font = monoFont;
            if (monoFont.material != null)
            {
                textTmp.fontSharedMaterial = monoFont.material;
            }
        }
        textTmp.fontSize = UiFactory.GetScaledFontSize(10f);
        textTmp.color = CyberPalette.ColorTextMain;
        textTmp.alignment = TextAlignmentOptions.Center;
        textTmp.textWrappingMode = TextWrappingModes.NoWrap;
        textTmp.richText = false;

        var input = boxObj.AddComponent<TMP_InputField>();
        input.textComponent = textTmp;
        input.textViewport = textRT;
        input.targetGraphic = boxObj.GetComponent<Image>();
        input.text = initialValue;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.navigation = new Navigation { mode = Navigation.Mode.None };
        input.customCaretColor = true;
        input.caretColor = CyberPalette.ColorIceBlueBright;
        input.caretWidth = 2;
        input.caretBlinkRate = 0.85f;
        input.selectionColor = new Color(0.12f, 0.50f, 0.75f, 0.45f);

        input.onEndEdit.AddListener(val => onCommit(val));
        input.onValueChanged.AddListener(val => onLiveChange(val));

        inputField = input;
        return boxObj;
    }

    private static GameObject CreateHexBox(Transform parent, string initialValue, Action<string> onCommit, Action<string> onLiveChange, out TMP_InputField inputField)
    {
        var boxObj = UiFactory.CreatePanel(parent, "Box_Hex", CyberPalette.ColorInputGroove, CyberPalette.ColorInputWell, 1f);
        var boxRT = boxObj.GetComponent<RectTransform>();
        boxRT.sizeDelta = new Vector2(92f, 22f);

        var le = boxObj.AddComponent<LayoutElement>();
        le.minWidth = 92f;
        le.preferredWidth = 92f;
        le.flexibleWidth = 0f;
        le.minHeight = 22f;
        le.preferredHeight = 22f;
        le.flexibleHeight = 0f;

        var fill = boxObj.transform.Find("Fill") ?? boxObj.transform;

        var hlg = fill.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 2f;
        hlg.padding = new RectOffset(4, 4, 1, 1);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        var lbl = UiFactory.CreateLabel(fill, "Lbl", "#", CyberPalette.ColorTextMuted, 10f, TextAlignmentOptions.Center);
        var lblRT = lbl.GetComponent<RectTransform>();
        lblRT.sizeDelta = new Vector2(10f, 20f);
        var lblLE = lbl.gameObject.AddComponent<LayoutElement>();
        lblLE.minWidth = 10f;
        lblLE.preferredWidth = 10f;
        lblLE.flexibleWidth = 0f;

        var textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(fill, false);
        var textRT = textObj.GetComponent<RectTransform>();
        textRT.sizeDelta = new Vector2(72f, 20f);
        var textLE = textObj.AddComponent<LayoutElement>();
        textLE.minWidth = 72f;
        textLE.preferredWidth = 72f;
        textLE.flexibleWidth = 0f;

        var textTmp = textObj.AddComponent<TextMeshProUGUI>();
        var monoFont = UiFactory.ResolveTerminalFont() ?? UiFactory.ResolveFont();
        if (monoFont != null)
        {
            textTmp.font = monoFont;
            if (monoFont.material != null)
            {
                textTmp.fontSharedMaterial = monoFont.material;
            }
        }
        textTmp.fontSize = UiFactory.GetScaledFontSize(10f);
        textTmp.color = CyberPalette.ColorTextMain;
        textTmp.alignment = TextAlignmentOptions.Center;
        textTmp.textWrappingMode = TextWrappingModes.NoWrap;
        textTmp.richText = false;

        var input = boxObj.AddComponent<TMP_InputField>();
        input.textComponent = textTmp;
        input.textViewport = textRT;
        input.targetGraphic = boxObj.GetComponent<Image>();
        input.text = initialValue.StartsWith("#") ? initialValue.Substring(1) : initialValue;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.navigation = new Navigation { mode = Navigation.Mode.None };
        input.customCaretColor = true;
        input.caretColor = CyberPalette.ColorIceBlueBright;
        input.caretWidth = 2;
        input.caretBlinkRate = 0.85f;
        input.selectionColor = new Color(0.12f, 0.50f, 0.75f, 0.45f);

        input.onEndEdit.AddListener(val => onCommit("#" + val));
        input.onValueChanged.AddListener(val => onLiveChange("#" + val));

        inputField = input;
        return boxObj;
    }

    private static Texture2D GetOrCreateSpectrumTexture()
    {
        if (_spectrumTexture != null)
        {
            return _spectrumTexture;
        }

        _spectrumTexture = new Texture2D(300, 75, TextureFormat.RGBA32, false);
        _spectrumTexture.wrapMode = TextureWrapMode.Clamp;
        _spectrumTexture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < 75; y++)
        {
            float v = y / 74f;
            for (int x = 0; x < 300; x++)
            {
                float u = x / 299f;
                Color col;
                if (v < 0.5f)
                {
                    col = Color.HSVToRGB(u, 1.0f, v / 0.5f);
                }
                else
                {
                    col = Color.HSVToRGB(u, 1.0f - ((v - 0.5f) / 0.5f), 1.0f);
                }

                _spectrumTexture.SetPixel(x, y, col);
            }
        }

        _spectrumTexture.Apply(false, true);
        return _spectrumTexture;
    }
}
