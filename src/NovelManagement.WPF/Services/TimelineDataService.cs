using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;
using NovelManagement.WPF.Views;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 时间线数据服务。
    /// </summary>
    /// <remarks>
    /// 存储实现已由"项目级 JSON 文件"迁移为数据库（<see cref="TimelineEvent"/> 与 <see cref="TimelineEventParticipant"/>）。
    /// 对外仍保持原有一组异步方法签名，视图层（TimelineView）与章节同步链路（ChapterUpdateWorkflowService）无需感知存储变化。
    /// 首次读取某个项目时，若数据库中没有数据而旧的 JSON 文件仍然存在，会自动执行一次历史数据迁移，
    /// 迁移成功后将原 JSON 重命名为 <c>*.json.migrated</c> 保留备份，绝不直接删除用户数据。
    /// </remarks>
    public class TimelineDataService
    {
        private readonly ILogger<TimelineDataService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _timelineDirectory;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            WriteIndented = true
        };

        public TimelineDataService(ILogger<TimelineDataService> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _timelineDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NovelManagement",
                "timelines");
            Directory.CreateDirectory(_timelineDirectory);
        }

        /// <summary>
        /// 加载项目时间线事件（含参与者）。
        /// </summary>
        public async Task<List<TimelineEventViewModel>> LoadTimelineEventsAsync(Guid projectId)
        {
            try
            {
                if (projectId == Guid.Empty)
                {
                    return new List<TimelineEventViewModel>();
                }

                var hasLegacyFile = File.Exists(GetProjectTimelinePath(projectId));
                var existingCount = await _unitOfWork.TimelineEvents.CountByProjectIdAsync(projectId);

                if (existingCount == 0 && hasLegacyFile)
                {
                    await MigrateLegacyJsonAsync(projectId);
                }

                // 展示/检查路径使用只读查询，避免长生命周期的服务持续累积变更跟踪实体
                var events = await _unitOfWork.TimelineEvents.GetByProjectIdReadOnlyAsync(projectId);
                var result = events.Select(ToViewModel).ToList();

                // 兼容旧逻辑：视图层与统计逻辑使用 int Id 做展示序号
                for (var index = 0; index < result.Count; index++)
                {
                    result[index].Id = index + 1;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载时间线数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        /// <summary>
        /// 保存项目时间线事件（按 EventId 做增量 upsert，未出现在列表中的既有事件将被删除）。
        /// </summary>
        public async Task SaveTimelineEventsAsync(Guid projectId, IEnumerable<TimelineEventViewModel> events)
        {
            try
            {
                if (projectId == Guid.Empty)
                {
                    return;
                }

                var incoming = events?.ToList() ?? new List<TimelineEventViewModel>();
                var characterIdMap = await BuildCharacterIdMapAsync(projectId);

                var existing = (await _unitOfWork.TimelineEvents.GetByProjectIdAsync(projectId)).ToList();
                var existingMap = existing.ToDictionary(e => e.Id);
                var incomingIds = incoming
                    .Where(e => e.EventId != Guid.Empty)
                    .Select(e => e.EventId)
                    .ToHashSet();

                // 删除已不在列表中的事件
                foreach (var stale in existing.Where(e => !incomingIds.Contains(e.Id)))
                {
                    await _unitOfWork.TimelineEvents.DeleteByIdAsync(stale.Id);
                }

                for (var index = 0; index < incoming.Count; index++)
                {
                    var viewModel = incoming[index];

                    if (viewModel.EventId == Guid.Empty)
                    {
                        viewModel.EventId = Guid.NewGuid();
                    }

                    if (existingMap.TryGetValue(viewModel.EventId, out var entity))
                    {
                        ApplyToEntity(entity, viewModel, index, characterIdMap);
                        await _unitOfWork.TimelineEvents.UpdateAsync(entity);
                    }
                    else
                    {
                        var created = new TimelineEvent
                        {
                            Id = viewModel.EventId,
                            ProjectId = projectId,
                            CreatedAt = viewModel.CreatedAt == default ? DateTime.UtcNow : viewModel.CreatedAt
                        };
                        ApplyToEntity(created, viewModel, index, characterIdMap);
                        await _unitOfWork.TimelineEvents.AddAsync(created);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存时间线数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        /// <summary>
        /// 从 JSON 文件导入时间线事件（不落库，返回视图模型列表供调用方二次确认后保存）。
        /// </summary>
        public async Task<List<TimelineEventViewModel>> ImportTimelineEventsAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<TimelineProjectDocument>(json, _serializerOptions);
                var events = document?.Events ?? new List<TimelineEventViewModel>();

                // 导入数据作为全新记录写入，避免与当前库内记录撞 Id
                foreach (var evt in events)
                {
                    evt.EventId = Guid.NewGuid();
                }

                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入时间线数据失败，FilePath: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 导出项目时间线事件到 JSON 文件。
        /// </summary>
        public async Task ExportTimelineEventsAsync(Guid projectId, IEnumerable<TimelineEventViewModel> events, string filePath)
        {
            try
            {
                var document = new TimelineProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    Events = (events ?? Enumerable.Empty<TimelineEventViewModel>())
                        .OrderBy(e => e.EventDate)
                        .ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出时间线数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        #region 实体与视图模型转换

        private static TimelineEventViewModel ToViewModel(TimelineEvent entity)
        {
            return new TimelineEventViewModel
            {
                EventId = entity.Id,
                Title = entity.Title,
                Category = entity.Category ?? "历史事件",
                EventDate = entity.EventDate,
                Location = entity.Location ?? "",
                Importance = entity.Importance ?? "中",
                Status = entity.Status ?? "计划中",
                Description = entity.Description ?? "",
                Impact = entity.Impact ?? "",
                CreatedAt = entity.CreatedAt,
                Participants = entity.Participants
                    .OrderBy(p => p.Order)
                    .Select(p => new EventParticipantViewModel
                    {
                        Name = p.Name,
                        Type = p.Type ?? "角色",
                        Role = p.Role ?? "",
                        CharacterId = p.CharacterId
                    })
                    .ToList()
            }.WithParticipantCount();
        }

        private static void ApplyToEntity(
            TimelineEvent entity,
            TimelineEventViewModel viewModel,
            int displayOrder,
            IReadOnlyDictionary<string, Guid> characterIdMap)
        {
            entity.Title = string.IsNullOrWhiteSpace(viewModel.Title) ? "未命名事件" : viewModel.Title.Trim();
            entity.Category = string.IsNullOrWhiteSpace(viewModel.Category) ? "历史事件" : viewModel.Category;
            entity.EventDate = viewModel.EventDate;
            entity.Location = NullIfBlank(viewModel.Location);
            entity.Importance = NullIfBlank(viewModel.Importance);
            entity.Status = NullIfBlank(viewModel.Status);
            entity.Description = NullIfBlank(viewModel.Description);
            entity.Impact = NullIfBlank(viewModel.Impact);
            entity.Tags = NullIfBlank(viewModel.Tags);
            entity.DisplayOrder = displayOrder;
            entity.UpdatedAt = DateTime.UtcNow;

            var participants = viewModel.Participants ?? new List<EventParticipantViewModel>();
            var incomingIds = new HashSet<Guid>();

            // 复用既有参与者记录，避免每次保存都产生新行
            for (var index = 0; index < participants.Count; index++)
            {
                var participant = participants[index];
                if (string.IsNullOrWhiteSpace(participant.Name))
                {
                    continue;
                }

                var name = participant.Name.Trim();
                var resolvedCharacterId = participant.CharacterId;
                if (resolvedCharacterId == null && characterIdMap.TryGetValue(name, out var mappedId))
                {
                    resolvedCharacterId = mappedId;
                }

                var existing = entity.Participants.FirstOrDefault(p =>
                    (resolvedCharacterId.HasValue && p.CharacterId == resolvedCharacterId.Value)
                    || (!resolvedCharacterId.HasValue && p.Name == name));

                if (existing == null)
                {
                    existing = new TimelineEventParticipant
                    {
                        Id = Guid.NewGuid(),
                        TimelineEventId = entity.Id
                    };
                    entity.Participants.Add(existing);
                }

                existing.Name = name;
                existing.Type = NullIfBlank(participant.Type);
                existing.Role = NullIfBlank(participant.Role);
                existing.CharacterId = resolvedCharacterId;
                existing.Order = index;
                existing.UpdatedAt = DateTime.UtcNow;
                incomingIds.Add(existing.Id);
            }

            var staleParticipants = entity.Participants.Where(p => !incomingIds.Contains(p.Id)).ToList();
            foreach (var stale in staleParticipants)
            {
                entity.Participants.Remove(stale);
            }
        }

        private static string? NullIfBlank(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// 构建项目内"角色名称 -> 角色ID"映射，用于把参与者文本关联到真实角色。
        /// </summary>
        private async Task<Dictionary<string, Guid>> BuildCharacterIdMapAsync(Guid projectId)
        {
            var map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var characters = await _unitOfWork.Characters.GetByProjectIdAsync(projectId);
                foreach (var character in characters)
                {
                    if (string.IsNullOrWhiteSpace(character.Name))
                    {
                        continue;
                    }

                    var key = character.Name.Trim();
                    if (!map.ContainsKey(key))
                    {
                        map[key] = character.Id;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "构建角色名称映射失败，参与者将仅按名称保存，ProjectId: {ProjectId}", projectId);
            }

            return map;
        }

        #endregion

        #region 历史 JSON 数据迁移

        /// <summary>
        /// 将旧的项目级 JSON 时间线数据一次性迁移到数据库。
        /// </summary>
        public async Task<int> MigrateLegacyJsonAsync(Guid projectId)
        {
            var legacyPath = GetProjectTimelinePath(projectId);
            if (!File.Exists(legacyPath))
            {
                return 0;
            }

            try
            {
                var json = await File.ReadAllTextAsync(legacyPath);
                var document = JsonSerializer.Deserialize<TimelineProjectDocument>(json, _serializerOptions);
                var legacyEvents = document?.Events ?? new List<TimelineEventViewModel>();
                if (legacyEvents.Count == 0)
                {
                    ArchiveLegacyFile(legacyPath);
                    return 0;
                }

                var legacyIds = legacyEvents.Select(e => e.EventId == Guid.Empty ? e.Id.ToString() : e.EventId.ToString()).ToList();
                var alreadyMigrated = await _unitOfWork.TimelineEvents.FilterExistingLegacyIdsAsync(projectId, legacyIds);
                var migratedSet = new HashSet<string>(alreadyMigrated, StringComparer.OrdinalIgnoreCase);

                var characterIdMap = await BuildCharacterIdMapAsync(projectId);
                var migrated = 0;

                for (var index = 0; index < legacyEvents.Count; index++)
                {
                    var legacy = legacyEvents[index];
                    var legacyId = legacy.EventId == Guid.Empty ? legacy.Id.ToString() : legacy.EventId.ToString();
                    if (migratedSet.Contains(legacyId))
                    {
                        continue;
                    }

                    var entity = new TimelineEvent
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        LegacyId = legacyId,
                        CreatedAt = legacy.CreatedAt == default ? DateTime.UtcNow : legacy.CreatedAt
                    };
                    ApplyToEntity(entity, legacy, index, characterIdMap);
                    await _unitOfWork.TimelineEvents.AddAsync(entity);
                    migrated++;
                }

                if (migrated > 0)
                {
                    await _unitOfWork.SaveChangesAsync();
                }

                ArchiveLegacyFile(legacyPath);
                _logger.LogInformation("时间线历史 JSON 数据迁移完成，ProjectId: {ProjectId}，迁移 {Count} 条事件", projectId, migrated);
                return migrated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "迁移时间线历史 JSON 数据失败，ProjectId: {ProjectId}，已保留原文件不改动", projectId);
                return 0;
            }
        }

        /// <summary>
        /// 归档旧 JSON 文件（重命名备份，不删除）。
        /// </summary>
        private void ArchiveLegacyFile(string legacyPath)
        {
            try
            {
                var archivedPath = legacyPath + ".migrated";
                if (File.Exists(archivedPath))
                {
                    File.Delete(archivedPath);
                }

                File.Move(legacyPath, archivedPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "归档时间线历史 JSON 文件失败：{Path}", legacyPath);
            }
        }

        #endregion

        private string GetProjectTimelinePath(Guid projectId)
        {
            return Path.Combine(_timelineDirectory, $"{projectId:N}.json");
        }
    }

    /// <summary>
    /// 项目时间线文档（导入/导出与历史迁移格式）。
    /// </summary>
    public sealed class TimelineProjectDocument
    {
        public Guid ProjectId { get; init; }
        public DateTime UpdatedAt { get; init; }
        public List<TimelineEventViewModel> Events { get; init; } = new();
    }
}
