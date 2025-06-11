using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.DTOs;
using NovelManagement.Core.Interfaces;

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

        /// <summary>
        /// 导出进度更新事件
        /// </summary>
        public event EventHandler<OperationResultDto>? ProgressUpdated;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="unitOfWork">工作单元</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="excelService">Excel处理服务</param>
        /// <param name="wordService">Word处理服务</param>
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
        /// <param name="request">导出请求</param>
        /// <returns>操作结果</returns>
        public async Task<OperationResultDto> StartExportAsync(ExportRequestDto request)
        {
            var operationId = Guid.NewGuid();
            var result = new OperationResultDto
            {
                OperationId = operationId,
                Status = OperationStatus.Pending,
                StartTime = DateTime.Now,
                CurrentStep = "准备导出..."
            };

            _activeOperations[operationId] = result;

            try
            {
                _logger.LogInformation($"开始导出操作: {operationId}, 格式: {request.Format}, 范围: {request.Scope}");

                // 异步执行导出，使用Task.Factory.StartNew避免DbContext并发问题
                _ = Task.Factory.StartNew(async () => await ExecuteExportAsync(request, result), TaskCreationOptions.LongRunning).Unwrap();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"启动导出操作失败: {operationId}");
                result.Status = OperationStatus.Failed;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.Now;
                return result;
            }
        }

        /// <summary>
        /// 获取操作状态
        /// </summary>
        /// <param name="operationId">操作ID</param>
        /// <returns>操作结果</returns>
        public OperationResultDto? GetOperationStatus(Guid operationId)
        {
            return _activeOperations.TryGetValue(operationId, out var result) ? result : null;
        }

        /// <summary>
        /// 取消导出操作
        /// </summary>
        /// <param name="operationId">操作ID</param>
        /// <returns>是否成功</returns>
        public bool CancelExport(Guid operationId)
        {
            if (_activeOperations.TryGetValue(operationId, out var result))
            {
                if (result.Status == OperationStatus.InProgress || result.Status == OperationStatus.Pending)
                {
                    result.Status = OperationStatus.Cancelled;
                    result.EndTime = DateTime.Now;
                    result.CurrentStep = "操作已取消";
                    
                    OnProgressUpdated(result);
                    
                    _logger.LogInformation($"导出操作已取消: {operationId}");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取导出历史记录
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns>历史记录列表</returns>
        public async Task<List<OperationHistoryDto>> GetExportHistoryAsync(Guid projectId)
        {
            try
            {
                // 这里应该从数据库获取历史记录，现在返回模拟数据
                var history = new List<OperationHistoryDto>
                {
                    new OperationHistoryDto
                    {
                        OperationId = Guid.NewGuid(),
                        ProjectId = projectId,
                        ProjectName = "千面劫·宿命轮回",
                        OperationType = "Export",
                        Format = "DOCX",
                        FilePath = @"C:\Exports\千面劫·宿命轮回.docx",
                        FileSize = 2048576,
                        Status = OperationStatus.Completed,
                        StartTime = DateTime.Now.AddDays(-1),
                        EndTime = DateTime.Now.AddDays(-1).AddMinutes(5),
                        Notes = "完整项目导出"
                    },
                    new OperationHistoryDto
                    {
                        OperationId = Guid.NewGuid(),
                        ProjectId = projectId,
                        ProjectName = "千面劫·宿命轮回",
                        OperationType = "Export",
                        Format = "PDF",
                        FilePath = @"C:\Exports\千面劫·宿命轮回.pdf",
                        FileSize = 1536000,
                        Status = OperationStatus.Completed,
                        StartTime = DateTime.Now.AddDays(-3),
                        EndTime = DateTime.Now.AddDays(-3).AddMinutes(8),
                        Notes = "PDF格式导出"
                    }
                };

                return await Task.FromResult(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取导出历史记录失败: {projectId}");
                return new List<OperationHistoryDto>();
            }
        }

        /// <summary>
        /// 执行导出操作
        /// </summary>
        /// <param name="request">导出请求</param>
        /// <param name="result">操作结果</param>
        private async Task ExecuteExportAsync(ExportRequestDto request, OperationResultDto result)
        {
            try
            {
                result.Status = OperationStatus.InProgress;
                result.CurrentStep = "正在收集数据...";
                result.Progress = 10;
                OnProgressUpdated(result);

                // 模拟数据收集
                await Task.Delay(1000);

                if (result.Status == OperationStatus.Cancelled) return;

                // 根据导出范围获取数据
                var exportData = await CollectExportDataAsync(request);
                result.TotalItems = exportData.Count;
                result.CurrentStep = "正在生成文件...";
                result.Progress = 30;
                OnProgressUpdated(result);

                await Task.Delay(1000);

                if (result.Status == OperationStatus.Cancelled) return;

                // 根据格式生成文件
                var outputPath = await GenerateFileAsync(request, exportData, result);
                
                result.OutputPath = outputPath;
                result.CurrentStep = "正在完成导出...";
                result.Progress = 90;
                OnProgressUpdated(result);

                await Task.Delay(500);

                if (result.Status == OperationStatus.Cancelled) return;

                // 获取文件信息
                if (File.Exists(outputPath))
                {
                    var fileInfo = new FileInfo(outputPath);
                    result.FileSize = fileInfo.Length;
                }

                result.Status = OperationStatus.Completed;
                result.EndTime = DateTime.Now;
                result.Progress = 100;
                result.CurrentStep = "导出完成";
                result.IsSuccess = true;

                OnProgressUpdated(result);

                _logger.LogInformation($"导出操作完成: {result.OperationId}, 输出文件: {outputPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"导出操作失败: {result.OperationId}");
                
                result.Status = OperationStatus.Failed;
                result.EndTime = DateTime.Now;
                result.ErrorMessage = ex.Message;
                result.IsSuccess = false;
                result.CurrentStep = "导出失败";

                OnProgressUpdated(result);
            }
        }

        /// <summary>
        /// 收集导出数据
        /// </summary>
        /// <param name="request">导出请求</param>
        /// <returns>导出数据</returns>
        private async Task<Dictionary<string, List<object>>> CollectExportDataAsync(ExportRequestDto request)
        {
            var data = new Dictionary<string, List<object>>();

            try
            {
                // 根据导出范围收集数据
                switch (request.Scope)
                {
                    case ExportScope.EntireProject:
                        // 获取整个项目的数据
                        data["Project"] = await GetProjectDataAsync(request.ProjectId);
                        data["Volumes"] = await GetAllVolumesDataAsync(request.ProjectId);
                        data["Chapters"] = await GetAllChaptersDataAsync(request.ProjectId);
                        break;

                    case ExportScope.SelectedVolumes:
                        // 获取选中卷宗的数据
                        var volumeData = new List<object>();
                        foreach (var volumeId in request.SelectedVolumeIds)
                        {
                            volumeData.AddRange(await GetVolumeDataAsync(volumeId));
                        }
                        data["Volumes"] = volumeData;
                        break;

                    case ExportScope.SelectedChapters:
                        // 获取选中章节的数据
                        var chapterData = new List<object>();
                        foreach (var chapterId in request.SelectedChapterIds)
                        {
                            chapterData.AddRange(await GetChapterDataAsync(chapterId));
                        }
                        data["Chapters"] = chapterData;
                        break;
                }

                // 根据配置添加额外数据
                if (request.IncludeCharacters)
                {
                    data["Characters"] = await GetCharacterDataAsync(request.ProjectId);
                }

                if (request.IncludeFactions)
                {
                    data["Factions"] = await GetFactionDataAsync(request.ProjectId);
                }

                if (request.IncludePlots)
                {
                    data["Plots"] = await GetPlotDataAsync(request.ProjectId);
                }

                if (request.IncludeSettings)
                {
                    data["WorldSettings"] = await GetSettingDataAsync(request.ProjectId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "收集导出数据失败");
                throw;
            }

            return data;
        }

        /// <summary>
        /// 生成导出文件
        /// </summary>
        /// <param name="request">导出请求</param>
        /// <param name="data">导出数据</param>
        /// <param name="result">操作结果</param>
        /// <returns>输出文件路径</returns>
        private async Task<string> GenerateFileAsync(ExportRequestDto request, Dictionary<string, List<object>> data, OperationResultDto result)
        {
            var outputPath = request.OutputPath;

            // 确保输出目录存在
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            switch (request.Format)
            {
                case ExportFormat.TXT:
                    await GenerateTxtFileAsync(outputPath, data, result);
                    break;

                case ExportFormat.EXCEL:
                    await _excelService.ExportToExcelAsync(outputPath, data, result);
                    break;

                case ExportFormat.DOCX:
                    await _wordService.ExportToWordAsync(outputPath, data, result);
                    break;

                case ExportFormat.JSON:
                    await GenerateJsonFileAsync(outputPath, data, result);
                    break;

                case ExportFormat.PDF:
                    await GeneratePdfFileAsync(outputPath, data, result);
                    break;

                case ExportFormat.EPUB:
                    await GenerateEpubFileAsync(outputPath, data, result);
                    break;

                case ExportFormat.HTML:
                    await GenerateHtmlFileAsync(outputPath, data, result);
                    break;

                case ExportFormat.MARKDOWN:
                    await GenerateMarkdownFileAsync(outputPath, data, result);
                    break;

                default:
                    throw new NotSupportedException($"不支持的导出格式: {request.Format}");
            }

            return outputPath;
        }

        /// <summary>
        /// 生成TXT文件
        /// </summary>
        private async Task GenerateTxtFileAsync(string outputPath, Dictionary<string, List<object>> data, OperationResultDto result)
        {
            var content = new StringBuilder();
            content.AppendLine("千面劫·宿命轮回");
            content.AppendLine("==================");
            content.AppendLine();
            content.AppendLine($"导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            content.AppendLine();

            // 导出各类数据
            foreach (var kvp in data)
            {
                content.AppendLine($"=== {kvp.Key} ===");
                content.AppendLine($"数量：{kvp.Value.Count}");
                content.AppendLine();

                // 更新进度
                result.ProcessedItems++;
                result.Progress = 30 + (int)((double)result.ProcessedItems / data.Count * 60);
                OnProgressUpdated(result);

                await Task.Delay(100); // 模拟处理时间
            }

            await File.WriteAllTextAsync(outputPath, content.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// 生成JSON文件
        /// </summary>
        private async Task GenerateJsonFileAsync(string outputPath, Dictionary<string, List<object>> data, OperationResultDto result)
        {
            try
            {
                var exportData = new
                {
                    ProjectName = "千面劫·宿命轮回",
                    ExportTime = DateTime.Now,
                    Data = data
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(exportData, Newtonsoft.Json.Formatting.Indented);
                await File.WriteAllTextAsync(outputPath, json, Encoding.UTF8);

                result.ProcessedItems = data.Count;
                result.Progress = 90;
                OnProgressUpdated(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"生成JSON文件失败: {outputPath}");
                throw;
            }
        }

        /// <summary>
        /// 生成DOCX文件
        /// </summary>
        private async Task GenerateDocxFileAsync(string outputPath, Dictionary<string, List<object>> data, OperationResultDto result)
        {
            // 这里应该使用DocumentFormat.OpenXml或其他库生成DOCX文件
            // 现在创建一个简单的文本文件作为示例
            await GenerateTxtFileAsync(outputPath.Replace(".docx", ".txt"), data, result);
            
            // 模拟DOCX生成
            await Task.Delay(2000);
        }

        /// <summary>
        /// 生成PDF文件
        /// </summary>
        private async Task GeneratePdfFileAsync(string outputPath, Dictionary<string, List<object>> data, OperationResultDto result)
        {
            // 这里应该使用iTextSharp或其他库生成PDF文件
            await GenerateTxtFileAsync(outputPath.Replace(".pdf", ".txt"), data, result);
            
            // 模拟PDF生成
            await Task.Delay(3000);
        }

        /// <summary>
        /// 生成EPUB文件
        /// </summary>
        private async Task GenerateEpubFileAsync(string outputPath, Dictionary<string, List<object>> data, OperationResultDto result)
        {
            // 这里应该使用EpubSharp或其他库生成EPUB文件
            await GenerateTxtFileAsync(outputPath.Replace(".epub", ".txt"), data, result);
            
            // 模拟EPUB生成
            await Task.Delay(2500);
        }

        /// <summary>
        /// 生成HTML文件
        /// </summary>
        private async Task GenerateHtmlFileAsync(string outputPath, Dictionary<string, List<object>> data, OperationResultDto result)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head><title>千面劫·宿命轮回</title></head><body>");
            html.AppendLine("<h1>千面劫·宿命轮回</h1>");

            foreach (var kvp in data)
            {
                html.AppendLine($"<h2>{kvp.Key}</h2>");
                html.AppendLine($"<p>数量：{kvp.Value.Count}</p>");

                result.ProcessedItems++;
                result.Progress = 30 + (int)((double)result.ProcessedItems / data.Count * 60);
                OnProgressUpdated(result);

                await Task.Delay(100);
            }

            html.AppendLine("</body></html>");
            await File.WriteAllTextAsync(outputPath, html.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// 生成Markdown文件
        /// </summary>
        private async Task GenerateMarkdownFileAsync(string outputPath, Dictionary<string, List<object>> data, OperationResultDto result)
        {
            var markdown = new StringBuilder();
            markdown.AppendLine("# 千面劫·宿命轮回");
            markdown.AppendLine();
            markdown.AppendLine($"导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            markdown.AppendLine();

            foreach (var kvp in data)
            {
                markdown.AppendLine($"## {kvp.Key}");
                markdown.AppendLine();
                markdown.AppendLine($"数量：{kvp.Value.Count}");
                markdown.AppendLine();

                result.ProcessedItems++;
                result.Progress = 30 + (int)((double)result.ProcessedItems / data.Count * 60);
                OnProgressUpdated(result);

                await Task.Delay(100);
            }

            await File.WriteAllTextAsync(outputPath, markdown.ToString(), Encoding.UTF8);
        }

        #region 数据获取方法（模拟实现）

        private async Task<List<object>> GetProjectDataAsync(Guid projectId)
        {
            // 模拟获取项目数据
            await Task.Delay(100);
            return Enumerable.Range(1, 20).Cast<object>().ToList();
        }

        private async Task<List<object>> GetVolumeDataAsync(Guid volumeId)
        {
            await Task.Delay(50);
            return Enumerable.Range(1, 5).Cast<object>().ToList();
        }

        private async Task<List<object>> GetChapterDataAsync(Guid chapterId)
        {
            await Task.Delay(20);
            return new List<object> { new { ChapterId = chapterId } };
        }

        private async Task<List<object>> GetCharacterDataAsync(Guid projectId)
        {
            await Task.Delay(50);
            return new List<object> { "角色数据" };
        }

        private async Task<List<object>> GetFactionDataAsync(Guid projectId)
        {
            await Task.Delay(50);
            return new List<object> { "势力数据" };
        }

        private async Task<List<object>> GetPlotDataAsync(Guid projectId)
        {
            await Task.Delay(50);
            return new List<object> { "剧情数据" };
        }

        private async Task<List<object>> GetSettingDataAsync(Guid projectId)
        {
            await Task.Delay(50);
            return new List<object> { "设定数据" };
        }

        private async Task<List<object>> GetAllVolumesDataAsync(Guid projectId)
        {
            await Task.Delay(100);
            return new List<object> { "卷宗数据1", "卷宗数据2" };
        }

        private async Task<List<object>> GetAllChaptersDataAsync(Guid projectId)
        {
            await Task.Delay(100);
            return new List<object> { "章节数据1", "章节数据2", "章节数据3" };
        }

        #endregion

        /// <summary>
        /// 触发进度更新事件
        /// </summary>
        /// <param name="result">操作结果</param>
        private void OnProgressUpdated(OperationResultDto result)
        {
            ProgressUpdated?.Invoke(this, result);
        }
    }
}
