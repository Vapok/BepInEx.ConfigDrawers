using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using BepInEx.Bootstrap;
using BepInEx.ConfigDrawers.Components;
using BepInEx.ConfigDrawers.Configuration;
using BepInEx.Configuration;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Files;

public class ConfigFileWatcher : MonoBehaviour
{
    private const double DebounceMilliseconds = 300.0;
    private const double IgnoreDurationSeconds = 1.5;

    public static ConfigFileWatcher? Instance { get; private set; }

    private static readonly ConcurrentDictionary<string, DateTime> PendingChanges = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, DateTime> IgnoredPaths = new(StringComparer.OrdinalIgnoreCase);

    private FileSystemWatcher? _watcher;

    public static void Initialize(MonoBehaviour runner)
    {
        if (runner == null || Instance != null)
        {
            return;
        }

        GameObject host = runner.gameObject;
        Instance = host.AddComponent<ConfigFileWatcher>();
    }

    public static void IgnoreNextChange(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
        {
            return;
        }

        string normalized = Path.GetFullPath(fullPath);
        IgnoredPaths[normalized] = DateTime.UtcNow.AddSeconds(IgnoreDurationSeconds);
    }

    public static bool ReloadPluginConfigIfLoaded(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
        {
            return false;
        }

        string normalizedPath = Path.GetFullPath(fullPath);

        if (Chainloader.PluginInfos != null)
        {
            foreach (KeyValuePair<string, PluginInfo> kvp in Chainloader.PluginInfos)
            {
                PluginInfo? pluginInfo = kvp.Value;
                ConfigFile? config = pluginInfo?.Instance?.Config;
                if (config != null && !string.IsNullOrEmpty(config.ConfigFilePath))
                {
                    string configPath = Path.GetFullPath(config.ConfigFilePath);
                    if (string.Equals(configPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            config.Reload();
                            ConfigDrawers.Log?.LogInfo($"[ConfigDrawers] Reloaded config for plugin '{pluginInfo!.Metadata.Name}'.");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            ConfigDrawers.Log?.LogWarning($"[ConfigDrawers] Failed to reload config for '{pluginInfo!.Metadata.Name}': {ex.Message}");
                            return false;
                        }
                    }
                }
            }
        }

        if (ConfigDrawers.Instance != null && ConfigDrawers.Instance.Config != null)
        {
            string ourConfigPath = Path.GetFullPath(ConfigDrawers.Instance.Config.ConfigFilePath);
            if (string.Equals(ourConfigPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    ConfigDrawers.Instance.Config.Reload();
                    ConfigDrawers.Log?.LogInfo("[ConfigDrawers] Reloaded ConfigDrawers configuration.");
                    return true;
                }
                catch (Exception ex)
                {
                    ConfigDrawers.Log?.LogWarning($"[ConfigDrawers] Failed to reload ConfigDrawers config: {ex.Message}");
                    return false;
                }
            }
        }

        return false;
    }

    private void Awake()
    {
        Instance = this;
        StartWatcher();
    }

    private void StartWatcher()
    {
        if (!ConfigDrawerConfig.WatchConfigFiles.Value)
        {
            return;
        }

        string root = Paths.ConfigPath;
        if (!Directory.Exists(root))
        {
            return;
        }

        try
        {
            _watcher = new FileSystemWatcher(root);
            _watcher.IncludeSubdirectories = true;
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime;
            _watcher.Filter = "*.*";
            _watcher.Changed += OnFileEvent;
            _watcher.Created += OnFileEvent;
            _watcher.Renamed += OnFileRenamed;
            _watcher.EnableRaisingEvents = true;

            ConfigDrawers.Log?.LogInfo($"[ConfigDrawers] Config file watcher active on '{root}'.");
        }
        catch (Exception ex)
        {
            ConfigDrawers.Log?.LogWarning($"[ConfigDrawers] Failed to start config file watcher: {ex.Message}");
        }
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        QueueFileChange(e.FullPath);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        QueueFileChange(e.FullPath);
    }

    private void QueueFileChange(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
        {
            return;
        }

        string normalized = Path.GetFullPath(fullPath);
        if (IgnoredPaths.TryGetValue(normalized, out DateTime ignoreUntil) && DateTime.UtcNow < ignoreUntil)
        {
            return;
        }

        PendingChanges[normalized] = DateTime.UtcNow;
    }

    private void Update()
    {
        if (PendingChanges.IsEmpty)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        List<string>? readyPaths = null;

        foreach (KeyValuePair<string, DateTime> kvp in PendingChanges)
        {
            if ((now - kvp.Value).TotalMilliseconds >= DebounceMilliseconds)
            {
                if (readyPaths == null)
                {
                    readyPaths = new List<string>();
                }
                readyPaths.Add(kvp.Key);
            }
        }

        if (readyPaths == null || readyPaths.Count == 0)
        {
            return;
        }

        for (int i = 0; i < readyPaths.Count; i++)
        {
            string path = readyPaths[i];
            if (!PendingChanges.TryRemove(path, out _))
            {
                continue;
            }

            if (!File.Exists(path))
            {
                continue;
            }

            if (!IsFileReady(path))
            {
                PendingChanges[path] = now;
                continue;
            }

            ProcessFileChange(path);
        }
    }

    private static bool IsFileReady(string path)
    {
        try
        {
            using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return stream.Length >= 0;
        }
        catch (IOException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void ProcessFileChange(string fullPath)
    {
        string ext = Path.GetExtension(fullPath).ToLowerInvariant();
        if (ext == ".cfg")
        {
            ReloadPluginConfigIfLoaded(fullPath);
        }

        ConfigFileManager.Instance.Refresh();

        if (ConfigDrawerWindow.Instance != null && ConfigDrawerWindow.Instance.IsVisible)
        {
            ConfigDrawerWindow.Instance.NotifyExternalFileChanged(fullPath);
        }
    }

    private void OnDestroy()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
        PendingChanges.Clear();
        IgnoredPaths.Clear();
    }
}
