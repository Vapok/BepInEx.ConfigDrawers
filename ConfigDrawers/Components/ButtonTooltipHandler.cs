using System;
using System.Collections;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Components;

public class ButtonTooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private string _headerText = string.Empty;
    private string _bodyText = string.Empty;
    private Coroutine? _hoverRoutine;
    private static ButtonTooltipHandler? _activeHandler;
    private static GameObject? _tooltipRoot;
    private static TextMeshProUGUI? _text;
    private const float HoverDelaySeconds = 0.2f;

    public void Bind(string headerText, string bodyText)
    {
        _headerText = headerText ?? string.Empty;
        _bodyText = bodyText ?? string.Empty;
    }

    public static ButtonTooltipHandler Attach(GameObject target, string headerText, string bodyText)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        var handler = target.AddComponent<ButtonTooltipHandler>();
        handler.Bind(headerText, bodyText);
        return handler;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(_headerText) && string.IsNullOrEmpty(_bodyText))
        {
            return;
        }

        if (_hoverRoutine != null)
        {
            StopCoroutine(_hoverRoutine);
        }

        _hoverRoutine = StartCoroutine(ShowAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ClearHover();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ClearHover();
    }

    private void ClearHover()
    {
        if (_hoverRoutine != null)
        {
            StopCoroutine(_hoverRoutine);
            _hoverRoutine = null;
        }

        if (_activeHandler == this)
        {
            HideTooltip();
        }
    }

    private void OnDisable()
    {
        ClearHover();
    }

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSecondsRealtime(HoverDelaySeconds);
        ShowTooltip();
        _hoverRoutine = null;
    }

    private void ShowTooltip()
    {
        _activeHandler = this;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        EnsureTooltipCreated(canvas);
        if (_tooltipRoot == null || _text == null)
        {
            return;
        }

        var headerFormatted = $"<color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorIceBlueBright)}><b>{_headerText}</b></color>";
        _text.text = string.IsNullOrEmpty(_headerText)
            ? $"<color=#c8dbee>{_bodyText}</color>"
            : $"{headerFormatted}\n<color=#c8dbee>{_bodyText}</color>";

        _text.ForceMeshUpdate();

        const float targetWidth = 200f;
        var preferred = _text.GetPreferredValues(targetWidth - 16f, 1000f);
        var width = Mathf.Min(targetWidth, preferred.x + 20f);
        var height = preferred.y + 14f;

        var rt = _tooltipRoot.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(Mathf.Max(width, 140f), Mathf.Max(height, 28f));

        var buttonRT = (RectTransform)transform;
        var corners = new Vector3[4];
        buttonRT.GetWorldCorners(corners);
        var buttonBottomCenter = new Vector3((corners[0].x + corners[3].x) * 0.5f, corners[0].y, 0f);

        var scale = canvas.scaleFactor > 0.01f ? canvas.scaleFactor : 1f;
        var pixelW = rt.sizeDelta.x * scale;
        var pixelH = rt.sizeDelta.y * scale;

        rt.pivot = new Vector2(0.5f, 1f);
        var posX = buttonBottomCenter.x;
        var posY = buttonBottomCenter.y - 6f * scale;

        if (posY - pixelH < 10f)
        {
            rt.pivot = new Vector2(0.5f, 0f);
            posY = corners[1].y + 6f * scale;
        }

        var minX = 10f + pixelW * 0.5f;
        var maxX = Screen.width - 10f - pixelW * 0.5f;
        posX = Mathf.Clamp(posX, minX, maxX);

        rt.position = new Vector3(posX, posY, 0f);
        _tooltipRoot.SetActive(true);
    }

    public static void HideTooltip()
    {
        _activeHandler = null;
        if (_tooltipRoot != null && _tooltipRoot.activeSelf)
        {
            _tooltipRoot.SetActive(false);
        }
    }

    private static void EnsureTooltipCreated(Canvas canvas)
    {
        if (_tooltipRoot != null)
        {
            return;
        }

        _tooltipRoot = new GameObject("ButtonCyberTooltip", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        _tooltipRoot.transform.SetParent(canvas.transform, false);

        var cg = _tooltipRoot.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        var borderImg = _tooltipRoot.GetComponent<Image>();
        borderImg.color = CyberPalette.ColorIceBlueBright;
        borderImg.raycastTarget = false;

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(_tooltipRoot.transform, false);
        var fillRT = fillGo.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(1f, 1f);
        fillRT.offsetMax = new Vector2(-1f, -1f);

        var fillImg = fillGo.GetComponent<Image>();
        fillImg.color = CyberPalette.ColorVoidBlack;
        fillImg.raycastTarget = false;

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(fillGo.transform, false);
        var tRT = textGo.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(8f, 5f);
        tRT.offsetMax = new Vector2(-8f, -5f);

        _text = textGo.AddComponent<TextMeshProUGUI>();
        var standardFont = UiFactory.ResolveFont();
        if (standardFont != null)
        {
            _text.font = standardFont;
            if (standardFont.material != null)
            {
                _text.fontSharedMaterial = standardFont.material;
            }
        }
        _text.fontSize = 9.5f;
        _text.lineSpacing = 1.5f;
        _text.color = CyberPalette.ColorTextMain;
        _text.alignment = TextAlignmentOptions.TopLeft;
        _text.textWrappingMode = TextWrappingModes.Normal;
        _text.overflowMode = TextOverflowModes.Overflow;
        _text.raycastTarget = false;

        _tooltipRoot.SetActive(false);
    }
}
