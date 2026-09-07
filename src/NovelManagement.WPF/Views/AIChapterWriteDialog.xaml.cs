using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;
using NovelManagement.WPF.Services;
using NovelManagement.WPF.Models;
using NovelManagement.Application.Services;
using NovelManagement.Application.Interfaces;
using NovelManagement.Core.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// AI章节编写对话框
    /// </summary>
    public partial class AIChapterWriteDialog : Window
    {
        #region 字段

        private readonly ChapterEditData _chapterData;
        private AIAssistantService? _aiAssistantService;
        private bool _isGenerating;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
        private Dictionary<string, object> _lastGenerationMetadata = new();

        #endregion

        #region 属性

        /// <summary>
        /// 生成的内容
        /// </summary>
        public string GeneratedContent { get; private set; } = string.Empty;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="chapterData">章节数据</param>
        public AIChapterWriteDialog(ChapterEditData chapterData)
        {
            InitializeComponent();
            _chapterData = chapterData;
            InitializeAIService();
            LoadChapterData();

            // 确保UI完全加载后再初始化统计信息
            this.Loaded += (s, e) =>
            {
                try
                {
                    ClearRwkvDebugInfo();
                    UpdateRealTimeStatistics();
                    UpdateQualityScore();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"初始化统计信息失败: {ex.Message}");
                }
            };
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
                    MessageBox.Show("AI服务未初始化，请检查配置", "警告",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化AI服务失败：{ex.Message}", "警告",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 加载章节数据
        /// </summary>
        private void LoadChapterData()
        {
            ChapterTitleTextBox.Text = _chapterData.Title;
            TargetWordCountTextBox.Text = _chapterData.TargetWordCount.ToString();
            CharactersTextBox.Text = _chapterData.Characters;
            ChapterOutlineTextBox.Text = _chapterData.Summary;
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 生成按钮点击事件
        /// </summary>
        private async void Generate_Click(object sender, RoutedEventArgs e)
        {
            await GenerateChapterContent();
        }

        /// <summary>
        /// 重新生成按钮点击事件
        /// </summary>
        private async void Regenerate_Click(object sender, RoutedEventArgs e)
        {
            await GenerateChapterContent();
        }

        /// <summary>
        /// 复制到剪贴板按钮点击事件
        /// </summary>
        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(GeneratedContentTextBox.Text))
                {
                    Clipboard.SetText(GeneratedContentTextBox.Text);
                    MessageBox.Show("内容已复制到剪贴板", "提示", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 应用内容按钮点击事件
        /// </summary>
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            GeneratedContent = GeneratedContentTextBox.Text;
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
        /// 生成内容文本变化事件
        /// </summary>
        private void GeneratedContentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                UpdateWordCount();
                UpdateRealTimeStatistics();
            }
            catch (Exception ex)
            {
                // 静默处理文本变化事件错误，避免影响主要功能
                System.Diagnostics.Debug.WriteLine($"处理文本变化事件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 快速生成按钮点击事件
        /// </summary>
        private async void QuickGenerate_Click(object sender, RoutedEventArgs e)
        {
            // 使用默认设置快速生成
            await GenerateChapterContent(useQuickMode: true);
        }

        /// <summary>
        /// 管理模板按钮点击事件
        /// </summary>
        private void ManageTemplates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var templateDirectory = GetChapterTemplateDirectory();
                Directory.CreateDirectory(templateDirectory);

                var dialog = new ChapterTemplateManagementDialog(templateDirectory)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.SelectedTemplatePath))
                {
                    return;
                }

                var template = LoadTemplateFromFile(dialog.SelectedTemplatePath);
                if (template == null)
                {
                    MessageBox.Show("模板内容无效。", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ApplyTemplate(template);
                MessageBox.Show($"模板已加载：{template.Name}", "加载成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开模板管理失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存模板按钮点击事件
        /// </summary>
        private void SaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(GeneratedContentTextBox.Text) &&
                    string.IsNullOrWhiteSpace(ChapterOutlineTextBox.Text) &&
                    string.IsNullOrWhiteSpace(KeyPlotsTextBox.Text))
                {
                    MessageBox.Show("没有内容可保存为模板", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var inputDialog = new TextInputDialog("保存章节模板", "模板名称", $"章节模板_{DateTime.Now:yyyyMMdd_HHmmss}")
                {
                    Owner = this
                };

                if (inputDialog.ShowDialog() != true)
                {
                    return;
                }

                var template = BuildTemplate(inputDialog.InputText);
                var templateDirectory = GetChapterTemplateDirectory();
                Directory.CreateDirectory(templateDirectory);
                var templatePath = Path.Combine(templateDirectory, $"{SanitizeFileName(template.Name)}.json");
                File.WriteAllText(templatePath, JsonSerializer.Serialize(template, _jsonSerializerOptions));

                MessageBox.Show($"模板已保存：{templatePath}", "保存成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存模板失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 润色内容按钮点击事件
        /// </summary>
        private void PolishContent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(GeneratedContentTextBox.Text))
                {
                    MessageBox.Show("没有内容可润色", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 打开AI润色对话框
                var polishDialog = new AIPolishDialog(GeneratedContentTextBox.Text);
                polishDialog.Owner = this;

                if (polishDialog.ShowDialog() == true)
                {
                    var result = polishDialog.PolishedContent;
                    if (!string.IsNullOrEmpty(result))
                    {
                        GeneratedContentTextBox.Text = result;
                        UpdateWordCount();
                        UpdateRealTimeStatistics();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"润色内容失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 生成章节内容
        /// </summary>
        /// <param name="useQuickMode">是否使用快速模式</param>
        private async Task GenerateChapterContent(bool useQuickMode = false)
        {
            if (_isGenerating)
                return;

            try
            {
                _isGenerating = true;
                GenerateButton.IsEnabled = false;
                QuickGenerateButton.IsEnabled = false;

                // 显示进度和状态
                GenerationProgressBar.Visibility = Visibility.Visible;
                StatusTextBlock.Visibility = Visibility.Visible;
                StatusTextBlock.Text = "正在准备生成参数...";
                GenerationProgressBar.Value = 10;

                if (useQuickMode)
                {
                    GenerateButton.Content = "快速生成中...";
                }
                else
                {
                    GenerateButton.Content = "生成中...";
                }

                if (_aiAssistantService == null)
                {
                    MessageBox.Show("AI服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 获取项目上下文数据
                var contextData = await GetProjectContextDataAsync();

                // 构建生成参数
                var parameters = new Dictionary<string, object>
                {
                    ["ChapterTitle"] = ChapterTitleTextBox.Text,
                    ["WritingStyle"] = ((ComboBoxItem)WritingStyleComboBox.SelectedItem)?.Content?.ToString() ?? "古风仙侠",
                    ["ChapterType"] = ((ComboBoxItem)ChapterTypeComboBox.SelectedItem)?.Content?.ToString() ?? "正文章节",
                    ["TargetWordCount"] = TargetWordCountTextBox.Text,
                    ["ChapterOutline"] = ChapterOutlineTextBox.Text,
                    ["KeyPlots"] = KeyPlotsTextBox.Text,
                    ["Characters"] = CharactersTextBox.Text,
                    ["SpecialRequirements"] = SpecialRequirementsTextBox.Text,
                    ["ExistingChapterData"] = _chapterData,
                    // 新增高级参数
                    ["AIModel"] = ((ComboBoxItem)AIModelComboBox.SelectedItem)?.Content?.ToString() ?? "DeepSeek",
                    ["CreativityLevel"] = CreativitySlider.Value,
                    ["StyleIntensity"] = ((ComboBoxItem)StyleIntensityComboBox.SelectedItem)?.Content?.ToString() ?? "中度",
                    ["SegmentedGeneration"] = SegmentedGenerationCheckBox.IsChecked == true,
                    ["RealTimePreview"] = RealTimePreviewCheckBox.IsChecked == true,
                    ["QuickMode"] = useQuickMode,
                    // 添加项目上下文数据
                    ["ProjectId"] = contextData.ProjectId,
                    ["ProjectName"] = contextData.ProjectName,
                    ["ProjectDescription"] = contextData.ProjectDescription,
                    ["PromptSummary"] = contextData.PromptSummary,
                    ["PlotOutlines"] = contextData.PlotOutlines,
                    ["MainCharacters"] = contextData.MainCharacters,
                    ["WorldSettings"] = contextData.WorldSettings
                };

                StatusTextBlock.Text = "正在调用AI服务...";
                GenerationProgressBar.Value = 30;

                // 调用AI服务生成内容
                if (_aiAssistantService != null)
                {
                    StatusTextBlock.Text = "AI正在生成章节内容...";
                    GenerationProgressBar.Value = 50;

                    // 使用AI助手服务生成章节内容
                    var result = await _aiAssistantService.GenerateChapterAsync(parameters);

                    GenerationProgressBar.Value = 80;
                    StatusTextBlock.Text = "正在处理生成结果...";

                    if (result.IsSuccess && result.Data != null)
                    {
                        try
                        {
                            // 直接使用返回的文本内容
                            GeneratedContent = result.Data.ToString() ?? "";
                            _lastGenerationMetadata = result.Metadata ?? new Dictionary<string, object>();
                            GeneratedContentTextBox.Text = GeneratedContent;
                            UpdateGeneratedWordCount();
                            UpdateRealTimeStatistics();
                            UpdateQualityScore();
                            UpdateRwkvDebugInfo(_lastGenerationMetadata);

                            GenerationProgressBar.Value = 100;
                            StatusTextBlock.Text = "章节生成完成！";

                            // 延迟隐藏进度条
                            await Task.Delay(1000);
                            GenerationProgressBar.Visibility = Visibility.Collapsed;
                            StatusTextBlock.Visibility = Visibility.Collapsed;

                            if (!useQuickMode)
                            {
                                MessageBox.Show("章节生成完成！", "成功",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        catch (Exception updateEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"更新生成内容失败: {updateEx.Message}");

                            // 即使更新失败，也要隐藏进度条
                            GenerationProgressBar.Visibility = Visibility.Collapsed;
                            StatusTextBlock.Visibility = Visibility.Collapsed;

                            MessageBox.Show($"内容生成成功，但界面更新失败：{updateEx.Message}", "警告",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    else
                    {
                        ClearRwkvDebugInfo();
                        GenerationProgressBar.Visibility = Visibility.Collapsed;
                        StatusTextBlock.Visibility = Visibility.Collapsed;
                        MessageBox.Show($"章节生成失败：{result.Message}", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    StatusTextBlock.Text = "AI服务不可用";
                    ClearRwkvDebugInfo();
                    GenerationProgressBar.Visibility = Visibility.Collapsed;
                    StatusTextBlock.Visibility = Visibility.Collapsed;
                    MessageBox.Show("AI服务未初始化，无法生成章节内容。请先在AI配置中启用可用模型。", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                ClearRwkvDebugInfo();
                MessageBox.Show($"生成章节内容失败：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isGenerating = false;
                GenerateButton.IsEnabled = true;
                QuickGenerateButton.IsEnabled = true;
                GenerateButton.Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new MaterialDesignThemes.Wpf.PackIcon { Kind = MaterialDesignThemes.Wpf.PackIconKind.RobotExcited, Margin = new Thickness(0,0,8,0) },
                        new TextBlock { Text = "开始生成" }
                    }
                };

                // 确保进度条隐藏
                if (GenerationProgressBar.Visibility == Visibility.Visible)
                {
                    GenerationProgressBar.Visibility = Visibility.Collapsed;
                    StatusTextBlock.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 构建章节生成提示词
        /// </summary>
        private string BuildChapterPrompt(Dictionary<string, object> parameters)
        {
            var title = parameters.GetValueOrDefault("ChapterTitle", "")?.ToString() ?? "";
            var style = parameters.GetValueOrDefault("WritingStyle", "古风仙侠")?.ToString() ?? "古风仙侠";
            var type = parameters.GetValueOrDefault("ChapterType", "正文章节")?.ToString() ?? "正文章节";
            var wordCount = parameters.GetValueOrDefault("TargetWordCount", "2000")?.ToString() ?? "2000";
            var outline = parameters.GetValueOrDefault("ChapterOutline", "")?.ToString() ?? "";
            var keyPlots = parameters.GetValueOrDefault("KeyPlots", "")?.ToString() ?? "";
            var characters = parameters.GetValueOrDefault("Characters", "")?.ToString() ?? "";
            var requirements = parameters.GetValueOrDefault("SpecialRequirements", "")?.ToString() ?? "";

            return $@"请根据以下要求创作一个{style}风格的书籍章节：

章节标题：{title}
章节类型：{type}
目标字数：{wordCount}字
写作风格：{style}

章节大纲：
{outline}

关键剧情：
{keyPlots}

主要角色：
{characters}

特殊要求：
{requirements}

请创作一个完整的章节内容，要求：
1. 符合{style}的写作风格
2. 情节紧凑，描写生动
3. 人物性格鲜明，对话自然
4. 字数控制在{wordCount}字左右
5. 内容积极向上，符合网络文学规范

请开始创作：";
        }

        /// <summary>
        /// 更新生成内容字数统计
        /// </summary>
        private void UpdateGeneratedWordCount()
        {
            var content = GeneratedContentTextBox.Text ?? "";
            var wordCount = content.Length;
            // 假设有一个显示字数的标签，如果没有则忽略
            try
            {
                // 查找字数显示控件
                var wordCountLabel = this.FindName("GeneratedWordCountLabel") as TextBlock;
                if (wordCountLabel != null)
                {
                    wordCountLabel.Text = $"({wordCount:N0}字)";
                }
            }
            catch
            {
                // 忽略错误
            }
        }

        /// <summary>
        /// 更新字数统计
        /// </summary>
        private void UpdateWordCount()
        {
            var content = GeneratedContentTextBox.Text ?? "";
            var wordCount = content.Length;
            // 兼容旧的字数显示方法
            UpdateGeneratedWordCount();
        }

        /// <summary>
        /// 更新实时统计信息
        /// </summary>
        private void UpdateRealTimeStatistics()
        {
            try
            {
                var content = GeneratedContentTextBox?.Text ?? "";

                // 使用Dispatcher确保在UI线程中更新
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        // 字符统计
                        var characterCount = content.Length;
                        if (CharacterCountLabel != null)
                            CharacterCountLabel.Text = $"字符: {characterCount:N0}";

                        // 段落统计
                        var paragraphCount = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).Length;
                        if (ParagraphCountLabel != null)
                            ParagraphCountLabel.Text = $"段落: {paragraphCount}";

                        // 阅读时间估算（按每分钟300字计算）
                        var readingTime = Math.Ceiling((double)characterCount / 300);
                        if (ReadingTimeLabel != null)
                            ReadingTimeLabel.Text = $"阅读时间: {readingTime}分钟";
                    }
                    catch (Exception uiEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"更新统计UI失败: {uiEx.Message}");
                    }
                }));
            }
            catch (Exception ex)
            {
                // 静默处理统计更新错误，避免影响主要功能
                System.Diagnostics.Debug.WriteLine($"更新实时统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新质量评分
        /// </summary>
        private void UpdateQualityScore()
        {
            try
            {
                var content = GeneratedContentTextBox?.Text ?? "";
                if (QualityScoreLabel == null)
                    return;

                if (string.IsNullOrWhiteSpace(content))
                {
                    QualityScoreLabel.Text = "";
                    return;
                }

                // 简单的质量评分算法
                var score = CalculateContentQuality(content);
                var scoreText = score >= 90 ? "优秀" : score >= 80 ? "良好" : score >= 70 ? "一般" : "需改进";
                var color = score >= 90 ? "Green" : score >= 80 ? "Orange" : score >= 70 ? "Blue" : "Red";

                QualityScoreLabel.Text = $"质量: {scoreText}({score}分)";
                QualityScoreLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            }
            catch (Exception ex)
            {
                // 静默处理质量评分错误，避免影响主要功能
                System.Diagnostics.Debug.WriteLine($"更新质量评分失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 计算内容质量分数
        /// </summary>
        /// <param name="content">内容</param>
        /// <returns>质量分数</returns>
        private int CalculateContentQuality(string content)
        {
            var score = 60; // 基础分

            // 长度评分
            if (content.Length > 1000) score += 10;
            if (content.Length > 2000) score += 10;

            // 段落结构评分
            var paragraphs = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (paragraphs.Length > 3) score += 5;
            if (paragraphs.Length > 6) score += 5;

            // 对话检测
            if (content.Contains(""") && content.Contains(""")) score += 5;

            // 描写丰富度
            var descriptiveWords = new[] { "缓缓", "轻柔", "深深", "静静", "慢慢" };
            var descriptiveCount = descriptiveWords.Count(word => content.Contains(word));
            score += Math.Min(descriptiveCount * 2, 10);

            return Math.Min(score, 100);
        }

        private void UpdateRwkvDebugInfo(Dictionary<string, object>? metadata)
        {
            if (RwkvDebugExpander == null)
            {
                return;
            }

            metadata ??= new Dictionary<string, object>();
            var hasRwkvMetadata = string.Equals(
                GetMetadataText(metadata, "AIModel", "Engine"),
                "RWKV",
                StringComparison.OrdinalIgnoreCase) ||
                metadata.ContainsKey("RwkvSessionId") ||
                metadata.ContainsKey("RwkvBigBatchEnabled");

            if (!hasRwkvMetadata)
            {
                ClearRwkvDebugInfo();
                return;
            }

            RwkvDebugExpander.Visibility = Visibility.Visible;
            RwkvEngineInfoTextBlock.Text = BuildRwkvEngineInfo(metadata);
            RwkvSessionInfoTextBlock.Text = GetMetadataText(metadata, "RwkvSessionId", fallback: "-");
            RwkvBigBatchInfoTextBlock.Text = BuildRwkvBatchInfo(metadata);
            RwkvBigBatchSelectionTextBlock.Text = BuildRwkvSelectionInfo(metadata);
        }

        private void ClearRwkvDebugInfo()
        {
            _lastGenerationMetadata = new Dictionary<string, object>();
            if (RwkvDebugExpander == null)
            {
                return;
            }

            RwkvDebugExpander.Visibility = Visibility.Collapsed;
            RwkvEngineInfoTextBlock.Text = "-";
            RwkvSessionInfoTextBlock.Text = "-";
            RwkvBigBatchInfoTextBlock.Text = "-";
            RwkvBigBatchSelectionTextBlock.Text = "-";
        }

        private static string BuildRwkvEngineInfo(IReadOnlyDictionary<string, object> metadata)
        {
            var quality = GetMetadataText(metadata, "Quality");
            var temperature = GetMetadataText(metadata, "Temperature");
            var topP = GetMetadataText(metadata, "TopP");
            var parts = new List<string> { "RWKV" };

            if (!string.IsNullOrWhiteSpace(quality))
            {
                parts.Add(quality);
            }

            if (!string.IsNullOrWhiteSpace(temperature))
            {
                parts.Add($"Temp={temperature}");
            }

            if (!string.IsNullOrWhiteSpace(topP))
            {
                parts.Add($"TopP={topP}");
            }

            return string.Join(" | ", parts);
        }

        private static string BuildRwkvBatchInfo(IReadOnlyDictionary<string, object> metadata)
        {
            var enabled = GetMetadataText(metadata, "RwkvBigBatchEnabled", fallback: "false");
            var selectedRounds = GetMetadataText(metadata, "RwkvBigBatchSelectedRounds", fallback: "0");
            var fallbackRounds = GetMetadataText(metadata, "RwkvBigBatchFallbackRounds", fallback: "0");
            return $"启用={enabled}，命中轮次={selectedRounds}，回退轮次={fallbackRounds}";
        }

        private static string BuildRwkvSelectionInfo(IReadOnlyDictionary<string, object> metadata)
        {
            var index = GetMetadataText(metadata, "RwkvBigBatchLastSelectedIndex", fallback: "-1");
            var score = GetMetadataText(metadata, "RwkvBigBatchLastScore", fallback: "0");
            return $"候选索引={index}，评分={score}";
        }

        private static string GetMetadataText(IReadOnlyDictionary<string, object> metadata, string key, string? alternateKey = null, string fallback = "")
        {
            if (metadata.TryGetValue(key, out var value) && value != null)
            {
                return value.ToString() ?? fallback;
            }

            if (!string.IsNullOrWhiteSpace(alternateKey) &&
                metadata.TryGetValue(alternateKey, out var alternateValue) &&
                alternateValue != null)
            {
                return alternateValue.ToString() ?? fallback;
            }

            return fallback;
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

        private ChapterWriteTemplate BuildTemplate(string name)
        {
            return new ChapterWriteTemplate
            {
                Name = name,
                ChapterTitle = ChapterTitleTextBox.Text?.Trim() ?? string.Empty,
                WritingStyle = (WritingStyleComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty,
                ChapterType = (ChapterTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty,
                TargetWordCount = TargetWordCountTextBox.Text?.Trim() ?? string.Empty,
                ChapterOutline = ChapterOutlineTextBox.Text?.Trim() ?? string.Empty,
                KeyPlots = KeyPlotsTextBox.Text?.Trim() ?? string.Empty,
                Characters = CharactersTextBox.Text?.Trim() ?? string.Empty,
                SpecialRequirements = SpecialRequirementsTextBox.Text?.Trim() ?? string.Empty,
                AIModel = (AIModelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty,
                Creativity = CreativitySlider.Value,
                StyleIntensity = (StyleIntensityComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty,
                SegmentedGeneration = SegmentedGenerationCheckBox.IsChecked == true,
                RealTimePreview = RealTimePreviewCheckBox.IsChecked == true,
                GeneratedContent = GeneratedContentTextBox.Text ?? string.Empty,
                SavedAt = DateTime.Now
            };
        }

        private void ApplyTemplate(ChapterWriteTemplate template)
        {
            ChapterTitleTextBox.Text = template.ChapterTitle;
            TargetWordCountTextBox.Text = template.TargetWordCount;
            CharactersTextBox.Text = template.Characters;
            ChapterOutlineTextBox.Text = template.ChapterOutline;
            KeyPlotsTextBox.Text = template.KeyPlots;
            SpecialRequirementsTextBox.Text = template.SpecialRequirements;
            GeneratedContentTextBox.Text = template.GeneratedContent;
            CreativitySlider.Value = Math.Clamp(template.Creativity, CreativitySlider.Minimum, CreativitySlider.Maximum);
            SegmentedGenerationCheckBox.IsChecked = template.SegmentedGeneration;
            RealTimePreviewCheckBox.IsChecked = template.RealTimePreview;

            SetComboBoxSelection(AIModelComboBox, template.AIModel);
            SetComboBoxSelection(WritingStyleComboBox, template.WritingStyle);
            SetComboBoxSelection(ChapterTypeComboBox, template.ChapterType);
            SetComboBoxSelection(StyleIntensityComboBox, template.StyleIntensity);

            UpdateGeneratedWordCount();
            UpdateRealTimeStatistics();
            UpdateQualityScore();
            ClearRwkvDebugInfo();
        }

        private ChapterWriteTemplate? LoadTemplateFromFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ChapterWriteTemplate>(json);
        }

        private static void SetComboBoxSelection(ComboBox comboBox, string content)
        {
            foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
            {
                if (string.Equals(item.Content?.ToString(), content, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private static string GetChapterTemplateDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NovelManagement",
                "config",
                "chapter-templates");
        }

        private static string SanitizeFileName(string name)
        {
            return string.Concat(name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        }

        #endregion
    }

    internal sealed class ChapterWriteTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string ChapterTitle { get; set; } = string.Empty;
        public string WritingStyle { get; set; } = string.Empty;
        public string ChapterType { get; set; } = string.Empty;
        public string TargetWordCount { get; set; } = string.Empty;
        public string ChapterOutline { get; set; } = string.Empty;
        public string KeyPlots { get; set; } = string.Empty;
        public string Characters { get; set; } = string.Empty;
        public string SpecialRequirements { get; set; } = string.Empty;
        public string AIModel { get; set; } = string.Empty;
        public double Creativity { get; set; }
        public string StyleIntensity { get; set; } = string.Empty;
        public bool SegmentedGeneration { get; set; }
        public bool RealTimePreview { get; set; }
        public string GeneratedContent { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; }
    }

    internal sealed class ChapterTemplateManagementDialog : Window
    {
        private readonly string _templateDirectory;
        private readonly ListBox _templateListBox;
        public string? SelectedTemplatePath { get; private set; }

        public ChapterTemplateManagementDialog(string templateDirectory)
        {
            _templateDirectory = templateDirectory;
            Title = "章节模板管理";
            Width = 520;
            Height = 420;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _templateListBox = new ListBox { Margin = new Thickness(0, 0, 0, 12) };
            RefreshTemplates();

            var panel = new DockPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock
            {
                Text = "请选择要加载或删除的章节模板：",
                Margin = new Thickness(0, 0, 0, 12)
            });

            DockPanel.SetDock(_templateListBox, Dock.Top);
            panel.Children.Add(_templateListBox);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var openFolderButton = new Button { Content = "打开目录", Width = 84, Margin = new Thickness(0, 0, 12, 0) };
            openFolderButton.Click += (_, _) =>
            {
                Directory.CreateDirectory(_templateDirectory);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _templateDirectory,
                    UseShellExecute = true
                });
            };

            var deleteButton = new Button { Content = "删除", Width = 84, Margin = new Thickness(0, 0, 12, 0) };
            deleteButton.Click += (_, _) =>
            {
                if (_templateListBox.SelectedItem is not ChapterTemplateListItem selectedItem)
                {
                    MessageBox.Show("请先选择模板。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var confirm = MessageBox.Show($"确定删除模板“{selectedItem.Name}”吗？", "确认删除",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }

                File.Delete(selectedItem.FilePath);
                RefreshTemplates();
            };

            var loadButton = new Button { Content = "加载", Width = 84, Margin = new Thickness(0, 0, 12, 0), IsDefault = true };
            loadButton.Click += (_, _) =>
            {
                if (_templateListBox.SelectedItem is not ChapterTemplateListItem selectedItem)
                {
                    MessageBox.Show("请先选择模板。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SelectedTemplatePath = selectedItem.FilePath;
                DialogResult = true;
            };

            var cancelButton = new Button { Content = "关闭", Width = 84, IsCancel = true };

            buttons.Children.Add(openFolderButton);
            buttons.Children.Add(deleteButton);
            buttons.Children.Add(loadButton);
            buttons.Children.Add(cancelButton);

            DockPanel.SetDock(buttons, Dock.Bottom);
            panel.Children.Add(buttons);
            Content = panel;
        }

        private void RefreshTemplates()
        {
            Directory.CreateDirectory(_templateDirectory);
            _templateListBox.ItemsSource = Directory.GetFiles(_templateDirectory, "*.json")
                .Select(filePath => new ChapterTemplateListItem
                {
                    Name = Path.GetFileNameWithoutExtension(filePath),
                    FilePath = filePath,
                    DisplayText = $"{Path.GetFileNameWithoutExtension(filePath)}  ({File.GetLastWriteTime(filePath):yyyy-MM-dd HH:mm})"
                })
                .OrderByDescending(item => File.GetLastWriteTime(item.FilePath))
                .ToList();
            _templateListBox.DisplayMemberPath = nameof(ChapterTemplateListItem.DisplayText);
        }
    }

    internal sealed class ChapterTemplateListItem
    {
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
    }
}
