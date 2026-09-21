using System;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace BepInEx.ConfigDrawers.Files;

public static class ConfigFileSyntaxHighlighter
{
    private static readonly Color32 ColorComment = new(0x54, 0x6E, 0x7A, 0xFF);
    private static readonly Color32 ColorKey = new(0x5C, 0xE1, 0xE6, 0xFF);
    private static readonly Color32 ColorString = new(0xA8, 0xFF, 0x78, 0xFF);
    private static readonly Color32 ColorNumber = new(0xBD, 0x93, 0xF9, 0xFF);
    private static readonly Color32 ColorKeyword = new(0xFF, 0x79, 0xC6, 0xFF);
    private static readonly Color32 ColorBracket = new(0x80, 0xD8, 0xFF, 0xFF);
    private static readonly Color32 ColorPunct = new(0x4A, 0x65, 0x72, 0xFF);
    private static readonly Color32 ColorSection = new(0x80, 0xD8, 0xFF, 0xFF);
    private static readonly Color32 ColorDefault = new(0xC8, 0xDB, 0xEE, 0xFF);

    private static readonly Regex JsonTokenRegex = new(
        @"(?<comment>//[^\r\n]*|/\*[\s\S]*?\*/)|(?<string>""(?:\\.|[^""\\])*"")(?:\s*(?<colon>:))?|(?<number>-?\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b)|(?<keyword>\b(?:true|false|null)\b)|(?<bracket>[\[\]\{\}])|(?<punct>[:,])",
        RegexOptions.Compiled);

    private static readonly Regex CfgSectionRegex = new(@"^\s*(\[[^\]]+\])\s*$", RegexOptions.Compiled);
    private static readonly Regex CfgKeyValueRegex = new(@"^\s*([^#;=\r\n]+?)\s*(=)\s*(.*)$", RegexOptions.Compiled);
    private static readonly Regex NumberRegex = new(@"^-?\d+(?:\.\d+)?$", RegexOptions.Compiled);

    public static string Highlight(string rawContent, string extension)
    {
        if (string.IsNullOrEmpty(rawContent))
        {
            return string.Empty;
        }

        string safeContent = EscapeAngleBrackets(rawContent);

        return extension.ToLowerInvariant() switch
        {
            ".json" => HighlightJson(safeContent),
            ".cfg" or ".ini" => HighlightCfg(safeContent),
            ".yml" or ".yaml" => HighlightYaml(safeContent),
            _ => safeContent
        };
    }

    private static string EscapeAngleBrackets(string input)
    {
        return input.Replace("<", "<\u200B");
    }

    private static string HighlightJson(string content)
    {
        return JsonTokenRegex.Replace(content, match =>
        {
            if (match.Groups["comment"].Success)
            {
                return $"<color=#546E7A><i>{match.Value}</i></color>";
            }

            if (match.Groups["string"].Success)
            {
                string str = match.Groups["string"].Value;
                if (match.Groups["colon"].Success)
                {
                    return $"<color=#5CE1E6>{str}</color><color=#4A6572>:</color>";
                }
                return $"<color=#A8FF78>{str}</color>";
            }

            if (match.Groups["number"].Success)
            {
                return $"<color=#BD93F9>{match.Value}</color>";
            }

            if (match.Groups["keyword"].Success)
            {
                return $"<color=#FF79C6>{match.Value}</color>";
            }

            if (match.Groups["bracket"].Success)
            {
                return $"<color=#80D8FF>{match.Value}</color>";
            }

            if (match.Groups["punct"].Success)
            {
                return $"<color=#4A6572>{match.Value}</color>";
            }

            return match.Value;
        });
    }

