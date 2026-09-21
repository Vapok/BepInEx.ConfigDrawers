using System;
using BepInEx.Configuration;
using UnityEngine;

namespace BepInEx.Configuration;

/// <summary>
/// Fluent procedural builder scope provided to <see cref="ConfigurationManagerAttributes.CustomUguiDrawer"/>
/// callbacks for rendering modern uGUI controls inside BepInEx.ConfigDrawers.
/// </summary>
public interface IUguiDrawerScope
{
    /// <summary>The parent RectTransform container for the current row or layout block.</summary>
    Transform Container { get; }

    /// <summary>True if the setting is locked by ServerSync or configured as ReadOnly.</summary>
    bool IsReadOnly { get; }

    /// <summary>Begins a horizontal row layout. Dispose at end of row.</summary>
    IDisposable Horizontal(float spacing = 4f);

    /// <summary>Begins a vertical layout. Dispose at end of block.</summary>
    IDisposable Vertical(float spacing = 4f);

    /// <summary>Begins an enclosed padded box container with optional background tint.</summary>
    IDisposable Box(Color? backgroundColor = null, float padding = 4f, float spacing = 4f);

    /// <summary>Begins a full-width row container with optional alternating background tint.</summary>
    IDisposable Row(Color? backgroundColor = null, float padding = 4f, float spacing = 4f);

    /// <summary>Renders a styled label.</summary>
    void Label(string text, float width = -1f);

    /// <summary>Renders an editable input text field.</summary>
    void TextField(string value, Action<string> onCommit, float width = -1f);

    /// <summary>Renders a styled button.</summary>
    void Button(string text, Action onClick, float width = 50f);

    /// <summary>Renders a styled button with an interactive hover tooltip.</summary>
    void Button(string text, Action onClick, float width, string? tooltip);

    /// <summary>Renders a slider with linked manual text input for high precision.</summary>
    void Slider(float value, float min, float max, Action<float> onChanged, float width = 120f, string? format = null);

    /// <summary>Renders an interactive toggle checkbox.</summary>
    void Toggle(bool value, string label, Action<bool> onChanged);

    /// <summary>Inserts horizontal or vertical spacing.</summary>
    void Space(float pixels);

    /// <summary>Renders a subtle horizontal divider line.</summary>
    void Separator(float height = 1f, Color? color = null);
}

/// <summary>
/// Metadata attributes attached to a configuration setting to customize its appearance and behavior.
/// Add an instance to the <see cref="BepInEx.Configuration.ConfigDescription.Tags"/> array.
/// </summary>
public class ConfigurationManagerAttributes
{
    /// <summary>Legacy IMGUI custom drawer action.</summary>
    public Action<ConfigEntryBase>? CustomDrawer;

    /// <summary>Modern uGUI custom drawer action using procedural UI scope.</summary>
    public Action<IUguiDrawerScope>? CustomUguiDrawer;

    /// <summary>Show this setting in the settings window. Default true.</summary>
    public bool? Browsable;

    /// <summary>Category the setting belongs to.</summary>
    public string? Category;

    /// <summary>Default value for the setting used when resetting.</summary>
    public object? DefaultValue;

    /// <summary>Setting description tooltip.</summary>
    public string? Description;

    /// <summary>Display name overriding the configuration key.</summary>
    public string? DispName;

    /// <summary>Sort order within the category (higher numbers appear first).</summary>
    public int? Order;

    /// <summary>If true, value cannot be edited.</summary>
    public bool? ReadOnly;

    /// <summary>If true, only visible when advanced settings are toggled on.</summary>
    public bool? IsAdvanced;

    /// <summary>If true, setting is synchronized from server and writable only by admins.</summary>
    public bool? IsAdminOnly;

    /// <summary>If true, the Reset button is hidden.</summary>
    public bool? HideDefaultButton;

    /// <summary>If true, setting name label is omitted.</summary>
    public bool? HideSettingName;

    /// <summary>Display numerical ranges as percentages.</summary>
    public bool? ShowRangeAsPercent;

    /// <summary>Color of the entry text.</summary>
    public Color? EntryColor;

    /// <summary>Color of the description text.</summary>
    public Color? DescriptionColor;

    /// <summary>Custom setting editor that allows polling keyboard input.</summary>
    public Delegate? CustomHotkeyDrawer;

    /// <summary>Custom converter from setting type to string for built-in editor textboxes.</summary>
    public Func<object, string>? ObjToStr;

    /// <summary>Custom converter from string to setting type for built-in editor textboxes.</summary>
    public Func<string, object>? StrToObj;
}
