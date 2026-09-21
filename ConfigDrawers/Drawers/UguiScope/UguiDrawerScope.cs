using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.UguiScope;

public class UguiDrawerScope : IUguiDrawerScope
{
    private readonly Stack<Transform> _parentStack = new();
    public Transform Container { get; }
    public bool IsReadOnly { get; set; }

    public UguiDrawerScope(Transform container, bool isReadOnly = false)
    {
        if (container == null)
        {
            throw new ArgumentNullException(nameof(container));
        }

        Container = container;
        IsReadOnly = isReadOnly;
        _parentStack.Push(container);
    }

    private Transform CurrentParent => _parentStack.Count > 0 ? _parentStack.Peek() : Container;

    public IDisposable Horizontal(float spacing = 4f)
    {
        GameObject row = new GameObject("Scope_HGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        row.transform.SetParent(CurrentParent, false);

        HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = spacing;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        ContentSizeFitter csf = row.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        LayoutElement le = row.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        _parentStack.Push(row.transform);
        return new ScopeDisposable(() =>
        {
            if (_parentStack.Count > 1)
            {
                _parentStack.Pop();
            }
        });
    }

    public IDisposable Vertical(float spacing = 4f)
    {
        GameObject col = new GameObject("Scope_VGroup", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        col.transform.SetParent(CurrentParent, false);

        VerticalLayoutGroup vlg = col.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = spacing;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter csf = col.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        LayoutElement le = col.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        _parentStack.Push(col.transform);
        return new ScopeDisposable(() =>
        {
            if (_parentStack.Count > 1)
            {
                _parentStack.Pop();
            }
        });
    }

    public IDisposable Box(Color? backgroundColor = null, float padding = 4f, float spacing = 4f)
    {
        GameObject box = new GameObject("Scope_Box", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
        box.transform.SetParent(CurrentParent, false);

        Image img = box.GetComponent<Image>();
        img.color = backgroundColor ?? new Color(0.04f, 0.08f, 0.14f, 0.45f);

        VerticalLayoutGroup vlg = box.GetComponent<VerticalLayoutGroup>();
        int pad = Mathf.RoundToInt(padding);
        vlg.padding = new RectOffset(pad, pad, pad, pad);
        vlg.spacing = spacing;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter csf = box.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        LayoutElement le = box.GetComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        _parentStack.Push(box.transform);
        return new ScopeDisposable(() =>
        {
            if (_parentStack.Count > 1)
            {
                _parentStack.Pop();
            }
        });
    }

    public IDisposable Row(Color? backgroundColor = null, float padding = 4f, float spacing = 4f)
    {
        GameObject row = new GameObject("Scope_Row", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
        row.transform.SetParent(CurrentParent, false);

        Image img = row.GetComponent<Image>();
        img.color = backgroundColor ?? Color.clear;

        HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
        int pad = Mathf.RoundToInt(padding);
        hlg.padding = new RectOffset(pad, pad, pad, pad);
        hlg.spacing = spacing;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        ContentSizeFitter csf = row.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        LayoutElement le = row.GetComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        _parentStack.Push(row.transform);
        return new ScopeDisposable(() =>
        {
            if (_parentStack.Count > 1)
            {
                _parentStack.Pop();
            }
        });
    }

    public void Separator(float height = 1f, Color? color = null)
    {
        GameObject sep = new GameObject("Scope_Separator", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        sep.transform.SetParent(CurrentParent, false);

        Image img = sep.GetComponent<Image>();
        img.color = color ?? new Color(0.12f, 0.22f, 0.32f, 0.45f);

        LayoutElement le = sep.GetComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
        le.flexibleHeight = 0f;
        le.flexibleWidth = 1f;
    }

    public void Label(string text, float width = -1f)
    {
        TextMeshProUGUI label = UiFactory.CreateLabel(CurrentParent, "Scope_Label", text, CyberPalette.ColorTextMain, 10.5f);
        LayoutElement le = label.gameObject.AddComponent<LayoutElement>();
        if (width > 0f)
        {
            le.minWidth = width;
            le.preferredWidth = width;
        }
        else
        {
            label.ForceMeshUpdate();
            float pw = label.preferredWidth + 6f;
            le.minWidth = pw;
            le.preferredWidth = pw;
        }
        le.flexibleWidth = 0f;
        le.minHeight = 22f;
        le.preferredHeight = 22f;
        le.flexibleHeight = 0f;
    }

    public void TextField(string value, Action<string> onCommit, float width = -1f)
    {
        float targetWidth = width > 0f ? width : 120f;
        (GameObject root, TMP_InputField input) = UiFactory.CreateInputField(CurrentParent, "Scope_InputField", value, onCommit, targetWidth, 22f);
    }

    public void Button(string text, Action onClick, float width = 50f)
    {
        Button(text, onClick, width, null);
    }

    public void Button(string text, Action onClick, float width, string? tooltip)
    {
        bool isDelete = text == "X" || text == "x";
        Color border = isDelete ? new Color(0.6f, 0.18f, 0.18f, 0.6f) : CyberPalette.ColorBorderSubtle;
        Color textColor = isDelete ? new Color(0.95f, 0.45f, 0.45f, 1f) : CyberPalette.ColorIceBlueBright;
        GameObject btnObj = UiFactory.CreateCyberButton(CurrentParent, "Scope_Btn", text, onClick, border, textColor, width, 22f);

        string? resolvedTooltip = tooltip;
        if (string.IsNullOrEmpty(resolvedTooltip))
        {
            if (isDelete)
            {
                resolvedTooltip = "Delete";
            }
            else if (text == "+")
            {
                resolvedTooltip = "Add";
            }
        }

        if (!string.IsNullOrEmpty(resolvedTooltip))
        {
            ButtonTooltipHandler.Attach(btnObj, resolvedTooltip!, "");
        }
    }

    public void Slider(float value, float min, float max, Action<float> onChanged, float width = 120f)
    {
        Slider(value, min, max, onChanged, width, null);
    }

    public void Slider(float value, float min, float max, Action<float> onChanged, float width, string? format)
    {
        float targetWidth = width > 0f ? width : 120f;
        GameObject sliderObj = new GameObject("Scope_Slider", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Slider));
        sliderObj.transform.SetParent(CurrentParent, false);

        Image hitArea = sliderObj.GetComponent<Image>();
        hitArea.color = Color.clear;
        hitArea.raycastTarget = true;

        RectTransform sliderRT = sliderObj.GetComponent<RectTransform>();
        sliderRT.sizeDelta = new Vector2(targetWidth, 20f);

        LayoutElement le = sliderObj.GetComponent<LayoutElement>();
        le.minWidth = targetWidth;
        le.preferredWidth = targetWidth;
        le.flexibleWidth = 0f;
        le.minHeight = 20f;
        le.preferredHeight = 20f;
        le.flexibleHeight = 0f;

        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.5f);
        bgRT.anchorMax = new Vector2(1f, 0.5f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.sizeDelta = new Vector2(0f, 3f);
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = CyberPalette.ColorInputGroove;
        bgImg.raycastTarget = true;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRT.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRT.pivot = new Vector2(0.5f, 0.5f);
        fillAreaRT.sizeDelta = new Vector2(0f, 3f);
        fillAreaRT.offsetMin = new Vector2(4f, -1.5f);
        fillAreaRT.offsetMax = new Vector2(-4f, 1.5f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.sizeDelta = Vector2.zero;
        Image fillImg = fill.GetComponent<Image>();
        fillImg.color = CyberPalette.ColorCyberTeal;

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRT = handleArea.GetComponent<RectTransform>();
        handleAreaRT.anchorMin = new Vector2(0f, 0.5f);
        handleAreaRT.anchorMax = new Vector2(1f, 0.5f);
        handleAreaRT.pivot = new Vector2(0.5f, 0.5f);
        handleAreaRT.sizeDelta = new Vector2(0f, 13f);
        handleAreaRT.offsetMin = new Vector2(4f, -6.5f);
        handleAreaRT.offsetMax = new Vector2(-4f, 6.5f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRT = handle.GetComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0f, 0f);
        handleRT.anchorMax = new Vector2(0f, 1f);
        handleRT.pivot = new Vector2(0.5f, 0.5f);
        handleRT.sizeDelta = new Vector2(6.5f, 0f);
        Image handleImg = handle.GetComponent<Image>();
        handleImg.color = CyberPalette.ColorIceBlueBright;
        handleImg.raycastTarget = true;

        Slider slider = sliderObj.GetComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;
        slider.interactable = !IsReadOnly;

        ColorBlock colors = slider.colors;
        colors.normalColor = CyberPalette.ColorIceBlueBright;
        colors.highlightedColor = Color.white;
        colors.pressedColor = CyberPalette.ColorGlacialMint;
        colors.disabledColor = CyberPalette.ColorTextMuted;
        slider.colors = colors;

        bool isUpdating = false;

        string FormatInputValue(float v)
        {
            if (Math.Abs(v - Mathf.Round(v)) < 0.00001f)
            {
                return Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture);
            }
            return Math.Round(v, 4).ToString(CultureInfo.InvariantCulture);
        }

        string FormatValue(float v)
        {
            if (string.IsNullOrEmpty(format))
            {
                return "";
            }
            if (format!.Contains("%"))
            {
                string percentFormat = format.Replace("%", "");
                return string.Format(percentFormat, v * 100f) + "%";
            }
            return string.Format(format, v);
        }

        TMP_InputField? inField = null;
        TextMeshProUGUI? formatLabel = null;

        (GameObject inRoot, TMP_InputField createdField) = UiFactory.CreateInputField(CurrentParent, "SliderInput", FormatInputValue(value), text =>
        {
            if (isUpdating)
            {
                return;
            }

            string clean = text.Trim();
            bool isPct = clean.EndsWith("%");
            if (isPct)
            {
                clean = clean.TrimEnd('%').Trim();
            }

            if (float.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsed))
            {
                if (isPct && format != null && format.Contains("%"))
                {
                    parsed /= 100f;
                }
                parsed = Mathf.Clamp(parsed, min, max);
                isUpdating = true;
                slider.value = parsed;
                if (inField != null)
                {
                    inField.text = FormatInputValue(parsed);
                }
                if (formatLabel != null && !string.IsNullOrEmpty(format))
                {
                    formatLabel.text = FormatValue(parsed);
                }
                isUpdating = false;
                onChanged?.Invoke(parsed);
            }
            else if (inField != null)
            {
                inField.text = FormatInputValue(slider.value);
            }
        }, 52f, 20f);

        inField = createdField;
        inField.interactable = !IsReadOnly;

        if (!string.IsNullOrEmpty(format))
        {
            string initialText = FormatValue(value);
            formatLabel = UiFactory.CreateLabel(CurrentParent, "SliderLabel", initialText, CyberPalette.ColorIceBlueBright, 9.5f);
            formatLabel.ForceMeshUpdate();
            LayoutElement formatLe = formatLabel.gameObject.AddComponent<LayoutElement>();
            formatLe.minWidth = 40f;
            formatLe.preferredWidth = 40f;
            formatLe.flexibleWidth = 0f;
        }

        slider.onValueChanged.AddListener(new UnityAction<float>(val =>
        {
            if (isUpdating)
            {
                return;
            }
            isUpdating = true;
            if (inField != null)
            {
                inField.text = FormatInputValue(val);
            }
            if (formatLabel != null && !string.IsNullOrEmpty(format))
            {
                formatLabel.text = FormatValue(val);
            }
            isUpdating = false;
            onChanged?.Invoke(val);
        }));
    }

    public void Toggle(bool value, string label, Action<bool> onChanged)
    {
        GameObject row = new GameObject("Scope_ToggleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        row.transform.SetParent(CurrentParent, false);

        HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        ContentSizeFitter csf = row.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        bool state = value;
        string btnText = state ? "ON" : "OFF";
        Color border = state ? CyberPalette.ColorGlacialMint : CyberPalette.ColorBorderSubtle;
        Color text = state ? CyberPalette.ColorGlacialMint : CyberPalette.ColorTextMuted;

        GameObject? btnObj = null;
        btnObj = UiFactory.CreateCyberButton(row.transform, "ToggleBtn", btnText, () =>
        {
            state = !state;
            if (btnObj != null)
            {
                TextMeshProUGUI tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                Image img = btnObj.GetComponent<Image>();
                if (tmp != null)
                {
                    tmp.text = state ? "ON" : "OFF";
                    tmp.color = state ? CyberPalette.ColorGlacialMint : CyberPalette.ColorTextMuted;
                }
                if (img != null)
                {
                    img.color = state ? CyberPalette.ColorGlacialMint : CyberPalette.ColorBorderSubtle;
                }
            }
            onChanged?.Invoke(state);
        }, border, text, 40f, 20f);

        if (!string.IsNullOrEmpty(label))
        {
            TextMeshProUGUI lbl = UiFactory.CreateLabel(row.transform, "ToggleLabel", label, CyberPalette.ColorTextMain, 10.5f);
            lbl.ForceMeshUpdate();
            LayoutElement le = lbl.gameObject.AddComponent<LayoutElement>();
            float pw = lbl.preferredWidth + 6f;
            le.minWidth = pw;
            le.preferredWidth = pw;
            le.flexibleWidth = 0f;
            le.minHeight = 20f;
            le.preferredHeight = 20f;
            le.flexibleHeight = 0f;
        }
    }

    public void Space(float pixels)
    {
        GameObject space = new GameObject("Scope_Space", typeof(RectTransform), typeof(LayoutElement));
        space.transform.SetParent(CurrentParent, false);
        LayoutElement le = space.GetComponent<LayoutElement>();
        le.minWidth = pixels;
        le.preferredWidth = pixels;
        le.minHeight = pixels;
        le.preferredHeight = pixels;
        le.flexibleWidth = 0f;
        le.flexibleHeight = 0f;
    }

    private class ScopeDisposable : IDisposable
    {
        private readonly Action _onDispose;
        public ScopeDisposable(Action onDispose) => _onDispose = onDispose;
        public void Dispose() => _onDispose?.Invoke();
    }
}
