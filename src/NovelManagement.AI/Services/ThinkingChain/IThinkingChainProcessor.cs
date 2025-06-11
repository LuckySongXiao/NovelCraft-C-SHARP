using NovelManagement.AI.Services.ThinkingChain.Models;
using ThinkingChainModel = NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain;

namespace NovelManagement.AI.Services.ThinkingChain
{
    /// <summary>
    /// 思维链处理器接口
    /// </summary>
    public interface IThinkingChainProcessor
    {
        /// <summary>
        /// 思维链更新事件
        /// </summary>
        event EventHandler<ThinkingChainUpdateEventArgs>? ThinkingChainUpdated;

        /// <summary>
        /// 步骤更新事件
        /// </summary>
        event EventHandler<ThinkingStepUpdateEventArgs>? StepUpdated;

        /// <summary>
        /// 解析思维链文本
        /// </summary>
        /// <param name="thinkingText">思维链原始文本</param>
        /// <param name="taskId">任务ID</param>
        /// <param name="agentId">Agent ID</param>
        /// <returns>解析后的思维链</returns>
        Task<ThinkingChainModel> ParseThinkingChainAsync(string thinkingText, string? taskId = null, string? agentId = null);

        /// <summary>
        /// 流式处理思维链
        /// </summary>
        /// <param name="thinkingChain">思维链对象</param>
        /// <param name="thinkingText">思维链文本</param>
        /// <param name="onStepUpdated">步骤更新回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理任务</returns>
        Task ProcessThinkingChainStreamAsync(
            ThinkingChainModel thinkingChain,
            string thinkingText,
            Action<ThinkingStep>? onStepUpdated = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 过滤和优化思维链
        /// </summary>
        /// <param name="thinkingChain">原始思维链</param>
        /// <param name="filterOptions">过滤选项</param>
        /// <returns>过滤后的思维链</returns>
        Task<ThinkingChainModel> FilterThinkingChainAsync(ThinkingChainModel thinkingChain, ThinkingChainFilterOptions? filterOptions = null);

        /// <summary>
        /// 提取关键思维步骤
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        /// <param name="maxSteps">最大步骤数</param>
        /// <returns>关键步骤列表</returns>
        Task<List<ThinkingStep>> ExtractKeyStepsAsync(ThinkingChainModel thinkingChain, int maxSteps = 5);

        /// <summary>
        /// 生成思维链摘要
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        /// <returns>摘要文本</returns>
        Task<string> GenerateSummaryAsync(ThinkingChainModel thinkingChain);

        /// <summary>
        /// 验证思维链逻辑
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        /// <returns>验证结果</returns>
        Task<ThinkingChainValidationResult> ValidateLogicAsync(ThinkingChainModel thinkingChain);

        /// <summary>
        /// 合并多个思维链
        /// </summary>
        /// <param name="thinkingChains">思维链列表</param>
        /// <param name="mergeStrategy">合并策略</param>
        /// <returns>合并后的思维链</returns>
        Task<ThinkingChainModel> MergeThinkingChainsAsync(List<ThinkingChainModel> thinkingChains, ThinkingChainMergeStrategy mergeStrategy = ThinkingChainMergeStrategy.Sequential);

        /// <summary>
        /// 导出思维链
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        /// <param name="format">导出格式</param>
        /// <returns>导出内容</returns>
        Task<string> ExportThinkingChainAsync(ThinkingChainModel thinkingChain, ThinkingChainExportFormat format = ThinkingChainExportFormat.Markdown);

        /// <summary>
        /// 获取思维链统计信息
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        /// <returns>统计信息</returns>
        ThinkingChainStatistics GetStatistics(ThinkingChainModel thinkingChain);
    }

    /// <summary>
    /// 思维链过滤选项
    /// </summary>
    public class ThinkingChainFilterOptions
    {
        /// <summary>
        /// 最小置信度阈值
        /// </summary>
        public double MinConfidence { get; set; } = 0.3;

        /// <summary>
        /// 是否移除重复步骤
        /// </summary>
        public bool RemoveDuplicates { get; set; } = true;

        /// <summary>
        /// 是否合并相似步骤
        /// </summary>
        public bool MergeSimilarSteps { get; set; } = true;

        /// <summary>
        /// 最大步骤数
        /// </summary>
        public int? MaxSteps { get; set; }

        /// <summary>
        /// 包含的步骤类型
        /// </summary>
        public List<ThinkingStepType>? IncludeStepTypes { get; set; }

        /// <summary>
        /// 排除的步骤类型
        /// </summary>
        public List<ThinkingStepType>? ExcludeStepTypes { get; set; }

        /// <summary>
        /// 是否保留子步骤
        /// </summary>
        public bool PreserveSubSteps { get; set; } = true;
    }

    /// <summary>
    /// 思维链验证结果
    /// </summary>
    public class ThinkingChainValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 逻辑一致性评分 (0-1)
        /// </summary>
        public double LogicConsistencyScore { get; set; }

        /// <summary>
        /// 完整性评分 (0-1)
        /// </summary>
        public double CompletenessScore { get; set; }

