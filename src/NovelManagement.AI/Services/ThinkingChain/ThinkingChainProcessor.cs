using Microsoft.Extensions.Logging;
using NovelManagement.AI.Services.ThinkingChain.Models;
using System.Text.RegularExpressions;
using System.Text;
using ThinkingChainModel = NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain;

namespace NovelManagement.AI.Services.ThinkingChain
{
    /// <summary>
    /// 思维链处理器实现
    /// </summary>
    public class ThinkingChainProcessor : IThinkingChainProcessor
    {
        private readonly ILogger<ThinkingChainProcessor> _logger;

        /// <summary>
        /// 思维链更新事件
        /// </summary>
        public event EventHandler<ThinkingChainUpdateEventArgs>? ThinkingChainUpdated;

        /// <summary>
        /// 步骤更新事件
        /// </summary>
        public event EventHandler<ThinkingStepUpdateEventArgs>? StepUpdated;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public ThinkingChainProcessor(ILogger<ThinkingChainProcessor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 解析思维链文本
        /// </summary>
        /// <param name="thinkingText">思维链原始文本</param>
        /// <param name="taskId">任务ID</param>
        /// <param name="agentId">Agent ID</param>
        /// <returns>解析后的思维链</returns>
        public async Task<ThinkingChainModel> ParseThinkingChainAsync(string thinkingText, string? taskId = null, string? agentId = null)
        {
            try
            {
                _logger.LogDebug("开始解析思维链文本，长度: {Length}", thinkingText.Length);

                var thinkingChain = new ThinkingChainModel
                {
                    Title = "AI思维过程",
                    Description = "解析AI的思维推理过程",
                    TaskId = taskId,
                    AgentId = agentId,
                    OriginalInput = thinkingText
                };

                // 解析思维步骤
                var steps = await ParseThinkingStepsAsync(thinkingText);
                foreach (var step in steps)
                {
                    thinkingChain.AddStep(step);
                }

                thinkingChain.Complete();
                OnThinkingChainUpdated(new ThinkingChainUpdateEventArgs(thinkingChain, ThinkingChainUpdateType.Created));

                _logger.LogDebug("思维链解析完成，共 {StepCount} 个步骤", steps.Count);
                return thinkingChain;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析思维链文本失败");
                throw;
            }
        }

        /// <summary>
        /// 流式处理思维链
        /// </summary>
        /// <param name="thinkingChain">思维链对象</param>
        /// <param name="thinkingText">思维链文本</param>
        /// <param name="onStepUpdated">步骤更新回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理任务</returns>
        public async Task ProcessThinkingChainStreamAsync(
            ThinkingChainModel thinkingChain,
            string thinkingText,
            Action<ThinkingStep>? onStepUpdated = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("开始流式处理思维链");

                thinkingChain.Start();
                OnThinkingChainUpdated(new ThinkingChainUpdateEventArgs(thinkingChain, ThinkingChainUpdateType.Started));

                // 模拟流式处理，逐步添加思维步骤
                var steps = await ParseThinkingStepsAsync(thinkingText);
                
                for (int i = 0; i < steps.Count && !cancellationToken.IsCancellationRequested; i++)
                {
                    var step = steps[i];
                    step.Start();
                    
                    thinkingChain.AddStep(step);
                    OnStepUpdated(new ThinkingStepUpdateEventArgs(step, ThinkingStepUpdateType.Added));
                    
                    // 模拟处理延迟
                    await Task.Delay(100, cancellationToken);
                    
                    step.Complete();
                    OnStepUpdated(new ThinkingStepUpdateEventArgs(step, ThinkingStepUpdateType.Completed));
                    
                    thinkingChain.UpdateProgress();
                    OnThinkingChainUpdated(new ThinkingChainUpdateEventArgs(thinkingChain, ThinkingChainUpdateType.ProgressUpdated));
                    
                    onStepUpdated?.Invoke(step);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    thinkingChain.Complete();
                    OnThinkingChainUpdated(new ThinkingChainUpdateEventArgs(thinkingChain, ThinkingChainUpdateType.Completed));
                }

                _logger.LogDebug("思维链流式处理完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "流式处理思维链失败");
                thinkingChain.Fail();
                OnThinkingChainUpdated(new ThinkingChainUpdateEventArgs(thinkingChain, ThinkingChainUpdateType.Failed));
                throw;
            }
        }

        /// <summary>
        /// 过滤和优化思维链
        /// </summary>
        /// <param name="thinkingChain">原始思维链</param>
        /// <param name="filterOptions">过滤选项</param>
        /// <returns>过滤后的思维链</returns>
        public async Task<ThinkingChainModel> FilterThinkingChainAsync(ThinkingChainModel thinkingChain, ThinkingChainFilterOptions? filterOptions = null)
        {
            try
            {
                _logger.LogDebug("开始过滤思维链，原始步骤数: {StepCount}", thinkingChain.Steps.Count);

                filterOptions ??= new ThinkingChainFilterOptions();

                var filteredChain = new ThinkingChainModel
                {
                    Title = thinkingChain.Title + " (已过滤)",
                    Description = thinkingChain.Description,
                    TaskId = thinkingChain.TaskId,
                    AgentId = thinkingChain.AgentId,
                    OriginalInput = thinkingChain.OriginalInput
                };

                var filteredSteps = thinkingChain.Steps.Where(step =>
                {
                    // 置信度过滤
                    if (step.Confidence < filterOptions.MinConfidence)
                        return false;

                    // 步骤类型过滤
                    if (filterOptions.IncludeStepTypes != null && !filterOptions.IncludeStepTypes.Contains(step.Type))
                        return false;

                    if (filterOptions.ExcludeStepTypes != null && filterOptions.ExcludeStepTypes.Contains(step.Type))
                        return false;

                    return true;
                }).ToList();

                // 移除重复步骤
                if (filterOptions.RemoveDuplicates)
                {
                    filteredSteps = RemoveDuplicateSteps(filteredSteps);
                }

                // 限制最大步骤数
                if (filterOptions.MaxSteps.HasValue && filteredSteps.Count > filterOptions.MaxSteps.Value)
                {
                    filteredSteps = filteredSteps.Take(filterOptions.MaxSteps.Value).ToList();
                }

                foreach (var step in filteredSteps)
                {
                    filteredChain.AddStep(step);
                }

                filteredChain.Complete();

                _logger.LogDebug("思维链过滤完成，过滤后步骤数: {StepCount}", filteredChain.Steps.Count);
                return filteredChain;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "过滤思维链失败");
                throw;
            }
        }

