using System;
using BepInEx.Configuration;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.Drawers.JsonDrawer;
using BepInEx.ConfigDrawers.Drawers.LegacyBridge;
using BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;
using BepInEx.ConfigDrawers.Drawers.StructuredDrawers;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers;

public static class DrawerDispatcher
{
    public static GameObject DrawSetting(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        if (entry.HasCustomUguiDrawer)
        {
            return BepInEx.ConfigDrawers.Drawers.UguiScope.UguiScopeDrawer.Draw(parent, entry);
        }

        if (entry.CustomDrawer != null || entry.CustomHotkeyDrawer != null)
        {
            return LegacyImguiBridge.Draw(parent, entry);
        }

        if (DataGridDrawer.CanDraw(entry))
        {
            return DataGridDrawer.Draw(parent, entry);
        }

        if (JsonEditorDrawer.CanDraw(entry))
        {
            return JsonEditorDrawer.Draw(parent, entry);
        }

        if (VectorDrawer.CanDraw(entry))
        {
            return VectorDrawer.Draw(parent, entry);
        }

        if (entry.RangeBounds != null)
        {
            return SliderDrawer.Draw(parent, entry);
        }

        if (RadioDrawer.ShouldUseRadio(entry))
        {
            return RadioDrawer.Draw(parent, entry);
        }

        if (entry.SettingType == typeof(bool))
        {
            return BoolDrawer.Draw(parent, entry);
        }

        if (entry.SettingType.IsEnum || entry.AcceptableValuesList != null)
        {
            return EnumDrawer.Draw(parent, entry);
        }

        if (entry.SettingType == typeof(KeyboardShortcut))
        {
            return ShortcutDrawer.Draw(parent, entry);
        }

        if (entry.SettingType == typeof(Color))
        {
            return ColorDrawer.Draw(parent, entry);
        }

        if (IsNumericType(entry.SettingType))
        {
            return NumberDrawer.Draw(parent, entry);
        }

        return TextDrawer.Draw(parent, entry);
    }

    public static GameObject CreateRowContainer(Transform parent, SettingEntry entry, out Transform valueArea, Action? onReset = null, float controlWidth = 46f)
    {
        var row = UiFactory.CreatePanel(parent, $"Row_{entry.Key}", CyberPalette.ColorBorderCard, CyberPalette.ColorCardSurface, 1f);
        var layout = row.AddComponent<LayoutElement>();
        layout.minHeight = 30f;
        layout.preferredHeight = 30f;
        layout.flexibleHeight = 0f;
        layout.flexibleWidth = 1f;

        var borderImg = row.GetComponent<Image>();
        var fill = row.transform.Find("Fill");
        var target = fill != null ? fill : row.transform;
        var fillImg = fill != null ? fill.GetComponent<Image>() : null;

        var cardHover = row.AddComponent<CyberHoverHandler>();
        cardHover.Init(borderImg, CyberPalette.ColorBorderCard, CyberPalette.ColorIceBlueBright, fillImg, CyberPalette.ColorCardSurface, CyberPalette.ColorVoidBlack);

        var leftArea = new GameObject("LeftArea", typeof(RectTransform), typeof(Image));
        leftArea.transform.SetParent(target, false);
        var leftRT = leftArea.GetComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0f, 0f);
        leftRT.anchorMax = new Vector2(1f, 1f);
        leftRT.offsetMin = new Vector2(8f, 0f);

        float initialMargin = controlWidth > 0f ? -(74f + controlWidth + 8f) : -78f;
        leftRT.offsetMax = new Vector2(initialMargin, 0f);

        var leftImg = leftArea.GetComponent<Image>();
        leftImg.color = Color.clear;
        leftImg.raycastTarget = true;

        var hover = leftArea.AddComponent<HoverCardHandler>();
        hover.Bind(entry);

