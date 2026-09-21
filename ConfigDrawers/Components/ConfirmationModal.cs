using System;
using BepInEx.ConfigDrawers.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Components;

public class ConfirmationModal : MonoBehaviour
{
    private static ConfirmationModal? _instance;
    public static ConfirmationModal? Instance => _instance;

    private GameObject? _modalRoot;
    private TextMeshProUGUI? _titleText;
    private TextMeshProUGUI? _messageText;
    private Transform? _buttonContainer;

    public bool IsOpen => _modalRoot != null && _modalRoot.activeSelf;

    public static ConfirmationModal Attach(GameObject host)
    {
        if (_instance != null)
        {
            return _instance;
        }

        _instance = host.AddComponent<ConfirmationModal>();
        return _instance;
    }

    private void Awake()
    {
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void EnsureModalBuilt()
    {
        if (_modalRoot != null)
        {
            return;
        }

        _modalRoot = new GameObject("ConfirmationModalRoot", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        _modalRoot.transform.SetParent(transform, false);

        RectTransform rootRT = _modalRoot.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        Image backdropImg = _modalRoot.GetComponent<Image>();
        backdropImg.color = new Color(0.01f, 0.02f, 0.04f, 0.82f);
        backdropImg.raycastTarget = true;

        CanvasGroup cg = _modalRoot.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
        cg.interactable = true;

        GameObject card = UiFactory.CreatePanel(_modalRoot.transform, "DialogCard", CyberPalette.ColorIceBlueBright, CyberPalette.ColorVoidBlack, 1.5f);
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(460f, 210f);

        Transform cardFill = card.transform.Find("Fill");
        Transform targetParent = cardFill != null ? cardFill : card.transform;

        VerticalLayoutGroup vlg = targetParent.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 16, 16);
        vlg.spacing = 14f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        _titleText = UiFactory.CreateLabel(targetParent, "Title", "[ CONFIRM ACTION ]", CyberPalette.ColorIceBlueBright, 13f);
        _titleText.fontStyle = FontStyles.Bold;

        _messageText = UiFactory.CreateLabel(targetParent, "Message", "Are you sure you want to proceed?", CyberPalette.ColorTextMain, 10.5f);
        _messageText.textWrappingMode = TextWrappingModes.Normal;
        LayoutElement msgLe = _messageText.gameObject.AddComponent<LayoutElement>();
        msgLe.minHeight = 44f;
        msgLe.flexibleHeight = 1f;

        GameObject btnRow = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        btnRow.transform.SetParent(targetParent, false);
        _buttonContainer = btnRow.transform;

        HorizontalLayoutGroup hlg = btnRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleRight;

        LayoutElement btnRowLe = btnRow.AddComponent<LayoutElement>();
        btnRowLe.minHeight = 28f;
        btnRowLe.preferredHeight = 28f;

        _modalRoot.SetActive(false);
    }

    public void Show(
        string title,
        string message,
        string confirmText,
        Action onConfirm,
        string cancelText = "Cancel",
        Action? onCancel = null,
        string? saveText = null,
        Action? onSave = null)
    {
        EnsureModalBuilt();
        if (_modalRoot == null || _buttonContainer == null)
        {
            return;
        }

        if (_titleText != null)
        {
            _titleText.text = title;
        }

        if (_messageText != null)
        {
            _messageText.text = message;
        }

        for (int i = _buttonContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(_buttonContainer.GetChild(i).gameObject);
        }

        if (!string.IsNullOrEmpty(cancelText))
        {
            UiFactory.CreateCyberButton(_buttonContainer, "CancelBtn", cancelText, () =>
            {
                Hide();
                onCancel?.Invoke();
            }, CyberPalette.ColorBorderSubtle, CyberPalette.ColorTextMuted, 90f, 26f);
        }

        if (!string.IsNullOrEmpty(saveText) && onSave != null)
        {
            UiFactory.CreateCyberButton(_buttonContainer, "SaveBtn", saveText!, () =>
            {
                Hide();
                onSave();
            }, CyberPalette.ColorIceBlueBright, CyberPalette.ColorIceBlueBright, 110f, 26f);
        }

        UiFactory.CreateCyberButton(_buttonContainer, "ConfirmBtn", confirmText, () =>
        {
            Hide();
            onConfirm();
        }, CyberPalette.ColorErrorRed, CyberPalette.ColorErrorRed, 100f, 26f);

        _modalRoot.transform.SetAsLastSibling();
        _modalRoot.SetActive(true);
    }

    public void Hide()
    {
        if (_modalRoot != null && _modalRoot.activeSelf)
        {
            _modalRoot.SetActive(false);
        }
    }
}
