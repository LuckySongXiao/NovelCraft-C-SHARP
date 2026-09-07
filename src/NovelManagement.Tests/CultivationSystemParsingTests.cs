using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NovelManagement.Application.Interfaces;
using NovelManagement.AI.Services.RWKV;
using NovelManagement.Tests.Fakes;
using NovelManagement.WPF.Models;
using NovelManagement.WPF.Services;
using Xunit;

namespace NovelManagement.Tests;

/// <summary>
/// 修为等级自定义体系解析逻辑单元测试
/// 覆盖 2026-09-06 修复的两个解析坑：
/// 1. AiAutoFillFormatter.Normalize 会剥掉「N.」行首编号 → 必须在原始文本上切分
/// 2. ExtractSingleLineValue 取整段首行 → 「【修炼体系】」标题行会被当成字段值
/// </summary>
public class CultivationSystemParsingTests
{
    private const string ProjectId = "00000000-0000-0000-0000-000000000001";

    /// <summary>RWKV 真实输出形态样本（与「灵脉共鸣体系」生成一致的结构）</summary>
    private const string RwkvOutput = """"
        【修炼体系】
        体系名称：灵脉共鸣体系
        体系类型：灵能共鸣型
        修炼方法：以灵脉为根基，通过共鸣强化肉体、灵魂与神魂，并最终领悟世界法则。

        1.
        等级名：共鸣初启
        描述：初次感知天地灵脉的律动，能够引动一丝灵气入体。
        突破条件：灵气感知达到稳定，体内灵息初成。
        能力特点：可以缓慢恢复体力，夜视能力增强。

        2.
        等级名：共鸣稳固
        描述：体内灵息形成稳定循环，与灵脉建立初步共鸣。
        突破条件：完成灵息周天运转，共鸣频率连续十息不散。
        能力特点：体魄明显强化，可使用低阶共鸣术法。
        """";

    private static PrerequisiteGenerationService CreateService(IServiceProvider provider)
        => new(provider, NullLogger<PrerequisiteGenerationService>.Instance);

    private static IServiceProvider BuildProvider(
        FakeCultivationSystemService? cultivation = null,
        IRwkvLightningService? rwkv = null)
    {
        var services = new ServiceCollection();
        if (cultivation != null) services.AddSingleton<ICultivationSystemService>(cultivation);
        if (rwkv != null) services.AddSingleton<IRwkvLightningService>(rwkv);
        return services.BuildServiceProvider();
    }

    #region SplitNumberedLevelBlocks

    [Fact]
    public void Split_StandardNumberedLines_ReturnsOneBlockPerLevel()
    {
        var levelsPart = """
            1.
            等级名：共鸣初启
            描述：初次感知。

            2.
            等级名：共鸣稳固
            描述：稳定循环。
            """;

        var blocks = PrerequisiteGenerationService.SplitNumberedLevelBlocks(levelsPart);

        Assert.Equal(2, blocks.Count);
        Assert.Contains("等级名：共鸣初启", blocks[0]);
        Assert.Contains("等级名：共鸣稳固", blocks[1]);
    }

    [Fact]
    public void Split_NumberedInlineContent_KeepsInlineText()
    {
        // 模型常见变体：「1. 共鸣初启」编号与内容同行
        var levelsPart = """
            1. 共鸣初启
            描述：初次感知。
            2. 共鸣稳固
            描述：稳定循环。
            """;

        var blocks = PrerequisiteGenerationService.SplitNumberedLevelBlocks(levelsPart);

        Assert.Equal(2, blocks.Count);
        Assert.StartsWith("共鸣初启", blocks[0]);
        Assert.StartsWith("共鸣稳固", blocks[1]);
    }

    [Fact]
    public void Split_TrailingWhitespaceNumbers_StillRecognized()
    {
        // 「1.」行尾带空格 / 中文顿号变体
        var levelsPart = "1. \n等级名：甲\n2、\n等级名：乙\n3\n等级名：丙";

        var blocks = PrerequisiteGenerationService.SplitNumberedLevelBlocks(levelsPart);

        Assert.Equal(3, blocks.Count);
        Assert.Equal("等级名：甲", blocks[0].Trim());
        Assert.Equal("等级名：乙", blocks[1].Trim());
        Assert.Equal("等级名：丙", blocks[2].Trim());
    }

