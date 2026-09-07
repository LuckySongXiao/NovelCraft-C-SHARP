using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Services.RWKV;

namespace NovelManagement.WPF.Services.Copilot;

/// <summary>
/// 创作助手意图识别服务：规则优先（关键词映射 + 中文序数解析），
/// 规则未命中才调用 RWKV 最简 JSON 兜底，两者皆败回落入自由问答（Chat）。
/// 铁律：绝不产出模型编造的假导航目标——RWKV 解析失败一律按 Chat 处理。
/// </summary>
public class CopilotIntentService
{
    /// <summary>页面关键词 → 导航目标映射表（第一期覆盖全部 14 个导航目标）。</summary>
    private static readonly (string[] Keywords, NovelManagement.WPF.Services.NavigationTarget Target)[] PageRules =
    {
        (new[] { "剧情管理", "剧情页", "大纲管理", "剧情大纲页" }, NovelManagement.WPF.Services.NavigationTarget.PlotManagement),
        (new[] { "卷宗管理", "卷章管理", "卷章", "章节管理", "分卷管理" }, NovelManagement.WPF.Services.NavigationTarget.VolumeManagement),
        (new[] { "角色管理", "人物管理", "角色页", "人物页" }, NovelManagement.WPF.Services.NavigationTarget.CharacterManagement),
        (new[] { "关系网络", "人物关系" }, NovelManagement.WPF.Services.NavigationTarget.RelationshipNetwork),
        (new[] { "势力管理", "势力页", "势力组织" }, NovelManagement.WPF.Services.NavigationTarget.FactionManagement),
        (new[] { "时间线", "时间轴" }, NovelManagement.WPF.Services.NavigationTarget.Timeline),
        (new[] { "世界设定", "设定管理" }, NovelManagement.WPF.Services.NavigationTarget.WorldSettingManagement),
        (new[] { "项目概览", "项目首页", "概览页" }, NovelManagement.WPF.Services.NavigationTarget.ProjectOverview),
        (new[] { "项目管理", "项目列表" }, NovelManagement.WPF.Services.NavigationTarget.ProjectManagement),
        (new[] { "健康检查", "项目体检", "体检报告" }, NovelManagement.WPF.Services.NavigationTarget.ProjectHealthCheck),
        (new[] { "导入导出", "导出项目" }, NovelManagement.WPF.Services.NavigationTarget.ImportExport),
        (new[] { "AI配置", "AI模型配置", "模型配置" }, NovelManagement.WPF.Services.NavigationTarget.AIConfiguration),
        (new[] { "AI协作", "AI协作创作" }, NovelManagement.WPF.Services.NavigationTarget.AICollaboration),
        (new[] { "对话生成器", "对话生成" }, NovelManagement.WPF.Services.NavigationTarget.DialogGeneration)
    };

    /// <summary>流水线动作关键词（优先级高于查询类：含「进度」词时不与查询冲突）。</summary>
    private static readonly (string[] Keywords, string Action)[] PipelineRules =
    {
        (new[] { "开始规划", "开始创作", "启动流水线", "开始生成" }, "start"),
        (new[] { "继续创作", "继续写", "接着写", "恢复创作", "继续流水线" }, "resume"),
        (new[] { "流水线状态", "创作进度", "查看进度", "写到哪一级" }, "status"),
        (new[] { "重新生成", "换一版", "重新规划", "重写这一级" }, "regen")
    };

    /// <summary>本地查询关键词（进度/统计类，模型零参与）。</summary>
    private static readonly string[] QueryKeywords =
    {
        "写到哪", "多少字", "字数", "多少章", "多少卷", "几个角色", "多少角色",
        "几条剧情线", "多少剧情", "统计", "总纲是什么", "大纲是什么"
    };

    /// <summary>中文序数正则：第X章 / 第X卷（X 支持阿拉伯数字与中文数字）。</summary>
    private static readonly Regex OrdinalRegex = new("第(?<num>[0-9零〇一二两三四五六七八九十]+)(?<unit>章|卷)", RegexOptions.Compiled);