        /// <summary>
        /// 提取关键思维步骤
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        /// <param name="maxSteps">最大步骤数</param>
        /// <returns>关键步骤列表</returns>
        public async Task<List<ThinkingStep>> ExtractKeyStepsAsync(ThinkingChainModel thinkingChain, int maxSteps = 5)
        {
            try
            {
                _logger.LogDebug("提取关键思维步骤，最大数量: {MaxSteps}", maxSteps);

                var keySteps = thinkingChain.Steps
                    .OrderByDescending(step => step.Confidence)
                    .ThenByDescending(step => step.Content.Length)
                    .Take(maxSteps)
                    .OrderBy(step => step.StepNumber)
                    .ToList();

                _logger.LogDebug("提取到 {Count} 个关键步骤", keySteps.Count);
                return keySteps;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提取关键思维步骤失败");
                throw;
            }
        }

        /// <summary>
        /// 生成思维链摘要
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        /// <returns>摘要文本</returns>
        public async Task<string> GenerateSummaryAsync(ThinkingChainModel thinkingChain)
        {
            try
            {
                _logger.LogDebug("生成思维链摘要");

                var summary = new StringBuilder();
                summary.AppendLine($"# {thinkingChain.Title}");
                summary.AppendLine($"**描述**: {thinkingChain.Description}");
                summary.AppendLine($"**步骤数**: {thinkingChain.TotalSteps}");
                summary.AppendLine($"**处理时间**: {thinkingChain.Duration.TotalSeconds:F2}秒");
                summary.AppendLine();

                var keySteps = await ExtractKeyStepsAsync(thinkingChain, 3);
                summary.AppendLine("## 关键步骤");
                foreach (var step in keySteps)
                {
                    summary.AppendLine($"- **{step.TypeDescription}**: {step.Content.Substring(0, Math.Min(100, step.Content.Length))}...");
                }

                if (!string.IsNullOrEmpty(thinkingChain.FinalOutput))
                {
                    summary.AppendLine();
                    summary.AppendLine("## 最终输出");
                    summary.AppendLine(thinkingChain.FinalOutput.Substring(0, Math.Min(200, thinkingChain.FinalOutput.Length)) + "...");
                }

                return summary.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成思维链摘要失败");
                throw;
            }
        }

