using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NovelManagement.AI.Interfaces
{
    /// <summary>
    /// 记忆管理器接口
    /// </summary>
    public interface IMemoryManager
    {
        /// <summary>
        /// 获取上下文信息
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <param name="scope">记忆范围</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="volumeId">卷宗ID（可选）</param>
        /// <param name="chapterId">章节ID（可选）</param>
        /// <returns>上下文信息</returns>
        Task<MemoryContext> GetContextAsync(string taskType, MemoryScope scope, Guid projectId, Guid? volumeId = null, Guid? chapterId = null);

        /// <summary>
        /// 更新记忆信息
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="importanceScore">重要性评分（1-10）</param>
        /// <param name="scope">记忆范围</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="volumeId">卷宗ID（可选）</param>
        /// <param name="chapterId">章节ID（可选）</param>
        /// <returns>更新结果</returns>
        Task<bool> UpdateMemoryAsync(string content, int importanceScore, MemoryScope scope, Guid projectId, Guid? volumeId = null, Guid? chapterId = null);

        /// <summary>
        /// 压缩记忆信息
        /// </summary>
        /// <param name="scope">记忆范围</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="volumeId">卷宗ID（可选）</param>
        /// <param name="chapterId">章节ID（可选）</param>
        /// <returns>压缩结果</returns>
        Task<MemoryCompressionResult> CompressMemoryAsync(MemoryScope scope, Guid projectId, Guid? volumeId = null, Guid? chapterId = null);

        /// <summary>
        /// 搜索记忆信息
        /// </summary>
        /// <param name="query">搜索查询</param>
        /// <param name="scope">记忆范围</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="maxResults">最大结果数</param>
        /// <returns>搜索结果</returns>
        Task<List<MemoryItem>> SearchMemoryAsync(string query, MemoryScope scope, Guid projectId, int maxResults = 10);

        /// <summary>
        /// 清理过期记忆
        /// </summary>
        /// <param name="scope">记忆范围</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="retentionDays">保留天数</param>
        /// <returns>清理结果</returns>
        Task<int> CleanupExpiredMemoryAsync(MemoryScope scope, Guid projectId, int retentionDays = 30);

        /// <summary>
        /// 获取记忆统计信息
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns>统计信息</returns>
        Task<MemoryStatistics> GetMemoryStatisticsAsync(Guid projectId);
    }

    /// <summary>
    /// 记忆范围枚举
    /// </summary>
    public enum MemoryScope
    {
        /// <summary>
        /// 全局记忆层
        /// </summary>
        Global,

        /// <summary>
        /// 卷宗记忆层
        /// </summary>
        Volume,

        /// <summary>
        /// 章节记忆层
        /// </summary>
        Chapter,

        /// <summary>
        /// 段落记忆层
        /// </summary>
        Paragraph
    }

    /// <summary>
    /// 记忆上下文
    /// </summary>
    public class MemoryContext
    {
        /// <summary>
        /// 上下文ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 任务类型
        /// </summary>
        public string TaskType { get; set; } = string.Empty;

        /// <summary>
        /// 记忆范围
        /// </summary>
        public MemoryScope Scope { get; set; }

        /// <summary>
        /// 项目ID
        /// </summary>
        public Guid ProjectId { get; set; }

        /// <summary>
        /// 卷宗ID
        /// </summary>
        public Guid? VolumeId { get; set; }

        /// <summary>
        /// 章节ID
        /// </summary>
        public Guid? ChapterId { get; set; }

        /// <summary>
        /// 相关记忆项列表
        /// </summary>
        public List<MemoryItem> RelevantMemories { get; set; } = new();

        /// <summary>
        /// 上下文摘要
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 总重要性评分
        /// </summary>
        public double TotalImportanceScore { get; set; }
    }

    /// <summary>
    /// 记忆项
    /// </summary>
    public class MemoryItem
    {
        /// <summary>
        /// 记忆项ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 重要性评分（1-10）
        /// </summary>
        public int ImportanceScore { get; set; }

        /// <summary>
        /// 记忆类型
        /// </summary>
        public MemoryType Type { get; set; }

        /// <summary>
        /// 记忆范围
        /// </summary>
        public MemoryScope Scope { get; set; }

        /// <summary>
        /// 项目ID
        /// </summary>
        public Guid ProjectId { get; set; }

        /// <summary>
        /// 卷宗ID
        /// </summary>
        public Guid? VolumeId { get; set; }

        /// <summary>
        /// 章节ID
        /// </summary>
        public Guid? ChapterId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后访问时间
        /// </summary>
        public DateTime LastAccessedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 访问次数
        /// </summary>
        public int AccessCount { get; set; } = 0;

        /// <summary>
        /// 是否已压缩
        /// </summary>
        public bool IsCompressed { get; set; } = false;

        /// <summary>
        /// 原始内容长度（压缩前）
        /// </summary>
        public int OriginalLength { get; set; }

        /// <summary>
        /// 标签列表
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// 关联的实体ID列表
        /// </summary>
        public List<Guid> RelatedEntityIds { get; set; } = new();
    }

    /// <summary>
    /// 记忆类型枚举
    /// </summary>
    public enum MemoryType
    {
        /// <summary>
        /// 世界设定
        /// </summary>
        WorldSetting,

        /// <summary>
        /// 角色信息
        /// </summary>
        Character,

        /// <summary>
        /// 剧情信息
        /// </summary>
        Plot,

        /// <summary>
        /// 对话内容
        /// </summary>
        Dialogue,

        /// <summary>
        /// 场景描述
        /// </summary>
        Scene,

        /// <summary>
        /// 事件记录
        /// </summary>
        Event,

        /// <summary>
        /// 关系信息
        /// </summary>
        Relationship,

        /// <summary>
        /// 系统信息
        /// </summary>
        System,

        /// <summary>
        /// 其他
        /// </summary>
        Other
    }

    /// <summary>
    /// 记忆压缩结果
    /// </summary>
    public class MemoryCompressionResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 压缩前的记忆项数量
        /// </summary>
        public int OriginalCount { get; set; }

        /// <summary>
        /// 压缩后的记忆项数量
        /// </summary>
        public int CompressedCount { get; set; }

        /// <summary>
        /// 压缩比例
        /// </summary>
        public double CompressionRatio => OriginalCount > 0 ? (double)CompressedCount / OriginalCount : 0;

        /// <summary>
        /// 释放的内存大小（字节）
        /// </summary>
        public long FreedMemoryBytes { get; set; }

        /// <summary>
        /// 压缩耗时
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 记忆统计信息
    /// </summary>
    public class MemoryStatistics
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        public Guid ProjectId { get; set; }

        /// <summary>
        /// 总记忆项数量
        /// </summary>
        public int TotalMemoryItems { get; set; }

        /// <summary>
        /// 各范围的记忆项数量
        /// </summary>
        public Dictionary<MemoryScope, int> MemoryCountByScope { get; set; } = new();

        /// <summary>
        /// 各类型的记忆项数量
        /// </summary>
        public Dictionary<MemoryType, int> MemoryCountByType { get; set; } = new();

        /// <summary>
        /// 总内存使用量（字节）
        /// </summary>
        public long TotalMemoryUsage { get; set; }

        /// <summary>
        /// 压缩率
        /// </summary>
        public double CompressionRatio { get; set; }

        /// <summary>
        /// 平均重要性评分
        /// </summary>
        public double AverageImportanceScore { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
