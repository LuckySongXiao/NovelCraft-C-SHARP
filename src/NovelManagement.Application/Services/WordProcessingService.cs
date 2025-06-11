using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.DTOs;
using System.Text;

namespace NovelManagement.Application.Services
{
    /// <summary>
    /// Word文档处理服务
    /// </summary>
    public class WordProcessingService
    {
        private readonly ILogger<WordProcessingService> _logger;

        public WordProcessingService(ILogger<WordProcessingService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 导出项目数据到Word文档
        /// </summary>
        /// <param name="outputPath">输出文件路径</param>
        /// <param name="projectData">项目数据</param>
        /// <param name="result">操作结果</param>
        public async Task ExportToWordAsync(string outputPath, Dictionary<string, List<object>> projectData, OperationResultDto result)
        {
            try
            {
                _logger.LogInformation($"开始导出Word文档: {outputPath}");

                using var document = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
                
                // 创建主文档部分
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // 添加文档标题
                AddTitle(body, "千面劫·宿命轮回 - 项目导出文档");
                
                // 添加项目概览
                await AddProjectOverview(body, projectData, result);
                
                // 添加角色信息
                if (projectData.ContainsKey("Characters"))
                {
                    await AddCharactersSection(body, projectData["Characters"], result);
                }
                
                // 添加势力信息
                if (projectData.ContainsKey("Factions"))
                {
                    await AddFactionsSection(body, projectData["Factions"], result);
                }
                
                // 添加剧情信息
                if (projectData.ContainsKey("Plots"))
                {
                    await AddPlotsSection(body, projectData["Plots"], result);
                }
                
                // 添加卷宗信息
                if (projectData.ContainsKey("Volumes"))
                {
                    await AddVolumesSection(body, projectData["Volumes"], result);
                }
                
                // 添加章节信息
                if (projectData.ContainsKey("Chapters"))
                {
                    await AddChaptersSection(body, projectData["Chapters"], result);
                }

                // 保存文档
                mainPart.Document.Save();
                
                result.CurrentStep = "Word文档导出完成";
                result.Progress = 100;
                
                _logger.LogInformation($"Word文档导出成功: {outputPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"导出Word文档失败: {outputPath}");
                throw;
            }
        }

        /// <summary>
        /// 从Word文档导入数据
        /// </summary>
        /// <param name="filePath">Word文档路径</param>
        /// <returns>导入的数据</returns>
        public async Task<Dictionary<string, List<Dictionary<string, object>>>> ImportFromWordAsync(string filePath)
        {
            try
            {
                _logger.LogInformation($"开始导入Word文档: {filePath}");
                
                var result = new Dictionary<string, List<Dictionary<string, object>>>();
                
                using var document = WordprocessingDocument.Open(filePath, false);
                var body = document.MainDocumentPart?.Document?.Body;
                
                if (body != null)
                {
                    // 解析文档内容
                    var textContent = ExtractTextFromBody(body);
                    
                    // 简单的章节识别和解析
                    var chapters = ParseChaptersFromText(textContent);
                    if (chapters.Any())
                    {
                        result["Chapters"] = chapters;
                    }
                }
                
                _logger.LogInformation($"Word文档导入成功: {filePath}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"导入Word文档失败: {filePath}");
                throw;
            }
        }

        /// <summary>
        /// 预览Word文档内容
        /// </summary>
        /// <param name="filePath">Word文档路径</param>
        /// <returns>预览信息</returns>
        public async Task<ImportPreviewDto> PreviewWordFileAsync(string filePath)
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

                using var document = WordprocessingDocument.Open(filePath, false);
                var body = document.MainDocumentPart?.Document?.Body;
                
                if (body != null)
                {
                    var textContent = ExtractTextFromBody(body);
                    var wordCount = textContent.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                    
                    preview.FileInfo.RecordCount = wordCount;
                    preview.DetectedDataTypes.Add("文本内容");
                    
                    // 检测章节
                    var chapterMatches = System.Text.RegularExpressions.Regex.Matches(textContent, @"第[一二三四五六七八九十\d]+章");
                    if (chapterMatches.Count > 0)
                    {
                        preview.DetectedDataTypes.Add("章节结构");
                        preview.FileInfo.RecordCount = chapterMatches.Count;
                    }
                    
                    preview.Fields.Add(new FieldInfoDto
                    {
                        Name = "章节标题",
                        Type = "String",
                        IsRequired = true,
                        SampleValue = chapterMatches.Count > 0 ? chapterMatches[0].Value : "第一章"
                    });
                    
                    preview.Fields.Add(new FieldInfoDto
                    {
                        Name = "章节内容",
                        Type = "String",
                        IsRequired = true,
                        SampleValue = textContent.Length > 100 ? textContent.Substring(0, 100) + "..." : textContent
                    });
                }
                
                return preview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"预览Word文档失败: {filePath}");
                throw;
            }
        }

