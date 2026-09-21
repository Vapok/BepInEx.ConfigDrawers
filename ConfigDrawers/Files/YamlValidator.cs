using System;
using System.Collections.Generic;

namespace BepInEx.ConfigDrawers.Files;

public static class YamlValidator
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

    private enum BlockType
    {
        Root,
        Mapping,
        Sequence
    }

    private sealed class BlockFrame
    {
        public int Indent { get; }
        public BlockType Type { get; set; }
        public HashSet<string> Keys { get; } = new(StringComparer.Ordinal);
        public bool ExpectingChild { get; set; }

        public BlockFrame(int indent, BlockType type)
        {
            Indent = indent;
            Type = type;
        }
    }

    private sealed class FlowFrame
    {
        public char OpenChar { get; }
        public int Line { get; }
        public int Column { get; }

        public FlowFrame(char openChar, int line, int column)
        {
            OpenChar = openChar;
            Line = line;
            Column = column;
        }
    }

    public static ValidationResult Validate(string? yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return new ValidationResult(true);
        }

        string[] rawLines = yaml!.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        Stack<BlockFrame> blockStack = new();
        blockStack.Push(new BlockFrame(-1, BlockType.Root));

        Stack<FlowFrame> flowStack = new();

        bool inBlockScalar = false;
        int blockScalarIndent = -1;
        char inMultiLineQuote = '\0';
        int quoteStartLine = 0;
        int quoteStartCol = 0;

        for (int lineIdx = 0; lineIdx < rawLines.Length; lineIdx++)
        {
            int lineNum = lineIdx + 1;
            string line = rawLines[lineIdx];

            if (inMultiLineQuote != '\0')
            {
                int closeIdx = FindClosingQuote(line, 0, inMultiLineQuote);
                if (closeIdx >= 0)
                {
                    inMultiLineQuote = '\0';
                    line = line.Substring(closeIdx + 1);
                }
                else
                {
                    continue;
                }
            }

            if (inBlockScalar)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                int indent = GetLeadingSpaces(line, lineNum, out string? tabError, out int tabCol);
                if (tabError != null)
                {
                    return new ValidationResult(false, tabError, lineNum, tabCol);
                }

                if (blockScalarIndent == -1)
                {
                    BlockFrame top = blockStack.Peek();
                    if (indent > top.Indent)
                    {
                        blockScalarIndent = indent;
                        continue;
                    }
                    else
                    {
                        inBlockScalar = false;
                    }
                }
                else
                {
                    if (indent >= blockScalarIndent)
                    {
                        continue;
                    }
                    else
                    {
                        inBlockScalar = false;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            int lineIndent = GetLeadingSpaces(line, lineNum, out string? leadTabErr, out int leadTabCol);
            if (leadTabErr != null)
            {
                return new ValidationResult(false, leadTabErr, lineNum, leadTabCol);
            }

            int contentStart = lineIndent;
            string trimmed = line.Substring(contentStart);

            if (trimmed.StartsWith("#"))
            {
                continue;
            }

            if (trimmed.StartsWith("%"))
            {
                if (blockStack.Count > 1)
                {
                    return new ValidationResult(false, "YAML directives must appear at document root", lineNum, contentStart + 1);
                }
                continue;
            }

            if (trimmed == "---" || trimmed.StartsWith("--- ") || trimmed == "..." || trimmed.StartsWith("... "))
            {
                blockStack.Clear();
                blockStack.Push(new BlockFrame(-1, BlockType.Root));
                flowStack.Clear();
                inBlockScalar = false;
                blockScalarIndent = -1;

                if (trimmed.Length > 3 && trimmed.StartsWith("--- "))
                {
                    trimmed = trimmed.Substring(4).TrimStart();
                    contentStart = line.Length - trimmed.Length;
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }
            }

            if (flowStack.Count > 0)
            {
                ValidationResult flowScanRes = ScanFlowTokens(line, contentStart, lineNum, flowStack, ref inMultiLineQuote, ref quoteStartLine, ref quoteStartCol);
                if (!flowScanRes.IsValid)
                {
                    return flowScanRes;
                }
                continue;
            }

            bool isSequenceItem = line[contentStart] == '-' && (contentStart + 1 >= line.Length || char.IsWhiteSpace(line[contentStart + 1]));

            if (isSequenceItem && blockStack.Peek().ExpectingChild && lineIndent == blockStack.Peek().Indent)
            {
                blockStack.Peek().ExpectingChild = false;
                BlockFrame compactSeq = new(lineIndent, BlockType.Sequence);
                blockStack.Push(compactSeq);
            }
            else if (!isSequenceItem && blockStack.Count > 1 && blockStack.Peek().Type == BlockType.Sequence && blockStack.Peek().Indent == lineIndent)
            {
                blockStack.Pop();
            }

            BlockFrame current = blockStack.Peek();

            if (lineIndent > current.Indent)
            {
                if (!current.ExpectingChild && current.Type != BlockType.Root)
                {
                    return new ValidationResult(false, $"Unexpected indentation level of {lineIndent} spaces under scalar value", lineNum, contentStart + 1);
                }

                BlockFrame newFrame = new(lineIndent, isSequenceItem ? BlockType.Sequence : BlockType.Root);
                blockStack.Push(newFrame);
                current.ExpectingChild = false;
                current = newFrame;
            }
            else if (lineIndent < current.Indent)
            {
                while (blockStack.Count > 1 && blockStack.Peek().Indent > lineIndent)
                {
                    blockStack.Pop();
                }

                current = blockStack.Peek();
                if (current.Indent != lineIndent && current.Indent != -1)
                {
                    return new ValidationResult(false, $"Indentation level of {lineIndent} spaces does not match any outer indentation level (expected {current.Indent})", lineNum, contentStart + 1);
                }

                if (!isSequenceItem && blockStack.Count > 1 && blockStack.Peek().Type == BlockType.Sequence && blockStack.Peek().Indent == lineIndent)
                {
                    blockStack.Pop();
                    current = blockStack.Peek();
                }
            }

            int cursor = contentStart;

            if (isSequenceItem)
            {
                cursor++;
                if (cursor < line.Length && line[cursor] == ' ')
                {
                    cursor++;
                }

                if (current.Type == BlockType.Mapping)
                {
                    return new ValidationResult(false, "Sequence item '-' cannot appear directly inside a mapping at the same indentation level", lineNum, contentStart + 1);
                }
                current.Type = BlockType.Sequence;

                while (cursor < line.Length && line[cursor] == ' ')
                {
                    cursor++;
                }

                if (cursor >= line.Length || line[cursor] == '#')
                {
                    current.ExpectingChild = true;
                    continue;
                }
            }

            int colonIdx = FindMappingColon(line, cursor);
            if (colonIdx >= 0)
            {
                if (current.Type == BlockType.Sequence && !isSequenceItem)
                {
                    return new ValidationResult(false, "Mapping key cannot appear directly inside a sequence without a '-' indicator", lineNum, cursor + 1);
                }

                BlockFrame targetFrame;
                if (isSequenceItem)
                {
                    targetFrame = new BlockFrame(cursor, BlockType.Mapping);
                    blockStack.Push(targetFrame);
                }
                else
                {
                    current.Type = BlockType.Mapping;
                    targetFrame = current;
                }

                string keyPart = line.Substring(cursor, colonIdx - cursor).Trim();
                int keyCol = cursor + 1;

                string cleanKey = UnquoteString(keyPart);
                if (string.IsNullOrEmpty(cleanKey))
                {
                    return new ValidationResult(false, "Empty mapping key", lineNum, keyCol);
                }

                targetFrame.Keys.Add(cleanKey);

                int valStart = colonIdx + 1;
                while (valStart < line.Length && (line[valStart] == ' ' || line[valStart] == '\t'))
                {
                    valStart++;
                }

                if (valStart >= line.Length || line[valStart] == '#')
                {
                    targetFrame.ExpectingChild = true;
                    continue;
                }

                string valPart = line.Substring(valStart).TrimStart();

                if (valPart.StartsWith("|") || valPart.StartsWith(">"))
                {
                    inBlockScalar = true;
                    blockScalarIndent = -1;
                    continue;
                }

                ValidationResult valScanRes = ScanFlowTokens(line, valStart, lineNum, flowStack, ref inMultiLineQuote, ref quoteStartLine, ref quoteStartCol);
                if (!valScanRes.IsValid)
                {
                    return valScanRes;
                }

                targetFrame.ExpectingChild = false;
            }
            else
            {
                if (isSequenceItem)
                {
                    ValidationResult itemScan = ScanFlowTokens(line, cursor, lineNum, flowStack, ref inMultiLineQuote, ref quoteStartLine, ref quoteStartCol);
                    if (!itemScan.IsValid)
                    {
                        return itemScan;
                    }
                    current.ExpectingChild = false;
                }
                else
                {
                    if (current.Type == BlockType.Mapping)
                    {
                        return new ValidationResult(false, $"Expected mapping key with ':' but found bare scalar '{line.Substring(cursor).Trim()}'", lineNum, cursor + 1);
                    }

                    ValidationResult scalarScan = ScanFlowTokens(line, cursor, lineNum, flowStack, ref inMultiLineQuote, ref quoteStartLine, ref quoteStartCol);
                    if (!scalarScan.IsValid)
                    {
                        return scalarScan;
                    }
                }
            }
        }

        if (inMultiLineQuote != '\0')
        {
            string qName = inMultiLineQuote == '\'' ? "single quote" : "double quote";
            return new ValidationResult(false, $"Unclosed {qName} opened on line {quoteStartLine}", quoteStartLine, quoteStartCol);
        }

        if (flowStack.Count > 0)
        {
            FlowFrame openF = flowStack.Pop();
            return new ValidationResult(false, $"Unclosed '{openF.OpenChar}' in flow collection", openF.Line, openF.Column);
        }

        return new ValidationResult(true);
    }

    private static int GetLeadingSpaces(string line, int lineNum, out string? error, out int errorCol)
    {
        error = null;
        errorCol = 0;
        int count = 0;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == ' ')
            {
                count++;
            }
            else if (c == '\t')
            {
                error = "Tabs are not allowed for indentation in YAML";
                errorCol = i + 1;
                return 0;
            }
            else
            {
                break;
            }
        }
        return count;
    }

    private static int FindMappingColon(string line, int start)
    {
        bool inDouble = false;
        bool inSingle = false;

        for (int i = start; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"' && !inSingle)
            {
                if (i == 0 || line[i - 1] != '\\')
                {
                    inDouble = !inDouble;
                }
            }
            else if (c == '\'' && !inDouble)
            {
                if (!inSingle)
                {
                    if (i == start || char.IsWhiteSpace(line[i - 1]) || line[i - 1] is '[' or '{' or ',' or ':')
                    {
                        inSingle = true;
                    }
                }
                else
                {
                    if (i + 1 < line.Length && line[i + 1] == '\'')
                    {
                        i++;
                    }
                    else
                    {
                        inSingle = false;
                    }
                }
            }
            else if (!inDouble && !inSingle)
            {
                if (c == '#')
                {
                    return -1;
                }

                if (c == ':')
                {
                    if (i + 1 >= line.Length || line[i + 1] == ' ' || line[i + 1] == '\t' || line[i + 1] == '\r' || line[i + 1] == '\n')
                    {
                        return i;
                    }
                }
            }
        }

        return -1;
    }

    private static int FindClosingQuote(string line, int start, char quoteChar)
    {
        for (int i = start; i < line.Length; i++)
        {
            char c = line[i];
            if (quoteChar == '"')
            {
                if (c == '"' && (i == 0 || line[i - 1] != '\\'))
                {
                    return i;
                }
            }
            else if (quoteChar == '\'')
            {
                if (c == '\'')
                {
                    if (i + 1 < line.Length && line[i + 1] == '\'')
                    {
                        i++;
                        continue;
                    }
                    return i;
                }
            }
        }
        return -1;
    }

    private static ValidationResult ScanFlowTokens(
        string line,
        int start,
        int lineNum,
        Stack<FlowFrame> flowStack,
        ref char inMultiLineQuote,
        ref int quoteStartLine,
        ref int quoteStartCol)
    {
        for (int i = start; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '#')
            {
                break;
            }

            if (c == '"')
            {
                quoteStartLine = lineNum;
                quoteStartCol = i + 1;
                int closeIdx = FindClosingQuote(line, i + 1, '"');
                if (closeIdx >= 0)
                {
                    i = closeIdx;
                }
                else
                {
                    inMultiLineQuote = '"';
                    return new ValidationResult(true);
                }
            }
            else if (c == '\'')
            {
                if (i == start || char.IsWhiteSpace(line[i - 1]) || line[i - 1] is '[' or '{' or ',' or ':')
                {
                    quoteStartLine = lineNum;
                    quoteStartCol = i + 1;
                    int closeIdx = FindClosingQuote(line, i + 1, '\'');
                    if (closeIdx >= 0)
                    {
                        i = closeIdx;
                    }
                    else
                    {
                        inMultiLineQuote = '\'';
                        return new ValidationResult(true);
                    }
                }
            }
            else if (c == '[' || c == '{')
            {
                flowStack.Push(new FlowFrame(c, lineNum, i + 1));
            }
            else if (c == ']' || c == '}')
            {
                if (flowStack.Count == 0)
                {
                    return new ValidationResult(false, $"Unexpected closing '{c}' with no matching opening bracket", lineNum, i + 1);
                }

                FlowFrame open = flowStack.Pop();
                if ((c == ']' && open.OpenChar != '[') || (c == '}' && open.OpenChar != '{'))
                {
                    return new ValidationResult(false, $"Mismatched closing bracket '{c}' (expected matching '{MatchingClose(open.OpenChar)}')", lineNum, i + 1);
                }
            }
        }

        return new ValidationResult(true);
    }

    private static char MatchingClose(char open)
    {
        return open switch
        {
            '[' => ']',
            '{' => '}',
            '(' => ')',
            _ => '?'
        };
    }

    private static string UnquoteString(string raw)
    {
        string s = raw.Trim();
        if (s.Length >= 2 && ((s.StartsWith("\"") && s.EndsWith("\"")) || (s.StartsWith("'") && s.EndsWith("'"))))
        {
            return s.Substring(1, s.Length - 2);
        }
        return s;
    }
}
