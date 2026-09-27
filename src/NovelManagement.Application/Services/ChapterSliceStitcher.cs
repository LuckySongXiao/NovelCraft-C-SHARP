using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NovelManagement.Application.Services;

/// <summary>
/// 章节切片拼接与防复读工具（长文「切片创作 + 拼接」工艺的公共纯逻辑）。
/// <para>
/// 原先在 <c>FullNovelBatchGenerationService</c> 与 <c>CreationPipelineService</c>
/// 中各有一份逐字节相同的私有实现（复制粘贴重复），统一提取到 Application 层
/// 供两处共享，并纳入单元测试覆盖。
/// </para>
/// </summary>
public static class ChapterSliceStitcher
{
    /// <summary>防复读检测的头部对比长度（下一片头部 N 字符与上一片尾部相同即视为复读）。</summary>
    public const int DuplicateHeadLength = 60;

    /// <summary>
    /// 将多个切片拼接为完整正文：跳过空白切片，逐片 Trim 后以换行连接。
    /// </summary>
    public static string Stitch(IEnumerable<string?> parts)
    {
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            if (sb.Length > 0)
            {
                sb.Append('\n');
            }

            sb.Append(part.Trim());
        }

        return sb.ToString();
    }

    /// <summary>
    /// 统计切片总字符数（含未 Trim 的原始长度，与拼接前字数口径一致）。
    /// </summary>
    public static int TotalChars(IEnumerable<string?> parts) => parts.Sum(p => p?.Length ?? 0);

    /// <summary>
    /// 防复读检测：下一片头部（前 <see cref="DuplicateHeadLength"/> 字符）与上一片尾部相同，
    /// 或两片完全相同时，视为模型复读，应触发换提示词/滚动新会话/止损。
    /// </summary>
    public static bool IsDuplicateSlice(string? previous, string? next)
    {
        if (string.IsNullOrWhiteSpace(previous) || string.IsNullOrWhiteSpace(next))
        {
            return false;
        }

        var a = previous.Trim();
        var b = next.Trim();
        if (a == b)
        {
            return true;
        }

        var head = b.Length >= DuplicateHeadLength ? b[..DuplicateHeadLength] : b;
        return a.EndsWith(head, StringComparison.Ordinal);
    }

    /// <summary>
    /// 取文本尾部（滚动会话/跨章衔接注入用）。空文本返回占位符「（缺失）」。
    /// </summary>
    public static string Tail(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "（缺失）";
        }

        return text.Length <= maxChars ? text : "……" + text[^maxChars..];
    }
}
