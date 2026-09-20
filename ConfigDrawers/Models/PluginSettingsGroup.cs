using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;

namespace BepInEx.ConfigDrawers.Models;

public class PluginSettingsGroup
{
    public PluginInfo PluginInfo { get; }
    public string ModGuid => PluginInfo.Metadata.GUID;
    public string ModName => !string.IsNullOrEmpty(PluginInfo.Metadata.Name) ? PluginInfo.Metadata.Name : ModGuid;
    public string Version => PluginInfo.Metadata.Version.ToString();
    public List<SettingEntry> AllSettings { get; } = new();
    public bool IsPinned { get; set; }

    public PluginSettingsGroup(PluginInfo pluginInfo)
    {
        PluginInfo = pluginInfo ?? throw new ArgumentNullException(nameof(pluginInfo));
        LoadSettings();
    }

    public void LoadSettings()
    {
        AllSettings.Clear();
        var config = PluginInfo.Instance?.Config;
        if (config == null)
        {
            return;
        }

        foreach (var entry in config.Select(kv => kv.Value))
        {
            if (entry != null)
            {
                AllSettings.Add(new SettingEntry(entry));
            }
        }
    }

    public IEnumerable<KeyValuePair<string, List<SettingEntry>>> GetFilteredCategories(string query, bool showAdvanced)
    {
        var settings = AllSettings.Where(s => s.IsCurrentlyBrowsable && (showAdvanced || !s.IsAdvanced));

        if (!string.IsNullOrWhiteSpace(query))
        {
            settings = settings.Where(s =>
                s.DispName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                s.Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                s.Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                ModName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
            );
        }

        return settings
            .GroupBy(s => s.Category)
            .OrderBy(g => g.Key)
            .Select(g => new KeyValuePair<string, List<SettingEntry>>(
                g.Key,
                g.OrderByDescending(s => s.Order).ThenBy(s => s.DispName).ToList()
            ));
    }
}
