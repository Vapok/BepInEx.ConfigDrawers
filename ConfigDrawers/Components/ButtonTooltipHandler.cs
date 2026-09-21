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
    private const float TooltipTargetWidth = 200f;
    private const float TooltipMinWidth = 140f;
    private const float TooltipMinHeight = 28f;
    private const float ScreenPadding = 10f;
    private const float OffsetPadding = 6f;

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

        ButtonTooltipHandler handler = target.AddComponent<ButtonTooltipHandler>();
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

    private void OnDestroy()
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

        Canvas? canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        EnsureTooltipCreated(canvas);
        if (_tooltipRoot == null || _text == null)
        {
            return;
        }

        string headerFormatted = $"<color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorIceBlueBright)}><b>{_headerText}</b></color>";
        if (string.IsNullOrEmpty(_bodyText))
        {
            _text.text = headerFormatted;
        }
        else if (string.IsNullOrEmpty(_headerText))
        {
            _text.text = $"<color=#c8dbee>{_bodyText}</color>";
        }
        else
        {
            _text.text = $"{headerFormatted}\n<color=#c8dbee>{_bodyText}</color>";
        }

        _text.ForceMeshUpdate();

        float effectiveMinWidth = string.IsNullOrEmpty(_bodyText) ? 50f : TooltipMinWidth;
        float effectiveMinHeight = string.IsNullOrEmpty(_bodyText) ? 22f : TooltipMinHeight;

        Vector2 unconstrained = _text.GetPreferredValues(_text.text, 1000f, 1000f);
        float targetWidth = Mathf.Clamp(unconstrained.x + 28f, effectiveMinWidth, 340f);

        RectTransform rt = _tooltipRoot.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(targetWidth, 300f);

        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        _text.ForceMeshUpdate();

        float targetHeight = Mathf.Max(_text.preferredHeight + 18f, effectiveMinHeight);
        rt.sizeDelta = new Vector2(targetWidth, targetHeight);

        RectTransform buttonRT = (RectTransform)transform;
        Vector3[] corners = new Vector3[4];
        buttonRT.GetWorldCorners(corners);
        Vector3 buttonBottomCenter = new Vector3((corners[0].x + corners[3].x) * 0.5f, corners[0].y, 0f);

        float scale = canvas.scaleFactor > 0.01f ? canvas.scaleFactor : 1f;
        float pixelW = rt.sizeDelta.x * scale;
        float pixelH = rt.sizeDelta.y * scale;

        rt.pivot = new Vector2(0.5f, 1f);
        float posX = buttonBottomCenter.x;
        float posY = buttonBottomCenter.y - OffsetPadding * scale;

        if (posY - pixelH < ScreenPadding)
        {
            rt.pivot = new Vector2(0.5f, 0f);
            posY = corners[1].y + OffsetPadding * scale;
        }

        float minX = ScreenPadding + pixelW * 0.5f;
        float maxX = Screen.width - ScreenPadding - pixelW * 0.5f;
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

    public static void DestroyTooltip()
    {
        _activeHandler = null;
        if (_tooltipRoot != null)
        {
            Destroy(_tooltipRoot);
            _tooltipRoot = null;
            _text = null;
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

        CanvasGroup cg = _tooltipRoot.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        Image borderImg = _tooltipRoot.GetComponent<Image>();
        borderImg.color = CyberPalette.ColorIceBlueBright;
        borderImg.raycastTarget = false;

        GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(_tooltipRoot.transform, false);
        RectTransform fillRT = fillGo.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(1f, 1f);
        fillRT.offsetMax = new Vector2(-1f, -1f);

        Image fillImg = fillGo.GetComponent<Image>();
        fillImg.color = CyberPalette.ColorVoidBlack;
        fillImg.raycastTarget = false;

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.SetActive(false);
        textGo.transform.SetParent(fillGo.transform, false);
        RectTransform tRT = textGo.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(8f, 5f);
        tRT.offsetMax = new Vector2(-8f, -5f);

        _text = textGo.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset? standardFont = UIFonts.GetPrimaryFont();
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
        textGo.SetActive(true);

        _tooltipRoot.SetActive(false);
    }
}
