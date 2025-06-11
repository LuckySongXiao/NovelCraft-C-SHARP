using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;

namespace NovelManagement.AI.Memory.MemoryLayers
{
    /// <summary>
    /// 记忆层基础类
    /// </summary>
    public abstract class BaseMemoryLayer
    {
        protected readonly ILogger _logger;
        protected readonly ICompressionEngine _compressionEngine;
        protected readonly List<MemoryItem> _memories = new();
        protected readonly object _lockObject = new object();

        /// <summary>
        /// 记忆层范围
        /// </summary>
        public abstract MemoryScope Scope { get; }

        /// <summary>
        /// 最大记忆容量
        /// </summary>
        public virtual int MaxCapacity { get; protected set; } = 1000;

        /// <summary>
        /// 当前记忆数量
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lockObject)
                {
                    return _memories.Count;
                }
            }
        }

        /// <summary>
        /// 是否已满
        /// </summary>
        public bool IsFull => Count >= MaxCapacity;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="compressionEngine">压缩引擎</param>
        protected BaseMemoryLayer(ILogger logger, ICompressionEngine compressionEngine)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _compressionEngine = compressionEngine ?? throw new ArgumentNullException(nameof(compressionEngine));
        }

        /// <summary>
        /// 添加记忆项
        /// </summary>
        /// <param name="memoryItem">记忆项</param>
        /// <returns>是否成功添加</returns>
        public virtual async Task<bool> AddMemoryAsync(MemoryItem memoryItem)
        {
            try
            {
                if (memoryItem == null)
                    throw new ArgumentNullException(nameof(memoryItem));

                _logger.LogDebug($"向 {Scope} 层添加记忆项: {memoryItem.Id}");

                // 检查容量
                if (IsFull)
                {
                    await TriggerCompressionAsync();
                }

                lock (_lockObject)
                {
                    // 检查是否已存在
                    if (_memories.Any(m => m.Id == memoryItem.Id))
                    {
                        _logger.LogWarning($"记忆项 {memoryItem.Id} 已存在，跳过添加");
                        return false;
                    }

                    _memories.Add(memoryItem);
                }

                _logger.LogDebug($"成功添加记忆项到 {Scope} 层，当前数量: {Count}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"添加记忆项到 {Scope} 层失败");
                return false;
            }
        }

        /// <summary>
        /// 获取记忆项
        /// </summary>
        /// <param name="memoryId">记忆项ID</param>
        /// <returns>记忆项</returns>
        public virtual MemoryItem? GetMemory(string memoryId)
        {
            lock (_lockObject)
            {
                var memory = _memories.FirstOrDefault(m => m.Id == memoryId);
                if (memory != null)
                {
                    memory.LastAccessedAt = DateTime.Now;
                    memory.AccessCount++;
                }
                return memory;
            }
        }

        /// <summary>
        /// 搜索记忆项
        /// </summary>
        /// <param name="query">搜索查询</param>
        /// <param name="maxResults">最大结果数</param>
        /// <returns>搜索结果</returns>
        public virtual async Task<List<MemoryItem>> SearchAsync(string query, int maxResults = 10)
        {
            try
            {
                List<MemoryItem> memories;
                lock (_lockObject)
                {
                    memories = new List<MemoryItem>(_memories);
                }

                var results = await _compressionEngine.OptimizeRetrievalAsync(query, memories, maxResults);
                
                // 更新访问统计
                foreach (var result in results)
                {
                    result.LastAccessedAt = DateTime.Now;
                    result.AccessCount++;
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"在 {Scope} 层搜索记忆失败");
                return new List<MemoryItem>();
            }
        }

        /// <summary>
        /// 获取所有记忆项
        /// </summary>
        /// <returns>记忆项列表</returns>
        public virtual List<MemoryItem> GetAllMemories()
        {
            lock (_lockObject)
            {
                return new List<MemoryItem>(_memories);
            }
        }

        /// <summary>
        /// 按重要性获取记忆项
        /// </summary>
        /// <param name="minImportance">最小重要性</param>
        /// <param name="maxResults">最大结果数</param>
        /// <returns>记忆项列表</returns>
        public virtual List<MemoryItem> GetMemoriesByImportance(int minImportance, int maxResults = 50)
        {
            lock (_lockObject)
            {
                return _memories
                    .Where(m => m.ImportanceScore >= minImportance)
                    .OrderByDescending(m => m.ImportanceScore)
                    .Take(maxResults)
                    .ToList();
            }
        }

        /// <summary>
        /// 按类型获取记忆项
        /// </summary>
        /// <param name="memoryType">记忆类型</param>
        /// <param name="maxResults">最大结果数</param>
        /// <returns>记忆项列表</returns>
        public virtual List<MemoryItem> GetMemoriesByType(MemoryType memoryType, int maxResults = 50)
        {
            lock (_lockObject)
            {
                return _memories
                    .Where(m => m.Type == memoryType)
                    .OrderByDescending(m => m.ImportanceScore)
                    .Take(maxResults)
                    .ToList();
            }
        }

        /// <summary>
        /// 更新记忆项
        /// </summary>
        /// <param name="memoryItem">记忆项</param>
        /// <returns>是否成功更新</returns>
        public virtual bool UpdateMemory(MemoryItem memoryItem)
        {
            try
            {
                lock (_lockObject)
                {
                    var index = _memories.FindIndex(m => m.Id == memoryItem.Id);
                    if (index >= 0)
                    {
                        _memories[index] = memoryItem;
                        _logger.LogDebug($"更新 {Scope} 层记忆项: {memoryItem.Id}");
                        return true;
                    }
                }

                _logger.LogWarning($"未找到要更新的记忆项: {memoryItem.Id}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新 {Scope} 层记忆项失败");
                return false;
            }
        }

        /// <summary>
        /// 删除记忆项
        /// </summary>
        /// <param name="memoryId">记忆项ID</param>
        /// <returns>是否成功删除</returns>
        public virtual bool RemoveMemory(string memoryId)
        {
            try
            {
                lock (_lockObject)
                {
                    var removed = _memories.RemoveAll(m => m.Id == memoryId);
                    if (removed > 0)
                    {
                        _logger.LogDebug($"从 {Scope} 层删除记忆项: {memoryId}");
                        return true;
                    }
                }

                _logger.LogWarning($"未找到要删除的记忆项: {memoryId}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"从 {Scope} 层删除记忆项失败");
                return false;
            }
        }

        /// <summary>
        /// 压缩记忆
        /// </summary>
        /// <returns>压缩结果</returns>
        public virtual async Task<MemoryCompressionResult> CompressAsync()
        {
            var startTime = DateTime.Now;

            try
            {
                _logger.LogInformation($"开始压缩 {Scope} 层记忆");

                List<MemoryItem> memories;
                lock (_lockObject)
                {
                    memories = new List<MemoryItem>(_memories);
                }

                var originalCount = memories.Count;
                var originalSize = memories.Sum(m => m.Content.Length);

                // 执行压缩
                var compressedMemories = await _compressionEngine.CompressLowImportanceAsync(memories, GetCompressionThreshold());
                
                // 合并相似记忆
                compressedMemories = await _compressionEngine.MergeSimilarMemoriesAsync(compressedMemories, 0.8);

                var compressedCount = compressedMemories.Count;
                var compressedSize = compressedMemories.Sum(m => m.Content.Length);

                // 更新内存存储
                lock (_lockObject)
                {
                    _memories.Clear();
                    _memories.AddRange(compressedMemories);
                }

                var result = new MemoryCompressionResult
                {
                    IsSuccess = true,
                    OriginalCount = originalCount,
                    CompressedCount = compressedCount,
                    FreedMemoryBytes = originalSize - compressedSize,
                    Duration = DateTime.Now - startTime
                };

                _logger.LogInformation($"{Scope} 层记忆压缩完成: {originalCount} -> {compressedCount}, 压缩比 {result.CompressionRatio:P2}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Scope} 层记忆压缩失败");
                return new MemoryCompressionResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Duration = DateTime.Now - startTime
                };
            }
        }

        /// <summary>
        /// 清理过期记忆
        /// </summary>
        /// <param name="retentionDays">保留天数</param>
        /// <returns>清理的记忆项数量</returns>
        public virtual int CleanupExpiredMemories(int retentionDays = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                var removedCount = 0;

                lock (_lockObject)
                {
                    var originalCount = _memories.Count;
                    
                    // 保留高重要性记忆和最近访问的记忆
                    _memories.RemoveAll(m => 
                        m.ImportanceScore < GetHighImportanceThreshold() && 
                        m.LastAccessedAt < cutoffDate && 
                        m.CreatedAt < cutoffDate);
                    
                    removedCount = originalCount - _memories.Count;
                }

                _logger.LogInformation($"{Scope} 层清理完成，删除了 {removedCount} 个过期记忆项");
                return removedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Scope} 层清理过期记忆失败");
                return 0;
            }
        }

        /// <summary>
        /// 获取记忆统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public virtual MemoryLayerStatistics GetStatistics()
        {
            lock (_lockObject)
            {
                return new MemoryLayerStatistics
                {
                    Scope = Scope,
                    TotalCount = _memories.Count,
                    MaxCapacity = MaxCapacity,
                    UtilizationRate = (double)_memories.Count / MaxCapacity,
                    AverageImportanceScore = _memories.Count > 0 ? _memories.Average(m => m.ImportanceScore) : 0,
                    CompressedCount = _memories.Count(m => m.IsCompressed),
                    CompressionRate = _memories.Count > 0 ? (double)_memories.Count(m => m.IsCompressed) / _memories.Count : 0,
                    TotalMemoryUsage = _memories.Sum(m => m.Content.Length),
                    TypeDistribution = _memories.GroupBy(m => m.Type).ToDictionary(g => g.Key, g => g.Count()),
                    LastUpdated = DateTime.Now
                };
            }
        }

        #region 受保护的虚方法

        /// <summary>
        /// 获取压缩阈值
        /// </summary>
        /// <returns>压缩阈值</returns>
        protected virtual int GetCompressionThreshold()
        {
            return Scope switch
            {
                MemoryScope.Global => 7,
                MemoryScope.Volume => 6,
                MemoryScope.Chapter => 5,
                MemoryScope.Paragraph => 4,
                _ => 5
            };
        }

        /// <summary>
        /// 获取高重要性阈值
        /// </summary>
        /// <returns>高重要性阈值</returns>
        protected virtual int GetHighImportanceThreshold()
        {
            return Scope switch
            {
                MemoryScope.Global => 8,
                MemoryScope.Volume => 7,
                MemoryScope.Chapter => 6,
                MemoryScope.Paragraph => 5,
                _ => 6
            };
        }

        /// <summary>
        /// 触发压缩
        /// </summary>
        /// <returns>压缩任务</returns>
        protected virtual async Task TriggerCompressionAsync()
        {
            _logger.LogInformation($"{Scope} 层达到容量限制，触发自动压缩");
            await CompressAsync();
        }

        #endregion
    }

    /// <summary>
    /// 记忆层统计信息
    /// </summary>
    public class MemoryLayerStatistics
    {
        /// <summary>
        /// 记忆范围
        /// </summary>
        public MemoryScope Scope { get; set; }

        /// <summary>
        /// 总记忆项数量
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 最大容量
        /// </summary>
        public int MaxCapacity { get; set; }

        /// <summary>
        /// 利用率
        /// </summary>
        public double UtilizationRate { get; set; }

        /// <summary>
        /// 平均重要性评分
        /// </summary>
        public double AverageImportanceScore { get; set; }

        /// <summary>
        /// 已压缩记忆项数量
        /// </summary>
        public int CompressedCount { get; set; }

        /// <summary>
        /// 压缩率
        /// </summary>
        public double CompressionRate { get; set; }

        /// <summary>
        /// 总内存使用量
        /// </summary>
        public long TotalMemoryUsage { get; set; }

        /// <summary>
        /// 类型分布
        /// </summary>
        public Dictionary<MemoryType, int> TypeDistribution { get; set; } = new();

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }
}
