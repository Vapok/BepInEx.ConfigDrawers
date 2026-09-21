using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Components;

public class EditorScrollHandler : MonoBehaviour, IScrollHandler
{
    private const float PixelsPerScrollNotch = 45f;

    public Scrollbar? VerticalScrollbar { get; set; }
    public Scrollbar? HorizontalScrollbar { get; set; }
    public Scrollbar? Scrollbar
    {
        get => VerticalScrollbar;
        set => VerticalScrollbar = value;
    }

    public RectTransform? Viewport { get; set; }
    public TMP_Text? TextComponent { get; set; }

    public void OnScroll(PointerEventData eventData)
    {
        bool isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (isShift || Mathf.Abs(eventData.scrollDelta.x) > Mathf.Abs(eventData.scrollDelta.y))
        {
            if (HorizontalScrollbar != null && HorizontalScrollbar.size < 1f)
            {
                float delta = isShift ? eventData.scrollDelta.y : -eventData.scrollDelta.x;
                float textWidth = TextComponent != null ? TextComponent.preferredWidth : 0f;
                float viewWidth = Viewport != null ? Viewport.rect.width : 100f;
                float scrollableWidth = textWidth - viewWidth + 40f;

                float step = scrollableWidth > 0f ? (PixelsPerScrollNotch / scrollableWidth) : 0.05f;
                step = Mathf.Clamp(step, 0.005f, 0.2f);

                HorizontalScrollbar.value = Mathf.Clamp01(HorizontalScrollbar.value - delta * step);
            }
            return;
        }

        if (VerticalScrollbar != null && VerticalScrollbar.size < 1f)
        {
            float textHeight = TextComponent != null ? TextComponent.preferredHeight : 0f;
            float viewHeight = Viewport != null ? Viewport.rect.height : 100f;
            float scrollableHeight = textHeight - viewHeight;

            float step = scrollableHeight > 0f ? (PixelsPerScrollNotch / scrollableHeight) : 0.05f;
            step = Mathf.Clamp(step, 0.005f, 0.2f);

            VerticalScrollbar.value = Mathf.Clamp01(VerticalScrollbar.value - eventData.scrollDelta.y * step);
        }
    }
}
