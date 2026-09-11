using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services;
using NovelManagement.AI.Utilities;

namespace NovelManagement.WPF.Services;

/// <summary>
/// MainAgent / SubAgent 双代理写作编排服务。
/// </summary>
public interface IAIAgentRoleWorkflowService
{
    Task<AIAgentRoleWorkflowResult?> TryExecuteAsync(
        string taskType,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);
}

public class AIAgentRoleWorkflowService : IAIAgentRoleWorkflowService
{
    private readonly IConfiguration _configuration;
    private readonly ModelManager _modelManager;
    private readonly ProjectArchiveService _projectArchiveService;
    private readonly ILogger<AIAgentRoleWorkflowService> _logger;

    public AIAgentRoleWorkflowService(
        IConfiguration configuration,
        ModelManager modelManager,
        ProjectArchiveService projectArchiveService,
        ILogger<AIAgentRoleWorkflowService> logger)
    {
        _configuration = configuration;
        _modelManager = modelManager;
        _projectArchiveService = projectArchiveService;
        _logger = logger;
    }

    public async Task<AIAgentRoleWorkflowResult?> TryExecuteAsync(
        string taskType,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!IsSupportedTask(taskType))
        {
            return null;
        }

        var settings = LoadSettings();
        if (!settings.EnableDualAgentWorkflow)
        {
            return null;
        }

        var mainProvider = ResolveProvider(settings.MainAgentProvider);
        var subProvider = ResolveProvider(settings.SubAgentProvider);
        if (mainProvider == null || subProvider == null)
        {
            #region debug-point E:workflow-provider-missing
            await ReportDebugEventAsync(
                "E",
                "TryExecuteAsync",
                "双代理流程缺少可用提供者",
                new Dictionary<string, object?>
                {
                    ["taskType"] = taskType,
                    ["mainProvider"] = settings.MainAgentProvider,
                    ["subProvider"] = settings.SubAgentProvider
                });
            #endregion
            return AIAgentRoleWorkflowResult.Failed("MainAgent/SubAgent 未找到可用模型提供者。");
        }

