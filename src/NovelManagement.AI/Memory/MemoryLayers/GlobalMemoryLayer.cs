using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;

namespace NovelManagement.AI.Memory.MemoryLayers
{
    /// <summary>
    /// 全局记忆层 - 存储核心世界观设定、主要人物基础信息、整体剧情大纲等
    /// </summary>
    public class GlobalMemoryLayer : BaseMemoryLayer
    {
        /// <inheritdoc/>
        public override MemoryScope Scope => MemoryScope.Global;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="compressionEngine">压缩引擎</param>
        public GlobalMemoryLayer(ILogger<GlobalMemoryLayer> logger, ICompressionEngine compressionEngine) 
            : base(logger, compressionEngine)
        {
            // 全局记忆层容量较大，存储最重要的信息
            MaxCapacity = 2000;
        }

        /// <summary>
        /// 添加世界设定记忆
        /// </summary>
        /// <param name="content">设定内容</param>
        /// <param name="importance">重要性评分</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="tags">标签</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddWorldSettingAsync(string content, int importance, Guid projectId, List<string>? tags = null)
        {
            var memoryItem = new MemoryItem
            {
                Content = content,
                ImportanceScore = Math.Max(importance, 7), // 世界设定最低重要性为7
                Type = MemoryType.WorldSetting,
                Scope = MemoryScope.Global,
                ProjectId = projectId,
                Tags = tags ?? new List<string>(),
                OriginalLength = content.Length
            };

            return await AddMemoryAsync(memoryItem);
        }

        /// <summary>
        /// 添加主要角色记忆
        /// </summary>
        /// <param name="content">角色内容</param>
        /// <param name="importance">重要性评分</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="characterId">角色ID</param>
        /// <param name="tags">标签</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddMainCharacterAsync(string content, int importance, Guid projectId, Guid characterId, List<string>? tags = null)
        {
            var memoryItem = new MemoryItem
            {
                Content = content,
                ImportanceScore = Math.Max(importance, 6), // 主要角色最低重要性为6
                Type = MemoryType.Character,
                Scope = MemoryScope.Global,
                ProjectId = projectId,
                Tags = tags ?? new List<string>(),
                RelatedEntityIds = new List<Guid> { characterId },
                OriginalLength = content.Length
            };

            return await AddMemoryAsync(memoryItem);
        }

        /// <summary>
        /// 添加剧情大纲记忆
        /// </summary>
        /// <param name="content">大纲内容</param>
        /// <param name="importance">重要性评分</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="tags">标签</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddPlotOutlineAsync(string content, int importance, Guid projectId, List<string>? tags = null)
        {
            var memoryItem = new MemoryItem
            {
                Content = content,
                ImportanceScore = Math.Max(importance, 8), // 剧情大纲最低重要性为8
                Type = MemoryType.Plot,
                Scope = MemoryScope.Global,
                ProjectId = projectId,
                Tags = tags ?? new List<string>(),
                OriginalLength = content.Length
            };

            return await AddMemoryAsync(memoryItem);
        }

        /// <summary>
        /// 获取核心世界设定
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns>世界设定记忆项列表</returns>
        public List<MemoryItem> GetCoreWorldSettings(Guid projectId)
        {
            lock (_lockObject)
            {
                return _memories
                    .Where(m => m.ProjectId == projectId && m.Type == MemoryType.WorldSetting)
                    .OrderByDescending(m => m.ImportanceScore)
                    .ToList();
            }
        }

        /// <summary>
        /// 获取主要角色信息
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns>角色记忆项列表</returns>
        public List<MemoryItem> GetMainCharacters(Guid projectId)
        {
            lock (_lockObject)
            {
                return _memories
                    .Where(m => m.ProjectId == projectId && m.Type == MemoryType.Character)
                    .OrderByDescending(m => m.ImportanceScore)
                    .ToList();
            }
        }

        /// <summary>
        /// 获取剧情大纲
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns>剧情记忆项列表</returns>
        public List<MemoryItem> GetPlotOutlines(Guid projectId)
        {
            lock (_lockObject)
            {
                return _memories
                    .Where(m => m.ProjectId == projectId && m.Type == MemoryType.Plot)
                    .OrderByDescending(m => m.ImportanceScore)
                    .ToList();
            }
        }

        /// <summary>
        /// 获取时间线节点
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns>时间线记忆项列表</returns>
        public List<MemoryItem> GetTimelineNodes(Guid projectId)
        {
            lock (_lockObject)
            {
                return _memories
                    .Where(m => m.ProjectId == projectId && m.Type == MemoryType.Event)
                    .OrderByDescending(m => m.ImportanceScore)
                    .ToList();
            }
        }

        /// <summary>
        /// 获取关键设定规则
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="ruleType">规则类型</param>
        /// <returns>设定规则记忆项列表</returns>
        public List<MemoryItem> GetSettingRules(Guid projectId, string? ruleType = null)
        {
            lock (_lockObject)
            {
                var query = _memories.Where(m => 
                    m.ProjectId == projectId && 
                    m.Type == MemoryType.System &&
                    m.ImportanceScore >= 7);

                if (!string.IsNullOrEmpty(ruleType))
                {
                    query = query.Where(m => m.Tags.Contains(ruleType));
                }

                return query.OrderByDescending(m => m.ImportanceScore).ToList();
            }
        }

