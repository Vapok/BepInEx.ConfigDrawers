using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.UI;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Files;

public class ConfigFileEditor : MonoBehaviour
{
    private static readonly MethodInfo? AdjustPositionMethod = AccessTools.Method(typeof(TMP_InputField), "AdjustTextPositionRelativeToViewport", new Type[] { typeof(float) });
    private static readonly MethodInfo? GetScrollPositionMethod = AccessTools.Method(typeof(TMP_InputField), "GetScrollPositionRelativeToViewport");
    private static readonly MethodInfo? AssignPositioningMethod = AccessTools.Method(typeof(TMP_InputField), "AssignPositioningIfNeeded");

    private ConfigFileItem? _fileItem;
    private string _originalContent = string.Empty;
    private string _currentContent = string.Empty;

    private readonly Stack<string> _undoStack = new();
    private readonly Stack<string> _redoStack = new();
    private const int MaxUndoSteps = 50;

    private Action? _onBackRequested;

    private TextMeshProUGUI? _titleLabel;
    private TextMeshProUGUI? _dirtyIndicator;
    private TextMeshProUGUI? _statusFooter;
    private TextMeshProUGUI? _lineNumbersText;
    private TMP_InputField? _editorInput;
    private int _lastCaretPosition = -1;

    private GameObject? _saveBtnObj;
    private Image? _saveBtnBorder;
    private Image? _saveBtnIcon;

    private GameObject? _validatorBadgeObj;
    private TextMeshProUGUI? _validatorBadgeText;
    private Image? _validatorBadgeBorder;
    private ButtonTooltipHandler? _validatorTooltip;

    private Scrollbar? _editorScrollbar;
    private ScrollbarDragTracker? _dragTracker;
    private Scrollbar? _editorHScrollbar;
    private Image? _hHandleImg;
    private ScrollbarDragTracker? _hDragTracker;
    private bool _isSyncingScrollbar;
    private bool _isSyncingHScrollbar;
    private bool _isApplyingHistory;
    private Coroutine? _syncGutterRoutine;

    public bool IsDirty => _fileItem != null && _currentContent != _originalContent;
    public ConfigFileItem? CurrentFile => _fileItem;
    public bool IsFocused => _editorInput != null && _editorInput.isFocused;

    public void Defocus()
    {
        if (_editorInput != null && _editorInput.isFocused)
        {
            _editorInput.DeactivateInputField();
        }
    }

    public void OpenFile(Transform container, ConfigFileItem fileItem, Action onBack)
    {
        if (container == null)
        {
            throw new ArgumentNullException(nameof(container));
        }

        _fileItem = fileItem ?? throw new ArgumentNullException(nameof(fileItem));
        _onBackRequested = onBack;

        try
        {
            _originalContent = ConfigFileManager.Instance.ReadFileText(fileItem);
        }
        catch (Exception ex)
        {
            _originalContent = $"[Error reading file: {ex.Message}]";
        }

        _currentContent = _originalContent.Replace("\r\n", "\n").Replace("\r", "\n");
        _undoStack.Clear();
        _redoStack.Clear();

        BuildEditorUI(container);
        UpdateDirtyState();
        UpdateLineNumbers();
        ValidateJsonIfNeeded();

        StartCoroutine(FocusEditorRoutine());
    }

