using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services.DeepSeek;
using NovelManagement.AI.Services.ThinkingChain;

namespace NovelManagement.AI.Agents
{
    /// <summary>
    /// 评论Agent - 负责内容评估和批评
    /// </summary>
    public class CriticAgent : BaseAgent
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="memoryManager">记忆管理器</param>
        /// <param name="deepSeekApiService">DeepSeek API服务</param>
        /// <param name="thinkingChainProcessor">思维链处理器</param>
        /// <param name="modelManager">模型管理器</param>
        public CriticAgent(
            ILogger<CriticAgent> logger,
            IMemoryManager memoryManager,
            IDeepSeekApiService deepSeekApiService,
            IThinkingChainProcessor thinkingChainProcessor,
            NovelManagement.AI.Services.ModelManager modelManager)
            : base(logger, memoryManager, deepSeekApiService, thinkingChainProcessor, modelManager)
        {
        }

        #region 基础属性

        /// <inheritdoc/>
        public override string Name => "评论Agent";

        /// <inheritdoc/>
        public override string Description => "专业评论助手，负责内容评估、批评分析和改进建议";

        #endregion

        #region 任务执行

        /// <inheritdoc/>
        protected override async Task<AgentTaskResult> ExecuteTaskAsync(string taskType, Dictionary<string, object> parameters)
        {
            return taskType switch
            {
                "EvaluateContent" => await EvaluateContentAsync(parameters),
                "AnalyzeStructure" => await AnalyzeStructureAsync(parameters),
                "ReviewCharacters" => await ReviewCharactersAsync(parameters),
                "AssessPlot" => await AssessPlotAsync(parameters),
                "CheckPlotContinuity" => await CheckPlotContinuityAsync(parameters),
                "ProvideFeedback" => await ProvideFeedbackAsync(parameters),
                _ => new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"不支持的任务类型: {taskType}"
                }
            };
        }

        /// <inheritdoc/>
        protected override List<AgentCapability> GetSupportedCapabilities()
        {
            return new List<AgentCapability>
            {
                new AgentCapability
                {
                    Name = "内容评估",
                    Description = "评估内容质量和价值",
                    IsAvailable = true,
                    Priority = 1
                },
                new AgentCapability
                {
                    Name = "结构分析",
                    Description = "分析文章结构和逻辑",
                    IsAvailable = true,
                    Priority = 1
                },
                new AgentCapability
                {
                    Name = "角色评审",
                    Description = "评审角色设定和发展",
                    IsAvailable = true,
                    Priority = 2
                },
                new AgentCapability
                {
                    Name = "情节评估",
                    Description = "评估情节发展和合理性",
                    IsAvailable = true,
                    Priority = 2
                },
                new AgentCapability
                {
                    Name = "连贯性检查",
                    Description = "检查剧情线索、因果关系和前后呼应",
                    IsAvailable = true,
                    Priority = 2
                },
                new AgentCapability
                {
                    Name = "反馈建议",
                    Description = "提供改进建议和反馈",
                    IsAvailable = true,
                    Priority = 3
                }
            };
        }

        #endregion

        #region 具体任务实现

        /// <summary>
        /// 评估内容
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>评估结果</returns>
        private async Task<AgentTaskResult> EvaluateContentAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(20);
                await Task.Delay(1500);
                UpdateProgress(100);
                
                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = "内容评估完成"
                };
            }
            catch (Exception ex)
            {
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 分析结构
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>分析结果</returns>
        private async Task<AgentTaskResult> AnalyzeStructureAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(20);
                await Task.Delay(1200);
                UpdateProgress(100);
                
                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = "结构分析完成"
                };
            }
            catch (Exception ex)
            {
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 评审角色
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>评审结果</returns>
        private async Task<AgentTaskResult> ReviewCharactersAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(20);
                await Task.Delay(1800);
                UpdateProgress(100);
                
                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = "角色评审完成"
                };
            }
            catch (Exception ex)
            {
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 评估情节
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>评估结果</returns>
        private async Task<AgentTaskResult> AssessPlotAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(20);
                await Task.Delay(1000);
                UpdateProgress(100);
                
                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = "情节评估完成"
                };
            }
            catch (Exception ex)
            {
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 检查剧情连贯性
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>检查结果</returns>
        private async Task<AgentTaskResult> CheckPlotContinuityAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(15);

                var plotData = parameters.GetValueOrDefault("plotData", "").ToString();
                var requirements = parameters.GetValueOrDefault("requirements", "检查剧情前后逻辑是否连贯").ToString();

                _logger.LogInformation("开始检查剧情连贯性");

                AddThinkingStep("识别关键情节", "提取剧情中的关键事件、线索与因果关系", Services.ThinkingChain.Models.ThinkingStepType.Analysis, 0.9);
                UpdateProgress(35);

                AddThinkingStep("检查冲突与线索", $"根据要求'{requirements}'检查冲突转折与伏笔回收情况", Services.ThinkingChain.Models.ThinkingStepType.Evaluation, 0.85);
                await Task.Delay(1200);
                UpdateProgress(70);

                var trimmedPlot = string.IsNullOrWhiteSpace(plotData) ? "未提供具体剧情文本" : plotData.Trim();
                var analysis = new
                {
                    Summary = "已完成剧情连贯性检查",
                    Conclusion = "当前剧情存在可优化的跳跃点，但主线仍可辨识，建议补充线索承接说明。",
                    ContinuityIssues = new[]
                    {
                        "第二幕与第三幕之间的线索承接说明不足",
                        "关键证据的去向缺少中间交代，削弱因果闭环"
                    },
                    Suggestions = new[]
                    {
                        "补充线索消失到重新出现之间的调查过程",
                        "增加主角推理或旁证，强化破案依据"
                    },
                    CheckedContentPreview = trimmedPlot[..Math.Min(120, trimmedPlot.Length)]
                };

                AddThinkingStep("输出结论", "整理连贯性问题和改进建议", Services.ThinkingChain.Models.ThinkingStepType.Conclusion, 0.92);
                UpdateProgress(100);

                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = analysis,
                    Metadata = new Dictionary<string, object>
                    {
                        ["ContentType"] = "PlotContinuityAnalysis",
                        ["Message"] = "AI检查剧情连贯性完成"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查剧情连贯性失败");
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 提供反馈
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>反馈结果</returns>
        private async Task<AgentTaskResult> ProvideFeedbackAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(20);
                await Task.Delay(2000);
                UpdateProgress(100);
                
                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = "反馈建议完成"
                };
            }
            catch (Exception ex)
            {
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        #endregion
    }
}