    /// <summary>书名号/引号实体名正则。</summary>
    private static readonly Regex QuotedNameRegex = new("[《「](?<name>[^》」]{2,20})[》」]", RegexOptions.Compiled);

    private readonly IRwkvLightningService? _rwkvService;
    private readonly ILogger<CopilotIntentService>? _logger;

    /// <summary>
    /// 构造函数（RWKV 服务可选：离线时规则与查询类意图照常可用）。
    /// </summary>
    public CopilotIntentService(IRwkvLightningService? rwkvService = null, ILogger<CopilotIntentService>? logger = null)
    {
        _rwkvService = rwkvService;
        _logger = logger;
    }

    /// <summary>
    /// 解析用户输入意图：PipelineAction → Navigate → Query → RWKV 兜底 → Chat。
    /// </summary>
    public async Task<CopilotIntentResult> ParseAsync(string userText)
    {
        var text = (userText ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return new CopilotIntentResult { Kind = CopilotIntentKind.Chat, MatchedBy = "fallback" };
        }

        // 1. 流水线动作（含状态/继续/重新生成）
        var pipeline = MatchPipeline(text);
        if (pipeline != null)
        {
            return pipeline;
        }

        // 2. 页面导航（含实体名/序数提取）
        var navigate = MatchNavigation(text);
        if (navigate != null)
        {
            return navigate;
        }

        // 3. 本地查询
        if (QueryKeywords.Any(k => text.Contains(k, StringComparison.Ordinal)))
        {
            return new CopilotIntentResult { Kind = CopilotIntentKind.Query, MatchedBy = "rule", RawText = text };
        }

        // 4. RWKV 最简 JSON 兜底（仅当规则未命中；失败按 Chat 处理，绝不假导航）
        var rwkv = await ParseByRwkvAsync(text);
        if (rwkv != null)
        {
            return rwkv;
        }

        // 5. 自由问答兜底
        return new CopilotIntentResult { Kind = CopilotIntentKind.Chat, MatchedBy = "fallback", RawText = text };
    }

    #region 规则匹配

    /// <summary>
    /// 流水线动作匹配。
    /// </summary>
    private static CopilotIntentResult? MatchPipeline(string text)
    {
        foreach (var (keywords, action) in PipelineRules)
        {
            if (keywords.Any(k => text.Contains(k, StringComparison.Ordinal)))
            {
                return new CopilotIntentResult
                {
                    Kind = CopilotIntentKind.PipelineAction,
                    PipelineAction = action,
                    MatchedBy = "rule",
                    RawText = text
                };
            }
        }
        return null;
    }

    /// <summary>
    /// 页面导航匹配：命中页面关键词后提取中文序数与实体名。
    /// </summary>
    private static CopilotIntentResult? MatchNavigation(string text)
    {
        foreach (var (keywords, target) in PageRules)
        {
            var hit = keywords.FirstOrDefault(k => text.Contains(k, StringComparison.Ordinal));
            if (hit == null)
            {
                continue;
            }

            var (ordinal, ordinalUnit) = ExtractOrdinal(text);
            var entityName = ExtractEntityName(text, hit);

            // 「第X章」隐含卷章页：未显式提及页面名时也按卷章管理处理
            NovelManagement.WPF.Services.NavigationTarget finalTarget = target;
            if (target == NovelManagement.WPF.Services.NavigationTarget.VolumeManagement && ordinal.HasValue && ordinalUnit == "卷")
            {
                finalTarget = NovelManagement.WPF.Services.NavigationTarget.VolumeManagement;
            }

            return new CopilotIntentResult
            {
                Kind = CopilotIntentKind.Navigate,
                Target = finalTarget,
                EntityName = entityName,
                OrdinalNumber = ordinal,
                MatchedBy = "rule",
                RawText = text
            };
        }

        // 未命中页面关键词但出现「第X章/第X卷」→ 引导到卷章管理
        var (ord, _) = ExtractOrdinal(text);
        if (ord.HasValue)
        {
            return new CopilotIntentResult
            {
                Kind = CopilotIntentKind.Navigate,
                Target = NovelManagement.WPF.Services.NavigationTarget.VolumeManagement,
                OrdinalNumber = ord,
                MatchedBy = "rule",
                RawText = text
            };
        }

        return null;
    }

