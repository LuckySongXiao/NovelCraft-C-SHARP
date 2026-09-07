using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using NovelManagement.AI.Utilities;

namespace NovelManagement.WPF.Services;

internal static class AiAutoFillFormatter
{
    private static readonly string[] NoisePrefixes =
    {
        "思考过程", "推理过程", "分析过程", "推理", "分析", "思路", "说明", "补充说明",
        "输出说明", "下面是", "以下是", "好的", "当然", "让我", "首先", "其次", "最后"
    };

    public static string Normalize(string? rawContent)
    {
        var content = AIOutputSanitizer.ExtractCleanOutput(rawContent);
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var lines = content.Replace("\r\n", "\n").Split('\n');
        var cleanedLines = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = NormalizeLine(rawLine);
            if (string.IsNullOrWhiteSpace(line))
            {
                if (cleanedLines.Count > 0 && cleanedLines[^1].Length > 0)
                {
                    cleanedLines.Add(string.Empty);
                }

                continue;
            }

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                continue;
            }

            if (IsNoiseLine(line))
            {
                continue;
            }

            cleanedLines.Add(line);
        }

        while (cleanedLines.Count > 0 && cleanedLines[^1].Length == 0)
        {
            cleanedLines.RemoveAt(cleanedLines.Count - 1);
        }

        return string.Join(Environment.NewLine, cleanedLines).Trim();
    }

    public static string ExtractSection(string? rawContent, params string[] headings)
    {
        var content = Normalize(rawContent);
        if (string.IsNullOrWhiteSpace(content) || headings.Length == 0)
        {
            return string.Empty;
        }

        var lines = content.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var currentLine = lines[i].Trim();
            if (!TryMatchHeading(currentLine, headings, out var inlineValue))
            {
                continue;
            }

            var sectionLines = new List<string>();
            if (!string.IsNullOrWhiteSpace(inlineValue))
            {
                sectionLines.Add(inlineValue);
            }

            for (var lineIndex = i + 1; lineIndex < lines.Length; lineIndex++)
            {
                var nextLine = lines[lineIndex].Trim();
                if (string.IsNullOrWhiteSpace(nextLine))
                {
                    if (sectionLines.Count > 0)
                    {
                        break;
                    }

                    continue;
                }

                if (LooksLikeHeading(nextLine) || LooksLikeInlineHeadingWithValue(nextLine))
                {
                    break;
                }

                sectionLines.Add(nextLine);
            }

            return string.Join(Environment.NewLine, sectionLines).Trim();
        }

        return string.Empty;
    }

    public static string ExtractSummary(string? rawContent, params string[] preferredHeadings)
    {
        var section = ExtractSection(rawContent, preferredHeadings);
        if (!string.IsNullOrWhiteSpace(section))
        {
            return section;
        }

        var content = Normalize(rawContent);
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var paragraphs = content
            .Split(new[] { $"{Environment.NewLine}{Environment.NewLine}", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(paragraph => paragraph.Trim())
            .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph) && !LooksLikeHeading(paragraph))
            .ToList();

        return paragraphs.FirstOrDefault() ?? content;
    }

    public static string? ExtractFirstMeaningfulLine(string? rawContent)
    {
        var content = Normalize(rawContent);
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        return content.Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line =>
                !string.IsNullOrWhiteSpace(line) &&
                !LooksLikeHeading(line) &&
                !LooksLikeInlineHeadingWithValue(line));
    }

    public static string CleanFieldValue(string? value, params string[] headings)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cleaned = NormalizeLine(value);
        foreach (var heading in headings.Where(heading => !string.IsNullOrWhiteSpace(heading)))
        {
            cleaned = Regex.Replace(
                cleaned,
                $"^(?:【)?{Regex.Escape(heading)}(?:】)?\\s*[:：]?\\s*",
                string.Empty,
                RegexOptions.IgnoreCase);
        }

        return cleaned.Trim('•', '-', ' ', ':', '：', '【', '】');
    }

    public static string ExtractSingleLineValue(string? value, params string[] headings)
    {
        var cleaned = CleanFieldValue(value, headings);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        return cleaned
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeLine)
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line))?
            .Trim() ?? string.Empty;
    }

    public static bool HasFieldPrefix(string? value, params string[] headings)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = NormalizeLine(value);
        return headings
            .Where(heading => !string.IsNullOrWhiteSpace(heading))
            .Any(heading => Regex.IsMatch(
                normalized,
                $"^(?:【)?{Regex.Escape(heading)}(?:】)?\\s*[:：]",
                RegexOptions.IgnoreCase));
    }

    public static void FillIfEmpty(TextBox textBox, string? value)
    {
        if (textBox == null || !string.IsNullOrWhiteSpace(textBox.Text))
        {
            return;
        }

        var normalized = Normalize(value);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            textBox.Text = normalized;
        }
    }

    public static void FillSectionIfEmpty(TextBox textBox, string? rawContent, params string[] headings)
    {
        if (textBox == null || !string.IsNullOrWhiteSpace(textBox.Text))
        {
            return;
        }

        var section = ExtractSection(rawContent, headings);
        if (!string.IsNullOrWhiteSpace(section))
        {
            textBox.Text = section;
        }
    }

    private static string NormalizeLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return string.Empty;
        }

        var normalized = line.Trim();
        normalized = normalized.TrimStart('#').Trim();
        normalized = Regex.Replace(normalized, @"^\d+[\.\)、]\s*", string.Empty);
        normalized = Regex.Replace(normalized, @"^[\-\*•]+\s*", string.Empty);
        return normalized.Trim();
    }

    private static bool IsNoiseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return true;
        }

        if (line.All(ch => ch is '-' or '*' or '=' or '#' or '`' or '~'))
        {
            return true;
        }

        return NoisePrefixes.Any(prefix =>
            line.StartsWith(prefix + "：", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith(prefix + ":", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(line, prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryMatchHeading(string line, IEnumerable<string> headings, out string inlineValue)
    {
        inlineValue = string.Empty;
        foreach (var heading in headings.Where(heading => !string.IsNullOrWhiteSpace(heading)))
        {
            foreach (var marker in new[] { $"{heading}：", $"{heading}:", $"【{heading}】", $"{heading} ", $"{heading}\t" })
            {
                if (line.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
                {
                    inlineValue = line[marker.Length..].Trim();
                    return true;
                }
            }

            if (string.Equals(line.Trim('【', '】'), heading, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool LooksLikeHeading(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        if (Regex.IsMatch(line, @"^【[^】]{1,20}】$"))
        {
            return true;
        }

        if (Regex.IsMatch(line, @"^[\u4e00-\u9fa5A-Za-z0-9_（）()\-]{1,20}\s*[:：]\s*$"))
        {
            return true;
        }

        return false;
    }

    private static bool LooksLikeInlineHeadingWithValue(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        return Regex.IsMatch(
            line,
            @"^(?:【)?[\u4e00-\u9fa5A-Za-z0-9_（）()\-]{1,20}(?:】)?\s*[:：]\s*\S+");
    }
}