        var label = UiFactory.CreateLabel(leftArea.transform, "Label", entry.DispName, entry.EntryColor, 10.5f, TextAlignmentOptions.MidlineLeft);
        if (entry.HideSettingName)
        {
            label.gameObject.SetActive(false);
        }
        var labelRT = label.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        var rightArea = new GameObject("RightArea", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rightArea.transform.SetParent(target, false);
        var rightRT = rightArea.GetComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(1f, 0f);
        rightRT.anchorMax = new Vector2(1f, 1f);
        rightRT.pivot = new Vector2(1f, 0.5f);
        rightRT.anchoredPosition = new Vector2(-74f, 0f);
        var w = Mathf.Max(controlWidth, 0f);
        rightRT.sizeDelta = new Vector2(w, 0f);

        var rightHlg = rightArea.GetComponent<HorizontalLayoutGroup>();
        rightHlg.spacing = 3f;
        rightHlg.childAlignment = TextAnchor.MiddleRight;
        rightHlg.childControlWidth = false;
        rightHlg.childControlHeight = false;

        valueArea = rightArea.transform;

        if (entry.CanReset)
        {
            var resetBtn = UiFactory.CreateCyberButton(target, "ResetBtn", "Reset", () =>
            {
                entry.ResetToDefault();
                onReset?.Invoke();
            }, CyberPalette.ColorBorderSubtle, CyberPalette.ColorWarningAmber, 40f, 22f);

            var resetRT = resetBtn.GetComponent<RectTransform>();
            resetRT.anchorMin = new Vector2(1f, 0.5f);
            resetRT.anchorMax = new Vector2(1f, 0.5f);
            resetRT.pivot = new Vector2(1f, 0.5f);
            resetRT.anchoredPosition = new Vector2(-28f, 0f);
        }

        if (onReset != null)
        {
            SettingChangeTracker tracker = row.AddComponent<SettingChangeTracker>();
            tracker.Init(entry, onReset);
        }

        if (entry.IsAdminOnly)
        {
            var iconObj = new GameObject("StatusIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(target, false);
            var iconRT = iconObj.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(1f, 0.5f);
            iconRT.anchorMax = new Vector2(1f, 0.5f);
            iconRT.pivot = new Vector2(1f, 0.5f);
            iconRT.sizeDelta = new Vector2(16f, 16f);
            iconRT.anchoredPosition = new Vector2(-6f, 0f);

            var iconImg = iconObj.GetComponent<Image>();
            iconImg.sprite = entry.CanEdit ? IconFactory.GetSyncIcon() : IconFactory.GetLockIcon();
            iconImg.color = entry.CanEdit ? CyberPalette.ColorIceBlueBright : CyberPalette.ColorWarningAmber;
            iconImg.raycastTarget = true;

            var tooltip = iconObj.AddComponent<StatusIconTooltipHandler>();
            tooltip.Bind(entry);
        }

        return row;
    }

    public static void SetRightControlWidth(GameObject rowObj, float controlWidth)
    {
        var leftRT = rowObj.transform.Find("Fill/LeftArea")?.GetComponent<RectTransform>();
        if (leftRT != null)
        {
            float margin = controlWidth > 0f ? -(74f + controlWidth + 8f) : -78f;
            leftRT.offsetMax = new Vector2(margin, 0f);
        }

        var rightRT = rowObj.transform.Find("Fill/RightArea")?.GetComponent<RectTransform>();
        if (rightRT != null)
        {
            var w = Mathf.Max(controlWidth, 0f);
            rightRT.sizeDelta = new Vector2(w, 0f);
        }
    }

    public static void AttachBarToggle(GameObject rowObj, Action toggleAction)
    {
        var fillObj = rowObj.transform.Find("Fill")?.gameObject ?? rowObj;
        var fillClick = fillObj.GetComponent<ClickableBarHandler>() ?? fillObj.AddComponent<ClickableBarHandler>();
        fillClick.OnClick = toggleAction;

        var rowClick = rowObj.GetComponent<ClickableBarHandler>() ?? rowObj.AddComponent<ClickableBarHandler>();
        rowClick.OnClick = toggleAction;

        var leftObj = rowObj.transform.Find("Fill/LeftArea")?.gameObject;
        if (leftObj != null)
        {
            var leftClick = leftObj.GetComponent<ClickableBarHandler>() ?? leftObj.AddComponent<ClickableBarHandler>();
            leftClick.OnClick = toggleAction;
        }
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) ||
               type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(long) ||
               type == typeof(short) ||
               type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(uint) ||
               type == typeof(ulong) ||
               type == typeof(ushort);
    }
}
