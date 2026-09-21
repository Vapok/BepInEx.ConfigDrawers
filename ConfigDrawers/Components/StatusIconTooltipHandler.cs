using System;
using System.Collections;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Components;

public class StatusIconTooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private SettingEntry? _entry;
    private Coroutine? _hoverRoutine;
    private static StatusIconTooltipHandler? _activeHandler;
    private static GameObject? _tooltipRoot;
    private static TextMeshProUGUI? _text;
    private static SettingEntry? _lastEntry;
    private static bool? _lastCanEdit;
    private static object? _lastBoxedValue;

    private const float HoverDelaySeconds = 0.2f;
    private const float TooltipTargetWidth = 270f;
    private const float TooltipMinWidth = 220f;
    private const float TooltipMinHeight = 36f;
    private const float ScreenPadding = 10f;
    private const float OffsetPadding = 6f;

    public void Bind(SettingEntry entry)
    {
        _entry = entry ?? throw new ArgumentNullException(nameof(entry));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_entry == null)
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
        if (_activeHandler == this)
        {
            HideTooltip();
        }
    }

    private void OnDestroy()
    {
        if (_activeHandler == this)
        {
            HideTooltip();
        }
    }

    private void Update()
    {
        if (_activeHandler == this && _tooltipRoot != null && _tooltipRoot.activeSelf && _entry != null)
        {
            UpdateContent();
        }
    }

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSecondsRealtime(HoverDelaySeconds);
        ShowTooltip();
        _hoverRoutine = null;
    }

    private void UpdateContent()
    {
        if (_entry == null || _text == null)
        {
            return;
        }

        bool isEditable = _entry.CanEdit;
        object? boxedVal = _entry.ConfigEntry?.BoxedValue;

        if (_lastEntry == _entry && _lastCanEdit == isEditable && Equals(_lastBoxedValue, boxedVal))
        {
            return;
        }

        _lastEntry = _entry;
        _lastCanEdit = isEditable;
        _lastBoxedValue = boxedVal;
        string header = isEditable
            ? "<color=#64f0fc><b>[<size=140%>⇄</size>] SERVER SYNCED:</b></color>"
            : "<color=#e5a93c><b>[🔒] SERVER ENFORCED:</b></color>";

        string desc = isEditable
            ? "Synchronized with server. You have admin access to modify this setting globally."
            : "Locked by server configuration. Only server administrators can change this value.";

        string valStr = _entry.ConfigEntry?.BoxedValue?.ToString() ?? "null";
        if (_entry.SettingType == typeof(string))
        {
            valStr = valStr.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
            if (valStr.Length > 40)
            {
                valStr = valStr.Substring(0, 37) + "...";
            }
            valStr = $"\"{valStr}\"";
        }
        else if (valStr.Length > 40)
        {
            valStr = valStr.Substring(0, 37) + "...";
        }

        string valInfo = $"<color=#8ba2b5>Setting:</color> <color=#c8dbee>{_entry.DispName}</color>\n<color=#8ba2b5>Current Value:</color> <color=#5cfbde><b>{valStr}</b></color>";

        _text.text = $"{header}\n<color=#c8dbee>{desc}</color>\n\n{valInfo}";
    }

    private void ShowTooltip()
    {
        if (_entry == null)
        {
            return;
        }

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

        UpdateContent();
        _text.ForceMeshUpdate();

        Vector2 preferred = _text.GetPreferredValues(TooltipTargetWidth - 16f, 1000f);
        float width = Mathf.Min(TooltipTargetWidth, preferred.x + 20f);
        float height = preferred.y + 16f;

        RectTransform rt = _tooltipRoot.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(Mathf.Max(width, TooltipMinWidth), Mathf.Max(height, TooltipMinHeight));

        RectTransform iconRT = (RectTransform)transform;
        Vector3[] corners = new Vector3[4];
        iconRT.GetWorldCorners(corners);
        Vector3 iconTopRight = corners[2];
        Vector3 iconBottomRight = corners[3];

        float scale = canvas.scaleFactor > 0.01f ? canvas.scaleFactor : 1f;
        float pixelW = rt.sizeDelta.x * scale;
        float pixelH = rt.sizeDelta.y * scale;

        rt.pivot = new Vector2(1f, 0f);
        float posX = iconTopRight.x;
        float posY = iconTopRight.y + OffsetPadding * scale;

        if (posY + pixelH > Screen.height - ScreenPadding)
        {
            rt.pivot = new Vector2(1f, 1f);
            posY = iconBottomRight.y - OffsetPadding * scale;
        }

        if (posX - pixelW < ScreenPadding)
        {
            rt.pivot = new Vector2(0f, rt.pivot.y);
            posX = corners[1].x;
        }

        rt.position = new Vector3(posX, posY, 0f);
        _tooltipRoot.SetActive(true);
    }

    public static void HideTooltip()
    {
        _activeHandler = null;
        _lastEntry = null;
        _lastCanEdit = null;
        _lastBoxedValue = null;
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

        _tooltipRoot = new GameObject("SyncCyberTooltip", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
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
        tRT.offsetMin = new Vector2(8f, 6f);
        tRT.offsetMax = new Vector2(-8f, -6f);

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
        _text.lineSpacing = 2f;
        _text.color = CyberPalette.ColorTextMain;
        _text.alignment = TextAlignmentOptions.TopLeft;
        _text.textWrappingMode = TextWrappingModes.Normal;
        _text.overflowMode = TextOverflowModes.Overflow;
        _text.raycastTarget = false;
        textGo.SetActive(true);

        _tooltipRoot.SetActive(false);
    }
}
