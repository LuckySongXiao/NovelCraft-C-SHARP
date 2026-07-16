using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NovelManagement.Application.DTOs;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NovelManagement.Application.Services
{
    /// <summary>
    /// 导入服务
    /// </summary>
    public class ImportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ImportService> _logger;
        private readonly ExcelProcessingService _excelService;
        private readonly WordProcessingService _wordService;
        private readonly Dictionary<Guid, OperationResultDto> _activeOperations;
        private readonly Dictionary<Guid, List<OperationHistoryDto>> _historyByProject = new();
        private readonly object _historyLock = new();

        /// <summary>
        /// 导入进度更新事件
        /// </summary>
        public event EventHandler<OperationResultDto>? ProgressUpdated;

        public ImportService(
            IUnitOfWork unitOfWork,
            ILogger<ImportService> logger,
            ExcelProcessingService excelService,
            WordProcessingService wordService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _wordService = wordService ?? throw new ArgumentNullException(nameof(wordService));
            _activeOperations = new Dictionary<Guid, OperationResultDto>();
        }

        /// <summary>
        /// 预览导入文件
        /// </summary>
        public async Task<ImportPreviewDto> PreviewImportFileAsync(string filePath, ImportFormat format)
        {
            try
            {
                _logger.LogInformation("开始预览导入文件: {FilePath}, 格式: {Format}", filePath, format);

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"文件不存在: {filePath}");
                }

                ImportPreviewDto preview = format switch
                {
                    ImportFormat.EXCEL => await _excelService.PreviewExcelFileAsync(filePath),
                    ImportFormat.DOCX => await _wordService.PreviewWordFileAsync(filePath),
                    ImportFormat.CSV => await BuildPreviewFromSectionsAsync(filePath, format, await ParseCsvSectionsAsync(filePath)),
                    ImportFormat.TXT => await BuildPreviewFromSectionsAsync(filePath, format, await ParseTxtSectionsAsync(filePath)),
                    ImportFormat.JSON => await BuildPreviewFromSectionsAsync(filePath, format, await ParseJsonSectionsAsync(filePath)),
                    ImportFormat.XML => await BuildPreviewFromSectionsAsync(filePath, format, await ParseXmlSectionsAsync(filePath)),
                    _ => throw new NotSupportedException($"不支持的导入格式: {format}")
                };

                preview.FileInfo.Format = format.ToString();
                preview.FileInfo.FilePath = filePath;

                _logger.LogInformation("文件预览完成: {FilePath}, 检测到 {Count} 个数据区", filePath, preview.DetectedDataTypes.Count);
                return preview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "预览导入文件失败: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 开始导入操作
        /// </summary>
        public Task<OperationResultDto> StartImportAsync(ImportRequestDto request)
        {
            var operationId = Guid.NewGuid();
            var result = new OperationResultDto
            {
                OperationId = operationId,
                Status = OperationStatus.Pending,
                StartTime = DateTime.Now,
                CurrentStep = "准备导入..."
            };
            result.Metadata["ProjectId"] = request.ProjectId;
            result.Metadata["Format"] = request.Format.ToString();
            result.Metadata["SourcePath"] = request.SourcePath;
            _activeOperations[operationId] = result;

            try
            {
                _logger.LogInformation("开始导入操作: {OperationId}, 格式: {Format}, 文件: {SourcePath}",
                    operationId, request.Format, request.SourcePath);
                _ = Task.Run(() => ExecuteImportAsync(request, result));
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动导入操作失败: {OperationId}", operationId);
                result.Status = OperationStatus.Failed;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.Now;
                RecordHistory(request.ProjectId, result, "Import", ex.Message, string.Empty);
                return Task.FromResult(result);
            }
        }

        /// <summary>
        /// 获取操作状态
        /// </summary>
        public OperationResultDto? GetOperationStatus(Guid operationId)
        {
            return _activeOperations.TryGetValue(operationId, out var result) ? result : null;
        }

        /// <summary>
        /// 取消导入操作
        /// </summary>
        public bool CancelImport(Guid operationId)
        {
            if (_activeOperations.TryGetValue(operationId, out var result)
                && (result.Status == OperationStatus.InProgress || result.Status == OperationStatus.Pending))
            {
                result.Status = OperationStatus.Cancelled;
                result.EndTime = DateTime.Now;
                result.CurrentStep = "操作已取消";
                OnProgressUpdated(result);
                if (TryGetProjectId(result, out var projectId))
                {
                    RecordHistory(projectId, result, "Import", "用户取消导入", string.Empty);
                }
                _logger.LogInformation("导入操作已取消: {OperationId}", operationId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取导入历史记录
        /// </summary>
        public Task<List<OperationHistoryDto>> GetImportHistoryAsync(Guid projectId)
        {
            lock (_historyLock)
            {
                _historyByProject.TryGetValue(projectId, out var histories);
                histories ??= new List<OperationHistoryDto>();

                var active = _activeOperations.Values
                    .Where(result => TryGetProjectId(result, out var activeProjectId) && activeProjectId == projectId)
                    .Where(result => result.Status == OperationStatus.Pending || result.Status == OperationStatus.InProgress)
                    .Select(result => ToHistoryDto(projectId, result, "Import", result.CurrentStep, string.Empty))
                    .ToList();

                return Task.FromResult(histories
                    .Concat(active)
                    .OrderByDescending(item => item.StartTime)
                    .ToList());
            }
        }

        private async Task ExecuteImportAsync(ImportRequestDto request, OperationResultDto result)
        {
            string projectName = string.Empty;
            try
            {
                result.Status = OperationStatus.InProgress;
                result.CurrentStep = "正在校验项目...";
                result.Progress = 5;
                OnProgressUpdated(result);

                if (!File.Exists(request.SourcePath))
                {
                    throw new FileNotFoundException($"源文件不存在: {request.SourcePath}");
                }

                var project = await _unitOfWork.Projects.GetByIdAsync(request.ProjectId)
                    ?? throw new InvalidOperationException("未找到当前项目，无法执行导入。");
                projectName = project.Name;

                if (request.CreateBackup)
                {
                    result.CurrentStep = "正在创建备份...";
                    result.Progress = 15;
                    OnProgressUpdated(result);
                    var backupPath = await CreateBackupAsync(project);
                    result.Metadata["BackupPath"] = backupPath;
                }

                if (result.Status == OperationStatus.Cancelled)
                {
                    return;
                }

                result.CurrentStep = "正在解析文件...";
                result.Progress = 30;
                OnProgressUpdated(result);
                var sections = await ParseFileAsync(request);
                result.TotalItems = sections.Sum(section => section.Rows.Count);

                result.CurrentStep = "正在验证数据...";
                result.Progress = 45;
                OnProgressUpdated(result);
                var validationResult = ValidateImportData(sections);
                result.Warnings.AddRange(validationResult.Warnings);
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException(string.Join("；", validationResult.Warnings));
                }

                if (result.Status == OperationStatus.Cancelled)
                {
                    return;
                }

                result.CurrentStep = "正在导入数据...";
                result.Progress = 55;
                OnProgressUpdated(result);
                await ImportDataAsync(sections, request, result, projectName);

                if (result.Status == OperationStatus.Cancelled)
                {
                    return;
                }

                result.Status = OperationStatus.Completed;
                result.EndTime = DateTime.Now;
                result.Progress = 100;
                result.CurrentStep = "导入完成";
                result.IsSuccess = true;
                OnProgressUpdated(result);
                RecordHistory(request.ProjectId, result, "Import", BuildImportNote(sections), projectName);
                _logger.LogInformation("导入操作完成: {OperationId}, 处理了 {ProcessedItems} 条记录",
                    result.OperationId, result.ProcessedItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入操作失败: {OperationId}", result.OperationId);
                result.Status = OperationStatus.Failed;
                result.EndTime = DateTime.Now;
                result.ErrorMessage = ex.Message;
                result.IsSuccess = false;
                result.CurrentStep = "导入失败";
                OnProgressUpdated(result);
                RecordHistory(request.ProjectId, result, "Import", ex.Message, projectName);
            }
            finally
            {
                _activeOperations[result.OperationId] = result;
            }
        }

        private async Task<string> CreateBackupAsync(Project project)
        {
            var backupRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "NovelCraftBackups",
                SanitizeFileName(project.Name));
            Directory.CreateDirectory(backupRoot);

            var backupPath = Path.Combine(backupRoot, $"{DateTime.Now:yyyyMMdd_HHmmss}_import_backup.json");
            var payload = new Dictionary<string, object?>
            {
                ["Project"] = ConvertEntityToDictionary(project),
                ["Volumes"] = (await _unitOfWork.Volumes.GetByProjectIdAsync(project.Id)).Select(ConvertEntityToDictionary).ToList(),
                ["Chapters"] = (await _unitOfWork.Chapters.GetByProjectIdAsync(project.Id)).Select(ConvertEntityToDictionary).ToList(),
                ["Characters"] = (await _unitOfWork.Characters.GetByProjectIdAsync(project.Id)).Select(ConvertEntityToDictionary).ToList(),
                ["Factions"] = (await _unitOfWork.Factions.GetByProjectIdAsync(project.Id)).Select(ConvertEntityToDictionary).ToList(),
                ["Plots"] = (await _unitOfWork.Plots.GetByProjectIdAsync(project.Id)).Select(ConvertEntityToDictionary).ToList(),
                ["WorldSettings"] = (await _unitOfWork.WorldSettings.GetByProjectIdAsync(project.Id)).Select(ConvertEntityToDictionary).ToList(),
                ["BackupAt"] = DateTime.Now
            };

            var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
            await File.WriteAllTextAsync(backupPath, json, Encoding.UTF8);
            return backupPath;
        }

        private async Task<List<ImportSectionData>> ParseFileAsync(ImportRequestDto request)
        {
            return request.Format switch
            {
                ImportFormat.EXCEL => (await _excelService.ImportFromExcelAsync(request.SourcePath))
                    .Select(pair => new ImportSectionData(pair.Key, pair.Value))
                    .ToList(),
                ImportFormat.DOCX => (await _wordService.ImportFromWordAsync(request.SourcePath))
                    .Select(pair => new ImportSectionData(pair.Key, pair.Value))
                    .ToList(),
                ImportFormat.CSV => await ParseCsvSectionsAsync(request.SourcePath),
                ImportFormat.TXT => await ParseTxtSectionsAsync(request.SourcePath),
                ImportFormat.JSON => await ParseJsonSectionsAsync(request.SourcePath),
                ImportFormat.XML => await ParseXmlSectionsAsync(request.SourcePath),
                _ => throw new NotSupportedException($"不支持的导入格式: {request.Format}")
            };
        }

        private static ValidationResult ValidateImportData(List<ImportSectionData> sections)
        {
            var result = new ValidationResult { IsValid = true };
            var totalRows = sections.Sum(section => section.Rows.Count);
            if (totalRows == 0)
            {
                result.IsValid = false;
                result.Warnings.Add("没有找到可导入的数据。");
                return result;
            }

            var recognizedRows = sections
                .Where(section => DetermineSectionType(section.Name, section.Rows.FirstOrDefault()) != ImportSectionType.Unknown)
                .Sum(section => section.Rows.Count);

            if (recognizedRows == 0)
            {
                result.IsValid = false;
                result.Warnings.Add("未识别到可导入的数据结构，当前版本支持卷宗、章节、角色、势力、剧情和世界设定。");
            }

            if (totalRows > 1000)
            {
                result.Warnings.Add("数据量较大，导入过程可能需要更长时间。");
            }

            foreach (var section in sections.Where(section =>
                         DetermineSectionType(section.Name, section.Rows.FirstOrDefault()) == ImportSectionType.Unknown))
            {
                result.Warnings.Add($"未识别的数据区将被跳过：{section.Name}");
            }

            return result;
        }

        private async Task ImportDataAsync(
            List<ImportSectionData> sections,
            ImportRequestDto request,
            OperationResultDto result,
            string projectName)
        {
            var context = await CreateImportContextAsync(request.ProjectId);
            var orderedSections = sections
                .OrderBy(section => GetSectionPriority(DetermineSectionType(section.Name, section.Rows.FirstOrDefault())))
                .ToList();

            var processed = 0;
            var total = Math.Max(result.TotalItems, 1);

            foreach (var section in orderedSections)
            {
                var sectionType = DetermineSectionType(section.Name, section.Rows.FirstOrDefault());
                if (sectionType == ImportSectionType.Unknown)
                {
                    continue;
                }

                for (var i = 0; i < section.Rows.Count; i++)
                {
                    if (result.Status == OperationStatus.Cancelled)
                    {
                        return;
                    }

                    result.CurrentStep = $"正在导入 {section.Name}：{i + 1}/{section.Rows.Count}";
                    await ImportRowAsync(sectionType, section.Rows[i], request, context, result.Warnings);

                    processed++;
                    result.ProcessedItems = processed;
                    result.Progress = 55 + (int)((double)processed / total * 40);
                    OnProgressUpdated(result);
                }
            }

            result.Metadata["ImportedProjectName"] = projectName;
        }

        private async Task<ImportExecutionContext> CreateImportContextAsync(Guid projectId)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId)
                ?? throw new InvalidOperationException("未找到当前项目，无法写入导入数据。");

            var context = new ImportExecutionContext
            {
                Project = project,
                Volumes = (await _unitOfWork.Volumes.GetByProjectIdAsync(projectId)).OrderBy(v => v.Order).ToList(),
                Chapters = (await _unitOfWork.Chapters.GetByProjectIdAsync(projectId)).OrderBy(c => c.Order).ToList(),
                Characters = (await _unitOfWork.Characters.GetByProjectIdAsync(projectId)).ToList(),
                Factions = (await _unitOfWork.Factions.GetByProjectIdAsync(projectId)).ToList(),
                Plots = (await _unitOfWork.Plots.GetByProjectIdAsync(projectId)).ToList(),
                WorldSettings = (await _unitOfWork.WorldSettings.GetByProjectIdAsync(projectId)).ToList()
            };

            context.DefaultVolumeId = await EnsureImportVolumeAsync(context);
            return context;
        }

        private async Task ImportRowAsync(
            ImportSectionType sectionType,
            Dictionary<string, object> row,
            ImportRequestDto request,
            ImportExecutionContext context,
            List<string> warnings)
        {
            switch (sectionType)
            {
                case ImportSectionType.Volumes:
                    await ImportVolumeAsync(row, request, context, warnings);
                    break;
                case ImportSectionType.Chapters:
                    await ImportChapterAsync(row, request, context, warnings);
                    break;
                case ImportSectionType.Characters:
                    await ImportCharacterAsync(row, request, context, warnings);
                    break;
                case ImportSectionType.Factions:
                    await ImportFactionAsync(row, request, context, warnings);
                    break;
                case ImportSectionType.Plots:
                    await ImportPlotAsync(row, request, context, warnings);
                    break;
                case ImportSectionType.WorldSettings:
                    await ImportWorldSettingAsync(row, request, context, warnings);
                    break;
            }
        }

        private async Task ImportVolumeAsync(
            Dictionary<string, object> row,
            ImportRequestDto request,
            ImportExecutionContext context,
            List<string> warnings)
        {
            var title = GetString(row, "Title", "卷宗标题", "标题", "Name");
            if (string.IsNullOrWhiteSpace(title))
            {
                warnings.Add("已跳过一条卷宗数据：缺少标题。");
                return;
            }

            var oldId = GetGuid(row, "Id");
            var existing = context.Volumes.FirstOrDefault(volume =>
                string.Equals(volume.Title, title, StringComparison.OrdinalIgnoreCase));

            if (existing != null && !request.OverwriteExisting)
            {
                if (oldId != Guid.Empty)
                {
                    context.VolumeIdMap[oldId] = existing.Id;
                }
                return;
            }

            var volume = existing ?? new Volume
            {
                ProjectId = request.ProjectId,
                CreatedAt = DateTime.UtcNow
            };

            volume.Title = title;
            volume.Description = GetString(row, "Description", "描述");
            volume.Status = GetString(row, "Status", "状态") ?? volume.Status;
            volume.Type = GetString(row, "Type", "类型");
            volume.Tags = GetString(row, "Tags", "标签");
            volume.Notes = GetString(row, "Notes", "备注");
            volume.Order = GetInt(row, "Order", "序号") ?? volume.Order;
            volume.Progress = GetDecimal(row, "Progress", "进度") ?? volume.Progress;
            volume.EstimatedWordCount = GetInt(row, "EstimatedWordCount", "预计字数");
            volume.WordCount = GetInt(row, "ActualWordCount", "WordCount", "字数") ?? volume.WordCount;
            volume.UpdatedAt = DateTime.UtcNow;

            if (existing == null)
            {
                if (volume.Order <= 0)
                {
                    volume.Order = context.Volumes.Any() ? context.Volumes.Max(item => item.Order) + 1 : 1;
                }
                await _unitOfWork.Volumes.AddAsync(volume);
                context.Volumes.Add(volume);
            }
            else
            {
                await _unitOfWork.Volumes.UpdateAsync(volume);
            }

            await _unitOfWork.SaveChangesAsync();
            if (oldId != Guid.Empty)
            {
                context.VolumeIdMap[oldId] = volume.Id;
            }
        }

        private async Task ImportChapterAsync(
            Dictionary<string, object> row,
            ImportRequestDto request,
            ImportExecutionContext context,
            List<string> warnings)
        {
            var title = GetString(row, "Title", "章节标题", "标题", "Name");
            if (string.IsNullOrWhiteSpace(title))
            {
                warnings.Add("已跳过一条章节数据：缺少标题。");
                return;
            }

            var content = GetString(row, "Content", "内容", "正文") ?? string.Empty;
            var oldId = GetGuid(row, "Id");
            var targetVolumeId = ResolveVolumeId(row, context);
            var existing = context.Chapters.FirstOrDefault(chapter =>
                chapter.VolumeId == targetVolumeId &&
                string.Equals(chapter.Title, title, StringComparison.OrdinalIgnoreCase));

            if (existing != null && !request.OverwriteExisting)
            {
                if (oldId != Guid.Empty)
                {
                    context.ChapterIdMap[oldId] = existing.Id;
                }
                return;
            }

            var chapter = existing ?? new Chapter
            {
                VolumeId = targetVolumeId,
                CreatedAt = DateTime.UtcNow
            };

            chapter.VolumeId = targetVolumeId;
            chapter.Title = title;
            chapter.Content = content;
            chapter.Summary = GetString(row, "Summary", "摘要");
            chapter.Status = GetString(row, "Status", "状态") ?? chapter.Status;
            chapter.Type = GetString(row, "Type", "类型");
            chapter.Tags = GetString(row, "Tags", "标签");
            chapter.Notes = GetString(row, "Notes", "备注");
            chapter.Importance = GetInt(row, "Importance", "重要性");
            chapter.DifficultyLevel = GetInt(row, "DifficultyLevel", "难度");
            chapter.ReadingTime = GetInt(row, "ReadingTime", "阅读时长");
            chapter.Order = GetInt(row, "Order", "序号") ?? chapter.Order;
            chapter.WordCount = GetInt(row, "WordCount", "字数") ?? content.Length;
            chapter.LastEditedAt = DateTime.UtcNow;
            chapter.UpdatedAt = DateTime.UtcNow;

            if (existing == null)
            {
                if (chapter.Order <= 0)
                {
                    chapter.Order = context.Chapters
                        .Where(item => item.VolumeId == targetVolumeId)
                        .Select(item => item.Order)
                        .DefaultIfEmpty(0)
                        .Max() + 1;
                }
                await _unitOfWork.Chapters.AddAsync(chapter);
                context.Chapters.Add(chapter);
            }
            else
            {
                await _unitOfWork.Chapters.UpdateAsync(chapter);
            }

            await _unitOfWork.SaveChangesAsync();
            if (oldId != Guid.Empty)
            {
                context.ChapterIdMap[oldId] = chapter.Id;
            }
        }

        private async Task ImportFactionAsync(
            Dictionary<string, object> row,
            ImportRequestDto request,
            ImportExecutionContext context,
            List<string> warnings)
        {
            var name = GetString(row, "Name", "名称", "势力名称");
            if (string.IsNullOrWhiteSpace(name))
            {
                warnings.Add("已跳过一条势力数据：缺少名称。");
                return;
            }

            var oldId = GetGuid(row, "Id");
            var existing = context.Factions.FirstOrDefault(faction =>
                string.Equals(faction.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing != null && !request.OverwriteExisting)
            {
                if (oldId != Guid.Empty)
                {
                    context.FactionIdMap[oldId] = existing.Id;
                }
                return;
            }

            var faction = existing ?? new Faction
            {
                ProjectId = request.ProjectId,
                CreatedAt = DateTime.UtcNow
            };

            faction.Name = name;
            faction.Type = GetString(row, "Type", "类型") ?? faction.Type;
            faction.Level = GetString(row, "Level", "等级");
            faction.Description = GetString(row, "Description", "描述");
            faction.History = GetString(row, "History", "历史");
            faction.SpecialAbilities = GetString(row, "SpecialAbilities", "特色能力");
            faction.Resources = GetString(row, "Resources", "资源");
            faction.Tags = GetString(row, "Tags", "标签");
            faction.Notes = GetString(row, "Notes", "备注");
            faction.Status = GetString(row, "Status", "状态") ?? faction.Status;
            faction.MemberCount = GetInt(row, "MemberCount", "成员数量");
            faction.PowerLevel = GetInt(row, "PowerLevel", "实力等级") ?? faction.PowerLevel;
            faction.Influence = GetInt(row, "Influence", "影响力") ?? faction.Influence;
            faction.Importance = GetInt(row, "Importance", "重要性") ?? faction.Importance;
            faction.UpdatedAt = DateTime.UtcNow;

            if (existing == null)
            {
                await _unitOfWork.Factions.AddAsync(faction);
                context.Factions.Add(faction);
            }
            else
            {
                await _unitOfWork.Factions.UpdateAsync(faction);
            }

            await _unitOfWork.SaveChangesAsync();
            if (oldId != Guid.Empty)
            {
                context.FactionIdMap[oldId] = faction.Id;
            }
        }

        private async Task ImportCharacterAsync(
            Dictionary<string, object> row,
            ImportRequestDto request,
            ImportExecutionContext context,
            List<string> warnings)
        {
            var name = GetString(row, "Name", "姓名", "名称");
            if (string.IsNullOrWhiteSpace(name))
            {
                warnings.Add("已跳过一条角色数据：缺少名称。");
                return;
            }

            var existing = context.Characters.FirstOrDefault(character =>
                string.Equals(character.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing != null && !request.OverwriteExisting)
            {
                return;
            }

            var character = existing ?? new Character
            {
                ProjectId = request.ProjectId,
                CreatedAt = DateTime.UtcNow
            };

            character.Name = name;
            character.Type = GetString(row, "Type", "角色类型", "类型") ?? character.Type;
            character.Gender = GetString(row, "Gender", "性别");
            character.Age = GetInt(row, "Age", "年龄");
            character.CultivationLevel = GetString(row, "CultivationLevel", "修为");
            character.Appearance = GetString(row, "Appearance", "外貌");
            character.Personality = GetString(row, "Personality", "性格");
            character.Background = GetString(row, "Background", "背景故事", "背景");
            character.Abilities = GetString(row, "Abilities", "能力技能");
            character.History = GetString(row, "History", "履历");
            character.Tags = GetString(row, "Tags", "标签");
            character.Notes = GetString(row, "Notes", "备注");
            character.Status = GetString(row, "Status", "状态") ?? character.Status;
            character.Importance = GetInt(row, "Importance", "重要性") ?? character.Importance;
            character.FactionId = ResolveFactionId(row, context);
            character.FirstAppearanceChapterId = ResolveChapterId(row, context, "FirstAppearanceChapterId", "首次出场章节ID");
            character.LastAppearanceChapterId = ResolveChapterId(row, context, "LastAppearanceChapterId", "最后出场章节ID");
            character.UpdatedAt = DateTime.UtcNow;

            if (existing == null)
            {
                await _unitOfWork.Characters.AddAsync(character);
                context.Characters.Add(character);
            }
            else
            {
                await _unitOfWork.Characters.UpdateAsync(character);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        private async Task ImportWorldSettingAsync(
            Dictionary<string, object> row,
            ImportRequestDto request,
            ImportExecutionContext context,
            List<string> warnings)
        {
            var name = GetString(row, "Name", "设定名称", "名称");
            if (string.IsNullOrWhiteSpace(name))
            {
                warnings.Add("已跳过一条世界设定数据：缺少名称。");
                return;
            }

            var existing = context.WorldSettings.FirstOrDefault(setting =>
                string.Equals(setting.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing != null && !request.OverwriteExisting)
            {
                return;
            }

            var setting = existing ?? new WorldSetting
            {
                ProjectId = request.ProjectId,
                CreatedAt = DateTime.UtcNow
            };

            setting.Name = name;
            setting.Type = GetString(row, "Type", "类型") ?? setting.Type;
            setting.Category = GetString(row, "Category", "分类");
            setting.Description = GetString(row, "Description", "描述");
            setting.Content = GetString(row, "Content", "内容");
            setting.Rules = GetString(row, "Rules", "规则");
            setting.History = GetString(row, "History", "历史");
            setting.RelatedSettings = GetString(row, "RelatedSettings", "相关设定");
            setting.Tags = GetString(row, "Tags", "标签");
            setting.Notes = GetString(row, "Notes", "备注");
            setting.Status = GetString(row, "Status", "状态") ?? setting.Status;
            setting.Order = GetInt(row, "Order", "序号") ?? setting.Order;
            setting.Importance = GetInt(row, "Importance", "重要性") ?? setting.Importance;
            setting.IsPublic = GetBool(row, "IsPublic", "是否公开") ?? setting.IsPublic;
            setting.UpdatedAt = DateTime.UtcNow;

            if (existing == null)
            {
                await _unitOfWork.WorldSettings.AddAsync(setting);
                context.WorldSettings.Add(setting);
            }
            else
            {
                await _unitOfWork.WorldSettings.UpdateAsync(setting);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        private async Task ImportPlotAsync(
            Dictionary<string, object> row,
            ImportRequestDto request,
            ImportExecutionContext context,
            List<string> warnings)
        {
            var title = GetString(row, "Title", "剧情标题", "标题", "Name");
            if (string.IsNullOrWhiteSpace(title))
            {
                warnings.Add("已跳过一条剧情数据：缺少标题。");
                return;
            }

            var existing = context.Plots.FirstOrDefault(plot =>
                string.Equals(plot.Title, title, StringComparison.OrdinalIgnoreCase));
            if (existing != null && !request.OverwriteExisting)
            {
                return;
            }

            var plot = existing ?? new Plot
            {
                ProjectId = request.ProjectId,
                CreatedAt = DateTime.UtcNow
            };

            plot.Title = title;
            plot.Type = GetString(row, "Type", "类型") ?? plot.Type;
            plot.Status = GetString(row, "Status", "状态") ?? plot.Status;
            plot.Priority = GetString(row, "Priority", "优先级") ?? plot.Priority;
            plot.Description = GetString(row, "Description", "描述");
            plot.Outline = GetString(row, "Outline", "大纲");
            plot.ConflictElements = GetString(row, "ConflictElements", "冲突要素");
            plot.ThemeElements = GetString(row, "ThemeElements", "主题元素");
            plot.Tags = GetString(row, "Tags", "标签");
            plot.Notes = GetString(row, "Notes", "备注");
            plot.Progress = GetDecimal(row, "Progress", "进度") ?? plot.Progress;
            plot.Importance = GetInt(row, "Importance", "重要性") ?? plot.Importance;
            plot.StartChapterId = ResolveChapterId(row, context, "StartChapterId", "起始章节ID");
            plot.EndChapterId = ResolveChapterId(row, context, "EndChapterId", "结束章节ID");
            plot.UpdatedAt = DateTime.UtcNow;

            if (existing == null)
            {
                await _unitOfWork.Plots.AddAsync(plot);
                context.Plots.Add(plot);
            }
            else
            {
                await _unitOfWork.Plots.UpdateAsync(plot);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<Guid> EnsureImportVolumeAsync(ImportExecutionContext context)
        {
            var existing = context.Volumes.FirstOrDefault(volume =>
                string.Equals(volume.Title, "导入卷", StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                return existing.Id;
            }

            var volume = new Volume
            {
                ProjectId = context.Project.Id,
                Title = "导入卷",
                Description = "导入服务自动创建的默认卷。",
                Status = "Planning",
                Order = context.Volumes.Any() ? context.Volumes.Max(item => item.Order) + 1 : 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Volumes.AddAsync(volume);
            await _unitOfWork.SaveChangesAsync();
            context.Volumes.Add(volume);
            return volume.Id;
        }

        private Guid ResolveVolumeId(Dictionary<string, object> row, ImportExecutionContext context)
        {
            var oldVolumeId = GetGuid(row, "VolumeId", "卷宗ID");
            if (oldVolumeId != Guid.Empty && context.VolumeIdMap.TryGetValue(oldVolumeId, out var mappedVolumeId))
            {
                return mappedVolumeId;
            }

            var volumeTitle = GetString(row, "VolumeTitle", "Volume", "卷宗标题", "卷宗");
            if (!string.IsNullOrWhiteSpace(volumeTitle))
            {
                var matched = context.Volumes.FirstOrDefault(volume =>
                    string.Equals(volume.Title, volumeTitle, StringComparison.OrdinalIgnoreCase));
                if (matched != null)
                {
                    return matched.Id;
                }
            }

            return context.DefaultVolumeId;
        }

        private Guid? ResolveFactionId(Dictionary<string, object> row, ImportExecutionContext context)
        {
            var oldFactionId = GetGuid(row, "FactionId", "势力ID");
            if (oldFactionId != Guid.Empty && context.FactionIdMap.TryGetValue(oldFactionId, out var mappedFactionId))
            {
                return mappedFactionId;
            }

            var factionName = GetString(row, "Faction", "FactionName", "势力");
            if (string.IsNullOrWhiteSpace(factionName))
            {
                return null;
            }

            return context.Factions.FirstOrDefault(faction =>
                string.Equals(faction.Name, factionName, StringComparison.OrdinalIgnoreCase))?.Id;
        }

        private Guid? ResolveChapterId(Dictionary<string, object> row, ImportExecutionContext context, params string[] keys)
        {
            var oldChapterId = GetGuid(row, keys);
            if (oldChapterId != Guid.Empty && context.ChapterIdMap.TryGetValue(oldChapterId, out var mappedChapterId))
            {
                return mappedChapterId;
            }

            return null;
        }

        private async Task<List<ImportSectionData>> ParseCsvSectionsAsync(string filePath)
        {
            var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
            var rows = new List<Dictionary<string, object>>();
            if (lines.Length == 0)
            {
                return new List<ImportSectionData>();
            }

            var headers = ParseDelimitedLine(lines[0]);
            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                var values = ParseDelimitedLine(lines[i]);
                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                for (var j = 0; j < headers.Count; j++)
                {
                    row[headers[j]] = j < values.Count ? values[j] : string.Empty;
                }
                rows.Add(row);
            }

            var sectionName = InferSectionName(Path.GetFileNameWithoutExtension(filePath), rows.FirstOrDefault());
            return new List<ImportSectionData> { new(sectionName, rows) };
        }

        private async Task<List<ImportSectionData>> ParseTxtSectionsAsync(string filePath)
        {
            var content = (await File.ReadAllTextAsync(filePath, Encoding.UTF8)).Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                return new List<ImportSectionData>();
            }

            var chapters = ParseChaptersFromText(content);
            if (chapters.Count == 0)
            {
                chapters.Add(new Dictionary<string, object>
                {
                    ["Title"] = Path.GetFileNameWithoutExtension(filePath),
                    ["Content"] = content,
                    ["Order"] = 1,
                    ["WordCount"] = content.Length
                });
            }

            return new List<ImportSectionData> { new("Chapters", chapters) };
        }

        private async Task<List<ImportSectionData>> ParseJsonSectionsAsync(string filePath)
        {
            var text = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var token = JToken.Parse(text);
            var sections = new List<ImportSectionData>();

            if (token is JObject rootObject)
            {
                if (rootObject.TryGetValue("Sections", StringComparison.OrdinalIgnoreCase, out var sectionsToken)
                    && sectionsToken is JObject exportedSections)
                {
                    foreach (var property in exportedSections.Properties())
                    {
                        sections.Add(new ImportSectionData(property.Name, ConvertTokenToRows(property.Value)));
                    }
                    return sections;
                }

                foreach (var property in rootObject.Properties())
                {
                    if (string.Equals(property.Name, "ExportedAt", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var rows = ConvertTokenToRows(property.Value);
                    if (rows.Count > 0)
                    {
                        sections.Add(new ImportSectionData(property.Name, rows));
                    }
                }

                if (sections.Count == 0)
                {
                    sections.Add(new ImportSectionData(
                        InferSectionName(Path.GetFileNameWithoutExtension(filePath), ConvertTokenToRows(rootObject).FirstOrDefault()),
                        ConvertTokenToRows(rootObject)));
                }

                return sections;
            }

            sections.Add(new ImportSectionData(
                InferSectionName(Path.GetFileNameWithoutExtension(filePath), ConvertTokenToRows(token).FirstOrDefault()),
                ConvertTokenToRows(token)));
            return sections;
        }

        private Task<List<ImportSectionData>> ParseXmlSectionsAsync(string filePath)
        {
            var document = XDocument.Load(filePath);
            var root = document.Root;
            var sections = new List<ImportSectionData>();
            if (root == null)
            {
                return Task.FromResult(sections);
            }

            var groupedChildren = root.Elements().GroupBy(element => element.Name.LocalName).ToList();
            if (groupedChildren.Count > 1)
            {
                foreach (var group in groupedChildren)
                {
                    sections.Add(new ImportSectionData(group.Key, group.Select(ConvertElementToRow).ToList()));
                }
            }
            else if (root.Elements().Any())
            {
                var rows = root.Elements().Select(ConvertElementToRow).ToList();
                sections.Add(new ImportSectionData(InferSectionName(root.Name.LocalName, rows.FirstOrDefault()), rows));
            }
            else
            {
                sections.Add(new ImportSectionData(
                    InferSectionName(root.Name.LocalName, null),
                    new List<Dictionary<string, object>> { ConvertElementToRow(root) }));
            }

            return Task.FromResult(sections);
        }

        private async Task<ImportPreviewDto> BuildPreviewFromSectionsAsync(
            string filePath,
            ImportFormat format,
            List<ImportSectionData> sections)
        {
            var fileInfo = new FileInfo(filePath);
            var preview = new ImportPreviewDto
            {
                FileInfo = new FileInfoDto
                {
                    FileName = fileInfo.Name,
                    FilePath = filePath,
                    FileSize = fileInfo.Length,
                    Format = format.ToString(),
                    CreatedTime = fileInfo.CreationTime,
                    ModifiedTime = fileInfo.LastWriteTime,
                    RecordCount = sections.Sum(section => section.Rows.Count)
                }
            };

            preview.DetectedDataTypes.AddRange(sections.Select(section => section.Name));

            var previewRows = sections.SelectMany(section => section.Rows).Take(5).ToList();
            foreach (var row in previewRows)
            {
                preview.PreviewRows.Add(new Dictionary<string, object>(row, StringComparer.OrdinalIgnoreCase));
            }

            var firstRow = sections.SelectMany(section => section.Rows).FirstOrDefault();
            if (firstRow != null)
            {
                foreach (var pair in firstRow)
                {
                    preview.Fields.Add(new FieldInfoDto
                    {
                        Name = pair.Key,
                        Type = DetermineValueType(pair.Value),
                        IsRequired = !string.IsNullOrWhiteSpace(pair.Value?.ToString()),
                        SampleValue = pair.Value?.ToString()
                    });

                    var suggested = GetSuggestedMapping(pair.Key);
                    if (!string.IsNullOrWhiteSpace(suggested))
                    {
                        preview.SuggestedMapping[pair.Key] = suggested;
                    }
                }
            }

            await Task.CompletedTask;
            return preview;
        }

        private void RecordHistory(
            Guid projectId,
            OperationResultDto result,
            string operationType,
            string? notes,
            string projectName)
        {
            lock (_historyLock)
            {
                if (!_historyByProject.TryGetValue(projectId, out var histories))
                {
                    histories = new List<OperationHistoryDto>();
                    _historyByProject[projectId] = histories;
                }

                histories.RemoveAll(item => item.OperationId == result.OperationId);
                histories.Add(ToHistoryDto(projectId, result, operationType, notes, projectName));
            }
        }

        private OperationHistoryDto ToHistoryDto(
            Guid projectId,
            OperationResultDto result,
            string operationType,
            string? notes,
            string projectName)
        {
            return new OperationHistoryDto
            {
                OperationId = result.OperationId,
                ProjectId = projectId,
                ProjectName = projectName,
                OperationType = operationType,
                Format = result.Metadata.TryGetValue("Format", out var format) ? format?.ToString() ?? string.Empty : string.Empty,
                FilePath = result.Metadata.TryGetValue("SourcePath", out var sourcePath)
                    ? sourcePath?.ToString() ?? string.Empty
                    : result.OutputPath,
                FileSize = result.FileSize,
                Status = result.Status,
                StartTime = result.StartTime,
                EndTime = result.EndTime,
                ErrorMessage = result.ErrorMessage,
                Notes = notes
            };
        }

        private static bool TryGetProjectId(OperationResultDto result, out Guid projectId)
        {
            projectId = Guid.Empty;
            if (!result.Metadata.TryGetValue("ProjectId", out var value) || value == null)
            {
                return false;
            }

            return Guid.TryParse(value.ToString(), out projectId);
        }

        private void OnProgressUpdated(OperationResultDto result)
        {
            ProgressUpdated?.Invoke(this, result);
        }

        private static ImportSectionType DetermineSectionType(string? sectionName, Dictionary<string, object>? sampleRow)
        {
            var normalizedSectionName = (sectionName ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
            if (normalizedSectionName.Contains("chapter") || normalizedSectionName.Contains("章节"))
            {
                return ImportSectionType.Chapters;
            }
            if (normalizedSectionName.Contains("volume") || normalizedSectionName.Contains("卷宗") || normalizedSectionName == "volumes")
            {
                return ImportSectionType.Volumes;
            }
            if (normalizedSectionName.Contains("character") || normalizedSectionName.Contains("角色") || normalizedSectionName.Contains("人物"))
            {
                return ImportSectionType.Characters;
            }
            if (normalizedSectionName.Contains("faction") || normalizedSectionName.Contains("势力"))
            {
                return ImportSectionType.Factions;
            }
            if (normalizedSectionName.Contains("plot") || normalizedSectionName.Contains("剧情"))
            {
                return ImportSectionType.Plots;
            }
            if (normalizedSectionName.Contains("setting") || normalizedSectionName.Contains("设定"))
            {
                return ImportSectionType.WorldSettings;
            }

            if (sampleRow == null)
            {
                return ImportSectionType.Unknown;
            }

            var keys = sampleRow.Keys.Select(key => key.ToLowerInvariant()).ToList();
            if (keys.Contains("content") || keys.Contains("章节内容") || keys.Contains("正文"))
            {
                return ImportSectionType.Chapters;
            }
            if (keys.Contains("projectid") && keys.Contains("title"))
            {
                return ImportSectionType.Volumes;
            }
            if (keys.Contains("cultivationlevel") || keys.Contains("姓名") || keys.Contains("name"))
            {
                return ImportSectionType.Characters;
            }
            if (keys.Contains("membercount") || keys.Contains("势力名称"))
            {
                return ImportSectionType.Factions;
            }
            if (keys.Contains("outline") || keys.Contains("剧情标题"))
            {
                return ImportSectionType.Plots;
            }
            if (keys.Contains("rules") || keys.Contains("relatedsettings"))
            {
                return ImportSectionType.WorldSettings;
            }

            return ImportSectionType.Unknown;
        }

        private static int GetSectionPriority(ImportSectionType sectionType)
        {
            return sectionType switch
            {
                ImportSectionType.Volumes => 1,
                ImportSectionType.Factions => 2,
                ImportSectionType.WorldSettings => 3,
                ImportSectionType.Chapters => 4,
                ImportSectionType.Characters => 5,
                ImportSectionType.Plots => 6,
                _ => 99
            };
        }

        private static string InferSectionName(string fallback, Dictionary<string, object>? sampleRow)
        {
            return DetermineSectionType(fallback, sampleRow) switch
            {
                ImportSectionType.Volumes => "Volumes",
                ImportSectionType.Chapters => "Chapters",
                ImportSectionType.Characters => "Characters",
                ImportSectionType.Factions => "Factions",
                ImportSectionType.Plots => "Plots",
                ImportSectionType.WorldSettings => "WorldSettings",
                _ => string.IsNullOrWhiteSpace(fallback) ? "ImportedData" : fallback
            };
        }

        private static List<Dictionary<string, object>> ParseChaptersFromText(string text)
        {
            var chapters = new List<Dictionary<string, object>>();
            var matches = Regex.Matches(text, @"第[一二三四五六七八九十百千\d]+章[^\r\n]*");
            if (matches.Count == 0)
            {
                return chapters;
            }

            for (var i = 0; i < matches.Count; i++)
            {
                var start = matches[i].Index + matches[i].Length;
                var end = i < matches.Count - 1 ? matches[i + 1].Index : text.Length;
                var content = text[start..end].Trim();
                chapters.Add(new Dictionary<string, object>
                {
                    ["Title"] = matches[i].Value.Trim(),
                    ["Content"] = content,
                    ["Order"] = i + 1,
                    ["WordCount"] = content.Length
                });
            }

            return chapters;
        }

        private static List<string> ParseDelimitedLine(string line)
        {
            var result = new List<string>();
            var builder = new StringBuilder();
            var inQuotes = false;

            foreach (var ch in line)
            {
                switch (ch)
                {
                    case '"':
                        inQuotes = !inQuotes;
                        break;
                    case ',' when !inQuotes:
                        result.Add(builder.ToString().Trim());
                        builder.Clear();
                        break;
                    default:
                        builder.Append(ch);
                        break;
                }
            }

            result.Add(builder.ToString().Trim());
            return result;
        }

        private static List<Dictionary<string, object>> ConvertTokenToRows(JToken token)
        {
            if (token is JArray array)
            {
                return array.Select(ConvertJTokenToRow).ToList();
            }

            return new List<Dictionary<string, object>> { ConvertJTokenToRow(token) };
        }

        private static Dictionary<string, object> ConvertJTokenToRow(JToken token)
        {
            if (token is JObject obj)
            {
                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in obj.Properties())
                {
                    row[property.Name] = ConvertJValue(property.Value);
                }
                return row;
            }

            return new Dictionary<string, object> { ["Value"] = ConvertJValue(token) };
        }

        private static object ConvertJValue(JToken token)
        {
            return token.Type switch
            {
                JTokenType.Null => string.Empty,
                JTokenType.Integer => token.Value<long>(),
                JTokenType.Float => token.Value<double>(),
                JTokenType.Boolean => token.Value<bool>(),
                JTokenType.Date => token.Value<DateTime>(),
                JTokenType.Guid => token.Value<Guid>(),
                JTokenType.Array or JTokenType.Object => token.ToString(Formatting.None),
                _ => token.ToString()
            };
        }

        private static Dictionary<string, object> ConvertElementToRow(XElement element)
        {
            var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (!element.Elements().Any())
            {
                row[element.Name.LocalName] = element.Value;
                return row;
            }

            foreach (var child in element.Elements())
            {
                if (child.HasElements)
                {
                    row[child.Name.LocalName] = child.ToString(SaveOptions.DisableFormatting);
                    continue;
                }

                row[child.Name.LocalName] = child.Value;
            }

            return row;
        }

        private static string DetermineValueType(object? value)
        {
            return value switch
            {
                null => "String",
                int or long => "Integer",
                decimal or double or float => "Double",
                bool => "Boolean",
                DateTime => "DateTime",
                Guid => "Guid",
                _ => "String"
            };
        }

        private static string? GetSuggestedMapping(string key)
        {
            var normalized = key.Replace(" ", string.Empty).ToLowerInvariant();
            return normalized switch
            {
                "title" or "章节标题" or "卷宗标题" or "剧情标题" => "Title",
                "name" or "姓名" or "名称" or "设定名称" or "势力名称" => "Name",
                "content" or "内容" or "正文" => "Content",
                "summary" or "摘要" => "Summary",
                "type" or "类型" or "角色类型" => "Type",
                "status" or "状态" => "Status",
                "description" or "描述" => "Description",
                "order" or "序号" => "Order",
                "wordcount" or "字数" => "WordCount",
                "faction" or "势力" => "Faction",
                _ => null
            };
        }

        private static string? GetString(Dictionary<string, object> row, params string[] keys)
        {
            foreach (var key in keys)
            {
                var actualKey = row.Keys.FirstOrDefault(existing =>
                    string.Equals(existing, key, StringComparison.OrdinalIgnoreCase));
                if (actualKey == null)
                {
                    continue;
                }

                var text = row[actualKey]?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }

            return null;
        }

        private static int? GetInt(Dictionary<string, object> row, params string[] keys)
        {
            var text = GetString(row, keys);
            return int.TryParse(text, out var value) ? value : null;
        }

        private static decimal? GetDecimal(Dictionary<string, object> row, params string[] keys)
        {
            var text = GetString(row, keys);
            return decimal.TryParse(text, out var value) ? value : null;
        }

        private static bool? GetBool(Dictionary<string, object> row, params string[] keys)
        {
            var text = GetString(row, keys);
            return bool.TryParse(text, out var value) ? value : null;
        }

        private static Guid GetGuid(Dictionary<string, object> row, params string[] keys)
        {
            var text = GetString(row, keys);
            return Guid.TryParse(text, out var value) ? value : Guid.Empty;
        }

        private static Dictionary<string, object?> ConvertEntityToDictionary(object entity)
        {
            return entity.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead && IsScalarType(property.PropertyType))
                .ToDictionary(property => property.Name, property => FormatValue(property.GetValue(entity)));
        }

        private static bool IsScalarType(Type type)
        {
            var actualType = Nullable.GetUnderlyingType(type) ?? type;
            return actualType.IsPrimitive
                || actualType.IsEnum
                || actualType == typeof(string)
                || actualType == typeof(decimal)
                || actualType == typeof(Guid)
                || actualType == typeof(DateTime)
                || actualType == typeof(DateTimeOffset)
                || actualType == typeof(TimeSpan);
        }

        private static object? FormatValue(object? value)
        {
            return value switch
            {
                DateTime dateTime => dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                _ => value
            };
        }

        private static string BuildImportNote(List<ImportSectionData> sections)
        {
            return string.Join("，", sections.Select(section => $"{section.Name}:{section.Rows.Count}"));
        }

        private static string SanitizeFileName(string fileName)
        {
            return string.Concat(fileName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        }
    }

    internal sealed class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    internal sealed class ImportSectionData
    {
        public ImportSectionData(string name, List<Dictionary<string, object>> rows)
        {
            Name = name;
            Rows = rows;
        }

        public string Name { get; }
        public List<Dictionary<string, object>> Rows { get; }
    }

    internal sealed class ImportExecutionContext
    {
        public required Project Project { get; init; }
        public List<Volume> Volumes { get; init; } = new();
        public List<Chapter> Chapters { get; init; } = new();
        public List<Character> Characters { get; init; } = new();
        public List<Faction> Factions { get; init; } = new();
        public List<Plot> Plots { get; init; } = new();
        public List<WorldSetting> WorldSettings { get; init; } = new();
        public Dictionary<Guid, Guid> VolumeIdMap { get; } = new();
        public Dictionary<Guid, Guid> ChapterIdMap { get; } = new();
        public Dictionary<Guid, Guid> FactionIdMap { get; } = new();
        public Guid DefaultVolumeId { get; set; }
    }

    internal enum ImportSectionType
    {
        Unknown,
        Volumes,
        Chapters,
        Characters,
        Factions,
        Plots,
        WorldSettings
    }
}
