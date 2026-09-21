using System;
using System.Text;

namespace BepInEx.ConfigDrawers.Files;

public static class JsonValidator
{
    public readonly struct ValidationResult
    {
        public bool IsValid { get; }
        public string? ErrorMessage { get; }
        public int ErrorLine { get; }
        public int ErrorColumn { get; }

        public ValidationResult(bool isValid, string? errorMessage = null, int errorLine = 0, int errorColumn = 0)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            ErrorLine = errorLine;
            ErrorColumn = errorColumn;
        }
    }

    public static ValidationResult Validate(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ValidationResult(false, "Empty JSON document", 1, 1);
        }

        int index = 0;
        int line = 1;
        int col = 1;

        try
        {
            SkipWhitespace(json!, ref index, ref line, ref col);
            if (index >= json!.Length)
            {
                return new ValidationResult(false, "Empty JSON document", line, col);
            }

            ParseValue(json, ref index, ref line, ref col);
            SkipWhitespace(json, ref index, ref line, ref col);

            if (index < json.Length)
            {
                return new ValidationResult(false, $"Unexpected token '{json[index]}' after root JSON element", line, col);
            }

            return new ValidationResult(true);
        }
        catch (JsonParseException ex)
        {
            return new ValidationResult(false, ex.Message, ex.Line, ex.Column);
        }
        catch (Exception ex)
        {
            return new ValidationResult(false, ex.Message, line, col);
        }
    }

    public static string FormatJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        StringBuilder sb = new StringBuilder(json.Length + 64);
        bool inQuotes = false;
        bool isEscaped = false;
        int indent = 0;

        for (int i = 0; i < json.Length; i++)
        {
            char ch = json[i];

            if (isEscaped)
            {
                sb.Append(ch);
                isEscaped = false;
                continue;
            }

            if (ch == '\\')
            {
                isEscaped = true;
                sb.Append(ch);
                continue;
            }

            if (ch == '"')
            {
                inQuotes = !inQuotes;
                sb.Append(ch);
                continue;
            }

            if (inQuotes)
            {
                sb.Append(ch);
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                continue;
            }

            switch (ch)
            {
                case '{':
                case '[':
                    sb.Append(ch);
                    if (i + 1 < json.Length && ((ch == '{' && json[i + 1] == '}') || (ch == '[' && json[i + 1] == ']')))
                    {
                        sb.Append(json[i + 1]);
                        i++;
                    }
                    else
                    {
                        indent++;
                        sb.AppendLine();
                        AppendIndent(sb, indent);
                    }
                    break;

                case '}':
                case ']':
                    indent = Math.Max(0, indent - 1);
                    sb.AppendLine();
                    AppendIndent(sb, indent);
                    sb.Append(ch);
                    break;

                case ',':
                    sb.Append(ch);
                    sb.AppendLine();
                    AppendIndent(sb, indent);
                    break;

                case ':':
                    sb.Append(": ");
                    break;

                default:
                    sb.Append(ch);
                    break;
            }
        }

        return sb.ToString();
    }

    private static void AppendIndent(StringBuilder sb, int indent)
    {
        sb.Append(' ', indent * 2);
    }

    private static void SkipWhitespace(string text, ref int index, ref int line, ref int col)
    {
        while (index < text.Length)
        {
            char c = text[index];
            if (c == '\n')
            {
                line++;
                col = 1;
                index++;
            }
            else if (c == '\r')
            {
                index++;
            }
            else if (char.IsWhiteSpace(c))
            {
                col++;
                index++;
            }
            else
            {
                break;
            }
        }
    }

    private static void ParseValue(string text, ref int index, ref int line, ref int col)
    {
        SkipWhitespace(text, ref index, ref line, ref col);
        if (index >= text.Length)
        {
            throw new JsonParseException("Unexpected end of JSON input", line, col);
        }

        char c = text[index];
        switch (c)
        {
            case '{':
                ParseObject(text, ref index, ref line, ref col);
                break;
            case '[':
                ParseArray(text, ref index, ref line, ref col);
                break;
            case '"':
                ParseString(text, ref index, ref line, ref col);
                break;
            case 't':
            case 'f':
                ParseBoolean(text, ref index, ref line, ref col);
                break;
            case 'n':
                ParseNull(text, ref index, ref line, ref col);
                break;
            default:
                if (c == '-' || (c >= '0' && c <= '9'))
                {
                    ParseNumber(text, ref index, ref line, ref col);
                }
                else
                {
                    throw new JsonParseException($"Unexpected token '{c}'", line, col);
                }
                break;
        }
    }

    private static void ParseObject(string text, ref int index, ref int line, ref int col)
    {
        index++;
        col++;
        SkipWhitespace(text, ref index, ref line, ref col);

        if (index < text.Length && text[index] == '}')
        {
            index++;
            col++;
            return;
        }

        while (index < text.Length)
        {
            SkipWhitespace(text, ref index, ref line, ref col);
            if (index >= text.Length || text[index] != '"')
            {
                throw new JsonParseException("Expected string key in object", line, col);
            }

            ParseString(text, ref index, ref line, ref col);
            SkipWhitespace(text, ref index, ref line, ref col);

            if (index >= text.Length || text[index] != ':')
            {
                throw new JsonParseException("Expected ':' after property name", line, col);
            }

            index++;
            col++;
            ParseValue(text, ref index, ref line, ref col);
            SkipWhitespace(text, ref index, ref line, ref col);

            if (index < text.Length && text[index] == ',')
            {
                index++;
                col++;
                continue;
            }

            if (index < text.Length && text[index] == '}')
            {
                index++;
                col++;
                return;
            }

            throw new JsonParseException("Expected ',' or '}' in object", line, col);
        }

        throw new JsonParseException("Unterminated object: missing '}'", line, col);
    }

    private static void ParseArray(string text, ref int index, ref int line, ref int col)
    {
        index++;
        col++;
        SkipWhitespace(text, ref index, ref line, ref col);

        if (index < text.Length && text[index] == ']')
        {
            index++;
            col++;
            return;
        }

        while (index < text.Length)
        {
            ParseValue(text, ref index, ref line, ref col);
            SkipWhitespace(text, ref index, ref line, ref col);

            if (index < text.Length && text[index] == ',')
            {
                index++;
                col++;
                continue;
            }

            if (index < text.Length && text[index] == ']')
            {
                index++;
                col++;
                return;
            }

            throw new JsonParseException("Expected ',' or ']' in array", line, col);
        }

        throw new JsonParseException("Unterminated array: missing ']'", line, col);
    }

    private static void ParseString(string text, ref int index, ref int line, ref int col)
    {
        index++;
        col++;
        bool escaped = false;

        while (index < text.Length)
        {
            char c = text[index];
            index++;
            col++;

            if (c == '\n')
            {
                line++;
                col = 1;
            }

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                return;
            }
        }

        throw new JsonParseException("Unterminated string literal", line, col);
    }

    private static void ParseNumber(string text, ref int index, ref int line, ref int col)
    {
        int start = index;
        if (text[index] == '-')
        {
            index++;
            col++;
        }

        while (index < text.Length && (char.IsDigit(text[index]) || text[index] == '.' || text[index] == 'e' || text[index] == 'E' || text[index] == '+' || text[index] == '-'))
        {
            index++;
            col++;
        }

        string numStr = text.Substring(start, index - start);
        if (!double.TryParse(numStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
        {
            throw new JsonParseException($"Invalid number '{numStr}'", line, col);
        }
    }

    private static void ParseBoolean(string text, ref int index, ref int line, ref int col)
    {
        if (index + 4 <= text.Length && text.Substring(index, 4) == "true")
        {
            index += 4;
            col += 4;
            return;
        }

        if (index + 5 <= text.Length && text.Substring(index, 5) == "false")
        {
            index += 5;
            col += 5;
            return;
        }

        throw new JsonParseException("Invalid boolean value", line, col);
    }

    private static void ParseNull(string text, ref int index, ref int line, ref int col)
    {
        if (index + 4 <= text.Length && text.Substring(index, 4) == "null")
        {
            index += 4;
            col += 4;
            return;
        }

        throw new JsonParseException("Invalid null value", line, col);
    }

    private class JsonParseException : Exception
    {
        public int Line { get; }
        public int Column { get; }

        public JsonParseException(string message, int line, int column)
            : base(message)
        {
            Line = line;
            Column = column;
        }
    }
}
