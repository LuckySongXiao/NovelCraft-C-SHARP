using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NovelManagement.WPF.Services;
using static NovelManagement.WPF.Localization.LocalizationManager;
using NovelManagement.WPF.Models;
using NovelManagement.Application.Services;
using NovelManagement.Application.Interfaces;
using NovelManagement.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Services.RWKV;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// AI章节续写对话框
    /// </summary>
    public partial class AIContinueWriteDialog : Window
    {
        #region 字段

        private readonly ChapterEditData _chapterData;
        private readonly string _existingContent;
        private AIAssistantService? _aiAssistantService;
        private readonly ILogger<AIContinueWriteDialog>? _logger;
        private bool _isGenerating;

        #endregion

        #region 属性

        /// <summary>
        /// 续写的内容
        /// </summary>
        public string ContinuedContent { get; private set; } = string.Empty;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="chapterData">章节数据</param>
        /// <param name="existingContent">现有内容</param>
        public AIContinueWriteDialog(ChapterEditData chapterData, string existingContent)
        {
            InitializeComponent();
            _chapterData = chapterData;
            _existingContent = existingContent;

            // 初始化服务
            _aiAssistantService = App.ServiceProvider?.GetService<AIAssistantService>();
            _logger = App.ServiceProvider?.GetService<ILogger<AIContinueWriteDialog>>();

            InitializeAIService();
            LoadData();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化AI服务
        /// </summary>
        private void InitializeAIService()
        {
            try
            {
                _aiAssistantService = App.ServiceProvider?.GetService<AIAssistantService>();
                if (_aiAssistantService == null)
                {
                    MessageBox.Show(T("CW.ServiceNotInit", "AI服务未初始化，请检查配置"), T("Msg.Warning", "警告"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(TF("CW.InitFailedFmt", "初始化AI服务失败：{0}", ex.Message), T("Msg.Warning", "警告"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        private void LoadData()
        {
            ExistingContentTextBox.Text = _existingContent;
            UpdateExistingWordCount();
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 续写按钮点击事件
        /// </summary>
        private async void Continue_Click(object sender, RoutedEventArgs e)
        {
            await ContinueChapterContent();
        }

        /// <summary>
        /// 重新续写按钮点击事件
        /// </summary>
        private async void RegenerateContinue_Click(object sender, RoutedEventArgs e)
        {
            await ContinueChapterContent();
        }

        /// <summary>
        /// 复制到剪贴板按钮点击事件
        /// </summary>
        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(ContinuedContentTextBox.Text))
                {
                    Clipboard.SetText(ContinuedContentTextBox.Text);
                    MessageBox.Show(T("CW.Copied", "续写内容已复制到剪贴板"), T("Msg.Tip", "提示"), 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(TF("CW.CopyFailedFmt", "复制失败：{0}", ex.Message), T("Msg.Error", "错误"), 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 预览合并按钮点击事件
        /// </summary>
        private void PreviewMerged_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mergedContent = _existingContent + "\n\n" + ContinuedContentTextBox.Text;
                var previewDialog = new ChapterPreviewDialog(new ChapterEditData
                {
                    Title = _chapterData.Title + T("CW.PreviewMergeSuffix", " (预览合并)"),
                    Content = mergedContent,
                    Summary = _chapterData.Summary,
                    Status = _chapterData.Status,
                    ImportanceLevel = _chapterData.ImportanceLevel,
                    Characters = _chapterData.Characters,
                    Tags = _chapterData.Tags,
                    Notes = _chapterData.Notes,
                    TargetWordCount = _chapterData.TargetWordCount
                });
                previewDialog.Owner = this;
                previewDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(TF("CW.PreviewFailFmt", "预览失败：{0}", ex.Message), T("Msg.Error"), 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 应用续写按钮点击事件
        /// </summary>
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            ContinuedContent = ContinuedContentTextBox.Text;
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 续写内容文本变化事件
        /// </summary>
        private void ContinuedContentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateContinuedWordCount();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 续写章节内容
        /// </summary>
        private async Task ContinueChapterContent()
        {
            if (_isGenerating)
                return;

            try
            {
                _isGenerating = true;
                ContinueButton.IsEnabled = false;
                ContinueButton.Content = T("CW.Continuing", "续写中...");

                if (_aiAssistantService == null)
                {
                    MessageBox.Show(T("CW.ServiceNotInitShort", "AI服务未初始化"), T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 前置检查本地 RWKV 推理服务可用性，避免等待超时后报晦涩的连接错误
                var rwkvReady = await CheckRwkvOnlineAsync();
                if (!rwkvReady)
                {
                    return;
                }

                // 获取项目上下文数据
                var contextData = await GetProjectContextDataAsync();

                // 构建续写参数
                var parameters = new Dictionary<string, object>
                {
                    ["ExistingContent"] = _existingContent,
                    ["ChapterData"] = _chapterData,
                    ["ContinueLength"] = ((ComboBoxItem)ContinueLengthComboBox.SelectedItem)?.Tag?.ToString()
                                  ?? ((ComboBoxItem)ContinueLengthComboBox.SelectedItem)?.Tag?.ToString() ?? ((ComboBoxItem)ContinueLengthComboBox.SelectedItem)?.Content?.ToString() ?? "中续写",
                    ["CustomWordCount"] = CustomWordCountTextBox.Text,
                    ["ContinueDirection"] = ContinueDirectionTextBox.Text,
                    // 添加项目上下文数据
                    ["ProjectId"] = contextData.ProjectId,
                    ["ProjectName"] = contextData.ProjectName,
                    ["ProjectDescription"] = contextData.ProjectDescription,
                    ["PromptSummary"] = contextData.PromptSummary,
                    ["PlotOutlines"] = contextData.PlotOutlines,
                    ["MainCharacters"] = contextData.MainCharacters,
                    ["WorldSettings"] = contextData.WorldSettings,
                    // 指定本地 RWKV 推理（WriterAgent 按该值选择 RWKV 直连链路）
                    ["AIModel"] = "RWKV"
                };

                // 调用AI服务进行续写
                var result = await _aiAssistantService.ContinueChapterAsync(parameters);

                if (result.IsSuccess && result.Data != null)
                {
                    // 直接使用返回的文本内容
                    var continuedContent = result.Data.ToString() ?? "";
                    ContinuedContentTextBox.Text = continuedContent;
                    UpdateContinuedWordCount();

                    MessageBox.Show(T("CW.Done", "章节续写完成！"), T("CW.SuccessTitle", "成功"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // AI服务失败，显示友好错误信息
                    MessageBox.Show(TF("CW.AIFailFmt", "AI续写失败：{0}", BuildFriendlyAiError(result.Message)), T("Msg.Error"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(TF("CW.ContinueFailedFmt", "续写章节内容失败：{0}", ex.Message), T("Msg.Error", "错误"), 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isGenerating = false;
                ContinueButton.IsEnabled = true;
                ContinueButton.Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new MaterialDesignThemes.Wpf.PackIcon { Kind = MaterialDesignThemes.Wpf.PackIconKind.RobotLove, Margin = new Thickness(0,0,8,0) },
                        new TextBlock { Text = T("CW.StartContinue", "开始续写") }
                    }
                };
            }
        }



        /// <summary>
        /// 检查本地 RWKV 推理服务是否在线；离线时给出明确指引并返回 false
        /// </summary>
        private async Task<bool> CheckRwkvOnlineAsync()
        {
            try
            {
                var rwkvService = App.ServiceProvider?.GetService<IRwkvLightningService>();
                if (rwkvService == null)
                {
                    MessageBox.Show(T("CW.RwkvNotRegistered", "RWKV 推理服务未注册，请检查应用配置。"), T("CW.AiUnavailable", "AI服务不可用"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                // 3 秒竞速超时：服务离线时 GetStatusAsync 可能阻塞到 HttpClient 长超时（配置可达 600 秒）
                var status = await AIPolishDialog.GetStatusWithFastTimeoutAsync(rwkvService);
                if (status is not { Ready: true })
                {
                    MessageBox.Show(
                        TF("CW.RwkvOfflineFmt", "RWKV 推理服务不在线或无响应（{0}）。\n\n请到「AI模型配置」页启动 RWKV 服务后再试。", rwkvService.Configuration.BaseUrl),
                        T("CW.AiUnavailable", "AI服务不可用"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(TF("CW.CheckRwkvFailedFmt", "检查 RWKV 推理服务状态失败：{0}", ex.Message), T("CW.AiUnavailable", "AI服务不可用"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        /// <summary>
        /// 将底层连接类异常转换为可操作的中文提示
        /// </summary>
        private static string BuildFriendlyAiError(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return T("CW.UnknownError", "未知错误，请重试。");
            }

            if (message.Contains("An error occurred while sending", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("HttpRequestException", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("socket", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("connection", StringComparison.OrdinalIgnoreCase))
            {
                return T("CW.ConnectionError", "模型服务连接失败，请确认 RWKV 推理服务已启动（可在「AI模型配置」页查看状态），然后重试。");
            }

            return message;
        }

        /// <summary>
        /// 更新现有内容字数统计
        /// </summary>
        private void UpdateExistingWordCount()
        {
            var content = ExistingContentTextBox.Text ?? "";
            var wordCount = CountChineseCharacters(content);
            ExistingWordCountLabel.Text = TF("CW.WordCountFmt", "({0:N0}字)", wordCount);
        }

        /// <summary>
        /// 更新续写内容字数统计
        /// </summary>
        private void UpdateContinuedWordCount()
        {
            var content = ContinuedContentTextBox.Text ?? "";
            var wordCount = CountChineseCharacters(content);
            ContinuedWordCountLabel.Text = TF("CW.WordCountFmt", "({0:N0}字)", wordCount);
        }

        /// <summary>
        /// 计算中文字符数量
        /// </summary>
        /// <param name="text">文本</param>
        /// <returns>中文字符数量</returns>
        private int CountChineseCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            var count = 0;
            foreach (var c in text)
            {
                // 统计中文字符、英文字母、数字和常用标点符号
                if (char.IsLetterOrDigit(c) ||
                    (c >= 0x4e00 && c <= 0x9fff) || // 中文字符范围
                    IsChinesePunctuation(c))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 判断是否为中文标点符号
        /// </summary>
        private bool IsChinesePunctuation(char c)
        {
            var punctuations = new char[]
            {
                '，', '。', '！', '？', '；', '：',
                '\u201c', '\u201d', '\u2018', '\u2019', '（', '）',
                '【', '】', '《', '》', '、'
            };
            return punctuations.Contains(c);
        }

        /// <summary>
        /// 从AI结果中提取纯净的续写文本
        /// </summary>
        private string ExtractContinuedTextFromResult(object resultData)
        {
            try
            {
                // 如果是字符串，直接返回
                if (resultData is string text)
                {
                    return CleanupText(text);
                }

                // 如果是动态对象，尝试获取ContinuedText属性
                var resultType = resultData.GetType();
                var continuedTextProperty = resultType.GetProperty("ContinuedText");
                if (continuedTextProperty != null)
                {
                    var continuedText = continuedTextProperty.GetValue(resultData)?.ToString() ?? "";
                    return CleanupText(continuedText);
                }

                // 如果是字典类型
                if (resultData is Dictionary<string, object> dict)
                {
                    if (dict.TryGetValue("ContinuedText", out var continuedTextObj))
                    {
                        return CleanupText(continuedTextObj?.ToString() ?? "");
                    }
                    if (dict.TryGetValue("Content", out var contentObj))
                    {
                        return CleanupText(contentObj?.ToString() ?? "");
                    }
                }

                // 使用反射尝试获取文本内容
                var properties = resultType.GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.Name.Contains("Text") || prop.Name.Contains("Content"))
                    {
                        var value = prop.GetValue(resultData)?.ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            return CleanupText(value);
                        }
                    }
                }

                // 如果都没找到，返回ToString结果
                return CleanupText(resultData.ToString() ?? "");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "提取续写文本失败");
                return T("CW.ExtractErrorRetry", "提取续写内容时发生错误，请重试。");
            }
        }

        /// <summary>
        /// 清理文本内容，移除不必要的格式和代码结构
        /// </summary>
        private string CleanupText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // 移除开头和结尾的空白字符
            text = text.Trim();

            // 移除可能的JSON或代码结构标记
            text = text.Replace("{ ContinuedText = ", "")
                      .Replace(", WordCount = ", "")
                      .Replace(", ContinuationStyle = ", "")
                      .Replace(", PlotProgression = ", "")
                      .Replace(", CharacterDevelopment = ", "")
                      .Replace(", StyleConsistency = ", "")
                      .Replace(" }", "");

            // 移除多余的引号
            if (text.StartsWith("\"") && text.EndsWith("\""))
            {
                text = text.Substring(1, text.Length - 2);
            }

            // 移除转义字符
            text = text.Replace("\\n", "\n")
                      .Replace("\\r", "\r")
                      .Replace("\\t", "\t")
                      .Replace("\\\"", "\"");

            // 规范化换行符
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");

            // 移除开头的多余空行
            while (text.StartsWith("\n"))
            {
                text = text.Substring(1);
            }

            // 移除结尾的多余空行
            while (text.EndsWith("\n"))
            {
                text = text.Substring(0, text.Length - 1);
            }

            return text.Trim();
        }

        /// <summary>
        /// 获取项目上下文数据
        /// </summary>
        /// <returns>项目上下文数据</returns>
        private async Task<ProjectContextData> GetProjectContextDataAsync()
        {
            var assembler = App.ServiceProvider?.GetService<ProjectContextAssembler>();
            if (assembler == null)
            {
                return new ProjectContextData();
            }

            return await assembler.BuildCurrentProjectContextAsync();
        }

        #endregion
    }


}
