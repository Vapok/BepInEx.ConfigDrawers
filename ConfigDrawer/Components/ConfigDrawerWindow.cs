using BepInEx.ConfigDrawers.Drawers;
using BepInEx.Configuration;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using BepInEx.ConfigDrawers.Configuration;
using BepInEx.ConfigDrawers.Models;
using BepInEx.ConfigDrawers.UI;
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
    private Transform? _contentContainer;
    private TMP_InputField? _searchField;
    private TextMeshProUGUI? _rebindButtonText;

    private DockPosition _currentDock = DockPosition.Left;
    private Vector2 _floatingPosition;
    private bool _isRecordingKeybind;
    private PluginSettingsGroup? _activePlugin;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildWindow();
    }

    private void BuildWindow()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999;

        _canvasScaler = gameObject.AddComponent<CanvasScaler>();
        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        _canvasScaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        var drawerObj = UiFactory.CreatePanel(transform, "DrawerRoot", CyberPalette.ColorIceBlue, CyberPalette.ColorVoidBlack, 1f);
        _drawerRootRT = drawerObj.GetComponent<RectTransform>();
        ApplyDockPosition(ConfigDrawerConfig.DefaultDockPosition.Value);

        var fill = drawerObj.transform.Find("Fill");
        var container = fill != null ? fill : drawerObj.transform;

        BuildHeader(container);
        BuildBody(container);

        SetVisible(false);
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
        var headerObj = UiFactory.CreatePanel(parent, "Header", CyberPalette.ColorBorderCard, CyberPalette.ColorCardSurface, 1f);
        var headerRT = headerObj.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.offsetMin = new Vector2(4f, -44f);
        headerRT.offsetMax = new Vector2(-4f, -4f);

        var fill = headerObj.transform.Find("Fill");
        var target = fill != null ? fill : headerObj.transform;

        var title = UiFactory.CreateLabel(target, "Title", "// BEPINEX CONFIG DRAWERS //", CyberPalette.ColorIceBlue, 13f);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0f);
        titleRT.anchorMax = new Vector2(0.45f, 1f);
        titleRT.offsetMin = new Vector2(10f, 0f);
        titleRT.offsetMax = Vector2.zero;

        var controlsRow = new GameObject("Controls", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        controlsRow.transform.SetParent(target, false);
        var controlsRT = controlsRow.GetComponent<RectTransform>();
        controlsRT.anchorMin = new Vector2(0.45f, 0f);
        controlsRT.anchorMax = new Vector2(1f, 1f);
        controlsRT.offsetMin = Vector2.zero;
        controlsRT.offsetMax = new Vector2(-6f, 0f);

        var hlg = controlsRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        var rebindBtn = UiFactory.CreateCyberButton(controlsRow.transform, "RebindBtn", $"[ {ConfigDrawerConfig.ToggleKeybind.Value.MainKey} ]", OnStartRebindClicked, CyberPalette.ColorIceBlue, CyberPalette.ColorIceBlueBright, 70f, 24f);
        _rebindButtonText = rebindBtn.GetComponentInChildren<TextMeshProUGUI>();

        UiFactory.CreateCyberButton(controlsRow.transform, "DockLeftBtn", "[ L ]", () => ApplyDockPosition(DockPosition.Left), CyberPalette.ColorCyberTeal, CyberPalette.ColorTextMain, 32f, 24f);
        UiFactory.CreateCyberButton(controlsRow.transform, "DockRightBtn", "[ R ]", () => ApplyDockPosition(DockPosition.Right), CyberPalette.ColorCyberTeal, CyberPalette.ColorTextMain, 32f, 24f);
        UiFactory.CreateCyberButton(controlsRow.transform, "DetachBtn", "[ ⧉ ]", () => ApplyDockPosition(DockPosition.Floating), CyberPalette.ColorCyberTeal, CyberPalette.ColorTextMain, 32f, 24f);
        UiFactory.CreateCyberButton(controlsRow.transform, "CloseBtn", "[ X ]", () => SetVisible(false), CyberPalette.ColorWarningAmber, CyberPalette.ColorTextMain, 32f, 24f);
    }

    private void BuildBody(Transform parent)
    {
        var bodyObj = new GameObject("Body", typeof(RectTransform));
        bodyObj.transform.SetParent(parent, false);
        var bodyRT = bodyObj.GetComponent<RectTransform>();
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = new Vector2(4f, 4f);
        bodyRT.offsetMax = new Vector2(-4f, -48f);

        var searchPanel = UiFactory.CreatePanel(bodyObj.transform, "SearchPanel", CyberPalette.ColorBorderSubtle, CyberPalette.ColorVoidBlack, 1f);
        var searchRT = searchPanel.GetComponent<RectTransform>();
        searchRT.anchorMin = new Vector2(0f, 1f);
        searchRT.anchorMax = new Vector2(1f, 1f);
        searchRT.offsetMin = new Vector2(0f, -32f);
        searchRT.offsetMax = Vector2.zero;

        var (_, input) = UiFactory.CreateInputField(searchPanel.transform, "SearchInput", "", OnSearchChanged, -1f, 26f);
        _searchField = input;
        var inputRT = _searchField.GetComponent<RectTransform>();
        inputRT.anchorMin = Vector2.zero;
        inputRT.anchorMax = Vector2.one;
        inputRT.offsetMin = new Vector2(4f, 2f);
        inputRT.offsetMax = new Vector2(-4f, -2f);

        var contentArea = new GameObject("ContentArea", typeof(RectTransform));
        contentArea.transform.SetParent(bodyObj.transform, false);
        var contentRT = contentArea.GetComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = new Vector2(0f, -36f);

        _contentContainer = contentArea.transform;
    }

    public void SetVisible(bool visible)
    {
        IsVisible = visible;
        if (_drawerRootRT != null)
        {
            _drawerRootRT.gameObject.SetActive(visible);
        }

        if (visible)
        {
            ConfigRegistry.Instance.Refresh();
            PopulatePlugins();
            UnlockCursor();
            TryPauseGameInput(true);
        }
        else
        {
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
        if (_contentContainer == null)
        {
            return;
        }

        foreach (Transform child in _contentContainer)
        {
            Destroy(child.gameObject);
        }

        var scrollObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(_contentContainer, false);
        var scrollRT = scrollObj.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;

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
        vlg.spacing = 6f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;

        var csf = listContainer.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.content = listRT;
        scrollRect.viewport = vpRT;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        var query = _searchField?.text ?? string.Empty;
        var plugins = ConfigRegistry.Instance.SearchPlugins(query, !ConfigDrawerConfig.HideAdvancedByDefault.Value).ToList();

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

        var fill = card.transform.Find("Fill");
        var target = fill != null ? fill : card.transform;

        var btn = card.AddComponent<Button>();
        btn.onClick.AddListener(() => ShowPluginSettings(plugin));

        var label = UiFactory.CreateLabel(target, "ModName", $"{plugin.ModName} <color=#{ColorUtility.ToHtmlStringRGB(CyberPalette.ColorIceBlueBright)}>[ {plugin.AllSettings.Count} ]</color>", CyberPalette.ColorTextMain, 12f);
        var labelRT = label.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(12f, 0f);
        labelRT.offsetMax = new Vector2(-12f, 0f);
    }

    private void ShowPluginSettings(PluginSettingsGroup plugin)
    {
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
        navRT.offsetMin = new Vector2(0f, -32f);
        navRT.offsetMax = Vector2.zero;

        var hlg = navRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        UiFactory.CreateCyberButton(navRow.transform, "BackBtn", "[ < MODS ]", PopulatePlugins, CyberPalette.ColorIceBlue, CyberPalette.ColorIceBlueBright, 75f, 24f);
        UiFactory.CreateLabel(navRow.transform, "ModHeader", $"{plugin.ModName} v{plugin.Version}", CyberPalette.ColorTextMain, 12f);

        var scrollObj = new GameObject("SettingsScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(_contentContainer, false);
        var scrollRT = scrollObj.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = new Vector2(0f, -36f);

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
        vlg.spacing = 6f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;

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
            var catHeader = UiFactory.CreateLabel(listContainer.transform, $"Cat_{categoryGroup.Key}", $"// {categoryGroup.Key.ToUpperInvariant()} //", CyberPalette.ColorIceBlueBright, 11f);
            var catLayout = catHeader.gameObject.AddComponent<LayoutElement>();
            catLayout.minHeight = 22f;
            catLayout.preferredHeight = 22f;

            foreach (var setting in categoryGroup.Value)
            {
                DrawerDispatcher.DrawSetting(listContainer.transform, setting);
            }
        }
    }

    private void OnSearchChanged(string query)
    {
        PopulatePlugins();
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
            _rebindButtonText.text = "[ PRESS KEY ]";
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
                    break;
                }
            }
        }

        _isRecordingKeybind = false;
        if (_rebindButtonText != null)
        {
            _rebindButtonText.text = $"[ {ConfigDrawerConfig.ToggleKeybind.Value.MainKey} ]";
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
                _drawerRootRT.sizeDelta = new Vector2(width, Screen.height * 0.85f);
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
        if (_drawerRootRT == null)
        {
            return;
        }

        _floatingPosition += eventData.delta;
        _drawerRootRT.anchoredPosition = _floatingPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.position.x < 80f)
        {
            ApplyDockPosition(DockPosition.Left);
        }
        else if (eventData.position.x > Screen.width - 80f)
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
