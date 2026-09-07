using NovelManagement.WPF.Services;
using Xunit;

namespace NovelManagement.Tests;

/// <summary>
/// AiAutoFillFormatter 关键行为固化测试
/// Normalize 的「剥编号」语义是修炼体系解析坑点的根源，此处固化以防无意回归
/// </summary>
public class AiAutoFillFormatterTests
{
    [Fact]
    public void Normalize_NumberedInlinePrefix_IsStripped()
    {
        // NormalizeLine 会剥掉「1. 」行首编号 —— 这是设计语义，调用方必须知晓
        var normalized = AiAutoFillFormatter.Normalize("1. 共鸣初启\n2. 共鸣稳固");

        Assert.DoesNotContain("1.", normalized);
        Assert.Contains("共鸣初启", normalized);
        Assert.Contains("共鸣稳固", normalized);
    }

    [Fact]
    public void Normalize_StandAloneNumberLine_IsDropped()
    {
        // 单独的「1.」编号行被当噪声删除 —— 依赖编号切分的调用方必须使用原始文本
        var normalized = AiAutoFillFormatter.Normalize("1.\n等级名：甲\n2.\n等级名：乙");

        Assert.DoesNotContain("2.", normalized);
        Assert.Contains("等级名：甲", normalized);
        Assert.Contains("等级名：乙", normalized);
    }

    [Fact]
    public void ExtractSection_MatchesFieldRowAndSkipsTitleRow()
    {
        // ExtractSection 逐行匹配字段名，「【修炼体系】」标题行不会被误认为「名称」字段
        var header = "【修炼体系】\n体系名称：灵脉共鸣体系\n体系类型：灵能共鸣型";

        Assert.Equal("灵脉共鸣体系", AiAutoFillFormatter.ExtractSection(header, "体系名称", "名称"));
        Assert.Equal("灵能共鸣型", AiAutoFillFormatter.ExtractSection(header, "体系类型", "类型"));
    }

    [Fact]
    public void ExtractSingleLineValue_TakesFirstLineOfWholeInput()
    {
        // 固化首行语义 + Trim 边界：首行「【修炼体系】」仅删头部【（尾部】不在段尾），
        // 返回「修炼体系】」—— 正是此前的落库垃圾等级形态，因此不能用整段调用它提取字段值
        var header = "【修炼体系】\n体系名称：灵脉共鸣体系";

        var value = AiAutoFillFormatter.ExtractSingleLineValue(header, "体系名称");

        Assert.Equal("修炼体系】", value);
    }

    [Fact]
    public void ExtractSection_MultilineFieldStopsAtNextHeading()
    {
        var block = "等级名：共鸣初启\n描述：第一行\n第二行\n突破条件：灵息初成";

        var desc = AiAutoFillFormatter.ExtractSection(block, "描述");

        Assert.Contains("第一行", desc);
        Assert.Contains("第二行", desc);
        Assert.DoesNotContain("突破条件", desc);
    }
}
