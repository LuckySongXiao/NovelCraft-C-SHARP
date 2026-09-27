using System;
using System.Collections.Generic;
using System.Linq;
using NovelManagement.Application.Services;
using Xunit;

namespace NovelManagement.Tests;

/// <summary>
/// ChapterSliceStitcher 单元测试：长文「切片创作 + 拼接」工艺的公共纯逻辑
/// （原先在批量服务与创作流水线中各有一份相同的私有实现，提取后纳入测试覆盖）。
/// </summary>
public class ChapterSliceStitcherTests
{
    [Fact]
    public void Stitch_JoinsTrimmedPartsWithNewline()
    {
        var result = ChapterSliceStitcher.Stitch(new[] { "  第一段。  ", "第二段。", " 第三段。 " });
        Assert.Equal("第一段。\n第二段。\n第三段。", result);
    }

    [Fact]
    public void Stitch_SkipsNullOrWhitespaceParts()
    {
        var result = ChapterSliceStitcher.Stitch(new string?[] { null, "", "   ", "正文。", "结尾。" });
        Assert.Equal("正文。\n结尾。", result);
    }

    [Fact]
    public void Stitch_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ChapterSliceStitcher.Stitch(Array.Empty<string?>()));
        Assert.Equal(string.Empty, ChapterSliceStitcher.Stitch(new string?[] { null, "  " }));
    }

    [Fact]
    public void TotalChars_SumsLengthsIncludingWhitespace()
    {
        // 口径与拼接前一致：未 Trim 的原始长度（进度条/字数达标判断用）
        // "  abc " = 6 字符，"de" = 2 字符
        Assert.Equal(8, ChapterSliceStitcher.TotalChars(new[] { "  abc ", "de" }));
        Assert.Equal(0, ChapterSliceStitcher.TotalChars(new string?[] { null, "" }));
    }

    [Fact]
    public void IsDuplicateSlice_IdenticalParts_AreDuplicates()
    {
        Assert.True(ChapterSliceStitcher.IsDuplicateSlice("夜色渐深，灯火阑珊。", "夜色渐深，灯火阑珊。"));
    }

    [Fact]
    public void IsDuplicateSlice_HeadMatchesTail_IsDuplicate()
    {
        // 下一片头部 60 字符与上一片尾部相同 → 复读（模型从停笔处原样重写）
        var tail = new string('前', 80);
        var prev = new string('无', 50) + tail;
        var next = new string('后', 10);
        var duplicateHead = tail[..60] + next;
        Assert.True(ChapterSliceStitcher.IsDuplicateSlice(prev, duplicateHead));
    }

    [Fact]
    public void IsDuplicateSlice_ShortNext_FullCompare()
    {
        // 下一片不足 60 字符时整片作为头部对比
        Assert.True(ChapterSliceStitcher.IsDuplicateSlice("上文……重复开头。", "重复开头。"));
        Assert.False(ChapterSliceStitcher.IsDuplicateSlice("上文……重复开头。", "全新情节。"));
    }

    [Fact]
    public void IsDuplicateSlice_DistinctContent_NotDuplicate()
    {
        var prev = new string('甲', 100);
        var next = new string('乙', 100);
        Assert.False(ChapterSliceStitcher.IsDuplicateSlice(prev, next));
    }

    [Fact]
    public void IsDuplicateSlice_NullOrWhitespace_ReturnsFalse()
    {
        Assert.False(ChapterSliceStitcher.IsDuplicateSlice(null, "正文"));
        Assert.False(ChapterSliceStitcher.IsDuplicateSlice("正文", null));
        Assert.False(ChapterSliceStitcher.IsDuplicateSlice("  ", "正文"));
        Assert.False(ChapterSliceStitcher.IsDuplicateSlice("正文", "  "));
    }

    [Fact]
    public void Tail_EmptyText_ReturnsPlaceholder()
    {
        Assert.Equal("（缺失）", ChapterSliceStitcher.Tail(null, 100));
        Assert.Equal("（缺失）", ChapterSliceStitcher.Tail("", 100));
    }

    [Fact]
    public void Tail_ShortText_ReturnedAsIs()
    {
        Assert.Equal("短文本。", ChapterSliceStitcher.Tail("短文本。", 100));
    }

    [Fact]
    public void Tail_LongText_ReturnsLastNCharsWithEllipsis()
    {
        var text = new string('甲', 150) + new string('乙', 50);
        var result = ChapterSliceStitcher.Tail(text, 50);
        Assert.StartsWith("……", result);
        Assert.Equal(52, result.Length);
        Assert.Equal(new string('乙', 50), result[2..]);
    }
}
