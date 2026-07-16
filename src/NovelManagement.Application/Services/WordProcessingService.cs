using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.DTOs;
using NovelManagement.Core.Entities;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

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
        public async Task ExportToWordAsync(string outputPath, Dictionary<string, List<object>> projectData, OperationResultDto result)
        {
            try
            {
                _logger.LogInformation("开始导出Word文档: {OutputPath}", outputPath);

                using var document = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());
                var body = mainPart.Document.Body!;

                var project = projectData.TryGetValue("Project", out var projects)
                    ? projects.OfType<Project>().FirstOrDefault()
                    : null;
                AddTitle(body, $"{project?.Name ?? "未命名项目"} - 项目导出文档");
                AddParagraph(body, $"导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                AddParagraph(body, string.Empty);

                var sections = projectData.ToList();
                for (var i = 0; i < sections.Count; i++)
                {
                    var section = sections[i];
                    result.CurrentStep = $"正在写入章节：{section.Key}";
                    result.Progress = 20 + (int)((double)(i + 1) / Math.Max(sections.Count, 1) * 70);
                    result.ProcessedItems = i + 1;

                    AddSectionTitle(body, section.Key);
                    if (section.Value.Count == 0)
                    {
                        AddParagraph(body, "暂无数据");
                        AddParagraph(body, string.Empty);
                        continue;
                    }

                    foreach (var item in section.Value)
                    {
                        foreach (var line in DescribeItem(item))
                        {
                            AddParagraph(body, line);
                        }
                        AddParagraph(body, string.Empty);
                    }
                }

                mainPart.Document.Save();
                result.CurrentStep = "Word文档导出完成";
                result.Progress = 100;
                _logger.LogInformation("Word文档导出成功: {OutputPath}", outputPath);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出Word文档失败: {OutputPath}", outputPath);
                throw;
            }
        }

        /// <summary>
        /// 从Word文档导入数据
        /// </summary>
        public async Task<Dictionary<string, List<Dictionary<string, object>>>> ImportFromWordAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("开始导入Word文档: {FilePath}", filePath);
                var result = new Dictionary<string, List<Dictionary<string, object>>>();

                using var document = WordprocessingDocument.Open(filePath, false);
                var body = document.MainDocumentPart?.Document?.Body;
                if (body != null)
                {
                    var textContent = ExtractTextFromBody(body);
                    var chapters = ParseChaptersFromText(textContent);
                    if (chapters.Count > 0)
                    {
                        result["Chapters"] = chapters;
                    }
                    else if (!string.IsNullOrWhiteSpace(textContent))
                    {
                        result["Chapters"] = new List<Dictionary<string, object>>
                        {
                            new()
                            {
                                ["Title"] = Path.GetFileNameWithoutExtension(filePath),
                                ["Content"] = textContent.Trim(),
                                ["Order"] = 1,
                                ["WordCount"] = textContent.Trim().Length
                            }
                        };
                    }
                }

                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入Word文档失败: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 预览Word文档内容
        /// </summary>
        public async Task<ImportPreviewDto> PreviewWordFileAsync(string filePath)
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

                using var document = WordprocessingDocument.Open(filePath, false);
                var body = document.MainDocumentPart?.Document?.Body;
                if (body == null)
                {
                    return preview;
                }

                var textContent = ExtractTextFromBody(body).Trim();
                var chapterMatches = Regex.Matches(textContent, @"第[一二三四五六七八九十百千\d]+章[^\r\n]*");

                preview.FileInfo.RecordCount = chapterMatches.Count > 0
                    ? chapterMatches.Count
                    : textContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
                preview.DetectedDataTypes.Add(chapterMatches.Count > 0 ? "章节结构" : "文本内容");
                preview.Fields.Add(new FieldInfoDto
                {
                    Name = "章节标题",
                    Type = "String",
                    IsRequired = true,
                    SampleValue = chapterMatches.Count > 0 ? chapterMatches[0].Value : Path.GetFileNameWithoutExtension(filePath)
                });
                preview.Fields.Add(new FieldInfoDto
                {
                    Name = "章节内容",
                    Type = "String",
                    IsRequired = true,
                    SampleValue = textContent.Length > 120 ? textContent[..120] + "..." : textContent
                });
                preview.PreviewRows.Add(new Dictionary<string, object>
                {
                    ["文件"] = fileInfo.Name,
                    ["结构"] = chapterMatches.Count > 0 ? "章节文档" : "连续文本",
                    ["摘要"] = textContent.Length > 120 ? textContent[..120] + "..." : textContent
                });

                return await Task.FromResult(preview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "预览Word文档失败: {FilePath}", filePath);
                throw;
            }
        }

        private static void AddTitle(Body body, string title)
        {
            var paragraph = new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(new RunProperties(new Bold(), new FontSize { Val = "28" }), new Text(title)));
            body.AppendChild(paragraph);
            body.AppendChild(new Paragraph());
        }

        private static void AddSectionTitle(Body body, string title)
        {
            var paragraph = new Paragraph(
                new Run(new RunProperties(new Bold(), new FontSize { Val = "22" }), new Text(title)));
            body.AppendChild(paragraph);
        }

        private static void AddParagraph(Body body, string text)
        {
            body.AppendChild(new Paragraph(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve })));
        }

        private static IEnumerable<string> DescribeItem(object item)
        {
            if (item is IDictionary<string, object> dictionary)
            {
                foreach (var pair in dictionary)
                {
                    yield return $"- {pair.Key}: {pair.Value}";
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

                yield return $"- {property.Name}: {FormatValue(value)}";
            }
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

        private static string FormatValue(object value)
        {
            return value switch
            {
                DateTime dateTime => dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                _ => value.ToString() ?? string.Empty
            };
        }

        private static string ExtractTextFromBody(Body body)
        {
            var builder = new StringBuilder();
            foreach (var paragraph in body.Elements<Paragraph>())
            {
                builder.AppendLine(paragraph.InnerText);
            }
            return builder.ToString();
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
    }
}
