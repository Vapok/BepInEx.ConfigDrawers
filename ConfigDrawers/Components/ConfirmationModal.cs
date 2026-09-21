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
    private RectTransform? _cardRT;
    private TextMeshProUGUI? _titleText;
    private TextMeshProUGUI? _messageText;
    private LayoutElement? _msgBoxLe;
    private Transform? _buttonContainer;

    public bool IsOpen => _modalRoot != null && _modalRoot.activeSelf;

    public static ConfirmationModal Attach(GameObject host)
    {
        if (_instance != null)
        {
            if (_instance.gameObject == host)
            {
                return _instance;
            }

            Destroy(_instance);
            _instance = null;
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
        backdropImg.color = new Color(0.015f, 0.025f, 0.04f, 0.85f);
        backdropImg.raycastTarget = true;

        CanvasGroup cg = _modalRoot.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
        cg.interactable = true;

        GameObject card = UiFactory.CreatePanel(_modalRoot.transform, "DialogCard", CyberPalette.ColorIceBlue, CyberPalette.ColorCardSurface, 1.5f);
        _cardRT = card.GetComponent<RectTransform>();
        _cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        _cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        _cardRT.pivot = new Vector2(0.5f, 0.5f);
        _cardRT.anchoredPosition = Vector2.zero;
        _cardRT.sizeDelta = new Vector2(360f, 160f);

        Transform? cardFill = card.transform.Find("Fill");
        Transform targetParent = cardFill != null ? cardFill : card.transform;

        VerticalLayoutGroup vlg = targetParent.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 12, 12);
        vlg.spacing = 10f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        _titleText = UiFactory.CreateLabel(targetParent, "Title", "[ CONFIRM ACTION ]", CyberPalette.ColorIceBlueBright, 11.5f);
        _titleText.fontStyle = FontStyles.Bold;
        LayoutElement titleLe = _titleText.gameObject.AddComponent<LayoutElement>();
        titleLe.minHeight = 18f;
        titleLe.preferredHeight = 18f;
        titleLe.flexibleHeight = 0f;

        GameObject sep = new GameObject("Separator", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        sep.transform.SetParent(targetParent, false);
        Image sepImg = sep.GetComponent<Image>();
        sepImg.color = CyberPalette.ColorSeparator;
        LayoutElement sepLe = sep.GetComponent<LayoutElement>();
        sepLe.minHeight = 1f;
        sepLe.preferredHeight = 1f;
        sepLe.flexibleHeight = 0f;

        GameObject msgBox = UiFactory.CreatePanel(targetParent, "MessageBox", CyberPalette.ColorInputGroove, CyberPalette.ColorInputWell, 1f);
        _msgBoxLe = msgBox.AddComponent<LayoutElement>();
        _msgBoxLe.minHeight = 44f;
        _msgBoxLe.preferredHeight = 44f;
        _msgBoxLe.flexibleHeight = 0f;

        Transform? msgFill = msgBox.transform.Find("Fill");
        Transform msgTarget = msgFill != null ? msgFill : msgBox.transform;

        VerticalLayoutGroup msgVlg = msgTarget.gameObject.AddComponent<VerticalLayoutGroup>();
        msgVlg.padding = new RectOffset(10, 10, 8, 8);
        msgVlg.childControlWidth = true;
        msgVlg.childControlHeight = true;
        msgVlg.childForceExpandWidth = true;
        msgVlg.childForceExpandHeight = true;

        _messageText = UiFactory.CreateLabel(msgTarget, "Message", "Are you sure you want to proceed?", CyberPalette.ColorTextMain, 10f);
        _messageText.textWrappingMode = TextWrappingModes.Normal;
        _messageText.overflowMode = TextOverflowModes.Overflow;

        GameObject btnRow = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        btnRow.transform.SetParent(targetParent, false);
        _buttonContainer = btnRow.transform;

        HorizontalLayoutGroup hlg = btnRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleRight;

        LayoutElement btnRowLe = btnRow.GetComponent<LayoutElement>();
        btnRowLe.minHeight = 24f;
        btnRowLe.preferredHeight = 24f;
        btnRowLe.flexibleHeight = 0f;

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
            if (title.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _titleText.color = CyberPalette.ColorErrorRed;
            }
            else if (title.IndexOf("warning", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _titleText.color = CyberPalette.ColorWarningAmber;
            }
            else
            {
                _titleText.color = CyberPalette.ColorIceBlueBright;
            }
        }

        float msgBoxH = 44f;
        if (_messageText != null)
        {
            _messageText.text = message;
            _messageText.ForceMeshUpdate();
            float textH = _messageText.preferredHeight;
            msgBoxH = Mathf.Clamp(textH + 18f, 44f, 220f);
            if (_msgBoxLe != null)
            {
                _msgBoxLe.minHeight = msgBoxH;
                _msgBoxLe.preferredHeight = msgBoxH;
            }
        }

        RectTransform? hostRT = transform as RectTransform;
        float drawerWidth = 480f;
        if (hostRT != null && hostRT.rect.width > 50f)
        {
            drawerWidth = hostRT.rect.width;
        }

        float cardWidth = Mathf.Clamp(drawerWidth - 40f, 320f, 380f);
        float cardHeight = 97f + msgBoxH;

        if (_cardRT != null)
        {
            _cardRT.sizeDelta = new Vector2(cardWidth, cardHeight);
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
            }, CyberPalette.ColorBorderSubtle, CyberPalette.ColorTextMuted, 75f, 24f);
        }

        if (!string.IsNullOrEmpty(saveText) && onSave != null)
        {
            UiFactory.CreateCyberButton(_buttonContainer, "SaveBtn", saveText!, () =>
            {
                Hide();
                onSave();
            }, CyberPalette.ColorIceBlueBright, CyberPalette.ColorIceBlueBright, 95f, 24f);
        }

        Color confirmBorderColor;
        Color confirmTextColor;
        float confirmWidth = 75f;

        if (confirmText.Equals("OK", StringComparison.OrdinalIgnoreCase))
        {
            confirmBorderColor = CyberPalette.ColorIceBlueBright;
            confirmTextColor = CyberPalette.ColorIceBlueBright;
            confirmWidth = 65f;
        }
        else if (confirmText.IndexOf("warning", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 confirmText.IndexOf("anyway", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            confirmBorderColor = CyberPalette.ColorWarningAmber;
            confirmTextColor = CyberPalette.ColorWarningAmber;
            confirmWidth = 95f;
        }
        else
        {
            confirmBorderColor = CyberPalette.ColorErrorRed;
            confirmTextColor = CyberPalette.ColorErrorRed;
            confirmWidth = 75f;
        }

        UiFactory.CreateCyberButton(_buttonContainer, "ConfirmBtn", confirmText, () =>
        {
            Hide();
            onConfirm();
        }, confirmBorderColor, confirmTextColor, confirmWidth, 24f);

        if (_cardRT != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_cardRT);
        }

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
