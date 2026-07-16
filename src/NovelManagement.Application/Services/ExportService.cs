using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NovelManagement.Application.DTOs;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;
using System.Reflection;
using System.Text;
using System.Net;

namespace NovelManagement.Application.Services
{
    /// <summary>
    /// 导出服务
    /// </summary>
    public class ExportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ExportService> _logger;
        private readonly ExcelProcessingService _excelService;
        private readonly WordProcessingService _wordService;
        private readonly Dictionary<Guid, OperationResultDto> _activeOperations;
        private readonly Dictionary<Guid, List<OperationHistoryDto>> _historyByProject = new();
        private readonly object _historyLock = new();

        /// <summary>
        /// 导出进度更新事件
        /// </summary>
        public event EventHandler<OperationResultDto>? ProgressUpdated;

        public ExportService(
            IUnitOfWork unitOfWork,
            ILogger<ExportService> logger,
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
        /// 开始导出操作
        /// </summary>
        public Task<OperationResultDto> StartExportAsync(ExportRequestDto request)
        {
            var operationId = Guid.NewGuid();
            var result = new OperationResultDto
            {
                OperationId = operationId,
                Status = OperationStatus.Pending,
                StartTime = DateTime.Now,
                CurrentStep = "准备导出..."
            };
            result.Metadata["ProjectId"] = request.ProjectId;
            result.Metadata["Format"] = request.Format.ToString();
            _activeOperations[operationId] = result;

            try
            {
                _logger.LogInformation("开始导出操作: {OperationId}, 格式: {Format}, 范围: {Scope}", operationId, request.Format, request.Scope);
                _ = Task.Run(() => ExecuteExportAsync(request, result));
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动导出操作失败: {OperationId}", operationId);
                result.Status = OperationStatus.Failed;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.Now;
                RecordHistory(request.ProjectId, result, "Export", ex.Message);
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
        /// 取消导出操作
        /// </summary>
        public bool CancelExport(Guid operationId)
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
                    RecordHistory(projectId, result, "Export", "用户取消导出");
                }
                _logger.LogInformation("导出操作已取消: {OperationId}", operationId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取导出历史记录
        /// </summary>
        public Task<List<OperationHistoryDto>> GetExportHistoryAsync(Guid projectId)
        {
            lock (_historyLock)
            {
                _historyByProject.TryGetValue(projectId, out var histories);
                histories ??= new List<OperationHistoryDto>();
                var active = _activeOperations.Values
                    .Where(result => TryGetProjectId(result, out var activeProjectId) && activeProjectId == projectId)
                    .Where(result => result.Status == OperationStatus.Pending || result.Status == OperationStatus.InProgress)
                    .Select(result => ToHistoryDto(projectId, result, "Export", result.CurrentStep))
                    .ToList();

                return Task.FromResult(histories
                    .Concat(active)
                    .OrderByDescending(item => item.StartTime)
                    .ToList());
            }
        }

        private async Task ExecuteExportAsync(ExportRequestDto request, OperationResultDto result)
        {
            try
            {
                result.Status = OperationStatus.InProgress;
                result.CurrentStep = "正在收集数据...";
                result.Progress = 10;
                OnProgressUpdated(result);

                var exportData = await CollectExportDataAsync(request);
                result.TotalItems = exportData.Sum(pair => pair.Value.Count);
                result.CurrentStep = "正在生成文件...";
                result.Progress = 35;
                OnProgressUpdated(result);

                var outputPath = await GenerateFileAsync(request, exportData, result);
                result.OutputPath = outputPath;

                if (File.Exists(outputPath))
                {
                    result.FileSize = new FileInfo(outputPath).Length;
                }

                result.Status = OperationStatus.Completed;
                result.EndTime = DateTime.Now;
                result.Progress = 100;
                result.CurrentStep = "导出完成";
                result.IsSuccess = true;
                OnProgressUpdated(result);
                RecordHistory(request.ProjectId, result, "Export", BuildExportNote(request, exportData));
                _logger.LogInformation("导出操作完成: {OperationId}, 输出文件: {OutputPath}", result.OperationId, outputPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出操作失败: {OperationId}", result.OperationId);
                result.Status = OperationStatus.Failed;
                result.EndTime = DateTime.Now;
                result.ErrorMessage = ex.Message;
                result.IsSuccess = false;
                result.CurrentStep = "导出失败";
                OnProgressUpdated(result);
                RecordHistory(request.ProjectId, result, "Export", ex.Message);
            }
            finally
            {
                _activeOperations[result.OperationId] = result;
            }
        }

        private async Task<Dictionary<string, List<object>>> CollectExportDataAsync(ExportRequestDto request)
        {
            var data = new Dictionary<string, List<object>>(StringComparer.OrdinalIgnoreCase);
            var project = await _unitOfWork.Projects.GetByIdAsync(request.ProjectId);
            if (project == null)
            {
                throw new InvalidOperationException("未找到当前项目，无法执行导出。");
            }

            data["Project"] = new List<object> { project };

            var allVolumes = (await _unitOfWork.Volumes.GetByProjectIdAsync(request.ProjectId)).OrderBy(v => v.Order).ToList();
            var allChapters = (await _unitOfWork.Chapters.GetByProjectIdAsync(request.ProjectId)).OrderBy(c => c.Volume?.Order).ThenBy(c => c.Order).ToList();

            switch (request.Scope)
            {
                case ExportScope.EntireProject:
                    data["Volumes"] = allVolumes.Cast<object>().ToList();
                    data["Chapters"] = allChapters.Cast<object>().ToList();
                    break;
                case ExportScope.SelectedVolumes:
                    var selectedVolumes = allVolumes.Where(volume => request.SelectedVolumeIds.Contains(volume.Id)).ToList();
                    data["Volumes"] = selectedVolumes.Cast<object>().ToList();
                    data["Chapters"] = allChapters.Where(chapter => request.SelectedVolumeIds.Contains(chapter.VolumeId)).Cast<object>().ToList();
                    break;
                case ExportScope.SelectedChapters:
                    var selectedChapters = allChapters.Where(chapter => request.SelectedChapterIds.Contains(chapter.Id)).ToList();
                    data["Chapters"] = selectedChapters.Cast<object>().ToList();
                    var selectedVolumeIds = selectedChapters.Select(chapter => chapter.VolumeId).Distinct().ToHashSet();
                    data["Volumes"] = allVolumes.Where(volume => selectedVolumeIds.Contains(volume.Id)).Cast<object>().ToList();
                    break;
                default:
                    throw new NotSupportedException($"暂不支持的导出范围: {request.Scope}");
            }

            if (request.IncludeCharacters)
            {
                data["Characters"] = (await _unitOfWork.Characters.GetByProjectIdAsync(request.ProjectId)).OrderBy(c => c.Name).Cast<object>().ToList();
            }
            if (request.IncludeFactions)
            {
                data["Factions"] = (await _unitOfWork.Factions.GetByProjectIdAsync(request.ProjectId)).OrderBy(f => f.Name).Cast<object>().ToList();
            }
            if (request.IncludePlots)
            {
                data["Plots"] = (await _unitOfWork.Plots.GetByProjectIdAsync(request.ProjectId)).OrderBy(p => p.Title).Cast<object>().ToList();
            }
            if (request.IncludeSettings)
            {
                data["WorldSettings"] = (await _unitOfWork.WorldSettings.GetByProjectIdAsync(request.ProjectId)).OrderBy(s => s.Order).ThenBy(s => s.Name).Cast<object>().ToList();
            }

            return data;
        }

        private async Task<string> GenerateFileAsync(ExportRequestDto request, Dictionary<string, List<object>> data, OperationResultDto result)
        {
            var outputPath = request.OutputPath;
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            switch (request.Format)
            {
                case ExportFormat.TXT:
                    await GenerateTxtFileAsync(outputPath, data, result);
                    break;
                case ExportFormat.JSON:
                    await GenerateJsonFileAsync(outputPath, data, result);
                    break;
                case ExportFormat.HTML:
                    await GenerateHtmlFileAsync(outputPath, data, result);
                    break;
                case ExportFormat.MARKDOWN:
                    await GenerateMarkdownFileAsync(outputPath, data, result);
                    break;
                case ExportFormat.EXCEL:
                    await _excelService.ExportToExcelAsync(outputPath, data, result);
                    break;
                case ExportFormat.DOCX:
                    await _wordService.ExportToWordAsync(outputPath, data, result);
                    break;
                case ExportFormat.PDF:
                case ExportFormat.EPUB:
                    throw new NotSupportedException($"当前版本暂不支持 {request.Format} 真正导出，请先使用 TXT、JSON、HTML、MARKDOWN、EXCEL 或 DOCX。");
                default:
                    throw new NotSupportedException($"不支持的导出格式: {request.Format}");
            }

            return outputPath;
        }

        private async Task GenerateTxtFileAsync(string outputPath, Dictionary<string, List<object>> data, OperationResultDto result)
        {
            var builder = new StringBuilder();
            AppendHeader(builder, data);
            AppendSections(builder, data, result, false);
            await File.WriteAllTextAsync(outputPath, builder.ToString(), Encoding.UTF8);
        }

        private async Task GenerateJsonFileAsync(string outputPath, Dictionary<string, List<object>> data, OperationResultDto result)
        {
            var exportObject = new Dictionary<string, object?>
            {
                ["ExportedAt"] = DateTime.Now,
                ["Sections"] = data.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.Select(ConvertToSerializableObject).ToList())
            };
            var json = JsonConvert.SerializeObject(exportObject, Formatting.Indented);
            await File.WriteAllTextAsync(outputPath, json, Encoding.UTF8);
            result.ProcessedItems = data.Count;
            result.Progress = 95;
            OnProgressUpdated(result);
        }

        private async Task GenerateHtmlFileAsync(string outputPath, Dictionary<string, List<object>> data, OperationResultDto result)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<!DOCTYPE html>");
            builder.AppendLine("<html><head><meta charset=\"utf-8\"/><title>项目导出</title></head><body>");
            builder.AppendLine("<h1>项目导出</h1>");
            builder.AppendLine($"<p>导出时间：{WebUtility.HtmlEncode(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}</p>");
            var sections = data.ToList();
            for (var i = 0; i < sections.Count; i++)
            {
                builder.AppendLine($"<h2>{WebUtility.HtmlEncode(sections[i].Key)}</h2>");
                if (sections[i].Value.Count == 0)
                {
                    builder.AppendLine("<p>暂无数据</p>");
                }
                else
                {
                    foreach (var item in sections[i].Value)
                    {
                        builder.AppendLine("<ul>");
                        foreach (var line in DescribeItem(item))
                        {
                            builder.AppendLine($"<li>{WebUtility.HtmlEncode(line)}</li>");
                        }
                        builder.AppendLine("</ul>");
                    }
                }

                result.ProcessedItems = i + 1;
                result.Progress = 20 + (int)((double)(i + 1) / Math.Max(sections.Count, 1) * 70);
                OnProgressUpdated(result);
            }
            builder.AppendLine("</body></html>");
            await File.WriteAllTextAsync(outputPath, builder.ToString(), Encoding.UTF8);
        }

        private async Task GenerateMarkdownFileAsync(string outputPath, Dictionary<string, List<object>> data, OperationResultDto result)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# 项目导出");
            builder.AppendLine();
            builder.AppendLine($"导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();
            AppendSections(builder, data, result, true);
            await File.WriteAllTextAsync(outputPath, builder.ToString(), Encoding.UTF8);
        }

        private static void AppendHeader(StringBuilder builder, Dictionary<string, List<object>> data)
        {
            var project = data.TryGetValue("Project", out var projects) ? projects.OfType<Project>().FirstOrDefault() : null;
            builder.AppendLine(project?.Name ?? "未命名项目");
            builder.AppendLine(new string('=', 24));
            builder.AppendLine($"导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();
        }

        private void AppendSections(StringBuilder builder, Dictionary<string, List<object>> data, OperationResultDto result, bool markdown)
        {
            var sections = data.ToList();
            for (var i = 0; i < sections.Count; i++)
            {
                builder.AppendLine(markdown ? $"## {sections[i].Key}" : $"=== {sections[i].Key} ===");
                if (sections[i].Value.Count == 0)
                {
                    builder.AppendLine("暂无数据");
                    builder.AppendLine();
                    continue;
                }

                foreach (var item in sections[i].Value)
                {
                    foreach (var line in DescribeItem(item))
                    {
                        builder.AppendLine(markdown ? $"- {line}" : line);
                    }
                    builder.AppendLine();
                }

                result.ProcessedItems = i + 1;
                result.Progress = 20 + (int)((double)(i + 1) / Math.Max(sections.Count, 1) * 70);
                OnProgressUpdated(result);
            }
        }

        private static IEnumerable<string> DescribeItem(object item)
        {
            if (item is IDictionary<string, object> dictionary)
            {
                foreach (var pair in dictionary)
                {
                    yield return $"{pair.Key}: {pair.Value}";
                }
                yield break;
            }

            foreach (var property in item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead || !IsScalarType(property.PropertyType))
                {
                    continue;
                }

                var value = property.GetValue(item);
                if (value == null)
                {
                    continue;
                }

                yield return $"{property.Name}: {FormatValue(value)}";
            }
        }

        private static object ConvertToSerializableObject(object item)
        {
            if (item is IDictionary<string, object> dictionary)
            {
                return dictionary;
            }

            return item.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead && IsScalarType(property.PropertyType))
                .ToDictionary(property => property.Name, property => FormatValue(property.GetValue(item)));
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
                null => null,
                DateTime dateTime => dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                _ => value
            };
        }

        private string BuildExportNote(ExportRequestDto request, Dictionary<string, List<object>> data)
        {
            var sectionSummary = string.Join("，", data.Select(pair => $"{pair.Key}:{pair.Value.Count}"));
            return $"格式={request.Format}; 范围={request.Scope}; {sectionSummary}";
        }

        private void RecordHistory(Guid projectId, OperationResultDto result, string operationType, string? notes)
        {
            lock (_historyLock)
            {
                if (!_historyByProject.TryGetValue(projectId, out var histories))
                {
                    histories = new List<OperationHistoryDto>();
                    _historyByProject[projectId] = histories;
                }

                histories.RemoveAll(item => item.OperationId == result.OperationId);
                histories.Add(ToHistoryDto(projectId, result, operationType, notes));
            }
        }

        private OperationHistoryDto ToHistoryDto(Guid projectId, OperationResultDto result, string operationType, string? notes)
        {
            return new OperationHistoryDto
            {
                OperationId = result.OperationId,
                ProjectId = projectId,
                ProjectName = string.Empty,
                OperationType = operationType,
                Format = result.Metadata.TryGetValue("Format", out var format) ? format?.ToString() ?? string.Empty : string.Empty,
                FilePath = result.OutputPath,
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
    }
}
