using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;

namespace NovelManagement.AI.Memory
{
    /// <summary>
    /// 记忆管理器实现
    /// </summary>
    public class MemoryManager : IMemoryManager
    {
        private readonly ILogger<MemoryManager> _logger;
        private readonly ICompressionEngine _compressionEngine;
        
        // 内存存储 - 在实际应用中应该使用数据库或缓存
        private readonly Dictionary<string, List<MemoryItem>> _memoryStorage = new();
        private readonly object _lockObject = new object();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="compressionEngine">压缩引擎</param>
        public MemoryManager(ILogger<MemoryManager> logger, ICompressionEngine compressionEngine)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _compressionEngine = compressionEngine ?? throw new ArgumentNullException(nameof(compressionEngine));
        }

        /// <inheritdoc/>
        public async Task<MemoryContext> GetContextAsync(string taskType, MemoryScope scope, Guid projectId, Guid? volumeId = null, Guid? chapterId = null)
        {
            try
            {
                _logger.LogInformation($"获取记忆上下文: TaskType={taskType}, Scope={scope}, ProjectId={projectId}");

                var context = new MemoryContext
                {
                    TaskType = taskType,
                    Scope = scope,
                    ProjectId = projectId,
                    VolumeId = volumeId,
                    ChapterId = chapterId
                };

                // 获取相关记忆项
                var relevantMemories = await GetRelevantMemoriesAsync(taskType, scope, projectId, volumeId, chapterId);
                
                // 按重要性排序
                relevantMemories = relevantMemories.OrderByDescending(m => m.ImportanceScore).ToList();

                context.RelevantMemories = relevantMemories;
                context.TotalImportanceScore = relevantMemories.Sum(m => m.ImportanceScore);

                // 生成上下文摘要
                if (relevantMemories.Any())
                {
                    var combinedContent = string.Join("\n", relevantMemories.Select(m => m.Content));
                    context.Summary = await _compressionEngine.GenerateSummaryAsync(combinedContent, 500);
                }

                _logger.LogInformation($"成功获取记忆上下文，包含 {relevantMemories.Count} 个相关记忆项");
                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取记忆上下文失败: TaskType={taskType}, Scope={scope}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateMemoryAsync(string content, int importanceScore, MemoryScope scope, Guid projectId, Guid? volumeId = null, Guid? chapterId = null)
        {
            try
            {
                _logger.LogInformation($"更新记忆: Scope={scope}, ImportanceScore={importanceScore}, ProjectId={projectId}");

                // 验证重要性评分
                if (importanceScore < 1 || importanceScore > 10)
                {
                    throw new ArgumentException("重要性评分必须在1-10之间", nameof(importanceScore));
                }

                // 分析内容类型
                var memoryType = await _compressionEngine.AnalyzeContentTypeAsync(content);

                // 提取关键词
                var keywords = await _compressionEngine.ExtractKeywordsAsync(content);

                // 创建记忆项
                var memoryItem = new MemoryItem
                {
                    Content = content,
                    ImportanceScore = importanceScore,
                    Type = memoryType,
                    Scope = scope,
                    ProjectId = projectId,
                    VolumeId = volumeId,
                    ChapterId = chapterId,
                    OriginalLength = content.Length,
                    Tags = keywords
                };

                // 存储记忆项
                var storageKey = GetStorageKey(scope, projectId, volumeId, chapterId);
                
                lock (_lockObject)
                {
                    if (!_memoryStorage.ContainsKey(storageKey))
                    {
                        _memoryStorage[storageKey] = new List<MemoryItem>();
                    }
                    
                    _memoryStorage[storageKey].Add(memoryItem);
                }

                _logger.LogInformation($"成功更新记忆，记忆项ID: {memoryItem.Id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新记忆失败: Scope={scope}, ProjectId={projectId}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<MemoryCompressionResult> CompressMemoryAsync(MemoryScope scope, Guid projectId, Guid? volumeId = null, Guid? chapterId = null)
        {
            var startTime = DateTime.Now;
            
            try
            {
                _logger.LogInformation($"开始压缩记忆: Scope={scope}, ProjectId={projectId}");

                var storageKey = GetStorageKey(scope, projectId, volumeId, chapterId);
                List<MemoryItem> memoryItems;

                lock (_lockObject)
                {
                    if (!_memoryStorage.ContainsKey(storageKey))
                    {
                        return new MemoryCompressionResult
                        {
                            IsSuccess = true,
                            OriginalCount = 0,
                            CompressedCount = 0
                        };
                    }

                    memoryItems = new List<MemoryItem>(_memoryStorage[storageKey]);
                }

                var originalCount = memoryItems.Count;
                var originalSize = memoryItems.Sum(m => m.Content.Length);

                // 执行压缩
                var compressedItems = await _compressionEngine.CompressLowImportanceAsync(memoryItems, 5);
                
                // 合并相似记忆项
                compressedItems = await _compressionEngine.MergeSimilarMemoriesAsync(compressedItems, 0.8);

                var compressedCount = compressedItems.Count;
                var compressedSize = compressedItems.Sum(m => m.Content.Length);

                // 更新存储
                lock (_lockObject)
                {
                    _memoryStorage[storageKey] = compressedItems;
                }

                var result = new MemoryCompressionResult
                {
                    IsSuccess = true,
                    OriginalCount = originalCount,
                    CompressedCount = compressedCount,
                    FreedMemoryBytes = originalSize - compressedSize,
                    Duration = DateTime.Now - startTime
                };

                _logger.LogInformation($"记忆压缩完成: 原始{originalCount}项 -> 压缩后{compressedCount}项, 压缩比{result.CompressionRatio:P2}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"记忆压缩失败: Scope={scope}, ProjectId={projectId}");
                return new MemoryCompressionResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Duration = DateTime.Now - startTime
                };
            }
        }

        /// <inheritdoc/>
        public async Task<List<MemoryItem>> SearchMemoryAsync(string query, MemoryScope scope, Guid projectId, int maxResults = 10)
        {
            try
            {
                _logger.LogInformation($"搜索记忆: Query={query}, Scope={scope}, ProjectId={projectId}");

                var allMemories = new List<MemoryItem>();

                // 根据范围获取记忆项
                lock (_lockObject)
                {
                    foreach (var kvp in _memoryStorage)
                    {
                        if (kvp.Key.Contains(projectId.ToString()))
                        {
                            var memories = kvp.Value.Where(m => m.Scope == scope || scope == MemoryScope.Global).ToList();
                            allMemories.AddRange(memories);
                        }
                    }
                }

                // 使用压缩引擎优化检索
                var optimizedResults = await _compressionEngine.OptimizeRetrievalAsync(query, allMemories, maxResults);

                // 更新访问统计
                foreach (var memory in optimizedResults)
                {
                    memory.LastAccessedAt = DateTime.Now;
                    memory.AccessCount++;
                }

                _logger.LogInformation($"搜索完成，返回 {optimizedResults.Count} 个结果");
                return optimizedResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"搜索记忆失败: Query={query}, Scope={scope}");
                return new List<MemoryItem>();
            }
        }

        /// <inheritdoc/>
        public async Task<int> CleanupExpiredMemoryAsync(MemoryScope scope, Guid projectId, int retentionDays = 30)
        {
            try
            {
                _logger.LogInformation($"清理过期记忆: Scope={scope}, ProjectId={projectId}, RetentionDays={retentionDays}");

                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                var removedCount = 0;

                lock (_lockObject)
                {
                    var keysToUpdate = _memoryStorage.Keys.Where(k => k.Contains(projectId.ToString())).ToList();
                    
                    foreach (var key in keysToUpdate)
                    {
                        var memories = _memoryStorage[key];
                        var originalCount = memories.Count;
                        
                        // 保留高重要性记忆项和最近访问的记忆项
                        _memoryStorage[key] = memories.Where(m => 
                            m.ImportanceScore >= 8 || 
                            m.LastAccessedAt > cutoffDate ||
                            m.CreatedAt > cutoffDate).ToList();
                        
                        removedCount += originalCount - _memoryStorage[key].Count;
                    }
                }

                _logger.LogInformation($"清理完成，删除了 {removedCount} 个过期记忆项");
                return removedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"清理过期记忆失败: Scope={scope}, ProjectId={projectId}");
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<MemoryStatistics> GetMemoryStatisticsAsync(Guid projectId)
        {
            try
            {
                _logger.LogInformation($"获取记忆统计信息: ProjectId={projectId}");

                var statistics = new MemoryStatistics
                {
                    ProjectId = projectId
                };

                var allMemories = new List<MemoryItem>();

                lock (_lockObject)
                {
                    foreach (var kvp in _memoryStorage)
                    {
                        if (kvp.Key.Contains(projectId.ToString()))
                        {
                            allMemories.AddRange(kvp.Value);
                        }
                    }
                }

                statistics.TotalMemoryItems = allMemories.Count;
                statistics.TotalMemoryUsage = allMemories.Sum(m => m.Content.Length);

                // 按范围统计
                statistics.MemoryCountByScope = allMemories
                    .GroupBy(m => m.Scope)
                    .ToDictionary(g => g.Key, g => g.Count());

                // 按类型统计
                statistics.MemoryCountByType = allMemories
                    .GroupBy(m => m.Type)
                    .ToDictionary(g => g.Key, g => g.Count());

                // 计算压缩率
                var compressedItems = allMemories.Where(m => m.IsCompressed).ToList();
                statistics.CompressionRatio = allMemories.Count > 0 ? 
                    (double)compressedItems.Count / allMemories.Count : 0;

                // 计算平均重要性评分
                statistics.AverageImportanceScore = allMemories.Count > 0 ? 
                    allMemories.Average(m => m.ImportanceScore) : 0;

                _logger.LogInformation($"统计信息获取完成: 总计 {statistics.TotalMemoryItems} 个记忆项");
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取记忆统计信息失败: ProjectId={projectId}");
                throw;
            }
        }

        #region 私有方法

        /// <summary>
        /// 获取相关记忆项
        /// </summary>
        private async Task<List<MemoryItem>> GetRelevantMemoriesAsync(string taskType, MemoryScope scope, Guid projectId, Guid? volumeId, Guid? chapterId)
        {
            var relevantMemories = new List<MemoryItem>();

            lock (_lockObject)
            {
                // 根据范围获取记忆项
                switch (scope)
                {
                    case MemoryScope.Global:
                        var globalKey = GetStorageKey(MemoryScope.Global, projectId);
                        if (_memoryStorage.ContainsKey(globalKey))
                        {
                            relevantMemories.AddRange(_memoryStorage[globalKey]);
                        }
                        break;

                    case MemoryScope.Volume:
                        var volumeKey = GetStorageKey(MemoryScope.Volume, projectId, volumeId);
                        if (_memoryStorage.ContainsKey(volumeKey))
                        {
                            relevantMemories.AddRange(_memoryStorage[volumeKey]);
                        }
                        // 同时包含全局记忆
                        var globalKey2 = GetStorageKey(MemoryScope.Global, projectId);
                        if (_memoryStorage.ContainsKey(globalKey2))
                        {
                            relevantMemories.AddRange(_memoryStorage[globalKey2]);
                        }
                        break;

                    case MemoryScope.Chapter:
                        var chapterKey = GetStorageKey(MemoryScope.Chapter, projectId, volumeId, chapterId);
                        if (_memoryStorage.ContainsKey(chapterKey))
                        {
                            relevantMemories.AddRange(_memoryStorage[chapterKey]);
                        }
                        // 同时包含卷宗和全局记忆
                        var volumeKey2 = GetStorageKey(MemoryScope.Volume, projectId, volumeId);
                        if (_memoryStorage.ContainsKey(volumeKey2))
                        {
                            relevantMemories.AddRange(_memoryStorage[volumeKey2]);
                        }
                        var globalKey3 = GetStorageKey(MemoryScope.Global, projectId);
                        if (_memoryStorage.ContainsKey(globalKey3))
                        {
                            relevantMemories.AddRange(_memoryStorage[globalKey3]);
                        }
                        break;

                    case MemoryScope.Paragraph:
                        // 段落级记忆包含所有上级记忆
                        foreach (var kvp in _memoryStorage)
                        {
                            if (kvp.Key.Contains(projectId.ToString()))
                            {
                                relevantMemories.AddRange(kvp.Value);
                            }
                        }
                        break;
                }
            }

            return relevantMemories.Distinct().ToList();
        }

        /// <summary>
        /// 获取存储键
        /// </summary>
        private string GetStorageKey(MemoryScope scope, Guid projectId, Guid? volumeId = null, Guid? chapterId = null)
        {
            return scope switch
            {
                MemoryScope.Global => $"global_{projectId}",
                MemoryScope.Volume => $"volume_{projectId}_{volumeId}",
                MemoryScope.Chapter => $"chapter_{projectId}_{volumeId}_{chapterId}",
                MemoryScope.Paragraph => $"paragraph_{projectId}_{volumeId}_{chapterId}_{Guid.NewGuid()}",
                _ => throw new ArgumentException($"不支持的记忆范围: {scope}")
            };
        }

        #endregion
    }
}
