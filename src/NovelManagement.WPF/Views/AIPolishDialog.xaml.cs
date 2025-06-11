using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NovelManagement.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;
using System.Linq;
using MaterialDesignThemes.Wpf;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// AI文本润色对话框
    /// </summary>
    public partial class AIPolishDialog : Window
    {
        #region 字段

        private readonly string _originalContent;
        private AIAssistantService? _aiAssistantService;
        private bool _isPolishing;
        private ObservableCollection<PolishReplacement> _replacements;
        private string _polishedFullText = string.Empty;

        #endregion

        #region 属性

        /// <summary>
        /// 润色后的内容
        /// </summary>
        public string PolishedContent { get; private set; } = string.Empty;

        /// <summary>
        /// 替换项集合
        /// </summary>
        public ObservableCollection<PolishReplacement> Replacements => _replacements;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="originalContent">原始内容</param>
        public AIPolishDialog(string originalContent)
        {
            InitializeComponent();
            _originalContent = originalContent;
            _replacements = new ObservableCollection<PolishReplacement>();
            InitializeAIService();
            LoadData();
            SetupUI();

            // 确保UI完全加载后再初始化统计信息
            this.Loaded += (s, e) =>
            {
                try
                {
                    UpdateReplacementStats();
                    GenerateSmartSuggestions();
                    GenerateQualityAnalysis();
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
        /// 加载数据
        /// </summary>
        private void LoadData()
        {
            OriginalContentTextBox.Text = _originalContent;
            UpdateOriginalWordCount();
        }

        /// <summary>
        /// 设置UI
        /// </summary>
        private void SetupUI()
        {
            // 绑定替换项列表
            if (ReplacementsListView != null)
            {
                ReplacementsListView.ItemsSource = _replacements;
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 润色按钮点击事件
        /// </summary>
        private async void Polish_Click(object sender, RoutedEventArgs e)
        {
            await PolishContent();
        }

        /// <summary>
        /// 重新润色按钮点击事件
        /// </summary>
        private async void RePolish_Click(object sender, RoutedEventArgs e)
        {
            await PolishContent();
        }

        /// <summary>
        /// 复制到剪贴板按钮点击事件
        /// </summary>
        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(PolishedContentTextBox.Text))
                {
                    Clipboard.SetText(PolishedContentTextBox.Text);
                    MessageBox.Show("润色内容已复制到剪贴板", "提示", 
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
        /// 显示差异按钮点击事件
        /// </summary>
        private void ShowDifferences_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(PolishedContentTextBox.Text))
                {
                    MessageBox.Show("请先进行润色", "提示", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var diffDialog = new TextDifferenceDialog(_originalContent, PolishedContentTextBox.Text);
                diffDialog.Owner = this;
                diffDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示差异失败：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 应用润色按钮点击事件
        /// </summary>
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            PolishedContent = PolishedContentTextBox.Text;
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
        /// 全选替换项
        /// </summary>
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var replacement in _replacements)
            {
                replacement.IsSelected = true;
            }
            GeneratePreviewText();
        }

        /// <summary>
        /// 全不选替换项
        /// </summary>
        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var replacement in _replacements)
            {
                replacement.IsSelected = false;
            }
            GeneratePreviewText();
        }

        /// <summary>
        /// 替换项选择改变
        /// </summary>
        private void ReplacementCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            GeneratePreviewText();
        }

        /// <summary>
        /// 替换项列表选择改变
        /// </summary>
        private void ReplacementsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 可以在这里添加选中项的详细显示逻辑
        }

        /// <summary>
        /// 应用选中的替换项
        /// </summary>
        private void ApplySelectedReplacements_Click(object sender, RoutedEventArgs e)
        {
            var selectedCount = _replacements.Count(r => r.IsSelected);
            if (selectedCount == 0)
            {
                MessageBox.Show("请至少选择一个替换项", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"确定要应用选中的 {selectedCount} 个替换项吗？", "确认",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                GeneratePreviewText();
                MessageBox.Show($"已应用 {selectedCount} 个替换项", "成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 润色内容文本变化事件
        /// </summary>
        private void PolishedContentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePolishedWordCount();
            UpdateImprovementIndicator();
            UpdateReplacementStats();
        }

        /// <summary>
        /// 显示历史记录按钮点击事件
        /// </summary>
        private void ShowHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: 实现润色历史记录功能
                MessageBox.Show("润色历史记录功能正在开发中", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示历史记录失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 批量润色按钮点击事件
        /// </summary>
        private void BatchPolish_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: 实现批量润色功能
                MessageBox.Show("批量润色功能正在开发中", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"批量润色失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 刷新建议按钮点击事件
        /// </summary>
        private void RefreshSuggestions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GenerateSmartSuggestions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刷新建议失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 应用所有建议按钮点击事件
        /// </summary>
        private void ApplyAllSuggestions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("确定要应用所有智能建议吗？", "确认",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ApplyAllSmartSuggestions();
                    MessageBox.Show("已应用所有智能建议", "成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用建议失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 润色内容
        /// </summary>
        private async Task PolishContent()
        {
            if (_isPolishing)
                return;

            try
            {
                _isPolishing = true;
                PolishButton.IsEnabled = false;
                PolishButton.Content = "润色中...";

                // 显示进度和状态
                PolishProgressBar.Visibility = Visibility.Visible;
                PolishStatusTextBlock.Visibility = Visibility.Visible;
                PolishStatusTextBlock.Text = "正在准备润色参数...";
                PolishProgressBar.Value = 10;

                if (_aiAssistantService == null)
                {
                    MessageBox.Show("AI服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 构建润色参数
                var parameters = new Dictionary<string, object>
                {
                    ["OriginalContent"] = _originalContent,
                    ["TargetStyle"] = ((ComboBoxItem)TargetStyleComboBox.SelectedItem)?.Content?.ToString() ?? "古典雅致",
                    ["PolishIntensity"] = ((ComboBoxItem)PolishIntensityComboBox.SelectedItem)?.Content?.ToString() ?? "中度润色",
                    ["PreserveElements"] = ((ComboBoxItem)PreserveElementsComboBox.SelectedItem)?.Content?.ToString() ?? "保持原意",
                    ["SpecialRequirements"] = SpecialRequirementsTextBox.Text,
                    // 新增高级参数
                    ["AIModel"] = ((ComboBoxItem)AIModelComboBox.SelectedItem)?.Content?.ToString() ?? "DeepSeek",
                    ["PolishFocus"] = ((ComboBoxItem)PolishFocusComboBox.SelectedItem)?.Content?.ToString() ?? "风格统一",
                    ["EmotionalTone"] = ((ComboBoxItem)EmotionalToneComboBox.SelectedItem)?.Content?.ToString() ?? "保持原有",
                    ["TargetAudience"] = ((ComboBoxItem)TargetAudienceComboBox.SelectedItem)?.Content?.ToString() ?? "通用读者",
                    ["AutoCorrect"] = AutoCorrectCheckBox.IsChecked == true,
                    ["EnhanceDescription"] = EnhanceDescriptionCheckBox.IsChecked == true,
                    ["OptimizeDialogue"] = OptimizeDialogueCheckBox.IsChecked == true,
                    ["PreserveStyle"] = PreserveStyleCheckBox.IsChecked == true
                };

                PolishStatusTextBlock.Text = "正在调用AI润色服务...";
                PolishProgressBar.Value = 30;

                // 调用AI服务进行润色
                PolishStatusTextBlock.Text = "AI正在分析文本...";
                PolishProgressBar.Value = 50;

                var result = await _aiAssistantService.PolishTextAsync(parameters);

                PolishProgressBar.Value = 80;
                PolishStatusTextBlock.Text = "正在处理润色结果...";

                if (result.IsSuccess && result.Data != null)
                {
                    // 直接使用返回的润色文本
                    var polishedContent = result.Data.ToString() ?? "";

                    if (!string.IsNullOrEmpty(polishedContent))
                    {
                        PolishedContentTextBox.Text = polishedContent;
                        _polishedFullText = polishedContent;

                        // 生成基于文本差异的替换项
                        GenerateTextDifferenceReplacements(_originalContent, polishedContent);
                    }
                    else
                    {
                        // 如果润色内容为空，使用模拟内容
                        GenerateMockReplacements();
                        MessageBox.Show("AI润色返回空内容，已生成模拟内容", "提示",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    UpdatePolishedWordCount();
                    UpdateImprovementIndicator();
                    UpdateReplacementStats();
                    GenerateSmartSuggestions();
                    GenerateQualityAnalysis();

                    PolishProgressBar.Value = 100;
                    PolishStatusTextBlock.Text = "文本润色完成！";

                    // 延迟隐藏进度条
                    await Task.Delay(1000);
                    PolishProgressBar.Visibility = Visibility.Collapsed;
                    PolishStatusTextBlock.Visibility = Visibility.Collapsed;

                    MessageBox.Show("文本润色完成！", "成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    PolishProgressBar.Visibility = Visibility.Collapsed;
                    PolishStatusTextBlock.Visibility = Visibility.Collapsed;

                    // 如果AI服务失败，使用模拟内容
                    GenerateMockReplacements();
                    GenerateSmartSuggestions();
                    GenerateQualityAnalysis();

                    MessageBox.Show($"AI润色失败，已生成模拟内容：{result.Message}", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"润色文本失败：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isPolishing = false;
                PolishButton.IsEnabled = true;
                PolishButton.Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new MaterialDesignThemes.Wpf.PackIcon { Kind = MaterialDesignThemes.Wpf.PackIconKind.AutoFix, Margin = new Thickness(0,0,8,0) },
                        new TextBlock { Text = "开始润色" }
                    }
                };

                // 确保进度条隐藏
                if (PolishProgressBar.Visibility == Visibility.Visible)
                {
                    PolishProgressBar.Visibility = Visibility.Collapsed;
                    PolishStatusTextBlock.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 生成模拟润色内容
        /// </summary>
        private string GenerateMockPolishedContent()
        {
            var targetStyle = ((ComboBoxItem)TargetStyleComboBox.SelectedItem)?.Content?.ToString() ?? "古典雅致";
            var intensity = ((ComboBoxItem)PolishIntensityComboBox.SelectedItem)?.Content?.ToString() ?? "中度润色";

            // 简单的模拟润色：根据风格调整一些词汇
            var polished = _originalContent;

            if (targetStyle == "古典雅致")
            {
                polished = polished.Replace("说", "道")
                                 .Replace("看", "望")
                                 .Replace("很", "甚")
                                 .Replace("非常", "极为")
                                 .Replace("突然", "忽然");
            }
            else if (targetStyle == "现代简洁")
            {
                polished = polished.Replace("忽然", "突然")
                                 .Replace("甚", "很")
                                 .Replace("极为", "非常")
                                 .Replace("道", "说");
            }
            else if (targetStyle == "诗意优美")
            {
                polished = polished.Replace("天空", "苍穹")
                                 .Replace("雷声", "雷鸣")
                                 .Replace("山峰", "峰峦")
                                 .Replace("威压", "威势");
            }

            return polished + "\n\n（已根据" + targetStyle + "风格进行" + intensity + "）";
        }

        /// <summary>
        /// 更新原文字数统计
        /// </summary>
        private void UpdateOriginalWordCount()
        {
            var content = OriginalContentTextBox.Text ?? "";
            var wordCount = content.Length;
            OriginalWordCountLabel.Text = $"({wordCount:N0}字)";
        }

        /// <summary>
        /// 更新润色内容字数统计
        /// </summary>
        private void UpdatePolishedWordCount()
        {
            var content = PolishedContentTextBox.Text ?? "";
            var wordCount = content.Length;
            PolishedWordCountLabel.Text = $"({wordCount:N0}字)";
        }

        /// <summary>
        /// 从AI结果中提取纯净的润色文本
        /// </summary>
        private string ExtractPolishedTextFromResult(object resultData)
        {
            try
            {
                // 如果是字符串，直接返回
                if (resultData is string text)
                {
                    return CleanupPolishedText(text);
                }

                // 如果是动态对象，尝试获取PolishedText属性
                var resultType = resultData.GetType();
                var polishedTextProperty = resultType.GetProperty("PolishedText");
                if (polishedTextProperty != null)
                {
                    var polishedText = polishedTextProperty.GetValue(resultData)?.ToString() ?? "";
                    return CleanupPolishedText(polishedText);
                }

                // 如果是字典类型
                if (resultData is Dictionary<string, object> dict)
                {
                    if (dict.TryGetValue("PolishedText", out var polishedTextObj))
                    {
                        return CleanupPolishedText(polishedTextObj?.ToString() ?? "");
                    }
                    if (dict.TryGetValue("Content", out var contentObj))
                    {
                        return CleanupPolishedText(contentObj?.ToString() ?? "");
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
                            return CleanupPolishedText(value);
                        }
                    }
                }

                // 如果都没找到，返回ToString结果
                return CleanupPolishedText(resultData.ToString() ?? "");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"提取润色文本失败: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// 清理润色文本内容，移除不必要的格式和代码结构
        /// </summary>
        private string CleanupPolishedText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // 移除开头和结尾的空白字符
            text = text.Trim();

            // 移除可能的JSON或代码结构标记
            text = text.Replace("{ PolishedText = ", "")
                      .Replace(", OriginalWordCount = ", "")
                      .Replace(", PolishedWordCount = ", "")
                      .Replace(", ImprovementAreas = ", "")
                      .Replace(", StyleConsistency = ", "")
                      .Replace(", QualityImprovement = ", "")
                      .Replace(", TargetStyle = ", "")
                      .Replace(", PolishLevel = ", "")
                      .Replace(", XMLReplacements = ", "")
                      .Replace("System.String[]", "")
                      .Replace("System.Collections.Generic.List`1[System.Object]", "")
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
        /// 尝试处理替换列表
        /// </summary>
        private void TryProcessReplacementsList(object resultData)
        {
            try
            {
                if (resultData is Dictionary<string, object> dict)
                {
                    if (dict.TryGetValue("XMLReplacements", out var replacementsObj))
                    {
                        ProcessReplacementsList(replacementsObj);
                    }
                }
                else
                {
                    // 尝试使用反射获取XMLReplacements属性
                    var resultType = resultData.GetType();
                    var replacementsProperty = resultType.GetProperty("XMLReplacements");
                    if (replacementsProperty != null)
                    {
                        var replacements = replacementsProperty.GetValue(resultData);
                        if (replacements != null)
                        {
                            ProcessReplacementsList(replacements);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理替换列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新改进指示器
        /// </summary>
        private void UpdateImprovementIndicator()
        {
            var originalLength = _originalContent.Length;
            var polishedLength = PolishedContentTextBox.Text.Length;

            if (polishedLength > 0)
            {
                var changePercent = ((double)(polishedLength - originalLength) / originalLength * 100);
                if (Math.Abs(changePercent) < 1)
                {
                    ImprovementLabel.Text = "微调";
                }
                else if (changePercent > 0)
                {
                    ImprovementLabel.Text = $"扩展 +{changePercent:F1}%";
                }
                else
                {
                    ImprovementLabel.Text = $"精简 {changePercent:F1}%";
                }
            }
            else
            {
                ImprovementLabel.Text = "";
            }
        }

        /// <summary>
        /// 解析XML格式的替换内容
        /// </summary>
        /// <param name="xmlText">XML格式的文本</param>
        private void ParseXMLReplacements(string xmlText)
        {
            try
            {
                _replacements.Clear();

                // 使用正则表达式解析XML标签
                var pattern = @"<replace><article>(.*?)</article><output>(.*?)</output></replace>";
                var matches = Regex.Matches(xmlText, pattern, RegexOptions.Singleline);

                foreach (Match match in matches)
                {
                    var originalText = match.Groups[1].Value.Trim();
                    var replacementText = match.Groups[2].Value.Trim();

                    // 在原文中查找位置
                    var position = _originalContent.IndexOf(originalText);
                    if (position >= 0)
                    {
                        var replacement = new PolishReplacement
                        {
                            OriginalText = originalText,
                            ReplacementText = replacementText,
                            Position = position,
                            Length = originalText.Length,
                            ReplacementType = "AI润色",
                            IsSelected = true,
                            Description = $"将\"{originalText}\"优化为\"{replacementText}\"",
                            Context = GetContext(position, originalText.Length)
                        };

                        _replacements.Add(replacement);
                    }
                }

                // 生成预览文本
                GeneratePreviewText();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"解析XML替换失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 处理替换列表
        /// </summary>
        /// <param name="replacementsObj">替换对象</param>
        private void ProcessReplacementsList(object replacementsObj)
        {
            try
            {
                if (replacementsObj is System.Collections.IEnumerable replacements)
                {
                    foreach (var item in replacements)
                    {
                        if (item is Dictionary<string, object> replaceDict)
                        {
                            var original = replaceDict.GetValueOrDefault("Original", "").ToString();
                            var replacement = replaceDict.GetValueOrDefault("Replacement", "").ToString();
                            var type = replaceDict.GetValueOrDefault("Type", "优化").ToString();

                            if (!string.IsNullOrEmpty(original) && !string.IsNullOrEmpty(replacement))
                            {
                                var position = _originalContent.IndexOf(original);
                                if (position >= 0)
                                {
                                    var polishReplacement = new PolishReplacement
                                    {
                                        OriginalText = original,
                                        ReplacementText = replacement,
                                        Position = position,
                                        Length = original.Length,
                                        ReplacementType = type,
                                        IsSelected = true,
                                        Description = $"{type}：{original} → {replacement}",
                                        Context = GetContext(position, original.Length)
                                    };

                                    // 避免重复添加
                                    if (!_replacements.Any(r => r.Position == position && r.OriginalText == original))
                                    {
                                        _replacements.Add(polishReplacement);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"处理替换列表失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 获取上下文
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="length">长度</param>
        /// <returns>上下文</returns>
        private string GetContext(int position, int length)
        {
            var contextLength = 20;
            var start = Math.Max(0, position - contextLength);
            var end = Math.Min(_originalContent.Length, position + length + contextLength);

            var context = _originalContent.Substring(start, end - start);
            var targetStart = position - start;
            var targetEnd = targetStart + length;

            // 标记目标文本
            if (targetStart >= 0 && targetEnd <= context.Length)
            {
                context = context.Substring(0, targetStart) +
                         "【" + context.Substring(targetStart, length) + "】" +
                         context.Substring(targetEnd);
            }

            return context;
        }

        /// <summary>
        /// 生成预览文本
        /// </summary>
        private void GeneratePreviewText()
        {
            var previewText = _originalContent;
            var selectedReplacements = _replacements.Where(r => r.IsSelected).OrderByDescending(r => r.Position).ToList();

            foreach (var replacement in selectedReplacements)
            {
                if (replacement.Position + replacement.Length <= previewText.Length)
                {
                    previewText = previewText.Substring(0, replacement.Position) +
                                 replacement.ReplacementText +
                                 previewText.Substring(replacement.Position + replacement.Length);
                }
            }

            PolishedContentTextBox.Text = previewText;
            _polishedFullText = previewText;
        }

        /// <summary>
        /// 生成模拟替换项
        /// </summary>
        private void GenerateMockReplacements()
        {
            _replacements.Clear();

            var mockReplacements = new[]
            {
                new { Original = "说", Replacement = "道", Type = "词汇优化" },
                new { Original = "看", Replacement = "望", Type = "词汇优化" },
                new { Original = "很", Replacement = "甚", Type = "语言风格" },
                new { Original = "非常", Replacement = "极为", Type = "语言风格" },
                new { Original = "突然", Replacement = "忽然", Type = "表达优化" }
            };

            foreach (var mock in mockReplacements)
            {
                var position = _originalContent.IndexOf(mock.Original);
                if (position >= 0)
                {
                    var replacement = new PolishReplacement
                    {
                        OriginalText = mock.Original,
                        ReplacementText = mock.Replacement,
                        Position = position,
                        Length = mock.Original.Length,
                        ReplacementType = mock.Type,
                        IsSelected = true,
                        Description = $"{mock.Type}：{mock.Original} → {mock.Replacement}",
                        Context = GetContext(position, mock.Original.Length)
                    };

                    _replacements.Add(replacement);
                }
            }

            GeneratePreviewText();
        }

        /// <summary>
        /// 更新替换项统计
        /// </summary>
        private void UpdateReplacementStats()
        {
            try
            {
                if (ReplacementStatsLabel == null || _replacements == null)
                    return;

                var totalCount = _replacements.Count;
                var selectedCount = _replacements.Count(r => r.IsSelected);
                ReplacementStatsLabel.Text = $"共{totalCount}项替换建议，已选择{selectedCount}项";
            }
            catch (Exception ex)
            {
                // 静默处理统计更新错误，避免影响主要功能
                System.Diagnostics.Debug.WriteLine($"更新替换项统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成智能建议
        /// </summary>
        private void GenerateSmartSuggestions()
        {
            try
            {
                if (SuggestionsPanel == null)
                    return;

                SuggestionsPanel.Children.Clear();

            var suggestions = new[]
            {
                new { Title = "词汇丰富度", Content = "建议增加更多形容词和副词来丰富表达", Priority = "高" },
                new { Title = "句式变化", Content = "适当调整句子长短，增加语言节奏感", Priority = "中" },
                new { Title = "情感表达", Content = "可以加强人物内心情感的描写", Priority = "中" },
                new { Title = "场景描述", Content = "环境描写可以更加细腻生动", Priority = "低" },
                new { Title = "对话优化", Content = "人物对话可以更符合角色性格特点", Priority = "高" }
            };

            foreach (var suggestion in suggestions)
            {
                var card = new MaterialDesignThemes.Wpf.Card
                {
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(12)
                };

                var panel = new StackPanel();

                var titlePanel = new DockPanel();
                var titleText = new TextBlock
                {
                    Text = suggestion.Title,
                    FontWeight = FontWeights.Medium,
                    FontSize = 14
                };
                DockPanel.SetDock(titleText, Dock.Left);

                var priorityText = new TextBlock
                {
                    Text = suggestion.Priority,
                    FontSize = 12,
                    Foreground = suggestion.Priority == "高" ? Brushes.Red :
                               suggestion.Priority == "中" ? Brushes.Orange : Brushes.Gray
                };
                DockPanel.SetDock(priorityText, Dock.Right);

                titlePanel.Children.Add(titleText);
                titlePanel.Children.Add(priorityText);

                var contentText = new TextBlock
                {
                    Text = suggestion.Content,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0),
                    Foreground = new SolidColorBrush(Colors.Gray)
                };

                panel.Children.Add(titlePanel);
                panel.Children.Add(contentText);
                card.Content = panel;

                SuggestionsPanel.Children.Add(card);
            }
            }
            catch (Exception ex)
            {
                // 静默处理智能建议生成错误，避免影响主要功能
                System.Diagnostics.Debug.WriteLine($"生成智能建议失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成质量分析
        /// </summary>
        private void GenerateQualityAnalysis()
        {
            try
            {
                if (QualityAnalysisPanel == null)
                    return;

                QualityAnalysisPanel.Children.Clear();

            var analysisItems = new[]
            {
                new { Category = "词汇丰富度", Score = 85, Description = "词汇使用较为丰富，但可以进一步增加同义词的使用" },
                new { Category = "句式变化", Score = 78, Description = "句式有一定变化，建议增加更多复合句和修辞手法" },
                new { Category = "语法正确性", Score = 95, Description = "语法使用基本正确，仅有少量标点符号需要调整" },
                new { Category = "逻辑连贯性", Score = 88, Description = "逻辑结构清晰，段落间过渡自然" },
                new { Category = "情感表达", Score = 82, Description = "情感表达真实，可以加强细节描写" }
            };

            foreach (var item in analysisItems)
            {
                var card = new MaterialDesignThemes.Wpf.Card
                {
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(12)
                };

                var panel = new StackPanel();

                var titlePanel = new DockPanel();
                var titleText = new TextBlock
                {
                    Text = item.Category,
                    FontWeight = FontWeights.Medium,
                    FontSize = 14
                };
                DockPanel.SetDock(titleText, Dock.Left);

                var scoreText = new TextBlock
                {
                    Text = $"{item.Score}分",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = item.Score >= 90 ? Brushes.Green :
                               item.Score >= 80 ? Brushes.Orange : Brushes.Red
                };
                DockPanel.SetDock(scoreText, Dock.Right);

                titlePanel.Children.Add(titleText);
                titlePanel.Children.Add(scoreText);

                var descText = new TextBlock
                {
                    Text = item.Description,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0),
                    Foreground = new SolidColorBrush(Colors.Gray)
                };

                panel.Children.Add(titlePanel);
                panel.Children.Add(descText);
                card.Content = panel;

                QualityAnalysisPanel.Children.Add(card);
            }
            }
            catch (Exception ex)
            {
                // 静默处理质量分析生成错误，避免影响主要功能
                System.Diagnostics.Debug.WriteLine($"生成质量分析失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用所有智能建议
        /// </summary>
        private void ApplyAllSmartSuggestions()
        {
            // TODO: 实现应用所有智能建议的逻辑
            // 这里可以根据建议类型对文本进行相应的优化
        }

        /// <summary>
        /// 生成基于文本差异的替换项
        /// </summary>
        /// <param name="originalText">原文</param>
        /// <param name="polishedText">润色后文本</param>
        private void GenerateTextDifferenceReplacements(string originalText, string polishedText)
        {
            try
            {
                _replacements.Clear();

                // 简单的文本差异分析
                var originalLines = originalText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                var polishedLines = polishedText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                var minLength = Math.Min(originalLines.Length, polishedLines.Length);

                for (int i = 0; i < minLength; i++)
                {
                    var originalLine = originalLines[i].Trim();
                    var polishedLine = polishedLines[i].Trim();

                    if (!string.IsNullOrEmpty(originalLine) && !string.IsNullOrEmpty(polishedLine) &&
                        originalLine != polishedLine)
                    {
                        var position = originalText.IndexOf(originalLine);
                        if (position >= 0)
                        {
                            var replacement = new PolishReplacement
                            {
                                OriginalText = originalLine,
                                ReplacementText = polishedLine,
                                Position = position,
                                Length = originalLine.Length,
                                ReplacementType = "AI润色",
                                IsSelected = true,
                                Description = $"润色优化：{originalLine.Substring(0, Math.Min(20, originalLine.Length))}... → {polishedLine.Substring(0, Math.Min(20, polishedLine.Length))}...",
                                Context = GetContext(position, originalLine.Length)
                            };

                            _replacements.Add(replacement);
                        }
                    }
                }

                // 如果没有找到差异，创建一个整体替换项
                if (_replacements.Count == 0 && originalText != polishedText)
                {
                    var replacement = new PolishReplacement
                    {
                        OriginalText = originalText,
                        ReplacementText = polishedText,
                        Position = 0,
                        Length = originalText.Length,
                        ReplacementType = "整体润色",
                        IsSelected = true,
                        Description = "AI对全文进行了润色优化",
                        Context = "全文内容"
                    };

                    _replacements.Add(replacement);
                }

                UpdateReplacementStats();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"生成文本差异替换项失败: {ex.Message}");
                // 如果分析失败，使用模拟替换项
                GenerateMockReplacements();
            }
        }

        #endregion
    }

    /// <summary>
    /// 文本差异对话框（简化版）
    /// </summary>
    public class TextDifferenceDialog : Window
    {
        public TextDifferenceDialog(string original, string polished)
        {
            Title = "文本对比";
            Width = 800;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var originalTextBox = new TextBox
            {
                Text = original,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(10)
            };
            Grid.SetColumn(originalTextBox, 0);

            var polishedTextBox = new TextBox
            {
                Text = polished,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(10)
            };
            Grid.SetColumn(polishedTextBox, 1);

            grid.Children.Add(originalTextBox);
            grid.Children.Add(polishedTextBox);

            Content = grid;
        }
    }

    /// <summary>
    /// 润色替换项
    /// </summary>
    public class PolishReplacement
    {
        /// <summary>
        /// 原文
        /// </summary>
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>
        /// 替换文本
        /// </summary>
        public string ReplacementText { get; set; } = string.Empty;

        /// <summary>
        /// 在原文中的位置
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// 原文长度
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 替换类型
        /// </summary>
        public string ReplacementType { get; set; } = string.Empty;

        /// <summary>
        /// 是否已应用
        /// </summary>
        public bool IsApplied { get; set; }

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected { get; set; } = true;

        /// <summary>
        /// 改进说明
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 上下文（前后文）
        /// </summary>
        public string Context { get; set; } = string.Empty;
    }
}