    /// <summary>
    /// 提取中文序数（第三章→3、第12卷→12）；unit 返回命中的「章/卷」。
    /// </summary>
    internal static (int? Number, string? Unit) ExtractOrdinal(string text)
    {
        var match = OrdinalRegex.Match(text);
        if (!match.Success)
        {
            return (null, null);
        }

        var number = ParseChineseNumber(match.Groups["num"].Value);
        if (!number.HasValue)
        {
            return (null, null);
        }

        return (number, match.Groups["unit"].Value);
    }

    /// <summary>
    /// 提取实体名：书名号/引号内容优先，否则剥离页面词与动词后的短文本。
    /// </summary>
    private static string? ExtractEntityName(string text, string pageKeyword)
    {
        var quoted = QuotedNameRegex.Match(text);
        if (quoted.Success)
        {
            return quoted.Groups["name"].Value.Trim();
        }

        var removed = text;
        foreach (var (keywords, _) in PageRules)
        {
            foreach (var k in keywords)
            {
                removed = removed.Replace(k, string.Empty, StringComparison.Ordinal);
            }
        }

        foreach (var verb in new[] { "打开", "看看", "查看", "跳转", "切换", "进入", "帮我", "我要", "我想", "显示", "定位", "一下", "的", "去", "到", "看", "找", "查" })
        {
            removed = removed.Replace(verb, string.Empty, StringComparison.Ordinal);
        }

        removed = removed.Trim('，', '。', '、', ' ', '！', '？', '，', '。');
        if (removed.Length is >= 2 and <= 12 && !removed.Contains('第', StringComparison.Ordinal))
        {
            return removed;
        }

        return null;
    }

    /// <summary>
    /// 中文数字解析（1-99，支持阿拉伯数字混排）。
    /// </summary>
    internal static int? ParseChineseNumber(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        if (int.TryParse(input, out var arabic))
        {
            return arabic > 0 ? arabic : null;
        }

        var digits = new Dictionary<char, int>
        {
            ['零'] = 0, ['〇'] = 0, ['一'] = 1, ['二'] = 2, ['两'] = 2, ['三'] = 3,
            ['四'] = 4, ['五'] = 5, ['六'] = 6, ['七'] = 7, ['八'] = 8, ['九'] = 9
        };

        var text = input.Trim();
        if (text.Length == 1 && digits.TryGetValue(text[0], out var single))
        {
            return single;
        }

        // 十 / 十X / 十X / X十 / X十Y
        var tenIndex = text.IndexOf('十');
        if (tenIndex < 0)
        {
            return null;
        }

        var tens = 1;
        if (tenIndex > 0 && digits.TryGetValue(text[0], out var tensDigit))
        {
            tens = tensDigit;
        }

        var ones = 0;
        if (tenIndex + 1 < text.Length && digits.TryGetValue(text[tenIndex + 1], out var onesDigit))
        {
            ones = onesDigit;
        }

        var result = tens * 10 + ones;
        return result > 0 ? result : null;
    }

    #endregion

    #region RWKV 兜底

