using System;
using System.Collections;
using System.Text;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Components;

public class HoverCardHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private SettingEntry? _entry;
    private Coroutine? _hoverRoutine;
    private static HoverCardHandler? _activeHandler;
    private static GameObject? _cardRoot;
    private static TextMeshProUGUI? _text;
    private const float HoverDelaySeconds = 0.35f;

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

        if (IsCursorOverInput(eventData))
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
            HideCard();
        }
    }

    private void OnDisable()
    {
        if (_activeHandler == this)
        {
            HideCard();
        }
    }

    private void Update()
    {
        if (_activeHandler == this && _cardRoot != null && _cardRoot.activeSelf)
        {
            if (EventSystem.current != null)
            {
                var selected = EventSystem.current.currentSelectedGameObject;
                if (selected != null && selected.GetComponentInParent<TMP_InputField>() != null)
                {
                    HideCard();
                }
            }
        }
    }

    private static bool IsCursorOverInput(PointerEventData? eventData)
    {
        if (eventData?.pointerEnter != null && eventData.pointerEnter.GetComponentInParent<TMP_InputField>() != null)
        {
            return true;
        }

        if (EventSystem.current != null)
        {
            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null && selected.GetComponentInParent<TMP_InputField>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSecondsRealtime(HoverDelaySeconds);
        if (IsCursorOverInput(null))
        {
            yield break;
        }
        ShowCard();
        _hoverRoutine = null;
    }

    private void ShowCard()
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

        EnsureCardCreated(canvas);
        if (_cardRoot == null || _text == null)
        {
            return;
        }

        var header = $"<color=#64f0fc><b>[ i ] {_entry.DispName.ToUpperInvariant()}:</b></color>";
        var desc = !string.IsNullOrEmpty(_entry.Description) ? _entry.Description : "No description provided.";

        var badges = new StringBuilder();
        if (!string.IsNullOrEmpty(_entry.Section))
        {
            badges.Append($"<color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorTextMuted)}>[<noparse>{_entry.Section}</noparse>]</color>  ");
        }

        if (_entry.DefaultValue != null)
        {
            var defVal = _entry.DefaultValue.ToString() ?? string.Empty;
            if (_entry.SettingType == typeof(string))
            {
                defVal = defVal.Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n");
                if (string.IsNullOrEmpty(defVal))
                {
                    defVal = "\"\"";
                }
            }
            badges.Append($"<color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorIceBlueBright)}>[DEFAULT: <noparse>{defVal}</noparse>]</color>  ");
        }

        if (_entry.IsAdminOnly)
        {
            if (_entry.CanEdit)
            {
                badges.Append($"<color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorGlacialMint)}>[SYNC: UNLOCKED]</color>  ");
            }
            else
            {
                badges.Append($"<color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorWarningAmber)}>[SYNC: LOCKED]</color>  ");
            }
        }
        else if (_entry.ReadOnly)
        {
            badges.Append($"<color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorWarningAmber)}>[READ-ONLY]</color>  ");
        }

        if (_entry.IsDirty)
        {
            badges.Append($"<color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorGlacialMint)}>[UNSAVED EDITS]</color>  ");
        }

        var badgeStr = badges.ToString().TrimEnd();
        if (!string.IsNullOrEmpty(badgeStr))
        {
            _text.text = $"{header}\n<color=#c8dbee>{desc}</color>\n\n<size=8.5pt>{badgeStr}</size>";
        }
        else
        {
            _text.text = $"{header}\n<color=#c8dbee>{desc}</color>";
        }

        _text.ForceMeshUpdate();

        const float targetWidth = 320f;
        var preferred = _text.GetPreferredValues(targetWidth - 20f, 1000f);
        var width = Mathf.Min(targetWidth, preferred.x + 24f);
        var height = preferred.y + 20f;

        var rt = _cardRoot.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(Mathf.Max(width, 240f), Mathf.Max(height, 36f));

        var leftRT = (RectTransform)transform;
        var corners = new Vector3[4];
        leftRT.GetWorldCorners(corners);
        var fieldTopLeft = corners[1];
        var fieldBottomLeft = corners[0];

        var scale = canvas.scaleFactor > 0.01f ? canvas.scaleFactor : 1f;
        var pixelW = rt.sizeDelta.x * scale;
        var pixelH = rt.sizeDelta.y * scale;

        rt.pivot = new Vector2(0f, 0f);
        var posX = fieldTopLeft.x;
        var posY = fieldTopLeft.y + 6f * scale;

        if (posY + pixelH > Screen.height - 10f)
        {
            rt.pivot = new Vector2(0f, 1f);
            posY = fieldBottomLeft.y - 6f * scale;
        }

        if (posX + pixelW > Screen.width - 10f)
        {
            posX = Screen.width - 10f - pixelW;
        }
        if (posX < 10f)
        {
            posX = 10f;
        }

        rt.position = new Vector3(posX, posY, 0f);
        _cardRoot.SetActive(true);
    }

    public static void HideCard()
    {
        _activeHandler = null;
        if (_cardRoot != null && _cardRoot.activeSelf)
        {
            _cardRoot.SetActive(false);
        }
    }

    private static void EnsureCardCreated(Canvas canvas)
    {
        if (_cardRoot != null)
        {
            return;
        }

        _cardRoot = new GameObject("CyberHoverCard", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        _cardRoot.transform.SetParent(canvas.transform, false);

        var cg = _cardRoot.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        var borderImg = _cardRoot.GetComponent<Image>();
        borderImg.color = CyberPalette.ColorIceBlueBright;
        borderImg.raycastTarget = false;

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(_cardRoot.transform, false);
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
        tRT.offsetMin = new Vector2(10f, 8f);
        tRT.offsetMax = new Vector2(-10f, -8f);

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

        _cardRoot.SetActive(false);
    }
}
