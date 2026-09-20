using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;

namespace ConfigDrawer.Models;

public class ConfigRegistry
{
    private static ConfigRegistry? _instance;
    public static ConfigRegistry Instance => _instance ??= new ConfigRegistry();

    public List<PluginSettingsGroup> Plugins { get; } = new();
    public event Action? OnSettingsRefreshed;

    private ConfigRegistry()
    {
    }

    public void Refresh()
    {
        Plugins.Clear();

        if (Chainloader.PluginInfos == null)
        {
            return;
        }

        foreach (var kvp in Chainloader.PluginInfos)
        {
            var pluginInfo = kvp.Value;
            if (pluginInfo?.Instance?.Config == null)
            {
                continue;
            }

            var group = new PluginSettingsGroup(pluginInfo);
            if (group.AllSettings.Count > 0)
            {
                Plugins.Add(group);
            }
        }

        Plugins.Sort((a, b) => string.Compare(a.ModName, b.ModName, StringComparison.OrdinalIgnoreCase));
        OnSettingsRefreshed?.Invoke();
    }

    public IEnumerable<PluginSettingsGroup> SearchPlugins(string query, bool showAdvanced)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Plugins.Where(p => p.AllSettings.Any(s => s.Browsable && (showAdvanced || !s.IsAdvanced)));
        }

        return Plugins.Where(p =>
            p.ModName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
            p.ModGuid.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
            p.AllSettings.Any(s =>
                s.Browsable &&
                (showAdvanced || !s.IsAdvanced) &&
                (s.DispName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 s.Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 s.Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
        );
    }
}
