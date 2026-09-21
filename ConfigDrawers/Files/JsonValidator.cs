using System;
using System.Collections.Generic;
using System.Text;

namespace BepInEx.ConfigDrawers.Files;

public static class JsonValidator
{
    private const int MaxInlineArrayLength = 80;

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

            BuildNode(json, ref index, ref line, ref col);
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

        try
        {
            int index = 0;
            int line = 1;
            int col = 1;

            SkipWhitespace(json, ref index, ref line, ref col);
            if (index >= json.Length)
            {
                return json;
            }

            JsonNode root = BuildNode(json, ref index, ref line, ref col);
            StringBuilder sb = new StringBuilder(json.Length + 64);
            root.Format(sb, 0);
            return sb.ToString();
        }
        catch
        {
            return json;
        }
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

    private static JsonNode BuildNode(string text, ref int index, ref int line, ref int col)
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
                return BuildObject(text, ref index, ref line, ref col);
            case '[':
                return BuildArray(text, ref index, ref line, ref col);
            case '"':
                return new JsonPrimitive(ExtractString(text, ref index, ref line, ref col));
            case 't':
            case 'f':
                return new JsonPrimitive(ExtractBoolean(text, ref index, ref line, ref col));
            case 'n':
                return new JsonPrimitive(ExtractNull(text, ref index, ref line, ref col));
            default:
                if (c == '-' || (c >= '0' && c <= '9'))
                {
                    return new JsonPrimitive(ExtractNumber(text, ref index, ref line, ref col));
                }
                throw new JsonParseException($"Unexpected token '{c}'", line, col);
        }
    }

    private static JsonObject BuildObject(string text, ref int index, ref int line, ref int col)
    {
        index++;
        col++;
        SkipWhitespace(text, ref index, ref line, ref col);

        JsonObject obj = new JsonObject();
        if (index < text.Length && text[index] == '}')
        {
            index++;
            col++;
            return obj;
        }

        while (index < text.Length)
        {
            SkipWhitespace(text, ref index, ref line, ref col);
            if (index >= text.Length || text[index] != '"')
            {
                throw new JsonParseException("Expected string key in object", line, col);
            }

            string key = ExtractString(text, ref index, ref line, ref col);
            SkipWhitespace(text, ref index, ref line, ref col);

            if (index >= text.Length || text[index] != ':')
            {
                throw new JsonParseException("Expected ':' after property name", line, col);
            }

            index++;
            col++;
            JsonNode value = BuildNode(text, ref index, ref line, ref col);
            obj.Properties.Add(new KeyValuePair<string, JsonNode>(key, value));

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
                return obj;
            }

            throw new JsonParseException("Expected ',' or '}' in object", line, col);
        }

        throw new JsonParseException("Unterminated object: missing '}'", line, col);
    }

    private static JsonArray BuildArray(string text, ref int index, ref int line, ref int col)
    {
        index++;
        col++;
        SkipWhitespace(text, ref index, ref line, ref col);

        JsonArray arr = new JsonArray();
        if (index < text.Length && text[index] == ']')
        {
            index++;
            col++;
            return arr;
        }

        while (index < text.Length)
        {
            JsonNode item = BuildNode(text, ref index, ref line, ref col);
            arr.Elements.Add(item);

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
                return arr;
            }

            throw new JsonParseException("Expected ',' or ']' in array", line, col);
        }

        throw new JsonParseException("Unterminated array: missing ']'", line, col);
    }

    private static string ExtractString(string text, ref int index, ref int line, ref int col)
    {
        int start = index;
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
                return text.Substring(start, index - start);
            }
        }

        throw new JsonParseException("Unterminated string literal", line, col);
    }

    private static string ExtractNumber(string text, ref int index, ref int line, ref int col)
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

        return numStr;
    }

    private static string ExtractBoolean(string text, ref int index, ref int line, ref int col)
    {
        if (index + 4 <= text.Length && text.Substring(index, 4) == "true")
        {
            index += 4;
            col += 4;
            return "true";
        }

        if (index + 5 <= text.Length && text.Substring(index, 5) == "false")
        {
            index += 5;
            col += 5;
            return "false";
        }

        throw new JsonParseException("Invalid boolean value", line, col);
    }

    private static string ExtractNull(string text, ref int index, ref int line, ref int col)
    {
        if (index + 4 <= text.Length && text.Substring(index, 4) == "null")
        {
            index += 4;
            col += 4;
            return "null";
        }

        throw new JsonParseException("Invalid null value", line, col);
    }

    private abstract class JsonNode
    {
        public abstract void Format(StringBuilder sb, int indent);
    }

    private class JsonPrimitive : JsonNode
    {
        public string RawText { get; }

        public JsonPrimitive(string rawText)
        {
            RawText = rawText;
        }

        public override void Format(StringBuilder sb, int indent)
        {
            sb.Append(RawText);
        }
    }

    private class JsonObject : JsonNode
    {
        public List<KeyValuePair<string, JsonNode>> Properties { get; } = new List<KeyValuePair<string, JsonNode>>();

        public override void Format(StringBuilder sb, int indent)
        {
            if (Properties.Count == 0)
            {
                sb.Append("{}");
                return;
            }

            sb.Append("{\n");
            int nextIndent = indent + 1;
            for (int i = 0; i < Properties.Count; i++)
            {
                KeyValuePair<string, JsonNode> prop = Properties[i];
                AppendIndent(sb, nextIndent);
                sb.Append(prop.Key);
                sb.Append(": ");
                prop.Value.Format(sb, nextIndent);

                if (i < Properties.Count - 1)
                {
                    sb.Append(',');
                }
                sb.Append('\n');
            }

            AppendIndent(sb, indent);
            sb.Append('}');
        }
    }

    private class JsonArray : JsonNode
    {
        public List<JsonNode> Elements { get; } = new List<JsonNode>();

        public bool CanFormatInline()
        {
            if (Elements.Count == 0)
            {
                return true;
            }

            int totalLength = 2;
            for (int i = 0; i < Elements.Count; i++)
            {
                if (Elements[i] is not JsonPrimitive prim)
                {
                    return false;
                }

                if (prim.RawText.IndexOf('\n') >= 0 || prim.RawText.IndexOf('\r') >= 0)
                {
                    return false;
                }

                totalLength += prim.RawText.Length;
                if (i < Elements.Count - 1)
                {
                    totalLength += 2;
                }

                if (totalLength > MaxInlineArrayLength)
                {
                    return false;
                }
            }

            return true;
        }

        public override void Format(StringBuilder sb, int indent)
        {
            if (Elements.Count == 0)
            {
                sb.Append("[]");
                return;
            }

            if (CanFormatInline())
            {
                sb.Append('[');
                for (int i = 0; i < Elements.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }
                    Elements[i].Format(sb, indent);
                }
                sb.Append(']');
                return;
            }

            sb.Append("[\n");
            int nextIndent = indent + 1;
            for (int i = 0; i < Elements.Count; i++)
            {
                AppendIndent(sb, nextIndent);
                Elements[i].Format(sb, nextIndent);

                if (i < Elements.Count - 1)
                {
                    sb.Append(',');
                }
                sb.Append('\n');
            }

            AppendIndent(sb, indent);
            sb.Append(']');
        }
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