        /// <summary>
        /// 验证思维链逻辑
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        /// <returns>验证结果</returns>
        public async Task<ThinkingChainValidationResult> ValidateLogicAsync(ThinkingChainModel thinkingChain)
        {
            try
            {
                _logger.LogDebug("验证思维链逻辑");

                var result = new ThinkingChainValidationResult();

                // 检查逻辑一致性
                result.LogicConsistencyScore = CalculateLogicConsistency(thinkingChain);

                // 检查完整性
                result.CompletenessScore = CalculateCompleteness(thinkingChain);

                // 检查连贯性
                result.CoherenceScore = CalculateCoherence(thinkingChain);

                // 判断是否有效
                result.IsValid = result.OverallScore >= 0.6;

                // 生成问题和建议
                if (result.LogicConsistencyScore < 0.5)
                {
                    result.Issues.Add("逻辑一致性较低，存在矛盾的推理步骤");
                    result.Suggestions.Add("检查并修正矛盾的推理逻辑");
                }

                if (result.CompletenessScore < 0.5)
                {
                    result.Issues.Add("思维链不够完整，缺少关键步骤");
                    result.Suggestions.Add("补充缺失的推理步骤");
                }

                if (result.CoherenceScore < 0.5)
                {
                    result.Issues.Add("思维链连贯性不足，步骤间缺乏逻辑联系");
                    result.Suggestions.Add("改善步骤间的逻辑连接");
                }

                _logger.LogDebug("思维链逻辑验证完成，总体评分: {Score:F2}", result.OverallScore);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证思维链逻辑失败");
                throw;
            }
        }

        #region 私有方法

