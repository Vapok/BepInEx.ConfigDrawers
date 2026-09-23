using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.UI;

public class CyberHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Image? _borderImg;
    private Image? _fillImg;
    private TextMeshProUGUI? _text;
    private Color _normalBorder;
    private Color _hoverBorder;
    private Color _pressedBorder;
    private Color _normalFill;
    private Color _hoverFill;
    private Color _pressedFill;
    private Color _normalText;
    private Color _hoverText;
    private Color _pressedText;
    private bool _isHovered;
    private bool _isPressed;

    public void Init(
        Image borderImg,
        Color normalBorder,
        Color hoverBorder,
        Image? fillImg = null,
        Color? normalFill = null,
        Color? hoverFill = null,
        Color? pressedBorder = null,
        Color? pressedFill = null,
        TextMeshProUGUI? text = null,
        Color? normalText = null,
        Color? hoverText = null,
        Color? pressedText = null)
    {
        _borderImg = borderImg;
        _normalBorder = normalBorder;
        _hoverBorder = hoverBorder;
        _pressedBorder = pressedBorder ?? Color.Lerp(hoverBorder, Color.white, 0.45f);

        _fillImg = fillImg;
        _normalFill = normalFill ?? (fillImg != null ? fillImg.color : Color.clear);
        _hoverFill = hoverFill ?? _normalFill;
        _pressedFill = pressedFill ?? (fillImg != null ? new Color(0.12f, 0.22f, 0.32f, 1f) : Color.clear);

        _text = text;
        if (text != null)
        {
            _normalText = normalText ?? text.color;
            _hoverText = hoverText ?? _normalText;
            _pressedText = pressedText ?? Color.white;
        }

        UpdateVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        UpdateVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        UpdateVisuals();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPressed = true;
        UpdateVisuals();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressed = false;
        UpdateVisuals();
    }

    public void SetNormalColors(
        Color normalBorder,
        Color normalText,
        Color? normalFill = null,
        Color? hoverBorder = null,
        Color? hoverText = null,
        Color? hoverFill = null,
        Color? pressedBorder = null,
        Color? pressedText = null,
        Color? pressedFill = null)
    {
        _normalBorder = normalBorder;
        _normalText = normalText;
        if (normalFill.HasValue) _normalFill = normalFill.Value;
        if (hoverBorder.HasValue) _hoverBorder = hoverBorder.Value;
        if (hoverText.HasValue) _hoverText = hoverText.Value;
        if (hoverFill.HasValue) _hoverFill = hoverFill.Value;
        if (pressedBorder.HasValue) _pressedBorder = pressedBorder.Value;
        if (pressedText.HasValue) _pressedText = pressedText.Value;
        if (pressedFill.HasValue) _pressedFill = pressedFill.Value;

        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (_isPressed)
        {
            if (_borderImg != null) _borderImg.color = _pressedBorder;
            if (_fillImg != null) _fillImg.color = _pressedFill;
            if (_text != null) _text.color = _pressedText;
        }
        else if (_isHovered)
        {
            if (_borderImg != null) _borderImg.color = _hoverBorder;
            if (_fillImg != null) _fillImg.color = _hoverFill;
            if (_text != null) _text.color = _hoverText;
        }
        else
        {
            if (_borderImg != null) _borderImg.color = _normalBorder;
            if (_fillImg != null) _fillImg.color = _normalFill;
            if (_text != null) _text.color = _normalText;
        }
    }
}
