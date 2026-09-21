using System;
using System.IO;
using BepInEx.ConfigDrawers.UI;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Files;

public class ConfigFileItem
{
    public string FullPath { get; }
    public string RelativePath { get; }
    public string FileName { get; }
    public string Extension { get; }
    public long SizeBytes { get; }
    public DateTime LastModified { get; }

    public ConfigFileItem(FileInfo fileInfo, string configRoot)
    {
        if (fileInfo == null)
        {
            throw new ArgumentNullException(nameof(fileInfo));
        }

        FullPath = fileInfo.FullName;
        FileName = fileInfo.Name;
        Extension = fileInfo.Extension.ToLowerInvariant();
        SizeBytes = fileInfo.Length;
        LastModified = fileInfo.LastWriteTime;

        if (!string.IsNullOrEmpty(configRoot) && FullPath.StartsWith(configRoot, StringComparison.OrdinalIgnoreCase))
        {
            string rel = FullPath.Substring(configRoot.Length);
            RelativePath = rel.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        else
        {
            RelativePath = FileName;
        }
    }

    public string FormattedSize
    {
        get
        {
            if (SizeBytes < 1024)
            {
                return $"{SizeBytes} B";
            }
            if (SizeBytes < 1024 * 1024)
            {
                return $"{(SizeBytes / 1024.0f):F1} KB";
            }
            return $"{(SizeBytes / (1024.0f * 1024.0f)):F2} MB";
        }
    }

    public string BadgeText
    {
        get
        {
            return Extension switch
            {
                ".cfg" => "CFG",
                ".json" => "JSON",
                ".yml" or ".yaml" => "YAML",
                ".ini" => "INI",
                ".txt" => "TXT",
                ".xml" => "XML",
                _ => Extension.TrimStart('.').ToUpperInvariant()
            };
        }
    }

    public Color BadgeColor
    {
        get
        {
            return Extension switch
            {
                ".cfg" => CyberPalette.ColorIceBlueBright,
                ".json" => CyberPalette.ColorGlacialMint,
                ".yml" or ".yaml" => CyberPalette.ColorWarningAmber,
                ".ini" => CyberPalette.ColorTextMain,
                ".txt" => CyberPalette.ColorTextMuted,
                ".xml" => CyberPalette.ColorIceBlue,
                _ => CyberPalette.ColorBorderSubtle
            };
        }
    }
}
