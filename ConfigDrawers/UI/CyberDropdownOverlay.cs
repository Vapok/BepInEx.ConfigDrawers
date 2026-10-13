using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.UI;

public class CyberDropdownOverlay : MonoBehaviour
{
    private static GameObject? _currentOverlay;
    private static Action? _currentOnClose;

    public static void Show(RectTransform targetButtonRT, string[] options, int selectedIndex, Action<int> onSelect)
    {
        Close();

        if (targetButtonRT == null || options == null || options.Length == 0)
        {
            return;
        }

        Canvas canvas = targetButtonRT.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject overlayObj = CreateOverlayRoot(canvas);
        _currentOverlay = overlayObj;

        Vector3[] corners = new Vector3[4];
        targetButtonRT.GetWorldCorners(corners);
        float targetWidth = Mathf.Max(targetButtonRT.rect.width, 110f);

        int visibleRows = Mathf.Min(options.Length, 8);
        bool needsScroll = options.Length > visibleRows;
        float calculatedHeight = (visibleRows * 24f) + 8f;
        float extraWidth = needsScroll ? 14f : 8f;

        GameObject popupPanel = UiFactory.CreatePanel(overlayObj.transform, "PopupCard", CyberPalette.ColorIceBlue, CyberPalette.ColorVoidBlack, 1f);
        RectTransform popupRT = popupPanel.GetComponent<RectTransform>();
        if (corners[0].y < calculatedHeight + 10f)
        {
            popupRT.pivot = new Vector2(1f, 0f);
            popupRT.position = corners[2];
        }
        else
        {
            popupRT.pivot = new Vector2(1f, 1f);
            popupRT.position = corners[2];
        }
        popupRT.sizeDelta = new Vector2(targetWidth + extraWidth, calculatedHeight);

        Transform fill = popupPanel.transform.Find("Fill");
        Transform container = fill != null ? fill : popupPanel.transform;

        ScrollRect scrollRect = CreateScrollArea(container, needsScroll, out RectTransform contentRT, 2f);

        for (int i = 0; i < options.Length; i++)
        {
            int idx = i;
            bool isSelected = idx == selectedIndex;
            string optName = options[idx];
            Color textColor = isSelected ? CyberPalette.ColorGlacialMint : CyberPalette.ColorTextMain;
            Color borderColor = isSelected ? CyberPalette.ColorGlacialMint : CyberPalette.ColorBorderSubtle;

            GameObject itemBtn = UiFactory.CreateCyberButton(contentRT, $"Item_{idx}", optName, () =>
            {
                Close();
                onSelect?.Invoke(idx);
            }, borderColor, textColor, targetWidth - 6f, 22f);

            LayoutElement le = itemBtn.GetComponent<LayoutElement>();
            le.minHeight = 22f;
            le.preferredHeight = 22f;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;
        }

        if (needsScroll)
        {
            AttachScrollbar(container, scrollRect, 2f);
        }

        overlayObj.AddComponent<CyberDropdownOverlay>();
    }

