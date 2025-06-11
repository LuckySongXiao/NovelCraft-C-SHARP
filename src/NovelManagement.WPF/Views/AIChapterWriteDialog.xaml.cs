using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
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
                // TODO: 实现模板管理对话框
                MessageBox.Show("模板管理功能正在开发中", "提示",
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
                if (string.IsNullOrWhiteSpace(GeneratedContentTextBox.Text))
                {
                    MessageBox.Show("没有内容可保存为模板", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // TODO: 实现保存模板功能
                MessageBox.Show("模板保存功能正在开发中", "提示",
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
                            GeneratedContentTextBox.Text = GeneratedContent;
                            UpdateGeneratedWordCount();
                            UpdateRealTimeStatistics();
                            UpdateQualityScore();

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
                        GenerationProgressBar.Visibility = Visibility.Collapsed;
                        StatusTextBlock.Visibility = Visibility.Collapsed;
                        MessageBox.Show($"章节生成失败：{result.Message}", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    StatusTextBlock.Text = "AI服务不可用，生成模拟内容...";
                    GenerationProgressBar.Value = 70;

                    // 如果服务不可用，使用模拟内容
                    var generatedContent = GenerateMockContent();
                    GeneratedContentTextBox.Text = generatedContent;
                    UpdateGeneratedWordCount();
                    UpdateRealTimeStatistics();

                    GenerationProgressBar.Value = 100;
                    await Task.Delay(500);
                    GenerationProgressBar.Visibility = Visibility.Collapsed;
                    StatusTextBlock.Visibility = Visibility.Collapsed;

                    MessageBox.Show("AI服务未初始化，已生成模拟内容", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
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

            return $@"请根据以下要求创作一个{style}风格的小说章节：

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
        /// 生成模拟内容
        /// </summary>
        private string GenerateMockContent()
        {
            var title = ChapterTitleTextBox.Text;
            var outline = ChapterOutlineTextBox.Text;
            var characters = CharactersTextBox.Text;
            var keyPlots = KeyPlotsTextBox.Text;

            return $@"    {title}

    天空中乌云密布，雷声阵阵。{characters.Split(',')[0].Trim()}站在山峰之上，感受着天地间涌动的恐怖威压。

    ""天劫...终于来了。""他深吸一口气，眼中闪过一丝坚定。

    {outline}

    这是他修炼路上最重要的一关，成功便能突破到更高境界，失败则可能魂飞魄散。

    {keyPlots}

    第一道雷劫从天而降，带着毁天灭地的威势。{characters.Split(',')[0].Trim()}不敢大意，立即运转体内真气，准备迎接这生死考验。

    雷光照亮了整个天空，也照亮了他坚毅的面庞。无论前路如何凶险，他都要勇敢面对，因为这是他选择的道路。

    ""来吧！""他大喝一声，迎向了那道毁灭的雷光...

    （本章节由AI根据您的设定自动生成，您可以继续编辑和完善内容）";
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

        /// <summary>
        /// 获取项目上下文数据
        /// </summary>
        /// <returns>项目上下文数据</returns>
        private async Task<ProjectContextData> GetProjectContextDataAsync()
        {
            var contextData = new ProjectContextData();

            try
            {
                var projectContextService = App.ServiceProvider?.GetService<ProjectContextService>();
                if (projectContextService?.CurrentProjectId == null)
                {
                    return contextData; // 返回空的上下文数据
                }

                var projectId = projectContextService.CurrentProjectId.Value;
                contextData.ProjectId = projectId;

                // 获取剧情大纲
                try
                {
                    var plotService = App.ServiceProvider?.GetService<PlotService>();
                    if (plotService != null)
                    {
                        var plots = await plotService.GetPlotsByProjectIdAsync(projectId);
                        contextData.PlotOutlines = plots.Take(5).Select(p => new { p.Title, p.Description, Type = p.Type }).ToList().Cast<object>().ToList();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"获取剧情大纲失败: {ex.Message}");
                }

                // 获取主要角色
                try
                {
                    var characterService = App.ServiceProvider?.GetService<CharacterService>();
                    if (characterService != null)
                    {
                        var characters = await characterService.GetCharactersByProjectIdAsync(projectId);
                        contextData.MainCharacters = characters.Take(10).Select(c => new { c.Name, c.Background, c.Type }).ToList().Cast<object>().ToList();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"获取角色数据失败: {ex.Message}");
                }

                // 获取世界设定
                try
                {
                    var worldSettingService = App.ServiceProvider?.GetService<IWorldSettingService>();
                    if (worldSettingService != null)
                    {
                        var settings = await worldSettingService.GetAllAsync(projectId);
                        contextData.WorldSettings = settings.Take(10).Select(s => new { s.Name, s.Content, s.Type }).ToList().Cast<object>().ToList();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"获取世界设定失败: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取项目上下文数据失败: {ex.Message}");
            }

            return contextData;
        }

        #endregion
    }
}
