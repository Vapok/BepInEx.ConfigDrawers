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
    private static GameObject? _cardRoot;
    private static TextMeshProUGUI? _headerText;
    private static TextMeshProUGUI? _bodyText;
    private static TextMeshProUGUI? _badgeText;
    private const float HoverDelaySeconds = 0.4f;

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

        _hoverRoutine = StartCoroutine(ShowAfterDelay(eventData.position));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_hoverRoutine != null)
        {
            StopCoroutine(_hoverRoutine);
            _hoverRoutine = null;
        }

        HideCard();
    }

    private void OnDisable()
    {
        HideCard();
    }

    private void Update()
    {
        if (_cardRoot != null && _cardRoot.activeSelf)
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

    private IEnumerator ShowAfterDelay(Vector2 mousePos)
    {
        yield return new WaitForSecondsRealtime(HoverDelaySeconds);
        if (IsCursorOverInput(null))
        {
            yield break;
        }
        ShowCard(mousePos);
        _hoverRoutine = null;
    }

    private void ShowCard(Vector2 screenPos)
    {
        if (_entry == null)
        {
            return;
        }

        EnsureCardCreated();
        if (_cardRoot == null || _headerText == null || _bodyText == null || _badgeText == null)
        {
            return;
        }

        _headerText.text = $"<b><noparse>{_entry.DispName}</noparse></b>";
        _bodyText.text = !string.IsNullOrEmpty(_entry.Description) ? _entry.Description : "No description provided.";

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

        _badgeText.text = badges.ToString().TrimEnd();

        var rt = _cardRoot.GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        var cardW = rt.rect.width > 0f ? rt.rect.width : 250f;
        var cardH = rt.rect.height > 0f ? rt.rect.height : 75f;

        var posX = screenPos.x + 18f;
        if (posX + cardW > Screen.width - 10f)
        {
            posX = screenPos.x - cardW - 10f;
        }

        var posY = screenPos.y + cardH + 12f;
        if (posY > Screen.height - 10f)
        {
            posY = screenPos.y - 12f;
        }
        if (posY - cardH < 10f)
        {
            posY = cardH + 10f;
        }

        rt.position = new Vector3(Mathf.Clamp(posX, 10f, Screen.width - cardW - 10f), Mathf.Clamp(posY, cardH + 10f, Screen.height - 10f), 0f);

        _cardRoot.SetActive(true);
    }

    public static void HideCard()
    {
        if (_cardRoot != null && _cardRoot.activeSelf)
        {
            _cardRoot.SetActive(false);
        }
    }

    private void EnsureCardCreated()
    {
        if (_cardRoot != null)
        {
            return;
        }

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        _cardRoot = UiFactory.CreatePanel(canvas.transform, "HoverCard", CyberPalette.ColorBorderSubtle, CyberPalette.ColorVoidBlack, 1f);
        var rt = _cardRoot.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(250f, 0f);

        var le = _cardRoot.AddComponent<LayoutElement>();
        le.preferredWidth = 250f;
        le.flexibleWidth = 0f;

        var csf = _cardRoot.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var fill = _cardRoot.transform.Find("Fill") ?? _cardRoot.transform;

        var vlg = fill.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 6, 6);
        vlg.spacing = 3f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var fillCsf = fill.gameObject.AddComponent<ContentSizeFitter>();
        fillCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fillCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _headerText = UiFactory.CreateLabel(fill, "Header", "", CyberPalette.ColorIceBlueBright, 10f);
        _headerText.textWrappingMode = TextWrappingModes.Normal;

        _bodyText = UiFactory.CreateLabel(fill, "Body", "", CyberPalette.ColorTextMain, 9f);
        _bodyText.textWrappingMode = TextWrappingModes.Normal;

        _badgeText = UiFactory.CreateLabel(fill, "Badges", "", CyberPalette.ColorWarningAmber, 8.5f);
        _badgeText.textWrappingMode = TextWrappingModes.Normal;

        var canvasGroup = _cardRoot.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        _cardRoot.SetActive(false);
    }
}
