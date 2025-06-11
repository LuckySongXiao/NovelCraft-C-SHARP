using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;

namespace NovelManagement.AI.Memory.MemoryLayers
{
    /// <summary>
    /// 卷宗记忆层 - 存储当前卷的主要剧情线、卷内人物发展轨迹、重要事件和转折点等
    /// </summary>
    public class VolumeMemoryLayer : BaseMemoryLayer
    {
        /// <inheritdoc/>
        public override MemoryScope Scope => MemoryScope.Volume;

        /// <summary>
        /// 卷宗ID
        /// </summary>
        public Guid VolumeId { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="volumeId">卷宗ID</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="compressionEngine">压缩引擎</param>
        public VolumeMemoryLayer(Guid volumeId, ILogger<VolumeMemoryLayer> logger, ICompressionEngine compressionEngine) 
            : base(logger, compressionEngine)
        {
            VolumeId = volumeId;
            // 卷宗记忆层容量中等
            MaxCapacity = 1500;
        }

        /// <summary>
        /// 添加卷宗剧情线记忆
        /// </summary>
        /// <param name="content">剧情内容</param>
        /// <param name="importance">重要性评分</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="tags">标签</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddPlotLineAsync(string content, int importance, Guid projectId, List<string>? tags = null)
        {
            var memoryItem = new MemoryItem
            {
                Content = content,
                ImportanceScore = Math.Max(importance, 6), // 卷宗剧情线最低重要性为6
                Type = MemoryType.Plot,
                Scope = MemoryScope.Volume,
                ProjectId = projectId,
                VolumeId = VolumeId,
                Tags = tags ?? new List<string>(),
                OriginalLength = content.Length
            };

            return await AddMemoryAsync(memoryItem);
        }

        /// <summary>
        /// 添加人物发展轨迹记忆
        /// </summary>
        /// <param name="content">发展内容</param>
        /// <param name="importance">重要性评分</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="characterId">角色ID</param>
        /// <param name="tags">标签</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddCharacterDevelopmentAsync(string content, int importance, Guid projectId, Guid characterId, List<string>? tags = null)
        {
            var memoryItem = new MemoryItem
            {
                Content = content,
                ImportanceScore = Math.Max(importance, 5), // 人物发展最低重要性为5
                Type = MemoryType.Character,
                Scope = MemoryScope.Volume,
                ProjectId = projectId,
                VolumeId = VolumeId,
                Tags = tags ?? new List<string>(),
                RelatedEntityIds = new List<Guid> { characterId },
                OriginalLength = content.Length
            };

            return await AddMemoryAsync(memoryItem);
        }

        /// <summary>
        /// 添加重要事件记忆
        /// </summary>
        /// <param name="content">事件内容</param>
        /// <param name="importance">重要性评分</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="eventType">事件类型</param>
        /// <param name="tags">标签</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddImportantEventAsync(string content, int importance, Guid projectId, string eventType, List<string>? tags = null)
        {
            var eventTags = tags ?? new List<string>();
            eventTags.Add(eventType);

            var memoryItem = new MemoryItem
            {
                Content = content,
                ImportanceScore = Math.Max(importance, 6), // 重要事件最低重要性为6
                Type = MemoryType.Event,
                Scope = MemoryScope.Volume,
                ProjectId = projectId,
                VolumeId = VolumeId,
                Tags = eventTags,
                OriginalLength = content.Length
            };

            return await AddMemoryAsync(memoryItem);
        }

        /// <summary>
        /// 添加卷宗间承接关系记忆
        /// </summary>
        /// <param name="content">承接内容</param>
        /// <param name="importance">重要性评分</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="previousVolumeId">前一卷ID</param>
        /// <param name="nextVolumeId">后一卷ID</param>
        /// <param name="tags">标签</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddVolumeConnectionAsync(string content, int importance, Guid projectId, Guid? previousVolumeId, Guid? nextVolumeId, List<string>? tags = null)
        {
            var connectionTags = tags ?? new List<string>();
            connectionTags.Add("卷宗承接");

            var relatedIds = new List<Guid>();
            if (previousVolumeId.HasValue) relatedIds.Add(previousVolumeId.Value);
            if (nextVolumeId.HasValue) relatedIds.Add(nextVolumeId.Value);

            var memoryItem = new MemoryItem
            {
                Content = content,
                ImportanceScore = Math.Max(importance, 7), // 卷宗承接关系重要性较高
                Type = MemoryType.Relationship,
                Scope = MemoryScope.Volume,
                ProjectId = projectId,
                VolumeId = VolumeId,
                Tags = connectionTags,
                RelatedEntityIds = relatedIds,
                OriginalLength = content.Length
            };

            return await AddMemoryAsync(memoryItem);
        }

        /// <summary>
        /// 获取卷宗主要剧情线
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns>剧情线记忆项列表</returns>
        public List<MemoryItem> GetMainPlotLines(Guid projectId)
        {
            lock (_lockObject)
            {
                return _memories
                    .Where(m => m.ProjectId == projectId && m.VolumeId == VolumeId && m.Type == MemoryType.Plot)
                    .OrderByDescending(m => m.ImportanceScore)
                    .ToList();
            }
        }

        /// <summary>
        /// 获取角色发展轨迹
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="characterId">角色ID（可选）</param>
        /// <returns>角色发展记忆项列表</returns>
        public List<MemoryItem> GetCharacterDevelopments(Guid projectId, Guid? characterId = null)
        {
            lock (_lockObject)
            {
                var query = _memories.Where(m => 
                    m.ProjectId == projectId && 
                    m.VolumeId == VolumeId && 
                    m.Type == MemoryType.Character);

                if (characterId.HasValue)
                {
                    query = query.Where(m => m.RelatedEntityIds.Contains(characterId.Value));
                }

                return query.OrderByDescending(m => m.ImportanceScore).ToList();
            }
        }

        /// <summary>
        /// 获取重要事件
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="eventType">事件类型（可选）</param>
        /// <returns>事件记忆项列表</returns>
        public List<MemoryItem> GetImportantEvents(Guid projectId, string? eventType = null)
        {
            lock (_lockObject)
            {
                var query = _memories.Where(m => 
                    m.ProjectId == projectId && 
                    m.VolumeId == VolumeId && 
                    m.Type == MemoryType.Event);

                if (!string.IsNullOrEmpty(eventType))
                {
                    query = query.Where(m => m.Tags.Contains(eventType));
                }

                return query.OrderByDescending(m => m.ImportanceScore).ToList();
            }
        }

        /// <summary>
        /// 获取转折点
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns>转折点记忆项列表</returns>
        public List<MemoryItem> GetTurningPoints(Guid projectId)
        {
            lock (_lockObject)
            {
                return _memories
                    .Where(m => 
                        m.ProjectId == projectId && 
                        m.VolumeId == VolumeId && 
                        (m.Tags.Contains("转折点") || m.Tags.Contains("高潮") || m.Tags.Contains("冲突")) &&
                        m.ImportanceScore >= 7)
                    .OrderByDescending(m => m.ImportanceScore)
                    .ToList();
            }
        }

        /// <summary>
        /// 获取卷宗承接关系
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns>承接关系记忆项列表</returns>
        public List<MemoryItem> GetVolumeConnections(Guid projectId)
        {
            lock (_lockObject)
            {
                return _memories
                    .Where(m => 
                        m.ProjectId == projectId && 
                        m.VolumeId == VolumeId && 
                        m.Type == MemoryType.Relationship &&
                        m.Tags.Contains("卷宗承接"))
                    .OrderByDescending(m => m.ImportanceScore)
                    .ToList();
            }
        }

        /// <summary>
        /// 分析卷宗完整性
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns>完整性分析结果</returns>
        public async Task<VolumeCompletenessAnalysis> AnalyzeCompletenessAsync(Guid projectId)
        {
            try
            {
                _logger.LogDebug($"分析卷宗完整性: VolumeId={VolumeId}, ProjectId={projectId}");

                var analysis = new VolumeCompletenessAnalysis
                {
                    VolumeId = VolumeId,
                    ProjectId = projectId
                };

                var plotLines = GetMainPlotLines(projectId);
                var characterDevelopments = GetCharacterDevelopments(projectId);
                var importantEvents = GetImportantEvents(projectId);
                var turningPoints = GetTurningPoints(projectId);
                var connections = GetVolumeConnections(projectId);

                // 分析剧情线完整性
                analysis.HasMainPlotLine = plotLines.Any(p => p.ImportanceScore >= 8);
                analysis.PlotLineCount = plotLines.Count;

                // 分析角色发展完整性
                analysis.CharacterDevelopmentCount = characterDevelopments.Count;
                analysis.HasCharacterGrowth = characterDevelopments.Any(c => c.Tags.Contains("成长") || c.Tags.Contains("变化"));

                // 分析事件完整性
                analysis.ImportantEventCount = importantEvents.Count;
                analysis.HasClimax = turningPoints.Any(t => t.Tags.Contains("高潮"));

                // 分析承接关系
                analysis.HasVolumeConnection = connections.Any();

                // 计算完整性评分
                var score = 0.0;
                if (analysis.HasMainPlotLine) score += 30;
                if (analysis.PlotLineCount >= 3) score += 20;
                if (analysis.CharacterDevelopmentCount >= 2) score += 15;
                if (analysis.HasCharacterGrowth) score += 15;
                if (analysis.ImportantEventCount >= 5) score += 10;
                if (analysis.HasClimax) score += 10;

                analysis.CompletenessScore = Math.Min(score, 100);

                // 生成建议
                if (!analysis.HasMainPlotLine)
                    analysis.Suggestions.Add("建议添加明确的主要剧情线");
                if (analysis.PlotLineCount < 3)
                    analysis.Suggestions.Add("建议增加更多的剧情线以丰富故事内容");
                if (!analysis.HasCharacterGrowth)
                    analysis.Suggestions.Add("建议添加角色成长和变化的描述");
                if (!analysis.HasClimax)
                    analysis.Suggestions.Add("建议添加卷宗高潮部分");

                _logger.LogDebug($"卷宗完整性分析完成: Score={analysis.CompletenessScore}");
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析卷宗完整性失败");
                return new VolumeCompletenessAnalysis
                {
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
            return 6; // 卷宗记忆层压缩重要性低于6的记忆项
        }

        /// <inheritdoc/>
        protected override int GetHighImportanceThreshold()
        {
            return 7; // 卷宗记忆层高重要性阈值为7
        }

        #endregion
    }

    /// <summary>
    /// 卷宗完整性分析结果
    /// </summary>
    public class VolumeCompletenessAnalysis
    {
        /// <summary>
        /// 卷宗ID
        /// </summary>
        public Guid VolumeId { get; set; }

        /// <summary>
        /// 项目ID
        /// </summary>
        public Guid ProjectId { get; set; }

        /// <summary>
        /// 是否有主要剧情线
        /// </summary>
        public bool HasMainPlotLine { get; set; }

        /// <summary>
        /// 剧情线数量
        /// </summary>
        public int PlotLineCount { get; set; }

        /// <summary>
        /// 角色发展数量
        /// </summary>
        public int CharacterDevelopmentCount { get; set; }

        /// <summary>
        /// 是否有角色成长
        /// </summary>
        public bool HasCharacterGrowth { get; set; }

        /// <summary>
        /// 重要事件数量
        /// </summary>
        public int ImportantEventCount { get; set; }

        /// <summary>
        /// 是否有高潮
        /// </summary>
        public bool HasClimax { get; set; }

        /// <summary>
        /// 是否有卷宗承接关系
        /// </summary>
        public bool HasVolumeConnection { get; set; }

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
