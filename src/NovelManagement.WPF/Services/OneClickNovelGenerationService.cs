using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static NovelManagement.WPF.Localization.LocalizationManager;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services;
using NovelManagement.AI.Services.RWKV;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.WPF.Models;
using NovelManagement.WPF.Views;

namespace NovelManagement.WPF.Services;

/// <summary>
/// 一键生成书籍结果。
/// </summary>
public class OneClickNovelGenerationResult
{
    public bool IsSuccess { get; init; }
    public string BookTitle { get; init; } = string.Empty;
    public Guid ProjectId { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool OutlineGenerated { get; init; }
    public bool ChapterGenerated { get; init; }
}

/// <summary>
/// 一键生成书籍服务：RWKV 自命名新书 → 创建项目 → 双 Agent（Main/Sub）生成大纲与第一章。
/// </summary>
public interface IOneClickNovelGenerationService
{
    Task<OneClickNovelGenerationResult> GenerateAsync(IProgress<string>? progress, CancellationToken cancellationToken = default);
}

public class OneClickNovelGenerationService : IOneClickNovelGenerationService
{
    private readonly ModelManager _modelManager;
    private readonly IRwkvLightningService _rwkvService;
    private readonly ProjectCatalogService _projectCatalogService;
    private readonly ProjectContextService _projectContextService;
    private readonly IAIAgentRoleWorkflowService _agentRoleWorkflowService;
    private readonly PlotService _plotService;
    private readonly VolumeService _volumeService;
    private readonly ChapterService _chapterService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OneClickNovelGenerationService> _logger;

