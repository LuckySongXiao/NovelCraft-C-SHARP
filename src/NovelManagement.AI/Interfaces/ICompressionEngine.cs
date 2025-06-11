using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NovelManagement.AI.Interfaces
{
    /// <summary>
    /// 记忆压缩引擎接口
    /// </summary>
    public interface ICompressionEngine
    {
        /// <summary>
        /// 评估内容重要性
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="context">上下文信息</param>
        /// <returns>重要性评分（1-10）</returns>
        Task<int> EvaluateImportanceAsync(string content, MemoryContext context);

        /// <summary>
        /// 压缩低重要性信息
        /// </summary>
        /// <param name="memoryItems">记忆项列表</param>
        /// <param name="compressionThreshold">压缩阈值（重要性评分低于此值的将被压缩）</param>
        /// <returns>压缩结果</returns>
        Task<List<MemoryItem>> CompressLowImportanceAsync(List<MemoryItem> memoryItems, int compressionThreshold = 5);

        /// <summary>
        /// 优化记忆检索
        /// </summary>
        /// <param name="query">查询内容</param>
        /// <param name="memoryItems">记忆项列表</param>
        /// <param name="maxResults">最大结果数</param>
        /// <returns>优化后的检索结果</returns>
        Task<List<MemoryItem>> OptimizeRetrievalAsync(string query, List<MemoryItem> memoryItems, int maxResults = 10);

        /// <summary>
        /// 生成内容摘要
        /// </summary>
        /// <param name="content">原始内容</param>
        /// <param name="maxLength">最大摘要长度</param>
        /// <returns>摘要内容</returns>
        Task<string> GenerateSummaryAsync(string content, int maxLength = 200);

        /// <summary>
        /// 合并相似记忆项
        /// </summary>
        /// <param name="memoryItems">记忆项列表</param>
        /// <param name="similarityThreshold">相似度阈值（0-1）</param>
        /// <returns>合并后的记忆项列表</returns>
        Task<List<MemoryItem>> MergeSimilarMemoriesAsync(List<MemoryItem> memoryItems, double similarityThreshold = 0.8);

        /// <summary>
        /// 计算内容相似度
        /// </summary>
        /// <param name="content1">内容1</param>
        /// <param name="content2">内容2</param>
        /// <returns>相似度（0-1）</returns>
        Task<double> CalculateSimilarityAsync(string content1, string content2);

        /// <summary>
        /// 提取关键词
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="maxKeywords">最大关键词数量</param>
        /// <returns>关键词列表</returns>
        Task<List<string>> ExtractKeywordsAsync(string content, int maxKeywords = 10);

        /// <summary>
        /// 分析内容类型
        /// </summary>
        /// <param name="content">内容</param>
        /// <returns>内容类型</returns>
        Task<MemoryType> AnalyzeContentTypeAsync(string content);
    }

    /// <summary>
    /// 压缩策略枚举
    /// </summary>
    public enum CompressionStrategy
    {
        /// <summary>
        /// 基于重要性的压缩
        /// </summary>
        ImportanceBased,

        /// <summary>
        /// 基于时间的压缩
        /// </summary>
        TimeBased,

        /// <summary>
        /// 基于访问频率的压缩
        /// </summary>
        AccessFrequencyBased,

        /// <summary>
        /// 基于相似性的合并
        /// </summary>
        SimilarityBased,

        /// <summary>
        /// 混合策略
        /// </summary>
        Hybrid
    }

    /// <summary>
    /// 压缩配置
    /// </summary>
    public class CompressionConfig
    {
        /// <summary>
        /// 压缩策略
        /// </summary>
        public CompressionStrategy Strategy { get; set; } = CompressionStrategy.Hybrid;

        /// <summary>
        /// 重要性阈值（低于此值的记忆项将被压缩）
        /// </summary>
        public int ImportanceThreshold { get; set; } = 5;

        /// <summary>
        /// 时间阈值（超过此天数的记忆项将被考虑压缩）
        /// </summary>
        public int TimeThresholdDays { get; set; } = 30;

        /// <summary>
        /// 访问频率阈值（低于此频率的记忆项将被考虑压缩）
        /// </summary>
        public int AccessFrequencyThreshold { get; set; } = 3;

        /// <summary>
        /// 相似度阈值（高于此值的记忆项将被考虑合并）
        /// </summary>
        public double SimilarityThreshold { get; set; } = 0.8;

        /// <summary>
        /// 最大压缩比例（0-1）
        /// </summary>
        public double MaxCompressionRatio { get; set; } = 0.5;

        /// <summary>
        /// 是否保留高重要性记忆项
        /// </summary>
        public bool PreserveHighImportance { get; set; } = true;

        /// <summary>
        /// 高重要性阈值
        /// </summary>
        public int HighImportanceThreshold { get; set; } = 8;

        /// <summary>
        /// 摘要最大长度
        /// </summary>
        public int SummaryMaxLength { get; set; } = 200;

        /// <summary>
        /// 是否启用语义分析
        /// </summary>
        public bool EnableSemanticAnalysis { get; set; } = true;

        /// <summary>
        /// 是否启用关键词提取
        /// </summary>
        public bool EnableKeywordExtraction { get; set; } = true;
    }

    /// <summary>
    /// 重要性评估因子
    /// </summary>
    public class ImportanceFactors
    {
        /// <summary>
        /// 内容长度权重
        /// </summary>
        public double ContentLengthWeight { get; set; } = 0.1;

        /// <summary>
        /// 关键词密度权重
        /// </summary>
        public double KeywordDensityWeight { get; set; } = 0.2;

        /// <summary>
        /// 实体关联权重
        /// </summary>
        public double EntityRelationWeight { get; set; } = 0.3;

        /// <summary>
        /// 时间新鲜度权重
        /// </summary>
        public double TimeRecencyWeight { get; set; } = 0.1;

        /// <summary>
        /// 访问频率权重
        /// </summary>
        public double AccessFrequencyWeight { get; set; } = 0.2;

        /// <summary>
        /// 用户标记权重
        /// </summary>
        public double UserMarkingWeight { get; set; } = 0.1;
    }

    /// <summary>
    /// 语义分析结果
    /// </summary>
    public class SemanticAnalysisResult
    {
        /// <summary>
        /// 主题标签
        /// </summary>
        public List<string> Topics { get; set; } = new();

        /// <summary>
        /// 情感倾向（-1到1）
        /// </summary>
        public double SentimentScore { get; set; }

        /// <summary>
        /// 实体列表
        /// </summary>
        public List<string> Entities { get; set; } = new();

        /// <summary>
        /// 关键短语
        /// </summary>
        public List<string> KeyPhrases { get; set; } = new();

        /// <summary>
        /// 语言复杂度（1-10）
        /// </summary>
        public int ComplexityScore { get; set; }

        /// <summary>
        /// 信息密度（1-10）
        /// </summary>
        public int InformationDensity { get; set; }
    }
}
