using Microsoft.Extensions.Logging.Abstractions;
using NovelManagement.AI.Services.RWKV;
using NovelManagement.Tests.Fakes;
using NovelManagement.WPF.Services;
using NovelManagement.WPF.Services.Copilot;
using Xunit;

namespace NovelManagement.Tests;

/// <summary>
/// 创作助手意图识别单元测试：
/// 1. 规则优先（页面导航/流水线动作/本地查询）零模型参与
/// 2. RWKV 兜底：target 白名单校验，解析失败一律按 Chat 处理（绝不假导航）
/// </summary>
public class CopilotIntentServiceTests
{
    private static CopilotIntentService CreateService(FakeRwkvLightningService? rwkv = null)
        => new(rwkv ?? new FakeRwkvLightningService(), NullLogger<CopilotIntentService>.Instance);

    #region 规则匹配

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ParseAsync_EmptyInput_FallsBackToChat(string input)
    {
        var result = await CreateService().ParseAsync(input);
        Assert.Equal(CopilotIntentKind.Chat, result.Kind);
        Assert.Equal("fallback", result.MatchedBy);
    }

    [Theory]
    [InlineData("打开剧情管理", NavigationTarget.PlotManagement)]
    [InlineData("我要看卷章管理", NavigationTarget.VolumeManagement)]
    [InlineData("切换到角色管理", NavigationTarget.CharacterManagement)]
    [InlineData("进入势力页", NavigationTarget.FactionManagement)]
    [InlineData("看看时间线", NavigationTarget.Timeline)]
    [InlineData("打开健康检查", NavigationTarget.ProjectHealthCheck)]
    [InlineData("帮我打开AI配置", NavigationTarget.AIConfiguration)]
    [InlineData("对话生成器", NavigationTarget.DialogGeneration)]
    public async Task ParseAsync_PageKeyword_NavigatesToTarget(string input, NavigationTarget expected)
    {
        var result = await CreateService().ParseAsync(input);
        Assert.Equal(CopilotIntentKind.Navigate, result.Kind);
        Assert.Equal(expected, result.Target);
        Assert.Equal("rule", result.MatchedBy);
    }

    [Fact]
    public async Task ParseAsync_OrdinalChapterWithoutPageKeyword_NavigatesVolumeManagement()
    {
        var result = await CreateService().ParseAsync("看看第三章");
        Assert.Equal(CopilotIntentKind.Navigate, result.Kind);
        Assert.Equal(NavigationTarget.VolumeManagement, result.Target);
        Assert.Equal(3, result.OrdinalNumber);
    }

    [Fact]
    public async Task ParseAsync_OrdinalWithPageKeyword_ExtractsOrdinal()
    {
        var result = await CreateService().ParseAsync("打开卷章管理第12章");
        Assert.Equal(NavigationTarget.VolumeManagement, result.Target);
        Assert.Equal(12, result.OrdinalNumber);
        Assert.Null(result.EntityName); // 含「第」的剩余文本不作为实体名
    }

    [Fact]
    public async Task ParseAsync_QuotedEntityName_Extracted()
    {
        var result = await CreateService().ParseAsync("打开剧情管理《灵剑山传奇》");
        Assert.Equal(NavigationTarget.PlotManagement, result.Target);
        Assert.Equal("灵剑山传奇", result.EntityName);
    }

    [Theory]
    [InlineData("开始规划", "start")]
    [InlineData("继续创作", "resume")]
    [InlineData("接着写", "resume")]
    [InlineData("流水线状态", "status")]
    [InlineData("创作进度", "status")]
    [InlineData("重新生成", "regen")]
    [InlineData("换一版", "regen")]
    public async Task ParseAsync_PipelineKeywords_MatchedAsPipelineAction(string input, string expectedAction)
    {
        var result = await CreateService().ParseAsync(input);
        Assert.Equal(CopilotIntentKind.PipelineAction, result.Kind);
        Assert.Equal(expectedAction, result.PipelineAction);
        Assert.Equal("rule", result.MatchedBy);
    }

    [Fact]
    public async Task ParseAsync_PipelineRuleHasPriorityOverQuery()
    {
        // 「创作进度」同时含查询语感，但必须命中流水线 status 而非 Query
        var result = await CreateService().ParseAsync("看看创作进度");
        Assert.Equal(CopilotIntentKind.PipelineAction, result.Kind);
        Assert.Equal("status", result.PipelineAction);
    }

    [Theory]
    [InlineData("已经写了多少字")]
    [InlineData("现在有多少章了")]
    [InlineData("帮我统计一下")]
    [InlineData("总纲是什么")]
    public async Task ParseAsync_QueryKeywords_MatchedAsLocalQuery(string input)
    {
        var result = await CreateService().ParseAsync(input);
        Assert.Equal(CopilotIntentKind.Query, result.Kind);
        Assert.Equal("rule", result.MatchedBy);
    }

