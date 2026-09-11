using System.Text.RegularExpressions;

namespace NovelManagement.AI.Services.RWKV;

/// <summary>
/// RWKV7-G1「world」模型输出净化器：剥离正文前的固定思维链规划前缀（如 "Let me craft..." / "We need to write..." / "我需要…"）。
/// <para>
/// 背景：world 13B 模型即使使用空 think 块提示（"Assistant: &lt;think&gt;&lt;/think"），
/// 也常在正文前输出一段计划性文字；当 max_tokens 偏低时思维链还会挤占正文预算导致截断
/// （finish_reason=length 时正文残缺）。本类在应用层统一剥离思考段：
/// </para>
/// <list type="bullet">
/// <item>显式闭合块：模型自己输出 "&lt;/think&gt;" 时，取其最后一次出现之后的正文；</item>
/// <item>无显式闭合块：按段落启发式剥离开头的规划段（仅在首段即命中规划特征时才剥离，避免误伤正文）。</item>
/// </list>
/// </summary>
public static class RwkvThinkingStripper
{
    /// <summary>规划段起始特征（中英文）。仅当输出第一段命中时才触发启发式剥离。</summary>
    private static readonly Regex PlanningStartRegex = new(
        @"^[>\-*•\s]*\**\s*" +
        @"(we need to|we must|we should|we'?ll|we will|let'?s|let me|i need to|i should|i'?ll|i will|i must|" +
        @"okay,|first,|to write|drafting|refining|planning|word count|opening:|routine:|the anomaly:|" +
        @"sensory details?|writing:|paragraph\s*[:：]?|draft\s*[:：]|prose\s*[:：]|outline\s*[:：]|" +
        @"正文[:：]|草稿[:：]|思路[:：]|我需要|我们必须|我们应该|让我|接下来|首先|计划)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>规划段内部的 markdown 列表/加粗行（如 "*   **Opening:** …"），一律视为思考段内容。</summary>
    private static readonly Regex PlanningBulletLineRegex = new(
        @"^[>\-*•\s]*\*{1,3}\s*\**\s*(opening|routine|the anomaly|sensory|refining|word count|character|atmosphere|structure|tone|pacing|setting|drafting|writing|planning|note)s?\s*[:：]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// 剥离 RWKV 输出中的思维链前缀，返回纯正文。
    /// </summary>
    public static string Strip(string? completion)
    {
        if (string.IsNullOrWhiteSpace(completion))
        {
            return completion ?? string.Empty;
        }

        var text = completion;

        // 1) 显式思维链闭合块：取最后一个 </think> 之后的正文（fake-think 提示下模型会自行闭合）
        var lastClose = text.LastIndexOf("</think>", StringComparison.OrdinalIgnoreCase);
        if (lastClose >= 0)
        {
            text = text[(lastClose + "</think>".Length)..];
        }

        // 2) 残留未闭合的 <think>（截断场景）：丢弃其后的思考内容
        var open = text.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
        if (open >= 0 && text.IndexOf("</think>", open, StringComparison.OrdinalIgnoreCase) < 0)
        {
            text = text[..open];
        }

        // 3) 启发式剥离：无显式闭合块时，剥离开头的连续规划段
        text = StripLeadingPlanning(text);

        return text.Trim();
    }

    /// <summary>
    /// 按段落启发式剥离开头规划段：仅当第一段命中规划特征时触发，
    /// 连续剥离规划段直到第一个非规划段（即正文）为止。全程未发现正文时保守返回原文。
    /// </summary>
    private static string StripLeadingPlanning(string text)
    {
        var paragraphs = text.Replace("\r\n", "\n").Split('\n');
        var normalized = new string[paragraphs.Length];
        for (var i = 0; i < paragraphs.Length; i++)
        {
            normalized[i] = paragraphs[i].Trim();
        }

        // 第一段必须命中规划特征才剥离（正文开头的输出不做任何改动）
        if (normalized.Length == 0 || normalized[0].Length == 0 || !PlanningStartRegex.IsMatch(normalized[0]))
        {
            return text;
        }

        var proseStart = -1;
        for (var i = 0; i < normalized.Length; i++)
        {
            var line = normalized[i];
            if (line.Length == 0)
            {
                continue; // 空行不算正文
            }

            var isPlanning = PlanningStartRegex.IsMatch(line) || PlanningBulletLineRegex.IsMatch(line);
            if (!isPlanning)
            {
                proseStart = i;
                break;
            }
        }

        if (proseStart <= 0)
        {
            // 整段都是规划（或第一段即正文段）：保守返回原文
            return text;
        }

        return string.Join('\n', normalized[proseStart..]);
    }
}
