using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;

public static class SliderDrawer
{
    private const float ControlWidth = 148f;
    private const float SliderWidth = 98f;
    private const float InputWidth = 44f;
    private const int MaxTicksToRender = 20;

    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        var bounds = entry.RangeBounds;
        if (!bounds.HasValue)
        {
            return NumberDrawer.Draw(parent, entry);
        }

        var min = (float)Convert.ToDouble(bounds.Value.Key, CultureInfo.InvariantCulture);
        var max = (float)Convert.ToDouble(bounds.Value.Value, CultureInfo.InvariantCulture);
        var isInteger = IsIntegerType(entry.SettingType);

        Slider? sliderComp = null;
        TMP_InputField? inputField = null;
        TextMeshProUGUI? percentLabel = null;
        var tickImages = new List<(float Value, Image Image)>();
        var isUpdatingInternal = false;

        void RefreshDisplay(float val)
        {
            if (entry.ShowRangeAsPercent)
            {
                if (percentLabel != null)
                {
                    var pct = (max - min) > 0.0001f ? (val - min) / (max - min) : 0f;
                    percentLabel.text = $"{pct:P0}";
                }
            }
            else if (inputField != null)
            {
                inputField.text = isInteger ? $"{Mathf.RoundToInt(val)}" : $"{val:0.##}";
            }

            foreach (var (tickVal, tickImg) in tickImages)
            {
                if (tickImg != null)
                {
                    tickImg.color = val >= tickVal
                        ? CyberPalette.ColorIceBlue
                        : new Color(0.20f, 0.35f, 0.48f, 0.45f);
                }
            }
        }

        var rowObj = DrawerDispatcher.CreateRowContainer(parent, entry, out var valueArea, () =>
        {
            var cur = GetCurrentValue(entry);
            isUpdatingInternal = true;
            if (sliderComp != null)
            {
                sliderComp.value = cur;
            }
            RefreshDisplay(cur);
            isUpdatingInternal = false;
        }, ControlWidth);

        var currentVal = GetCurrentValue(entry);

        if (entry.ShowRangeAsPercent)
        {
            var pctObj = new GameObject("PercentLabel", typeof(RectTransform));
            pctObj.transform.SetParent(valueArea, false);
            var pctRT = pctObj.GetComponent<RectTransform>();
            pctRT.sizeDelta = new Vector2(InputWidth, 22f);
            var pctLe = pctObj.AddComponent<LayoutElement>();
            pctLe.minWidth = InputWidth;
            pctLe.preferredWidth = InputWidth;
            pctLe.flexibleWidth = 0f;

            percentLabel = UiFactory.CreateLabel(pctObj.transform, "Text", "", CyberPalette.ColorIceBlueBright, 9.5f, TextAlignmentOptions.Center);
            percentLabel.raycastTarget = false;
        }
        else
        {
            var (inRoot, inField) = UiFactory.CreateInputField(valueArea, "ValueInput", isInteger ? $"{Mathf.RoundToInt(currentVal)}" : $"{currentVal:0.##}", text =>
            {
                if (isUpdatingInternal)
                {
                    return;
                }

                if (float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                {
                    parsed = Mathf.Clamp(parsed, min, max);
                    if (isInteger)
                    {
                        parsed = Mathf.RoundToInt(parsed);
                    }
                    else
                    {
                        parsed = (float)Math.Round(parsed, 3);
                    }

                    isUpdatingInternal = true;
                    if (sliderComp != null)
                    {
                        sliderComp.value = parsed;
                    }
                    entry.SetValue(Convert.ChangeType(parsed, entry.SettingType, CultureInfo.InvariantCulture));
                    RefreshDisplay(parsed);
                    isUpdatingInternal = false;
                }
            }, InputWidth, 20f, "");

            inputField = inField;
            inputField.interactable = entry.CanEdit;
            inputField.contentType = isInteger ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber;
        }

        sliderComp = CreateSliderComponent(valueArea, min, max, isInteger, currentVal, entry.CanEdit, tickImages, newVal =>
        {
            if (isUpdatingInternal)
            {
                return;
            }

            isUpdatingInternal = true;
            var finalVal = isInteger ? Mathf.RoundToInt(newVal) : (float)Math.Round(newVal, 3);
            entry.SetValue(Convert.ChangeType(finalVal, entry.SettingType, CultureInfo.InvariantCulture));
            RefreshDisplay(finalVal);
            isUpdatingInternal = false;
        });

        RefreshDisplay(currentVal);

        sliderComp.gameObject.transform.SetAsFirstSibling();

        return rowObj;
    }

    private static float GetCurrentValue(SettingEntry entry)
    {
        try
        {
            return (float)Convert.ToDouble(entry.ConfigEntry.BoxedValue, CultureInfo.InvariantCulture);
        }
        catch
        {
            return 0f;
        }
    }