    private static string HighlightCfg(string content)
    {
        string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        StringBuilder sb = new StringBuilder(content.Length + lines.Length * 32);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmed = line.TrimStart();

            if (trimmed.StartsWith("#") || trimmed.StartsWith(";"))
            {
                sb.Append("<color=#546E7A><i>").Append(line).Append("</i></color>");
            }
            else
            {
                Match sectionMatch = CfgSectionRegex.Match(line);
                if (sectionMatch.Success)
                {
                    sb.Append("<color=#80D8FF><b>").Append(line).Append("</b></color>");
                }
                else
                {
                    Match kvMatch = CfgKeyValueRegex.Match(line);
                    if (kvMatch.Success)
                    {
                        string key = kvMatch.Groups[1].Value;
                        string eq = kvMatch.Groups[2].Value;
                        string val = kvMatch.Groups[3].Value;

                        sb.Append("<color=#5CE1E6>").Append(key).Append("</color> ");
                        sb.Append("<color=#4A6572>").Append(eq).Append("</color> ");

                        string valTrim = val.Trim();
                        if (valTrim.Equals("true", StringComparison.OrdinalIgnoreCase) || valTrim.Equals("false", StringComparison.OrdinalIgnoreCase))
                        {
                            sb.Append("<color=#FF79C6>").Append(val).Append("</color>");
                        }
                        else if (NumberRegex.IsMatch(valTrim))
                        {
                            sb.Append("<color=#BD93F9>").Append(val).Append("</color>");
                        }
                        else
                        {
                            sb.Append("<color=#A8FF78>").Append(val).Append("</color>");
                        }
                    }
                    else
                    {
                        sb.Append(line);
                    }
                }
            }

            if (i < lines.Length - 1)
            {
                sb.Append("\n");
            }
        }