        #region 私有方法

        /// <summary>
        /// 添加文档标题
        /// </summary>
        private void AddTitle(Body body, string title)
        {
            var titleParagraph = new Paragraph();
            var titleRun = new Run();
            var titleText = new Text(title);
            
            // 设置标题样式
            var titleRunProperties = new RunProperties();
            titleRunProperties.AppendChild(new Bold());
            titleRunProperties.AppendChild(new FontSize() { Val = "28" });
            titleRun.AppendChild(titleRunProperties);
            titleRun.AppendChild(titleText);
            titleParagraph.AppendChild(titleRun);
            
            // 设置段落居中
            var paragraphProperties = new ParagraphProperties();
            paragraphProperties.AppendChild(new Justification() { Val = JustificationValues.Center });
            titleParagraph.AppendChild(paragraphProperties);
            
            body.AppendChild(titleParagraph);
            
            // 添加空行
            body.AppendChild(new Paragraph());
        }

        /// <summary>
        /// 添加项目概览
        /// </summary>
        private async Task AddProjectOverview(Body body, Dictionary<string, List<object>> projectData, OperationResultDto result)
        {
            // 添加章节标题
            AddSectionTitle(body, "项目概览");
            
            // 添加项目信息
            AddParagraph(body, $"项目名称：千面劫·宿命轮回");
            AddParagraph(body, $"导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            AddParagraph(body, "");
            
            // 添加统计信息
            AddParagraph(body, "数据统计：");
            foreach (var kvp in projectData)
            {
                AddParagraph(body, $"  {kvp.Key}：{kvp.Value.Count} 项");
            }
            
            AddParagraph(body, "");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 添加角色信息章节
        /// </summary>
        private async Task AddCharactersSection(Body body, List<object> characters, OperationResultDto result)
        {
            AddSectionTitle(body, "角色信息");
            
            // 添加示例角色数据
            var sampleCharacters = new[]
            {
                new { Name = "林轩", Age = 18, Gender = "男", Cultivation = "练气期", Faction = "玄天宗", Description = "主角，天赋异禀" },
                new { Name = "苏雨", Age = 17, Gender = "女", Cultivation = "练气期", Faction = "玄天宗", Description = "女主角，冰雪聪明" }
            };
            
            foreach (var character in sampleCharacters)
            {
                AddParagraph(body, $"姓名：{character.Name}");
                AddParagraph(body, $"年龄：{character.Age}");
                AddParagraph(body, $"性别：{character.Gender}");
                AddParagraph(body, $"修为：{character.Cultivation}");
                AddParagraph(body, $"势力：{character.Faction}");
                AddParagraph(body, $"描述：{character.Description}");
                AddParagraph(body, "");
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 添加势力信息章节
        /// </summary>
        private async Task AddFactionsSection(Body body, List<object> factions, OperationResultDto result)
        {
            AddSectionTitle(body, "势力信息");
            
            var sampleFactions = new[]
            {
                new { Name = "玄天宗", Type = "修仙门派", Level = "一流", Leader = "玄天真人", Territory = "玄天山", Description = "正道大宗" },
                new { Name = "魔道联盟", Type = "魔道组织", Level = "超一流", Leader = "魔主", Territory = "魔域", Description = "邪道势力" }
            };
            
            foreach (var faction in sampleFactions)
            {
                AddParagraph(body, $"势力名称：{faction.Name}");
                AddParagraph(body, $"类型：{faction.Type}");
                AddParagraph(body, $"等级：{faction.Level}");
                AddParagraph(body, $"领袖：{faction.Leader}");
                AddParagraph(body, $"地盘：{faction.Territory}");
                AddParagraph(body, $"描述：{faction.Description}");
                AddParagraph(body, "");
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 添加剧情信息章节
        /// </summary>
        private async Task AddPlotsSection(Body body, List<object> plots, OperationResultDto result)
        {
            AddSectionTitle(body, "剧情信息");
            AddParagraph(body, "剧情信息将在此处显示...");
            AddParagraph(body, "");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 添加卷宗信息章节
        /// </summary>
        private async Task AddVolumesSection(Body body, List<object> volumes, OperationResultDto result)
        {
            AddSectionTitle(body, "卷宗信息");
            AddParagraph(body, "卷宗信息将在此处显示...");
            AddParagraph(body, "");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 添加章节信息章节
        /// </summary>
        private async Task AddChaptersSection(Body body, List<object> chapters, OperationResultDto result)
        {
            AddSectionTitle(body, "章节信息");
            AddParagraph(body, "章节信息将在此处显示...");
            AddParagraph(body, "");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 添加章节标题
        /// </summary>
        private void AddSectionTitle(Body body, string title)
        {
            var titleParagraph = new Paragraph();
            var titleRun = new Run();
            var titleText = new Text(title);

            // 设置标题样式
            var titleRunProperties = new RunProperties();
            titleRunProperties.AppendChild(new Bold());
            titleRunProperties.AppendChild(new FontSize() { Val = "20" });
            titleRun.AppendChild(titleRunProperties);
            titleRun.AppendChild(titleText);
            titleParagraph.AppendChild(titleRun);

            body.AppendChild(titleParagraph);
        }

        /// <summary>
        /// 添加普通段落
        /// </summary>
        private void AddParagraph(Body body, string text)
        {
            var paragraph = new Paragraph();
            var run = new Run();
            var textElement = new Text(text);

            run.AppendChild(textElement);
            paragraph.AppendChild(run);
            body.AppendChild(paragraph);
        }

        /// <summary>
        /// 从文档主体提取文本
        /// </summary>
        private string ExtractTextFromBody(Body body)
        {
            var text = new StringBuilder();

            foreach (var element in body.Elements())
            {
                if (element is Paragraph paragraph)
                {
                    foreach (var run in paragraph.Elements<Run>())
                    {
                        foreach (var textElement in run.Elements<Text>())
                        {
                            text.Append(textElement.Text);
                        }
                    }
                    text.AppendLine();
                }
            }

            return text.ToString();
        }

        /// <summary>
        /// 从文本中解析章节
        /// </summary>
        private List<Dictionary<string, object>> ParseChaptersFromText(string text)
        {
            var chapters = new List<Dictionary<string, object>>();

            // 使用正则表达式匹配章节标题
            var chapterPattern = @"第[一二三四五六七八九十\d]+章[^\r\n]*";
            var matches = System.Text.RegularExpressions.Regex.Matches(text, chapterPattern);

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var title = match.Value.Trim();

                // 获取章节内容（从当前章节到下一章节之间的文本）
                var startIndex = match.Index + match.Length;
                var endIndex = i < matches.Count - 1 ? matches[i + 1].Index : text.Length;
                var content = text.Substring(startIndex, endIndex - startIndex).Trim();

                var chapter = new Dictionary<string, object>
                {
                    ["Title"] = title,
                    ["Content"] = content,
                    ["Order"] = i + 1,
                    ["WordCount"] = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                };

                chapters.Add(chapter);
            }

            return chapters;
        }

        #endregion
    }
}