    /// <summary>RWKV 兜底输出 target 键 → 导航目标白名单映射（键用英文短词，小模型输出更稳定）。</summary>
    private static readonly Dictionary<string, NovelManagement.WPF.Services.NavigationTarget> RwkvTargetMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["plot"] = NovelManagement.WPF.Services.NavigationTarget.PlotManagement,
            ["volume"] = NovelManagement.WPF.Services.NavigationTarget.VolumeManagement,
            ["character"] = NovelManagement.WPF.Services.NavigationTarget.CharacterManagement,
            ["relationship"] = NovelManagement.WPF.Services.NavigationTarget.RelationshipNetwork,
            ["faction"] = NovelManagement.WPF.Services.NavigationTarget.FactionManagement,
            ["timeline"] = NovelManagement.WPF.Services.NavigationTarget.Timeline,
            ["setting"] = NovelManagement.WPF.Services.NavigationTarget.WorldSettingManagement,
            ["overview"] = NovelManagement.WPF.Services.NavigationTarget.ProjectOverview,
            ["projects"] = NovelManagement.WPF.Services.NavigationTarget.ProjectManagement,
            ["health"] = NovelManagement.WPF.Services.NavigationTarget.ProjectHealthCheck,
            ["importexport"] = NovelManagement.WPF.Services.NavigationTarget.ImportExport,
            ["aicfg"] = NovelManagement.WPF.Services.NavigationTarget.AIConfiguration,
            ["aicollab"] = NovelManagement.WPF.Services.NavigationTarget.AICollaboration,
            ["dialog"] = NovelManagement.WPF.Services.NavigationTarget.DialogGeneration
        };

    /// <summary>
    /// RWKV 最简 JSON 兜底解析：仅当规则未命中时调用（maxTokens=60，成本极低）。
    /// 解析失败或字段非法返回 null（按 Chat 处理），绝不产出假导航目标。
    /// </summary>
    private async Task<CopilotIntentResult?> ParseByRwkvAsync(string text)
    {
        if (_rwkvService == null || !_rwkvService.IsAvailable)
        {
            return null;
        }

        try
        {
            var prompt =
                "User: 判断用户指令的意图，输出严格JSON（单行，禁止解释）。\n" +
                "仅当用户想打开/切换到某个页面时输出：{\"action\":\"navigate\",\"target\":\"页面键\"}\n" +
                "页面键可选：plot(剧情) volume(卷章) character(角色) relationship(关系) faction(势力) timeline(时间线) setting(设定) overview(概览) projects(项目) health(体检) importexport(导入导出) aicfg(模型配置) aicollab(协作) dialog(对话生成器)\n" +
                "其余任何情况（提问、闲聊、创作请求等）输出：{\"action\":\"chat\"}\n" +
                "用户输入：" + TruncateForPrompt(text, 120) + "\n\n" +
                "Assistant: <think></think\n";

            var response = await _rwkvService.CompleteAsync(prompt, 60);
            if (!response.Success || string.IsNullOrWhiteSpace(response.Text))
            {
                return null;
            }

            var json = ExtractJson(response.Text);
            if (json == null)
            {
                return null;
            }

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("action", out var actionEl) || actionEl.ValueKind != System.Text.Json.JsonValueKind.String)
            {
                return null;
            }

            var action = actionEl.GetString()?.Trim().ToLowerInvariant();
            if (action != "navigate")
            {
                return null; // 模型判断为问答 → 交回 Chat 流程
            }

            if (!doc.RootElement.TryGetProperty("target", out var targetEl) || targetEl.ValueKind != System.Text.Json.JsonValueKind.String)
            {
                return null;
            }

            var targetKey = targetEl.GetString()?.Trim();
            if (string.IsNullOrEmpty(targetKey) || !RwkvTargetMap.TryGetValue(targetKey, out var target))
            {
                return null; // target 不在白名单 → 绝不假导航
            }

            _logger?.LogInformation("RWKV 意图兜底命中导航：{TargetKey} → {Target}", targetKey, target);
            return new CopilotIntentResult
            {
                Kind = CopilotIntentKind.Navigate,
                Target = target,
                MatchedBy = "rwkv",
                RawText = text
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "RWKV 意图兜底解析失败，按自由问答处理");
            return null;
        }
    }

    /// <summary>
    /// 从模型输出提取 JSON 对象文本（首个 { 到末个 }）。
    /// </summary>
    private static string? ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        return text[start..(end + 1)];
    }

    /// <summary>
    /// 提示词内文本截断。
    /// </summary>
    private static string TruncateForPrompt(string? text, int maxChars) =>
        string.IsNullOrEmpty(text) ? string.Empty : text.Length <= maxChars ? text : text[..maxChars] + "……";

    #endregion
}
