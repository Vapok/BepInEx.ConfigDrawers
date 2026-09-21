using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using BepInEx.Configuration;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Models;

public class SettingEntry
{
    private object? _cmaTagObject;
    private static float _lastAdminCheck;
    private static bool _cachedAdmin = true;

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
    public bool IsCustomTextArea { get; private set; }
    public AcceptableValueBase? AcceptableValues => ConfigEntry.Description.AcceptableValues;
    public KeyValuePair<object, object>? RangeBounds { get; private set; }
    public object[]? AcceptableValuesList { get; private set; }
    public Func<bool>? DynamicBrowsability { get; private set; }
    public bool IsCurrentlyBrowsable => Browsable && (DynamicBrowsability == null || DynamicBrowsability());

    public string EditBuffer { get; set; } = string.Empty;
    public bool IsDirty { get; private set; }
    public bool IsValid { get; private set; } = true;
    public string? ValidationMessage { get; private set; }

    public bool CanEdit
    {
        get
        {
            if (CheckDynamicUnlocked())
            {
                return true;
            }

            if (IsAdminOnly)
            {
                return IsAdminOrSinglePlayer();
            }

            if (ReadOnly)
            {
                return false;
            }

            return true;
        }
    }

    public bool CanReset => CanEdit && (!HideDefaultButton || IsAdminOnly) && DefaultValue != null;

