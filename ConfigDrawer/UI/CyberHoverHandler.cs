using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.UI;

public class CyberHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image? _borderImg;
    private Image? _fillImg;
    private Color _normalBorder;
    private Color _hoverBorder;
    private Color _normalFill;
    private Color _hoverFill;

    public void Init(Image borderImg, Color normalBorder, Color hoverBorder, Image? fillImg = null, Color? normalFill = null, Color? hoverFill = null)
    {
        _borderImg = borderImg;
        _normalBorder = normalBorder;
        _hoverBorder = hoverBorder;
        _fillImg = fillImg;
        _normalFill = normalFill ?? (fillImg != null ? fillImg.color : Color.clear);
        _hoverFill = hoverFill ?? _normalFill;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_borderImg != null)
        {
            _borderImg.color = _hoverBorder;
        }

        if (_fillImg != null)
        {
            _fillImg.color = _hoverFill;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_borderImg != null)
        {
            _borderImg.color = _normalBorder;
        }

        if (_fillImg != null)
        {
            _fillImg.color = _normalFill;
        }
    }
}
