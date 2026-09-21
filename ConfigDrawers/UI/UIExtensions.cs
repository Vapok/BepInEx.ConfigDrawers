using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.UI;

public static class UIExtensions
{
    public static RectTransform SetAnchor(this RectTransform rectTransform, Vector2 min, Vector2 max)
    {
        rectTransform.anchorMin = min;
        rectTransform.anchorMax = max;
        return rectTransform;
    }

    public static RectTransform SetAnchorMin(this RectTransform rectTransform, Vector2 min)
    {
        rectTransform.anchorMin = min;
        return rectTransform;
    }

    public static RectTransform SetAnchorMax(this RectTransform rectTransform, Vector2 max)
    {
        rectTransform.anchorMax = max;
        return rectTransform;
    }

    public static RectTransform SetPivot(this RectTransform rectTransform, Vector2 pivot)
    {
        rectTransform.pivot = pivot;
        return rectTransform;
    }

    public static RectTransform SetSizeDelta(this RectTransform rectTransform, Vector2 sizeDelta)
    {
        rectTransform.sizeDelta = sizeDelta;
        return rectTransform;
    }

    public static RectTransform SetAnchoredPosition(this RectTransform rectTransform, Vector2 position)
    {
        rectTransform.anchoredPosition = position;
        return rectTransform;
    }

    public static RectTransform SetOffsets(this RectTransform rectTransform, Vector2 minOffset, Vector2 maxOffset)
    {
        rectTransform.offsetMin = minOffset;
        rectTransform.offsetMax = maxOffset;
        return rectTransform;
    }

    public static CanvasGroup SetAlpha(this CanvasGroup canvasGroup, float alpha)
    {
        canvasGroup.alpha = alpha;
        return canvasGroup;
    }

    public static CanvasGroup SetInteractable(this CanvasGroup canvasGroup, bool interactable)
    {
        canvasGroup.interactable = interactable;
        return canvasGroup;
    }

    public static CanvasGroup SetBlocksRaycasts(this CanvasGroup canvasGroup, bool blocksRaycasts)
    {
        canvasGroup.blocksRaycasts = blocksRaycasts;
        return canvasGroup;
    }

    public static Image SetColor(this Image image, Color color)
    {
        image.color = color;
        return image;
    }

    public static Image SetRaycastTarget(this Image image, bool raycastTarget)
    {
        image.raycastTarget = raycastTarget;
        return image;
    }

    public static LayoutElement SetDimensions(
        this LayoutElement element,
        float minWidth,
        float minHeight,
        float preferredWidth,
        float preferredHeight,
        float flexibleWidth = 0f,
        float flexibleHeight = 0f)
    {
        element.minWidth = minWidth;
        element.minHeight = minHeight;
        element.preferredWidth = preferredWidth;
        element.preferredHeight = preferredHeight;
        element.flexibleWidth = flexibleWidth;
        element.flexibleHeight = flexibleHeight;
        return element;
    }

    public static LayoutElement SetFixedWidth(this LayoutElement element, float width)
    {
        element.minWidth = width;
        element.preferredWidth = width;
        element.flexibleWidth = 0f;
        return element;
    }

    public static LayoutElement SetFixedHeight(this LayoutElement element, float height)
    {
        element.minHeight = height;
        element.preferredHeight = height;
        element.flexibleHeight = 0f;
        return element;
    }

    public static T SetChildControl<T>(this T layoutGroup, bool width, bool height) where T : HorizontalOrVerticalLayoutGroup
    {
        layoutGroup.childControlWidth = width;
        layoutGroup.childControlHeight = height;
        return layoutGroup;
    }

    public static T SetChildForceExpand<T>(this T layoutGroup, bool width, bool height) where T : HorizontalOrVerticalLayoutGroup
    {
        layoutGroup.childForceExpandWidth = width;
        layoutGroup.childForceExpandHeight = height;
        return layoutGroup;
    }

    public static T SetSpacing<T>(this T layoutGroup, float spacing) where T : HorizontalOrVerticalLayoutGroup
    {
        layoutGroup.spacing = spacing;
        return layoutGroup;
    }

    public static T SetPadding<T>(this T layoutGroup, int left, int right, int top, int bottom) where T : LayoutGroup
    {
        layoutGroup.padding = new RectOffset(left, right, top, bottom);
        return layoutGroup;
    }

    public static T SetChildAlignment<T>(this T layoutGroup, TextAnchor alignment) where T : LayoutGroup
    {
        layoutGroup.childAlignment = alignment;
        return layoutGroup;
    }

    public static ContentSizeFitter SetHorizontalFit(this ContentSizeFitter fitter, ContentSizeFitter.FitMode fitMode)
    {
        fitter.horizontalFit = fitMode;
        return fitter;
    }

    public static ContentSizeFitter SetVerticalFit(this ContentSizeFitter fitter, ContentSizeFitter.FitMode fitMode)
    {
        fitter.verticalFit = fitMode;
        return fitter;
    }
}
