using BepInEx.ConfigDrawers.Configuration;
using BepInEx.ConfigDrawers.Drawers;
using BepInEx.ConfigDrawers.Files;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
using BepInEx.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BepInEx.ConfigDrawers.Components;

public class ConfigDrawerWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum MainViewMode
    {
        Plugins,
        Files
    }

    public static ConfigDrawerWindow? Instance { get; private set; }

    public bool IsVisible { get; private set; }
    public float SettingsColumnWidth => 350f;

    private Canvas? _canvas;
    private CanvasScaler? _canvasScaler;
    private GraphicRaycaster? _graphicRaycaster;
    private RectTransform? _drawerRootRT;
    private CanvasGroup? _windowCanvasGroup;
    private GameObject? _resizeHandleObj;
    private Transform? _contentContainer;
    private TMP_InputField? _searchField;
    private GameObject? _clearSearchBtn;
    private Coroutine? _searchDebounceRoutine;
    private const float SearchDebounceSeconds = 0.15f;
    private TextMeshProUGUI? _rebindButtonText;
    private TextMeshProUGUI? _fontSizeButtonText;
    private TextMeshProUGUI? _shortcutHintLabel;
    private GameObject? _advancedToggleBtn;
    private TextMeshProUGUI? _advancedButtonText;
    private GameObject? _dockLeftBtn;
    private GameObject? _dockRightBtn;
    private GameObject? _dockFloatBtn;

    private DockPosition _currentDock = DockPosition.Left;
    private Vector2 _floatingPosition = Vector2.zero;
    private bool _isRecordingKeybind;
    private PluginSettingsGroup? _activePlugin;
    private readonly HashSet<string> _expandedCategories = new();

    private MainViewMode _currentViewMode = MainViewMode.Plugins;
    private ConfigFileFilter _currentFileFilter = ConfigFileFilter.All;
    private ConfigFileItem? _activeFile;
    private ConfigFileEditor? _activeFileEditor;

    private GameObject? _eventSystemObj;
    private EventSystem? _dormantEventSystem;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeEventSystem();
    }

    private void EnsureWindowBuilt()
    {
        if (_canvas != null)
        {
            return;
        }

        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999;

        _canvasScaler = gameObject.AddComponent<CanvasScaler>();
        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        _canvasScaler.matchWidthOrHeight = 0.5f;

        _graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();

        GameObject drawerObj = new GameObject("DrawerRoot", typeof(RectTransform), typeof(Image));
        drawerObj.transform.SetParent(transform, false);

        Image bgImg = drawerObj.GetComponent<Image>();
        bgImg.color = CyberPalette.ColorVoidBlack;

        _drawerRootRT = drawerObj.GetComponent<RectTransform>();
        _windowCanvasGroup = drawerObj.AddComponent<CanvasGroup>();
        UpdateWindowOpacity();

        UiFactory.AddBorderOutline(drawerObj.transform, CyberPalette.ColorIceBlue, 1f);

        BuildResizeHandle(drawerObj.transform);
        ApplyDockPosition(ConfigDrawerConfig.DefaultDockPosition.Value);

        Transform container = drawerObj.transform;

        BuildHeader(container);
        BuildSearchBar(container);
        BuildContentArea(container);
        BuildFooter(container);
        ConfirmationModal.Attach(drawerObj);

        ConfigDrawerConfig.ToggleKeybind.SettingChanged += (_, _) =>
        {
            UpdateKeybindDisplays();
        };

        ConfigDrawerConfig.HideAdvancedByDefault.SettingChanged += (_, _) =>
        {
            UpdateAdvancedButton();
        };

        ConfigDrawerConfig.WindowOpacity.SettingChanged += (_, _) =>
        {
            UpdateWindowOpacity();
        };

        UiFactory.RefreshAllFonts(gameObject);
    }

    private void BuildResizeHandle(Transform parent)
    {
        _resizeHandleObj = UiFactory.CreatePanel(parent, "ResizeHandle", CyberPalette.ColorBorderSubtle, CyberPalette.ColorCardSurface, 1f);
        var rt = _resizeHandleObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(16f, 16f);
        rt.anchoredPosition = Vector2.zero;

        var fill = _resizeHandleObj.transform.Find("Fill");
        var target = fill != null ? fill : _resizeHandleObj.transform;

        var gripLabel = UiFactory.CreateLabel(target, "Grip", "::", CyberPalette.ColorIceBlueBright, 9f, TextAlignmentOptions.Center);
        var gripRT = gripLabel.GetComponent<RectTransform>();
        gripRT.anchorMin = Vector2.zero;
        gripRT.anchorMax = Vector2.one;
        gripRT.offsetMin = Vector2.zero;
        gripRT.offsetMax = Vector2.zero;

        var resizer = _resizeHandleObj.AddComponent<WindowResizeHandler>();
        if (_drawerRootRT != null)
        {
            resizer.SetTarget(_drawerRootRT);
        }
    }

    private void InitializeEventSystem()
    {
        _eventSystemObj = new GameObject("ConfigDrawer_EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(_eventSystemObj);
        _eventSystemObj.SetActive(false);
    }

    private void ActivateEventSystem()
    {
        if (_eventSystemObj == null)
        {
            InitializeEventSystem();
        }

        EventSystem? active = EventSystem.current;
        if (active == null)
        {
            EventSystem[] all = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].gameObject != _eventSystemObj && all[i].enabled)
                {
                    active = all[i];
                    break;
                }
            }
        }

        if (active != null && active.gameObject != _eventSystemObj)
        {
            _dormantEventSystem = active;
            _dormantEventSystem.enabled = false;
        }

        if (_eventSystemObj != null)
        {
            _eventSystemObj.SetActive(true);
        }
    }

    private void DeactivateEventSystem()
    {
        if (_eventSystemObj != null)
        {
            _eventSystemObj.SetActive(false);
        }

        if (_dormantEventSystem != null)
        {
            _dormantEventSystem.enabled = true;
            _dormantEventSystem = null;
        }
    }

    private void BuildHeader(Transform parent)
    {
        var headerObj = UiFactory.CreatePanel(parent, "Header", CyberPalette.ColorBorderSubtle, CyberPalette.ColorCardSurface, 1f);
        var headerRT = headerObj.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.anchoredPosition = new Vector2(0f, -4f);
        headerRT.sizeDelta = new Vector2(-10f, 36f);

        var fill = headerObj.transform.Find("Fill");
        var target = fill != null ? fill : headerObj.transform;

        var titleArea = new GameObject("TitleArea", typeof(RectTransform));
        titleArea.transform.SetParent(target, false);
        var titleRT = titleArea.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0f);
        titleRT.anchorMax = new Vector2(0.24f, 1f);
        titleRT.offsetMin = new Vector2(8f, 0f);
        titleRT.offsetMax = Vector2.zero;

        var title = UiFactory.CreateLabel(titleArea.transform, "Title", "<b>CONFIG DRAWERS</b>", CyberPalette.ColorIceBlueBright, 11f, TextAlignmentOptions.MidlineLeft);
        var labelRT = title.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        var controlsRow = new GameObject("Controls", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        controlsRow.transform.SetParent(target, false);
        var controlsRT = controlsRow.GetComponent<RectTransform>();
        controlsRT.anchorMin = new Vector2(0.24f, 0f);
        controlsRT.anchorMax = new Vector2(1f, 1f);
        controlsRT.offsetMin = Vector2.zero;
        controlsRT.offsetMax = new Vector2(-4f, 0f);

        var hlg = controlsRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 3f;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        var rebindBtn = UiFactory.CreateCyberButton(controlsRow.transform, "RebindBtn", GetShortcutButtonText(), OnStartRebindClicked, CyberPalette.ColorIceBlue, CyberPalette.ColorIceBlueBright, 60f, 22f);
        _rebindButtonText = rebindBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (_rebindButtonText != null)
        {
            _rebindButtonText.enableAutoSizing = true;
            _rebindButtonText.fontSizeMin = 7.5f;
            _rebindButtonText.fontSizeMax = 10f;
        }
        ButtonTooltipHandler.Attach(rebindBtn, "[ TOGGLE HOTKEY ]", "Click to set a new key to open and close this window.");

        var fontBtn = UiFactory.CreateCyberButton(controlsRow.transform, "FontBtn", GetFontSizeLabel(), OnCycleFontSize, CyberPalette.ColorBorderSubtle, CyberPalette.ColorIceBlue, 74f, 22f);
        _fontSizeButtonText = fontBtn.GetComponentInChildren<TextMeshProUGUI>();
        ButtonTooltipHandler.Attach(fontBtn, "[ UI SCALE ]", "Cycle font size and spacing presets (Small, Normal, Large).");

        _advancedToggleBtn = UiFactory.CreateCyberButton(controlsRow.transform, "AdvBtn", GetAdvancedButtonText(), OnToggleAdvancedSettings, CyberPalette.ColorBorderSubtle, CyberPalette.ColorTextMuted, 56f, 22f);
        _advancedButtonText = _advancedToggleBtn.GetComponentInChildren<TextMeshProUGUI>();
        ButtonTooltipHandler.Attach(_advancedToggleBtn, "[ ADVANCED SETTINGS ]", "Toggle visibility of advanced configuration settings.");
        UpdateAdvancedButton();

        _dockLeftBtn = UiFactory.CreateCyberButton(controlsRow.transform, "DockLeftBtn", "Left", () => ApplyDockPosition(DockPosition.Left), CyberPalette.ColorBorderSubtle, CyberPalette.ColorTextMain, 36f, 22f);
        ButtonTooltipHandler.Attach(_dockLeftBtn, "[ DOCK LEFT ]", "Dock and pin the window to the left side of the screen.");

        _dockRightBtn = UiFactory.CreateCyberButton(controlsRow.transform, "DockRightBtn", "Right", () => ApplyDockPosition(DockPosition.Right), CyberPalette.ColorBorderSubtle, CyberPalette.ColorTextMain, 40f, 22f);
        ButtonTooltipHandler.Attach(_dockRightBtn, "[ DOCK RIGHT ]", "Dock and pin the window to the right side of the screen.");

        _dockFloatBtn = UiFactory.CreateCyberButton(controlsRow.transform, "DetachBtn", "Float", () => ApplyDockPosition(DockPosition.Floating), CyberPalette.ColorBorderSubtle, CyberPalette.ColorTextMain, 40f, 22f);
        ButtonTooltipHandler.Attach(_dockFloatBtn, "[ FLOAT WINDOW ]", "Detach and float the window. Freely drag anywhere.");

        UpdateDockButtons(_currentDock);

        var closeBtn = UiFactory.CreateCyberButton(controlsRow.transform, "CloseBtn", "X", () => SetVisible(false), CyberPalette.ColorWarningAmber, CyberPalette.ColorTextMain, 24f, 22f);
        ButtonTooltipHandler.Attach(closeBtn, "[ CLOSE ]", "Close the Config Drawers window.");
    }

    private string GetAdvancedButtonText()
    {
        return ConfigDrawerConfig.HideAdvancedByDefault.Value ? "Adv: Off" : "Adv: On";
    }

    private void OnToggleAdvancedSettings()
    {
        bool next = !ConfigDrawerConfig.HideAdvancedByDefault.Value;
        ConfigDrawerConfig.HideAdvancedByDefault.Value = next;
        UpdateAdvancedButton();
        RefreshCurrentView();
    }

    private void UpdateAdvancedButton()
    {
        if (_advancedButtonText != null)
        {
            _advancedButtonText.text = GetAdvancedButtonText();
            _advancedButtonText.color = ConfigDrawerConfig.HideAdvancedByDefault.Value
                ? CyberPalette.ColorTextMuted
                : CyberPalette.ColorIceBlueBright;
        }

        if (_advancedToggleBtn != null)
        {
            Image? img = _advancedToggleBtn.GetComponent<Image>();
            if (img != null)
            {
                img.color = ConfigDrawerConfig.HideAdvancedByDefault.Value
                    ? CyberPalette.ColorBorderSubtle
                    : CyberPalette.ColorIceBlue;
            }
        }
    }

    private void UpdateWindowOpacity()
    {
        if (_windowCanvasGroup != null)
        {
            float opacity = Mathf.Clamp(ConfigDrawerConfig.WindowOpacity.Value, 0.20f, 1.0f);
            _windowCanvasGroup.alpha = opacity;
        }
    }

    private string GetFontSizeLabel()
    {
        return ConfigDrawerConfig.UiFontSize.Value switch
        {
            FontSizeScale.Small => "Size: Sml",
            FontSizeScale.Large => "Size: Lrg",
            _ => "Size: Norm"
        };
    }

    private void OnCycleFontSize()
    {
        var next = (FontSizeScale)(((int)ConfigDrawerConfig.UiFontSize.Value + 1) % 3);
        ConfigDrawerConfig.UiFontSize.Value = next;
        if (_fontSizeButtonText != null)
        {
            _fontSizeButtonText.text = GetFontSizeLabel();
        }
        RefreshCurrentView();
    }

    private void BuildSearchBar(Transform parent)
    {
        GameObject searchContainer = new GameObject("SearchContainer", typeof(RectTransform));
        searchContainer.transform.SetParent(parent, false);
        RectTransform searchRT = searchContainer.GetComponent<RectTransform>();
        searchRT.anchorMin = new Vector2(0f, 1f);
        searchRT.anchorMax = new Vector2(1f, 1f);
        searchRT.pivot = new Vector2(0.5f, 1f);
        searchRT.anchoredPosition = new Vector2(0f, -42f);
        searchRT.sizeDelta = new Vector2(-10f, 26f);

        (GameObject inputObj, TMP_InputField input) = UiFactory.CreateInputField(searchContainer.transform, "SearchInput", string.Empty, OnSearchCommitted, -1f, 26f, "Search settings or mods...");
        _searchField = input;
        input.onValueChanged.AddListener(OnSearchValueChanged);

        RectTransform inputRT = inputObj.GetComponent<RectTransform>();
        inputRT.anchorMin = Vector2.zero;
        inputRT.anchorMax = Vector2.one;
        inputRT.offsetMin = Vector2.zero;
        inputRT.offsetMax = new Vector2(-26f, 0f);

        _clearSearchBtn = UiFactory.CreateCyberButton(searchContainer.transform, "ClearSearchBtn", "✕", () =>
        {
            if (_searchField != null)
            {
                _searchField.text = string.Empty;
                OnSearchCommitted(string.Empty);
                _searchField.ActivateInputField();
            }
        }, CyberPalette.ColorBorderSubtle, CyberPalette.ColorTextMuted, 20f, 20f);

        RectTransform clearRT = _clearSearchBtn.GetComponent<RectTransform>();
        clearRT.anchorMin = new Vector2(1f, 0.5f);
        clearRT.anchorMax = new Vector2(1f, 0.5f);
        clearRT.pivot = new Vector2(1f, 0.5f);
        clearRT.anchoredPosition = new Vector2(-3f, 0f);
        clearRT.sizeDelta = new Vector2(20f, 20f);

        _clearSearchBtn.SetActive(false);
    }

    private void BuildContentArea(Transform parent)
    {
        var contentArea = new GameObject("ContentArea", typeof(RectTransform));
        contentArea.transform.SetParent(parent, false);
        var contentRT = contentArea.GetComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = new Vector2(5f, 30f);
        contentRT.offsetMax = new Vector2(-5f, -72f);

        _contentContainer = contentArea.transform;
    }

    private void BuildFooter(Transform parent)
    {
        var footerObj = UiFactory.CreatePanel(parent, "Footer", CyberPalette.ColorBorderSubtle, CyberPalette.ColorCardSurface, 1f);
        var footerRT = footerObj.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0f, 0f);
        footerRT.anchorMax = new Vector2(1f, 0f);
        footerRT.pivot = new Vector2(0.5f, 0f);
        footerRT.anchoredPosition = new Vector2(0f, 4f);
        footerRT.sizeDelta = new Vector2(-10f, 22f);

        var fill = footerObj.transform.Find("Fill");
        var target = fill != null ? fill : footerObj.transform;

        var titleLabel = UiFactory.CreateLabel(target, "Brand", $"BEPINEX CONFIG DRAWERS  v{ConfigDrawers.ModVersion}", CyberPalette.ColorCyberTeal, 9f, TextAlignmentOptions.MidlineLeft);
        var titleRT = titleLabel.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0f);
        titleRT.anchorMax = new Vector2(0.6f, 1f);
        titleRT.offsetMin = new Vector2(8f, 0f);
        titleRT.offsetMax = Vector2.zero;

        _shortcutHintLabel = UiFactory.CreateLabel(target, "ShortcutHint", GetShortcutHintText(), CyberPalette.ColorTextMuted, 9f, TextAlignmentOptions.MidlineRight);
        var scRT = _shortcutHintLabel.GetComponent<RectTransform>();
        scRT.anchorMin = new Vector2(0.6f, 0f);
        scRT.anchorMax = new Vector2(1f, 1f);
        scRT.offsetMin = Vector2.zero;
        scRT.offsetMax = new Vector2(-8f, 0f);
    }

    private GameObject CreateScrollArea(Transform parent, out Transform listContainer, float topOffset = 28f)
    {
        var scrollObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(parent, false);
        var scrollRT = scrollObj.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = new Vector2(0f, -topOffset);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        viewport.transform.SetParent(scrollObj.transform, false);
        var vpImg = viewport.GetComponent<Image>();
        vpImg.color = Color.clear;
        vpImg.raycastTarget = true;
        var vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = new Vector2(-8f, 0f);

        GameObject listObj = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        listObj.transform.SetParent(viewport.transform, false);
        RectTransform listRT = listObj.GetComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0f, 1f);
        listRT.anchorMax = new Vector2(1f, 1f);
        listRT.pivot = new Vector2(0.5f, 1f);
        listRT.offsetMin = Vector2.zero;
        listRT.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = listObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 3f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = listObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject scrollbarObj = new GameObject("Scrollbar", typeof(RectTransform), typeof(Scrollbar), typeof(Image));
        scrollbarObj.transform.SetParent(scrollObj.transform, false);
        RectTransform sbRT = scrollbarObj.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1f, 0f);
        sbRT.anchorMax = new Vector2(1f, 1f);
        sbRT.pivot = new Vector2(1f, 0.5f);
        sbRT.sizeDelta = new Vector2(5f, 0f);
        sbRT.anchoredPosition = Vector2.zero;

        Image sbImg = scrollbarObj.GetComponent<Image>();
        sbImg.color = new Color(0.02f, 0.05f, 0.08f, 0.8f);

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

        Scrollbar scrollbar = scrollbarObj.GetComponent<Scrollbar>();
        scrollbar.handleRect = handleRT;
        scrollbar.targetGraphic = handleImg;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.content = listRT;
        scrollRect.viewport = vpRT;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 35f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        listContainer = listObj.transform;
        return scrollObj;
    }

    public void SetVisible(bool visible)
    {
        IsVisible = visible;

        if (visible)
        {
            EnsureWindowBuilt();
            if (_canvas != null)
            {
                _canvas.enabled = true;
            }
            if (_graphicRaycaster != null)
            {
                _graphicRaycaster.enabled = true;
            }

            ActivateEventSystem();
            if (_drawerRootRT != null)
            {
                _drawerRootRT.gameObject.SetActive(true);
            }

            ConfigRegistry.Instance.Refresh();
            ConfigFileManager.Instance.Refresh();

            if (_activeFileEditor != null && _activeFile != null)
            {
                ShowFileEditor(_activeFile);
            }
            else if (_activePlugin != null)
            {
                ShowPluginSettings(_activePlugin);
            }
            else if (_currentViewMode == MainViewMode.Files)
            {
                PopulateFiles();
            }
            else
            {
                PopulatePlugins();
            }

            UnlockCursor();
            UiFactory.RefreshAllFonts(gameObject);
        }
        else
        {
            if (_activeFileEditor != null && _activeFileEditor.IsDirty)
            {
                ConfirmationModal.Instance?.Show(
                    "[ UNSAVED CHANGES ]",
                    $"You have unsaved changes in '{_activeFileEditor.CurrentFile?.FileName}'. Do you want to save before closing?",
                    "Discard",
                    () =>
                    {
                        _activeFile = null;
                        _activeFileEditor = null;
                        SetVisible(false);
                    },
                    "Cancel",
                    null,
                    "Save & Close",
                    () =>
                    {
                        _activeFileEditor.PerformSave();
                        _activeFile = null;
                        _activeFileEditor = null;
                        SetVisible(false);
                    }
                );
                return;
            }

            if (_drawerRootRT != null)
            {
                _drawerRootRT.gameObject.SetActive(false);
            }

            HoverCardHandler.HideCard();
            StatusIconTooltipHandler.HideTooltip();
            ButtonTooltipHandler.HideTooltip();
            ConfirmationModal.Instance?.Hide();
            if (_canvas != null)
            {
                _canvas.enabled = false;
            }
            if (_graphicRaycaster != null)
            {
                _graphicRaycaster.enabled = false;
            }
            DeactivateEventSystem();
        }
    }

    public void Toggle()
    {
        SetVisible(!IsVisible);
    }

    private void BuildModeSelector(Transform parent, int pluginCount, int fileCount)
    {
        GameObject modeRow = new GameObject("ModeSelectorRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        modeRow.transform.SetParent(parent, false);

        RectTransform modeRT = modeRow.GetComponent<RectTransform>();
        modeRT.anchorMin = new Vector2(0f, 1f);
        modeRT.anchorMax = new Vector2(1f, 1f);
        modeRT.pivot = new Vector2(0.5f, 1f);
        modeRT.anchoredPosition = Vector2.zero;
        modeRT.sizeDelta = new Vector2(0f, 24f);

        HorizontalLayoutGroup hlg = modeRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;

        bool isPlugins = _currentViewMode == MainViewMode.Plugins;

        UiFactory.CreateCyberButton(modeRow.transform, "PluginsModeBtn", $"Plugins Loaded ({pluginCount})", () =>
        {
            if (_currentViewMode != MainViewMode.Plugins)
            {
                SwitchViewMode(MainViewMode.Plugins);
            }
        }, isPlugins ? CyberPalette.ColorIceBlueBright : CyberPalette.ColorBorderSubtle,
           isPlugins ? CyberPalette.ColorIceBlueBright : CyberPalette.ColorTextMuted);

        UiFactory.CreateCyberButton(modeRow.transform, "FilesModeBtn", $"Files Found ({fileCount})", () =>
        {
            if (_currentViewMode != MainViewMode.Files)
            {
                SwitchViewMode(MainViewMode.Files);
            }
        }, !isPlugins ? CyberPalette.ColorIceBlueBright : CyberPalette.ColorBorderSubtle,
           !isPlugins ? CyberPalette.ColorIceBlueBright : CyberPalette.ColorTextMuted);
    }

    private void SwitchViewMode(MainViewMode mode)
    {
        if (_activeFileEditor != null && _activeFileEditor.IsDirty)
        {
            ConfirmationModal.Instance?.Show(
                "[ UNSAVED CHANGES ]",
                $"You have unsaved changes in '{_activeFileEditor.CurrentFile?.FileName}'. Do you want to save before switching views?",
                "Discard",
                () =>
                {
                    _activeFile = null;
                    _activeFileEditor = null;
                    _currentViewMode = mode;
                    UpdateSearchPlaceholder();
                    RefreshCurrentView();
                },
                "Cancel",
                null,
                "Save & Switch",
                () =>
                {
                    _activeFileEditor.PerformSave();
                    _activeFile = null;
                    _activeFileEditor = null;
                    _currentViewMode = mode;
                    UpdateSearchPlaceholder();
                    RefreshCurrentView();
                }
            );
            return;
        }

        _activeFile = null;
        _activeFileEditor = null;
        _activePlugin = null;
        _currentViewMode = mode;
        UpdateSearchPlaceholder();
        RefreshCurrentView();
    }

    private void RefreshCurrentView()
    {
        if (_activeFileEditor != null && _activeFile != null)
        {
            ShowFileEditor(_activeFile);
        }
        else if (_activePlugin != null)
        {
            ShowPluginSettings(_activePlugin);
        }
        else if (_currentViewMode == MainViewMode.Files)
        {
            PopulateFiles();
        }
        else
        {
            PopulatePlugins();
        }
    }

    private void UpdateSearchPlaceholder()
    {
        if (_searchField != null && _searchField.placeholder is TextMeshProUGUI placeholderTmp)
        {
            placeholderTmp.text = _currentViewMode == MainViewMode.Files
                ? "Search config files or paths..."
                : "Search settings or mods...";
        }
    }

    private void BuildFileFilterRow(Transform parent)
    {
        GameObject filterRow = new GameObject("FilterRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        filterRow.transform.SetParent(parent, false);

        RectTransform filterRT = filterRow.GetComponent<RectTransform>();
        filterRT.anchorMin = new Vector2(0f, 1f);
        filterRT.anchorMax = new Vector2(1f, 1f);
        filterRT.pivot = new Vector2(0.5f, 1f);
        filterRT.anchoredPosition = new Vector2(0f, -27f);
        filterRT.sizeDelta = new Vector2(0f, 22f);

        HorizontalLayoutGroup hlg = filterRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 5f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;

        ConfigFileFilter[] filters = {
            ConfigFileFilter.All,
            ConfigFileFilter.Cfg,
            ConfigFileFilter.Json,
            ConfigFileFilter.Yaml,
            ConfigFileFilter.Other
        };

        for (int i = 0; i < filters.Length; i++)
        {
            ConfigFileFilter f = filters[i];
            int count = ConfigFileManager.Instance.GetFilterCount(f);
            string label = f switch
            {
                ConfigFileFilter.All => $"ALL ({count})",
                ConfigFileFilter.Cfg => $".CFG ({count})",
                ConfigFileFilter.Json => $".JSON ({count})",
                ConfigFileFilter.Yaml => $".YAML ({count})",
                ConfigFileFilter.Other => $"OTHER ({count})",
                _ => f.ToString()
            };

            bool active = _currentFileFilter == f;
            UiFactory.CreateCyberButton(filterRow.transform, $"Filter_{f}", label, () =>
            {
                _currentFileFilter = f;
                PopulateFiles();
            }, active ? CyberPalette.ColorGlacialMint : CyberPalette.ColorBorderSubtle,
               active ? CyberPalette.ColorGlacialMint : CyberPalette.ColorTextMuted);
        }
    }

    private void PopulateFiles()
    {
        _activePlugin = null;
        _activeFile = null;
        if (_activeFileEditor != null)
        {
            Destroy(_activeFileEditor.gameObject);
            _activeFileEditor = null;
        }

        if (_contentContainer == null)
        {
            return;
        }

        foreach (Transform child in _contentContainer)
        {
            Destroy(child.gameObject);
        }

        int pluginCount = ConfigRegistry.Instance.Plugins.Count;
        int fileCount = ConfigFileManager.Instance.GetAllFiles().Count;

        BuildModeSelector(_contentContainer, pluginCount, fileCount);
        BuildFileFilterRow(_contentContainer);

        CreateScrollArea(_contentContainer, out Transform listContainer, topOffset: 54f);

        string query = _searchField?.text ?? string.Empty;
        List<ConfigFileItem> files = ConfigFileManager.Instance.FilterFiles(_currentFileFilter, query);

        if (files.Count == 0)
        {
            TextMeshProUGUI emptyLabel = UiFactory.CreateLabel(listContainer, "EmptyFilesLabel", "No configuration files found matching current filter.", CyberPalette.ColorTextMuted, 10f, TextAlignmentOptions.Center);
            LayoutElement ele = emptyLabel.gameObject.AddComponent<LayoutElement>();
            ele.minHeight = 60f;
            return;
        }

        for (int i = 0; i < files.Count; i++)
        {
            ConfigFileCard.Create(listContainer, files[i], ShowFileEditor);
        }
    }

    private void ShowFileEditor(ConfigFileItem fileItem)
    {
        _activePlugin = null;
        _activeFile = fileItem;

        if (_activeFileEditor != null)
        {
            Destroy(_activeFileEditor.gameObject);
            _activeFileEditor = null;
        }

        if (_contentContainer == null)
        {
            return;
        }

        foreach (Transform child in _contentContainer)
        {
            Destroy(child.gameObject);
        }

        GameObject editorHost = new GameObject("FileEditorHost", typeof(RectTransform));
        editorHost.transform.SetParent(_contentContainer, false);
        RectTransform ehRT = editorHost.GetComponent<RectTransform>();
        ehRT.anchorMin = Vector2.zero;
        ehRT.anchorMax = Vector2.one;
        ehRT.offsetMin = Vector2.zero;
        ehRT.offsetMax = Vector2.zero;

        _activeFileEditor = editorHost.AddComponent<ConfigFileEditor>();
        _activeFileEditor.OpenFile(editorHost.transform, fileItem, () =>
        {
            _activeFile = null;
            _activeFileEditor = null;
            PopulateFiles();
        });
    }

    private void PopulatePlugins()
    {
        _activePlugin = null;
        _activeFile = null;
        if (_activeFileEditor != null)
        {
            Destroy(_activeFileEditor.gameObject);
            _activeFileEditor = null;
        }

        if (_contentContainer == null)
        {
            return;
        }

        foreach (Transform child in _contentContainer)
        {
            Destroy(child.gameObject);
        }

        string query = _searchField?.text ?? string.Empty;
        List<PluginSettingsGroup> plugins = ConfigRegistry.Instance.SearchPlugins(query, !ConfigDrawerConfig.HideAdvancedByDefault.Value).ToList();
        int fileCount = ConfigFileManager.Instance.GetAllFiles().Count;

        BuildModeSelector(_contentContainer, plugins.Count, fileCount);

        CreateScrollArea(_contentContainer, out Transform listContainer, topOffset: 28f);

        for (int i = 0; i < plugins.Count; i++)
        {
            RenderPluginCard(listContainer, plugins[i]);
        }
    }

    private void RenderPluginCard(Transform parent, PluginSettingsGroup plugin)
    {
        var card = UiFactory.CreatePanel(parent, $"Plugin_{plugin.ModGuid}", CyberPalette.ColorBorderCard, CyberPalette.ColorCardSurface, 1f);
        var layout = card.AddComponent<LayoutElement>();
        layout.minHeight = 36f;
        layout.preferredHeight = 36f;
        layout.flexibleHeight = 0f;
        layout.flexibleWidth = 1f;

        var borderImg = card.GetComponent<Image>();
        var fill = card.transform.Find("Fill");
        var target = fill != null ? fill : card.transform;
        var fillImg = fill != null ? fill.GetComponent<Image>() : null;

        var hover = card.AddComponent<CyberHoverHandler>();
        hover.Init(borderImg, CyberPalette.ColorBorderCard, CyberPalette.ColorIceBlueBright, fillImg, CyberPalette.ColorCardSurface, new Color(0.07f, 0.12f, 0.18f));

        Action openPlugin = () =>
        {
            _expandedCategories.Clear();
            ShowPluginSettings(plugin);
        };

        var cardClick = card.AddComponent<ClickableBarHandler>();
        cardClick.OnClick = openPlugin;

        if (fill != null)
        {
            var fillClick = fill.gameObject.AddComponent<ClickableBarHandler>();
            fillClick.OnClick = openPlugin;
        }

        var leftArea = new GameObject("LeftArea", typeof(RectTransform));
        leftArea.transform.SetParent(target, false);
        var leftRT = leftArea.GetComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0f, 0f);
        leftRT.anchorMax = new Vector2(0.72f, 1f);
        leftRT.offsetMin = new Vector2(8f, 0f);
        leftRT.offsetMax = Vector2.zero;

        var titleLabel = UiFactory.CreateLabel(leftArea.transform, "Title", $"<b>{plugin.ModName}</b>", CyberPalette.ColorTextMain, 11f, TextAlignmentOptions.MidlineLeft);
        titleLabel.raycastTarget = false;
        var titleRT = titleLabel.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0.45f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;

        var subtitle = UiFactory.CreateLabel(leftArea.transform, "Subtitle", $"<color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorCyberTeal)}>v{plugin.Version}</color>  <color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorTextMuted)}>{plugin.ModGuid}</color>", CyberPalette.ColorTextMuted, 9f, TextAlignmentOptions.MidlineLeft);
        subtitle.raycastTarget = false;
        var subRT = subtitle.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0f, 0f);
        subRT.anchorMax = new Vector2(1f, 0.45f);
        subRT.offsetMin = Vector2.zero;
        subRT.offsetMax = Vector2.zero;

        var rightArea = new GameObject("RightArea", typeof(RectTransform));
        rightArea.transform.SetParent(target, false);
        var rightRT = rightArea.GetComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(0.72f, 0f);
        rightRT.anchorMax = new Vector2(1f, 1f);
        rightRT.offsetMin = Vector2.zero;
        rightRT.offsetMax = new Vector2(-8f, 0f);

        var badgeLabel = UiFactory.CreateLabel(rightArea.transform, "Badge", $"<color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorIceBlueBright)}>{plugin.AllSettings.Count} settings</color>  <color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorCyberTeal)}>▶</color>", CyberPalette.ColorTextMain, 10f, TextAlignmentOptions.MidlineRight);
        badgeLabel.raycastTarget = false;
        var badgeRT = badgeLabel.GetComponent<RectTransform>();
        badgeRT.anchorMin = Vector2.zero;
        badgeRT.anchorMax = Vector2.one;
        badgeRT.offsetMin = Vector2.zero;
        badgeRT.offsetMax = Vector2.zero;
    }

    private void ShowPluginSettings(PluginSettingsGroup plugin)
    {
        if (_activePlugin != plugin)
        {
            _expandedCategories.Clear();
        }

        _activePlugin = plugin;
        if (_contentContainer == null)
        {
            return;
        }

        foreach (Transform child in _contentContainer)
        {
            Destroy(child.gameObject);
        }

        GameObject navRow = new("NavRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        navRow.transform.SetParent(_contentContainer, false);
        RectTransform navRT = navRow.GetComponent<RectTransform>();
        navRT.anchorMin = new Vector2(0f, 1f);
        navRT.anchorMax = new Vector2(1f, 1f);
        navRT.pivot = new Vector2(0.5f, 1f);
        navRT.anchoredPosition = Vector2.zero;
        navRT.sizeDelta = new Vector2(0f, 26f);

        HorizontalLayoutGroup hlg = navRow.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(4, 4, 0, 0);
        hlg.spacing = 6f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        UiFactory.CreateCyberButton(navRow.transform, "BackBtn", "◀ Back", PopulatePlugins, CyberPalette.ColorErrorRed, CyberPalette.ColorErrorRed, 65f, 22f);

        TextMeshProUGUI titleLabel = UiFactory.CreateLabel(navRow.transform, "ModHeader", $"<b>{plugin.ModName}</b> <color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorCyberTeal)}>v{plugin.Version}</color>", CyberPalette.ColorTextMain, 11f, TextAlignmentOptions.MidlineLeft);
        LayoutElement titleLayout = titleLabel.gameObject.AddComponent<LayoutElement>();
        titleLayout.minWidth = 60f;
        titleLayout.preferredWidth = 200f;
        titleLayout.flexibleWidth = 1f;
        titleLayout.minHeight = 22f;
        titleLayout.preferredHeight = 22f;
        titleLayout.flexibleHeight = 0f;

        UiFactory.CreateCyberButton(navRow.transform, "ExpandBtn", "Expand", () =>
        {
            IEnumerable<KeyValuePair<string, List<SettingEntry>>> cats = plugin.GetFilteredCategories(_searchField?.text ?? string.Empty, !ConfigDrawerConfig.HideAdvancedByDefault.Value);
            foreach (KeyValuePair<string, List<SettingEntry>> c in cats)
            {
                _expandedCategories.Add(c.Key);
            }
            ShowPluginSettings(plugin);
        }, CyberPalette.ColorBorderSubtle, CyberPalette.ColorTextMuted, 52f, 22f);

        UiFactory.CreateCyberButton(navRow.transform, "CollapseBtn", "Collapse", () =>
        {
            _expandedCategories.Clear();
            ShowPluginSettings(plugin);
        }, CyberPalette.ColorBorderSubtle, CyberPalette.ColorTextMuted, 60f, 22f);

        CreateScrollArea(_contentContainer, out Transform listContainer);

        string query = _searchField?.text ?? string.Empty;
        bool hasSearch = !string.IsNullOrEmpty(query.Trim());
        IEnumerable<KeyValuePair<string, List<SettingEntry>>> categories = plugin.GetFilteredCategories(query, !ConfigDrawerConfig.HideAdvancedByDefault.Value);

        foreach (KeyValuePair<string, List<SettingEntry>> categoryGroup in categories)
        {
            string catKey = categoryGroup.Key;
            List<SettingEntry> categorySettings = categoryGroup.Value;
            bool isExpanded = hasSearch || _expandedCategories.Contains(catKey);

            GameObject catPanel = UiFactory.CreatePanel(listContainer, $"Cat_{catKey}", CyberPalette.ColorBorderSubtle, CyberPalette.ColorCardSurface, 1f);
            LayoutElement catLayout = catPanel.AddComponent<LayoutElement>();
            catLayout.minHeight = 24f;
            catLayout.preferredHeight = 24f;
            catLayout.flexibleHeight = 0f;
            catLayout.flexibleWidth = 1f;

            Image catBorderImg = catPanel.GetComponent<Image>();
            Transform? fill = catPanel.transform.Find("Fill");
            Transform target = fill != null ? fill : catPanel.transform;
            Image? fillImg = fill != null ? fill.GetComponent<Image>() : null;

            CyberHoverHandler catHover = catPanel.AddComponent<CyberHoverHandler>();
            catHover.Init(catBorderImg, CyberPalette.ColorBorderSubtle, CyberPalette.ColorIceBlueBright, fillImg, CyberPalette.ColorCardSurface, CyberPalette.ColorVoidBlack);

            string arrow = isExpanded ? "▼" : "▶";
            TextMeshProUGUI catLabel = UiFactory.CreateLabel(target, "CatTitle", $"{arrow}  {catKey.ToUpperInvariant()}  <color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorIceBlueBright)}>({categorySettings.Count})</color>", CyberPalette.ColorIceBlueBright, 10.5f, TextAlignmentOptions.MidlineLeft);
            RectTransform catRT = catLabel.GetComponent<RectTransform>();
            catRT.anchorMin = Vector2.zero;
            catRT.anchorMax = Vector2.one;
            catRT.offsetMin = new Vector2(8f, 0f);
            catRT.offsetMax = new Vector2(-8f, 0f);

            GameObject itemsContainer = new GameObject($"CatItems_{catKey}", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            itemsContainer.transform.SetParent(listContainer, false);
            VerticalLayoutGroup itemsVlg = itemsContainer.GetComponent<VerticalLayoutGroup>();
            itemsVlg.spacing = 3f;
            itemsVlg.childControlWidth = true;
            itemsVlg.childControlHeight = true;
            itemsVlg.childForceExpandWidth = true;
            itemsVlg.childForceExpandHeight = false;

            ContentSizeFitter itemsCsf = itemsContainer.GetComponent<ContentSizeFitter>();
            itemsCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement itemsLe = itemsContainer.AddComponent<LayoutElement>();
            itemsLe.flexibleWidth = 1f;

            if (isExpanded)
            {
                PopulateCategorySettings(itemsContainer.transform, categorySettings);
                itemsContainer.SetActive(true);
            }
            else
            {
                itemsContainer.SetActive(false);
            }

            Action toggleCat = () =>
            {
                bool nextState = !itemsContainer.activeSelf;
                if (nextState)
                {
                    _expandedCategories.Add(catKey);
                    PopulateCategorySettings(itemsContainer.transform, categorySettings);
                    itemsContainer.SetActive(true);
                }
                else
                {
                    _expandedCategories.Remove(catKey);
                    ClearCategorySettings(itemsContainer.transform);
                    itemsContainer.SetActive(false);
                }

                string nextArrow = nextState ? "▼" : "▶";
                catLabel.text = $"{nextArrow}  {catKey.ToUpperInvariant()}  <color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorIceBlueBright)}>({categorySettings.Count})</color>";

                if (listContainer is RectTransform lRT)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(lRT);
                }
            };

            catLabel.raycastTarget = false;

            ClickableBarHandler catClick = catPanel.AddComponent<ClickableBarHandler>();
            catClick.OnClick = toggleCat;

            if (fill != null)
            {
                ClickableBarHandler fillClick = fill.gameObject.AddComponent<ClickableBarHandler>();
                fillClick.OnClick = toggleCat;
            }
        }
    }

    private static void PopulateCategorySettings(Transform container, IReadOnlyList<SettingEntry> settings)
    {
        for (int i = 0; i < settings.Count; i++)
        {
            DrawerDispatcher.DrawSetting(container, settings[i]);
        }
    }

    private static void ClearCategorySettings(Transform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            if (child != null)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }

    private void OnSearchValueChanged(string query)
    {
        if (_clearSearchBtn != null)
        {
            _clearSearchBtn.SetActive(!string.IsNullOrEmpty(query));
        }

        if (_searchDebounceRoutine != null)
        {
            StopCoroutine(_searchDebounceRoutine);
        }

        _searchDebounceRoutine = StartCoroutine(SearchDebounceRoutine(query));
    }

    private IEnumerator SearchDebounceRoutine(string query)
    {
        yield return new WaitForSecondsRealtime(SearchDebounceSeconds);
        _searchDebounceRoutine = null;
        ExecuteSearch(query);
    }

    private void OnSearchCommitted(string query)
    {
        if (_searchDebounceRoutine != null)
        {
            StopCoroutine(_searchDebounceRoutine);
            _searchDebounceRoutine = null;
        }

        ExecuteSearch(query);
    }

    private void ExecuteSearch(string query)
    {
        if (_activeFileEditor != null)
        {
            return;
        }

        if (_activePlugin != null)
        {
            ShowPluginSettings(_activePlugin);
        }
        else if (_currentViewMode == MainViewMode.Files)
        {
            PopulateFiles();
        }
        else
        {
            PopulatePlugins();
        }
    }

    private void OnStartRebindClicked()
    {
        if (_isRecordingKeybind)
        {
            return;
        }

        StartCoroutine(RecordKeybindRoutine());
    }

    private static readonly KeyCode[] ModifierKeys = new[]
    {
        KeyCode.LeftAlt, KeyCode.RightAlt,
        KeyCode.LeftControl, KeyCode.RightControl,
        KeyCode.LeftShift, KeyCode.RightShift,
        KeyCode.LeftCommand, KeyCode.RightCommand,
        KeyCode.LeftWindows, KeyCode.RightWindows
    };

    private IEnumerator RecordKeybindRoutine()
    {
        _isRecordingKeybind = true;
        yield return null;
        while (Input.GetKey(KeyCode.Mouse0))
        {
            yield return null;
        }

        if (_rebindButtonText != null)
        {
            _rebindButtonText.text = "...";
            _rebindButtonText.color = CyberPalette.ColorWarningAmber;
        }

        while (_isRecordingKeybind)
        {
            yield return null;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                break;
            }

            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
            {
                ConfigDrawerConfig.ToggleKeybind.Value = new KeyboardShortcut(KeyCode.None);
                _isRecordingKeybind = false;
                break;
            }

            var heldModifiers = new List<KeyCode>();
            foreach (var mod in ModifierKeys)
            {
                if (Input.GetKey(mod))
                {
                    heldModifiers.Add(mod);
                }
            }

            if (heldModifiers.Count > 0 && _rebindButtonText != null)
            {
                _rebindButtonText.text = $"{string.Join("+", heldModifiers)}+...";
            }

            foreach (KeyCode code in Enum.GetValues(typeof(KeyCode)))
            {
                if (code == KeyCode.None || code == KeyCode.Escape || code == KeyCode.Backspace || code == KeyCode.Delete)
                {
                    continue;
                }

                if (Array.IndexOf(ModifierKeys, code) >= 0)
                {
                    continue;
                }

                if (Input.GetKeyDown(code))
                {
                    ConfigDrawerConfig.ToggleKeybind.Value = new KeyboardShortcut(code, heldModifiers.ToArray());
                    _isRecordingKeybind = false;
                    break;
                }
            }

            if (_isRecordingKeybind && heldModifiers.Count > 0)
            {
                foreach (var mod in heldModifiers)
                {
                    if (Input.GetKeyUp(mod))
                    {
                        ConfigDrawerConfig.ToggleKeybind.Value = new KeyboardShortcut(mod);
                        _isRecordingKeybind = false;
                        break;
                    }
                }
            }
        }

        _isRecordingKeybind = false;
        UpdateKeybindDisplays();
    }

    private string GetShortcutButtonText()
    {
        var shortcut = ConfigDrawerConfig.ToggleKeybind?.Value ?? new KeyboardShortcut(KeyCode.F1);
        if (shortcut.MainKey == KeyCode.None)
        {
            return "None";
        }
        var text = shortcut.ToString();
        return string.IsNullOrEmpty(text) ? "None" : text;
    }

    private string GetShortcutHintText()
    {
        var text = GetShortcutButtonText();
        return $"PRESS {text} TO CLOSE";
    }

    private void UpdateKeybindDisplays()
    {
        var text = GetShortcutButtonText();
        if (_rebindButtonText != null)
        {
            _rebindButtonText.text = text;
            _rebindButtonText.color = CyberPalette.ColorIceBlueBright;
        }

        if (_shortcutHintLabel != null)
        {
            _shortcutHintLabel.text = GetShortcutHintText();
        }
    }

    private void UpdateDockButtons(DockPosition position)
    {
        if (_dockLeftBtn != null)
        {
            _dockLeftBtn.SetActive(position != DockPosition.Left);
        }
        if (_dockRightBtn != null)
        {
            _dockRightBtn.SetActive(position != DockPosition.Right);
        }
        if (_dockFloatBtn != null)
        {
            _dockFloatBtn.SetActive(position != DockPosition.Floating);
        }
    }

    public void ApplyDockPosition(DockPosition position)
    {
        _currentDock = position;
        UpdateDockButtons(position);
        if (_drawerRootRT == null)
        {
            return;
        }

        var width = ConfigDrawerConfig.DrawerWidth.Value;

        if (_resizeHandleObj != null)
        {
            _resizeHandleObj.SetActive(position == DockPosition.Floating);
        }

        switch (position)
        {
            case DockPosition.Left:
                _drawerRootRT.anchorMin = new Vector2(0f, 0f);
                _drawerRootRT.anchorMax = new Vector2(0f, 1f);
                _drawerRootRT.pivot = new Vector2(0f, 0.5f);
                _drawerRootRT.sizeDelta = new Vector2(width, 0f);
                _drawerRootRT.anchoredPosition = Vector2.zero;
                break;

            case DockPosition.Right:
                _drawerRootRT.anchorMin = new Vector2(1f, 0f);
                _drawerRootRT.anchorMax = new Vector2(1f, 1f);
                _drawerRootRT.pivot = new Vector2(1f, 0.5f);
                _drawerRootRT.sizeDelta = new Vector2(width, 0f);
                _drawerRootRT.anchoredPosition = Vector2.zero;
                break;

            case DockPosition.Floating:
                _drawerRootRT.anchorMin = new Vector2(0.5f, 0.5f);
                _drawerRootRT.anchorMax = new Vector2(0.5f, 0.5f);
                _drawerRootRT.pivot = new Vector2(0.5f, 0.5f);
                _drawerRootRT.sizeDelta = new Vector2(Mathf.Max(width, 480f), 560f);
                _drawerRootRT.anchoredPosition = _floatingPosition;
                break;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_currentDock != DockPosition.Floating)
        {
            ApplyDockPosition(DockPosition.Floating);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_drawerRootRT == null || _currentDock != DockPosition.Floating)
        {
            return;
        }

        _floatingPosition += eventData.delta;
        _drawerRootRT.anchoredPosition = _floatingPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.position.x < 60f)
        {
            ApplyDockPosition(DockPosition.Left);
        }
        else if (eventData.position.x > Screen.width - 60f)
        {
            ApplyDockPosition(DockPosition.Right);
        }
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (IsVisible)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            if (!Cursor.visible)
            {
                Cursor.visible = true;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (ConfirmationModal.Instance != null && ConfirmationModal.Instance.IsOpen)
                {
                    ConfirmationModal.Instance.Hide();
                }
                else if (_searchField != null && _searchField.isFocused)
                {
                    _searchField.DeactivateInputField();
                    if (EventSystem.current != null)
                    {
                        EventSystem.current.SetSelectedGameObject(null);
                    }
                }
                else if (_activeFileEditor != null)
                {
                    if (_activeFileEditor.IsFocused)
                    {
                        _activeFileEditor.Defocus();
                    }
                    else
                    {
                        _activeFileEditor.HandleBackClicked();
                    }
                }
                else if (_activePlugin != null)
                {
                    PopulatePlugins();
                }
            }
        }
    }
}
