using System;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Drawers.UguiScope;

public static class UguiScopeDrawer
{
    public static GameObject Draw(Transform parent, SettingEntry entry)
    {
        if (parent == null || entry == null)
        {
            throw new ArgumentNullException(entry == null ? nameof(entry) : nameof(parent));
        }

        GameObject? subpanelObj = null;
        TextMeshProUGUI? labelTmp = null;
        bool isExpanded = false;

        string arrowColorHex = ColorUtility.ToHtmlStringRGB(CyberPalette.ColorGlacialMint);

        void UpdateLeftLabel()
        {
            if (labelTmp != null)
            {
                string arrow = isExpanded ? "▼" : "▶";
                labelTmp.richText = true;
                labelTmp.text = $"<color=#{arrowColorHex}><b>{arrow}</b></color>  {entry.DispName}";
            }
        }

        GameObject rowObj = DrawerDispatcher.CreateRowContainer(parent, entry, out Transform valueArea, () =>
        {
            if (subpanelObj != null)
            {
                RebuildContent(subpanelObj.transform, entry, parent as RectTransform);
            }
        });

        Transform? leftAreaTransform = rowObj.transform.Find("Fill/LeftArea");
        if (leftAreaTransform != null)
        {
            RectTransform leftRT = leftAreaTransform.GetComponent<RectTransform>();
            if (leftRT != null)
            {
                leftRT.offsetMax = new Vector2(-80f, 0f);
            }
        }

        Transform? labelTransform = rowObj.transform.Find("Fill/LeftArea/Label");
        if (labelTransform != null)
        {
            labelTmp = labelTransform.GetComponent<TextMeshProUGUI>();
            if (labelTmp != null)
            {
                labelTmp.raycastTarget = false;
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
            if (parent is RectTransform pRT)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(pRT);
            }
        };

        DrawerDispatcher.AttachBarToggle(rowObj, toggleAction);

        subpanelObj = new GameObject($"SubScope_{entry.Key}", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        subpanelObj.transform.SetParent(parent, false);
        subpanelObj.transform.SetSiblingIndex(rowObj.transform.GetSiblingIndex() + 1);

        Image bgImg = subpanelObj.GetComponent<Image>();
        bgImg.color = CyberPalette.ColorVoidBlack;

        VerticalLayoutGroup vlg = subpanelObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(16, 12, 6, 6);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = subpanelObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement le = subpanelObj.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        RebuildContent(subpanelObj.transform, entry, parent as RectTransform);
        subpanelObj.SetActive(false);

        return rowObj;
    }

    private static void RebuildContent(Transform subpanel, SettingEntry entry, RectTransform? parentListRT)
    {
        for (int i = subpanel.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(subpanel.GetChild(i).gameObject);
        }

        try
        {
            UguiDrawerScope scope = new UguiDrawerScope(subpanel, !entry.CanEdit);
            entry.InvokeCustomUguiDrawer(scope);
        }
        catch (Exception ex)
        {
            TextMeshProUGUI err = UiFactory.CreateLabel(subpanel, "ErrorLabel", $"[CustomUguiDrawer Error]: {ex.Message}", CyberPalette.ColorErrorRed, 10f);
            Debug.LogError($"[ConfigDrawers] CustomUguiDrawer error on {entry.Key}: {ex}");
        }

        if (parentListRT != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentListRT);
        }
    }
}
