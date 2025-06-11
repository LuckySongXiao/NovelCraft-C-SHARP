using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;

namespace NovelManagement.AI.Memory.MemoryLayers
{
    /// <summary>
    /// 章节记忆层 - 存储当前章节的具体内容、章节内的人物状态、具体场景和对话等
    /// </summary>
    public class ChapterMemoryLayer : BaseMemoryLayer
    {
        /// <inheritdoc/>
        public override MemoryScope Scope => MemoryScope.Chapter;

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
        /// <param name="chapterId">章节ID</param>
        /// <param name="volumeId">卷宗ID</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="compressionEngine">压缩引擎</param>
        public ChapterMemoryLayer(Guid chapterId, Guid volumeId, ILogger<ChapterMemoryLayer> logger, ICompressionEngine compressionEngine) 
            : base(logger, compressionEngine)
        {
            ChapterId = chapterId;
            VolumeId = volumeId;
            // 章节记忆层容量较小，存储具体内容
            MaxCapacity = 800;
        }

        /// <summary>
        /// 添加章节内容记忆
        /// </summary>
        /// <param name="content">章节内容</param>
        /// <param name="importance">重要性评分</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="contentType">内容类型</param>
        /// <param name="tags">标签</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddChapterContentAsync(string content, int importance, Guid projectId, string contentType, List<string>? tags = null)
        {
            var contentTags = tags ?? new List<string>();
            contentTags.Add(contentType);

            var memoryType = contentType.ToLower() switch
            {
                "对话" => MemoryType.Dialogue,
                "场景" => MemoryType.Scene,
                "事件" => MemoryType.Event,
                "角色" => MemoryType.Character,
                _ => MemoryType.Other
            };

            var memoryItem = new MemoryItem
            {
                Content = content,
                ImportanceScore = importance,
                Type = memoryType,
                Scope = MemoryScope.Chapter,
                ProjectId = projectId,
                VolumeId = VolumeId,
                ChapterId = ChapterId,
                Tags = contentTags,
                OriginalLength = content.Length
            };

            return await AddMemoryAsync(memoryItem);
        }

