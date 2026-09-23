using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.UI;

public class CyberDropdownOverlay : MonoBehaviour
{
    private static GameObject? _currentOverlay;

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

        GameObject overlayObj = new GameObject("DropdownOverlay", typeof(RectTransform));
        overlayObj.transform.SetParent(canvas.transform, false);
        _currentOverlay = overlayObj;

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

        Vector3[] corners = new Vector3[4];
        targetButtonRT.GetWorldCorners(corners);
        float targetWidth = Mathf.Max(targetButtonRT.rect.width, 110f);

        GameObject popupPanel = UiFactory.CreatePanel(overlayObj.transform, "PopupCard", CyberPalette.ColorIceBlue, CyberPalette.ColorVoidBlack, 1f);
        RectTransform popupRT = popupPanel.GetComponent<RectTransform>();
        popupRT.pivot = new Vector2(1f, 1f);
        popupRT.position = corners[2];

        Transform fill = popupPanel.transform.Find("Fill");
        Transform container = fill != null ? fill : popupPanel.transform;

        int visibleRows = Mathf.Min(options.Length, 8);
        bool needsScroll = options.Length > visibleRows;

        GameObject scrollObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
        scrollObj.transform.SetParent(container, false);
        RectTransform scrollRT = scrollObj.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(2f, 2f);
        scrollRT.offsetMax = needsScroll ? new Vector2(-10f, -2f) : new Vector2(-2f, -2f);

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = needsScroll;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 35f;

        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(scrollObj.transform, false);
        RectTransform contentRT = contentObj.GetComponent<RectTransform>();
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

        for (int i = 0; i < options.Length; i++)
        {
            int idx = i;
            bool isSelected = idx == selectedIndex;
            string optName = options[idx];
            Color textColor = isSelected ? CyberPalette.ColorGlacialMint : CyberPalette.ColorTextMain;
            Color borderColor = isSelected ? CyberPalette.ColorGlacialMint : CyberPalette.ColorBorderSubtle;

            GameObject itemBtn = UiFactory.CreateCyberButton(contentObj.transform, $"Item_{idx}", optName, () =>
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
            GameObject scrollbarObj = new GameObject("Scrollbar", typeof(RectTransform), typeof(Scrollbar), typeof(Image));
            scrollbarObj.transform.SetParent(container, false);
            RectTransform sbRT = scrollbarObj.GetComponent<RectTransform>();
            sbRT.anchorMin = new Vector2(1f, 0f);
            sbRT.anchorMax = new Vector2(1f, 1f);
            sbRT.pivot = new Vector2(1f, 0.5f);
            sbRT.sizeDelta = new Vector2(5f, -4f);
            sbRT.anchoredPosition = new Vector2(-2f, 0f);

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

        float calculatedHeight = (visibleRows * 24f) + 8f;
        float extraWidth = needsScroll ? 14f : 8f;
        popupRT.sizeDelta = new Vector2(targetWidth + extraWidth, calculatedHeight);

        CyberDropdownOverlay overlayComp = overlayObj.AddComponent<CyberDropdownOverlay>();
    }

    public static void Close()
    {
        if (_currentOverlay != null)
        {
            Destroy(_currentOverlay);
            _currentOverlay = null;
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
