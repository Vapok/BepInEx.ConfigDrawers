using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace BepInEx.ConfigDrawers.UI;

public static class UIFonts
{
    private static TMP_FontAsset? _cachedPrimaryFont;
    private static TMP_FontAsset? _cachedTerminalFont;
    private static TMP_FontAsset? _customFontRegular;
    private static TMP_FontAsset? _customFontBold;
    private static bool _customFontLoadAttempted;

    private const string ResourceRegularFont = "BepInEx.ConfigDrawers.Resources.Hack-Regular.ttf";
    private const string ResourceBoldFont = "BepInEx.ConfigDrawers.Resources.Hack-Bold.ttf";
    private const string CacheDirectoryName = "ConfigDrawers";
    private const string FileRegularFont = "Hack-Regular.ttf";
    private const string FileBoldFont = "Hack-Bold.ttf";

    public static TMP_FontAsset? GetPrimaryFont()
    {
        if (_cachedPrimaryFont != null)
        {
            return _cachedPrimaryFont;
        }

        TMP_FontAsset? terminal = GetTerminalFont();
        if (terminal != null)
        {
            _cachedPrimaryFont = terminal;
            return _cachedPrimaryFont;
        }

        try
        {
            TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (allFonts != null && allFonts.Length > 0)
            {
                foreach (TMP_FontAsset font in allFonts)
                {
                    if (font == null || string.IsNullOrEmpty(font.name))
                    {
                        continue;
                    }

                    string fontName = font.name.ToLowerInvariant();
                    if ((fontName.Contains("valheim") || fontName.Contains("averia")) &&
                        !fontName.Contains("norse") &&
                        !fontName.Contains("bold") &&
                        !fontName.Contains("prstart"))
                    {
                        if (font.characterTable != null && font.characterTable.Count > 0)
                        {
                            SetCachedFont(font);
                            return _cachedPrimaryFont;
                        }
                    }
                }

                foreach (TMP_FontAsset font in allFonts)
                {
                    if (font == null || string.IsNullOrEmpty(font.name))
                    {
                        continue;
                    }

                    string fontName = font.name.ToLowerInvariant();
                    if (!fontName.Contains("prstart") && !fontName.Contains("norse"))
                    {
                        if (font.characterTable != null && font.characterTable.Count > 0)
                        {
                            SetCachedFont(font);
                            return _cachedPrimaryFont;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ConfigDrawers.Log?.LogWarning($"[ConfigDrawers] Failed during fallback font discovery: {ex.Message}");
        }

        return null;
    }

    public static TMP_FontAsset? GetTerminalFont()
    {
        if (_cachedTerminalFont != null)
        {
            return _cachedTerminalFont;
        }

        EnsureCustomFontLoaded();
        if (_customFontRegular != null)
        {
            _cachedTerminalFont = _customFontRegular;
            return _cachedTerminalFont;
        }

        try
        {
            TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (allFonts != null && allFonts.Length > 0)
            {
                foreach (TMP_FontAsset font in allFonts)
                {
                    if (font == null || string.IsNullOrEmpty(font.name))
                    {
                        continue;
                    }

                    string fontName = font.name.ToLowerInvariant();
                    if (fontName.Contains("hack") || fontName.Contains("mono") || fontName.Contains("consolas"))
                    {
                        if (font.characterTable != null && font.characterTable.Count > 0)
                        {
                            _cachedTerminalFont = font;
                            return _cachedTerminalFont;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ConfigDrawers.Log?.LogWarning($"[ConfigDrawers] Failed during terminal font discovery: {ex.Message}");
        }

        return GetPrimaryFont();
    }

    public static void RefreshAllFonts(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        TMP_FontAsset? font = GetPrimaryFont();
        if (font == null)
        {
            return;
        }

        TextMeshProUGUI[] allTexts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in allTexts)
        {
            if (text != null)
            {
                text.font = font;
                if (font.material != null)
                {
                    text.fontSharedMaterial = font.material;
                }
            }
        }
    }

    private static void EnsureCustomFontLoaded()
    {
        if (_customFontLoadAttempted)
        {
            return;
        }

        _customFontLoadAttempted = true;

        _customFontRegular = CreateFontFromTtf(ResourceRegularFont, FileRegularFont);
        _customFontBold = CreateFontFromTtf(ResourceBoldFont, FileBoldFont);

        if (_customFontRegular != null && _customFontRegular.material != null)
        {
            TMP_FontAsset[] existingFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (existingFonts != null && existingFonts.Length > 0)
            {
                foreach (TMP_FontAsset existing in existingFonts)
                {
                    if (existing != null && existing != _customFontRegular && existing.material != null && existing.material.shader != null)
                    {
                        _customFontRegular.material.shader = existing.material.shader;
                        if (_customFontBold != null && _customFontBold.material != null)
                        {
                            _customFontBold.material.shader = existing.material.shader;
                        }
                        break;
                    }
                }
            }
        }
    }

    private static TMP_FontAsset? CreateFontFromTtf(string resourceName, string fontFileName)
    {
        try
        {
            Assembly assembly = typeof(UIFonts).Assembly;
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                ConfigDrawers.Log?.LogWarning($"[ConfigDrawers] TTF resource stream not found: {resourceName}");
                return null;
            }

            string cacheDirectory = Path.Combine(BepInEx.Paths.CachePath, CacheDirectoryName);
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            string fontPath = Path.Combine(cacheDirectory, fontFileName);
            using (FileStream fileStream = new FileStream(fontPath, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(fileStream);
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                fontPath,
                0,
                36,
                5,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                1024,
                1024
            );

            if (fontAsset != null)
            {
                ConfigDrawers.Log?.LogInfo($"[ConfigDrawers] Generated dynamic TMP font asset from {fontFileName}");
                return fontAsset;
            }
        }
        catch (Exception ex)
        {
            ConfigDrawers.Log?.LogWarning($"[ConfigDrawers] Error creating dynamic font from {fontFileName}: {ex.Message}");
        }

        return null;
    }

    private static void SetCachedFont(TMP_FontAsset font)
    {
        _cachedPrimaryFont = font;
        try
        {
            if (TMP_Settings.defaultFontAsset == null)
            {
                TMP_Settings.defaultFontAsset = font;
            }
        }
        catch (Exception ex)
        {
            ConfigDrawers.Log?.LogWarning($"[ConfigDrawers] Failed setting TMP_Settings.defaultFontAsset: {ex.Message}");
        }
    }
}