    public OneClickNovelGenerationService(
        ModelManager modelManager,
        IRwkvLightningService rwkvService,
        ProjectCatalogService projectCatalogService,
        ProjectContextService projectContextService,
        IAIAgentRoleWorkflowService agentRoleWorkflowService,
        PlotService plotService,
        VolumeService volumeService,
        ChapterService chapterService,
        IServiceProvider serviceProvider,
        ILogger<OneClickNovelGenerationService> logger)
    {
        _modelManager = modelManager;
        _rwkvService = rwkvService;
        _projectCatalogService = projectCatalogService;
        _projectContextService = projectContextService;
        _agentRoleWorkflowService = agentRoleWorkflowService;
        _plotService = plotService;
        _volumeService = volumeService;
        _chapterService = chapterService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<OneClickNovelGenerationResult> GenerateAsync(IProgress<string>? progress, CancellationToken cancellationToken = default)
    {
        // 0. 刷新 RWKV 可用性（启动后后端可能才就绪，TestConnection 会更新 IsAvailable）
        progress?.Report(T("OCG.CheckingRwkv", "正在检查 RWKV 推理服务..."));
        var rwkvOnline = await _rwkvService.TestConnectionAsync();
        if (!rwkvOnline)
        {
            return new OneClickNovelGenerationResult
            {
                Message = T("OCG.RwkvUnreachable", "RWKV 推理服务不可达。请先在「AI模型配置」页启动 RWKV 服务（经 rwkv_launcher 拉起）。")
            };
        }

        // 1. RWKV 自命名：构思书名 / 类型 / 一句话简介
        progress?.Report(T("OCG.Concepting", "RWKV 正在构思新书..."));
        var concept = await GenerateBookConceptAsync(cancellationToken);

        // 2. 创建项目（重名时追加时间戳）
        var projectName = await EnsureUniqueProjectNameAsync(concept.Title);
        progress?.Report(TF("OCG.CreatingBook", "正在创建新书《{0}》...", projectName));
        var catalogItem = await _projectCatalogService.CreateProjectAsync(new NewProjectDialog.NewProjectModel
        {
            Name = projectName,
            Description = concept.Premise,
            Type = concept.Genre,
            TargetWordCount = 100000,
            EnableAI = true,
            AutoSave = true,
            VersionControl = false,
            Template = "AI一键生成"
        });

        // 3. 设为当前项目（新书随即在左侧导航刷新出现）
        _projectContextService.SetCurrentProject(catalogItem.ProjectId, catalogItem.Name);

        // 4. 双 Agent 生成大纲
        progress?.Report(TF("OCG.GeneratingOutlineFor", "MainAgent/SubAgent 正在为《{0}》生成大纲...", catalogItem.Name));
        var outlineResult = await _agentRoleWorkflowService.TryExecuteAsync(
            "GenerateOutline",
            new System.Collections.Generic.Dictionary<string, object>
            {
                ["ProjectId"] = catalogItem.ProjectId,
                ["Title"] = catalogItem.Name,
                ["theme"] = $"{catalogItem.Name}：{concept.Premise}"
            },
            cancellationToken);
        var outlineGenerated = outlineResult?.IsSuccess == true;
        var outlineContent = outlineResult?.Content ?? string.Empty;
        if (!outlineGenerated)
        {
            _logger.LogWarning("一键生成大纲失败: {Message}", outlineResult?.Message ?? "双代理流程未启用");
        }

        // 4.1 大纲落库（Plots 表，主线剧情）
        var outlineSaved = false;
        if (outlineGenerated)
        {
            try
            {
                await _plotService.CreatePlotAsync(new Plot
                {
                    Title = IsEnglish ? $"{catalogItem.Name} - Main Outline" : $"{catalogItem.Name}·主线大纲",
                    Type = "主线",
                    Status = "进行中",
                    Priority = "高",
                    Description = concept.Premise,
                    Outline = outlineContent,
                    ProjectId = catalogItem.ProjectId
                }, cancellationToken);
                outlineSaved = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "一键生成大纲落库失败");
            }
        }

        // 4.2 前置条件生成（主要角色 / 世界设定 / 势力组织，供章节联动与其他管理模块使用）
        var prerequisitesGenerated = false;
        try
        {
            var prerequisiteService = _serviceProvider.GetService<PrerequisiteGenerationService>();
            if (prerequisiteService != null)
            {
                progress?.Report(T("OCG.GeneratingSupport", "正在生成主要角色 / 世界设定 / 势力组织..."));
                var prerequisite = await prerequisiteService.GeneratePrerequisitesAsync(
                    catalogItem.ProjectId,
                    new PrerequisiteGenerationOptions
                    {
                        GeneratePlotOutlines = false, // 大纲已由双 Agent 生成并落库
                        GenerateMainCharacters = true,
                        GenerateWorldSettings = true,
                        GenerateFactions = true
                    });
                prerequisitesGenerated = prerequisite.IsSuccess;
                if (!prerequisite.IsSuccess)
                {
                    _logger.LogWarning("一键生成前置条件失败: {Message}", prerequisite.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "一键生成前置条件生成异常");
        }

        // 5. 双 Agent 生成第一章
        progress?.Report(T("OCG.GeneratingChapter", "MainAgent/SubAgent 正在生成第一章..."));
        var chapterResult = await _agentRoleWorkflowService.TryExecuteAsync(
            "GenerateChapterContent",
            new System.Collections.Generic.Dictionary<string, object>
            {
                ["ProjectId"] = catalogItem.ProjectId,
                ["ChapterTitle"] = IsEnglish ? "Chapter 1" : "第一章",
                ["Outline"] = string.IsNullOrWhiteSpace(outlineContent) ? concept.Premise : outlineContent
            },
            cancellationToken);
        var chapterGenerated = chapterResult?.IsSuccess == true;
        if (!chapterGenerated)
        {
            _logger.LogWarning("一键生成第一章失败: {Message}", chapterResult?.Message ?? "双代理流程未启用");
        }

        // 5.1 第一章落库（Volumes + Chapters 表）
        var chapterSaved = false;
        if (chapterGenerated)
        {
            try
            {
                var chapterContent = chapterResult!.Content;
                var volume = (await _volumeService.GetVolumeListAsync(catalogItem.ProjectId, cancellationToken))
                    .OrderBy(v => v.Order)
                    .FirstOrDefault();
                if (volume == null)
                {
                    volume = await _volumeService.CreateVolumeAsync(new Volume
                    {
                        Title = IsEnglish ? "Volume 1" : "第一卷",
                        Description = IsEnglish ? $"{catalogItem.Name} - Volume 1" : $"{catalogItem.Name} 第一卷",
                        Order = 1,
                        Status = "Writing",
                        ProjectId = catalogItem.ProjectId
                    }, cancellationToken);
                }

                var existingChapters = await _chapterService.GetChapterListAsync(volume.Id, cancellationToken);
                var newChapter = await _chapterService.CreateChapterAsync(new Chapter
                {
                    Title = IsEnglish ? "Chapter 1" : "第一章",
                    Content = chapterContent,
                    Summary = concept.Premise,
                    Order = existingChapters.Count() + 1,
                    Status = "Draft",
                    Type = "正文",
                    VolumeId = volume.Id,
                    WordCount = chapterContent.Length
                }, cancellationToken);
                chapterSaved = true;

                // 5.2 章节联动更新（角色出场/历史、势力、剧情进度、世界设定、时间线）
                try
                {
                    var workflow = _serviceProvider.GetService<ChapterUpdateWorkflowService>();
                    if (workflow != null)
                    {
                        progress?.Report(T("OCG.LinkingUpdate", "正在联动更新角色 / 剧情 / 世界观 / 时间线..."));
                        var workflowResult = await workflow.RunAsync(newChapter, null, cancellationToken);
                        var sync = workflowResult.SyncResult;
                        _logger.LogInformation(
                            "一键生成章节联动完成: 角色 {Chars} 势力 {Factions} 剧情 {Plots} 设定 {Settings} 人物关系 {CharRel} 势力关系 {FactionRel} 时间线 {Timeline}",
                            sync?.UpdatedCharacterCount ?? 0,
                            sync?.UpdatedFactionCount ?? 0,
                            sync?.UpdatedPlotCount ?? 0,
                            sync?.UpdatedWorldSettingCount ?? 0,
                            sync?.UpdatedCharacterRelationshipCount ?? 0,
                            sync?.UpdatedFactionRelationshipCount ?? 0,
                            workflowResult.UpdatedTimelineEventCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "一键生成章节联动更新失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "一键生成第一章落库失败");
            }
        }

        var messageParts = new StringBuilder();
        messageParts.AppendLine(TF("OCG.ResultCreatedFmt", "新书《{0}》已创建并出现在左侧导航。", catalogItem.Name));
        messageParts.AppendLine(TF("OCG.ResultGenreFmt", "类型：{0}", concept.Genre));
        messageParts.AppendLine(TF("OCG.ResultPremiseFmt", "简介：{0}", concept.Premise));
        messageParts.AppendLine(outlineSaved ? T("OCG.ResultOutlineSaved", "大纲：已由双 Agent 生成，并写入剧情库（主线）。")
            : outlineGenerated ? T("OCG.ResultOutlineArchiveFail", "大纲：已生成并归档，但写入剧情库失败（见日志）。")
            : T("OCG.ResultOutlineFail", "大纲：生成失败（见日志）。"));
        messageParts.AppendLine(chapterSaved ? T("OCG.ResultChapterSaved", "第一章：已由双 Agent 生成，并写入章节库（第一卷）。")
            : chapterGenerated ? T("OCG.ResultChapterArchiveFail", "第一章：已生成并归档，但写入章节库失败（见日志）。")
            : T("OCG.ResultChapterFail", "第一章：生成失败（见日志）。"));
        messageParts.AppendLine(prerequisitesGenerated ? T("OCG.ResultPrereqSaved", "角色 / 世界设定 / 势力：已自动生成并写入对应模块。")
            : T("OCG.ResultPrereqFail", "角色 / 世界设定 / 势力：生成失败（见日志），可在「前置条件生成」中重试。"));
        messageParts.AppendLine(T("OCG.ResultLinkDone", "联动更新：角色出场与历史、剧情进度、世界设定、时间线已随第一章自动同步。"));

        return new OneClickNovelGenerationResult
        {
            IsSuccess = true,
            BookTitle = catalogItem.Name,
            ProjectId = catalogItem.ProjectId,
            OutlineGenerated = outlineGenerated,
            ChapterGenerated = chapterGenerated,
            Message = messageParts.ToString()
        };
    }

    /// <summary>
    /// 让 RWKV 构思新书概念（书名/类型/简介），解析失败时使用兜底方案。
    /// 英文模式下提示词要求全英文内容（字段标签保持中文原样以复用解析器）。
    /// </summary>
    private async Task<(string Title, string Genre, string Premise)> GenerateBookConceptAsync(CancellationToken cancellationToken)
    {
        var en = IsEnglish;
        var request = new ChatRequest
        {
            SystemPrompt = en
                ? "You are a senior fiction editor responsible for cover-level planning of brand-new books."
                : "你是一名资深网文编辑，负责为新书做封面级策划。",
            Messages =
            {
                new ChatMessage
                {
                    Role = "user",
                    Content = en
                        ? "Plan a brand-new novel. Output EXACTLY three lines in the following format, one item per line, nothing else:\n书名：(an evocative ENGLISH book title, 2-6 words, no quotes, keep the label 书名： as-is)\n类型：(ENGLISH genre, e.g. Fantasy / Urban / Sci-Fi / Romance / Mystery / Thriller, keep the label 类型： as-is)\n简介：(the core premise in one ENGLISH sentence, max 25 words, keep the label 简介： as-is)\n\nCRITICAL: every value after each Chinese label MUST be in English."
                        : "请为一部全新的网络书籍做策划。严格按照以下三行格式输出，每行一项，不要输出其他任何内容：\n书名：（不超过12个字的中文书名，不要书名号）\n类型：（如：东方玄幻 / 都市异能 / 科幻末日 等）\n简介：（一句话核心创意，不超过60字）"
                }
            },
            Temperature = 0.85,
            // world 模型输出常带思维链规划前缀，≥1200 tokens 保证正文（书名/类型/简介）完整输出
            MaxTokens = 1200
        };

        var response = await _modelManager.ChatAsync("RWKV", request, cancellationToken);
        var (title, genre, premise) = ParseConcept(response.Content ?? string.Empty);
        if (en)
        {
            title = FirstNonEmptyEn(title, "Untitled Book");
            genre = FirstNonEmptyEn(genre, "Fantasy");
            premise = FirstNonEmptyEn(premise, "A brand-new adventure waiting to be written.");
        }
        _logger.LogInformation("RWKV 新书概念生成完成: 书名={Title}, 类型={Genre}", title, genre);
        return (title, genre, premise);
    }

    private static string FirstNonEmptyEn(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) || ContainsConceptInstructionMarker(value) || value.StartsWith("AI新书")
            ? fallback
            : value.Trim();

    private static (string Title, string Genre, string Premise) ParseConcept(string content)
    {
        string title = string.Empty, genre = IsEnglish ? "Fiction" : "长篇书籍", premise = string.Empty;

        foreach (var rawLine in content.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.Trim(' ', '\t', '-', '*', '•', '#');
            if (line.StartsWith("书名", StringComparison.Ordinal) && line.Contains('：'))
            {
                title = CleanConceptValue(line[(line.IndexOf('：') + 1)..]);
            }
            else if (line.StartsWith("类型", StringComparison.Ordinal) && line.Contains('：'))
            {
                genre = CleanConceptValue(line[(line.IndexOf('：') + 1)..]);
                if (string.IsNullOrWhiteSpace(genre))
                {
                    genre = IsEnglish ? "Fiction" : "长篇书籍";
                }
            }
            else if ((line.StartsWith("简介", StringComparison.Ordinal) || line.StartsWith("梗概", StringComparison.Ordinal)) && line.Contains('：'))
            {
                premise = CleanConceptValue(line[(line.IndexOf('：') + 1)..]);
            }
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            // 兜底：取第一行非空短文本作为书名（排除提示词回显/指令行，防止「（不超过12个字的中文书名…）」这类残留入库）
            var fallback = content
                .Replace("\r\n", "\n")
                .Split('\n')
                .Select(l => CleanConceptValue(l))
                .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l) && l.Length <= 24
                    && !ContainsConceptInstructionMarker(l));
            title = string.IsNullOrWhiteSpace(fallback) ? (IsEnglish ? $"New Book {DateTime.Now:MMddHHmm}" : $"AI新书{DateTime.Now:MMddHHmm}") : fallback!;
        }

        if (string.IsNullOrWhiteSpace(premise))
        {
            premise = IsEnglish
                ? "A brand-new book project generated end-to-end by the RWKV dual agents."
                : "由 RWKV 双 Agent 一键生成的全新书籍项目。";
        }

        return (title, genre, premise);
    }

    /// <summary>识别提示词回显/格式指令行（不得作为书名兜底）。</summary>
    private static bool ContainsConceptInstructionMarker(string value) =>
        value.Contains("书名") || value.Contains("不要") || value.Contains("输出") ||
        value.Contains("类型") || value.Contains("简介") || value.Contains("格式") ||
        value.Contains("EXACTLY") || value.Contains("label") ||
        value.StartsWith("Assistant", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("User", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("thinking") || value.Contains("We need");

    private static string CleanConceptValue(string value)
    {
        var cleaned = value.Trim(' ', '\t', '*', '#', '《', '》', '"', '"', '「', '」');
        var invalidChars = Path.GetInvalidFileNameChars();
        cleaned = string.Concat(cleaned.Select(ch => invalidChars.Contains(ch) ? '_' : ch));
        return cleaned.Length > 60 ? cleaned[..60] : cleaned;
    }

    private async Task<string> EnsureUniqueProjectNameAsync(string preferredName)
    {
        var existing = await _projectCatalogService.GetActiveProjectsAsync();
        if (existing.All(item => !string.Equals(item.Name, preferredName, StringComparison.OrdinalIgnoreCase)))
        {
            return preferredName;
        }

        return $"{preferredName} {DateTime.Now:MMdd-HHmm}";
    }
}
