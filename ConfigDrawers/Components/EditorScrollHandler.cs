using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Components;

public class EditorScrollHandler : MonoBehaviour, IScrollHandler
{
    private const float PixelsPerScrollNotch = 45f;

    public Scrollbar? Scrollbar { get; set; }
    public RectTransform? Viewport { get; set; }
    public TMP_Text? TextComponent { get; set; }

    public void OnScroll(PointerEventData eventData)
    {
        if (Scrollbar == null || Scrollbar.size >= 1f)
        {
            return;
        }

        float textHeight = TextComponent != null ? TextComponent.preferredHeight : 0f;
        float viewHeight = Viewport != null ? Viewport.rect.height : 100f;
        float scrollableHeight = textHeight - viewHeight;

        float step = scrollableHeight > 0f ? (PixelsPerScrollNotch / scrollableHeight) : 0.05f;
        step = Mathf.Clamp(step, 0.005f, 0.2f);

        Scrollbar.value = Mathf.Clamp01(Scrollbar.value - eventData.scrollDelta.y * step);
    }
}