    private static Slider CreateSliderComponent(Transform parent, float min, float max, bool isInteger, float initial, bool interactable, List<(float Value, Image Image)> tickImages, Action<float> onValueChanged)
    {
        var sliderObj = new GameObject("CyberSlider", typeof(RectTransform), typeof(LayoutElement), typeof(Slider));
        sliderObj.transform.SetParent(parent, false);

        var sliderRT = sliderObj.GetComponent<RectTransform>();
        sliderRT.sizeDelta = new Vector2(SliderWidth, 20f);

        var le = sliderObj.GetComponent<LayoutElement>();
        le.minWidth = SliderWidth;
        le.preferredWidth = SliderWidth;
        le.flexibleWidth = 0f;
        le.minHeight = 20f;
        le.preferredHeight = 20f;
        le.flexibleHeight = 0f;

        var bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(sliderObj.transform, false);
        var bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.5f);
        bgRT.anchorMax = new Vector2(1f, 0.5f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.sizeDelta = new Vector2(0f, 3f);
        var bgImg = bgObj.GetComponent<Image>();
        bgImg.color = CyberPalette.ColorInputGroove;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        var fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRT.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRT.pivot = new Vector2(0.5f, 0.5f);
        fillAreaRT.sizeDelta = new Vector2(0f, 3f);
        fillAreaRT.offsetMin = new Vector2(4f, -1.5f);
        fillAreaRT.offsetMax = new Vector2(-4f, 1.5f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.sizeDelta = Vector2.zero;
        var fillImg = fill.GetComponent<Image>();
        fillImg.color = CyberPalette.ColorCyberTeal;

        var range = max - min;
        if (isInteger && range >= 1 && range <= MaxTicksToRender)
        {
            var ticksContainer = new GameObject("TicksContainer", typeof(RectTransform));
            ticksContainer.transform.SetParent(sliderObj.transform, false);
            var ticksRT = ticksContainer.GetComponent<RectTransform>();
            ticksRT.anchorMin = new Vector2(0f, 0.5f);
            ticksRT.anchorMax = new Vector2(1f, 0.5f);
            ticksRT.pivot = new Vector2(0.5f, 0.5f);
            ticksRT.sizeDelta = new Vector2(0f, 4f);
            ticksRT.offsetMin = new Vector2(4f, -7f);
            ticksRT.offsetMax = new Vector2(-4f, -3f);

            var steps = Mathf.RoundToInt(range);
            for (var i = 0; i <= steps; i++)
            {
                var tickVal = min + i;
                var norm = (float)i / steps;

                var tickObj = new GameObject($"Tick_{i}", typeof(RectTransform), typeof(Image));
                tickObj.transform.SetParent(ticksContainer.transform, false);

                var tickRT = tickObj.GetComponent<RectTransform>();
                tickRT.anchorMin = new Vector2(norm, 0f);
                tickRT.anchorMax = new Vector2(norm, 1f);
                tickRT.pivot = new Vector2(0.5f, 0.5f);
                tickRT.sizeDelta = new Vector2(1.5f, 0f);

                var tImg = tickObj.GetComponent<Image>();
                tImg.color = initial >= tickVal
                    ? CyberPalette.ColorIceBlue
                    : new Color(0.20f, 0.35f, 0.48f, 0.45f);

                tickImages.Add((tickVal, tImg));
            }
        }

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObj.transform, false);
        var handleAreaRT = handleArea.GetComponent<RectTransform>();
        handleAreaRT.anchorMin = new Vector2(0f, 0.5f);
        handleAreaRT.anchorMax = new Vector2(1f, 0.5f);
        handleAreaRT.pivot = new Vector2(0.5f, 0.5f);
        handleAreaRT.sizeDelta = new Vector2(0f, 13f);
        handleAreaRT.offsetMin = new Vector2(4f, -6.5f);
        handleAreaRT.offsetMax = new Vector2(-4f, 6.5f);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        var handleRT = handle.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(6.5f, 0f);
        var handleImg = handle.GetComponent<Image>();
        handleImg.color = CyberPalette.ColorIceBlueBright;

        var slider = sliderObj.GetComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = isInteger;
        slider.value = initial;
        slider.interactable = interactable;

        var colors = slider.colors;
        colors.normalColor = CyberPalette.ColorIceBlueBright;
        colors.highlightedColor = Color.white;
        colors.pressedColor = CyberPalette.ColorGlacialMint;
        colors.disabledColor = CyberPalette.ColorTextMuted;
        slider.colors = colors;

        slider.onValueChanged.AddListener(v => onValueChanged(v));

        return slider;
    }

    private static bool IsIntegerType(Type type)
    {
        return type == typeof(int) ||
               type == typeof(long) ||
               type == typeof(short) ||
               type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(uint) ||
               type == typeof(ulong) ||
               type == typeof(ushort);
    }
}