    public SettingEntry(ConfigEntryBase configEntry)
    {
        ConfigEntry = configEntry ?? throw new ArgumentNullException(nameof(configEntry));
        DispName = configEntry.Definition.Key;
        Category = configEntry.Definition.Section;
        Description = configEntry.Description.Description ?? string.Empty;
        DefaultValue = configEntry.DefaultValue;

        ExtractAttributes();
        ExtractAcceptableValues();
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


    private void ExtractAcceptableValues()
    {
        var av = ConfigEntry.Description.AcceptableValues;
        if (av == null)
        {
            return;
        }

        var type = av.GetType();
        var minProp = type.GetProperty("MinValue");
        var maxProp = type.GetProperty("MaxValue");
        if (minProp != null && maxProp != null)
        {
            var min = minProp.GetValue(av, null);
            var max = maxProp.GetValue(av, null);
            if (min != null && max != null)
            {
                RangeBounds = new KeyValuePair<object, object>(min, max);
                return;
            }
        }

        var listProp = type.GetProperty("AcceptableValues");
        if (listProp != null)
        {
            if (listProp.GetValue(av, null) is IEnumerable enumerable)
            {
                AcceptableValuesList = enumerable.Cast<object>().ToArray();
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

        _cmaTagObject = tag;

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
                case nameof(CustomDrawer) when value is Delegate del:
                    if (SettingType == typeof(string) && IsSimpleTextAreaDrawer(del))
                    {
                        IsCustomTextArea = true;
                        CustomDrawer = null;
                        break;
                    }
                    CustomDrawer = cfg =>
                    {
                        try
                        {
                            del.DynamicInvoke(cfg);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[ConfigDrawers] CustomDrawer error for {Key}: {ex}");
                        }
                    };
                    break;
                case "browsability" when value is Func<bool> fb:
                    DynamicBrowsability = fb;
                    break;
            }
        }
        catch (Exception ex)
        {
            ConfigDrawers.Log?.LogDebug($"[ConfigDrawers] Ignored tag {name} on {Key}: {ex.Message}");
        }
    }

    private bool CheckDynamicUnlocked()
    {
        if (_cmaTagObject == null)
        {
            return false;
        }

        try
        {
            Type type = _cmaTagObject.GetType();
            PropertyInfo? prop = type.GetProperty("IsUnlocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.GetValue(_cmaTagObject, null) is bool unlocked)
            {
                return unlocked;
            }

            FieldInfo? field = type.GetField("IsUnlocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.GetValue(_cmaTagObject) is bool unlockedField)
            {
                return unlockedField;
            }
        }
        catch (Exception ex)
        {
            ConfigDrawers.Log?.LogDebug($"[ConfigDrawers] Dynamic unlocked check failed for {Key}: {ex.Message}");
        }

        return false;
    }

    public static bool IsAdminOrSinglePlayer()
    {
        if (Time.unscaledTime - _lastAdminCheck < 1.0f)
        {
            return _cachedAdmin;
        }

        _lastAdminCheck = Time.unscaledTime;

        try
        {
            Type? znetType = Type.GetType("ZNet, assembly_valheim");
            if (znetType == null)
            {
                _cachedAdmin = true;
                return true;
            }

            PropertyInfo? instanceProp = znetType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public)
                                      ?? znetType.GetProperty("m_instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            object? instance = instanceProp?.GetValue(null);
            if (instance == null)
            {
                _cachedAdmin = true;
                return true;
            }

            MethodInfo? adminOrHostMethod = znetType.GetMethod("LocalPlayerIsAdminOrHost", BindingFlags.Instance | BindingFlags.Public);
            if (adminOrHostMethod != null)
            {
                _cachedAdmin = (bool)adminOrHostMethod.Invoke(instance, null);
                return _cachedAdmin;
            }

            MethodInfo? isServerMethod = znetType.GetMethod("IsServer", BindingFlags.Instance | BindingFlags.Public);
            if (isServerMethod != null)
            {
                _cachedAdmin = (bool)isServerMethod.Invoke(instance, null);
                return _cachedAdmin;
            }

            _cachedAdmin = true;
            return true;
        }
        catch (Exception ex)
        {
            ConfigDrawers.Log?.LogDebug($"[ConfigDrawers] Admin check exception fallback: {ex.Message}");
            _cachedAdmin = true;
            return true;
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
        if (!IsValid || !IsDirty || !CanEdit)
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
        if (!CanEdit || newValue == null)
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

    private static bool IsSimpleTextAreaDrawer(Delegate del)
    {
        var method = del?.Method;
        if (method == null)
        {
            return false;
        }

        if (method.Name.IndexOf("TextArea", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        try
        {
            var body = method.GetMethodBody();
            if (body == null)
            {
                return false;
            }

            var bytes = body.GetILAsByteArray();
            if (bytes == null || bytes.Length > 250)
            {
                return false;
            }

            var module = method.Module;
            var hasTextArea = false;
            var hasOtherControls = false;

            for (var i = 0; i < bytes.Length - 4; i++)
            {
                try
                {
                    var token = BitConverter.ToInt32(bytes, i);
                    var member = module.ResolveMember(token);
                    if (member is MethodBase mb && mb.DeclaringType != null)
                    {
                        var typeName = mb.DeclaringType.FullName ?? string.Empty;
                        if (typeName.StartsWith("UnityEngine.GUI") || typeName.StartsWith("UnityEngine.GUILayout"))
                        {
                            if (mb.Name.Contains("TextArea"))
                            {
                                hasTextArea = true;
                            }
                            else if (mb.Name.Contains("Button") || mb.Name.Contains("Toggle") || mb.Name.Contains("Slider") || mb.Name.Contains("Window") || mb.Name.Contains("ScrollView") || mb.Name.Contains("SelectionGrid"))
                            {
                                hasOtherControls = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ConfigDrawers.Log?.LogDebug($"[ConfigDrawers] Token inspection skipped: {ex.Message}");
                }
            }

            return hasTextArea && !hasOtherControls;
        }
        catch (Exception ex)
        {
            ConfigDrawers.Log?.LogDebug($"[ConfigDrawers] TextArea detection failed: {ex.Message}");
            return false;
        }
    }

    public void ResetToDefault()
    {
        if (!CanReset || DefaultValue == null)
        {
            return;
        }

        SetValue(DefaultValue);
    }
}