    private IEnumerator FocusEditorRoutine()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        if (_editorInput != null)
        {
            EventSystem currentEventSystem = EventSystem.current;
            if (currentEventSystem != null)
            {
                currentEventSystem.SetSelectedGameObject(_editorInput.gameObject);
            }
            _editorInput.ActivateInputField();
            _editorInput.caretPosition = 0;
            _editorInput.stringPosition = 0;
        }
    }

    private void BuildEditorUI(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }

        GameObject rootPanel = new GameObject("EditorRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
        rootPanel.transform.SetParent(parent, false);

        RectTransform rootRT = rootPanel.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = rootPanel.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(6, 6, 4, 4);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        BuildTopToolbar(rootPanel.transform);
        BuildEditorSurface(rootPanel.transform);
        BuildStatusBar(rootPanel.transform);
    }

    private void BuildTopToolbar(Transform parent)
    {
        GameObject toolbarObj = new GameObject("EditorToolbar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        toolbarObj.transform.SetParent(parent, false);

        RectTransform tbRT = toolbarObj.GetComponent<RectTransform>();
        tbRT.sizeDelta = new Vector2(0f, 26f);

        LayoutElement tbLe = toolbarObj.AddComponent<LayoutElement>();
        tbLe.minHeight = 26f;
        tbLe.preferredHeight = 26f;
        tbLe.flexibleHeight = 0f;

        HorizontalLayoutGroup hlg = toolbarObj.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        GameObject backBtnObj = UiFactory.CreateCyberButton(toolbarObj.transform, "BackBtn", "◄ Back", HandleBackClicked, CyberPalette.ColorBorderSubtle, CyberPalette.ColorTextMain, 52f, 24f);
        LayoutElement backLe = backBtnObj.GetComponent<LayoutElement>();
        backLe.minWidth = 52f;
        backLe.preferredWidth = 52f;
        backLe.flexibleWidth = 0f;

        GameObject titleGroup = new GameObject("TitleGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        titleGroup.transform.SetParent(toolbarObj.transform, false);

        LayoutElement titleLe = titleGroup.AddComponent<LayoutElement>();
        titleLe.minWidth = 80f;
        titleLe.flexibleWidth = 1f;
        titleLe.flexibleHeight = 1f;

        HorizontalLayoutGroup titleHlg = titleGroup.GetComponent<HorizontalLayoutGroup>();
        titleHlg.spacing = 4f;
        titleHlg.childControlWidth = false;
        titleHlg.childControlHeight = true;
        titleHlg.childForceExpandWidth = false;
        titleHlg.childForceExpandHeight = false;
        titleHlg.childAlignment = TextAnchor.MiddleLeft;

        string displayTitle = _fileItem != null ? _fileItem.FileName : "File Editor";
        _titleLabel = UiFactory.CreateLabel(titleGroup.transform, "FileTitle", displayTitle, CyberPalette.ColorIceBlueBright, 10.5f);
        _titleLabel.fontStyle = FontStyles.Bold;

        _dirtyIndicator = UiFactory.CreateLabel(titleGroup.transform, "DirtyDot", "*", CyberPalette.ColorWarningAmber, 13f);
        _dirtyIndicator.fontStyle = FontStyles.Bold;
        _dirtyIndicator.gameObject.SetActive(false);

        GameObject rightBtns = new GameObject("RightActions", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rightBtns.transform.SetParent(toolbarObj.transform, false);

        bool isJson = _fileItem != null && _fileItem.Extension == ".json";
        float rightWidth = isJson ? 148f : 120f;

        LayoutElement rightLe = rightBtns.AddComponent<LayoutElement>();
        rightLe.minWidth = rightWidth;
        rightLe.preferredWidth = rightWidth;
        rightLe.flexibleWidth = 0f;
        rightLe.flexibleHeight = 1f;

        HorizontalLayoutGroup rightHlg = rightBtns.GetComponent<HorizontalLayoutGroup>();
        rightHlg.spacing = 4f;
        rightHlg.childControlWidth = false;
        rightHlg.childControlHeight = true;
        rightHlg.childForceExpandWidth = false;
        rightHlg.childForceExpandHeight = false;
        rightHlg.childAlignment = TextAnchor.MiddleRight;

        UiFactory.CreateIconButton(rightBtns.transform, "UndoBtn", IconFactory.GetUndoIcon(), "Undo", "Revert last change (Ctrl+Z)", PerformUndo, CyberPalette.ColorBorderSubtle, CyberPalette.ColorTextMuted, 24f);
        UiFactory.CreateIconButton(rightBtns.transform, "RedoBtn", IconFactory.GetRedoIcon(), "Redo", "Reapply change (Ctrl+Y)", PerformRedo, CyberPalette.ColorBorderSubtle, CyberPalette.ColorTextMuted, 24f);

        if (isJson)
        {
            UiFactory.CreateIconButton(rightBtns.transform, "FormatBtn", IconFactory.GetFormatIcon(), "Format JSON", "Format and indent JSON contents", FormatJsonContent, CyberPalette.ColorGlacialMint, CyberPalette.ColorGlacialMint, 24f);
        }

        UiFactory.CreateIconButton(rightBtns.transform, "RevertBtn", IconFactory.GetRevertIcon(), "Revert File", "Discard changes and reload original file from disk", HandleRevertClicked, CyberPalette.ColorWarningAmber, CyberPalette.ColorWarningAmber, 24f);

        (GameObject saveRoot, Image saveIcon) = UiFactory.CreateIconButton(rightBtns.transform, "SaveBtn", IconFactory.GetSaveIcon(), "Save File", "Save changes to disk (Ctrl+S)", PerformSave, CyberPalette.ColorBorderSubtle, CyberPalette.ColorTextMuted, 24f);
        _saveBtnObj = saveRoot;
        _saveBtnIcon = saveIcon;
        _saveBtnBorder = saveRoot.GetComponent<Image>();
    }

    private void BuildEditorSurface(Transform parent)
    {
        GameObject surfaceObj = UiFactory.CreatePanel(parent, "EditorSurface", CyberPalette.ColorBorderCard, CyberPalette.ColorVoidBlack, 1f);
        LayoutElement surfaceLe = surfaceObj.AddComponent<LayoutElement>();
        surfaceLe.minHeight = 120f;
        surfaceLe.flexibleHeight = 1f;
        surfaceLe.flexibleWidth = 1f;

        Transform? surfaceFill = surfaceObj.transform.Find("Fill");
        Transform fill = surfaceFill != null ? surfaceFill : surfaceObj.transform;

        GameObject hContainer = new GameObject("SplitLayout", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        hContainer.transform.SetParent(fill, false);

        RectTransform splitRT = hContainer.GetComponent<RectTransform>();
        splitRT.anchorMin = Vector2.zero;
        splitRT.anchorMax = Vector2.one;
        splitRT.offsetMin = Vector2.zero;
        splitRT.offsetMax = Vector2.zero;

        HorizontalLayoutGroup splitHlg = hContainer.GetComponent<HorizontalLayoutGroup>();
        splitHlg.spacing = 0f;
        splitHlg.childControlWidth = true;
        splitHlg.childControlHeight = true;
        splitHlg.childForceExpandWidth = false;
        splitHlg.childForceExpandHeight = true;

        GameObject gutterObj = new GameObject("LineGutter", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(LayoutElement));
        gutterObj.transform.SetParent(hContainer.transform, false);

        Image gutterBg = gutterObj.GetComponent<Image>();
        gutterBg.color = new Color(0.015f, 0.025f, 0.04f, 0.95f);
        gutterBg.raycastTarget = true;

        LayoutElement gutterLe = gutterObj.GetComponent<LayoutElement>();
        gutterLe.minWidth = 38f;
        gutterLe.preferredWidth = 38f;
        gutterLe.flexibleWidth = 0f;
        gutterLe.flexibleHeight = 1f;

        GameObject gutterTextObj = new GameObject("NumbersText", typeof(RectTransform));
        gutterTextObj.SetActive(false);
        gutterTextObj.transform.SetParent(gutterObj.transform, false);
        RectTransform gtRT = gutterTextObj.GetComponent<RectTransform>();
        gtRT.anchorMin = Vector2.zero;
        gtRT.anchorMax = Vector2.one;
        gtRT.offsetMin = new Vector2(2f, 12f);
        gtRT.offsetMax = new Vector2(-6f, -6f);

        _lineNumbersText = gutterTextObj.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset? font = UIFonts.GetPrimaryFont();
        if (font != null)
        {
            _lineNumbersText.font = font;
            if (font.material != null)
            {
                _lineNumbersText.fontSharedMaterial = font.material;
            }
        }
        _lineNumbersText.fontSize = UiFactory.GetScaledFontSize(10f);
        _lineNumbersText.lineSpacing = 0f;
        _lineNumbersText.color = CyberPalette.ColorTextMuted;
        _lineNumbersText.alignment = TextAlignmentOptions.TopRight;
        _lineNumbersText.textWrappingMode = TextWrappingModes.NoWrap;
        _lineNumbersText.overflowMode = TextOverflowModes.Overflow;
        _lineNumbersText.maxVisibleCharacters = int.MaxValue;
        _lineNumbersText.maxVisibleWords = int.MaxValue;
        _lineNumbersText.maxVisibleLines = int.MaxValue;
        _lineNumbersText.raycastTarget = false;
        gutterTextObj.SetActive(true);

        ClickableBarHandler gutterClick = gutterObj.AddComponent<ClickableBarHandler>();
        gutterClick.OnClick = () =>
        {
            if (_editorInput != null && !_editorInput.isFocused)
            {
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(_editorInput.gameObject);
                }
                _editorInput.ActivateInputField();
            }
        };

        GameObject divObj = new GameObject("GutterDivider", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        divObj.transform.SetParent(hContainer.transform, false);
        Image divImg = divObj.GetComponent<Image>();
        divImg.color = CyberPalette.ColorSeparator;
        divImg.raycastTarget = false;
        LayoutElement divLe = divObj.GetComponent<LayoutElement>();
        divLe.minWidth = 1f;
        divLe.preferredWidth = 1f;
        divLe.flexibleWidth = 0f;
        divLe.flexibleHeight = 1f;

        GameObject editorColumn = new GameObject("EditorColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        editorColumn.transform.SetParent(hContainer.transform, false);

        LayoutElement colLe = editorColumn.GetComponent<LayoutElement>();
        colLe.minWidth = 100f;
        colLe.preferredWidth = -1f;
        colLe.flexibleWidth = 1f;
        colLe.flexibleHeight = 1f;

        VerticalLayoutGroup colVlg = editorColumn.GetComponent<VerticalLayoutGroup>();
        colVlg.spacing = 0f;
        colVlg.childControlWidth = true;
        colVlg.childControlHeight = true;
        colVlg.childForceExpandWidth = true;
        colVlg.childForceExpandHeight = false;

        (GameObject inputRoot, TMP_InputField input) = UiFactory.CreateInputField(
            editorColumn.transform,
            "CodeInput",
            _currentContent,
            OnContentCommitted,
            -1f,
            -1f,
            "",
            true,
            false);

        _editorInput = input;
        _editorInput.customCaretColor = true;
        _editorInput.caretColor = CyberPalette.ColorIceBlueBright;
        _editorInput.caretWidth = 3;
        _editorInput.caretBlinkRate = 0.85f;
        _editorInput.onFocusSelectAll = false;
        _editorInput.scrollSensitivity = 4f;

        if (_editorInput.textComponent != null)
        {
            _editorInput.textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            _editorInput.textComponent.maxVisibleCharacters = int.MaxValue;
            _editorInput.textComponent.maxVisibleWords = int.MaxValue;
            _editorInput.textComponent.maxVisibleLines = int.MaxValue;
            _editorInput.textComponent.OnPreRenderText -= OnTextPreRender;
            _editorInput.textComponent.OnPreRenderText += OnTextPreRender;
        }

        LayoutElement inputLe = inputRoot.GetComponent<LayoutElement>();
        inputLe.minWidth = 100f;
        inputLe.preferredWidth = -1f;
        inputLe.flexibleWidth = 1f;
        inputLe.minHeight = 100f;
        inputLe.preferredHeight = -1f;
        inputLe.flexibleHeight = 1f;

        GameObject hScrollbarObj = new GameObject("EditorHScrollbar", typeof(RectTransform), typeof(Scrollbar), typeof(Image), typeof(LayoutElement));
        hScrollbarObj.transform.SetParent(editorColumn.transform, false);

        LayoutElement hsbLe = hScrollbarObj.GetComponent<LayoutElement>();
        hsbLe.minHeight = 6f;
        hsbLe.preferredHeight = 6f;
        hsbLe.flexibleHeight = 0f;
        hsbLe.flexibleWidth = 1f;

        Image hsbBg = hScrollbarObj.GetComponent<Image>();
        hsbBg.color = new Color(0.01f, 0.02f, 0.04f, 0.8f);

        GameObject hSlidingArea = new GameObject("SlidingArea", typeof(RectTransform));
        hSlidingArea.transform.SetParent(hScrollbarObj.transform, false);
        RectTransform hsaRT = hSlidingArea.GetComponent<RectTransform>();
        hsaRT.anchorMin = Vector2.zero;
        hsaRT.anchorMax = Vector2.one;
        hsaRT.sizeDelta = Vector2.zero;

        GameObject hHandleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        hHandleObj.transform.SetParent(hSlidingArea.transform, false);
        RectTransform hHandleRT = hHandleObj.GetComponent<RectTransform>();
        hHandleRT.sizeDelta = Vector2.zero;

        _hHandleImg = hHandleObj.GetComponent<Image>();
        _hHandleImg.color = CyberPalette.ColorIceBlue;

        _editorHScrollbar = hScrollbarObj.GetComponent<Scrollbar>();
        _editorHScrollbar.handleRect = hHandleRT;
        _editorHScrollbar.targetGraphic = _hHandleImg;
        _editorHScrollbar.direction = Scrollbar.Direction.LeftToRight;
        _editorHScrollbar.onValueChanged.AddListener(OnHScrollbarValueChanged);
        _hDragTracker = hScrollbarObj.AddComponent<ScrollbarDragTracker>();

        GameObject scrollbarObj = new GameObject("EditorScrollbar", typeof(RectTransform), typeof(Scrollbar), typeof(Image), typeof(LayoutElement));
        scrollbarObj.transform.SetParent(hContainer.transform, false);
        LayoutElement sbLe = scrollbarObj.GetComponent<LayoutElement>();
        sbLe.minWidth = 6f;
        sbLe.preferredWidth = 6f;
        sbLe.flexibleWidth = 0f;
        sbLe.flexibleHeight = 1f;

        Image sbBg = scrollbarObj.GetComponent<Image>();
        sbBg.color = new Color(0.01f, 0.02f, 0.04f, 0.8f);

        GameObject slidingArea = new GameObject("SlidingArea", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbarObj.transform, false);
        RectTransform saRT = slidingArea.GetComponent<RectTransform>();
        saRT.anchorMin = Vector2.zero;
        saRT.anchorMax = Vector2.one;
        saRT.sizeDelta = Vector2.zero;

        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObj.transform.SetParent(slidingArea.transform, false);
        RectTransform handleRT = handleObj.GetComponent<RectTransform>();
        handleRT.sizeDelta = Vector2.zero;

        Image handleImg = handleObj.GetComponent<Image>();
        handleImg.color = CyberPalette.ColorIceBlue;

        _editorScrollbar = scrollbarObj.GetComponent<Scrollbar>();
        _editorScrollbar.handleRect = handleRT;
        _editorScrollbar.targetGraphic = handleImg;
        _editorScrollbar.direction = Scrollbar.Direction.TopToBottom;
        _editorScrollbar.onValueChanged.AddListener(OnScrollbarValueChanged);
        _dragTracker = scrollbarObj.AddComponent<ScrollbarDragTracker>();

        if (_editorInput.textViewport != null)
        {
            EditorScrollHandler taScroll = _editorInput.textViewport.gameObject.AddComponent<EditorScrollHandler>();
            taScroll.VerticalScrollbar = _editorScrollbar;
            taScroll.HorizontalScrollbar = _editorHScrollbar;
            taScroll.Viewport = _editorInput.textViewport;
            taScroll.TextComponent = _editorInput.textComponent;
        }

        EditorScrollHandler rootScroll = inputRoot.AddComponent<EditorScrollHandler>();
        rootScroll.VerticalScrollbar = _editorScrollbar;
        rootScroll.HorizontalScrollbar = _editorHScrollbar;
        rootScroll.Viewport = _editorInput.textViewport;
        rootScroll.TextComponent = _editorInput.textComponent;

        EditorScrollHandler gutterScroll = gutterObj.AddComponent<EditorScrollHandler>();
        gutterScroll.VerticalScrollbar = _editorScrollbar;
        gutterScroll.HorizontalScrollbar = _editorHScrollbar;
        gutterScroll.Viewport = _editorInput.textViewport;
        gutterScroll.TextComponent = _editorInput.textComponent;

        _editorInput.enabled = false;
        _editorInput.enabled = true;

        _editorInput.text = _currentContent;
        _editorInput.onValueChanged.AddListener(OnContentChanged);
        _editorInput.onEndEdit.AddListener(OnContentCommitted);
    }

    private void BuildStatusBar(Transform parent)
    {
        GameObject statusObj = new GameObject("StatusBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        statusObj.transform.SetParent(parent, false);

        RectTransform sbRT = statusObj.GetComponent<RectTransform>();
        sbRT.sizeDelta = new Vector2(0f, 20f);

        LayoutElement sbLe = statusObj.AddComponent<LayoutElement>();
        sbLe.minHeight = 20f;
        sbLe.preferredHeight = 20f;
        sbLe.flexibleHeight = 0f;

        HorizontalLayoutGroup hlg = statusObj.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        _statusFooter = UiFactory.CreateLabel(statusObj.transform, "FooterText", "UTF-8", CyberPalette.ColorTextMuted, 8.5f, TextAlignmentOptions.MidlineLeft);
        LayoutElement footerLe = _statusFooter.gameObject.AddComponent<LayoutElement>();
        footerLe.flexibleWidth = 1f;
        footerLe.flexibleHeight = 1f;

        GameObject badgeObj = UiFactory.CreatePanel(statusObj.transform, "ValidatorBadge", CyberPalette.ColorGlacialMint, CyberPalette.ColorVoidBlack, 1f);
        _validatorBadgeObj = badgeObj;
        _validatorBadgeBorder = badgeObj.GetComponent<Image>();

        LayoutElement badgeLe = badgeObj.AddComponent<LayoutElement>();
        badgeLe.minWidth = 90f;
        badgeLe.preferredWidth = 90f;
        badgeLe.flexibleWidth = 0f;
        badgeLe.minHeight = 18f;
        badgeLe.preferredHeight = 18f;
        badgeLe.flexibleHeight = 0f;

        Transform? badgeFill = badgeObj.transform.Find("Fill");
        Transform targetFill = badgeFill != null ? badgeFill : badgeObj.transform;

        _validatorBadgeText = UiFactory.CreateLabel(targetFill, "BadgeText", "[ VALID JSON ]", CyberPalette.ColorGlacialMint, 8.5f, TextAlignmentOptions.Center);
        RectTransform btRT = _validatorBadgeText.GetComponent<RectTransform>();
        btRT.anchorMin = Vector2.zero;
        btRT.anchorMax = Vector2.one;
        btRT.offsetMin = Vector2.zero;
        btRT.offsetMax = Vector2.zero;

        _validatorTooltip = ButtonTooltipHandler.Attach(badgeObj, "VALIDATOR", "Validating file syntax...");
    }

    private void LateUpdate()
    {
        if (_editorInput == null || _editorInput.textComponent == null)
        {
            return;
        }

        if (_editorInput.isFocused && (_hDragTracker == null || !_hDragTracker.IsDragging))
        {
            EnsureCaretVisible();
        }

        Vector2 targetPos = _editorInput.textComponent.rectTransform.anchoredPosition;

        if (_lineNumbersText != null)
        {
            Vector2 currentGutter = _lineNumbersText.rectTransform.anchoredPosition;
            if (Mathf.Abs(currentGutter.y - targetPos.y) > 0.05f)
            {
                _lineNumbersText.rectTransform.anchoredPosition = new Vector2(currentGutter.x, targetPos.y);
            }
        }

        if (_editorScrollbar != null && (_dragTracker == null || !_dragTracker.IsDragging))
        {
            float textHeight = _editorInput.textComponent.preferredHeight;
            float viewHeight = _editorInput.textViewport != null ? _editorInput.textViewport.rect.height : 100f;

            if (textHeight > viewHeight && viewHeight > 0f)
            {
                float targetVal = (float)(GetScrollPositionMethod?.Invoke(_editorInput, null) ?? 0f);
                float targetSize = Mathf.Clamp(viewHeight / textHeight, 0.08f, 1f);

                _isSyncingScrollbar = true;
                _editorScrollbar.size = targetSize;
                _editorScrollbar.value = targetVal;
                _isSyncingScrollbar = false;
            }
            else
            {
                _isSyncingScrollbar = true;
                _editorScrollbar.size = 1f;
                _editorScrollbar.value = 0f;
                _isSyncingScrollbar = false;
            }
        }

        float textWidth = _editorInput.textComponent.preferredWidth;
        float viewWidth = _editorInput.textViewport != null ? _editorInput.textViewport.rect.width : 100f;
        float scrollableWidth = textWidth - viewWidth + 40f;

        if (scrollableWidth <= 0f && _editorInput.textComponent.rectTransform.anchoredPosition.x != 0f)
        {
            Vector2 cur = _editorInput.textComponent.rectTransform.anchoredPosition;
            _editorInput.textComponent.rectTransform.anchoredPosition = new Vector2(0f, cur.y);
            AssignPositioningMethod?.Invoke(_editorInput, null);
        }
        else if (scrollableWidth > 0f && _editorInput.textComponent.rectTransform.anchoredPosition.x < -scrollableWidth)
        {
            Vector2 cur = _editorInput.textComponent.rectTransform.anchoredPosition;
            _editorInput.textComponent.rectTransform.anchoredPosition = new Vector2(-scrollableWidth, cur.y);
            AssignPositioningMethod?.Invoke(_editorInput, null);
        }

        if (_editorHScrollbar != null && (_hDragTracker == null || !_hDragTracker.IsDragging))
        {
            if (scrollableWidth > 0f && viewWidth > 0f)
            {
                float targetSize = Mathf.Clamp(viewWidth / (textWidth + 40f), 0.08f, 1f);
                float currentScrollX = -_editorInput.textComponent.rectTransform.anchoredPosition.x;
                float targetVal = Mathf.Clamp01(currentScrollX / scrollableWidth);

                _isSyncingHScrollbar = true;
                _editorHScrollbar.size = targetSize;
                _editorHScrollbar.value = targetVal;
                _isSyncingHScrollbar = false;

                if (_hHandleImg != null)
                {
                    _hHandleImg.color = CyberPalette.ColorIceBlue;
                }
            }
            else
            {
                _isSyncingHScrollbar = true;
                _editorHScrollbar.size = 1f;
                _editorHScrollbar.value = 0f;
                _isSyncingHScrollbar = false;

                if (_hHandleImg != null)
                {
                    _hHandleImg.color = Color.clear;
                }
            }
        }
    }

    private void Update()
    {
        if (_editorInput == null || !_editorInput.isFocused)
        {
            return;
        }

        if (_editorInput.stringPosition != _lastCaretPosition)
        {
            _lastCaretPosition = _editorInput.stringPosition;
            UpdateStatusFooter();
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            int caretPos = _editorInput.stringPosition;
            _editorInput.text = _editorInput.text.Insert(caretPos, "  ");
            _editorInput.stringPosition = caretPos + 2;
        }

        bool isCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (isCtrl)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                PerformSave();
            }
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    PerformRedo();
                }
                else
                {
                    PerformUndo();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Y))
            {
                PerformRedo();
            }
            else if (Input.GetKeyDown(KeyCode.F) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                FormatJsonContent();
            }
        }
    }

    private void OnScrollbarValueChanged(float val)
    {
        if (_isSyncingScrollbar || _editorInput == null)
        {
            return;
        }

        AdjustPositionMethod?.Invoke(_editorInput, new object[] { val });
    }

    private void OnHScrollbarValueChanged(float val)
    {
        if (_isSyncingHScrollbar || _editorInput == null || _editorInput.textComponent == null)
        {
            return;
        }

        float textWidth = _editorInput.textComponent.preferredWidth;
        float viewWidth = _editorInput.textViewport != null ? _editorInput.textViewport.rect.width : 100f;
        float scrollableWidth = textWidth - viewWidth + 40f;

        if (scrollableWidth <= 0f)
        {
            return;
        }

        float targetX = -Mathf.Clamp01(val) * scrollableWidth;
        Vector2 curPos = _editorInput.textComponent.rectTransform.anchoredPosition;
        _editorInput.textComponent.rectTransform.anchoredPosition = new Vector2(targetX, curPos.y);
        AssignPositioningMethod?.Invoke(_editorInput, null);
    }

    private void EnsureCaretVisible()
    {
        if (_editorInput == null || !_editorInput.isFocused || _editorInput.textViewport == null || _editorInput.textComponent == null)
        {
            return;
        }

        TMP_TextInfo textInfo = _editorInput.textComponent.textInfo;
        if (textInfo == null || textInfo.characterCount == 0)
        {
            return;
        }

        int caretPos = Mathf.Clamp(_editorInput.stringPosition, 0, textInfo.characterCount);
        float caretX = 0f;
        if (caretPos < textInfo.characterCount)
        {
            caretX = textInfo.characterInfo[caretPos].origin;
        }
        else if (textInfo.characterCount > 0)
        {
            caretX = textInfo.characterInfo[textInfo.characterCount - 1].xAdvance;
        }

        Rect viewRect = _editorInput.textViewport.rect;
        float textX = _editorInput.textComponent.rectTransform.anchoredPosition.x;
        float caretViewX = caretX + textX;

        float margin = 20f;
        float minVisibleX = viewRect.xMin + margin;
        float maxVisibleX = viewRect.xMax - margin;

        if (caretViewX > maxVisibleX)
        {
            float shift = caretViewX - maxVisibleX;
            float newX = textX - shift;
            float textWidth = _editorInput.textComponent.preferredWidth;
            float maxScroll = Mathf.Max(0f, textWidth - viewRect.width + 40f);
            newX = Mathf.Clamp(newX, -maxScroll, 0f);
            _editorInput.textComponent.rectTransform.anchoredPosition = new Vector2(newX, _editorInput.textComponent.rectTransform.anchoredPosition.y);
            AssignPositioningMethod?.Invoke(_editorInput, null);
        }
        else if (caretViewX < minVisibleX)
        {
            float shift = minVisibleX - caretViewX;
            float newX = textX + shift;
            newX = Mathf.Clamp(newX, -Mathf.Max(0f, _editorInput.textComponent.preferredWidth - viewRect.width + 40f), 0f);
            _editorInput.textComponent.rectTransform.anchoredPosition = new Vector2(newX, _editorInput.textComponent.rectTransform.anchoredPosition.y);
            AssignPositioningMethod?.Invoke(_editorInput, null);
        }
    }

    private void OnContentChanged(string newText)
    {
        if (_isApplyingHistory)
        {
            return;
        }

        _undoStack.Push(_currentContent);
        if (_undoStack.Count > MaxUndoSteps)
        {
            _undoStack.TrimExcess();
        }
        _redoStack.Clear();

        _currentContent = newText;
        UpdateDirtyState();
        ScheduleGutterUpdate();
        ValidateJsonIfNeeded();
    }

    private void OnContentCommitted(string newText)
    {
        _currentContent = newText;
        UpdateDirtyState();
        UpdateLineNumbers();
        ValidateJsonIfNeeded();
    }

    private void ScheduleGutterUpdate()
    {
        if (_syncGutterRoutine != null)
        {
            StopCoroutine(_syncGutterRoutine);
        }
        _syncGutterRoutine = StartCoroutine(SyncGutterRoutine());
    }

    private IEnumerator SyncGutterRoutine()
    {
        yield return null;
        UpdateLineNumbers();
        _syncGutterRoutine = null;
    }

    private void UpdateLineNumbers()
    {
        if (_lineNumbersText == null)
        {
            return;
        }

        int lineCount = 1;
        for (int i = 0; i < _currentContent.Length; i++)
        {
            if (_currentContent[i] == '\n')
            {
                lineCount++;
            }
        }

        StringBuilder sb = new StringBuilder(lineCount * 4);
        for (int i = 1; i <= lineCount; i++)
        {
            sb.AppendLine(i.ToString());
        }

        _lineNumbersText.text = sb.ToString();
        UpdateStatusFooter();
    }

    private void UpdateStatusFooter()
    {
        if (_statusFooter == null || _fileItem == null)
        {
            return;
        }

        int lineCount = 1;
        for (int i = 0; i < _currentContent.Length; i++)
        {
            if (_currentContent[i] == '\n')
            {
                lineCount++;
            }
        }

        int caretLine = 1;
        int caretCol = 1;
        if (_editorInput != null)
        {
            int pos = Math.Min(_editorInput.stringPosition, _currentContent.Length);
            for (int i = 0; i < pos; i++)
            {
                if (_currentContent[i] == '\n')
                {
                    caretLine++;
                    caretCol = 1;
                }
                else
                {
                    caretCol++;
                }
            }
        }

        _statusFooter.text = $"UTF-8  |  {_fileItem.RelativePath}  |  Ln {caretLine}, Col {caretCol}  |  Lines: {lineCount}  |  {_fileItem.FormattedSize}";
    }

    private void ValidateJsonIfNeeded()
    {
        if (_validatorBadgeText == null || _validatorBadgeBorder == null || _fileItem == null)
        {
            return;
        }

        if (_fileItem.Extension == ".json")
        {
            JsonValidator.ValidationResult result = JsonValidator.Validate(_currentContent);
            if (result.IsValid)
            {
                _validatorBadgeText.text = "[ VALID JSON ]";
                _validatorBadgeText.color = CyberPalette.ColorGlacialMint;
                _validatorBadgeBorder.color = CyberPalette.ColorGlacialMint;
                if (_validatorTooltip != null)
                {
                    _validatorTooltip.Bind("JSON VALID", "Syntax is well-formed and valid.");
                }
            }
            else
            {
                _validatorBadgeText.text = "[ ! JSON ERR ]";
                _validatorBadgeText.color = CyberPalette.ColorErrorRed;
                _validatorBadgeBorder.color = CyberPalette.ColorErrorRed;
                if (_validatorTooltip != null)
                {
                    _validatorTooltip.Bind("JSON SYNTAX ERROR", $"Line {result.ErrorLine}, Col {result.ErrorColumn}:\n{result.ErrorMessage}");
                }
            }
        }
        else
        {
            string extBadge = $"[ {_fileItem.Extension.ToUpperInvariant().TrimStart('.')} ]";
            _validatorBadgeText.text = extBadge;
            _validatorBadgeText.color = CyberPalette.ColorTextMuted;
            _validatorBadgeBorder.color = CyberPalette.ColorBorderSubtle;
            if (_validatorTooltip != null)
            {
                _validatorTooltip.Bind("CONFIG FILE", $"{_fileItem.FileName}\n{_fileItem.RelativePath}");
            }
        }
    }

    private void UpdateDirtyState()
    {
        bool dirty = IsDirty;

        if (_dirtyIndicator != null)
        {
            _dirtyIndicator.gameObject.SetActive(dirty);
        }

        if (_saveBtnBorder != null && _saveBtnIcon != null)
        {
            _saveBtnBorder.color = dirty ? CyberPalette.ColorIceBlueBright : CyberPalette.ColorBorderSubtle;
            _saveBtnIcon.color = dirty ? CyberPalette.ColorIceBlueBright : CyberPalette.ColorTextMuted;
        }
    }

    private void PerformUndo()
    {
        if (_undoStack.Count == 0 || _editorInput == null)
        {
            return;
        }

        _isApplyingHistory = true;
        _redoStack.Push(_currentContent);
        _currentContent = _undoStack.Pop();
        _editorInput.text = _currentContent;
        _isApplyingHistory = false;

        UpdateDirtyState();
        UpdateLineNumbers();
        ValidateJsonIfNeeded();
    }

    private void PerformRedo()
    {
        if (_redoStack.Count == 0 || _editorInput == null)
        {
            return;
        }

        _isApplyingHistory = true;
        _undoStack.Push(_currentContent);
        _currentContent = _redoStack.Pop();
        _editorInput.text = _currentContent;
        _isApplyingHistory = false;

        UpdateDirtyState();
        UpdateLineNumbers();
        ValidateJsonIfNeeded();
    }

    private void FormatJsonContent()
    {
        if (_fileItem == null || _fileItem.Extension != ".json" || _editorInput == null)
        {
            return;
        }

        JsonValidator.ValidationResult result = JsonValidator.Validate(_currentContent);
        if (!result.IsValid)
        {
            return;
        }

        string formatted = JsonValidator.FormatJson(_currentContent);
        if (formatted != _currentContent)
        {
            _undoStack.Push(_currentContent);
            _redoStack.Clear();
            _currentContent = formatted;
            _editorInput.text = formatted;
            UpdateDirtyState();
            UpdateLineNumbers();
            ValidateJsonIfNeeded();
        }
    }

    public void PerformSave()
    {
        if (!IsDirty || _fileItem == null)
        {
            return;
        }

        if (_fileItem.Extension == ".json")
        {
            JsonValidator.ValidationResult result = JsonValidator.Validate(_currentContent);
            if (!result.IsValid && ConfirmationModal.Instance != null)
            {
                ConfirmationModal.Instance.Show(
                    "[ INVALID JSON WARNING ]",
                    $"The JSON syntax contains errors (Line {result.ErrorLine}):\n{result.ErrorMessage}\n\nSaving invalid JSON may break mod functionality. Save anyway?",
                    "Save Anyway",
                    ForceSaveFile,
                    "Cancel"
                );
                return;
            }
        }

        ForceSaveFile();
    }

    private void ForceSaveFile()
    {
        if (_fileItem == null)
        {
            return;
        }

        try
        {
            ConfigFileManager.Instance.SaveFileText(_fileItem, _currentContent);
            _originalContent = _currentContent;
            UpdateDirtyState();

            if (_saveBtnBorder != null && _saveBtnIcon != null)
            {
                _saveBtnBorder.color = CyberPalette.ColorGlacialMint;
                _saveBtnIcon.color = CyberPalette.ColorGlacialMint;
                StartCoroutine(ResetSaveButtonStateRoutine());
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ConfigDrawers] Failed to save {_fileItem.FullPath}: {ex}");
            if (ConfirmationModal.Instance != null)
            {
                ConfirmationModal.Instance.Show(
                    "[ SAVE ERROR ]",
                    $"Failed to write file to disk:\n{ex.Message}",
                    "OK",
                    () => { }
                );
            }
        }
    }

    private IEnumerator ResetSaveButtonStateRoutine()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        UpdateDirtyState();
    }

    private void HandleRevertClicked()
    {
        if (!IsDirty)
        {
            return;
        }

        if (ConfirmationModal.Instance != null)
        {
            ConfirmationModal.Instance.Show(
                "[ REVERT CHANGES ]",
                $"Discard all unsaved changes and reload '{_fileItem?.FileName}' from disk?",
                "Revert",
                () =>
                {
                    _undoStack.Push(_currentContent);
                    _redoStack.Clear();
                    _currentContent = _originalContent;
                    if (_editorInput != null)
                    {
                        _editorInput.text = _originalContent;
                    }
                    UpdateDirtyState();
                    UpdateLineNumbers();
                    ValidateJsonIfNeeded();
                },
                "Cancel"
            );
        }
    }

    public void HandleBackClicked()
    {
        if (IsDirty && ConfirmationModal.Instance != null)
        {
            ConfirmationModal.Instance.Show(
                "[ UNSAVED CHANGES ]",
                $"You have unsaved changes in '{_fileItem?.FileName}'. Do you want to save your changes before exiting?",
                "Discard",
                () => _onBackRequested?.Invoke(),
                "Cancel",
                null,
                "Save & Exit",
                () =>
                {
                    ForceSaveFile();
                    _onBackRequested?.Invoke();
                }
            );
            return;
        }

        _onBackRequested?.Invoke();
    }

    private void OnTextPreRender(TMP_TextInfo textInfo)
    {
        if (_fileItem == null || textInfo == null || textInfo.characterCount == 0)
        {
            return;
        }

        string content = _editorInput != null ? _editorInput.text : _currentContent;
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        ConfigFileSyntaxHighlighter.ApplyHighlightingToTextInfo(textInfo, content, _fileItem.Extension);
    }

    private void OnDestroy()
    {
        if (_editorInput != null && _editorInput.textComponent != null)
        {
            _editorInput.textComponent.OnPreRenderText -= OnTextPreRender;
        }
    }
}
