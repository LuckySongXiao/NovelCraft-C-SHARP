using Microsoft.Extensions.Logging;
using NovelManagement.Application.DTOs;
using NovelManagement.Core.Entities;
using OfficeOpenXml;
using System.Reflection;

namespace NovelManagement.Application.Services
{
    /// <summary>
    /// Excel文件处理服务
    /// </summary>
    public class ExcelProcessingService
    {
        private readonly ILogger<ExcelProcessingService> _logger;

        public ExcelProcessingService(ILogger<ExcelProcessingService> logger)
        {
            _logger = logger;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// 导出项目数据到Excel文件
        /// </summary>
        public async Task ExportToExcelAsync(string outputPath, Dictionary<string, List<object>> projectData, OperationResultDto result)
        {
            try
            {
                _logger.LogInformation("开始导出Excel文件: {OutputPath}", outputPath);

                using var package = new ExcelPackage();
                await CreateProjectOverviewWorksheetAsync(package, projectData);

                var dataSections = projectData
                    .Where(pair => !string.Equals(pair.Key, "Project", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                for (var i = 0; i < dataSections.Count; i++)
                {
                    var section = dataSections[i];
                    result.CurrentStep = $"正在写入工作表：{section.Key}";
                    result.Progress = 20 + (int)((double)(i + 1) / Math.Max(dataSections.Count, 1) * 70);
                    result.ProcessedItems = i + 1;
                    await CreateDataWorksheetAsync(package, section.Key, section.Value);
                }

                var fileInfo = new FileInfo(outputPath);
                await package.SaveAsAsync(fileInfo);

                result.CurrentStep = "Excel文件导出完成";
                result.Progress = 100;
                _logger.LogInformation("Excel文件导出成功: {OutputPath}", outputPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出Excel文件失败: {OutputPath}", outputPath);
                throw;
            }
        }

        /// <summary>
        /// 从Excel文件导入数据
        /// </summary>
        public async Task<Dictionary<string, List<Dictionary<string, object>>>> ImportFromExcelAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("开始导入Excel文件: {FilePath}", filePath);

                var result = new Dictionary<string, List<Dictionary<string, object>>>();
                using var package = new ExcelPackage(new FileInfo(filePath));

                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    var sheetData = await ReadWorksheetDataAsync(worksheet);
                    if (sheetData.Count > 0)
                    {
                        result[worksheet.Name] = sheetData;
                    }
                }

                _logger.LogInformation("Excel文件导入成功: {FilePath}, 共读取 {SheetCount} 个工作表", filePath, result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入Excel文件失败: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 预览Excel文件内容
        /// </summary>
        public async Task<ImportPreviewDto> PreviewExcelFileAsync(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var preview = new ImportPreviewDto
                {
                    FileInfo = new FileInfoDto
                    {
                        FileName = fileInfo.Name,
                        FilePath = filePath,
                        FileSize = fileInfo.Length,
                        LastModified = fileInfo.LastWriteTime
                    }
                };

                using var package = new ExcelPackage(fileInfo);
                var totalRecords = 0;

                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    var rowCount = worksheet.Dimension?.Rows ?? 0;
                    if (rowCount <= 1)
                    {
                        continue;
                    }

                    totalRecords += rowCount - 1;
                    preview.DetectedDataTypes.Add(worksheet.Name);

                    if (preview.Fields.Count == 0)
                    {
                        await AnalyzeWorksheetFieldsAsync(worksheet, preview);
                        preview.PreviewRows.AddRange((await ReadWorksheetDataAsync(worksheet)).Take(5));
                    }
                }

                preview.FileInfo.RecordCount = totalRecords;
                return preview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "预览Excel文件失败: {FilePath}", filePath);
                throw;
            }
        }

        private static async Task CreateProjectOverviewWorksheetAsync(ExcelPackage package, Dictionary<string, List<object>> projectData)
        {
            var worksheet = package.Workbook.Worksheets.Add("项目概览");
            var project = projectData.TryGetValue("Project", out var projects)
                ? projects.OfType<Project>().FirstOrDefault()
                : null;

            worksheet.Cells[1, 1].Value = "项目信息";
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Size = 14;

            var row = 3;
            WritePair(worksheet, row++, "项目名称", project?.Name ?? "未命名项目");
            WritePair(worksheet, row++, "项目类型", project?.Type ?? string.Empty);
            WritePair(worksheet, row++, "项目状态", project?.Status ?? string.Empty);
            WritePair(worksheet, row++, "创建时间", project?.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty);
            WritePair(worksheet, row++, "导出时间", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            if (!string.IsNullOrWhiteSpace(project?.Description))
            {
                WritePair(worksheet, row++, "项目描述", project.Description);
            }

            row += 2;
            worksheet.Cells[row, 1].Value = "数据统计";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            foreach (var pair in projectData)
            {
                WritePair(worksheet, row++, pair.Key, pair.Value.Count.ToString());
            }

            worksheet.Cells.AutoFitColumns();
            await Task.CompletedTask;
        }

        private static void WritePair(ExcelWorksheet worksheet, int row, string key, string value)
        {
            worksheet.Cells[row, 1].Value = key;
            worksheet.Cells[row, 2].Value = value;
        }

        private static async Task CreateDataWorksheetAsync(ExcelPackage package, string sectionName, List<object> items)
        {
            var worksheet = package.Workbook.Worksheets.Add(SanitizeWorksheetName(sectionName));
            var rows = items.Select(ConvertObjectToRow).Where(row => row.Count > 0).ToList();
            if (rows.Count == 0)
            {
                worksheet.Cells[1, 1].Value = "暂无数据";
                worksheet.Cells.AutoFitColumns();
                await Task.CompletedTask;
                return;
            }

            var headers = rows.SelectMany(row => row.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            for (var column = 0; column < headers.Count; column++)
            {
                worksheet.Cells[1, column + 1].Value = headers[column];
                worksheet.Cells[1, column + 1].Style.Font.Bold = true;
            }

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                for (var column = 0; column < headers.Count; column++)
                {
                    rows[rowIndex].TryGetValue(headers[column], out var value);
                    worksheet.Cells[rowIndex + 2, column + 1].Value = value;
                }
            }

            worksheet.Cells.AutoFitColumns();
            await Task.CompletedTask;
        }

        private async Task<List<Dictionary<string, object>>> ReadWorksheetDataAsync(ExcelWorksheet worksheet)
        {
            var result = new List<Dictionary<string, object>>();
            if (worksheet.Dimension == null || worksheet.Dimension.Rows < 2)
            {
                return result;
            }

            var headers = new List<string>();
            for (var col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                headers.Add(worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}");
            }

            for (var row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                var rowData = new Dictionary<string, object>();
                var hasData = false;
                for (var col = 1; col <= worksheet.Dimension.Columns; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Value;
                    if (cellValue != null)
                    {
                        hasData = true;
                    }

                    rowData[headers[col - 1]] = cellValue ?? string.Empty;
                }

                if (hasData)
                {
                    result.Add(rowData);
                }
            }

            await Task.CompletedTask;
            return result;
        }

        private async Task AnalyzeWorksheetFieldsAsync(ExcelWorksheet worksheet, ImportPreviewDto preview)
        {
            if (worksheet.Dimension == null || worksheet.Dimension.Rows < 2)
            {
                return;
            }

            for (var col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                var header = worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}";
                var sampleValue = worksheet.Cells[2, col].Value?.ToString() ?? string.Empty;
                preview.Fields.Add(new FieldInfoDto
                {
                    Name = header,
                    Type = DetermineFieldType(sampleValue),
                    IsRequired = !string.IsNullOrWhiteSpace(sampleValue),
                    SampleValue = sampleValue
                });
            }

            await Task.CompletedTask;
        }

        private static Dictionary<string, object?> ConvertObjectToRow(object item)
        {
            if (item is IDictionary<string, object> dictionary)
            {
                return dictionary.ToDictionary(pair => pair.Key, pair => (object?)pair.Value);
            }

            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead || !IsScalarType(property.PropertyType))
                {
                    continue;
                }

                var value = property.GetValue(item);
                row[property.Name] = value switch
                {
                    DateTime dateTime => dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    DateTimeOffset dateTimeOffset => dateTimeOffset.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    _ => value
                };
            }

            return row;
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

        private static string SanitizeWorksheetName(string sectionName)
        {
            var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };
            var sanitized = invalidChars.Aggregate(sectionName, (current, invalidChar) => current.Replace(invalidChar, '_'));
            return sanitized.Length <= 31 ? sanitized : sanitized[..31];
        }

        private static string DetermineFieldType(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "String";
            }

            if (int.TryParse(value, out _))
            {
                return "Integer";
            }

            if (double.TryParse(value, out _))
            {
                return "Double";
            }

            if (DateTime.TryParse(value, out _))
            {
                return "DateTime";
            }

            if (bool.TryParse(value, out _))
            {
                return "Boolean";
            }

            return "String";
        }
    }
}