        /// <summary>
        /// 解析思维步骤
        /// </summary>
        /// <param name="thinkingText">思维文本</param>
        /// <returns>思维步骤列表</returns>
        private async Task<List<ThinkingStep>> ParseThinkingStepsAsync(string thinkingText)
        {
            var steps = new List<ThinkingStep>();

            try
            {
                // 简单的文本分割策略
                var sentences = SplitIntoSentences(thinkingText);
                
                for (int i = 0; i < sentences.Count; i++)
                {
                    var sentence = sentences[i].Trim();
                    if (string.IsNullOrEmpty(sentence)) continue;

                    var step = new ThinkingStep
                    {
                        StepNumber = i + 1,
                        Title = $"思维步骤 {i + 1}",
                        Content = sentence,
                        Type = DetermineStepType(sentence),
                        Confidence = CalculateConfidence(sentence),
                        Status = ThinkingStepStatus.Completed
                    };

                    steps.Add(step);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解析思维步骤时出现错误");
            }

            return steps;
        }

        /// <summary>
        /// 将文本分割为句子
        /// </summary>
        /// <param name="text">输入文本</param>
        /// <returns>句子列表</returns>
        private List<string> SplitIntoSentences(string text)
        {
            // 使用正则表达式分割句子
            var pattern = @"[.!?。！？]+\s*";
            var sentences = Regex.Split(text, pattern, RegexOptions.IgnoreCase)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            return sentences;
        }

        /// <summary>
        /// 确定步骤类型
        /// </summary>
        /// <param name="content">步骤内容</param>
        /// <returns>步骤类型</returns>
        private ThinkingStepType DetermineStepType(string content)
        {
            var lowerContent = content.ToLower();

            if (lowerContent.Contains("分析") || lowerContent.Contains("analyze"))
                return ThinkingStepType.Analysis;
            if (lowerContent.Contains("计划") || lowerContent.Contains("plan"))
                return ThinkingStepType.Planning;
            if (lowerContent.Contains("推理") || lowerContent.Contains("reason"))
                return ThinkingStepType.Reasoning;
            if (lowerContent.Contains("评估") || lowerContent.Contains("evaluate"))
                return ThinkingStepType.Evaluation;
            if (lowerContent.Contains("综合") || lowerContent.Contains("synthesis"))
                return ThinkingStepType.Synthesis;
            if (lowerContent.Contains("验证") || lowerContent.Contains("verify"))
                return ThinkingStepType.Verification;
            if (lowerContent.Contains("结论") || lowerContent.Contains("conclusion"))
                return ThinkingStepType.Conclusion;

            return ThinkingStepType.Reasoning; // 默认为推理
        }

        /// <summary>
        /// 计算置信度
        /// </summary>
        /// <param name="content">内容</param>
        /// <returns>置信度</returns>
        private double CalculateConfidence(string content)
        {
            // 简单的置信度计算逻辑
            var baseConfidence = 0.5;
            
            // 根据内容长度调整
            if (content.Length > 50) baseConfidence += 0.2;
            if (content.Length > 100) baseConfidence += 0.1;
            
            // 根据关键词调整
            var keywords = new[] { "因为", "所以", "因此", "由于", "基于", "根据" };
            var keywordCount = keywords.Count(kw => content.Contains(kw));
            baseConfidence += keywordCount * 0.1;

            return Math.Min(1.0, baseConfidence);
        }

        /// <summary>
        /// 移除重复步骤
        /// </summary>
        /// <param name="steps">步骤列表</param>
        /// <returns>去重后的步骤列表</returns>
        private List<ThinkingStep> RemoveDuplicateSteps(List<ThinkingStep> steps)
        {
            var uniqueSteps = new List<ThinkingStep>();
            var seenContents = new HashSet<string>();

            foreach (var step in steps)
            {
                var normalizedContent = step.Content.Trim().ToLower();
                if (!seenContents.Contains(normalizedContent))
                {
                    seenContents.Add(normalizedContent);
                    uniqueSteps.Add(step);
                }
            }

            return uniqueSteps;
        }

        /// <summary>
        /// 计算逻辑一致性
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        /// <returns>一致性评分</returns>
        private double CalculateLogicConsistency(ThinkingChainModel thinkingChain)
        {
            // 简化的逻辑一致性计算
            if (thinkingChain.Steps.Count == 0) return 0.0;

            var consistencyScore = 0.8; // 基础分数
            
            // 检查步骤间的逻辑连接
            for (int i = 1; i < thinkingChain.Steps.Count; i++)
            {
                var prevStep = thinkingChain.Steps[i - 1];
                var currentStep = thinkingChain.Steps[i];
                
                // 简单的连贯性检查
                if (HasLogicalConnection(prevStep.Content, currentStep.Content))
                {
                    consistencyScore += 0.05;
                }
                else
                {
                    consistencyScore -= 0.1;
                }
            }

            return Math.Max(0.0, Math.Min(1.0, consistencyScore));
        }

        /// <summary>
        /// 计算完整性
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        /// <returns>完整性评分</returns>
        private double CalculateCompleteness(ThinkingChainModel thinkingChain)
        {
            if (thinkingChain.Steps.Count == 0) return 0.0;

            var completenessScore = 0.5; // 基础分数

            // 检查是否包含不同类型的步骤
            var stepTypes = thinkingChain.Steps.Select(s => s.Type).Distinct().Count();
            completenessScore += stepTypes * 0.1;

            // 检查是否有结论
            if (thinkingChain.Steps.Any(s => s.Type == ThinkingStepType.Conclusion))
            {
                completenessScore += 0.2;
            }

            return Math.Min(1.0, completenessScore);
        }

        /// <summary>
        /// 计算连贯性
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        /// <returns>连贯性评分</returns>
        private double CalculateCoherence(ThinkingChainModel thinkingChain)
        {
            if (thinkingChain.Steps.Count <= 1) return 1.0;

            var coherenceScore = 0.7; // 基础分数
            var connectedSteps = 0;

            for (int i = 1; i < thinkingChain.Steps.Count; i++)
            {
                var prevStep = thinkingChain.Steps[i - 1];
                var currentStep = thinkingChain.Steps[i];

                if (HasLogicalConnection(prevStep.Content, currentStep.Content))
                {
                    connectedSteps++;
                }
            }

            var connectionRatio = (double)connectedSteps / (thinkingChain.Steps.Count - 1);
            coherenceScore = coherenceScore * 0.5 + connectionRatio * 0.5;

            return coherenceScore;
        }

        /// <summary>
        /// 检查两个内容间是否有逻辑连接
        /// </summary>
        /// <param name="content1">内容1</param>
        /// <param name="content2">内容2</param>
        /// <returns>是否有逻辑连接</returns>
        private bool HasLogicalConnection(string content1, string content2)
        {
            // 简化的逻辑连接检查
            var connectionWords = new[] { "因此", "所以", "然后", "接下来", "基于", "根据", "由于" };
            return connectionWords.Any(word => content2.Contains(word));
        }

        /// <summary>
        /// 触发思维链更新事件
        /// </summary>
        /// <param name="args">事件参数</param>
        protected virtual void OnThinkingChainUpdated(ThinkingChainUpdateEventArgs args)
        {
            ThinkingChainUpdated?.Invoke(this, args);
        }

        /// <summary>
        /// 触发步骤更新事件
        /// </summary>
        /// <param name="args">事件参数</param>
        protected virtual void OnStepUpdated(ThinkingStepUpdateEventArgs args)
        {
            StepUpdated?.Invoke(this, args);
        }

        #endregion

        #region 其他接口方法实现

        /// <summary>
        /// 合并多个思维链
        /// </summary>
        /// <param name="thinkingChains">思维链列表</param>
        /// <param name="mergeStrategy">合并策略</param>
        /// <returns>合并后的思维链</returns>
        public async Task<ThinkingChainModel> MergeThinkingChainsAsync(List<ThinkingChainModel> thinkingChains, ThinkingChainMergeStrategy mergeStrategy = ThinkingChainMergeStrategy.Sequential)
        {
            try
            {
                _logger.LogDebug("合并 {Count} 个思维链，策略: {Strategy}", thinkingChains.Count, mergeStrategy);

                if (thinkingChains.Count == 0)
                    throw new ArgumentException("思维链列表不能为空");

                if (thinkingChains.Count == 1)
                    return thinkingChains[0];

                var mergedChain = new ThinkingChainModel
                {
                    Title = "合并的思维链",
                    Description = $"合并了 {thinkingChains.Count} 个思维链",
                    TaskId = Guid.NewGuid().ToString(),
                    AgentId = "MergedAgent"
                };

                switch (mergeStrategy)
                {
                    case ThinkingChainMergeStrategy.Sequential:
                        await MergeSequentially(mergedChain, thinkingChains);
                        break;
                    case ThinkingChainMergeStrategy.Parallel:
                        await MergeInParallel(mergedChain, thinkingChains);
                        break;
                    case ThinkingChainMergeStrategy.Hierarchical:
                        await MergeHierarchically(mergedChain, thinkingChains);
                        break;
                    case ThinkingChainMergeStrategy.Intelligent:
                        await MergeIntelligently(mergedChain, thinkingChains);
                        break;
                }

                mergedChain.Complete();
                _logger.LogDebug("思维链合并完成，总步骤数: {StepCount}", mergedChain.Steps.Count);
                return mergedChain;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "合并思维链失败");
                throw;
            }
        }

