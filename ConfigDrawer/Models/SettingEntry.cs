using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using BepInEx.Configuration;
using UnityEngine;

namespace ConfigDrawer.Models;

public class SettingEntry
{
    public ConfigEntryBase ConfigEntry { get; }
    public string Key => ConfigEntry.Definition.Key;
    public string Section => ConfigEntry.Definition.Section;
    public Type SettingType => ConfigEntry.SettingType;

    public string DispName { get; private set; }
    public string Category { get; private set; }
    public string Description { get; private set; }
    public int Order { get; private set; }
    public bool Browsable { get; private set; } = true;
    public bool IsAdvanced { get; private set; }
    public bool ReadOnly { get; private set; }
    public bool IsAdminOnly { get; private set; }
    public bool IsUnlocked { get; private set; } = true;
    public object? DefaultValue { get; private set; }
    public bool HideDefaultButton { get; private set; }
    public bool HideSettingName { get; private set; }
    public bool ShowRangeAsPercent { get; private set; }
    public Color EntryColor { get; private set; } = Color.white;
    public Color DescriptionColor { get; private set; } = Color.white;
    public Action<ConfigEntryBase>? CustomDrawer { get; private set; }
    public AcceptableValueBase? AcceptableValues => ConfigEntry.Description.AcceptableValues;

    public string EditBuffer { get; set; } = string.Empty;
    public bool IsDirty { get; private set; }
    public bool IsValid { get; private set; } = true;
    public string? ValidationMessage { get; private set; }

    public SettingEntry(ConfigEntryBase configEntry)
    {
        ConfigEntry = configEntry ?? throw new ArgumentNullException(nameof(configEntry));
        DispName = configEntry.Definition.Key;
        Category = configEntry.Definition.Section;
        Description = configEntry.Description.Description ?? string.Empty;
        DefaultValue = configEntry.DefaultValue;

        ExtractAttributes();
        ResetBuffer();
    }

    private void ExtractAttributes()
    {
        if (ConfigEntry.Description.Tags == null)
        {
            return;
        }

        foreach (var tag in ConfigEntry.Description.Tags)
        {
            if (tag == null)
            {
                continue;
            }

            switch (tag)
            {
                case DisplayNameAttribute da when !string.IsNullOrEmpty(da.DisplayName):
                    DispName = da.DisplayName;
                    break;
                case CategoryAttribute ca when !string.IsNullOrEmpty(ca.Category):
                    Category = ca.Category;
                    break;
                case DescriptionAttribute de when !string.IsNullOrEmpty(de.Description):
                    Description = de.Description;
                    break;
                case DefaultValueAttribute def:
                    DefaultValue = def.Value;
                    break;
                case ReadOnlyAttribute ro:
                    ReadOnly = ro.IsReadOnly;
                    break;
                case BrowsableAttribute bro:
                    Browsable = bro.Browsable;
                    break;
                default:
                    InspectDynamicTag(tag);
                    break;
            }
        }
    }

    private void InspectDynamicTag(object tag)
    {
        var tagType = tag.GetType();
        if (tagType.Name != "ConfigurationManagerAttributes")
        {
            return;
        }

        var fields = tagType.GetFields(BindingFlags.Instance | BindingFlags.Public);
        var properties = tagType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var prop in properties)
        {
            ReadTagMember(prop.Name, () => prop.GetValue(tag, null));
        }

        foreach (var field in fields)
        {
            ReadTagMember(field.Name, () => field.GetValue(tag));
        }
    }

    private void ReadTagMember(string name, Func<object?> getter)
    {
        try
        {
            var value = getter();
            if (value == null)
            {
                return;
            }

            switch (name)
            {
                case nameof(Browsable) when value is bool b:
                    Browsable = b;
                    break;
                case nameof(IsAdvanced) when value is bool b:
                    IsAdvanced = b;
                    break;
                case nameof(ReadOnly) when value is bool b:
                    ReadOnly = b;
                    break;
                case nameof(IsAdminOnly) when value is bool b:
                    IsAdminOnly = b;
                    break;
                case nameof(IsUnlocked) when value is bool b:
                    IsUnlocked = b;
                    break;
                case nameof(Category) when value is string s && !string.IsNullOrEmpty(s):
                    Category = s;
                    break;
                case nameof(DispName) when value is string s && !string.IsNullOrEmpty(s):
                    DispName = s;
                    break;
                case nameof(Description) when value is string s && !string.IsNullOrEmpty(s):
                    Description = s;
                    break;
                case nameof(Order) when value is int i:
                    Order = i;
                    break;
                case nameof(HideDefaultButton) when value is bool b:
                    HideDefaultButton = b;
                    break;
                case nameof(HideSettingName) when value is bool b:
                    HideSettingName = b;
                    break;
                case nameof(ShowRangeAsPercent) when value is bool b:
                    ShowRangeAsPercent = b;
                    break;
                case nameof(EntryColor) when value is Color c:
                    EntryColor = c;
                    break;
                case nameof(DescriptionColor) when value is Color c:
                    DescriptionColor = c;
                    break;
                case nameof(DefaultValue):
                    DefaultValue = value;
                    break;
                case nameof(CustomDrawer) when value is Action<ConfigEntryBase> drawer:
                    CustomDrawer = drawer;
                    break;
            }
        }
        catch
        {
            // Defensive ignore invalid tag values
        }
    }

    public void ResetBuffer()
    {
        var boxed = ConfigEntry.BoxedValue;
        EditBuffer = boxed != null ? Convert.ToString(boxed, CultureInfo.InvariantCulture) ?? string.Empty : string.Empty;
        IsDirty = false;
        IsValid = true;
        ValidationMessage = null;
    }

    public void UpdateBuffer(string newText)
    {
        EditBuffer = newText;
        var currentText = Convert.ToString(ConfigEntry.BoxedValue, CultureInfo.InvariantCulture) ?? string.Empty;
        IsDirty = EditBuffer != currentText;
        ValidateBuffer();
    }

    private void ValidateBuffer()
    {
        try
        {
            if (SettingType == typeof(string))
            {
                IsValid = true;
                ValidationMessage = null;
                return;
            }

            Convert.ChangeType(EditBuffer, SettingType, CultureInfo.InvariantCulture);
            IsValid = true;
            ValidationMessage = null;
        }
        catch (Exception ex)
        {
            IsValid = false;
            ValidationMessage = ex.Message;
        }
    }

    public bool CommitBuffer()
    {
        if (!IsValid || !IsDirty || ReadOnly || (!IsUnlocked && IsAdminOnly))
        {
            return false;
        }

        try
        {
            var parsedValue = SettingType == typeof(string) 
                ? EditBuffer 
                : Convert.ChangeType(EditBuffer, SettingType, CultureInfo.InvariantCulture);

            ConfigEntry.BoxedValue = parsedValue;
            IsDirty = false;
            ValidationMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            IsValid = false;
            ValidationMessage = ex.Message;
            return false;
        }
    }

    public void SetValue(object newValue)
    {
        if (ReadOnly || (!IsUnlocked && IsAdminOnly) || newValue == null)
        {
            return;
        }

        try
        {
            ConfigEntry.BoxedValue = newValue;
            ResetBuffer();
        }
        catch (Exception ex)
        {
            ValidationMessage = ex.Message;
        }
    }

    public void ResetToDefault()
    {
        if (DefaultValue == null || ReadOnly || (!IsUnlocked && IsAdminOnly))
        {
            return;
        }

        SetValue(DefaultValue);
    }
}
