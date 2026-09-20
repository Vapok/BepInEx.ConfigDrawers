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

        var canvas = targetButtonRT.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        var overlayObj = new GameObject("DropdownOverlay", typeof(RectTransform));
        overlayObj.transform.SetParent(canvas.transform, false);
        _currentOverlay = overlayObj;

        var overlayRT = overlayObj.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;

        var blocker = new GameObject("Blocker", typeof(RectTransform), typeof(Image), typeof(Button));
        blocker.transform.SetParent(overlayObj.transform, false);
        var blockerRT = blocker.GetComponent<RectTransform>();
        blockerRT.anchorMin = Vector2.zero;
        blockerRT.anchorMax = Vector2.one;
        blockerRT.offsetMin = Vector2.zero;
        blockerRT.offsetMax = Vector2.zero;

        var blockerImg = blocker.GetComponent<Image>();
        blockerImg.color = new Color(0f, 0f, 0f, 0.01f);

        var blockerBtn = blocker.GetComponent<Button>();
        blockerBtn.onClick.AddListener(Close);

        var corners = new Vector3[4];
        targetButtonRT.GetWorldCorners(corners);
        var targetBottomLeft = corners[0];
        var targetWidth = Mathf.Max(targetButtonRT.rect.width, 110f);

        var popupPanel = UiFactory.CreatePanel(overlayObj.transform, "PopupCard", CyberPalette.ColorIceBlue, CyberPalette.ColorVoidBlack, 1f);
        var popupRT = popupPanel.GetComponent<RectTransform>();
        popupRT.pivot = new Vector2(1f, 1f);
        popupRT.position = corners[2];

        var fill = popupPanel.transform.Find("Fill");
        var container = fill != null ? fill : popupPanel.transform;

        var scrollObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
        scrollObj.transform.SetParent(container, false);
        var scrollRT = scrollObj.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(2f, 2f);
        scrollRT.offsetMax = new Vector2(-2f, -2f);

        var scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 25f;

        var contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(scrollObj.transform, false);
        var contentRT = contentObj.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0f, 0f);

        var vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(2, 2, 2, 2);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;

        for (int i = 0; i < options.Length; i++)
        {
            var idx = i;
            var isSelected = idx == selectedIndex;
            var optName = options[idx];
            var textColor = isSelected ? CyberPalette.ColorGlacialMint : CyberPalette.ColorTextMain;
            var borderColor = isSelected ? CyberPalette.ColorGlacialMint : CyberPalette.ColorBorderSubtle;

            var itemBtn = UiFactory.CreateCyberButton(contentObj.transform, $"Item_{idx}", optName, () =>
            {
                Close();
                onSelect?.Invoke(idx);
            }, borderColor, textColor, targetWidth - 6f, 22f);

            var le = itemBtn.GetComponent<LayoutElement>();
            le.minHeight = 22f;
            le.preferredHeight = 22f;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;
        }

        var visibleRows = Mathf.Min(options.Length, 7);
        var calculatedHeight = (visibleRows * 24f) + 8f;
        popupRT.sizeDelta = new Vector2(targetWidth + 8f, calculatedHeight);

        var overlayComp = overlayObj.AddComponent<CyberDropdownOverlay>();
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