        /// <summary>
        /// 导出思维链
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        /// <param name="format">导出格式</param>
        /// <returns>导出内容</returns>
        public async Task<string> ExportThinkingChainAsync(ThinkingChainModel thinkingChain, ThinkingChainExportFormat format = ThinkingChainExportFormat.Markdown)
        {
            try
            {
                _logger.LogDebug("导出思维链，格式: {Format}", format);

                return format switch
                {
                    ThinkingChainExportFormat.Markdown => await ExportToMarkdownAsync(thinkingChain),
                    ThinkingChainExportFormat.Json => await ExportToJsonAsync(thinkingChain),
                    ThinkingChainExportFormat.Xml => await ExportToXmlAsync(thinkingChain),
                    ThinkingChainExportFormat.PlainText => await ExportToPlainTextAsync(thinkingChain),
                    ThinkingChainExportFormat.Html => await ExportToHtmlAsync(thinkingChain),
                    _ => throw new ArgumentException($"不支持的导出格式: {format}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出思维链失败");
                throw;
            }
        }

        /// <summary>
        /// 获取思维链统计信息
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        /// <returns>统计信息</returns>
        public ThinkingChainStatistics GetStatistics(ThinkingChainModel thinkingChain)
        {
            try
            {
                var statistics = new ThinkingChainStatistics
                {
                    TotalSteps = thinkingChain.Steps.Count,
                    TotalProcessingTime = thinkingChain.Duration,
                    ComplexityScore = CalculateComplexityScore(thinkingChain)
                };

                // 统计各类型步骤数量
                foreach (var step in thinkingChain.Steps)
                {
                    if (statistics.StepTypeCount.ContainsKey(step.Type))
                        statistics.StepTypeCount[step.Type]++;
                    else
                        statistics.StepTypeCount[step.Type] = 1;
                }

                // 计算平均置信度
                if (thinkingChain.Steps.Count > 0)
                {
                    statistics.AverageConfidence = thinkingChain.Steps.Average(s => s.Confidence);
                }

                // 计算时间统计
                var stepDurations = thinkingChain.Steps.Select(s => s.Duration).ToList();
                if (stepDurations.Count > 0)
                {
                    statistics.AverageStepTime = TimeSpan.FromTicks((long)stepDurations.Average(d => d.Ticks));
                    statistics.MaxStepTime = stepDurations.Max();
                    statistics.MinStepTime = stepDurations.Min();
                }

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取思维链统计信息失败");
                throw;
            }
        }

        #endregion

        #region 合并策略实现

        /// <summary>
        /// 顺序合并
        /// </summary>
        private async Task MergeSequentially(ThinkingChainModel mergedChain, List<ThinkingChainModel> chains)
        {
            foreach (var chain in chains)
            {
                foreach (var step in chain.Steps)
                {
                    mergedChain.AddStep(step);
                }
            }
        }

        /// <summary>
        /// 并行合并
        /// </summary>
        private async Task MergeInParallel(ThinkingChainModel mergedChain, List<ThinkingChainModel> chains)
        {
            var maxSteps = chains.Max(c => c.Steps.Count);

            for (int i = 0; i < maxSteps; i++)
            {
                foreach (var chain in chains)
                {
                    if (i < chain.Steps.Count)
                    {
                        mergedChain.AddStep(chain.Steps[i]);
                    }
                }
            }
        }

        /// <summary>
        /// 层次合并
        /// </summary>
        private async Task MergeHierarchically(ThinkingChainModel mergedChain, List<ThinkingChainModel> chains)
        {
            // 按重要性排序后合并
            var sortedChains = chains.OrderByDescending(c => c.Steps.Average(s => s.Confidence)).ToList();
            await MergeSequentially(mergedChain, sortedChains);
        }

        /// <summary>
        /// 智能合并
        /// </summary>
        private async Task MergeIntelligently(ThinkingChainModel mergedChain, List<ThinkingChainModel> chains)
        {
            // 提取所有步骤并按置信度和类型排序
            var allSteps = chains.SelectMany(c => c.Steps)
                .OrderBy(s => s.Type)
                .ThenByDescending(s => s.Confidence)
                .ToList();

            foreach (var step in allSteps)
            {
                mergedChain.AddStep(step);
            }
        }

        #endregion

        #region 导出格式实现

        /// <summary>
        /// 导出为Markdown格式
        /// </summary>
        private async Task<string> ExportToMarkdownAsync(ThinkingChainModel thinkingChain)
        {
            var markdown = new StringBuilder();

            markdown.AppendLine($"# {thinkingChain.Title}");
            markdown.AppendLine();
            markdown.AppendLine($"**描述**: {thinkingChain.Description}");
            markdown.AppendLine($"**状态**: {thinkingChain.StatusDescription}");
            markdown.AppendLine($"**开始时间**: {thinkingChain.StartTime:yyyy-MM-dd HH:mm:ss}");
            if (thinkingChain.EndTime.HasValue)
            {
                markdown.AppendLine($"**结束时间**: {thinkingChain.EndTime:yyyy-MM-dd HH:mm:ss}");
                markdown.AppendLine($"**持续时间**: {thinkingChain.Duration.TotalSeconds:F2}秒");
            }
            markdown.AppendLine();

            markdown.AppendLine("## 思维步骤");
            foreach (var step in thinkingChain.Steps)
            {
                markdown.AppendLine($"### {step.StepNumber}. {step.Title}");
                markdown.AppendLine($"**类型**: {step.TypeDescription}");
                markdown.AppendLine($"**置信度**: {step.ConfidencePercentage}");
                markdown.AppendLine($"**状态**: {step.StatusDescription}");
                markdown.AppendLine();
                markdown.AppendLine(step.Content);
                markdown.AppendLine();
            }

            if (!string.IsNullOrEmpty(thinkingChain.FinalOutput))
            {
                markdown.AppendLine("## 最终输出");
                markdown.AppendLine(thinkingChain.FinalOutput);
            }

            return markdown.ToString();
        }

        /// <summary>
        /// 导出为JSON格式
        /// </summary>
        private async Task<string> ExportToJsonAsync(ThinkingChainModel thinkingChain)
        {
            return System.Text.Json.JsonSerializer.Serialize(thinkingChain, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        }

        /// <summary>
        /// 导出为XML格式
        /// </summary>
        private async Task<string> ExportToXmlAsync(ThinkingChainModel thinkingChain)
        {
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<ThinkingChain>");
            xml.AppendLine($"  <Title>{System.Security.SecurityElement.Escape(thinkingChain.Title)}</Title>");
            xml.AppendLine($"  <Description>{System.Security.SecurityElement.Escape(thinkingChain.Description)}</Description>");
            xml.AppendLine($"  <Status>{thinkingChain.Status}</Status>");
            xml.AppendLine($"  <StartTime>{thinkingChain.StartTime:yyyy-MM-ddTHH:mm:ss}</StartTime>");
            if (thinkingChain.EndTime.HasValue)
            {
                xml.AppendLine($"  <EndTime>{thinkingChain.EndTime:yyyy-MM-ddTHH:mm:ss}</EndTime>");
            }

            xml.AppendLine("  <Steps>");
            foreach (var step in thinkingChain.Steps)
            {
                xml.AppendLine("    <Step>");
                xml.AppendLine($"      <StepNumber>{step.StepNumber}</StepNumber>");
                xml.AppendLine($"      <Title>{System.Security.SecurityElement.Escape(step.Title)}</Title>");
                xml.AppendLine($"      <Type>{step.Type}</Type>");
                xml.AppendLine($"      <Status>{step.Status}</Status>");
                xml.AppendLine($"      <Confidence>{step.Confidence}</Confidence>");
                xml.AppendLine($"      <Content>{System.Security.SecurityElement.Escape(step.Content)}</Content>");
                xml.AppendLine("    </Step>");
            }
            xml.AppendLine("  </Steps>");

            if (!string.IsNullOrEmpty(thinkingChain.FinalOutput))
            {
                xml.AppendLine($"  <FinalOutput>{System.Security.SecurityElement.Escape(thinkingChain.FinalOutput)}</FinalOutput>");
            }

            xml.AppendLine("</ThinkingChain>");
            return xml.ToString();
        }

        /// <summary>
        /// 导出为纯文本格式
        /// </summary>
        private async Task<string> ExportToPlainTextAsync(ThinkingChainModel thinkingChain)
        {
            var text = new StringBuilder();

            text.AppendLine($"思维链: {thinkingChain.Title}");
            text.AppendLine($"描述: {thinkingChain.Description}");
            text.AppendLine($"状态: {thinkingChain.StatusDescription}");
            text.AppendLine($"开始时间: {thinkingChain.StartTime:yyyy-MM-dd HH:mm:ss}");
            if (thinkingChain.EndTime.HasValue)
            {
                text.AppendLine($"结束时间: {thinkingChain.EndTime:yyyy-MM-dd HH:mm:ss}");
                text.AppendLine($"持续时间: {thinkingChain.Duration.TotalSeconds:F2}秒");
            }
            text.AppendLine();

            text.AppendLine("思维步骤:");
            foreach (var step in thinkingChain.Steps)
            {
                text.AppendLine($"{step.StepNumber}. {step.Title} ({step.TypeDescription})");
                text.AppendLine($"   置信度: {step.ConfidencePercentage}");
                text.AppendLine($"   内容: {step.Content}");
                text.AppendLine();
            }

            if (!string.IsNullOrEmpty(thinkingChain.FinalOutput))
            {
                text.AppendLine("最终输出:");
                text.AppendLine(thinkingChain.FinalOutput);
            }

            return text.ToString();
        }

        /// <summary>
        /// 导出为HTML格式
        /// </summary>
        private async Task<string> ExportToHtmlAsync(ThinkingChainModel thinkingChain)
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("  <meta charset=\"UTF-8\">");
            html.AppendLine($"  <title>{System.Security.SecurityElement.Escape(thinkingChain.Title)}</title>");
            html.AppendLine("  <style>");
            html.AppendLine("    body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("    .step { margin: 10px 0; padding: 10px; border-left: 3px solid #007acc; background-color: #f9f9f9; }");
            html.AppendLine("    .step-header { font-weight: bold; color: #007acc; }");
            html.AppendLine("    .step-meta { font-size: 0.9em; color: #666; }");
            html.AppendLine("  </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            html.AppendLine($"  <h1>{System.Security.SecurityElement.Escape(thinkingChain.Title)}</h1>");
            html.AppendLine($"  <p><strong>描述:</strong> {System.Security.SecurityElement.Escape(thinkingChain.Description)}</p>");
            html.AppendLine($"  <p><strong>状态:</strong> {thinkingChain.StatusDescription}</p>");
            html.AppendLine($"  <p><strong>开始时间:</strong> {thinkingChain.StartTime:yyyy-MM-dd HH:mm:ss}</p>");
            if (thinkingChain.EndTime.HasValue)
            {
                html.AppendLine($"  <p><strong>结束时间:</strong> {thinkingChain.EndTime:yyyy-MM-dd HH:mm:ss}</p>");
                html.AppendLine($"  <p><strong>持续时间:</strong> {thinkingChain.Duration.TotalSeconds:F2}秒</p>");
            }

            html.AppendLine("  <h2>思维步骤</h2>");
            foreach (var step in thinkingChain.Steps)
            {
                html.AppendLine("  <div class=\"step\">");
                html.AppendLine($"    <div class=\"step-header\">{step.StepNumber}. {System.Security.SecurityElement.Escape(step.Title)}</div>");
                html.AppendLine($"    <div class=\"step-meta\">类型: {step.TypeDescription} | 置信度: {step.ConfidencePercentage} | 状态: {step.StatusDescription}</div>");
                html.AppendLine($"    <p>{System.Security.SecurityElement.Escape(step.Content)}</p>");
                html.AppendLine("  </div>");
            }

            if (!string.IsNullOrEmpty(thinkingChain.FinalOutput))
            {
                html.AppendLine("  <h2>最终输出</h2>");
                html.AppendLine($"  <p>{System.Security.SecurityElement.Escape(thinkingChain.FinalOutput)}</p>");
            }

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        /// <summary>
        /// 计算复杂度评分
        /// </summary>
        private double CalculateComplexityScore(ThinkingChainModel thinkingChain)
        {
            if (thinkingChain.Steps.Count == 0) return 0.0;

            var complexityScore = 0.0;

            // 基于步骤数量
            complexityScore += Math.Min(0.3, thinkingChain.Steps.Count * 0.05);

            // 基于步骤类型多样性
            var uniqueTypes = thinkingChain.Steps.Select(s => s.Type).Distinct().Count();
            complexityScore += Math.Min(0.3, uniqueTypes * 0.05);

            // 基于平均内容长度
            var avgContentLength = thinkingChain.Steps.Average(s => s.Content.Length);
            complexityScore += Math.Min(0.2, avgContentLength / 1000);

            // 基于子步骤
            var hasSubSteps = thinkingChain.Steps.Any(s => s.HasSubSteps);
            if (hasSubSteps) complexityScore += 0.2;

            return Math.Min(1.0, complexityScore);
        }

        #endregion
    }
}