    [Fact]
    public void Split_NoNumberedLines_CollapsesToSingleBlock()
    {
        var levelsPart = "等级名：甲\n描述：无编号输入。";

        var blocks = PrerequisiteGenerationService.SplitNumberedLevelBlocks(levelsPart);

        Assert.Single(blocks);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n  ")]
    public void Split_NullOrWhitespace_ReturnsEmpty(string? input)
    {
        Assert.Empty(PrerequisiteGenerationService.SplitNumberedLevelBlocks(input));
    }

    #endregion

    #region ParseCultivationSystemSectionAsync

    [Fact]
    public async Task Parse_RealRwkvOutput_CreatesSystemAndLevels()
    {
        var fakeService = new FakeCultivationSystemService();
        var service = CreateService(BuildProvider());
        var result = new PrerequisiteGenerationResult { ProjectId = Guid.Parse(ProjectId) };

        var ok = await service.ParseCultivationSystemSectionAsync(
            Guid.Parse(ProjectId), RwkvOutput, result, fakeService);

        Assert.True(ok);
        var system = Assert.Single(fakeService.CreatedSystems);
        Assert.Equal("灵脉共鸣体系", system.Name);
        Assert.Equal("灵能共鸣型", system.Type);
        Assert.Contains("灵脉为根基", system.CultivationMethod);
        // 等级全部落库且不含标题行垃圾（「修炼体系】」回归防护）
        Assert.Equal(2, fakeService.CreatedLevels.Count);
        Assert.Equal("共鸣初启", fakeService.CreatedLevels[0].Name);
        Assert.Equal("共鸣稳固", fakeService.CreatedLevels[1].Name);
        Assert.Equal(1, fakeService.CreatedLevels[0].OrderIndex);
        Assert.Equal(2, fakeService.CreatedLevels[1].OrderIndex);
        Assert.Equal("初次感知天地灵脉的律动，能够引动一丝灵气入体。", fakeService.CreatedLevels[0].Description);
        Assert.Equal("灵气感知达到稳定，体内灵息初成。", fakeService.CreatedLevels[0].BreakthroughCondition);
        Assert.False(result.NeedsCultivationSystem);
        Assert.NotEmpty(result.GeneratedItems);
    }

    [Fact]
    public async Task Parse_OutputWithoutTitlePrefix_StillParses()
    {
        // 模型偶尔省略「【修炼体系】」标题行
        var output = RwkvOutput.Replace("【修炼体系】\n", string.Empty);
        var fakeService = new FakeCultivationSystemService();
        var service = CreateService(BuildProvider());
        var result = new PrerequisiteGenerationResult { ProjectId = Guid.Parse(ProjectId) };

        var ok = await service.ParseCultivationSystemSectionAsync(
            Guid.Parse(ProjectId), output, result, fakeService);

        Assert.True(ok);
        Assert.Equal("灵脉共鸣体系", Assert.Single(fakeService.CreatedSystems).Name);
        Assert.Equal(2, fakeService.CreatedLevels.Count);
    }

    [Fact]
    public async Task Parse_LessThanTwoLevels_Fails()
    {
        // 只有一个等级 → 解析失败（返回 false，调用方回退默认模板）
        var output = """
            【修炼体系】
            体系名称：单阶体系
            体系类型：通用

            1.
            等级名：唯一境
            描述：只有一级。
            """;
        var fakeService = new FakeCultivationSystemService();
        var service = CreateService(BuildProvider());
        var result = new PrerequisiteGenerationResult { ProjectId = Guid.Parse(ProjectId) };

        var ok = await service.ParseCultivationSystemSectionAsync(
            Guid.Parse(ProjectId), output, result, fakeService);

        Assert.False(ok);
        Assert.Empty(fakeService.CreatedSystems);
    }

    [Fact]
    public async Task Parse_MissingSystemName_Fails()
    {
        var output = """
            体系类型：通用

            1.
            等级名：甲
            2.
            等级名：乙
            """;
        var fakeService = new FakeCultivationSystemService();
        var service = CreateService(BuildProvider());
        var result = new PrerequisiteGenerationResult { ProjectId = Guid.Parse(ProjectId) };

        var ok = await service.ParseCultivationSystemSectionAsync(
            Guid.Parse(ProjectId), output, result, fakeService);

        Assert.False(ok);
        Assert.Empty(fakeService.CreatedSystems);
    }

    [Fact]
    public async Task Parse_GarbageHeaderInsideFirstBlock_IsSkipped()
    {
        // 回归场景：标题行混入第一个编号块内，不得产出「修炼体系】」垃圾等级
        var output = """
            【修炼体系】
            体系名称：玄脉体系
            体系类型：灵脉共鸣型
            修炼方法：感应玄脉。

            1.
            【修炼体系】
            等级名：玄脉初感
            描述：初感玄脉。
            突破条件：玄脉稳定。
            能力特点：灵气入体。

            2.
            等级名：玄脉共鸣
            描述：共鸣加深。
            突破条件：共鸣不散。
            能力特点：术法初成。
            """;
        var fakeService = new FakeCultivationSystemService();
        var service = CreateService(BuildProvider());
        var result = new PrerequisiteGenerationResult { ProjectId = Guid.Parse(ProjectId) };

        var ok = await service.ParseCultivationSystemSectionAsync(
            Guid.Parse(ProjectId), output, result, fakeService);

        Assert.True(ok);
        Assert.Equal(2, fakeService.CreatedLevels.Count);
        Assert.All(fakeService.CreatedLevels, l => Assert.DoesNotContain("】", l.Name));
        Assert.Equal("玄脉初感", fakeService.CreatedLevels[0].Name);
    }

    #endregion

    #region EnsureCultivationSystemAsync（端到端决策链路）

    [Fact]
    public async Task Ensure_RwkvAvailable_ParsesAndPersists()
    {
        var fakeService = new FakeCultivationSystemService();
        var fakeRwkv = new FakeRwkvLightningService();
        fakeRwkv.Responses.Enqueue(RwkvOutput);
        var service = CreateService(BuildProvider(fakeService, fakeRwkv));
        var result = new PrerequisiteGenerationResult { ProjectId = Guid.Parse(ProjectId) };

        await service.EnsureCultivationSystemAsync(Guid.Parse(ProjectId), result);

        Assert.Single(fakeService.CreatedSystems);
        Assert.Equal("灵脉共鸣体系", fakeService.CreatedSystems[0].Name);
        Assert.False(result.NeedsCultivationSystem);
        Assert.Equal(1, result.GeneratedCultivationSystemsCount);
    }

    [Fact]
    public async Task Ensure_ExistingSystem_SkipsGeneration()
    {
        var fakeService = new FakeCultivationSystemService();
        // 预置一个已有体系 → GetAllAsync 非空
        await fakeService.CreateAsync(new NovelManagement.Application.DTOs.CreateCultivationSystemDto
        {
            Name = "已有体系", Type = "通用", ProjectId = Guid.Parse(ProjectId)
        });
        var fakeRwkv = new FakeRwkvLightningService();
        var service = CreateService(BuildProvider(fakeService, fakeRwkv));
        var result = new PrerequisiteGenerationResult { ProjectId = Guid.Parse(ProjectId) };

        await service.EnsureCultivationSystemAsync(Guid.Parse(ProjectId), result);

        // 不新增体系、不调用 RWKV
        Assert.Single(fakeService.CreatedSystems);
        Assert.Empty(fakeRwkv.ReceivedPrompts);
        Assert.False(result.NeedsCultivationSystem);
    }

    [Fact]
    public async Task Ensure_RwkvUnavailable_FallsBackToGenericTemplate()
    {
        var fakeService = new FakeCultivationSystemService();
        var service = CreateService(BuildProvider(fakeService)); // 未注册 RWKV
        var result = new PrerequisiteGenerationResult { ProjectId = Guid.Parse(ProjectId) };

        await service.EnsureCultivationSystemAsync(Guid.Parse(ProjectId), result);

        var system = Assert.Single(fakeService.CreatedSystems);
        Assert.Equal("通用进阶体系", system.Name);
        Assert.Equal(9, fakeService.CreatedLevels.Count); // 中性九阶模板
        Assert.False(result.NeedsCultivationSystem);
    }

    [Fact]
    public async Task Ensure_RwkvParseFailure_FallsBackToGenericTemplate()
    {
        var fakeService = new FakeCultivationSystemService();
        var fakeRwkv = new FakeRwkvLightningService();
        fakeRwkv.Responses.Enqueue("完全无法解析的随机文本"); // 解析必失败
        var service = CreateService(BuildProvider(fakeService, fakeRwkv));
        var result = new PrerequisiteGenerationResult { ProjectId = Guid.Parse(ProjectId) };

        await service.EnsureCultivationSystemAsync(Guid.Parse(ProjectId), result);

        var system = Assert.Single(fakeService.CreatedSystems);
        Assert.Equal("通用进阶体系", system.Name);
    }

    #endregion
}