        try
        {
            #region debug-point C:workflow-enter
            await ReportDebugEventAsync(
                "C",
                "TryExecuteAsync",
                "进入双代理流程",
                new Dictionary<string, object?>
                {
                    ["taskType"] = taskType,
                    ["configuredMainProvider"] = settings.MainAgentProvider,
                    ["configuredMainModel"] = settings.MainAgentModel,
                    ["configuredSubProvider"] = settings.SubAgentProvider,
                    ["configuredSubModel"] = settings.SubAgentModel,
                    ["mainProvider"] = mainProvider.ProviderName,
                    ["subProvider"] = subProvider.ProviderName,
                    ["parameterKeys"] = parameters.Keys.OrderBy(key => key).ToArray()
                });
            #endregion

            var requirementBrief = await GenerateRequirementBriefAsync(
                taskType,
                parameters,
                settings,
                subProvider,
                cancellationToken);
            if (!requirementBrief.IsSuccess)
            {
                #region debug-point C:subagent-requirement-failed
                await ReportDebugEventAsync(
                    "C",
                    "TryExecuteAsync",
                    $"SubAgent需求总结失败: {requirementBrief.ErrorMessage}",
                    new Dictionary<string, object?>
                    {
                        ["taskType"] = taskType,
                        ["subProvider"] = subProvider.ProviderName,
                        ["errorMessage"] = requirementBrief.ErrorMessage
                    });
                #endregion
                return AIAgentRoleWorkflowResult.Failed($"SubAgent 需求总结失败: {requirementBrief.ErrorMessage}");
            }

            var mainDraft = await GenerateMainDraftAsync(
                taskType,
                requirementBrief.Content,
                settings,
                mainProvider,
                cancellationToken);
            if (!mainDraft.IsSuccess)
            {
                #region debug-point C:mainagent-draft-failed
                await ReportDebugEventAsync(
                    "C",
                    "TryExecuteAsync",
                    $"MainAgent文案生成失败: {mainDraft.ErrorMessage}",
                    new Dictionary<string, object?>
                    {
                        ["taskType"] = taskType,
                        ["mainProvider"] = mainProvider.ProviderName,
                        ["errorMessage"] = mainDraft.ErrorMessage
                    });
                #endregion
                return AIAgentRoleWorkflowResult.Failed($"MainAgent 文案生成失败: {mainDraft.ErrorMessage}");
            }

            var finalContentResponse = await RefineFinalContentAsync(
                taskType,
                requirementBrief.Content,
                mainDraft.Content,
                settings,
                subProvider,
                cancellationToken);
            if (!finalContentResponse.IsSuccess)
            {
                #region debug-point C:subagent-refine-failed
                await ReportDebugEventAsync(
                    "C",
                    "TryExecuteAsync",
                    $"SubAgent定稿整理失败: {finalContentResponse.ErrorMessage}",
                    new Dictionary<string, object?>
                    {
                        ["taskType"] = taskType,
                        ["subProvider"] = subProvider.ProviderName,
                        ["errorMessage"] = finalContentResponse.ErrorMessage
                    });
                #endregion
                return AIAgentRoleWorkflowResult.Failed($"SubAgent 定稿整理失败: {finalContentResponse.ErrorMessage}");
            }

            var finalContent = AIOutputSanitizer.ExtractCleanOutput(finalContentResponse.Content);
            if (string.IsNullOrWhiteSpace(finalContent))
            {
                #region debug-point C:subagent-empty-result
                await ReportDebugEventAsync(
                    "C",
                    "TryExecuteAsync",
                    "SubAgent定稿结果为空",
                    new Dictionary<string, object?>
                    {
                        ["taskType"] = taskType,
                        ["subProvider"] = subProvider.ProviderName
                    });
                #endregion
                return AIAgentRoleWorkflowResult.Failed("SubAgent 定稿结果为空。");
            }

            // 守卫：小模型在定稿阶段可能复刻“简报结构”而非正文，
            // 若定稿仍带简报特征而草稿是正文，则回退使用草稿。
            // 英文模式下小模型会把 "SubAgent requirement brief: ... MainAgent draft: ..." 整段复刻，
            // 先尝试从回显中截取 draft 之后的正文；截不出再回退草稿。
            if (TryExtractMainAgentDraft(finalContent, out var extractedDraft) &&
                extractedDraft.Length >= 200 &&
                !LooksLikeRequirementBrief(extractedDraft))
            {
                finalContent = extractedDraft;
                await ReportDebugEventAsync(
                    "C",
                    "TryExecuteAsync",
                    "SubAgent定稿为简报回显，已提取 MainAgent draft 正文",
                    new Dictionary<string, object?>
                    {
                        ["taskType"] = taskType
                    });
            }
            else if (LooksLikeRequirementBrief(finalContent) && !LooksLikeRequirementBrief(mainDraft.Content))
            {
                finalContent = AIOutputSanitizer.ExtractCleanOutput(mainDraft.Content);
                await ReportDebugEventAsync(
                    "C",
                    "TryExecuteAsync",
                    "SubAgent定稿疑似简报回显，已回退为 MainAgent 草稿",
                    new Dictionary<string, object?>
                    {
                        ["taskType"] = taskType
                    });
            }

            // 语言/跑题守卫：英文模式下输出大量中文，或以 Markdown 说明文档开头，
            // 视为模型跑题（实测 13B 会把简报模板解释成「用途说明」文档），回退 MainAgent 草稿。
            var trimFinal = finalContent.TrimStart();
            var offTopic = trimFinal.StartsWith("#") ||
                           trimFinal.Contains("用途说明", StringComparison.Ordinal) ||
                           (GenEn && ComputeCjkRatio(trimFinal) > 0.08);
            if (offTopic && !LooksLikeRequirementBrief(mainDraft.Content))
            {
                var sanitizedDraft = AIOutputSanitizer.ExtractCleanOutput(mainDraft.Content);
                if (!string.IsNullOrWhiteSpace(sanitizedDraft))
                {
                    finalContent = sanitizedDraft;
                    await ReportDebugEventAsync(
                        "C",
                        "TryExecuteAsync",
                        "SubAgent定稿跑题或语言错误，已回退为 MainAgent 草稿",
                        new Dictionary<string, object?>
                        {
                            ["taskType"] = taskType
                        });
                }
            }

            var result = new AIAgentRoleWorkflowResult
            {
                IsSuccess = true,
                Content = finalContent,
                Message = "已通过 MainAgent/SubAgent 双代理流程生成内容。",
                Metadata = new Dictionary<string, object>
                {
                    ["WorkflowMode"] = "DualAgent",
                    ["TaskType"] = taskType,
                    ["RequirementBrief"] = requirementBrief.Content,
                    ["MainDraft"] = mainDraft.Content,
                    ["MainAgentProvider"] = mainProvider.ProviderName,
                    ["SubAgentProvider"] = subProvider.ProviderName,
                    ["MainAgentModel"] = string.IsNullOrWhiteSpace(settings.MainAgentModel) ? mainProvider.DefaultModelName : settings.MainAgentModel,
                    ["SubAgentModel"] = string.IsNullOrWhiteSpace(settings.SubAgentModel) ? subProvider.DefaultModelName : settings.SubAgentModel
                }
            };

            if (settings.EnableArchiveWrite)
            {
                var archiveResult = await _projectArchiveService.WriteCleanContentAsync(
                    TryResolveProjectId(parameters),
                    taskType,
                    finalContent,
                    BuildTitleHint(taskType, parameters),
                    new Dictionary<string, string>
                    {
                        ["MainAgentProvider"] = mainProvider.ProviderName,
                        ["SubAgentProvider"] = subProvider.ProviderName,
                        ["MainAgentModel"] = string.IsNullOrWhiteSpace(settings.MainAgentModel) ? mainProvider.DefaultModelName : settings.MainAgentModel,
                        ["SubAgentModel"] = string.IsNullOrWhiteSpace(settings.SubAgentModel) ? subProvider.DefaultModelName : settings.SubAgentModel
                    },
                    cancellationToken);

                result.Metadata["ArchiveWriteMessage"] = archiveResult.Message;
                if (!string.IsNullOrWhiteSpace(archiveResult.ArchivePath))
                {
                    result.Metadata["ArchivePath"] = archiveResult.ArchivePath!;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行双代理写作流程失败: {TaskType}", taskType);

            #region debug-point E:workflow-exception
            await ReportDebugEventAsync(
                "E",
                "TryExecuteAsync",
                $"双代理流程异常: {ex.Message}",
                new Dictionary<string, object?>
                {
                    ["taskType"] = taskType,
                    ["exceptionType"] = ex.GetType().FullName,
                    ["stack"] = ex.ToString()
                });
            #endregion
            return AIAgentRoleWorkflowResult.Failed($"双代理流程执行失败: {ex.Message}");
        }
    }

    #region debug-point C:report-helper
    private static async Task ReportDebugEventAsync(string hypothesisId, string location, string message, Dictionary<string, object?>? data = null)
    {
        try
        {
            var debugServerUrl = "http://127.0.0.1:7777/event";
            var debugSessionId = "single-file-ai-ui";
            var envPath = FindDebugEnvPath();
            if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
            {
                foreach (var line in File.ReadAllLines(envPath, Encoding.UTF8))
                {
                    if (line.StartsWith("DEBUG_SERVER_URL=", StringComparison.OrdinalIgnoreCase))
                    {
                        debugServerUrl = line["DEBUG_SERVER_URL=".Length..].Trim();
                    }
                    else if (line.StartsWith("DEBUG_SESSION_ID=", StringComparison.OrdinalIgnoreCase))
                    {
                        debugSessionId = line["DEBUG_SESSION_ID=".Length..].Trim();
                    }
                }
            }

            var payload = JsonSerializer.Serialize(new
            {
                sessionId = debugSessionId,
                runId = "pre-fix",
                hypothesisId,
                location,
                msg = $"[DEBUG] {message}",
                data,
                ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });

            using var client = new System.Net.Http.HttpClient();
            using var content = new System.Net.Http.StringContent(payload, Encoding.UTF8, "application/json");
            await client.PostAsync(debugServerUrl, content);
        }
        catch
        {
            // ignore debug instrumentation failures
        }
    }

    private static string? FindDebugEnvPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".dbg", "single-file-ai-ui.env"),
            Path.Combine(Directory.GetCurrentDirectory(), ".dbg", "single-file-ai-ui.env")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, ".dbg", "single-file-ai-ui.env");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
    #endregion

    private AgentRoleWorkflowSettings LoadSettings()
    {
        var section = _configuration.GetSection("AI:AgentRoles");
        return new AgentRoleWorkflowSettings
        {
            EnableDualAgentWorkflow = bool.TryParse(section["EnableDualAgentWorkflow"], out var enabled) ? enabled : true,
            EnableArchiveWrite = bool.TryParse(section["EnableArchiveWrite"], out var archiveEnabled) ? archiveEnabled : true,
            MainAgentProvider = section["MainAgentProvider"] ?? "DeepSeek",
            MainAgentModel = section["MainAgentModel"] ?? string.Empty,
            MainAgentRoleDescription = section["MainAgentRoleDescription"] ?? "结合 SubAgent 的需求简报撰写正式文案，专注内容创作。",
            SubAgentProvider = section["SubAgentProvider"] ?? "LlamaCpp",
            SubAgentModel = section["SubAgentModel"] ?? string.Empty,
            SubAgentRoleDescription = section["SubAgentRoleDescription"] ?? "总结写作需求，整理 MainAgent 草稿，并将纯净内容写入项目档案库。"
        };
    }

    private async Task<ChatResponse> GenerateRequirementBriefAsync(
        string taskType,
        Dictionary<string, object> parameters,
        AgentRoleWorkflowSettings settings,
        ResolvedProviderInfo provider,
        CancellationToken cancellationToken)
    {
        var request = new ChatRequest
        {
            Model = settings.SubAgentModel,
            SystemPrompt = BuildSubAgentRequirementSystemPrompt(settings),
            Messages = new List<ChatMessage>
            {
                new()
                {
                    Role = "user",
                    Content = BuildSubAgentRequirementUserPrompt(taskType, parameters),
                    Timestamp = DateTime.UtcNow
                }
            },
            Temperature = 0.35,
            MaxTokens = 4000
        };

        var response = await _modelManager.ChatAsync(provider.ProviderName, request, cancellationToken);
        response.Content = AIOutputSanitizer.ExtractCleanOutput(response.Content);
        return response;
    }

    private async Task<ChatResponse> GenerateMainDraftAsync(
        string taskType,
        string requirementBrief,
        AgentRoleWorkflowSettings settings,
        ResolvedProviderInfo provider,
        CancellationToken cancellationToken)
    {
        var request = new ChatRequest
        {
            Model = settings.MainAgentModel,
            SystemPrompt = BuildMainAgentSystemPrompt(taskType, settings),
            Messages = new List<ChatMessage>
            {
                new()
                {
                    Role = "user",
                    Content = BuildMainAgentUserPrompt(taskType, requirementBrief),
                    Timestamp = DateTime.UtcNow
                }
            },
            Temperature = taskType == "PolishText" ? 0.55 : 0.85,
            MaxTokens = 6000
        };

        var response = await _modelManager.ChatAsync(provider.ProviderName, request, cancellationToken);
        response.Content = AIOutputSanitizer.ExtractCleanOutput(response.Content);
        return response;
    }

    private async Task<ChatResponse> RefineFinalContentAsync(
        string taskType,
        string requirementBrief,
        string mainDraft,
        AgentRoleWorkflowSettings settings,
        ResolvedProviderInfo provider,
        CancellationToken cancellationToken)
    {
        var request = new ChatRequest
        {
            Model = settings.SubAgentModel,
            SystemPrompt = BuildSubAgentRefineSystemPrompt(taskType, settings),
            Messages = new List<ChatMessage>
            {
                new()
                {
                    Role = "user",
                    Content = BuildSubAgentRefineUserPrompt(taskType, requirementBrief, mainDraft),
                    Timestamp = DateTime.UtcNow
                }
            },
            Temperature = 0.25,
            MaxTokens = 6000
        };

        var response = await _modelManager.ChatAsync(provider.ProviderName, request, cancellationToken);
        response.Content = AIOutputSanitizer.ExtractCleanOutput(response.Content);
        return response;
    }

    private ResolvedProviderInfo? ResolveProvider(string configuredProvider)
    {
        if (string.IsNullOrWhiteSpace(configuredProvider))
        {
            return null;
        }

        var provider = _modelManager.GetAllProviders()
            .FirstOrDefault(candidate => string.Equals(candidate.ProviderName, configuredProvider, StringComparison.OrdinalIgnoreCase));
        if (provider == null || !provider.IsAvailable)
        {
            return null;
        }

        return new ResolvedProviderInfo
        {
            ProviderName = provider.ProviderName,
            DefaultModelName = TryResolveDefaultModelName(provider.ProviderName)
        };
    }

    private string BuildSubAgentRequirementSystemPrompt(AgentRoleWorkflowSettings settings)
    {
        // 语种模板注册表优先（占位符 {RoleDescription}）
        var template = NovelManagement.AI.Utilities.PromptTemplate.Get("Workflow/SubAgentRequirement.System");
        if (template != null)
        {
            return template.Replace("{RoleDescription}", settings.SubAgentRoleDescription ?? string.Empty);
        }

        if (GenEn)
        {
            return
                $"You are SubAgent. Duty: {EnMainRoleText}" +
                "First summarize the writing requirements into a clean, actionable brief for MainAgent to write from." +
                "Internal thinking is allowed, but the final output must not contain thinking, tags, JSON, code blocks, or explanatory prefixes/suffixes.";
        }

        return
            $"你是 SubAgent。职责：{settings.SubAgentRoleDescription}" +
            "你需要先总结写作需求，输出一份纯净、可执行的需求简报，供 MainAgent 直接写作使用。" +
            "允许内部 thinking，但最终输出中不得包含思考过程、标签、JSON、代码块、解释性前后缀。";
    }

    private string BuildMainAgentSystemPrompt(string taskType, AgentRoleWorkflowSettings settings)
    {
        // 语种模板注册表优先（占位符 {RoleDescription}/{TaskName}）
        var template = NovelManagement.AI.Utilities.PromptTemplate.Get("Workflow/MainAgent.System");
        if (template != null)
        {
            return template
                .Replace("{RoleDescription}", settings.MainAgentRoleDescription ?? string.Empty)
                .Replace("{TaskName}", GetTaskDisplayName(taskType));
        }

        if (GenEn)
        {
            return
                $"You are MainAgent. Duty: {EnMainRoleText}" +
                $"The current task is {GetTaskDisplayName(taskType)}." +
                "Based solely on SubAgent's requirement brief, produce the content draft." +
                "Internal thinking is allowed, but output only the draft content, no explanations.";
        }

        return
            $"你是 MainAgent。职责：{settings.MainAgentRoleDescription}" +
            $"当前任务是 {GetTaskDisplayName(taskType)}。" +
            "你只需基于 SubAgent 的需求简报完成正文草稿。" +
            "允许内部 thinking，但最终只输出草稿正文，不要解释过程。";
    }

    private string BuildSubAgentRefineSystemPrompt(string taskType, AgentRoleWorkflowSettings settings)
    {
        // 语种模板注册表优先（占位符 {RoleDescription}/{TaskName}）
        var template = NovelManagement.AI.Utilities.PromptTemplate.Get("Workflow/SubAgentRefine.System");
        if (template != null)
        {
            return template
                .Replace("{RoleDescription}", settings.SubAgentRoleDescription ?? string.Empty)
                .Replace("{TaskName}", GetTaskDisplayName(taskType));
        }

        if (GenEn)
        {
            return
                $"You are SubAgent. Duty: {EnMainRoleText}" +
                $"Finalize the output for task {GetTaskDisplayName(taskType)}." +
                "Strip thinking, explanations, prompt residue, tags, redundant headings, and verbal filler from MainAgent's draft; keep only publication-ready content." +
                "The final output must be strictly clean.";
        }

        return
            $"你是 SubAgent。职责：{settings.SubAgentRoleDescription}" +
            $"当前任务是 {GetTaskDisplayName(taskType)} 的定稿整理。" +
            "请清理 MainAgent 草稿中的思考、解释、提示词残留、标签、多余标题和口头说明，只保留可直接入库的正式内容。" +
            "最终输出必须严格纯净。";
    }

    private string BuildSubAgentRequirementUserPrompt(string taskType, Dictionary<string, object> parameters)
    {
        var builder = new StringBuilder();
        if (GenEn)
        {
            builder.AppendLine($"Task type: {GetTaskDisplayName(taskType)}");
            builder.AppendLine("Organize the raw requirements below into a short writing brief (max 8 lines, plain text, no section headings):");
            builder.AppendLine("- Line 1: the story goal in one sentence;");
            builder.AppendLine("- Line 2: style and tone;");
            builder.AppendLine("- Up to three more lines: characters and settings that must appear.");
            builder.AppendLine("All content must be written in English. Do not output JSON, tags, code blocks, or any explanation.");
            builder.AppendLine();
            builder.AppendLine("Raw parameters:");
            builder.AppendLine(SerializeParameters(parameters));
            return builder.ToString();
        }

        builder.AppendLine($"任务类型：{GetTaskDisplayName(taskType)}");
        builder.AppendLine("请将以下原始需求整理成一份简短的写作简报（不超过8行，纯文本，不要分节标题）：");
        builder.AppendLine("- 第一行：一句话故事目标；");
        builder.AppendLine("- 第二行：风格与语气；");
        builder.AppendLine("- 其后最多三行：必须出现的人物与设定要点。");
        builder.AppendLine("不要输出 JSON、标签、代码块或任何解释。");
        builder.AppendLine();
        builder.AppendLine("原始参数：");
        builder.AppendLine(SerializeParameters(parameters));
        return builder.ToString();
    }

    private string BuildMainAgentUserPrompt(string taskType, string requirementBrief)
    {
        if (GenEn)
        {
            return
                $"Task type: {GetTaskDisplayName(taskType)}{Environment.NewLine}" +
                "Below is SubAgent's requirement brief. Write a high-quality content draft from it directly. No explanations."
                + Environment.NewLine + GetTaskFormatInstruction(taskType)
                + Environment.NewLine + Environment.NewLine + requirementBrief;
        }

        return
            $"任务类型：{GetTaskDisplayName(taskType)}{Environment.NewLine}" +
            "以下是 SubAgent 输出的需求简报，请据此直接生成高质量文案草稿。不要输出解释。"
            + Environment.NewLine + GetTaskFormatInstruction(taskType)
            + Environment.NewLine + Environment.NewLine + requirementBrief;
    }

    private string BuildSubAgentRefineUserPrompt(string taskType, string requirementBrief, string mainDraft)
    {
        var builder = new StringBuilder();
        if (GenEn)
        {
            builder.AppendLine($"Task type: {GetTaskDisplayName(taskType)}");
            builder.AppendLine("Based on the requirement brief and MainAgent's draft, output the final clean version.");
            builder.AppendLine(GetTaskFormatInstruction(taskType));
            builder.AppendLine();
            builder.AppendLine("Requirement brief:");
            builder.AppendLine(requirementBrief);
            builder.AppendLine();
            builder.AppendLine("MainAgent draft:");
            builder.AppendLine(mainDraft);
            return builder.ToString();
        }

        builder.AppendLine($"任务类型：{GetTaskDisplayName(taskType)}");
        builder.AppendLine("请基于需求简报与 MainAgent 草稿，输出最终纯净定稿。");
        builder.AppendLine(GetTaskFormatInstruction(taskType));
        builder.AppendLine();
        builder.AppendLine("需求简报：");
        builder.AppendLine(requirementBrief);
        builder.AppendLine();
        builder.AppendLine("MainAgent 草稿：");
        builder.AppendLine(mainDraft);
        return builder.ToString();
    }

    /// <summary>英文模式（界面语言为 en-US 时生成链路输出英文内容）。</summary>
    private static bool GenEn => Localization.LocalizationManager.IsEnglish;

    /// <summary>英文模式下的 MainAgent 角色描述（取自词条表英文列）。</summary>
    private static string EnMainRoleText =>
        Localization.LocalizationManager.T("AICfg.MainAgentRoleText", "Write final prose from SubAgent's requirement briefs, focused on content creation.");

    private static string GetTaskFormatInstruction(string taskType)
    {
        if (GenEn)
        {
            return taskType switch
            {
                "GenerateChapterContent" => "Format: output only the chapter prose (narrative story content starting from scene and character action, with dialogue and plot progression, at least 400 words), no explanations, no title prefix, no extra notes; never write it as a manual, technical document, or bullet list. All content must be in English.",
                "ContinueChapter" => "Format: output only the continued prose, no explanations, no labels such as \"Continued\".",
                "PolishText" => "Format: output only the polished full text, no reviews, no diff notes.",
                "GenerateOutline" => "Format: output only the story outline (core conflict, volume/stage-based plot progression, main character arcs) in narrative form; no explanations, no self-description; never write it as a project plan or technical document. All content must be in English.",
                _ => "Format: output only clean, publication-ready content."
            };
        }

        return taskType switch
        {
            "GenerateChapterContent" => "格式要求：只输出书籍章节正文（叙事性故事内容，从场景与人物动作切入，含对话与情节推进，篇幅不少于500字），不要解释，不要标题前缀，不要额外备注；严禁写成说明书、技术文档或要点罗列。",
            "ContinueChapter" => "格式要求：只输出续写后的正文内容，不要解释，不要加“续写内容”等标签。",
            "PolishText" => "格式要求：只输出润色后的完整文本，不要评价，不要差异说明。",
            "GenerateOutline" => "格式要求：只输出书籍故事大纲（核心冲突、分卷或分阶段剧情推进、主要角色走向），使用叙事性描述，不要解释，不要自我说明；严禁写成项目方案或技术文档。",
            _ => "格式要求：只输出纯净正式内容。"
        };
    }

    private static bool IsSupportedTask(string taskType)
    {
        return taskType is "GenerateChapterContent" or "ContinueChapter" or "PolishText" or "GenerateOutline";
    }

    /// <summary>
    /// 判断内容是否仍是“需求简报/结构化说明”而非正文（用于小模型回显守卫）。
    /// </summary>
    /// <summary>计算文本中 CJK 字符占比（按全部字符）。</summary>
    private static double ComputeCjkRatio(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var cjk = 0;
        foreach (var ch in text)
        {
            if (ch >= 0x4E00 && ch <= 0x9FFF)
            {
                cjk++;
            }
        }

        return (double)cjk / text.Length;
    }

    private static bool LooksLikeRequirementBrief(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        // 中文简报特征（>=2 即判定）
        var markers = new[] { "核心目标", "必须保留的信息", "输出格式要求", "需求简报", "写作简报", "禁止项" };
        var hitCount = markers.Count(marker => content.Contains(marker, StringComparison.Ordinal));
        if (hitCount >= 2)
        {
            return true;
        }

        // 英文简报特征：回显必然携带双代理结构标签（实测英文模式踩坑）
        var enMarkers = new[]
        {
            "SubAgent requirement brief", "Requirement brief:", "Requirement Brief:",
            "MainAgent draft", "MainAgent Draft", "MainAgent's draft", "tasktype:chapterwriting",
            "Tasktype:chapterwriting", "chapterwriting"
        };
        return enMarkers.Any(marker => content.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 从简报回显中提取 MainAgent draft 之后的正文（大小写/空格丢失均兼容）。
    /// </summary>
    private static bool TryExtractMainAgentDraft(string content, out string draft)
    {
        draft = string.Empty;
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        // 回显可能丢失空格（流式分词缺陷），用无空格形式匹配标记
        var compact = content.Replace(" ", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
        var markers = new[] { "MainAgentdraft:", "MainAgent'sdraft:", "MainAgentDraft:" };
        var idx = -1;
        var markerLen = 0;
        foreach (var marker in markers)
        {
            var pos = compact.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (pos >= 0 && (idx < 0 || pos < idx))
            {
                idx = pos;
                markerLen = marker.Length;
            }
        }

        if (idx < 0)
        {
            return false;
        }

        // 在原文中按压缩后偏移的字符比例定位（压缩仅删除空格/换行，比例近似安全）
        var ratio = (double)idx / Math.Max(1, compact.Length);
        var rawIdx = (int)(content.Length * ratio);
        // 从 rawIdx 向后找到真正的 draft 起点附近（跳过最多 markerLen+80 个字符）
        var windowEnd = Math.Min(content.Length, rawIdx + markerLen + 120);
        var slice = content.Substring(rawIdx, windowEnd - rawIdx);
        var m = System.Text.RegularExpressions.Regex.Match(
            slice,
            @"MainAgent('s)?\s*[Dd]raft\s*:\s*",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (!m.Success)
        {
            return false;
        }

        draft = content[(rawIdx + m.Index + m.Length)..].Trim();
        return draft.Length > 0;
    }

    private static string GetTaskDisplayName(string taskType)
    {
        if (GenEn)
        {
            return taskType switch
            {
                "GenerateChapterContent" => "chapter writing",
                "ContinueChapter" => "chapter continuation",
                "PolishText" => "text polishing",
                "GenerateOutline" => "book outline generation",
                _ => taskType
            };
        }

        return taskType switch
        {
            "GenerateChapterContent" => "章节生成",
            "ContinueChapter" => "章节续写",
            "PolishText" => "文本润色",
            "GenerateOutline" => "书籍大纲生成",
            _ => taskType
        };
    }

    private static string SerializeParameters(Dictionary<string, object> parameters)
    {
        // 注意：此处不能使用 JsonSerializer——其默认编码会把中文转义为 \uXXXX，
        // 小参数量的本地模型（如 RWKV-1.5B）会照抄转义序列污染产出。改为纯文本行。
        var builder = new StringBuilder();
        foreach (var pair in parameters.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            // ProjectId 等系统字段对创作无意义，剔除以免干扰模型
            if (pair.Key.Equals("ProjectId", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            builder.AppendLine($"{pair.Key}：{pair.Value}");
        }

        return builder.ToString().TrimEnd();
    }

    private Guid? TryResolveProjectId(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("ProjectId", out var projectIdValue) || projectIdValue == null)
        {
            return null;
        }

        if (projectIdValue is Guid guid && guid != Guid.Empty)
        {
            return guid;
        }

        return Guid.TryParse(projectIdValue.ToString(), out var parsedGuid) && parsedGuid != Guid.Empty
            ? parsedGuid
            : null;
    }

    private static string? BuildTitleHint(string taskType, Dictionary<string, object> parameters)
    {
        var preferredKeys = taskType switch
        {
            "GenerateChapterContent" => new[] { "ChapterTitle", "Title" },
            "ContinueChapter" => new[] { "ChapterTitle", "Title" },
            "GenerateOutline" => new[] { "theme", "Title" },
            "PolishText" => new[] { "Title", "DocumentTitle" },
            _ => Array.Empty<string>()
        };

        foreach (var key in preferredKeys)
        {
            if (parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value?.ToString()))
            {
                return value!.ToString();
            }
        }

        return null;
    }

    private string TryResolveDefaultModelName(string providerName)
    {
        var configured = _configuration[$"AI:Providers:{providerName}:DefaultModel"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return providerName switch
        {
            "LlamaCpp" => _configuration["AI:Providers:LlamaCpp:DefaultModel"]
                ?? System.IO.Path.GetFileNameWithoutExtension(_configuration["AI:Providers:LlamaCpp:ModelPath"] ?? string.Empty)
                ?? "local-gguf",
            "DeepSeek" => _configuration["AI:Providers:DeepSeek:Model"]
                ?? _configuration["AI:Providers:DeepSeek:DefaultModel"]
                ?? "deepseek-v4-flash",
            _ => string.Empty
        };
    }
}

public sealed class AIAgentRoleWorkflowResult
{
    public bool IsSuccess { get; init; }
    public string Content { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();

    public static AIAgentRoleWorkflowResult Failed(string message)
    {
        return new AIAgentRoleWorkflowResult
        {
            Message = message
        };
    }
}

internal sealed class AgentRoleWorkflowSettings
{
    public bool EnableDualAgentWorkflow { get; init; }
    public bool EnableArchiveWrite { get; init; }
    public string MainAgentProvider { get; init; } = string.Empty;
    public string MainAgentModel { get; init; } = string.Empty;
    public string MainAgentRoleDescription { get; init; } = string.Empty;
    public string SubAgentProvider { get; init; } = string.Empty;
    public string SubAgentModel { get; init; } = string.Empty;
    public string SubAgentRoleDescription { get; init; } = string.Empty;
}

internal sealed class ResolvedProviderInfo
{
    public string ProviderName { get; init; } = string.Empty;
    public string DefaultModelName { get; init; } = string.Empty;
}
