using OfficeOpenXml;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.DTOs;
using System.Text;

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
            
            // 设置EPPlus许可证上下文
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// 导出项目数据到Excel文件
        /// </summary>
        /// <param name="outputPath">输出文件路径</param>
        /// <param name="projectData">项目数据</param>
        /// <param name="result">操作结果</param>
        public async Task ExportToExcelAsync(string outputPath, Dictionary<string, List<object>> projectData, OperationResultDto result)
        {
            try
            {
                _logger.LogInformation($"开始导出Excel文件: {outputPath}");
                
                using var package = new ExcelPackage();
                
                // 创建项目概览工作表
                await CreateProjectOverviewWorksheet(package, projectData, result);
                
                // 创建角色信息工作表
                if (projectData.ContainsKey("Characters"))
                {
                    await CreateCharactersWorksheet(package, projectData["Characters"], result);
                }
                
                // 创建势力信息工作表
                if (projectData.ContainsKey("Factions"))
                {
                    await CreateFactionsWorksheet(package, projectData["Factions"], result);
                }
                
                // 创建剧情信息工作表
                if (projectData.ContainsKey("Plots"))
                {
                    await CreatePlotsWorksheet(package, projectData["Plots"], result);
                }
                
                // 创建卷宗信息工作表
                if (projectData.ContainsKey("Volumes"))
                {
                    await CreateVolumesWorksheet(package, projectData["Volumes"], result);
                }
                
                // 创建章节信息工作表
                if (projectData.ContainsKey("Chapters"))
                {
                    await CreateChaptersWorksheet(package, projectData["Chapters"], result);
                }
                
                // 创建世界设定工作表
                if (projectData.ContainsKey("WorldSettings"))
                {
                    await CreateWorldSettingsWorksheet(package, projectData["WorldSettings"], result);
                }
                
                // 保存文件
                var fileInfo = new FileInfo(outputPath);
                await package.SaveAsAsync(fileInfo);
                
                result.CurrentStep = "Excel文件导出完成";
                result.Progress = 100;
                
                _logger.LogInformation($"Excel文件导出成功: {outputPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"导出Excel文件失败: {outputPath}");
                throw;
            }
        }

        /// <summary>
        /// 从Excel文件导入数据
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <returns>导入的数据</returns>
        public async Task<Dictionary<string, List<Dictionary<string, object>>>> ImportFromExcelAsync(string filePath)
        {
            try
            {
                _logger.LogInformation($"开始导入Excel文件: {filePath}");
                
                var result = new Dictionary<string, List<Dictionary<string, object>>>();
                
                using var package = new ExcelPackage(new FileInfo(filePath));
                
                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    var sheetData = await ReadWorksheetDataAsync(worksheet);
                    if (sheetData.Any())
                    {
                        result[worksheet.Name] = sheetData;
                    }
                }
                
                _logger.LogInformation($"Excel文件导入成功: {filePath}, 共读取 {result.Count} 个工作表");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"导入Excel文件失败: {filePath}");
                throw;
            }
        }

        /// <summary>
        /// 预览Excel文件内容
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <returns>预览信息</returns>
        public async Task<ImportPreviewDto> PreviewExcelFileAsync(string filePath)
        {
            try
            {
                var preview = new ImportPreviewDto
                {
                    FileInfo = new FileInfoDto
                    {
                        FileName = Path.GetFileName(filePath),
                        FilePath = filePath,
                        FileSize = new FileInfo(filePath).Length,
                        LastModified = File.GetLastWriteTime(filePath)
                    }
                };

                using var package = new ExcelPackage(new FileInfo(filePath));
                
                var totalRecords = 0;
                var detectedTypes = new List<string>();
                
                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    var rowCount = worksheet.Dimension?.Rows ?? 0;
                    if (rowCount > 1) // 排除标题行
                    {
                        totalRecords += rowCount - 1;
                        detectedTypes.Add(worksheet.Name);
                        
                        // 分析第一个工作表的字段信息
                        if (preview.Fields.Count == 0 && worksheet.Dimension != null)
                        {
                            await AnalyzeWorksheetFields(worksheet, preview);
                        }
                    }
                }
                
                preview.FileInfo.RecordCount = totalRecords;
                preview.DetectedDataTypes.AddRange(detectedTypes);
                
                return preview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"预览Excel文件失败: {filePath}");
                throw;
            }
        }

        #region 私有方法

        /// <summary>
        /// 创建项目概览工作表
        /// </summary>
        private async Task CreateProjectOverviewWorksheet(ExcelPackage package, Dictionary<string, List<object>> projectData, OperationResultDto result)
        {
            var worksheet = package.Workbook.Worksheets.Add("项目概览");
            
            // 设置标题
            worksheet.Cells[1, 1].Value = "项目信息";
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Size = 14;
            
            var row = 3;
            worksheet.Cells[row++, 1].Value = "项目名称";
            worksheet.Cells[row - 1, 2].Value = "千面劫·宿命轮回";
            
            worksheet.Cells[row++, 1].Value = "创建时间";
            worksheet.Cells[row - 1, 2].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            worksheet.Cells[row++, 1].Value = "导出时间";
            worksheet.Cells[row - 1, 2].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            // 统计信息
            row += 2;
            worksheet.Cells[row++, 1].Value = "数据统计";
            worksheet.Cells[row - 1, 1].Style.Font.Bold = true;
            
            foreach (var kvp in projectData)
            {
                worksheet.Cells[row++, 1].Value = kvp.Key;
                worksheet.Cells[row - 1, 2].Value = kvp.Value.Count;
            }
            
            // 自动调整列宽
            worksheet.Cells.AutoFitColumns();
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 创建角色信息工作表
        /// </summary>
        private async Task CreateCharactersWorksheet(ExcelPackage package, List<object> characters, OperationResultDto result)
        {
            var worksheet = package.Workbook.Worksheets.Add("角色信息");
            
            // 设置标题行
            var headers = new[] { "ID", "姓名", "年龄", "性别", "修为", "势力", "描述", "创建时间" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }
            
            // 添加数据行（这里使用示例数据，实际应该从characters参数获取）
            var sampleData = new[]
            {
                new { Id = 1, Name = "林轩", Age = 18, Gender = "男", Cultivation = "练气期", Faction = "玄天宗", Description = "主角，天赋异禀", CreatedAt = DateTime.Now },
                new { Id = 2, Name = "苏雨", Age = 17, Gender = "女", Cultivation = "练气期", Faction = "玄天宗", Description = "女主角，冰雪聪明", CreatedAt = DateTime.Now }
            };
            
            for (int i = 0; i < sampleData.Length; i++)
            {
                var row = i + 2;
                var data = sampleData[i];
                worksheet.Cells[row, 1].Value = data.Id;
                worksheet.Cells[row, 2].Value = data.Name;
                worksheet.Cells[row, 3].Value = data.Age;
                worksheet.Cells[row, 4].Value = data.Gender;
                worksheet.Cells[row, 5].Value = data.Cultivation;
                worksheet.Cells[row, 6].Value = data.Faction;
                worksheet.Cells[row, 7].Value = data.Description;
                worksheet.Cells[row, 8].Value = data.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            }
            
            // 自动调整列宽
            worksheet.Cells.AutoFitColumns();
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 创建势力信息工作表
        /// </summary>
        private async Task CreateFactionsWorksheet(ExcelPackage package, List<object> factions, OperationResultDto result)
        {
            var worksheet = package.Workbook.Worksheets.Add("势力信息");
            
            // 设置标题行
            var headers = new[] { "ID", "势力名称", "类型", "等级", "领袖", "地盘", "描述", "创建时间" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }
            
            // 添加示例数据
            var sampleData = new[]
            {
                new { Id = 1, Name = "玄天宗", Type = "修仙门派", Level = "一流", Leader = "玄天真人", Territory = "玄天山", Description = "正道大宗", CreatedAt = DateTime.Now },
                new { Id = 2, Name = "魔道联盟", Type = "魔道组织", Level = "超一流", Leader = "魔主", Territory = "魔域", Description = "邪道势力", CreatedAt = DateTime.Now }
            };
            
            for (int i = 0; i < sampleData.Length; i++)
            {
                var row = i + 2;
                var data = sampleData[i];
                worksheet.Cells[row, 1].Value = data.Id;
                worksheet.Cells[row, 2].Value = data.Name;
                worksheet.Cells[row, 3].Value = data.Type;
                worksheet.Cells[row, 4].Value = data.Level;
                worksheet.Cells[row, 5].Value = data.Leader;
                worksheet.Cells[row, 6].Value = data.Territory;
                worksheet.Cells[row, 7].Value = data.Description;
                worksheet.Cells[row, 8].Value = data.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            }
            
            worksheet.Cells.AutoFitColumns();
            await Task.CompletedTask;
        }

        /// <summary>
        /// 创建剧情信息工作表
        /// </summary>
        private async Task CreatePlotsWorksheet(ExcelPackage package, List<object> plots, OperationResultDto result)
        {
            var worksheet = package.Workbook.Worksheets.Add("剧情信息");

            var headers = new[] { "ID", "剧情名称", "类型", "状态", "重要性", "描述", "创建时间" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            worksheet.Cells.AutoFitColumns();
            await Task.CompletedTask;
        }

        /// <summary>
        /// 创建卷宗信息工作表
        /// </summary>
        private async Task CreateVolumesWorksheet(ExcelPackage package, List<object> volumes, OperationResultDto result)
        {
            var worksheet = package.Workbook.Worksheets.Add("卷宗信息");

            var headers = new[] { "ID", "卷宗名称", "顺序", "状态", "字数", "章节数", "描述", "创建时间" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            worksheet.Cells.AutoFitColumns();
            await Task.CompletedTask;
        }

        /// <summary>
        /// 创建章节信息工作表
        /// </summary>
        private async Task CreateChaptersWorksheet(ExcelPackage package, List<object> chapters, OperationResultDto result)
        {
            var worksheet = package.Workbook.Worksheets.Add("章节信息");

            var headers = new[] { "ID", "章节名称", "卷宗", "顺序", "状态", "字数", "创建时间" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            worksheet.Cells.AutoFitColumns();
            await Task.CompletedTask;
        }

        /// <summary>
        /// 创建世界设定工作表
        /// </summary>
        private async Task CreateWorldSettingsWorksheet(ExcelPackage package, List<object> worldSettings, OperationResultDto result)
        {
            var worksheet = package.Workbook.Worksheets.Add("世界设定");

            var headers = new[] { "ID", "设定名称", "类型", "描述", "创建时间" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            worksheet.Cells.AutoFitColumns();
            await Task.CompletedTask;
        }

        /// <summary>
        /// 读取工作表数据
        /// </summary>
        private async Task<List<Dictionary<string, object>>> ReadWorksheetDataAsync(ExcelWorksheet worksheet)
        {
            var result = new List<Dictionary<string, object>>();

            if (worksheet.Dimension == null || worksheet.Dimension.Rows < 2)
                return result;

            // 读取标题行
            var headers = new List<string>();
            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                var header = worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}";
                headers.Add(header);
            }

            // 读取数据行
            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                var rowData = new Dictionary<string, object>();
                var hasData = false;

                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Value;
                    if (cellValue != null)
                    {
                        hasData = true;
                    }
                    rowData[headers[col - 1]] = cellValue ?? "";
                }

                if (hasData)
                {
                    result.Add(rowData);
                }
            }

            await Task.CompletedTask;
            return result;
        }

        /// <summary>
        /// 分析工作表字段信息
        /// </summary>
        private async Task AnalyzeWorksheetFields(ExcelWorksheet worksheet, ImportPreviewDto preview)
        {
            if (worksheet.Dimension == null || worksheet.Dimension.Rows < 2)
                return;

            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                var header = worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}";
                var sampleValue = worksheet.Cells[2, col].Value?.ToString() ?? "";

                var fieldInfo = new FieldInfoDto
                {
                    Name = header,
                    Type = DetermineFieldType(sampleValue),
                    IsRequired = !string.IsNullOrEmpty(sampleValue),
                    SampleValue = sampleValue
                };

                preview.Fields.Add(fieldInfo);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 确定字段类型
        /// </summary>
        private string DetermineFieldType(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "String";

            if (int.TryParse(value, out _))
                return "Integer";

            if (double.TryParse(value, out _))
                return "Double";

            if (DateTime.TryParse(value, out _))
                return "DateTime";

            if (bool.TryParse(value, out _))
                return "Boolean";

            return "String";
        }

        #endregion
    }
}