    #endregion

    #region 中文序数解析

    [Theory]
    [InlineData("第三章", 3)]
    [InlineData("第12卷", 12)]
    [InlineData("第二十一章", 21)]
    [InlineData("第三十卷", 30)]
    [InlineData("第两章", 2)]
    [InlineData("第〇章", 0)]
    public void ExtractOrdinal_ChineseAndArabicOrdinals_ParsedCorrectly(string text, int? expected)
    {
        var (number, _) = CopilotIntentService.ExtractOrdinal(text);
        Assert.Equal(expected, number);
    }

    [Theory]
    [InlineData("十", 10)]
    [InlineData("二十三", 23)]
    [InlineData("两", 2)]
    [InlineData("0", null)]
    [InlineData("", null)]
    [InlineData("abc", null)]
    public void ParseChineseNumber_SupportsArabicAndChineseNumerals(string input, int? expected)
    {
        Assert.Equal(expected, CopilotIntentService.ParseChineseNumber(input));
    }

    #endregion

    #region RWKV 兜底（白名单铁律）

    [Fact]
    public async Task ParseAsync_RwkvNavigateWithValidTarget_ReturnsNavigation()
    {
        var rwkv = new FakeRwkvLightningService();
        rwkv.Responses.Enqueue("{\"action\":\"navigate\",\"target\":\"character\"}");
        var result = await CreateService(rwkv).ParseAsync("帮我想一个主角名字"); // 规则未命中 → RWKV 兜底
        Assert.Equal(CopilotIntentKind.Navigate, result.Kind);
        Assert.Equal(NavigationTarget.CharacterManagement, result.Target);
        Assert.Equal("rwkv", result.MatchedBy);
    }

    [Fact]
    public async Task ParseAsync_RwkvNavigateWithIllegalTarget_NeverFakeNavigates()
    {
        // 铁律：target 不在白名单 → 绝不产出假导航，按 Chat 兜底
        var rwkv = new FakeRwkvLightningService();
        rwkv.Responses.Enqueue("{\"action\":\"navigate\",\"target\":\"delete_all_data\"}");
        var result = await CreateService(rwkv).ParseAsync("帮我想一个主角名字");
        Assert.Equal(CopilotIntentKind.Chat, result.Kind);
        Assert.Equal("fallback", result.MatchedBy);
        Assert.Null(result.Target);
    }

    [Fact]
    public async Task ParseAsync_RwkvJudgesChat_FallsBackToChat()
    {
        var rwkv = new FakeRwkvLightningService();
        rwkv.Responses.Enqueue("{\"action\":\"chat\"}");
        var result = await CreateService(rwkv).ParseAsync("帮我想一个主角名字");
        Assert.Equal(CopilotIntentKind.Chat, result.Kind);
        Assert.Equal("fallback", result.MatchedBy);
    }

    [Fact]
    public async Task ParseAsync_RwkvGarbageOutput_FallsBackToChat()
    {
        var rwkv = new FakeRwkvLightningService();
        rwkv.Responses.Enqueue("嗯嗯，我知道了，这个问题有点复杂呢。");
        var result = await CreateService(rwkv).ParseAsync("帮我想一个主角名字");
        Assert.Equal(CopilotIntentKind.Chat, result.Kind);
        Assert.Equal("fallback", result.MatchedBy);
    }

    [Fact]
    public async Task ParseAsync_RwkvCallFailed_FallsBackToChat()
    {
        var rwkv = new FakeRwkvLightningService { FailAll = true };
        var result = await CreateService(rwkv).ParseAsync("帮我想一个主角名字");
        Assert.Equal(CopilotIntentKind.Chat, result.Kind);
        Assert.Equal("fallback", result.MatchedBy);
    }

    [Fact]
    public async Task ParseAsync_RwkvOffline_RulesStillWork()
    {
        var rwkv = new FakeRwkvLightningService { IsAvailable = false };
        var service = new CopilotIntentService(rwkv, NullLogger<CopilotIntentService>.Instance);

        var navigate = await service.ParseAsync("打开角色管理");
        Assert.Equal(NavigationTarget.CharacterManagement, navigate.Target);

        var fallback = await service.ParseAsync("帮我想一个主角名字");
        Assert.Equal(CopilotIntentKind.Chat, fallback.Kind);
    }

    [Fact]
    public async Task ParseAsync_RuleHit_NeverCallsRwkv()
    {
        var rwkv = new FakeRwkvLightningService();
        rwkv.Responses.Enqueue("不应被消费");
        await CreateService(rwkv).ParseAsync("打开剧情管理");
        Assert.Empty(rwkv.ReceivedPrompts); // 规则命中零模型参与
    }

    #endregion
}
