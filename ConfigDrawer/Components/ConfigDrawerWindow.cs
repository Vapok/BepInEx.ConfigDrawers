using BepInEx.ConfigDrawers.Configuration;
using BepInEx.ConfigDrawers.Drawers;
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
    public static ConfigDrawerWindow? Instance { get; private set; }

    public bool IsVisible { get; private set; }
    public float SettingsColumnWidth => 280f;

    private Canvas? _canvas;
    private CanvasScaler? _canvasScaler;
    private RectTransform? _drawerRootRT;
    private GameObject? _resizeHandleObj;
    private Transform? _contentContainer;
    private TMP_InputField? _searchField;
    private TextMeshProUGUI? _rebindButtonText;
    private TextMeshProUGUI? _fontSizeButtonText;

    private DockPosition _currentDock = DockPosition.Left;
    private Vector2 _floatingPosition = Vector2.zero;
    private bool _isRecordingKeybind;
    private PluginSettingsGroup? _activePlugin;
    private readonly HashSet<string> _expandedCategories = new();

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureEventSystem();
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

        gameObject.AddComponent<GraphicRaycaster>();

        var drawerObj = UiFactory.CreatePanel(transform, "DrawerRoot", CyberPalette.ColorIceBlue, CyberPalette.ColorVoidBlack, 1f);
        _drawerRootRT = drawerObj.GetComponent<RectTransform>();

        BuildResizeHandle(drawerObj.transform);
        ApplyDockPosition(ConfigDrawerConfig.DefaultDockPosition.Value);

        var fill = drawerObj.transform.Find("Fill");
        var container = fill != null ? fill : drawerObj.transform;

        BuildHeader(container);
        BuildSearchBar(container);
        BuildContentArea(container);

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

    private void EnsureEventSystem()
    {
#pragma warning disable CS0618
        if (FindObjectOfType<EventSystem>() == null)
#pragma warning restore CS0618
        {
            var eventObj = new GameObject("ConfigDrawer_EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventObj);
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
        titleRT.anchorMax = new Vector2(0.32f, 1f);
        titleRT.offsetMin = new Vector2(8f, 0f);
        titleRT.offsetMax = Vector2.zero;

        var title = UiFactory.CreateLabel(titleArea.transform, "Title", "<b>CONFIG DRAWERS</b>", CyberPalette.ColorIceBlueBright, 11f, TextAlignmentOptions.Left);
        var labelRT = title.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        var controlsRow = new GameObject("Controls", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        controlsRow.transform.SetParent(target, false);
        var controlsRT = controlsRow.GetComponent<RectTransform>();
        controlsRT.anchorMin = new Vector2(0.32f, 0f);
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

        var rebindBtn = UiFactory.CreateCyberButton(controlsRow.transform, "RebindBtn", $"{ConfigDrawerConfig.ToggleKeybind.Value.MainKey}", OnStartRebindClicked, CyberPalette.ColorIceBlue, CyberPalette.ColorIceBlueBright, 46f, 22f);
        _rebindButtonText = rebindBtn.GetComponentInChildren<TextMeshProUGUI>();

        var fontBtn = UiFactory.CreateCyberButton(controlsRow.transform, "FontBtn", GetFontSizeLabel(), OnCycleFontSize, CyberPalette.ColorBorderSubtle, CyberPalette.ColorIceBlue, 74f, 22f);
        _fontSizeButtonText = fontBtn.GetComponentInChildren<TextMeshProUGUI>();

        UiFactory.CreateCyberButton(controlsRow.transform, "DockLeftBtn", "Left", () => ApplyDockPosition(DockPosition.Left), CyberPalette.ColorBorderSubtle, CyberPalette.ColorTextMain, 36f, 22f);
        UiFactory.CreateCyberButton(controlsRow.transform, "DockRightBtn", "Right", () => ApplyDockPosition(DockPosition.Right), CyberPalette.ColorBorderSubtle, CyberPalette.ColorTextMain, 40f, 22f);
        UiFactory.CreateCyberButton(controlsRow.transform, "DetachBtn", "Float", () => ApplyDockPosition(DockPosition.Floating), CyberPalette.ColorBorderSubtle, CyberPalette.ColorTextMain, 40f, 22f);
        UiFactory.CreateCyberButton(controlsRow.transform, "CloseBtn", "✕", () => SetVisible(false), CyberPalette.ColorWarningAmber, CyberPalette.ColorTextMain, 24f, 22f);
    }

    private string GetFontSizeLabel()
    {
        return ConfigDrawerConfig.UiFontSize.Value switch
        {
            FontSizeScale.Small => "Font: Sml",
            FontSizeScale.Large => "Font: Lrg",
            _ => "Font: Norm"
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

        if (_activePlugin != null)
        {
            ShowPluginSettings(_activePlugin);
        }
        else
        {
            PopulatePlugins();
        }
    }

    private void BuildSearchBar(Transform parent)
    {
        var searchContainer = new GameObject("SearchContainer", typeof(RectTransform));
        searchContainer.transform.SetParent(parent, false);
        var searchRT = searchContainer.GetComponent<RectTransform>();
        searchRT.anchorMin = new Vector2(0f, 1f);
        searchRT.anchorMax = new Vector2(1f, 1f);
        searchRT.pivot = new Vector2(0.5f, 1f);
        searchRT.anchoredPosition = new Vector2(0f, -42f);
        searchRT.sizeDelta = new Vector2(-10f, 26f);

        var (_, input) = UiFactory.CreateInputField(searchContainer.transform, "SearchInput", "", OnSearchChanged, -1f, 26f, "Search settings or mods...");
        _searchField = input;
        input.onValueChanged.AddListener(OnSearchChanged);

        var inputRT = input.gameObject.GetComponent<RectTransform>();
        inputRT.anchorMin = Vector2.zero;
        inputRT.anchorMax = Vector2.one;
        inputRT.offsetMin = Vector2.zero;
        inputRT.offsetMax = Vector2.zero;
    }

    private void BuildContentArea(Transform parent)
    {
        var contentArea = new GameObject("ContentArea", typeof(RectTransform));
        contentArea.transform.SetParent(parent, false);
        var contentRT = contentArea.GetComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = new Vector2(5f, 5f);
        contentRT.offsetMax = new Vector2(-5f, -72f);

        _contentContainer = contentArea.transform;
    }

    public void SetVisible(bool visible)
    {
        IsVisible = visible;

        if (visible)
        {
            EnsureWindowBuilt();
            if (_drawerRootRT != null)
            {
                _drawerRootRT.gameObject.SetActive(true);
            }

            ConfigRegistry.Instance.Refresh();
            if (_activePlugin != null)
            {
                ShowPluginSettings(_activePlugin);
            }
            else
            {
                PopulatePlugins();
            }

            UnlockCursor();
            TryPauseGameInput(true);
            UiFactory.RefreshAllFonts(gameObject);
        }
        else
        {
            if (_drawerRootRT != null)
            {
                _drawerRootRT.gameObject.SetActive(false);
            }

            HoverCardHandler.HideCard();
            TryPauseGameInput(false);
        }
    }

    public void Toggle()
    {
        SetVisible(!IsVisible);
    }

    private void PopulatePlugins()
    {
        _activePlugin = null;
        if (_contentContainer == null)
        {
            return;
        }

        foreach (Transform child in _contentContainer)
        {
            Destroy(child.gameObject);
        }

        var query = _searchField?.text ?? string.Empty;
        var plugins = ConfigRegistry.Instance.SearchPlugins(query, !ConfigDrawerConfig.HideAdvancedByDefault.Value).ToList();

        var headerRow = UiFactory.CreateLabel(_contentContainer, "ListHeader", $"LOADED PLUGINS ({plugins.Count})", CyberPalette.ColorIceBlueBright, 10.5f);
        var headerRT = headerRow.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.anchoredPosition = Vector2.zero;
        headerRT.sizeDelta = new Vector2(0f, 20f);

        var scrollObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(_contentContainer, false);
        var scrollRT = scrollObj.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = new Vector2(0f, -22f);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObj.transform, false);
        var vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;

        var listContainer = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        listContainer.transform.SetParent(viewport.transform, false);
        var listRT = listContainer.GetComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0f, 1f);
        listRT.anchorMax = new Vector2(1f, 1f);
        listRT.pivot = new Vector2(0.5f, 1f);
        listRT.offsetMin = Vector2.zero;
        listRT.offsetMax = Vector2.zero;

        var vlg = listContainer.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 3f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = listContainer.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.content = listRT;
        scrollRect.viewport = vpRT;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        foreach (var plugin in plugins)
        {
            RenderPluginCard(listContainer.transform, plugin);
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

        var btn = card.AddComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            _expandedCategories.Clear();
            ShowPluginSettings(plugin);
        });

        var leftArea = new GameObject("LeftArea", typeof(RectTransform));
        leftArea.transform.SetParent(target, false);
        var leftRT = leftArea.GetComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0f, 0f);
        leftRT.anchorMax = new Vector2(0.72f, 1f);
        leftRT.offsetMin = new Vector2(8f, 0f);
        leftRT.offsetMax = Vector2.zero;

        var titleLabel = UiFactory.CreateLabel(leftArea.transform, "Title", $"<b>{plugin.ModName}</b>", CyberPalette.ColorTextMain, 11f);
        var titleRT = titleLabel.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0.45f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;

        var subtitle = UiFactory.CreateLabel(leftArea.transform, "Subtitle", $"<color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorCyberTeal)}>v{plugin.Version}</color>  <color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorTextMuted)}>{plugin.ModGuid}</color>", CyberPalette.ColorTextMuted, 9f);
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

        var badgeLabel = UiFactory.CreateLabel(rightArea.transform, "Badge", $"<color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorIceBlueBright)}>{plugin.AllSettings.Count} settings</color>  <color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorCyberTeal)}>→</color>", CyberPalette.ColorTextMain, 10f, TextAlignmentOptions.Right);
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

        var navRow = new GameObject("NavRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        navRow.transform.SetParent(_contentContainer, false);
        var navRT = navRow.GetComponent<RectTransform>();
        navRT.anchorMin = new Vector2(0f, 1f);
        navRT.anchorMax = new Vector2(1f, 1f);
        navRT.pivot = new Vector2(0.5f, 1f);
        navRT.anchoredPosition = Vector2.zero;
        navRT.sizeDelta = new Vector2(0f, 26f);

        var hlg = navRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        UiFactory.CreateCyberButton(navRow.transform, "BackBtn", "← Mods", PopulatePlugins, CyberPalette.ColorIceBlue, CyberPalette.ColorIceBlueBright, 65f, 22f);

        var titleLabel = UiFactory.CreateLabel(navRow.transform, "ModHeader", $"<b>{plugin.ModName}</b> <color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorCyberTeal)}>v{plugin.Version}</color>", CyberPalette.ColorTextMain, 11f);
        var titleLayout = titleLabel.gameObject.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;

        UiFactory.CreateCyberButton(navRow.transform, "ExpandBtn", "Expand", () =>
        {
            var cats = plugin.GetFilteredCategories(_searchField?.text ?? string.Empty, !ConfigDrawerConfig.HideAdvancedByDefault.Value);
            foreach (var c in cats)
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

        var scrollObj = new GameObject("SettingsScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(_contentContainer, false);
        var scrollRT = scrollObj.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = new Vector2(0f, -28f);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObj.transform, false);
        var vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;

        var listContainer = new GameObject("SettingsList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        listContainer.transform.SetParent(viewport.transform, false);
        var listRT = listContainer.GetComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0f, 1f);
        listRT.anchorMax = new Vector2(1f, 1f);
        listRT.pivot = new Vector2(0.5f, 1f);
        listRT.offsetMin = Vector2.zero;
        listRT.offsetMax = Vector2.zero;

        var vlg = listContainer.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 3f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = listContainer.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.content = listRT;
        scrollRect.viewport = vpRT;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        var query = _searchField?.text ?? string.Empty;
        var categories = plugin.GetFilteredCategories(query, !ConfigDrawerConfig.HideAdvancedByDefault.Value);

        foreach (var categoryGroup in categories)
        {
            var catKey = categoryGroup.Key;
            var isExpanded = _expandedCategories.Contains(catKey);

            var catPanel = UiFactory.CreatePanel(listContainer.transform, $"Cat_{catKey}", CyberPalette.ColorBorderSubtle, CyberPalette.ColorCardSurface, 1f);
            var catLayout = catPanel.AddComponent<LayoutElement>();
            catLayout.minHeight = 24f;
            catLayout.preferredHeight = 24f;
            catLayout.flexibleHeight = 0f;
            catLayout.flexibleWidth = 1f;

            var catBtn = catPanel.AddComponent<Button>();
            catBtn.onClick.AddListener(() =>
            {
                if (isExpanded)
                {
                    _expandedCategories.Remove(catKey);
                }
                else
                {
                    _expandedCategories.Add(catKey);
                }
                ShowPluginSettings(plugin);
            });

            var fill = catPanel.transform.Find("Fill");
            var target = fill != null ? fill : catPanel.transform;

            var arrow = isExpanded ? "▼" : "►";
            var catLabel = UiFactory.CreateLabel(target, "CatTitle", $"{arrow}  {catKey.ToUpperInvariant()}  <color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorIceBlueBright)}>({categoryGroup.Value.Count})</color>", CyberPalette.ColorIceBlueBright, 10.5f);
            var catRT = catLabel.GetComponent<RectTransform>();
            catRT.anchorMin = Vector2.zero;
            catRT.anchorMax = Vector2.one;
            catRT.offsetMin = new Vector2(8f, 0f);
            catRT.offsetMax = new Vector2(-8f, 0f);

            if (isExpanded)
            {
                foreach (var setting in categoryGroup.Value)
                {
                    DrawerDispatcher.DrawSetting(listContainer.transform, setting);
                }
            }
        }
    }

    private void OnSearchChanged(string query)
    {
        if (_activePlugin != null)
        {
            ShowPluginSettings(_activePlugin);
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

    private IEnumerator RecordKeybindRoutine()
    {
        _isRecordingKeybind = true;
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

            foreach (KeyCode code in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(code) && code != KeyCode.Escape)
                {
                    ConfigDrawerConfig.ToggleKeybind.Value = new KeyboardShortcut(code);
                    _isRecordingKeybind = false;
                    break;
                }
            }
        }

        _isRecordingKeybind = false;
        if (_rebindButtonText != null)
        {
            _rebindButtonText.text = $"{ConfigDrawerConfig.ToggleKeybind.Value.MainKey}";
            _rebindButtonText.color = CyberPalette.ColorIceBlueBright;
        }
    }

    public void ApplyDockPosition(DockPosition position)
    {
        _currentDock = position;
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

    private void TryPauseGameInput(bool pause)
    {
        try
        {
            var playerType = Type.GetType("Player, assembly_valheim");
            if (playerType != null)
            {
                var takeInputMethod = playerType.GetMethod("TakeInput", BindingFlags.Instance | BindingFlags.Public);
                var localProp = playerType.GetProperty("m_localPlayer", BindingFlags.Static | BindingFlags.Public);
                var localPlayer = localProp?.GetValue(null, null);
                if (localPlayer != null && takeInputMethod != null)
                {
                    // Soft hook player input when active in Valheim
                }
            }
        }
        catch
        {
            // Soft failure ignore non-Valheim games
        }
    }
}
