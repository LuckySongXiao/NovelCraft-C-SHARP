using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NovelManagement.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;
using System.Linq;
using MaterialDesignThemes.Wpf;
using NovelManagement.AI.Services.RWKV;
using NovelManagement.AI.Services.RWKV.Models;

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
        private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

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
            this.Loaded += async (s, e) =>
            {
                try
                {
                    await LoadAvailableModelsAsync();
                    UpdateReplacementStats();
                    await GenerateSmartSuggestionsAsync();
                    await GenerateQualityAnalysisAsync();
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

        /// <summary>
        /// 加载真实可用的 AI 模型列表：rwkv_models 目录发现的模型文件 + RWKV 服务在线状态，
        /// 在线加载的模型默认选中；替换硬编码假选项。
        /// </summary>
        private async Task LoadAvailableModelsAsync()
        {
            AIModelComboBox.Items.Clear();

            // 查询 RWKV 服务在线状态与当前加载的模型
            var rwkvService = App.ServiceProvider?.GetService<IRwkvLightningService>();
            var serviceReady = false;
            string? onlineModel = null;
            string baseUrl = "http://localhost:8000";
            if (rwkvService != null)
            {
                baseUrl = rwkvService.Configuration.BaseUrl;
                try
                {
                    var status = await GetStatusWithFastTimeoutAsync(rwkvService);
                    serviceReady = status is { Ready: true };
                    onlineModel = string.IsNullOrWhiteSpace(status?.Model) ? null : status.Model;
                }
                catch
                {
                    // 服务离线：按离线状态填充
                }
            }

            // 扫描 rwkv_models 目录发现的模型文件
            var modelFiles = DiscoverRwkvModelFiles();

            if (modelFiles.Count == 0 && !serviceReady)
            {
                AIModelComboBox.Items.Add(new ComboBoxItem
                {
                    Content = $"RWKV（离线，{baseUrl}）",
                    Tag = "RWKV",
                    IsSelected = true
                });
                return;
            }

            foreach (var modelFile in modelFiles)
            {
                var fileName = Path.GetFileName(modelFile);
                var isOnline = serviceReady && onlineModel != null &&
                    fileName.Contains(onlineModel, StringComparison.OrdinalIgnoreCase);
                AIModelComboBox.Items.Add(new ComboBoxItem
                {
                    Content = isOnline ? $"{fileName}（当前在线）" : fileName,
                    Tag = fileName
                });
            }

            // 目录扫描为空但服务在线：至少展示在线模型
            if (AIModelComboBox.Items.Count == 0 && serviceReady)
            {
                AIModelComboBox.Items.Add(new ComboBoxItem
                {
                    Content = $"{onlineModel ?? "RWKV"}（当前在线）",
                    Tag = onlineModel ?? "RWKV"
                });
            }

            // 默认选中：优先在线加载的模型，否则第一项
            var selectedIndex = 0;
            if (serviceReady && onlineModel != null)
            {
                for (var i = 0; i < AIModelComboBox.Items.Count; i++)
                {
                    var item = (ComboBoxItem)AIModelComboBox.Items[i];
                    if (item.Content?.ToString()?.Contains("当前在线", StringComparison.Ordinal) == true)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }
            AIModelComboBox.SelectedIndex = selectedIndex;
        }

        /// <summary>支持的 RWKV 模型文件扩展名（与 AI 模型配置页发现逻辑一致，另含 llama.cpp 格式）。</summary>
        private static readonly string[] RwkvModelExtensions = { ".gguf", ".st", ".safetensors", ".pth" };

        /// <summary>
        /// 扫描 rwkv_models 目录发现的模型文件（向上查找项目根，与 AI 模型配置页一致）
        /// </summary>
        private static List<string> DiscoverRwkvModelFiles()
        {
            var results = new List<string>();
            try
            {
                var modelsDirectory = Path.Combine(ResolveProjectRoot(), "rwkv_models");
                if (!Directory.Exists(modelsDirectory))
                {
                    return results;
                }

                results.AddRange(Directory.EnumerateFiles(modelsDirectory, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(path => RwkvModelExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                    .OrderBy(path => Path.GetExtension(path).ToLowerInvariant() switch
                    {
                        ".pth" => 0,
                        ".safetensors" => 1,
                        ".st" => 2,
                        ".gguf" => 3,
                        _ => 9
                    })
                    .ThenBy(path => path, StringComparer.OrdinalIgnoreCase));
            }
            catch
            {
                // 扫描失败：返回空列表，由调用方按服务状态兜底
            }

            return results;
        }

        /// <summary>
        /// 从应用目录向上查找项目根（与 AI 模型配置页 ResolveProjectRoot 一致）
        /// </summary>
        private static string ResolveProjectRoot()
        {
            var current = AppDomain.CurrentDomain.BaseDirectory;
            for (var i = 0; i < 8; i++)
            {
                if (Directory.Exists(Path.Combine(current, "RWKV_lightning_CUDA_win")) ||
                    Directory.Exists(Path.Combine(current, "rwkv_lightning_libtorch_win")) ||
                    Directory.Exists(Path.Combine(current, "llama_cpp")))
                {
                    return current;
                }

                var parent = Directory.GetParent(current);
                if (parent == null)
                {
                    break;
                }

                current = parent.FullName;
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// 获取当前选中的模型名（Tag 存纯模型名，Content 仅展示状态）
        /// </summary>
        private string GetSelectedAiModel() =>
            (AIModelComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "RWKV";

        /// <summary>
        /// 检查本地 RWKV 推理服务是否在线且可真实推理；不可用时给出明确指引并返回 false。
        /// 两级探测均带竞速超时：服务离线/隧道假死时 GetStatusAsync 或推理可能阻塞到
        /// HttpClient 长超时（配置可达 600 秒），状态检查绝不能让用户干等。
        /// </summary>
        private async Task<bool> CheckRwkvOnlineAsync()
        {
            try
            {
                var rwkvService = App.ServiceProvider?.GetService<IRwkvLightningService>();
                if (rwkvService == null)
                {
                    MessageBox.Show("RWKV 推理服务未注册，请检查应用配置。", "AI服务不可用",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                var online = await IsRwkvReallyOnlineAsync(rwkvService);
                if (!online)
                {
                    MessageBox.Show(
                        $"RWKV 推理服务不在线或无响应（{rwkvService.Configuration.BaseUrl}）。\n\n请到「AI模型配置」页启动 RWKV 服务后再试。",
                        "AI服务不可用",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"检查 RWKV 推理服务状态失败：{ex.Message}", "AI服务不可用",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        /// <summary>
        /// 两级探测：状态端点（3 秒）+ 1-token 真实推理（6 秒）。
        /// 仅状态在线但推理黑洞（如隧道后端已死）时同样判定离线。
        /// </summary>
        internal static async Task<bool> IsRwkvReallyOnlineAsync(IRwkvLightningService rwkvService)
        {
            var status = await GetStatusWithFastTimeoutAsync(rwkvService);
            if (status is not { Ready: true })
            {
                return false;
            }

            var probeTask = rwkvService.CompleteAsync("ping", maxTokens: 1);
            var completed = await Task.WhenAny(probeTask, Task.Delay(ProbeTimeout));
            if (completed != probeTask)
            {
                return false;
            }

            return probeTask.IsCompletedSuccessfully && probeTask.Result.Success;
        }

        /// <summary>
        /// 带 3 秒竞速超时的状态探测；超时/失败均返回 null（视为离线）
        /// </summary>
        internal static async Task<RwkvStatusResponse?> GetStatusWithFastTimeoutAsync(IRwkvLightningService rwkvService)
        {
            var statusTask = rwkvService.GetStatusAsync();
            var completed = await Task.WhenAny(statusTask, Task.Delay(StatusProbeTimeout));
            if (completed != statusTask)
            {
                return null;
            }

            return statusTask.IsCompletedSuccessfully ? statusTask.Result : null;
        }

        /// <summary>状态探测竞速超时（毫秒）。</summary>
        private const int StatusProbeTimeout = 3000;

        /// <summary>真实推理探测竞速超时（毫秒）。</summary>
        private const int ProbeTimeout = 6000;

        /// <summary>
        /// 将底层连接类异常转换为可操作的中文提示
        /// </summary>
        private static string BuildFriendlyAiError(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "未知错误，请重试。";
            }

            if (message.Contains("An error occurred while sending", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("HttpRequestException", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("socket", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("connection", StringComparison.OrdinalIgnoreCase))
            {
                return "模型服务连接失败，请确认 RWKV 推理服务已启动（可在「AI模型配置」页查看状态），然后重试。";
            }

            return message;
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
                var historyDirectory = GetPolishHistoryDirectory();
                Directory.CreateDirectory(historyDirectory);

                var dialog = new PolishHistoryDialog(historyDirectory)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.SelectedHistoryPath))
                {
                    return;
                }

                var record = LoadPolishHistoryRecord(dialog.SelectedHistoryPath);
                if (record == null)
                {
                    MessageBox.Show("历史记录内容无效。", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ApplyHistoryRecord(record);
                MessageBox.Show($"已载入润色记录：{record.Title}", "加载成功",
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
        private async void BatchPolish_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await BatchPolishContentAsync();
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
        private async void RefreshSuggestions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await GenerateSmartSuggestionsAsync();
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

                // 前置检查本地 RWKV 推理服务可用性，避免等待超时后报晦涩的连接错误
                var rwkvReady = await CheckRwkvOnlineAsync();
                if (!rwkvReady)
                {
                    PolishProgressBar.Visibility = Visibility.Collapsed;
                    PolishStatusTextBlock.Visibility = Visibility.Collapsed;
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
                    // 新增高级参数（Tag 存纯模型名供 agent 判定，Content 仅展示）
                    ["AIModel"] = GetSelectedAiModel(),
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
                        PolishProgressBar.Visibility = Visibility.Collapsed;
                        PolishStatusTextBlock.Visibility = Visibility.Collapsed;
                        MessageBox.Show("AI润色返回空内容，未生成可用结果。请调整要求或切换模型后重试。", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    UpdatePolishedWordCount();
                    UpdateImprovementIndicator();
                    UpdateReplacementStats();
                    await GenerateSmartSuggestionsAsync();
                    await GenerateQualityAnalysisAsync();
                    SavePolishHistoryRecord(BuildPolishHistoryRecord());

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
                    MessageBox.Show($"AI润色失败：{BuildFriendlyAiError(result.Message)}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
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
        /// 按段落分块执行批量润色，适合较长正文。
        /// </summary>
        private async Task BatchPolishContentAsync()
        {
            if (_isPolishing)
            {
                return;
            }

            try
            {
                _isPolishing = true;
                PolishButton.IsEnabled = false;
                PolishProgressBar.Visibility = Visibility.Visible;
                PolishStatusTextBlock.Visibility = Visibility.Visible;
                PolishStatusTextBlock.Text = "正在拆分长文本...";
                PolishProgressBar.Value = 5;

                if (_aiAssistantService == null)
                {
                    MessageBox.Show("AI服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var chunks = SplitContentForBatchPolish(_originalContent);
                if (chunks.Count == 0)
                {
                    chunks.Add(_originalContent);
                }

                var polishedChunks = new List<string>(chunks.Count);

                for (var index = 0; index < chunks.Count; index++)
                {
                    var chunk = chunks[index];
                    PolishStatusTextBlock.Text = $"正在批量润色第 {index + 1}/{chunks.Count} 段...";
                    PolishProgressBar.Value = 10 + (index * 70.0 / chunks.Count);

                    var parameters = BuildPolishParameters(chunk, index + 1, chunks.Count);
                    var result = await _aiAssistantService.PolishTextAsync(parameters);
                    var polishedChunk = result.IsSuccess && result.Data != null
                        ? ExtractPolishedTextFromResult(result.Data)
                        : string.Empty;

                    if (string.IsNullOrWhiteSpace(polishedChunk))
                    {
                        throw new InvalidOperationException(
                            $"第 {index + 1} 段润色失败：{result.Message ?? "AI 返回空内容"}");
                    }

                    polishedChunks.Add(polishedChunk.Trim());
                }

                PolishStatusTextBlock.Text = "正在合并批量润色结果...";
                PolishProgressBar.Value = 85;

                var mergedContent = string.Join(Environment.NewLine + Environment.NewLine, polishedChunks
                    .Where(chunk => !string.IsNullOrWhiteSpace(chunk)));
                if (string.IsNullOrWhiteSpace(mergedContent))
                {
                    throw new InvalidOperationException("批量润色未返回可用结果。");
                }

                ApplyPolishedResult(mergedContent);
                await GenerateSmartSuggestionsAsync();
                await GenerateQualityAnalysisAsync();
                SavePolishHistoryRecord(BuildPolishHistoryRecord());

                PolishProgressBar.Value = 100;
                PolishStatusTextBlock.Text = $"批量润色完成，共处理 {chunks.Count} 段。";

                await Task.Delay(1000);
                PolishProgressBar.Visibility = Visibility.Collapsed;
                PolishStatusTextBlock.Visibility = Visibility.Collapsed;

                MessageBox.Show(
                    $"批量润色完成，共处理 {chunks.Count} 段。",
                    "成功",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                PolishProgressBar.Visibility = Visibility.Collapsed;
                PolishStatusTextBlock.Visibility = Visibility.Collapsed;
                MessageBox.Show($"批量润色失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isPolishing = false;
                PolishButton.IsEnabled = true;

                if (PolishProgressBar.Visibility == Visibility.Visible)
                {
                    PolishProgressBar.Visibility = Visibility.Collapsed;
                    PolishStatusTextBlock.Visibility = Visibility.Collapsed;
                }
            }
        }

        private Dictionary<string, object> BuildPolishParameters(string content, int chunkIndex = 1, int totalChunks = 1)
        {
            var specialRequirements = SpecialRequirementsTextBox.Text?.Trim() ?? string.Empty;
            if (totalChunks > 1)
            {
                var batchTip = $"当前为批量润色片段 {chunkIndex}/{totalChunks}，请保持与前后文风格一致，不要添加分段说明。";
                specialRequirements = string.IsNullOrWhiteSpace(specialRequirements)
                    ? batchTip
                    : $"{specialRequirements}；{batchTip}";
            }

            return new Dictionary<string, object>
            {
                ["OriginalContent"] = content,
                ["TargetStyle"] = ((ComboBoxItem)TargetStyleComboBox.SelectedItem)?.Content?.ToString() ?? "古典雅致",
                ["PolishIntensity"] = ((ComboBoxItem)PolishIntensityComboBox.SelectedItem)?.Content?.ToString() ?? "中度润色",
                ["PreserveElements"] = ((ComboBoxItem)PreserveElementsComboBox.SelectedItem)?.Content?.ToString() ?? "保持原意",
                ["SpecialRequirements"] = specialRequirements,
                ["AIModel"] = ((ComboBoxItem)AIModelComboBox.SelectedItem)?.Content?.ToString() ?? "DeepSeek",
                ["PolishFocus"] = ((ComboBoxItem)PolishFocusComboBox.SelectedItem)?.Content?.ToString() ?? "风格统一",
                ["EmotionalTone"] = ((ComboBoxItem)EmotionalToneComboBox.SelectedItem)?.Content?.ToString() ?? "保持原有",
                ["TargetAudience"] = ((ComboBoxItem)TargetAudienceComboBox.SelectedItem)?.Content?.ToString() ?? "通用读者",
                ["AutoCorrect"] = AutoCorrectCheckBox.IsChecked == true,
                ["EnhanceDescription"] = EnhanceDescriptionCheckBox.IsChecked == true,
                ["OptimizeDialogue"] = OptimizeDialogueCheckBox.IsChecked == true,
                ["PreserveStyle"] = PreserveStyleCheckBox.IsChecked == true
            };
        }

        private void ApplyPolishedResult(string polishedContent)
        {
            PolishedContentTextBox.Text = polishedContent;
            _polishedFullText = polishedContent;
            GenerateTextDifferenceReplacements(_originalContent, polishedContent);
            UpdatePolishedWordCount();
            UpdateImprovementIndicator();
            UpdateReplacementStats();
        }

        private List<string> SplitContentForBatchPolish(string content, int targetChunkLength = 1200, int maxChunkLength = 1800)
        {
            var chunks = new List<string>();
            if (string.IsNullOrWhiteSpace(content))
            {
                return chunks;
            }

            var paragraphs = Regex.Split(content.Replace("\r\n", "\n"), @"\n\s*\n")
                .Select(paragraph => paragraph.Trim())
                .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))
                .ToList();

            if (paragraphs.Count == 0)
            {
                chunks.Add(content.Trim());
                return chunks;
            }

            var currentChunk = new List<string>();
            var currentLength = 0;

            foreach (var paragraph in paragraphs)
            {
                var paragraphLength = paragraph.Length;
                var nextLength = currentLength + paragraphLength + (currentChunk.Count > 0 ? 2 : 0);
                if (currentChunk.Count > 0 && nextLength > maxChunkLength)
                {
                    chunks.Add(string.Join(Environment.NewLine + Environment.NewLine, currentChunk));
                    currentChunk.Clear();
                    currentLength = 0;
                }

                currentChunk.Add(paragraph);
                currentLength += paragraphLength + (currentChunk.Count > 1 ? 2 : 0);

                if (currentLength >= targetChunkLength)
                {
                    chunks.Add(string.Join(Environment.NewLine + Environment.NewLine, currentChunk));
                    currentChunk.Clear();
                    currentLength = 0;
                }
            }

            if (currentChunk.Count > 0)
            {
                chunks.Add(string.Join(Environment.NewLine + Environment.NewLine, currentChunk));
            }

            return chunks.Where(chunk => !string.IsNullOrWhiteSpace(chunk)).ToList();
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
        /// 生成智能建议（优先调用AI服务，失败时降级为预设参考建议）
        /// </summary>
        private async Task GenerateSmartSuggestionsAsync()
        {
            try
            {
                if (SuggestionsPanel == null)
                    return;

                SuggestionsPanel.Children.Clear();

                // 尝试通过AI服务获取智能建议
                if (_aiAssistantService != null && !string.IsNullOrWhiteSpace(_polishedFullText))
                {
                    try
                    {
                        var parameters = new Dictionary<string, object>
                        {
                            ["text"] = _polishedFullText,
                            ["taskType"] = "SmartSuggestions"
                        };

                        var result = await _aiAssistantService.PolishTextAsync(parameters);

                        if (result.IsSuccess && result.Data is string suggestionsText && !string.IsNullOrWhiteSpace(suggestionsText))
                        {
                            RenderSuggestionsFromText(suggestionsText);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AI智能建议失败，降级为预设建议: {ex.Message}");
                    }
                }

                // 降级：使用预设参考建议
                RenderFallbackSuggestions();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"生成智能建议失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从AI返回的文本渲染智能建议
        /// </summary>
        private void RenderSuggestionsFromText(string text)
        {
            var lines = text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.TrimStart('•', '-', '*', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ' ', '.', '）', ')');
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                var card = new MaterialDesignThemes.Wpf.Card
                {
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(12)
                };

                var contentText = new TextBlock
                {
                    Text = trimmed,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Colors.Gray)
                };

                card.Content = contentText;
                SuggestionsPanel.Children.Add(card);
            }
        }

        /// <summary>
        /// 渲染预设参考建议（降级fallback）
        /// </summary>
        private void RenderFallbackSuggestions()
        {
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

        /// <summary>
        /// 生成质量分析（优先调用AI服务，失败时降级为预设参考分数）
        /// </summary>
        private async Task GenerateQualityAnalysisAsync()
        {
            try
            {
                if (QualityAnalysisPanel == null)
                    return;

                QualityAnalysisPanel.Children.Clear();

                // 尝试通过AI服务获取质量分析
                if (_aiAssistantService != null && !string.IsNullOrWhiteSpace(_polishedFullText))
                {
                    try
                    {
                        var parameters = new Dictionary<string, object>
                        {
                            ["text"] = _polishedFullText,
                            ["taskType"] = "QualityAnalysis"
                        };

                        var result = await _aiAssistantService.PolishTextAsync(parameters);

                        if (result.IsSuccess && result.Data is string analysisText && !string.IsNullOrWhiteSpace(analysisText))
                        {
                            RenderQualityAnalysisFromText(analysisText);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AI质量分析失败，降级为预设分析: {ex.Message}");
                    }
                }

                // 降级：使用预设参考分数
                RenderFallbackQualityAnalysis();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"生成质量分析失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从AI返回的文本渲染质量分析
        /// </summary>
        private void RenderQualityAnalysisFromText(string text)
        {
            var lines = text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.TrimStart('•', '-', '*', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ' ', '.', '）', ')');
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                var card = new MaterialDesignThemes.Wpf.Card
                {
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(12)
                };

                var contentText = new TextBlock
                {
                    Text = trimmed,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Colors.Gray)
                };

                card.Content = contentText;
                QualityAnalysisPanel.Children.Add(card);
            }
        }

        /// <summary>
        /// 渲染预设参考质量分析（降级fallback）
        /// </summary>
        private void RenderFallbackQualityAnalysis()
        {
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
                    Foreground = item.Score >= 90 ? (Brush)FindResource("AppSuccessBrush") :
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

        /// <summary>
        /// 应用所有智能建议
        /// </summary>
        private void ApplyAllSmartSuggestions()
        {
            if (string.IsNullOrWhiteSpace(PolishedContentTextBox.Text) && string.IsNullOrWhiteSpace(_polishedFullText))
            {
                MessageBox.Show("当前没有可应用的润色内容。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (var replacement in _replacements)
            {
                replacement.IsSelected = true;
            }

            GeneratePreviewText();

            var optimizedText = ApplyHeuristicSuggestions(PolishedContentTextBox.Text);
            if (!string.Equals(optimizedText, PolishedContentTextBox.Text, StringComparison.Ordinal))
            {
                PolishedContentTextBox.Text = optimizedText;
                _polishedFullText = optimizedText;
            }

            UpdatePolishedWordCount();
            UpdateImprovementIndicator();
            UpdateReplacementStats();
            SavePolishHistoryRecord(BuildPolishHistoryRecord());
        }

        private string ApplyHeuristicSuggestions(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var result = text;

            if (AutoCorrectCheckBox.IsChecked == true)
            {
                result = NormalizeCommonPunctuation(result);
            }

            if (EnhanceDescriptionCheckBox.IsChecked == true)
            {
                result = EnhanceDescriptivePhrasing(result);
            }

            if (OptimizeDialogueCheckBox.IsChecked == true)
            {
                result = OptimizeDialogueFormatting(result);
            }

            if (PreserveStyleCheckBox.IsChecked != true)
            {
                result = Regex.Replace(result, @"\n{3,}", "\n\n");
            }

            return result.Trim();
        }

        private static string NormalizeCommonPunctuation(string text)
        {
            var normalized = text
                .Replace("。。", "。")
                .Replace("！！", "！")
                .Replace("？？", "？")
                .Replace("，。", "。")
                .Replace("。！", "！")
                .Replace("。？", "？");

            normalized = Regex.Replace(normalized, @"[ \t]+\r?\n", Environment.NewLine);
            normalized = Regex.Replace(normalized, @"\r?\n{3,}", Environment.NewLine + Environment.NewLine);
            return normalized;
        }

        private static string EnhanceDescriptivePhrasing(string text)
        {
            var replacements = new Dictionary<string, string>
            {
                ["很好"] = "颇为出色",
                ["很快"] = "转瞬之间",
                ["很强"] = "强横非常",
                ["很安静"] = "静得只余微弱回响",
                ["很亮"] = "明亮得近乎耀眼"
            };

            var result = text;
            foreach (var pair in replacements)
            {
                result = result.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
            }

            return result;
        }

        private static string OptimizeDialogueFormatting(string text)
        {
            var result = text.Replace("\"", "“");
            result = Regex.Replace(result, "“([^”\\n]+)“", "“$1”");
            result = Regex.Replace(result, @"([。！？])([^\r\n“])", $"$1{Environment.NewLine}$2");
            return result;
        }

        private PolishHistoryRecord BuildPolishHistoryRecord()
        {
            return new PolishHistoryRecord
            {
                Title = string.IsNullOrWhiteSpace(_originalContent)
                    ? $"润色记录_{DateTime.Now:yyyyMMdd_HHmmss}"
                    : $"{_originalContent.Substring(0, Math.Min(20, _originalContent.Length)).Replace(Environment.NewLine, " ")}...",
                OriginalContent = _originalContent,
                PolishedContent = PolishedContentTextBox.Text ?? string.Empty,
                AIModel = GetSelectedAiModel(),
                TargetStyle = (TargetStyleComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty,
                PolishIntensity = (PolishIntensityComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty,
                PreserveElements = (PreserveElementsComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty,
                SpecialRequirements = SpecialRequirementsTextBox.Text?.Trim() ?? string.Empty,
                ReplacementCount = _replacements.Count,
                SavedAt = DateTime.Now
            };
        }

        private void SavePolishHistoryRecord(PolishHistoryRecord record)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(record.PolishedContent))
                {
                    return;
                }

                var historyDirectory = GetPolishHistoryDirectory();
                Directory.CreateDirectory(historyDirectory);
                var filePath = Path.Combine(historyDirectory, $"{record.SavedAt:yyyyMMdd_HHmmssfff}_{SanitizeFileName(record.Title)}.json");
                File.WriteAllText(filePath, JsonSerializer.Serialize(record, _jsonSerializerOptions));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存润色历史失败: {ex.Message}");
            }
        }

        private PolishHistoryRecord? LoadPolishHistoryRecord(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<PolishHistoryRecord>(json);
        }

        private void ApplyHistoryRecord(PolishHistoryRecord record)
        {
            OriginalContentTextBox.Text = record.OriginalContent;
            PolishedContentTextBox.Text = record.PolishedContent;
            _polishedFullText = record.PolishedContent;

            SetComboBoxSelection(AIModelComboBox, record.AIModel);
            SetComboBoxSelection(TargetStyleComboBox, record.TargetStyle);
            SetComboBoxSelection(PolishIntensityComboBox, record.PolishIntensity);
            SetComboBoxSelection(PreserveElementsComboBox, record.PreserveElements);
            SpecialRequirementsTextBox.Text = record.SpecialRequirements;

            UpdateOriginalWordCount();
            UpdatePolishedWordCount();
            UpdateImprovementIndicator();
            GenerateTextDifferenceReplacements(record.OriginalContent, record.PolishedContent);
            _ = GenerateSmartSuggestionsAsync();
            _ = GenerateQualityAnalysisAsync();
        }

        private static void SetComboBoxSelection(ComboBox comboBox, string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
            {
                if (string.Equals(item.Content?.ToString(), content, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private static string GetPolishHistoryDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NovelManagement",
                "history",
                "polish");
        }

        private static string SanitizeFileName(string name)
        {
            return string.Concat(name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
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
                _replacements.Clear();
                if (originalText != polishedText)
                {
                    _replacements.Add(new PolishReplacement
                    {
                        OriginalText = originalText,
                        ReplacementText = polishedText,
                        Position = 0,
                        Length = originalText.Length,
                        ReplacementType = "整体润色",
                        IsSelected = true,
                        Description = "差异分析失败，已退化为全文替换项",
                        Context = "全文内容"
                    });
                }

                UpdateReplacementStats();
            }
        }

        #endregion
    }

    /// <summary>
    /// 文本差异对话框（简化版）
    /// </summary>
    public class TextDifferenceDialog : Window
    {
        /// <summary>
        /// 初始化 TextDifferenceDialog 的新实例，显示原文与润色后文本的对比
        /// </summary>
        /// <param name="original">原始文本</param>
        /// <param name="polished">润色后文本</param>
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

    internal sealed class PolishHistoryRecord
    {
        public string Title { get; set; } = string.Empty;
        public string OriginalContent { get; set; } = string.Empty;
        public string PolishedContent { get; set; } = string.Empty;
        public string AIModel { get; set; } = string.Empty;
        public string TargetStyle { get; set; } = string.Empty;
        public string PolishIntensity { get; set; } = string.Empty;
        public string PreserveElements { get; set; } = string.Empty;
        public string SpecialRequirements { get; set; } = string.Empty;
        public int ReplacementCount { get; set; }
        public DateTime SavedAt { get; set; }
    }

    internal sealed class PolishHistoryDialog : Window
    {
        private readonly string _historyDirectory;
        private readonly ListBox _historyListBox;
        private readonly TextBox _previewTextBox;
        public string? SelectedHistoryPath { get; private set; }

        public PolishHistoryDialog(string historyDirectory)
        {
            _historyDirectory = historyDirectory;
            Title = "润色历史记录";
            Width = 860;
            Height = 560;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _historyListBox = new ListBox { Margin = new Thickness(0, 0, 12, 0), DisplayMemberPath = nameof(PolishHistoryListItem.DisplayText) };
            _previewTextBox = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            _historyListBox.SelectionChanged += (_, _) => UpdatePreview();
            RefreshHistory();

            var root = new Grid { Margin = new Thickness(20) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var header = new TextBlock
            {
                Text = "请选择要查看或回填的润色记录：",
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid.SetColumnSpan(header, 3);
            root.Children.Add(header);

            Grid.SetRow(_historyListBox, 1);
            Grid.SetColumn(_historyListBox, 0);
            root.Children.Add(_historyListBox);

            Grid.SetRow(_previewTextBox, 1);
            Grid.SetColumn(_previewTextBox, 2);
            root.Children.Add(_previewTextBox);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };

            var deleteButton = new Button { Content = "删除", Width = 84, Margin = new Thickness(0, 0, 12, 0) };
            deleteButton.Click += (_, _) =>
            {
                if (_historyListBox.SelectedItem is not PolishHistoryListItem item)
                {
                    MessageBox.Show("请先选择一条历史记录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (MessageBox.Show($"确定删除记录“{item.Title}”吗？", "确认删除",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }

                File.Delete(item.FilePath);
                RefreshHistory();
                _previewTextBox.Clear();
            };

            var loadButton = new Button { Content = "载入", Width = 84, Margin = new Thickness(0, 0, 12, 0), IsDefault = true };
            loadButton.Click += (_, _) =>
            {
                if (_historyListBox.SelectedItem is not PolishHistoryListItem item)
                {
                    MessageBox.Show("请先选择一条历史记录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SelectedHistoryPath = item.FilePath;
                DialogResult = true;
            };

            var cancelButton = new Button { Content = "关闭", Width = 84, IsCancel = true };

            buttons.Children.Add(deleteButton);
            buttons.Children.Add(loadButton);
            buttons.Children.Add(cancelButton);

            Grid.SetRow(buttons, 2);
            Grid.SetColumnSpan(buttons, 3);
            root.Children.Add(buttons);

            Content = root;
        }

        private void RefreshHistory()
        {
            Directory.CreateDirectory(_historyDirectory);
            _historyListBox.ItemsSource = Directory.GetFiles(_historyDirectory, "*.json")
                .Select(filePath => new PolishHistoryListItem
                {
                    FilePath = filePath,
                    Title = Path.GetFileNameWithoutExtension(filePath),
                    DisplayText = $"{Path.GetFileNameWithoutExtension(filePath)}\n{File.GetLastWriteTime(filePath):yyyy-MM-dd HH:mm:ss}"
                })
                .OrderByDescending(item => File.GetLastWriteTime(item.FilePath))
                .ToList();
        }

        private void UpdatePreview()
        {
            if (_historyListBox.SelectedItem is not PolishHistoryListItem item)
            {
                _previewTextBox.Clear();
                return;
            }

            try
            {
                var record = JsonSerializer.Deserialize<PolishHistoryRecord>(File.ReadAllText(item.FilePath));
                if (record == null)
                {
                    _previewTextBox.Text = "记录内容无效。";
                    return;
                }

                _previewTextBox.Text =
                    $"标题: {record.Title}{Environment.NewLine}" +
                    $"时间: {record.SavedAt:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
                    $"模型: {record.AIModel}{Environment.NewLine}" +
                    $"目标风格: {record.TargetStyle}{Environment.NewLine}" +
                    $"润色强度: {record.PolishIntensity}{Environment.NewLine}" +
                    $"替换建议数: {record.ReplacementCount}{Environment.NewLine}{Environment.NewLine}" +
                    $"原文:{Environment.NewLine}{record.OriginalContent}{Environment.NewLine}{Environment.NewLine}" +
                    $"润色后:{Environment.NewLine}{record.PolishedContent}";
            }
            catch (Exception ex)
            {
                _previewTextBox.Text = $"读取记录失败：{ex.Message}";
            }
        }
    }

    internal sealed class PolishHistoryListItem
    {
        public string FilePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
    }
}
