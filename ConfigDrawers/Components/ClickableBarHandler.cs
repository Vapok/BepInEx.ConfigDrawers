using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BepInEx.ConfigDrawers.Components;

public class ClickableBarHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Action? OnClick { get; set; }
    private Vector2 _downPos;
    private float _downTime;
    private bool _isPressed;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _downPos = eventData.position;
            _downTime = Time.unscaledTime;
            _isPressed = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isPressed || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        _isPressed = false;
        var elapsed = Time.unscaledTime - _downTime;
        var distance = Vector2.Distance(_downPos, eventData.position);

        if (elapsed < 0.45f && distance < 25f)
        {
            OnClick?.Invoke();
        }
    }
}