        /// <summary>
        /// 连贯性评分 (0-1)
        /// </summary>
        public double CoherenceScore { get; set; }

        /// <summary>
        /// 发现的问题
        /// </summary>
        public List<string> Issues { get; set; } = new();

        /// <summary>
        /// 改进建议
        /// </summary>
        public List<string> Suggestions { get; set; } = new();

        /// <summary>
        /// 总体评分 (0-1)
        /// </summary>
        public double OverallScore => (LogicConsistencyScore + CompletenessScore + CoherenceScore) / 3;
    }

    /// <summary>
    /// 思维链合并策略
    /// </summary>
    public enum ThinkingChainMergeStrategy
    {
        /// <summary>
        /// 顺序合并
        /// </summary>
        Sequential,

        /// <summary>
        /// 并行合并
        /// </summary>
        Parallel,

        /// <summary>
        /// 层次合并
        /// </summary>
        Hierarchical,

        /// <summary>
        /// 智能合并
        /// </summary>
        Intelligent
    }

    /// <summary>
    /// 思维链导出格式
    /// </summary>
    public enum ThinkingChainExportFormat
    {
        /// <summary>
        /// Markdown格式
        /// </summary>
        Markdown,

        /// <summary>
        /// JSON格式
        /// </summary>
        Json,

        /// <summary>
        /// XML格式
        /// </summary>
        Xml,

        /// <summary>
        /// 纯文本格式
        /// </summary>
        PlainText,

        /// <summary>
        /// HTML格式
        /// </summary>
        Html
    }

    /// <summary>
    /// 思维链统计信息
    /// </summary>
    public class ThinkingChainStatistics
    {
        /// <summary>
        /// 总步骤数
        /// </summary>
        public int TotalSteps { get; set; }

        /// <summary>
        /// 各类型步骤数量
        /// </summary>
        public Dictionary<ThinkingStepType, int> StepTypeCount { get; set; } = new();

        /// <summary>
        /// 平均置信度
        /// </summary>
        public double AverageConfidence { get; set; }

        /// <summary>
        /// 总处理时间
        /// </summary>
        public TimeSpan TotalProcessingTime { get; set; }

        /// <summary>
        /// 平均步骤处理时间
        /// </summary>
        public TimeSpan AverageStepTime { get; set; }

        /// <summary>
        /// 最长步骤处理时间
        /// </summary>
        public TimeSpan MaxStepTime { get; set; }

        /// <summary>
        /// 最短步骤处理时间
        /// </summary>
        public TimeSpan MinStepTime { get; set; }

        /// <summary>
        /// 复杂度评分 (0-1)
        /// </summary>
        public double ComplexityScore { get; set; }
    }

    /// <summary>
    /// 思维链更新事件参数
    /// </summary>
    public class ThinkingChainUpdateEventArgs : EventArgs
    {
        /// <summary>
        /// 思维链
        /// </summary>
        public ThinkingChainModel ThinkingChain { get; }

        /// <summary>
        /// 更新类型
        /// </summary>
        public ThinkingChainUpdateType UpdateType { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        /// <param name="updateType">更新类型</param>
        public ThinkingChainUpdateEventArgs(ThinkingChainModel thinkingChain, ThinkingChainUpdateType updateType)
        {
            ThinkingChain = thinkingChain;
            UpdateType = updateType;
        }
    }

    /// <summary>
    /// 思维步骤更新事件参数
    /// </summary>
    public class ThinkingStepUpdateEventArgs : EventArgs
    {
        /// <summary>
        /// 思维步骤
        /// </summary>
        public ThinkingStep Step { get; }

        /// <summary>
        /// 更新类型
        /// </summary>
        public ThinkingStepUpdateType UpdateType { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="step">思维步骤</param>
        /// <param name="updateType">更新类型</param>
        public ThinkingStepUpdateEventArgs(ThinkingStep step, ThinkingStepUpdateType updateType)
        {
            Step = step;
            UpdateType = updateType;
        }
    }

    /// <summary>
    /// 思维链更新类型
    /// </summary>
    public enum ThinkingChainUpdateType
    {
        /// <summary>
        /// 创建
        /// </summary>
        Created,

        /// <summary>
        /// 开始处理
        /// </summary>
        Started,

        /// <summary>
        /// 进度更新
        /// </summary>
        ProgressUpdated,

        /// <summary>
        /// 完成
        /// </summary>
        Completed,

        /// <summary>
        /// 失败
        /// </summary>
        Failed,

        /// <summary>
        /// 取消
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// 思维步骤更新类型
    /// </summary>
    public enum ThinkingStepUpdateType
    {
        /// <summary>
        /// 添加
        /// </summary>
        Added,

        /// <summary>
        /// 开始
        /// </summary>
        Started,

        /// <summary>
        /// 内容更新
        /// </summary>
        ContentUpdated,

        /// <summary>
        /// 完成
        /// </summary>
        Completed,

        /// <summary>
        /// 失败
        /// </summary>
        Failed
    }
}