        return sb.ToString();
    }

    private static string HighlightYaml(string content)
    {
        string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        StringBuilder sb = new StringBuilder(content.Length + lines.Length * 32);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmed = line.TrimStart();

            if (trimmed.StartsWith("#"))
            {
                sb.Append("<color=#546E7A><i>").Append(line).Append("</i></color>");
            }
            else
            {
                int colonIdx = line.IndexOf(':');
                if (colonIdx > 0 && !line.StartsWith("- "))
                {
                    string key = line.Substring(0, colonIdx);
                    string rest = line.Substring(colonIdx + 1);

                    sb.Append("<color=#5CE1E6>").Append(key).Append("</color><color=#4A6572>:</color>");

                    string restTrim = rest.Trim();
                    if (restTrim.Equals("true", StringComparison.OrdinalIgnoreCase) || restTrim.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append("<color=#FF79C6>").Append(rest).Append("</color>");
                    }
                    else if (NumberRegex.IsMatch(restTrim))
                    {
                        sb.Append("<color=#BD93F9>").Append(rest).Append("</color>");
                    }
                    else if (!string.IsNullOrEmpty(restTrim))
                    {
                        sb.Append("<color=#A8FF78>").Append(rest).Append("</color>");
                    }
                    else
                    {
                        sb.Append(rest);
                    }
                }
                else
                {
                    sb.Append(line);
                }
            }

            if (i < lines.Length - 1)
            {
                sb.Append("\n");
            }
        }

        return sb.ToString();
    }

    public static void ApplyHighlightingToTextInfo(TMP_TextInfo textInfo, string rawContent, string extension)
    {
        if (textInfo == null || textInfo.characterCount == 0 || string.IsNullOrEmpty(rawContent))
        {
            return;
        }

        int totalChars = textInfo.characterCount;
        for (int i = 0; i < totalChars; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible)
            {
                continue;
            }

            int matIndex = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;

            if (matIndex >= 0 && matIndex < textInfo.meshInfo.Length && textInfo.meshInfo[matIndex].colors32 != null)
            {
                Color32[] colors = textInfo.meshInfo[matIndex].colors32;
                if (vertIndex + 3 < colors.Length)
                {
                    colors[vertIndex + 0] = ColorDefault;
                    colors[vertIndex + 1] = ColorDefault;
                    colors[vertIndex + 2] = ColorDefault;
                    colors[vertIndex + 3] = ColorDefault;
                }
            }
        }

        string ext = extension.ToLowerInvariant();
        if (ext == ".json")
        {
            HighlightJsonTokens(textInfo, rawContent);
        }
        else if (ext == ".cfg" || ext == ".ini")
        {
            HighlightCfgTokens(textInfo, rawContent);
        }
        else if (ext == ".yml" || ext == ".yaml")
        {
            HighlightYamlTokens(textInfo, rawContent);
        }
    }

    public static void ApplyHighlighting(TMP_Text textComponent, string rawContent, string extension)
    {
        if (textComponent == null || string.IsNullOrEmpty(rawContent))
        {
            return;
        }

        textComponent.ForceMeshUpdate();
        ApplyHighlightingToTextInfo(textComponent.textInfo, rawContent, extension);
        textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private static void SetColorRange(TMP_TextInfo textInfo, int start, int length, Color32 color)
    {
        int safeStart = Math.Max(0, start);
        int end = Math.Min(safeStart + length, textInfo.characterCount);
        for (int i = safeStart; i < end; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible)
            {
                continue;
            }

            int matIndex = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;

            if (matIndex >= 0 && matIndex < textInfo.meshInfo.Length && textInfo.meshInfo[matIndex].colors32 != null)
            {
                Color32[] colors = textInfo.meshInfo[matIndex].colors32;
                if (vertIndex + 3 < colors.Length)
                {
                    colors[vertIndex + 0] = color;
                    colors[vertIndex + 1] = color;
                    colors[vertIndex + 2] = color;
                    colors[vertIndex + 3] = color;
                }
            }
        }
    }

    private static void HighlightJsonTokens(TMP_TextInfo textInfo, string rawContent)
    {
        MatchCollection matches = JsonTokenRegex.Matches(rawContent);
        for (int m = 0; m < matches.Count; m++)
        {
            Match match = matches[m];
            if (match.Groups["comment"].Success)
            {
                SetColorRange(textInfo, match.Index, match.Length, ColorComment);
            }
            else if (match.Groups["string"].Success)
            {
                bool isKey = match.Groups["colon"].Success;
                SetColorRange(textInfo, match.Groups["string"].Index, match.Groups["string"].Length, isKey ? ColorKey : ColorString);
                if (isKey)
                {
                    SetColorRange(textInfo, match.Groups["colon"].Index, match.Groups["colon"].Length, ColorPunct);
                }
            }
            else if (match.Groups["number"].Success)
            {
                SetColorRange(textInfo, match.Index, match.Length, ColorNumber);
            }
            else if (match.Groups["keyword"].Success)
            {
                SetColorRange(textInfo, match.Index, match.Length, ColorKeyword);
            }
            else if (match.Groups["bracket"].Success)
            {
                SetColorRange(textInfo, match.Index, match.Length, ColorBracket);
            }
            else if (match.Groups["punct"].Success)
            {
                SetColorRange(textInfo, match.Index, match.Length, ColorPunct);
            }
        }
    }

    private static void HighlightCfgTokens(TMP_TextInfo textInfo, string rawContent)
    {
        int charIndex = 0;
        int contentLength = rawContent.Length;
        while (charIndex < contentLength)
        {
            int nextNewline = rawContent.IndexOf('\n', charIndex);
            int lineEnd = nextNewline >= 0 ? nextNewline : contentLength;
            int lineLength = lineEnd - charIndex;
            int lineStart = charIndex;
            charIndex = lineEnd + 1;

            int nonWsStart = lineStart;
            while (nonWsStart < lineEnd && (rawContent[nonWsStart] == ' ' || rawContent[nonWsStart] == '\t' || rawContent[nonWsStart] == '\r'))
            {
                nonWsStart++;
            }

            if (nonWsStart >= lineEnd)
            {
                continue;
            }

            char firstChar = rawContent[nonWsStart];
            if (firstChar == '#' || firstChar == ';')
            {
                SetColorRange(textInfo, lineStart, lineLength, ColorComment);
                continue;
            }

            int lastNonWs = lineEnd - 1;
            while (lastNonWs > nonWsStart && (rawContent[lastNonWs] == ' ' || rawContent[lastNonWs] == '\t' || rawContent[lastNonWs] == '\r'))
            {
                lastNonWs--;
            }

            if (firstChar == '[' && rawContent[lastNonWs] == ']')
            {
                SetColorRange(textInfo, nonWsStart, (lastNonWs - nonWsStart) + 1, ColorSection);
                continue;
            }

            int eqIndex = -1;
            for (int k = nonWsStart; k < lineEnd; k++)
            {
                if (rawContent[k] == '=')
                {
                    eqIndex = k;
                    break;
                }
            }

            if (eqIndex > 0)
            {
                SetColorRange(textInfo, nonWsStart, eqIndex - nonWsStart, ColorKey);
                SetColorRange(textInfo, eqIndex, 1, ColorPunct);

                int valStart = eqIndex + 1;
                while (valStart < lineEnd && rawContent[valStart] == ' ')
                {
                    valStart++;
                }

                int valLength = lineEnd - valStart;
                if (valLength > 0)
                {
                    string valStr = rawContent.Substring(valStart, valLength).TrimEnd('\r', ' ');
                    if (valStr.Equals("true", StringComparison.OrdinalIgnoreCase) || valStr.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        SetColorRange(textInfo, valStart, valLength, ColorKeyword);
                    }
                    else if (NumberRegex.IsMatch(valStr))
                    {
                        SetColorRange(textInfo, valStart, valLength, ColorNumber);
                    }
                    else
                    {
                        SetColorRange(textInfo, valStart, valLength, ColorString);
                    }
                }
            }
        }
    }

    private static void HighlightYamlTokens(TMP_TextInfo textInfo, string rawContent)
    {
        int charIndex = 0;
        int contentLength = rawContent.Length;
        while (charIndex < contentLength)
        {
            int nextNewline = rawContent.IndexOf('\n', charIndex);
            int lineEnd = nextNewline >= 0 ? nextNewline : contentLength;
            int lineLength = lineEnd - charIndex;
            int lineStart = charIndex;
            charIndex = lineEnd + 1;

            int nonWsStart = lineStart;
            while (nonWsStart < lineEnd && (rawContent[nonWsStart] == ' ' || rawContent[nonWsStart] == '\t' || rawContent[nonWsStart] == '\r'))
            {
                nonWsStart++;
            }

            if (nonWsStart >= lineEnd)
            {
                continue;
            }

            char firstChar = rawContent[nonWsStart];
            if (firstChar == '#')
            {
                SetColorRange(textInfo, lineStart, lineLength, ColorComment);
                continue;
            }

            int colonIndex = -1;
            for (int k = nonWsStart; k < lineEnd; k++)
            {
                if (rawContent[k] == ':')
                {
                    colonIndex = k;
                    break;
                }
            }

            if (colonIndex > 0)
            {
                SetColorRange(textInfo, nonWsStart, colonIndex - nonWsStart, ColorKey);
                SetColorRange(textInfo, colonIndex, 1, ColorPunct);

                int valStart = colonIndex + 1;
                while (valStart < lineEnd && rawContent[valStart] == ' ')
                {
                    valStart++;
                }

                int valLength = lineEnd - valStart;
                if (valLength > 0)
                {
                    string valStr = rawContent.Substring(valStart, valLength).TrimEnd('\r', ' ');
                    if (valStr.Equals("true", StringComparison.OrdinalIgnoreCase) || valStr.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        SetColorRange(textInfo, valStart, valLength, ColorKeyword);
                    }
                    else if (NumberRegex.IsMatch(valStr))
                    {
                        SetColorRange(textInfo, valStart, valLength, ColorNumber);
                    }
                    else if (!string.IsNullOrEmpty(valStr))
                    {
                        SetColorRange(textInfo, valStart, valLength, ColorString);
                    }
                }
            }
        }
    }
}
