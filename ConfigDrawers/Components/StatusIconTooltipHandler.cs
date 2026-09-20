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
    private const float HoverDelaySeconds = 0.2f;

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

        var isEditable = _entry.CanEdit;
        var header = isEditable
            ? "<color=#64f0fc><b>[ ⇄ ] SERVER SYNCED:</b></color>"
            : "<color=#e5a93c><b>[ 🔒 ] SERVER ENFORCED:</b></color>";

        var desc = isEditable
            ? "Synchronized with server. You have admin access to modify this setting globally."
            : "Locked by server configuration. Only server administrators can change this value.";

        var valStr = _entry.ConfigEntry?.BoxedValue?.ToString() ?? "null";
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

        var valInfo = $"<color=#8ba2b5>Setting:</color> <color=#c8dbee>{_entry.DispName}</color>\n<color=#8ba2b5>Current Value:</color> <color=#5cfbde><b>{valStr}</b></color>";

        _text.text = $"{header}\n<color=#c8dbee>{desc}</color>\n\n{valInfo}";
    }

    private void ShowTooltip()
    {
        if (_entry == null)
        {
            return;
        }

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

        UpdateContent();
        _text.ForceMeshUpdate();

        const float targetWidth = 270f;
        var preferred = _text.GetPreferredValues(targetWidth - 16f, 1000f);
        var width = Mathf.Min(targetWidth, preferred.x + 20f);
        var height = preferred.y + 16f;

        var rt = _tooltipRoot.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(Mathf.Max(width, 220f), Mathf.Max(height, 36f));

        var iconRT = (RectTransform)transform;
        var corners = new Vector3[4];
        iconRT.GetWorldCorners(corners);
        var iconTopRight = corners[2];
        var iconBottomRight = corners[3];

        var scale = canvas.scaleFactor > 0.01f ? canvas.scaleFactor : 1f;
        var pixelW = rt.sizeDelta.x * scale;
        var pixelH = rt.sizeDelta.y * scale;

        rt.pivot = new Vector2(1f, 0f);
        var posX = iconTopRight.x;
        var posY = iconTopRight.y + 6f * scale;

        if (posY + pixelH > Screen.height - 10f)
        {
            rt.pivot = new Vector2(1f, 1f);
            posY = iconBottomRight.y - 6f * scale;
        }

        if (posX - pixelW < 10f)
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
        tRT.offsetMin = new Vector2(8f, 6f);
        tRT.offsetMax = new Vector2(-8f, -6f);

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
        _text.lineSpacing = 2f;
        _text.color = CyberPalette.ColorTextMain;
        _text.alignment = TextAlignmentOptions.TopLeft;
        _text.textWrappingMode = TextWrappingModes.Normal;
        _text.overflowMode = TextOverflowModes.Overflow;
        _text.raycastTarget = false;

        _tooltipRoot.SetActive(false);
    }
}