        /// <summary>
        /// 更新核心设定
        /// </summary>
        /// <param name="settingId">设定ID</param>
        /// <param name="newContent">新内容</param>
        /// <param name="newImportance">新重要性评分</param>
        /// <returns>是否成功更新</returns>
        public async Task<bool> UpdateCoreSettingAsync(string settingId, string newContent, int newImportance)
        {
            try
            {
                var memory = GetMemory(settingId);
                if (memory == null)
                {
                    _logger.LogWarning($"未找到要更新的核心设定: {settingId}");
                    return false;
                }

                // 重新评估重要性
                var context = new MemoryContext
                {
                    Scope = MemoryScope.Global,
                    ProjectId = memory.ProjectId
                };

                var evaluatedImportance = await _compressionEngine.EvaluateImportanceAsync(newContent, context);
                
                memory.Content = newContent;
                memory.ImportanceScore = Math.Max(newImportance, evaluatedImportance);
                memory.LastAccessedAt = DateTime.Now;
                memory.OriginalLength = newContent.Length;

                // 重新提取关键词
                memory.Tags = await _compressionEngine.ExtractKeywordsAsync(newContent, 10);

                return UpdateMemory(memory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新核心设定失败: {settingId}");
                return false;
            }
        }

        /// <summary>
        /// 检查设定一致性
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="newContent">新内容</param>
        /// <returns>一致性检查结果</returns>
        public async Task<ConsistencyCheckResult> CheckConsistencyAsync(Guid projectId, string newContent)
        {
            try
            {
                _logger.LogDebug($"检查全局设定一致性: ProjectId={projectId}");

                var result = new ConsistencyCheckResult
                {
                    IsConsistent = true,
                    ConflictingMemories = new List<MemoryItem>(),
                    Suggestions = new List<string>()
                };

                var coreSettings = GetCoreWorldSettings(projectId);
                var mainCharacters = GetMainCharacters(projectId);
                var plotOutlines = GetPlotOutlines(projectId);

                // 检查与世界设定的冲突
                foreach (var setting in coreSettings)
                {
                    var similarity = await _compressionEngine.CalculateSimilarityAsync(newContent, setting.Content);
                    if (similarity > 0.3) // 有一定相关性
                    {
                        // 进一步检查是否存在冲突
                        if (await DetectConflictAsync(newContent, setting.Content))
                        {
                            result.IsConsistent = false;
                            result.ConflictingMemories.Add(setting);
                            result.Suggestions.Add($"新内容与世界设定 '{setting.Id}' 存在冲突，建议检查并调整");
                        }
                    }
                }

                // 检查与主要角色的冲突
                foreach (var character in mainCharacters)
                {
                    var similarity = await _compressionEngine.CalculateSimilarityAsync(newContent, character.Content);
                    if (similarity > 0.3)
                    {
                        if (await DetectConflictAsync(newContent, character.Content))
                        {
                            result.IsConsistent = false;
                            result.ConflictingMemories.Add(character);
                            result.Suggestions.Add($"新内容与主要角色 '{character.Id}' 设定存在冲突，建议检查并调整");
                        }
                    }
                }

                _logger.LogDebug($"一致性检查完成: IsConsistent={result.IsConsistent}, Conflicts={result.ConflictingMemories.Count}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查设定一致性失败");
                return new ConsistencyCheckResult
                {
                    IsConsistent = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        #region 受保护的方法重写

        /// <inheritdoc/>
        protected override int GetCompressionThreshold()
        {
            return 8; // 全局记忆层只压缩重要性低于8的记忆项
        }

        /// <inheritdoc/>
        protected override int GetHighImportanceThreshold()
        {
            return 9; // 全局记忆层高重要性阈值为9
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 检测内容冲突
        /// </summary>
        /// <param name="content1">内容1</param>
        /// <param name="content2">内容2</param>
        /// <returns>是否存在冲突</returns>
        private async Task<bool> DetectConflictAsync(string content1, string content2)
        {
            // 简化的冲突检测逻辑
            // 在实际应用中，这里应该使用更复杂的NLP技术来检测语义冲突
            
            var keywords1 = await _compressionEngine.ExtractKeywordsAsync(content1, 20);
            var keywords2 = await _compressionEngine.ExtractKeywordsAsync(content2, 20);

            // 检查是否有相同的关键词但描述不同
            var commonKeywords = keywords1.Intersect(keywords2).ToList();
            
            if (commonKeywords.Count > 2) // 有多个共同关键词
            {
                // 检查描述是否一致
                var similarity = await _compressionEngine.CalculateSimilarityAsync(content1, content2);
                return similarity < 0.5; // 相似度低可能表示冲突
            }

            return false;
        }

        #endregion
    }

    /// <summary>
    /// 一致性检查结果
    /// </summary>
    public class ConsistencyCheckResult
    {
        /// <summary>
        /// 是否一致
        /// </summary>
        public bool IsConsistent { get; set; }

        /// <summary>
        /// 冲突的记忆项
        /// </summary>
        public List<MemoryItem> ConflictingMemories { get; set; } = new();

        /// <summary>
        /// 建议列表
        /// </summary>
        public List<string> Suggestions { get; set; } = new();

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 检查时间
        /// </summary>
        public DateTime CheckedAt { get; set; } = DateTime.Now;
    }
}
