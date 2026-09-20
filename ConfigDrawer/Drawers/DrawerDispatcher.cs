using System;
using BepInEx.Configuration;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.Drawers.JsonDrawer;
using BepInEx.ConfigDrawers.Drawers.LegacyBridge;
using BepInEx.ConfigDrawers.Drawers.PrimitiveDrawers;
using BepInEx.ConfigDrawers.Drawers.StructuredDrawers;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
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

        if (entry.CustomDrawer != null)
        {
            var bridgeObj = new GameObject($"CustomDrawer_{entry.Key}");
            bridgeObj.transform.SetParent(parent, false);
            var bridge = bridgeObj.AddComponent<LegacyImguiBridge>();
            bridge.Bind(entry);
            return bridgeObj;
        }

        if (DataGridDrawer.CanDraw(entry))
        {
            return DataGridDrawer.Draw(parent, entry);
        }

        if (JsonEditorDrawer.CanDraw(entry))
        {
            return JsonEditorDrawer.Draw(parent, entry);
        }

        if (entry.SettingType == typeof(bool))
        {
            return BoolDrawer.Draw(parent, entry);
        }

        if (entry.SettingType.IsEnum)
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

    public static GameObject CreateRowContainer(Transform parent, SettingEntry entry, out Transform valueArea, Action? onReset = null)
    {
        var row = UiFactory.CreatePanel(parent, $"Row_{entry.Key}", CyberPalette.ColorBorderCard, CyberPalette.ColorCardSurface, 1f);
        var layout = row.AddComponent<LayoutElement>();
        layout.minHeight = 32f;
        layout.preferredHeight = 32f;
        layout.flexibleHeight = 0f;
        layout.flexibleWidth = 1f;

        var hover = row.AddComponent<HoverCardHandler>();
        hover.Bind(entry);

        var borderImg = row.GetComponent<Image>();
        var fill = row.transform.Find("Fill");
        var target = fill != null ? fill : row.transform;
        var fillImg = fill != null ? fill.GetComponent<Image>() : null;

        var cardHover = row.AddComponent<CyberHoverHandler>();
        cardHover.Init(borderImg, CyberPalette.ColorBorderCard, CyberPalette.ColorIceBlueBright, fillImg, CyberPalette.ColorCardSurface, CyberPalette.ColorVoidBlack);

        var leftArea = new GameObject("LeftArea", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        leftArea.transform.SetParent(target, false);
        var leftRT = leftArea.GetComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0f, 0f);
        leftRT.anchorMax = new Vector2(0.56f, 1f);
        leftRT.offsetMin = new Vector2(8f, 0f);
        leftRT.offsetMax = Vector2.zero;

        var leftHlg = leftArea.GetComponent<HorizontalLayoutGroup>();
        leftHlg.spacing = 4f;
        leftHlg.childAlignment = TextAnchor.MiddleLeft;
        leftHlg.childControlWidth = false;
        leftHlg.childControlHeight = false;

        if (entry.IsAdminOnly || !entry.IsUnlocked)
        {
            var lockLabel = UiFactory.CreateLabel(leftArea.transform, "LockBadge", "[🔒]", CyberPalette.ColorWarningAmber, 10f);
            var lockLayout = lockLabel.gameObject.AddComponent<LayoutElement>();
            lockLayout.minWidth = 18f;
            lockLayout.preferredWidth = 18f;
        }

        var label = UiFactory.CreateLabel(leftArea.transform, "Label", entry.DispName, entry.EntryColor, 11f);
        var labelLayout = label.gameObject.AddComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1f;

        var rightArea = new GameObject("RightArea", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rightArea.transform.SetParent(target, false);
        var rightRT = rightArea.GetComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(0.56f, 0f);
        rightRT.anchorMax = new Vector2(1f, 1f);
        rightRT.offsetMin = Vector2.zero;
        rightRT.offsetMax = new Vector2(-6f, 0f);

        var rightHlg = rightArea.GetComponent<HorizontalLayoutGroup>();
        rightHlg.spacing = 4f;
        rightHlg.childAlignment = TextAnchor.MiddleRight;
        rightHlg.childControlWidth = false;
        rightHlg.childControlHeight = false;

        valueArea = rightArea.transform;

        if (!entry.HideDefaultButton && entry.DefaultValue != null)
        {
            UiFactory.CreateCyberButton(rightArea.transform, "ResetBtn", "[ ↺ ]", () =>
            {
                entry.ResetToDefault();
                onReset?.Invoke();
            }, CyberPalette.ColorBorderSubtle, CyberPalette.ColorWarningAmber, 24f, 22f);
        }

        return row;
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
