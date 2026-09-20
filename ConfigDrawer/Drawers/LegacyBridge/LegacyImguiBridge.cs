using System;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.LegacyBridge;

public class LegacyImguiBridge : MonoBehaviour
{
    private SettingEntry? _entry;
    private LayoutElement? _layoutElement;
    private float _currentHeight;
    private string? _lastBoxedValue;
    private float _lastScale = 1f;

    private static Texture2D? _texInput;
    private static Texture2D? _texInputFocused;
    private static Texture2D? _texButton;
    private static Texture2D? _texButtonHover;
    private static Texture2D? _texButtonActive;
    private static Texture2D? _texSliderTrack;
    private static Texture2D? _texSliderThumb;
    private static Texture2D? _texSliderThumbHover;

    private static Texture2D MakeBorderedTex(int w, int h, Color fill, Color border)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };
        var pix = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                pix[y * w + x] = (x == 0 || x == w - 1 || y == 0 || y == h - 1) ? border : fill;
            }
        }
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }

    private static Texture2D MakeSolidTex(int w, int h, Color color)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };
        var pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = color;
        }
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }

    public static float CalculateHeight(SettingEntry? entry, string rawVal)
    {
        if (entry == null)
        {
            return 40f;
        }

        var items = rawVal.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var count = Mathf.Max(items.Length, 1);

        if (entry.Key.IndexOf("Drop", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return count * 90.5f + 10f;
        }

        if (entry.Key.IndexOf("Upgrade", StringComparison.OrdinalIgnoreCase) >= 0 || entry.Key.IndexOf("Quality", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return count * 23f + 26f;
        }

        return count * 23f + 8f;
    }

    private float GetCanvasScale()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.scaleFactor > 0.01f)
        {
            return canvas.scaleFactor;
        }

        if (ConfigDrawerWindow.Instance != null)
        {
            var winCanvas = ConfigDrawerWindow.Instance.GetComponent<Canvas>();
            if (winCanvas != null && winCanvas.scaleFactor > 0.01f)
            {
                return winCanvas.scaleFactor;
            }
        }

        return 1f;
    }

    private void ApplyHeight(float screenHeight)
    {
        _currentHeight = screenHeight;
        var scale = GetCanvasScale();
        var canvasHeight = screenHeight / scale;

        if (_layoutElement != null)
        {
            _layoutElement.minHeight = canvasHeight;
            _layoutElement.preferredHeight = canvasHeight;
        }

        var rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, canvasHeight);
        }

        var scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect != null && scrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        }
        else if (transform.parent is RectTransform parentRT)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRT);
        }
    }

    public void Bind(SettingEntry entry, LayoutElement layoutElement)
    {
        _entry = entry ?? throw new ArgumentNullException(nameof(entry));
        _layoutElement = layoutElement;
        var rawVal = entry.ConfigEntry?.BoxedValue?.ToString() ?? string.Empty;
        _lastBoxedValue = rawVal;
        _lastScale = GetCanvasScale();
        var screenH = CalculateHeight(entry, rawVal);
        ApplyHeight(screenH);
    }

    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        GameObject? subpanelObj = null;
        TextMeshProUGUI? labelTmp = null;
        RectTransform? iconRT = null;
        var isExpanded = false;

        var arrowColorHex = ColorUtility.ToHtmlStringRGB(CyberPalette.ColorGlacialMint);

        void UpdateLeftLabel()
        {
            if (labelTmp != null)
            {
                var arrow = isExpanded ? "▼" : "▶";
                labelTmp.richText = true;
                labelTmp.text = $"<color=#{arrowColorHex}><b>{arrow}</b></color>  {entry.DispName}";
                labelTmp.ForceMeshUpdate();
                if (iconRT != null)
                {
                    iconRT.anchoredPosition = new Vector2(labelTmp.preferredWidth + 6f, 0f);
                }
            }
        }

        var rowObj = DrawerDispatcher.CreateRowContainer(parent, entry, out _, () =>
        {
            UpdateLeftLabel();
        }, 0f);

        var leftArea = rowObj.transform.Find("Fill/LeftArea");
        var leftRT = leftArea?.GetComponent<RectTransform>();
        if (leftRT != null)
        {
            leftRT.offsetMax = new Vector2(-80f, 0f);
        }

        labelTmp = leftArea?.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (labelTmp != null)
        {
            labelTmp.raycastTarget = false;
            if (leftArea != null)
            {
                iconRT = UiFactory.AttachTextPadIcon(leftArea, labelTmp);
            }
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
            var scrollRect = parent.GetComponentInParent<ScrollRect>();
            if (scrollRect != null && scrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            }
            else if (parent is RectTransform pRT)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(pRT);
            }
        };

        DrawerDispatcher.AttachBarToggle(rowObj, toggleAction);

        subpanelObj = UiFactory.CreatePanel(parent, $"LegacyDrawer_{entry.Key}", CyberPalette.ColorBorderCard, CyberPalette.ColorVoidBlack, 1f);
        subpanelObj.transform.SetSiblingIndex(rowObj.transform.GetSiblingIndex() + 1);

        var le = subpanelObj.AddComponent<LayoutElement>();
        le.flexibleHeight = 0f;
        le.flexibleWidth = 1f;

        var bridge = subpanelObj.AddComponent<LegacyImguiBridge>();
        bridge.Bind(entry, le);

        subpanelObj.SetActive(false);

        return rowObj;
    }

    private void Update()
    {
        if (_entry?.ConfigEntry == null)
        {
            return;
        }

        var currentVal = _entry.ConfigEntry.BoxedValue?.ToString() ?? string.Empty;
        var currentScale = GetCanvasScale();
        if (_lastBoxedValue == null || currentVal != _lastBoxedValue || Mathf.Abs(_lastScale - currentScale) > 0.01f)
        {
            _lastBoxedValue = currentVal;
            _lastScale = currentScale;
            var targetHeight = CalculateHeight(_entry, currentVal);
            ApplyHeight(targetHeight);
        }
    }

    private void OnGUI()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (ConfigDrawerWindow.Instance == null || !ConfigDrawerWindow.Instance.IsVisible)
        {
            return;
        }

        var entry = _entry;
        if (entry?.CustomDrawer == null || entry.ConfigEntry == null)
        {
            return;
        }

        var rt = GetComponent<RectTransform>();
        if (rt == null)
        {
            return;
        }

        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        var bottomLeft = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        var topRight = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

        var screenY = Screen.height - topRight.y;
        var screenX = bottomLeft.x;
        var screenW = topRight.x - bottomLeft.x;
        var screenH = topRight.y - bottomLeft.y;

        float clipTop = 0f;
        float clipBottom = Screen.height;

        var scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect != null && scrollRect.viewport != null)
        {
            var vpCorners = new Vector3[4];
            scrollRect.viewport.GetWorldCorners(vpCorners);
            var vpBottomLeft = RectTransformUtility.WorldToScreenPoint(null, vpCorners[0]);
            var vpTopRight = RectTransformUtility.WorldToScreenPoint(null, vpCorners[2]);
            clipTop = Screen.height - vpTopRight.y;
            clipBottom = Screen.height - vpBottomLeft.y;

            if (screenY + screenH < clipTop || screenY > clipBottom)
            {
                return;
            }
        }

        var visibleY = Mathf.Max(screenY, clipTop);
        var visibleBottom = Mathf.Min(screenY + screenH, clipBottom);
        var visibleH = visibleBottom - visibleY;

        if (visibleH <= 5f)
        {
            return;
        }

        if (Event.current.type == EventType.ScrollWheel)
        {
            if (scrollRect != null)
            {
                var pointerData = new PointerEventData(EventSystem.current)
                {
                    scrollDelta = new Vector2(-Event.current.delta.x, -Event.current.delta.y)
                };
                scrollRect.OnScroll(pointerData);
                Event.current.Use();
                return;
            }
        }

        var areaW = Mathf.Max(screenW - 16f, 150f);
        var areaH = screenH + 20f;

        _texInput ??= MakeBorderedTex(6, 6, CyberPalette.ColorInputWell, CyberPalette.ColorInputGroove);
        _texInputFocused ??= MakeBorderedTex(6, 6, CyberPalette.ColorInputWell, CyberPalette.ColorIceBlueBright);
        _texButton ??= MakeBorderedTex(6, 6, CyberPalette.ColorCardSurface, CyberPalette.ColorBorderSubtle);
        _texButtonHover ??= MakeBorderedTex(6, 6, new Color(0.08f, 0.15f, 0.22f, 1f), CyberPalette.ColorIceBlueBright);
        _texButtonActive ??= MakeBorderedTex(6, 6, new Color(0.02f, 0.25f, 0.25f, 1f), CyberPalette.ColorGlacialMint);
        _texSliderTrack ??= MakeBorderedTex(6, 6, new Color(0.08f, 0.14f, 0.20f, 1f), CyberPalette.ColorBorderSubtle);
        _texSliderThumb ??= MakeSolidTex(6, 6, CyberPalette.ColorIceBlueBright);
        _texSliderThumbHover ??= MakeSolidTex(6, 6, Color.white);

        var prevContentColor = GUI.contentColor;
        var prevColor = GUI.color;

        var prevLabelText = GUI.skin.label.normal.textColor;
        var prevLabelSize = GUI.skin.label.fontSize;
        var prevLabelMargin = GUI.skin.label.margin;
        var prevLabelPadding = GUI.skin.label.padding;
        var prevLabelStretch = GUI.skin.label.stretchWidth;

        var prevBtnBg = GUI.skin.button.normal.background;
        var prevBtnHoverBg = GUI.skin.button.hover.background;
        var prevBtnActiveBg = GUI.skin.button.active.background;
        var prevBtnText = GUI.skin.button.normal.textColor;
        var prevBtnHoverText = GUI.skin.button.hover.textColor;
        var prevBtnActiveText = GUI.skin.button.active.textColor;
        var prevBtnBorder = GUI.skin.button.border;
        var prevBtnAlign = GUI.skin.button.alignment;
        var prevBtnMargin = GUI.skin.button.margin;
        var prevBtnPadding = GUI.skin.button.padding;

        var prevInputBg = GUI.skin.textField.normal.background;
        var prevInputFocusBg = GUI.skin.textField.focused.background;
        var prevInputText = GUI.skin.textField.normal.textColor;
        var prevInputFocusText = GUI.skin.textField.focused.textColor;
        var prevInputBorder = GUI.skin.textField.border;
        var prevInputMargin = GUI.skin.textField.margin;
        var prevInputPadding = GUI.skin.textField.padding;
        var prevInputSize = GUI.skin.textField.fontSize;

        var prevToggleText = GUI.skin.toggle.normal.textColor;
        var prevToggleHoverText = GUI.skin.toggle.hover.textColor;
        var prevToggleOnText = GUI.skin.toggle.onNormal.textColor;
        var prevToggleMargin = GUI.skin.toggle.margin;
        var prevTogglePadding = GUI.skin.toggle.padding;

        var prevSliderTrack = GUI.skin.horizontalSlider.normal.background;
        var prevSliderStretch = GUI.skin.horizontalSlider.stretchWidth;
        var prevSliderHeight = GUI.skin.horizontalSlider.fixedHeight;
        var prevSliderMargin = GUI.skin.horizontalSlider.margin;
        var prevSliderThumb = GUI.skin.horizontalSliderThumb.normal.background;
        var prevSliderThumbHover = GUI.skin.horizontalSliderThumb.hover.background;
        var prevThumbW = GUI.skin.horizontalSliderThumb.fixedWidth;
        var prevThumbH = GUI.skin.horizontalSliderThumb.fixedHeight;

        GUI.contentColor = Color.white;
        GUI.color = Color.white;

        GUI.skin.label.normal.textColor = CyberPalette.ColorTextMain;
        GUI.skin.label.fontSize = 11;
        GUI.skin.label.stretchWidth = false;
        GUI.skin.label.margin = new RectOffset(2, 2, 4, 4);
        GUI.skin.label.padding = new RectOffset(1, 1, 2, 2);

        GUI.skin.button.normal.background = _texButton;
        GUI.skin.button.hover.background = _texButtonHover;
        GUI.skin.button.active.background = _texButtonActive;
        GUI.skin.button.normal.textColor = CyberPalette.ColorIceBlueBright;
        GUI.skin.button.hover.textColor = Color.white;
        GUI.skin.button.active.textColor = CyberPalette.ColorGlacialMint;
        GUI.skin.button.border = new RectOffset(1, 1, 1, 1);
        GUI.skin.button.alignment = TextAnchor.MiddleCenter;
        GUI.skin.button.margin = new RectOffset(2, 2, 3, 3);
        GUI.skin.button.padding = new RectOffset(3, 3, 1, 1);

        GUI.skin.textField.normal.background = _texInput;
        GUI.skin.textField.focused.background = _texInputFocused;
        GUI.skin.textField.normal.textColor = Color.white;
        GUI.skin.textField.focused.textColor = CyberPalette.ColorIceBlueBright;
        GUI.skin.textField.border = new RectOffset(1, 1, 1, 1);
        GUI.skin.textField.fontSize = 10;
        GUI.skin.textField.margin = new RectOffset(2, 2, 3, 3);
        GUI.skin.textField.padding = new RectOffset(3, 2, 2, 2);

        GUI.skin.toggle.normal.textColor = CyberPalette.ColorTextMain;
        GUI.skin.toggle.hover.textColor = Color.white;
        GUI.skin.toggle.onNormal.textColor = CyberPalette.ColorIceBlueBright;
        GUI.skin.toggle.margin = new RectOffset(2, 2, 4, 4);
        GUI.skin.toggle.padding = new RectOffset(18, 4, 2, 2);

        GUI.skin.horizontalSlider.normal.background = _texSliderTrack;
        GUI.skin.horizontalSlider.stretchWidth = false;
        GUI.skin.horizontalSlider.fixedHeight = 8;
        GUI.skin.horizontalSlider.margin = new RectOffset(2, 2, 6, 6);
        GUI.skin.horizontalSliderThumb.normal.background = _texSliderThumb;
        GUI.skin.horizontalSliderThumb.hover.background = _texSliderThumbHover;
        GUI.skin.horizontalSliderThumb.fixedWidth = 12;
        GUI.skin.horizontalSliderThumb.fixedHeight = 16;

        GUI.BeginGroup(new Rect(screenX + 8f, visibleY + 2f, areaW, visibleH - 4f));
        GUILayout.BeginArea(new Rect(0f, screenY - visibleY, areaW, areaH + 10f));

        try
        {
            entry.CustomDrawer(entry.ConfigEntry);
        }
        catch (Exception ex)
        {
            GUILayout.Label($"[CustomDrawer Exception]: {ex.Message}");
        }
        finally
        {
            GUILayout.EndArea();
            GUI.EndGroup();

            GUI.contentColor = prevContentColor;
            GUI.color = prevColor;

            GUI.skin.label.normal.textColor = prevLabelText;
            GUI.skin.label.fontSize = prevLabelSize;
            GUI.skin.label.margin = prevLabelMargin;
            GUI.skin.label.padding = prevLabelPadding;
            GUI.skin.label.stretchWidth = prevLabelStretch;

            GUI.skin.button.normal.background = prevBtnBg;
            GUI.skin.button.hover.background = prevBtnHoverBg;
            GUI.skin.button.active.background = prevBtnActiveBg;
            GUI.skin.button.normal.textColor = prevBtnText;
            GUI.skin.button.hover.textColor = prevBtnHoverText;
            GUI.skin.button.active.textColor = prevBtnActiveText;
            GUI.skin.button.border = prevBtnBorder;
            GUI.skin.button.alignment = prevBtnAlign;
            GUI.skin.button.margin = prevBtnMargin;
            GUI.skin.button.padding = prevBtnPadding;

            GUI.skin.textField.normal.background = prevInputBg;
            GUI.skin.textField.focused.background = prevInputFocusBg;
            GUI.skin.textField.normal.textColor = prevInputText;
            GUI.skin.textField.focused.textColor = prevInputFocusText;
            GUI.skin.textField.border = prevInputBorder;
            GUI.skin.textField.fontSize = prevInputSize;
            GUI.skin.textField.margin = prevInputMargin;
            GUI.skin.textField.padding = prevInputPadding;

            GUI.skin.toggle.normal.textColor = prevToggleText;
            GUI.skin.toggle.hover.textColor = prevToggleHoverText;
            GUI.skin.toggle.onNormal.textColor = prevToggleOnText;
            GUI.skin.toggle.margin = prevToggleMargin;
            GUI.skin.toggle.padding = prevTogglePadding;

            GUI.skin.horizontalSlider.normal.background = prevSliderTrack;
            GUI.skin.horizontalSlider.stretchWidth = prevSliderStretch;
            GUI.skin.horizontalSlider.fixedHeight = prevSliderHeight;
            GUI.skin.horizontalSlider.margin = prevSliderMargin;
            GUI.skin.horizontalSliderThumb.normal.background = prevSliderThumb;
            GUI.skin.horizontalSliderThumb.hover.background = prevSliderThumbHover;
            GUI.skin.horizontalSliderThumb.fixedWidth = prevThumbW;
            GUI.skin.horizontalSliderThumb.fixedHeight = prevThumbH;
        }
    }
}
