using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;

namespace NovelManagement.AI.Memory.MemoryLayers
{
    /// <summary>
    /// 段落记忆层 - 存储当前处理的具体文本、局部上下文信息、临时状态变量等
    /// </summary>
    public class ParagraphMemoryLayer : BaseMemoryLayer
    {
        /// <inheritdoc/>
        public override MemoryScope Scope => MemoryScope.Paragraph;

        /// <summary>
        /// 段落ID
        /// </summary>
        public string ParagraphId { get; }

        /// <summary>
        /// 章节ID
        /// </summary>
        public Guid ChapterId { get; }

        /// <summary>
        /// 卷宗ID
        /// </summary>
        public Guid VolumeId { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="paragraphId">段落ID</param>
        /// <param name="chapterId">章节ID</param>
        /// <param name="volumeId">卷宗ID</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="compressionEngine">压缩引擎</param>
        public ParagraphMemoryLayer(string paragraphId, Guid chapterId, Guid volumeId, ILogger<ParagraphMemoryLayer> logger, ICompressionEngine compressionEngine) 
            : base(logger, compressionEngine)
        {
            ParagraphId = paragraphId;
            ChapterId = chapterId;
            VolumeId = volumeId;
            // 段落记忆层容量最小，存储临时信息
            MaxCapacity = 200;
        }

        /// <summary>
        /// 添加当前处理文本记忆
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="importance">重要性评分</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="textType">文本类型</param>
        /// <param name="tags">标签</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddCurrentTextAsync(string content, int importance, Guid projectId, string textType, List<string>? tags = null)
        {
            var textTags = tags ?? new List<string>();
            textTags.Add(textType);
            textTags.Add("当前文本");

            var memoryType = textType.ToLower() switch
            {
                "对话" => MemoryType.Dialogue,
                "场景" => MemoryType.Scene,
                "描述" => MemoryType.Scene,
                "动作" => MemoryType.Event,
                "心理" => MemoryType.Character,
                _ => MemoryType.Other
            };

            var memoryItem = new MemoryItem
            {
                Content = content,
                ImportanceScore = importance,
                Type = memoryType,
                Scope = MemoryScope.Paragraph,
                ProjectId = projectId,
                VolumeId = VolumeId,
                ChapterId = ChapterId,
                Tags = textTags,
                OriginalLength = content.Length
            };

            return await AddMemoryAsync(memoryItem);
        }

        /// <summary>
        /// 添加局部上下文记忆
        /// </summary>
        /// <param name="content">上下文内容</param>
        /// <param name="importance">重要性评分</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="contextType">上下文类型</param>
        /// <param name="tags">标签</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddLocalContextAsync(string content, int importance, Guid projectId, string contextType, List<string>? tags = null)
        {
            var contextTags = tags ?? new List<string>();
            contextTags.Add(contextType);
            contextTags.Add("局部上下文");

            var memoryItem = new MemoryItem
            {
                Content = content,
                ImportanceScore = importance,
                Type = MemoryType.System,
                Scope = MemoryScope.Paragraph,
                ProjectId = projectId,
                VolumeId = VolumeId,
                ChapterId = ChapterId,
                Tags = contextTags,
                OriginalLength = content.Length
            };

            return await AddMemoryAsync(memoryItem);
        }

        /// <summary>
        /// 添加临时状态变量记忆
        /// </summary>
        /// <param name="variableName">变量名</param>
        /// <param name="variableValue">变量值</param>
        /// <param name="importance">重要性评分</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="tags">标签</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddTemporaryStateAsync(string variableName, string variableValue, int importance, Guid projectId, List<string>? tags = null)
        {
            var stateTags = tags ?? new List<string>();
            stateTags.Add("临时状态");
            stateTags.Add(variableName);

            var content = $"{variableName}: {variableValue}";

            var memoryItem = new MemoryItem
            {
                Content = content,
                ImportanceScore = Math.Min(importance, 4), // 临时状态最高重要性为4
                Type = MemoryType.System,
                Scope = MemoryScope.Paragraph,
                ProjectId = projectId,
                VolumeId = VolumeId,
                ChapterId = ChapterId,
                Tags = stateTags,
                OriginalLength = content.Length
            };

            return await AddMemoryAsync(memoryItem);
        }

        /// <summary>
        /// 添加即时角色状态记忆
        /// </summary>
        /// <param name="content">状态内容</param>
        /// <param name="importance">重要性评分</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="characterId">角色ID</param>
        /// <param name="stateType">状态类型</param>
        /// <param name="tags">标签</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddInstantCharacterStateAsync(string content, int importance, Guid projectId, Guid characterId, string stateType, List<string>? tags = null)
        {
            var stateTags = tags ?? new List<string>();
            stateTags.Add(stateType);
            stateTags.Add("即时状态");

            var memoryItem = new MemoryItem
            {
                Content = content,
                ImportanceScore = importance,
                Type = MemoryType.Character,
                Scope = MemoryScope.Paragraph,
                ProjectId = projectId,
                VolumeId = VolumeId,
                ChapterId = ChapterId,
                Tags = stateTags,
                RelatedEntityIds = new List<Guid> { characterId },
                OriginalLength = content.Length
            };

            return await AddMemoryAsync(memoryItem);
        }

        /// <summary>
        /// 添加写作提示记忆
        /// </summary>
        /// <param name="content">提示内容</param>
        /// <param name="importance">重要性评分</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="promptType">提示类型</param>
        /// <param name="tags">标签</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddWritingPromptAsync(string content, int importance, Guid projectId, string promptType, List<string>? tags = null)
        {
            var promptTags = tags ?? new List<string>();
            promptTags.Add(promptType);
            promptTags.Add("写作提示");

            var memoryItem = new MemoryItem
            {
                Content = content,
                ImportanceScore = Math.Min(importance, 5), // 写作提示最高重要性为5
                Type = MemoryType.System,
                Scope = MemoryScope.Paragraph,
                ProjectId = projectId,
                VolumeId = VolumeId,
                ChapterId = ChapterId,
                Tags = promptTags,
                OriginalLength = content.Length
            };

            return await AddMemoryAsync(memoryItem);
        }

        /// <summary>
        /// 获取当前处理文本
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="textType">文本类型（可选）</param>
        /// <returns>当前文本记忆项列表</returns>
        public List<MemoryItem> GetCurrentTexts(Guid projectId, string? textType = null)
        {
            lock (_lockObject)
            {
                var query = _memories.Where(m => 
                    m.ProjectId == projectId && 
                    m.VolumeId == VolumeId && 
                    m.ChapterId == ChapterId &&
                    m.Tags.Contains("当前文本"));

                if (!string.IsNullOrEmpty(textType))
                {
                    query = query.Where(m => m.Tags.Contains(textType));
                }

                return query.OrderByDescending(m => m.CreatedAt).ToList();
            }
        }

        /// <summary>
        /// 获取局部上下文
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="contextType">上下文类型（可选）</param>
        /// <returns>局部上下文记忆项列表</returns>
        public List<MemoryItem> GetLocalContexts(Guid projectId, string? contextType = null)
        {
            lock (_lockObject)
            {
                var query = _memories.Where(m => 
                    m.ProjectId == projectId && 
                    m.VolumeId == VolumeId && 
                    m.ChapterId == ChapterId &&
                    m.Tags.Contains("局部上下文"));

                if (!string.IsNullOrEmpty(contextType))
                {
                    query = query.Where(m => m.Tags.Contains(contextType));
                }

                return query.OrderByDescending(m => m.ImportanceScore).ToList();
            }
        }

        /// <summary>
        /// 获取临时状态变量
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="variableName">变量名（可选）</param>
        /// <returns>临时状态记忆项列表</returns>
        public List<MemoryItem> GetTemporaryStates(Guid projectId, string? variableName = null)
        {
            lock (_lockObject)
            {
                var query = _memories.Where(m => 
                    m.ProjectId == projectId && 
                    m.VolumeId == VolumeId && 
                    m.ChapterId == ChapterId &&
                    m.Tags.Contains("临时状态"));

                if (!string.IsNullOrEmpty(variableName))
                {
                    query = query.Where(m => m.Tags.Contains(variableName));
                }

                return query.OrderByDescending(m => m.LastAccessedAt).ToList();
            }
        }

        /// <summary>
        /// 获取即时角色状态
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="characterId">角色ID（可选）</param>
        /// <returns>即时角色状态记忆项列表</returns>
        public List<MemoryItem> GetInstantCharacterStates(Guid projectId, Guid? characterId = null)
        {
            lock (_lockObject)
            {
                var query = _memories.Where(m => 
                    m.ProjectId == projectId && 
                    m.VolumeId == VolumeId && 
                    m.ChapterId == ChapterId &&
                    m.Type == MemoryType.Character &&
                    m.Tags.Contains("即时状态"));

                if (characterId.HasValue)
                {
                    query = query.Where(m => m.RelatedEntityIds.Contains(characterId.Value));
                }

                return query.OrderByDescending(m => m.CreatedAt).ToList();
            }
        }

        /// <summary>
        /// 获取写作提示
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="promptType">提示类型（可选）</param>
        /// <returns>写作提示记忆项列表</returns>
        public List<MemoryItem> GetWritingPrompts(Guid projectId, string? promptType = null)
        {
            lock (_lockObject)
            {
                var query = _memories.Where(m => 
                    m.ProjectId == projectId && 
                    m.VolumeId == VolumeId && 
                    m.ChapterId == ChapterId &&
                    m.Tags.Contains("写作提示"));

                if (!string.IsNullOrEmpty(promptType))
                {
                    query = query.Where(m => m.Tags.Contains(promptType));
                }

                return query.OrderByDescending(m => m.ImportanceScore).ToList();
            }
        }

        /// <summary>
        /// 更新临时状态变量
        /// </summary>
        /// <param name="variableName">变量名</param>
        /// <param name="newValue">新值</param>
        /// <param name="projectId">项目ID</param>
        /// <returns>是否成功更新</returns>
        public async Task<bool> UpdateTemporaryStateAsync(string variableName, string newValue, Guid projectId)
        {
            try
            {
                var existingStates = GetTemporaryStates(projectId, variableName);
                
                // 删除旧的状态
                foreach (var state in existingStates)
                {
                    RemoveMemory(state.Id);
                }

                // 添加新的状态
                return await AddTemporaryStateAsync(variableName, newValue, 3, projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新临时状态变量失败: {variableName}");
                return false;
            }
        }

        /// <summary>
        /// 清理过期的临时记忆
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="retentionMinutes">保留分钟数</param>
        /// <returns>清理的记忆项数量</returns>
        public int CleanupTemporaryMemories(Guid projectId, int retentionMinutes = 30)
        {
            try
            {
                var cutoffTime = DateTime.Now.AddMinutes(-retentionMinutes);
                var removedCount = 0;

                lock (_lockObject)
                {
                    var originalCount = _memories.Count;
                    
                    // 删除过期的临时记忆（保留重要性较高的）
                    _memories.RemoveAll(m => 
                        m.ProjectId == projectId &&
                        m.Tags.Contains("临时状态") &&
                        m.ImportanceScore <= 3 &&
                        m.CreatedAt < cutoffTime);
                    
                    removedCount = originalCount - _memories.Count;
                }

                _logger.LogInformation($"段落层清理完成，删除了 {removedCount} 个过期临时记忆项");
                return removedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期临时记忆失败");
                return 0;
            }
        }

        /// <summary>
        /// 获取段落摘要
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns>段落摘要</returns>
        public async Task<string> GenerateParagraphSummaryAsync(Guid projectId, int maxLength = 200)
        {
            try
            {
                var currentTexts = GetCurrentTexts(projectId);
                if (!currentTexts.Any())
                {
                    return "本段落暂无内容记录。";
                }

                var combinedContent = string.Join(" ", currentTexts.Select(m => m.Content));
                var summary = await _compressionEngine.GenerateSummaryAsync(combinedContent, maxLength);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成段落摘要失败");
                return "生成段落摘要时发生错误。";
            }
        }

        #region 受保护的方法重写

        /// <inheritdoc/>
        protected override int GetCompressionThreshold()
        {
            return 4; // 段落记忆层压缩重要性低于4的记忆项
        }

        /// <inheritdoc/>
        protected override int GetHighImportanceThreshold()
        {
            return 5; // 段落记忆层高重要性阈值为5
        }

        /// <inheritdoc/>
        protected override async Task TriggerCompressionAsync()
        {
            // 段落层优先清理临时记忆而不是压缩
            var projectIds = _memories.Select(m => m.ProjectId).Distinct().ToList();
            foreach (var projectId in projectIds)
            {
                CleanupTemporaryMemories(projectId, 15); // 保留15分钟内的临时记忆
            }

            // 如果还是满了，再进行压缩
            if (IsFull)
            {
                await base.TriggerCompressionAsync();
            }
        }

        #endregion
    }
}
