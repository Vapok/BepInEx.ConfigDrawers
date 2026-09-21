using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Files;

public enum ConfigFileFilter
{
    All,
    Cfg,
    Json,
    Yaml,
    Other
}

public class ConfigFileManager
{
    private static ConfigFileManager? _instance;
    public static ConfigFileManager Instance => _instance ??= new ConfigFileManager();

    public string ConfigRoot { get; }
    private readonly List<ConfigFileItem> _files = new();
    private DateTime _lastScanTime = DateTime.MinValue;

    private static readonly HashSet<string> KnownExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cfg", ".json", ".yml", ".yaml", ".ini", ".txt", ".xml"
    };

    public ConfigFileManager()
    {
        ConfigRoot = Paths.ConfigPath;
    }

    public IReadOnlyList<ConfigFileItem> GetAllFiles()
    {
        if (_files.Count == 0 || (DateTime.UtcNow - _lastScanTime).TotalSeconds > 5.0)
        {
            Refresh();
        }
        return _files;
    }

    public void Refresh()
    {
        _files.Clear();
        _lastScanTime = DateTime.UtcNow;

        if (!Directory.Exists(ConfigRoot))
        {
            return;
        }

        try
        {
            DirectoryInfo rootDir = new DirectoryInfo(ConfigRoot);
            FileInfo[] allFiles = rootDir.GetFiles("*", SearchOption.AllDirectories);

            for (int i = 0; i < allFiles.Length; i++)
            {
                FileInfo file = allFiles[i];
                if ((file.Attributes & FileAttributes.Hidden) != 0)
                {
                    continue;
                }

                string ext = file.Extension.ToLowerInvariant();
                if (KnownExtensions.Contains(ext))
                {
                    _files.Add(new ConfigFileItem(file, ConfigRoot));
                }
            }

            _files.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ConfigDrawers] Failed to scan config directory {ConfigRoot}: {ex}");
        }
    }

    public List<ConfigFileItem> FilterFiles(ConfigFileFilter filter, string? query)
    {
        IEnumerable<ConfigFileItem> source = GetAllFiles();

        if (filter != ConfigFileFilter.All)
        {
            source = filter switch
            {
                ConfigFileFilter.Cfg => source.Where(f => f.Extension == ".cfg" || f.Extension == ".ini"),
                ConfigFileFilter.Json => source.Where(f => f.Extension == ".json"),
                ConfigFileFilter.Yaml => source.Where(f => f.Extension == ".yml" || f.Extension == ".yaml"),
                ConfigFileFilter.Other => source.Where(f => f.Extension != ".cfg" && f.Extension != ".ini" && f.Extension != ".json" && f.Extension != ".yml" && f.Extension != ".yaml"),
                _ => source
            };
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            string q = query!.Trim();
            source = source.Where(f => f.RelativePath.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       f.FileName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        return source.ToList();
    }

    public int GetFilterCount(ConfigFileFilter filter)
    {
        IReadOnlyList<ConfigFileItem> all = GetAllFiles();
        if (filter == ConfigFileFilter.All)
        {
            return all.Count;
        }

        return filter switch
        {
            ConfigFileFilter.Cfg => all.Count(f => f.Extension == ".cfg" || f.Extension == ".ini"),
            ConfigFileFilter.Json => all.Count(f => f.Extension == ".json"),
            ConfigFileFilter.Yaml => all.Count(f => f.Extension == ".yml" || f.Extension == ".yaml"),
            ConfigFileFilter.Other => all.Count(f => f.Extension != ".cfg" && f.Extension != ".ini" && f.Extension != ".json" && f.Extension != ".yml" && f.Extension != ".yaml"),
            _ => all.Count
        };
    }

    public string ReadFileText(ConfigFileItem item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        return File.ReadAllText(item.FullPath, Encoding.UTF8);
    }

    public void SaveFileText(ConfigFileItem item, string content)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        File.WriteAllText(item.FullPath, content, Encoding.UTF8);
    }
}
