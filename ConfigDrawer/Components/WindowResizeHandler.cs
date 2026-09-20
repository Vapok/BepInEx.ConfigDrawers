using UnityEngine;
using UnityEngine.EventSystems;

namespace BepInEx.ConfigDrawers.Components;

public class WindowResizeHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    private RectTransform? _targetWindow;

    public void SetTarget(RectTransform target)
    {
        _targetWindow = target;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Drag began
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_targetWindow == null)
        {
            return;
        }

        var currentSize = _targetWindow.sizeDelta;
        var newWidth = Mathf.Clamp(currentSize.x + eventData.delta.x, 380f, 960f);
        var newHeight = Mathf.Clamp(currentSize.y - eventData.delta.y, 320f, Screen.height - 40f);

        _targetWindow.sizeDelta = new Vector2(newWidth, newHeight);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Drag finished
    }
}
