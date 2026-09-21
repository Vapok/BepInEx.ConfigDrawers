using UnityEngine;
using UnityEngine.EventSystems;

namespace BepInEx.ConfigDrawers.Components;

public class ScrollbarDragTracker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool IsDragging { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsDragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsDragging = false;
    }
}