    public static void ShowMulti(
        RectTransform targetButtonRT,
        string[] options,
        Func<int, bool> isIndexSelected,
        Action<int> onToggle,
        Action? onClose = null)
    {
        Close();

        if (targetButtonRT == null || options == null || options.Length == 0)
        {
            return;
        }

        Canvas canvas = targetButtonRT.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject overlayObj = CreateOverlayRoot(canvas);
        _currentOverlay = overlayObj;
        _currentOnClose = onClose;

        Vector3[] corners = new Vector3[4];
        targetButtonRT.GetWorldCorners(corners);

        int maxLen = 0;
        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] != null && options[i].Length > maxLen)
            {
                maxLen = options[i].Length;
            }
        }
        float minNeededWidth = ((maxLen + 4) * 7.2f) + 32f;
        float targetWidth = Mathf.Max(targetButtonRT.rect.width, minNeededWidth, 120f);

        int visibleRows = Mathf.Min(options.Length, 8);
        bool needsScroll = options.Length > visibleRows;
        float calculatedHeight = (visibleRows * 24f) + 8f + 26f;
        float extraWidth = needsScroll ? 14f : 8f;

        GameObject popupPanel = UiFactory.CreatePanel(overlayObj.transform, "PopupCard", CyberPalette.ColorIceBlue, CyberPalette.ColorVoidBlack, 1f);
        RectTransform popupRT = popupPanel.GetComponent<RectTransform>();
        if (corners[0].y < calculatedHeight + 10f)
        {
            popupRT.pivot = new Vector2(1f, 0f);
            popupRT.position = corners[2];
        }
        else
        {
            popupRT.pivot = new Vector2(1f, 1f);
            popupRT.position = corners[2];
        }
        popupRT.sizeDelta = new Vector2(targetWidth + extraWidth, calculatedHeight);

        Transform fill = popupPanel.transform.Find("Fill");
        Transform container = fill != null ? fill : popupPanel.transform;

        ScrollRect scrollRect = CreateScrollArea(container, needsScroll, out RectTransform contentRT, 26f);

        List<TextMeshProUGUI> textList = new List<TextMeshProUGUI>(options.Length);
        List<Image> borderImgList = new List<Image>(options.Length);
        List<CyberHoverHandler?> hoverList = new List<CyberHoverHandler?>(options.Length);

        Action<int> updateItemVisual = (int itemIndex) =>
        {
            if (itemIndex < 0 || itemIndex >= options.Length)
            {
                return;
            }

            bool isSelected = isIndexSelected(itemIndex);
            string optName = options[itemIndex];
            Color textColor = isSelected ? CyberPalette.ColorGlacialMint : CyberPalette.ColorTextMain;
            Color borderColor = isSelected ? CyberPalette.ColorGlacialMint : CyberPalette.ColorBorderSubtle;
            Color hoverBorder = isSelected ? Color.Lerp(CyberPalette.ColorGlacialMint, Color.white, 0.35f) : CyberPalette.ColorIceBlueBright;
            Color hoverText = isSelected ? Color.white : CyberPalette.ColorIceBlueBright;

            TextMeshProUGUI tmp = textList[itemIndex];
            Image borderImg = borderImgList[itemIndex];
            CyberHoverHandler? hover = hoverList[itemIndex];

            if (tmp != null)
            {
                tmp.text = isSelected ? $"[X]  {optName}" : $"[ ]  {optName}";
                tmp.color = textColor;
            }

            if (borderImg != null)
            {
                borderImg.color = borderColor;
            }

            if (hover != null)
            {
                hover.SetNormalColors(borderColor, textColor, hoverBorder: hoverBorder, hoverText: hoverText);
            }
        };

        Action updateAllItems = () =>
        {
            for (int k = 0; k < options.Length; k++)
            {
                updateItemVisual(k);
            }
        };

        for (int i = 0; i < options.Length; i++)
        {
            int idx = i;
            string optName = options[idx];
            bool isSelected = isIndexSelected(idx);
            string initialLabel = isSelected ? $"[X]  {optName}" : $"[ ]  {optName}";
            Color textColor = isSelected ? CyberPalette.ColorGlacialMint : CyberPalette.ColorTextMain;
            Color borderColor = isSelected ? CyberPalette.ColorGlacialMint : CyberPalette.ColorBorderSubtle;

            GameObject itemBtn = UiFactory.CreateCyberButton(contentRT, $"Item_{idx}", initialLabel, () =>
            {
                onToggle?.Invoke(idx);
                updateAllItems();
            }, borderColor, textColor, targetWidth - 6f, 22f);

            LayoutElement le = itemBtn.GetComponent<LayoutElement>();
            le.minHeight = 22f;
            le.preferredHeight = 22f;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;

            TextMeshProUGUI? tmp = itemBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                RectTransform textRT = tmp.GetComponent<RectTransform>();
                if (textRT != null)
                {
                    textRT.offsetMin = new Vector2(6f, 0f);
                    textRT.offsetMax = new Vector2(-6f, 0f);
                }
            }

            Image borderImg = itemBtn.GetComponent<Image>();
            CyberHoverHandler? hover = itemBtn.GetComponent<CyberHoverHandler>();

            textList.Add(tmp!);
            borderImgList.Add(borderImg!);
            hoverList.Add(hover);
        }

        if (needsScroll)
        {
            AttachScrollbar(container, scrollRect, 26f);
        }

        GameObject footerObj = new GameObject("Footer", typeof(RectTransform));
        footerObj.transform.SetParent(container, false);
        RectTransform footerRT = footerObj.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0f, 0f);
        footerRT.anchorMax = new Vector2(1f, 0f);
        footerRT.pivot = new Vector2(0.5f, 0f);
        footerRT.sizeDelta = new Vector2(0f, 24f);
        footerRT.anchoredPosition = new Vector2(0f, 2f);

        UiFactory.CreateCyberButton(footerObj.transform, "DoneBtn", "Done", Close, CyberPalette.ColorBorderSubtle, CyberPalette.ColorGlacialMint, targetWidth - 6f, 20f);

        overlayObj.AddComponent<CyberDropdownOverlay>();
    }

    private static GameObject CreateOverlayRoot(Canvas canvas)
    {
        GameObject overlayObj = new GameObject("DropdownOverlay", typeof(RectTransform));
        overlayObj.transform.SetParent(canvas.transform, false);

        RectTransform overlayRT = overlayObj.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;

        GameObject blocker = new GameObject("Blocker", typeof(RectTransform), typeof(Image), typeof(Button));
        blocker.transform.SetParent(overlayObj.transform, false);
        RectTransform blockerRT = blocker.GetComponent<RectTransform>();
        blockerRT.anchorMin = Vector2.zero;
        blockerRT.anchorMax = Vector2.one;
        blockerRT.offsetMin = Vector2.zero;
        blockerRT.offsetMax = Vector2.zero;

        Image blockerImg = blocker.GetComponent<Image>();
        blockerImg.color = new Color(0f, 0f, 0f, 0.01f);

        Button blockerBtn = blocker.GetComponent<Button>();
        blockerBtn.onClick.AddListener(Close);

        return overlayObj;
    }

    private static ScrollRect CreateScrollArea(Transform container, bool needsScroll, out RectTransform contentRT, float bottomOffset)
    {
        GameObject scrollObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
        scrollObj.transform.SetParent(container, false);
        RectTransform scrollRT = scrollObj.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(2f, bottomOffset);
        scrollRT.offsetMax = needsScroll ? new Vector2(-10f, -2f) : new Vector2(-2f, -2f);

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = needsScroll;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 35f;

        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(scrollObj.transform, false);
        contentRT = contentObj.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(2, 2, 2, 2);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;
        return scrollRect;
    }

    private static void AttachScrollbar(Transform container, ScrollRect scrollRect, float bottomOffset)
    {
        GameObject scrollbarObj = new GameObject("Scrollbar", typeof(RectTransform), typeof(Scrollbar), typeof(Image));
        scrollbarObj.transform.SetParent(container, false);
        RectTransform sbRT = scrollbarObj.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1f, 0f);
        sbRT.anchorMax = new Vector2(1f, 1f);
        sbRT.pivot = new Vector2(1f, 0.5f);
        sbRT.sizeDelta = new Vector2(5f, -(bottomOffset + 2f));
        sbRT.anchoredPosition = new Vector2(-2f, bottomOffset / 2f);

        Image sbImg = scrollbarObj.GetComponent<Image>();
        sbImg.color = new Color(0.02f, 0.05f, 0.08f, 0.8f);

        GameObject slidingArea = new GameObject("SlidingArea", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbarObj.transform, false);
        RectTransform saRT = slidingArea.GetComponent<RectTransform>();
        saRT.anchorMin = Vector2.zero;
        saRT.anchorMax = Vector2.one;
        saRT.sizeDelta = Vector2.zero;

        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObj.transform.SetParent(slidingArea.transform, false);
        RectTransform handleRT = handleObj.GetComponent<RectTransform>();
        handleRT.sizeDelta = Vector2.zero;

        Image handleImg = handleObj.GetComponent<Image>();
        handleImg.color = CyberPalette.ColorIceBlue;

        Scrollbar scrollbar = scrollbarObj.GetComponent<Scrollbar>();
        scrollbar.handleRect = handleRT;
        scrollbar.targetGraphic = handleImg;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
    }

    public static void Close()
    {
        if (_currentOverlay != null)
        {
            Action? onClose = _currentOnClose;
            _currentOnClose = null;

            Destroy(_currentOverlay);
            _currentOverlay = null;

            onClose?.Invoke();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }
}