        /// <summary>
        /// 添加人物状态记忆
        /// </summary>
        /// <param name="content">状态内容</param>
        /// <param name="importance">重要性评分</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="characterId">角色ID</param>
        /// <param name="stateType">状态类型</param>
        /// <param name="tags">标签</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddCharacterStateAsync(string content, int importance, Guid projectId, Guid characterId, string stateType, List<string>? tags = null)
        {
            var stateTags = tags ?? new List<string>();
            stateTags.Add(stateType);
            stateTags.Add("角色状态");

            var memoryItem = new MemoryItem
            {
                Content = content,
                ImportanceScore = importance,
                Type = MemoryType.Character,
                Scope = MemoryScope.Chapter,
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
        /// 添加场景描述记忆
        /// </summary>
        /// <param name="content">场景内容</param>
        /// <param name="importance">重要性评分</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="sceneType">场景类型</param>
        /// <param name="location">地点</param>
        /// <param name="tags">标签</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddSceneDescriptionAsync(string content, int importance, Guid projectId, string sceneType, string? location = null, List<string>? tags = null)
        {
            var sceneTags = tags ?? new List<string>();
            sceneTags.Add(sceneType);
            if (!string.IsNullOrEmpty(location))
                sceneTags.Add(location);

            var memoryItem = new MemoryItem
            {
                Content = content,
                ImportanceScore = importance,
                Type = MemoryType.Scene,
                Scope = MemoryScope.Chapter,
                ProjectId = projectId,
                VolumeId = VolumeId,
                ChapterId = ChapterId,
                Tags = sceneTags,
                OriginalLength = content.Length
            };

            return await AddMemoryAsync(memoryItem);
        }

        /// <summary>
        /// 添加对话记忆
        /// </summary>
        /// <param name="content">对话内容</param>
        /// <param name="importance">重要性评分</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="speakerIds">说话者ID列表</param>
        /// <param name="dialogueType">对话类型</param>
        /// <param name="tags">标签</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddDialogueAsync(string content, int importance, Guid projectId, List<Guid> speakerIds, string dialogueType, List<string>? tags = null)
        {
            var dialogueTags = tags ?? new List<string>();
            dialogueTags.Add(dialogueType);
            dialogueTags.Add("对话");

            var memoryItem = new MemoryItem
            {
                Content = content,
                ImportanceScore = importance,
                Type = MemoryType.Dialogue,
                Scope = MemoryScope.Chapter,
                ProjectId = projectId,
                VolumeId = VolumeId,
                ChapterId = ChapterId,
                Tags = dialogueTags,
                RelatedEntityIds = speakerIds,
                OriginalLength = content.Length
            };

            return await AddMemoryAsync(memoryItem);
        }

        /// <summary>
        /// 添加章节间连接信息
        /// </summary>
        /// <param name="content">连接内容</param>
        /// <param name="importance">重要性评分</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="previousChapterId">前一章节ID</param>
        /// <param name="nextChapterId">后一章节ID</param>
        /// <param name="tags">标签</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddChapterConnectionAsync(string content, int importance, Guid projectId, Guid? previousChapterId, Guid? nextChapterId, List<string>? tags = null)
        {
            var connectionTags = tags ?? new List<string>();
            connectionTags.Add("章节连接");

            var relatedIds = new List<Guid>();
            if (previousChapterId.HasValue) relatedIds.Add(previousChapterId.Value);
            if (nextChapterId.HasValue) relatedIds.Add(nextChapterId.Value);

            var memoryItem = new MemoryItem
            {
                Content = content,
                ImportanceScore = Math.Max(importance, 6), // 章节连接信息重要性较高
                Type = MemoryType.Relationship,
                Scope = MemoryScope.Chapter,
                ProjectId = projectId,
                VolumeId = VolumeId,
                ChapterId = ChapterId,
                Tags = connectionTags,
                RelatedEntityIds = relatedIds,
                OriginalLength = content.Length
            };

            return await AddMemoryAsync(memoryItem);
        }

        /// <summary>
        /// 获取章节主要内容
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="contentType">内容类型（可选）</param>
        /// <returns>章节内容记忆项列表</returns>
        public List<MemoryItem> GetChapterContent(Guid projectId, string? contentType = null)
        {
            lock (_lockObject)
            {
                var query = _memories.Where(m => 
                    m.ProjectId == projectId && 
                    m.VolumeId == VolumeId && 
                    m.ChapterId == ChapterId);

                if (!string.IsNullOrEmpty(contentType))
                {
                    query = query.Where(m => m.Tags.Contains(contentType));
                }

                return query.OrderByDescending(m => m.ImportanceScore).ToList();
            }
        }

        /// <summary>
        /// 获取角色状态
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="characterId">角色ID（可选）</param>
        /// <returns>角色状态记忆项列表</returns>
        public List<MemoryItem> GetCharacterStates(Guid projectId, Guid? characterId = null)
        {
            lock (_lockObject)
            {
                var query = _memories.Where(m => 
                    m.ProjectId == projectId && 
                    m.VolumeId == VolumeId && 
                    m.ChapterId == ChapterId &&
                    m.Type == MemoryType.Character &&
                    m.Tags.Contains("角色状态"));

                if (characterId.HasValue)
                {
                    query = query.Where(m => m.RelatedEntityIds.Contains(characterId.Value));
                }

                return query.OrderByDescending(m => m.ImportanceScore).ToList();
            }
        }

        /// <summary>
        /// 获取场景描述
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="sceneType">场景类型（可选）</param>
        /// <returns>场景描述记忆项列表</returns>
        public List<MemoryItem> GetSceneDescriptions(Guid projectId, string? sceneType = null)
        {
            lock (_lockObject)
            {
                var query = _memories.Where(m => 
                    m.ProjectId == projectId && 
                    m.VolumeId == VolumeId && 
                    m.ChapterId == ChapterId &&
                    m.Type == MemoryType.Scene);

                if (!string.IsNullOrEmpty(sceneType))
                {
                    query = query.Where(m => m.Tags.Contains(sceneType));
                }

                return query.OrderByDescending(m => m.ImportanceScore).ToList();
            }
        }

        /// <summary>
        /// 获取对话内容
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="speakerId">说话者ID（可选）</param>
        /// <param name="dialogueType">对话类型（可选）</param>
        /// <returns>对话记忆项列表</returns>
        public List<MemoryItem> GetDialogues(Guid projectId, Guid? speakerId = null, string? dialogueType = null)
        {
            lock (_lockObject)
            {
                var query = _memories.Where(m => 
                    m.ProjectId == projectId && 
                    m.VolumeId == VolumeId && 
                    m.ChapterId == ChapterId &&
                    m.Type == MemoryType.Dialogue);

                if (speakerId.HasValue)
                {
                    query = query.Where(m => m.RelatedEntityIds.Contains(speakerId.Value));
                }

                if (!string.IsNullOrEmpty(dialogueType))
                {
                    query = query.Where(m => m.Tags.Contains(dialogueType));
                }

                return query.OrderByDescending(m => m.ImportanceScore).ToList();
            }
        }

        /// <summary>
        /// 获取章节连接信息
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns>章节连接记忆项列表</returns>
        public List<MemoryItem> GetChapterConnections(Guid projectId)
        {
            lock (_lockObject)
            {
                return _memories
                    .Where(m => 
                        m.ProjectId == projectId && 
                        m.VolumeId == VolumeId && 
                        m.ChapterId == ChapterId &&
                        m.Type == MemoryType.Relationship &&
                        m.Tags.Contains("章节连接"))
                    .OrderByDescending(m => m.ImportanceScore)
                    .ToList();
            }
        }

        /// <summary>
        /// 生成章节摘要
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns>章节摘要</returns>
        public async Task<string> GenerateChapterSummaryAsync(Guid projectId, int maxLength = 500)
        {
            try
            {
                _logger.LogDebug($"生成章节摘要: ChapterId={ChapterId}, ProjectId={projectId}");

                var allContent = GetChapterContent(projectId);
                if (!allContent.Any())
                {
                    return "本章节暂无内容记录。";
                }

                // 按重要性排序，选择最重要的内容
                var importantContent = allContent
                    .Where(m => m.ImportanceScore >= 5)
                    .OrderByDescending(m => m.ImportanceScore)
                    .Take(10)
                    .ToList();

                var combinedContent = string.Join("\n", importantContent.Select(m => m.Content));
                var summary = await _compressionEngine.GenerateSummaryAsync(combinedContent, maxLength);

                _logger.LogDebug($"章节摘要生成完成，长度: {summary.Length}");
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成章节摘要失败");
                return "生成章节摘要时发生错误。";
            }
        }

        /// <summary>
        /// 分析章节完整性
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns>完整性分析结果</returns>
        public async Task<ChapterCompletenessAnalysis> AnalyzeCompletenessAsync(Guid projectId)
        {
            try
            {
                _logger.LogDebug($"分析章节完整性: ChapterId={ChapterId}, ProjectId={projectId}");

                var analysis = new ChapterCompletenessAnalysis
                {
                    ChapterId = ChapterId,
                    VolumeId = VolumeId,
                    ProjectId = projectId
                };

                var allContent = GetChapterContent(projectId);
                var scenes = GetSceneDescriptions(projectId);
                var dialogues = GetDialogues(projectId);
                var characterStates = GetCharacterStates(projectId);
                var connections = GetChapterConnections(projectId);

                // 分析内容完整性
                analysis.HasMainContent = allContent.Any(c => c.ImportanceScore >= 7);
                analysis.ContentCount = allContent.Count;
                analysis.SceneCount = scenes.Count;
                analysis.DialogueCount = dialogues.Count;
                analysis.CharacterStateCount = characterStates.Count;
                analysis.HasChapterConnection = connections.Any();

                // 计算完整性评分
                var score = 0.0;
                if (analysis.HasMainContent) score += 25;
                if (analysis.ContentCount >= 5) score += 20;
                if (analysis.SceneCount >= 2) score += 15;
                if (analysis.DialogueCount >= 3) score += 15;
                if (analysis.CharacterStateCount >= 1) score += 15;
                if (analysis.HasChapterConnection) score += 10;

                analysis.CompletenessScore = Math.Min(score, 100);

                // 生成建议
                if (!analysis.HasMainContent)
                    analysis.Suggestions.Add("建议添加重要的章节主要内容");
                if (analysis.SceneCount < 2)
                    analysis.Suggestions.Add("建议增加场景描述以丰富章节内容");
                if (analysis.DialogueCount < 3)
                    analysis.Suggestions.Add("建议添加更多对话内容");
                if (analysis.CharacterStateCount < 1)
                    analysis.Suggestions.Add("建议记录角色在本章节中的状态变化");

                _logger.LogDebug($"章节完整性分析完成: Score={analysis.CompletenessScore}");
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析章节完整性失败");
                return new ChapterCompletenessAnalysis
                {
                    ChapterId = ChapterId,
                    VolumeId = VolumeId,
                    ProjectId = projectId,
                    ErrorMessage = ex.Message
                };
            }
        }

        #region 受保护的方法重写

        /// <inheritdoc/>
        protected override int GetCompressionThreshold()
        {
            return 5; // 章节记忆层压缩重要性低于5的记忆项
        }

        /// <inheritdoc/>
        protected override int GetHighImportanceThreshold()
        {
            return 6; // 章节记忆层高重要性阈值为6
        }

        #endregion
    }

    /// <summary>
    /// 章节完整性分析结果
    /// </summary>
    public class ChapterCompletenessAnalysis
    {
        /// <summary>
        /// 章节ID
        /// </summary>
        public Guid ChapterId { get; set; }

        /// <summary>
        /// 卷宗ID
        /// </summary>
        public Guid VolumeId { get; set; }

        /// <summary>
        /// 项目ID
        /// </summary>
        public Guid ProjectId { get; set; }

        /// <summary>
        /// 是否有主要内容
        /// </summary>
        public bool HasMainContent { get; set; }

        /// <summary>
        /// 内容数量
        /// </summary>
        public int ContentCount { get; set; }

        /// <summary>
        /// 场景数量
        /// </summary>
        public int SceneCount { get; set; }

        /// <summary>
        /// 对话数量
        /// </summary>
        public int DialogueCount { get; set; }

        /// <summary>
        /// 角色状态数量
        /// </summary>
        public int CharacterStateCount { get; set; }

        /// <summary>
        /// 是否有章节连接
        /// </summary>
        public bool HasChapterConnection { get; set; }

        /// <summary>
        /// 完整性评分（0-100）
        /// </summary>
        public double CompletenessScore { get; set; }

        /// <summary>
        /// 建议列表
        /// </summary>
        public List<string> Suggestions { get; set; } = new();

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 分析时间
        /// </summary>
        public DateTime AnalyzedAt { get; set; } = DateTime.Now;
    }
}
