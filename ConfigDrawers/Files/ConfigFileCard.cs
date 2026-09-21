using System;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Files;

public static class ConfigFileCard
{
    public static GameObject Create(Transform parent, ConfigFileItem fileItem, Action<ConfigFileItem> onOpen)
    {
        if (parent == null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        if (fileItem == null)
        {
            throw new ArgumentNullException(nameof(fileItem));
        }

        GameObject card = UiFactory.CreatePanel(parent, $"File_{fileItem.FileName}", CyberPalette.ColorBorderCard, CyberPalette.ColorCardSurface, 1f);
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(0f, 42f);

        LayoutElement le = card.AddComponent<LayoutElement>();
        le.minHeight = 42f;
        le.preferredHeight = 42f;
        le.flexibleHeight = 0f;
        le.flexibleWidth = 1f;

        Transform? findFill = card.transform.Find("Fill");
        Transform fill = findFill != null ? findFill : card.transform;

        GameObject row = new GameObject("CardRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(fill, false);

        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.anchorMin = Vector2.zero;
        rowRT.anchorMax = Vector2.one;
        rowRT.offsetMin = new Vector2(8f, 3f);
        rowRT.offsetMax = new Vector2(-8f, -3f);

        HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        GameObject badgeObj = UiFactory.CreatePanel(row.transform, "Badge", fileItem.BadgeColor, CyberPalette.ColorVoidBlack, 1f);
        RectTransform badgeRT = badgeObj.GetComponent<RectTransform>();
        badgeRT.sizeDelta = new Vector2(46f, 22f);

        LayoutElement badgeLe = badgeObj.AddComponent<LayoutElement>();
        badgeLe.minWidth = 46f;
        badgeLe.preferredWidth = 46f;
        badgeLe.flexibleWidth = 0f;
        badgeLe.minHeight = 22f;
        badgeLe.preferredHeight = 22f;
        badgeLe.flexibleHeight = 0f;

        Transform? findBadgeFill = badgeObj.transform.Find("Fill");
        Transform badgeFill = findBadgeFill != null ? findBadgeFill : badgeObj.transform;
        TextMeshProUGUI badgeLabel = UiFactory.CreateLabel(badgeFill, "BadgeText", fileItem.BadgeText, fileItem.BadgeColor, 9f, TextAlignmentOptions.Center);
        RectTransform blRT = badgeLabel.GetComponent<RectTransform>();
        blRT.anchorMin = Vector2.zero;
        blRT.anchorMax = Vector2.one;
        blRT.offsetMin = Vector2.zero;
        blRT.offsetMax = Vector2.zero;
        badgeLabel.fontStyle = FontStyles.Bold;

        GameObject textGroup = new GameObject("TextGroup", typeof(RectTransform), typeof(VerticalLayoutGroup));
        textGroup.transform.SetParent(row.transform, false);

        LayoutElement tgLe = textGroup.AddComponent<LayoutElement>();
        tgLe.minWidth = 100f;
        tgLe.flexibleWidth = 1f;
        tgLe.flexibleHeight = 1f;

        VerticalLayoutGroup tgVlg = textGroup.GetComponent<VerticalLayoutGroup>();
        tgVlg.spacing = 1f;
        tgVlg.childControlWidth = true;
        tgVlg.childControlHeight = true;
        tgVlg.childForceExpandWidth = true;
        tgVlg.childForceExpandHeight = false;
        tgVlg.childAlignment = TextAnchor.MiddleLeft;

        TextMeshProUGUI nameLabel = UiFactory.CreateLabel(textGroup.transform, "FileName", fileItem.FileName, CyberPalette.ColorIceBlueBright, 10.5f, TextAlignmentOptions.MidlineLeft);
        nameLabel.fontStyle = FontStyles.Bold;

        UiFactory.CreateLabel(textGroup.transform, "FilePath", fileItem.RelativePath, CyberPalette.ColorTextMuted, 8.5f, TextAlignmentOptions.MidlineLeft);

        GameObject metaGroup = new GameObject("MetaGroup", typeof(RectTransform), typeof(VerticalLayoutGroup));
        metaGroup.transform.SetParent(row.transform, false);

        LayoutElement metaLe = metaGroup.AddComponent<LayoutElement>();
        metaLe.minWidth = 105f;
        metaLe.preferredWidth = 105f;
        metaLe.flexibleWidth = 0f;
        metaLe.flexibleHeight = 1f;

        VerticalLayoutGroup metaVlg = metaGroup.GetComponent<VerticalLayoutGroup>();
        metaVlg.spacing = 1f;
        metaVlg.childControlWidth = true;
        metaVlg.childControlHeight = true;
        metaVlg.childForceExpandWidth = true;
        metaVlg.childForceExpandHeight = false;
        metaVlg.childAlignment = TextAnchor.MiddleRight;

        TextMeshProUGUI sizeLabel = UiFactory.CreateLabel(metaGroup.transform, "FileSize", fileItem.FormattedSize, CyberPalette.ColorGlacialMint, 9.5f, TextAlignmentOptions.MidlineRight);
        sizeLabel.fontStyle = FontStyles.Bold;

        string dateStr = fileItem.LastModified.ToString("yyyy-MM-dd HH:mm");
        UiFactory.CreateLabel(metaGroup.transform, "FileDate", dateStr, CyberPalette.ColorTextMuted, 8.5f, TextAlignmentOptions.MidlineRight);

        Action openAction = () => onOpen(fileItem);

        UiFactory.CreateCyberButton(row.transform, "EditBtn", "Edit", openAction, CyberPalette.ColorIceBlueBright, CyberPalette.ColorIceBlueBright, 48f, 24f);

        ClickableBarHandler cardClick = card.AddComponent<ClickableBarHandler>();
        cardClick.OnClick = openAction;

        ClickableBarHandler fillClick = fill.gameObject.AddComponent<ClickableBarHandler>();
        fillClick.OnClick = openAction;

        Image borderImg = card.GetComponent<Image>();
        Image? fillImg = fill.GetComponent<Image>();
        CyberHoverHandler hover = card.AddComponent<CyberHoverHandler>();
        hover.Init(borderImg, CyberPalette.ColorBorderCard, CyberPalette.ColorIceBlueBright, fillImg, CyberPalette.ColorCardSurface, new Color(0.04f, 0.08f, 0.14f, 0.95f));

        return card;
    }
}
