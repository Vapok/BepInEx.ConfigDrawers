using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BepInEx.ConfigDrawers.UI;

public class ColorSpectrumPicker : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public Action<float, float>? OnPick;
    private RectTransform? _rt;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        HandlePointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        HandlePointer(eventData);
    }

    private void HandlePointer(PointerEventData eventData)
    {
        if (_rt == null || OnPick == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, eventData.position, eventData.pressEventCamera, out var localPoint))
        {
            var rect = _rt.rect;
            var u = Mathf.Clamp01((localPoint.x - rect.xMin) / rect.width);
            var v = Mathf.Clamp01((localPoint.y - rect.yMin) / rect.height);
            OnPick.Invoke(u, v);
        }
    }
}
