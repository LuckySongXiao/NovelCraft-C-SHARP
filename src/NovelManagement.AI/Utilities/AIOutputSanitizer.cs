using System.Text.Json;
using System.Text.RegularExpressions;

namespace NovelManagement.AI.Utilities
{
    /// <summary>
    /// 统一清理各模型返回结果中的思维链、代码围栏和常见 JSON 包装。
    /// </summary>
    public static class AIOutputSanitizer
    {
        private static readonly string[] DefaultJsonKeys =
        {
            "content",
            "text",
            "output",
            "answer",
            "result",
            "final_answer",
            "final",
            "message"
        };

        public static string ExtractCleanOutput(string? rawContent, params string[] preferredJsonKeys)
        {
            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return string.Empty;
            }

            var content = rawContent.Trim();
            content = TryUnwrapCodeFence(content);
            content = TryExtractFromJson(content, preferredJsonKeys);
            content = UnescapeCommonSequences(content);
            content = StripThinkingBlocks(content);
            content = StripPolitenessPreamble(content);

            return content.Trim();
        }

        public static string ExtractVisibleContent(string? visibleContent, string? reasoningContent = null)
        {
            var sanitizedVisibleContent = ExtractCleanOutput(visibleContent);
            if (!string.IsNullOrWhiteSpace(sanitizedVisibleContent))
            {
                return sanitizedVisibleContent;
            }

            // 明确区分：允许模型进行 thinking，但 reasoning 绝不作为最终可见输出。
            return string.Empty;
        }

        private static string TryUnwrapCodeFence(string content)
        {
            var match = Regex.Match(
                content,
                @"^\s*```(?:json|text|markdown|md)?\s*([\s\S]*?)\s*```\s*$",
                RegexOptions.IgnoreCase);

            return match.Success ? match.Groups[1].Value.Trim() : content;
        }

        private static string TryExtractFromJson(string content, params string[] preferredJsonKeys)
        {
            if (!(content.StartsWith("{", StringComparison.Ordinal) && content.EndsWith("}", StringComparison.Ordinal)) &&
                !(content.StartsWith("[", StringComparison.Ordinal) && content.EndsWith("]", StringComparison.Ordinal)))
            {
                return content;
            }

            try
            {
                using var jsonDoc = JsonDocument.Parse(content);
                var extracted = ExtractFromJsonElement(jsonDoc.RootElement, preferredJsonKeys);
                return string.IsNullOrWhiteSpace(extracted) ? content : extracted;
            }
            catch
            {
                return content;
            }
        }

        private static string? ExtractFromJsonElement(JsonElement element, params string[] preferredJsonKeys)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Object:
                {
                    foreach (var key in preferredJsonKeys.Concat(DefaultJsonKeys).Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        if (!element.TryGetProperty(key, out var property))
                        {
                            continue;
                        }

                        var extracted = ExtractFromJsonElement(property, preferredJsonKeys);
                        if (!string.IsNullOrWhiteSpace(extracted))
                        {
                            return extracted;
                        }
                    }

                    if (element.TryGetProperty("choices", out var choices) &&
                        choices.ValueKind == JsonValueKind.Array &&
                        choices.GetArrayLength() > 0)
                    {
                        var firstChoice = choices[0];
                        if (firstChoice.TryGetProperty("message", out var message))
                        {
                            var extracted = ExtractFromJsonElement(message, preferredJsonKeys);
                            if (!string.IsNullOrWhiteSpace(extracted))
                            {
                                return extracted;
                            }
                        }
                    }

                    if (element.TryGetProperty("data", out var data))
                    {
                        var extracted = ExtractFromJsonElement(data, preferredJsonKeys);
                        if (!string.IsNullOrWhiteSpace(extracted))
                        {
                            return extracted;
                        }
                    }

                    return null;
                }
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        var extracted = ExtractFromJsonElement(item, preferredJsonKeys);
                        if (!string.IsNullOrWhiteSpace(extracted))
                        {
                            return extracted;
                        }
                    }

                    return null;
                default:
                    return null;
            }
        }

        private static string UnescapeCommonSequences(string content)
        {
            return content.Replace("\\n", "\n")
                          .Replace("\\t", "\t")
                          .Replace("\\\"", "\"");
        }

        private static string StripThinkingBlocks(string content)
        {
            var sanitized = Regex.Replace(content, @"<think>[\s\S]*?</think>", string.Empty, RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"<thinking>[\s\S]*?</thinking>", string.Empty, RegexOptions.IgnoreCase);
            return sanitized;
        }

        /// <summary>
        /// 去掉小模型常见的客套前缀（如“好的，遵照您的指示，以下是……”），
        /// 仅当首行命中模式且后续仍有正文时才移除，避免误删正文。
        /// </summary>
        private static string StripPolitenessPreamble(string content)
        {
            var newlineIndex = content.IndexOf('\n');
            var firstLine = (newlineIndex >= 0 ? content[..newlineIndex] : content).Trim();
            if (firstLine.Length == 0 || firstLine.Length > 120)
            {
                return content;
            }

            var isPoliteness = Regex.IsMatch(
                firstLine,
                @"^(好的|当然|没问题|明白了|收到|遵照|根据您|以下)",
                RegexOptions.IgnoreCase);
            var mentionsDelivery = firstLine.Contains("以下是") || firstLine.Contains("为您") || firstLine.Contains("遵照");

            if (!isPoliteness || !mentionsDelivery)
            {
                return content;
            }

            var remainder = newlineIndex >= 0 ? content[(newlineIndex + 1)..].Trim() : string.Empty;
            return string.IsNullOrWhiteSpace(remainder) ? content : remainder;
        }
    }
}
