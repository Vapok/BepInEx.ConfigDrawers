using System;
using System.Collections;
using System.Text;
using ConfigDrawer.Models;
using ConfigDrawer.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ConfigDrawer.Components;

public class HoverCardHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private SettingEntry? _entry;
    private Coroutine? _hoverRoutine;
    private static GameObject? _cardRoot;
    private static TextMeshProUGUI? _headerText;
    private static TextMeshProUGUI? _bodyText;
    private static TextMeshProUGUI? _badgeText;
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

    private IEnumerator ShowAfterDelay(Vector2 mousePos)
    {
        yield return new WaitForSecondsRealtime(HoverDelaySeconds);
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

        _headerText.text = $"{_entry.DispName} <color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorTextMuted)}>({_entry.Section})</color>";
        _bodyText.text = !string.IsNullOrEmpty(_entry.Description) ? _entry.Description : "No description provided.";

        var badges = new StringBuilder();
        if (_entry.DefaultValue != null)
        {
            badges.Append($"<color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorIceBlueBright)}>[DEFAULT: {_entry.DefaultValue}]</color>  ");
        }

        if (_entry.IsAdminOnly || !_entry.IsUnlocked)
        {
            badges.Append($"<color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorWarningAmber)}>[SERVERSYNC LOCKED]</color>  ");
        }

        if (_entry.IsDirty)
        {
            badges.Append($"<color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorGlacialMint)}>[UNSAVED EDITS]</color>  ");
        }

        _badgeText.text = badges.ToString().TrimEnd();

        var rt = _cardRoot.GetComponent<RectTransform>();
        var clampedX = Mathf.Clamp(screenPos.x + 16f, 10f, Screen.width - rt.rect.width - 10f);
        var clampedY = Mathf.Clamp(screenPos.y - 16f, rt.rect.height + 10f, Screen.height - 10f);
        rt.position = new Vector3(clampedX, clampedY, 0f);

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

        _cardRoot = UiFactory.CreatePanel(canvas.transform, "HoverCard", CyberPalette.ColorIceBlue, CyberPalette.ColorVoidBlack, 1f);
        var rt = _cardRoot.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(340f, 160f);
        rt.pivot = new Vector2(0f, 1f);

        var fill = _cardRoot.transform.Find("Fill");
        var target = fill != null ? fill : _cardRoot.transform;

        _headerText = UiFactory.CreateLabel(target, "Header", "", CyberPalette.ColorIceBlueBright, 12f);
        var headerRT = _headerText.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.offsetMin = new Vector2(10f, -30f);
        headerRT.offsetMax = new Vector2(-10f, -8f);

        _bodyText = UiFactory.CreateLabel(target, "Body", "", CyberPalette.ColorTextMain, 11f);
        var bodyRT = _bodyText.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0f, 0f);
        bodyRT.anchorMax = new Vector2(1f, 1f);
        bodyRT.offsetMin = new Vector2(10f, 32f);
        bodyRT.offsetMax = new Vector2(-10f, -34f);

        _badgeText = UiFactory.CreateLabel(target, "Badges", "", CyberPalette.ColorWarningAmber, 10f);
        var badgeRT = _badgeText.GetComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(0f, 0f);
        badgeRT.anchorMax = new Vector2(1f, 0f);
        badgeRT.offsetMin = new Vector2(10f, 8f);
        badgeRT.offsetMax = new Vector2(-10f, 28f);

        _cardRoot.SetActive(false);
    }
}
