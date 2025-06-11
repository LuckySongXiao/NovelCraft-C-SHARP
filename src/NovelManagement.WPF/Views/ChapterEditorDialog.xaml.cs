using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NovelManagement.WPF.Services;
using NovelManagement.WPF.Models;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 章节编辑器对话框
    /// </summary>
    public partial class ChapterEditorDialog : Window
    {
        #region 字段

        private DispatcherTimer _autoSaveTimer;
        private bool _hasUnsavedChanges;
        private DateTime _lastSaved;
        private string _originalContent = string.Empty;
        private AIAssistantService? _aiAssistantService;
        private ChapterService? _chapterService;

        #endregion

        #region 属性

        /// <summary>
        /// 章节数据
        /// </summary>
        public ChapterEditData ChapterData { get; private set; }

        /// <summary>
        /// 是否已保存
        /// </summary>
        public bool IsSaved { get; private set; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="chapterName">章节名称</param>
        public ChapterEditorDialog(string chapterName = "新章节")
        {
            InitializeComponent();
            InitializeServices();
            InitializeEditor(chapterName);
            SetupAutoSave();
            SetupKeyBindings();
        }

        /// <summary>
        /// 构造函数（用于编辑现有章节）
        /// </summary>
        /// <param name="chapter">章节实体</param>
        public ChapterEditorDialog(Chapter chapter)
        {
            InitializeComponent();
            InitializeServices();
            InitializeEditorWithChapter(chapter);
            SetupAutoSave();
            SetupKeyBindings();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化服务
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                // 初始化AI服务
                var aiService = App.ServiceProvider?.GetService<IAIAssistantService>();
                if (aiService is AIAssistantService concreteService)
                {
                    _aiAssistantService = concreteService;
                }
                else
                {
                    _aiAssistantService = App.ServiceProvider?.GetService<AIAssistantService>();
                }

                // 初始化章节服务
                _chapterService = App.ServiceProvider?.GetService<ChapterService>();

                if (_aiAssistantService != null)
                {
                    System.Diagnostics.Debug.WriteLine("AI服务初始化成功");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("AI服务初始化失败：服务未注册");
                }

                if (_chapterService != null)
                {
                    System.Diagnostics.Debug.WriteLine("章节服务初始化成功");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("章节服务初始化失败：服务未注册");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化服务异常：{ex.Message}");
                MessageBox.Show($"初始化服务失败：{ex.Message}", "警告",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 初始化编辑器（新章节）
        /// </summary>
        private async void InitializeEditor(string chapterName)
        {
            try
            {
                // 获取默认卷ID
                var defaultVolumeId = new Guid("00000000-0000-0000-0000-000000000002");

                // 初始化章节数据
                ChapterData = new ChapterEditData
                {
                    Id = Guid.NewGuid(),
                    Title = chapterName,
                    Content = GetSampleContent(),
                    Summary = "这是一个精彩的章节，主角将面临重大挑战...",
                    Status = "写作中",
                    ImportanceLevel = 2,
                    Characters = "林轩, 张伟, 神秘老者",
                    Tags = "修炼, 突破, 危机",
                    Notes = "注意描写主角的心理变化",
                    TargetWordCount = 2800,
                    VolumeId = defaultVolumeId,  // 设置默认卷ID
                    Order = 1  // 设置默认顺序
                };

                // 加载数据到界面
                LoadChapterData();

                // 设置初始状态
                _originalContent = ChapterData.Content;
                _hasUnsavedChanges = false;
                UpdateWordCount();
                UpdateStatus();

                System.Diagnostics.Debug.WriteLine($"新章节初始化完成，VolumeId: {ChapterData.VolumeId}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化编辑器失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 初始化编辑器（现有章节）
        /// </summary>
        private void InitializeEditorWithChapter(Chapter chapter)
        {
            try
            {
                // 从章节实体创建编辑数据
                ChapterData = new ChapterEditData
                {
                    Id = chapter.Id,
                    Title = chapter.Title,
                    Content = chapter.Content ?? GetSampleContent(),
                    Summary = chapter.Summary ?? "这是一个精彩的章节，主角将面临重大挑战...",
                    Status = chapter.Status ?? "写作中",
                    ImportanceLevel = 2,
                    Characters = "林轩, 张伟, 神秘老者",
                    Tags = "修炼, 突破, 危机",
                    Notes = chapter.Notes ?? "注意描写主角的心理变化",
                    TargetWordCount = 2800,
                    VolumeId = chapter.VolumeId,
                    Order = chapter.Order
                };

                // 加载数据到界面
                LoadChapterData();

                // 设置初始状态
                _originalContent = ChapterData.Content;
                _hasUnsavedChanges = false;
                UpdateWordCount();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化编辑器失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载章节数据到界面
        /// </summary>
        private void LoadChapterData()
        {
            ChapterTitleLabel.Text = ChapterData.Title;
            TitleTextBox.Text = ChapterData.Title;
            ContentTextBox.Text = ChapterData.Content;
            SummaryTextBox.Text = ChapterData.Summary;
            CharactersTextBox.Text = ChapterData.Characters;
            TagsTextBox.Text = ChapterData.Tags;
            NotesTextBox.Text = ChapterData.Notes;
            
            // 设置状态
            foreach (System.Windows.Controls.ComboBoxItem item in StatusComboBox.Items)
            {
                if (item.Content.ToString() == ChapterData.Status)
                {
                    StatusComboBox.SelectedItem = item;
                    break;
                }
            }
            
            // 设置重要性
            ImportanceComboBox.SelectedIndex = ChapterData.ImportanceLevel - 1;
            
            // 设置目标字数
            TargetWordCount.Text = $"目标: {ChapterData.TargetWordCount:N0}";
        }

        /// <summary>
        /// 设置自动保存
        /// </summary>
        private void SetupAutoSave()
        {
            _autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(2) // 每2分钟自动保存
            };
            _autoSaveTimer.Tick += AutoSave_Tick;
            _autoSaveTimer.Start();
        }

        /// <summary>
        /// 设置快捷键
        /// </summary>
        private void SetupKeyBindings()
        {
            // Ctrl+S 保存
            var saveBinding = new KeyBinding(new RelayCommand(SaveDraft), Key.S, ModifierKeys.Control);
            InputBindings.Add(saveBinding);
            
            // Ctrl+P 预览
            var previewBinding = new KeyBinding(new RelayCommand(Preview), Key.P, ModifierKeys.Control);
            InputBindings.Add(previewBinding);
        }

        /// <summary>
        /// 获取示例内容
        /// </summary>
        private string GetSampleContent()
        {
            return @"    天空中乌云密布，雷声阵阵。林轩站在山峰之上，感受着天地间涌动的恐怖威压。

    ""天劫...终于来了。""他深吸一口气，眼中闪过一丝坚定。

    这是他修炼路上最重要的一关，成功便能突破到更高境界，失败则可能魂飞魄散。

    第一道雷劫从天而降，带着毁天灭地的威势。林轩不敢大意，立即运转体内真气，准备迎接这生死考验...

    （请在此处继续编写章节内容）";
        }

        #endregion

        #region 更新方法

        /// <summary>
        /// 更新字数统计
        /// </summary>
        private void UpdateWordCount()
        {
            try
            {
                var content = ContentTextBox.Text ?? "";
                var wordCount = content.Length;
                var paragraphs = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).Length;
                var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Length;

                CurrentWordCount.Text = $"当前字数: {wordCount:N0}";
                WordCountLabel.Text = $"({wordCount:N0}字)";
                
                StatsWordCount.Text = wordCount.ToString("N0");
                StatsParagraphs.Text = paragraphs.ToString();
                StatsLines.Text = lines.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新字数统计失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        private void UpdateStatus()
        {
            try
            {
                if (_hasUnsavedChanges)
                {
                    StatusLabel.Text = "状态: 有未保存的更改";
                    Title = $"章节编辑器 - {ChapterData.Title} *";
                }
                else
                {
                    StatusLabel.Text = "状态: 已保存";
                    Title = $"章节编辑器 - {ChapterData.Title}";
                }

                if (_lastSaved != default)
                {
                    LastSavedLabel.Text = $"最后保存: {_lastSaved:HH:mm:ss}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新状态失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 内容文本变化事件
        /// </summary>
        private void ContentTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _hasUnsavedChanges = true;
            UpdateWordCount();
            UpdateStatus();
        }

        /// <summary>
        /// 自动保存定时器事件
        /// </summary>
        private void AutoSave_Tick(object sender, EventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                SaveDraft();
            }
        }

        /// <summary>
        /// 保存草稿
        /// </summary>
        private async void SaveDraft()
        {
            try
            {
                // 收集当前数据
                ChapterData.Content = ContentTextBox.Text;
                ChapterData.Title = TitleTextBox.Text;
                ChapterData.Summary = SummaryTextBox.Text;
                ChapterData.Characters = CharactersTextBox.Text;
                ChapterData.Tags = TagsTextBox.Text;
                ChapterData.Notes = NotesTextBox.Text;

                if (StatusComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem statusItem)
                {
                    ChapterData.Status = statusItem.Content.ToString();
                }

                // 真正保存到数据库
                await SaveChapterToDatabaseAsync();

                _lastSaved = DateTime.Now;
                _hasUnsavedChanges = false;
                UpdateStatus();

                // 显示保存提示（短暂显示）
                StatusLabel.Text = "状态: 草稿已保存";

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    UpdateStatus();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存草稿失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存章节到数据库
        /// </summary>
        private async Task SaveChapterToDatabaseAsync()
        {
            if (_chapterService == null)
            {
                System.Diagnostics.Debug.WriteLine("章节服务未初始化，无法保存到数据库");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"开始保存章节到数据库，ID: {ChapterData.Id}, VolumeId: {ChapterData.VolumeId}");

                // 确保完整的层次结构存在：项目 -> 卷 -> 章节
                await EnsureCompleteHierarchyAsync();

                System.Diagnostics.Debug.WriteLine($"层次结构确保完成，VolumeId: {ChapterData.VolumeId}");

                // 验证VolumeId
                if (ChapterData.VolumeId == Guid.Empty)
                {
                    throw new InvalidOperationException("无法创建或获取有效的卷ID");
                }

                // 检查是否是新章节还是更新现有章节
                var existingChapter = await _chapterService.GetChapterByIdAsync(ChapterData.Id);
                if (existingChapter == null)
                {
                    System.Diagnostics.Debug.WriteLine("创建新章节");

                    // 新章节，需要创建
                    var newChapter = new Chapter
                    {
                        Id = ChapterData.Id,
                        Title = ChapterData.Title ?? "未命名章节",
                        Content = ChapterData.Content ?? "",
                        Summary = ChapterData.Summary,
                        Status = ChapterData.Status ?? "草稿",
                        Notes = ChapterData.Notes,
                        VolumeId = ChapterData.VolumeId,
                        Order = ChapterData.Order,
                        WordCount = ChapterData.Content?.Length ?? 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _chapterService.CreateChapterAsync(newChapter);
                    System.Diagnostics.Debug.WriteLine($"新章节已创建：{newChapter.Title}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("更新现有章节");

                    // 现有章节，直接更新已跟踪的实体属性，避免EF跟踪冲突
                    existingChapter.Title = ChapterData.Title ?? "未命名章节";
                    existingChapter.Content = ChapterData.Content ?? "";
                    existingChapter.Summary = ChapterData.Summary;
                    existingChapter.Status = ChapterData.Status ?? "草稿";
                    existingChapter.Notes = ChapterData.Notes;
                    existingChapter.WordCount = ChapterData.Content?.Length ?? 0;
                    existingChapter.UpdatedAt = DateTime.UtcNow;

                    await _chapterService.UpdateChapterAsync(existingChapter);
                    System.Diagnostics.Debug.WriteLine($"章节已更新：{existingChapter.Title}");
                }

                System.Diagnostics.Debug.WriteLine("章节保存成功");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存章节到数据库失败：{ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常详情：{ex}");

                // 提供更详细的错误信息
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n内部异常: {ex.InnerException.Message}";
                }

                throw new Exception($"保存到数据库失败: {errorMessage}", ex);
            }
        }

        /// <summary>
        /// 预览章节
        /// </summary>
        private void Preview()
        {
            try
            {
                var previewDialog = new ChapterPreviewDialog(ChapterData);
                previewDialog.Owner = this;
                previewDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开预览失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存草稿按钮点击
        /// </summary>
        private void SaveDraft_Click(object sender, RoutedEventArgs e)
        {
            SaveDraft();
        }

        /// <summary>
        /// 预览按钮点击
        /// </summary>
        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            Preview();
        }

        /// <summary>
        /// 保存并关闭按钮点击
        /// </summary>
        private void SaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveDraft();
                IsSaved = true;
                
                MessageBox.Show("章节已保存！", "成功", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存章节失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var result = MessageBox.Show("有未保存的更改，是否保存？", "确认",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SaveDraft();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _autoSaveTimer?.Stop();
            base.OnClosing(e);
        }

        #endregion

        #region AI功能事件处理

        /// <summary>
        /// AI编写按钮点击事件
        /// </summary>
        private async void AIWrite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查AI服务状态
                if (!await CheckAIServiceStatusAsync())
                {
                    return;
                }

                // 显示AI编写对话框
                var aiWriteDialog = new AIChapterWriteDialog(ChapterData);
                aiWriteDialog.Owner = this;

                if (aiWriteDialog.ShowDialog() == true)
                {
                    var result = aiWriteDialog.GeneratedContent;
                    if (!string.IsNullOrEmpty(result))
                    {
                        // 询问用户是否替换当前内容
                        var confirmResult = MessageBox.Show(
                            "AI已生成新的章节内容。是否替换当前内容？\n\n点击\"是\"替换全部内容\n点击\"否\"追加到末尾",
                            "确认操作",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Question);

                        if (confirmResult == MessageBoxResult.Yes)
                        {
                            ContentTextBox.Text = result;
                        }
                        else if (confirmResult == MessageBoxResult.No)
                        {
                            ContentTextBox.Text += "\n\n" + result;
                        }

                        _hasUnsavedChanges = true;
                        UpdateWordCount();
                        UpdateStatus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI编写失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// AI续写按钮点击事件
        /// </summary>
        private async void AIContinue_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查AI服务状态
                if (!await CheckAIServiceStatusAsync())
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(ContentTextBox.Text))
                {
                    MessageBox.Show("请先输入一些内容，AI将基于现有内容进行续写", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 显示AI续写对话框
                var aiContinueDialog = new AIContinueWriteDialog(ChapterData, ContentTextBox.Text);
                aiContinueDialog.Owner = this;

                if (aiContinueDialog.ShowDialog() == true)
                {
                    var result = aiContinueDialog.ContinuedContent;
                    if (!string.IsNullOrEmpty(result))
                    {
                        ContentTextBox.Text += "\n\n" + result;
                        _hasUnsavedChanges = true;
                        UpdateWordCount();
                        UpdateStatus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI续写失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// AI润色按钮点击事件
        /// </summary>
        private async void AIPolish_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查AI服务状态
                if (!await CheckAIServiceStatusAsync())
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(ContentTextBox.Text))
                {
                    MessageBox.Show("请先输入内容再进行润色", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 显示AI润色对话框
                var aiPolishDialog = new AIPolishDialog(ContentTextBox.Text);
                aiPolishDialog.Owner = this;

                if (aiPolishDialog.ShowDialog() == true)
                {
                    var result = aiPolishDialog.PolishedContent;
                    if (!string.IsNullOrEmpty(result))
                    {
                        var confirmResult = MessageBox.Show(
                            "AI已完成内容润色。是否应用润色结果？",
                            "确认润色",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (confirmResult == MessageBoxResult.Yes)
                        {
                            ContentTextBox.Text = result;
                            _hasUnsavedChanges = true;
                            UpdateWordCount();
                            UpdateStatus();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI润色失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 一致性检查按钮点击事件
        /// </summary>
        private async void ConsistencyCheck_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查AI服务状态
                if (!await CheckAIServiceStatusAsync())
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(ContentTextBox.Text))
                {
                    MessageBox.Show("请先输入内容再进行一致性检查", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 显示一致性检查对话框
                var consistencyDialog = new ConsistencyCheckDialog(ChapterData, ContentTextBox.Text);
                consistencyDialog.Owner = this;
                consistencyDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"一致性检查失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 检查AI服务状态
        /// </summary>
        /// <returns>AI服务是否可用</returns>
        private async Task<bool> CheckAIServiceStatusAsync()
        {
            try
            {
                // 检查AI助手服务是否初始化
                if (_aiAssistantService == null)
                {
                    // 尝试重新初始化服务
                    InitializeServices();

                    if (_aiAssistantService == null)
                    {
                        MessageBox.Show("AI服务未初始化，请检查配置。\n\n请确保：\n1. 在AI协作界面配置了AI模型\n2. AI服务正常启动", "AI服务未启动",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                }

                // 检查项目上下文
                if (!await CheckProjectContextAsync())
                {
                    return false;
                }

                // 使用AI服务状态检查器
                var statusChecker = App.ServiceProvider?.GetService<AIServiceStatusChecker>();
                if (statusChecker != null)
                {
                    var status = await statusChecker.CheckServiceStatusAsync();

                    if (status.OverallStatus != ServiceStatus.Available)
                    {
                        var result = MessageBox.Show(
                            $"{status.GetStatusDescription()}\n\n是否尝试重新连接？",
                            "AI服务状态检查",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            // 尝试重新初始化Ollama服务
                            var ollamaService = App.ServiceProvider?.GetService<NovelManagement.AI.Services.Ollama.IOllamaApiService>();
                            if (ollamaService != null)
                            {
                                var config = new NovelManagement.AI.Services.Ollama.Models.OllamaConfiguration
                                {
                                    BaseUrl = "http://localhost:11434",
                                    TimeoutSeconds = 30
                                };

                                var initResult = await ollamaService.InitializeAsync(config);
                                if (!initResult)
                                {
                                    MessageBox.Show("重新连接失败，请确保Ollama服务正在运行", "连接失败",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                    return false;
                                }

                                // 重新检查状态
                                var newStatus = await statusChecker.CheckServiceStatusAsync();
                                return newStatus.OverallStatus == ServiceStatus.Available;
                            }
                        }

                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"检查AI服务状态失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 检查项目上下文
        /// </summary>
        /// <returns>项目上下文是否可用</returns>
        private async Task<bool> CheckProjectContextAsync()
        {
            try
            {
                var projectContextService = App.ServiceProvider?.GetService<ProjectContextService>();
                if (projectContextService == null)
                {
                    MessageBox.Show("项目上下文服务未初始化", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // 检查是否有当前项目
                if (!projectContextService.CurrentProjectId.HasValue)
                {
                    // 尝试获取或创建默认项目
                    var projectId = await EnsureDefaultProjectAsync();
                    if (projectId == Guid.Empty)
                    {
                        MessageBox.Show("无法获取项目信息，AI编辑功能需要项目上下文。\n\n请先：\n1. 创建或选择一个项目\n2. 确保项目数据正常加载",
                            "项目上下文缺失", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                    projectContextService.SetCurrentProject(projectId, "默认项目");
                }

                // 检查并生成前置条件
                await EnsurePrerequisitesAsync(projectContextService.CurrentProjectId.Value);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"检查项目上下文失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 确保默认项目存在
        /// </summary>
        /// <returns>项目ID</returns>
        private async Task<Guid> EnsureDefaultProjectAsync()
        {
            try
            {
                var defaultProjectId = new Guid("00000000-0000-0000-0000-000000000001");
                var projectService = App.ServiceProvider?.GetService<ProjectService>();

                if (projectService != null)
                {
                    try
                    {
                        // 检查项目是否已存在（按ID）
                        var existingProject = await projectService.GetProjectByIdAsync(defaultProjectId);
                        if (existingProject != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"默认项目已存在: {existingProject.Name}");
                            return defaultProjectId;
                        }
                    }
                    catch (Exception)
                    {
                        // 项目不存在，继续检查
                        System.Diagnostics.Debug.WriteLine("按ID未找到默认项目，检查是否有同名项目");
                    }

                    try
                    {
                        // 检查是否有同名项目
                        var allProjects = await projectService.GetAllProjectsAsync();
                        var existingByName = allProjects?.FirstOrDefault(p => p.Name == "默认项目");
                        if (existingByName != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"找到同名项目，使用现有项目: {existingByName.Name} (ID: {existingByName.Id})");
                            return existingByName.Id;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"检查同名项目失败: {ex.Message}");
                    }

                    // 创建默认项目
                    var defaultProject = new Project
                    {
                        Id = defaultProjectId,
                        Name = "默认项目",
                        Description = "系统自动创建的默认项目",
                        Type = "小说",
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    try
                    {
                        await projectService.CreateProjectAsync(defaultProject);
                        System.Diagnostics.Debug.WriteLine($"成功创建默认项目: {defaultProject.Name}");
                    }
                    catch (Exception ex) when (ex.Message.Contains("UNIQUE constraint failed"))
                    {
                        // 如果还是有唯一约束冲突，使用时间戳创建唯一名称
                        System.Diagnostics.Debug.WriteLine("项目名称冲突，使用时间戳创建唯一名称");
                        defaultProject.Name = $"默认项目_{DateTime.Now:yyyyMMdd_HHmmss}";
                        await projectService.CreateProjectAsync(defaultProject);
                        System.Diagnostics.Debug.WriteLine($"成功创建默认项目: {defaultProject.Name}");
                    }
                }

                return defaultProjectId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建默认项目失败: {ex.Message}");
                // 发生错误时返回固定的默认项目ID
                return new Guid("00000000-0000-0000-0000-000000000001");
            }
        }

        /// <summary>
        /// 确保完整的层次结构存在（项目 -> 卷 -> 章节）
        /// </summary>
        private async Task EnsureCompleteHierarchyAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("开始确保完整的层次结构");

                // 1. 确保默认项目存在
                var defaultProjectId = await EnsureDefaultProjectAsync();
                System.Diagnostics.Debug.WriteLine($"默认项目ID: {defaultProjectId}");

                // 2. 确保默认卷存在
                var defaultVolumeId = await EnsureDefaultVolumeAsync(defaultProjectId);
                System.Diagnostics.Debug.WriteLine($"默认卷ID: {defaultVolumeId}");

                // 3. 设置章节的VolumeId
                if (ChapterData.VolumeId == Guid.Empty)
                {
                    ChapterData.VolumeId = defaultVolumeId;
                    System.Diagnostics.Debug.WriteLine($"设置章节VolumeId: {ChapterData.VolumeId}");
                }

                System.Diagnostics.Debug.WriteLine("完整层次结构确保完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"确保完整层次结构失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 确保章节有默认卷
        /// </summary>
        /// <returns>默认卷ID</returns>
        private async Task<Guid> EnsureDefaultVolumeForChapterAsync()
        {
            try
            {
                // 首先确保有默认项目
                var defaultProjectId = await EnsureDefaultProjectAsync();

                // 然后确保有默认卷
                var defaultVolumeId = await EnsureDefaultVolumeAsync(defaultProjectId);

                // 返回默认卷ID
                return defaultVolumeId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"确保默认卷失败: {ex.Message}");
                // 返回固定的默认卷ID
                return new Guid("00000000-0000-0000-0000-000000000002");
            }
        }

        /// <summary>
        /// 确保默认卷存在
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <returns>默认卷ID</returns>
        private async Task<Guid> EnsureDefaultVolumeAsync(Guid projectId)
        {
            try
            {
                var defaultVolumeId = new Guid("00000000-0000-0000-0000-000000000002");
                var volumeService = App.ServiceProvider?.GetService<VolumeService>();

                if (volumeService != null)
                {
                    try
                    {
                        // 检查卷是否已存在
                        var existingVolume = await volumeService.GetVolumeByIdAsync(defaultVolumeId);
                        if (existingVolume != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"默认卷已存在: {existingVolume.Title}");
                            return defaultVolumeId;
                        }
                    }
                    catch (Exception)
                    {
                        // 卷不存在，继续创建
                        System.Diagnostics.Debug.WriteLine("默认卷不存在，准备创建");
                    }

                    // 创建默认卷
                    var defaultVolume = new Volume
                    {
                        Id = defaultVolumeId,
                        Title = "默认卷",
                        Description = "系统自动创建的默认卷",
                        ProjectId = projectId,
                        Order = 1,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await volumeService.CreateVolumeAsync(defaultVolume);
                    System.Diagnostics.Debug.WriteLine($"成功创建默认卷: {defaultVolume.Title}");
                }

                return defaultVolumeId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建默认卷失败: {ex.Message}");
                // 发生错误时返回固定的默认卷ID
                return new Guid("00000000-0000-0000-0000-000000000002");
            }
        }


        /// <summary>
        /// 确保前置条件存在
        /// </summary>
        /// <param name="projectId">项目ID</param>
        private async Task EnsurePrerequisitesAsync(Guid projectId)
        {
            try
            {
                var prerequisiteService = App.ServiceProvider?.GetService<PrerequisiteGenerationService>();
                if (prerequisiteService == null)
                {
                    System.Diagnostics.Debug.WriteLine("前置条件生成服务未注册，跳过前置条件检查");
                    return;
                }

                var result = await prerequisiteService.GeneratePrerequisitesAsync(projectId);

                if (result.IsSuccess && result.TotalGeneratedCount > 0)
                {
                    // 显示生成结果
                    var message = $"为确保AI编辑功能正常运行，系统已自动生成必要的前置数据：\n\n{result.GetGenerationSummary()}\n\n这些数据将帮助AI更好地理解您的小说世界观和角色设定。";

                    MessageBox.Show(message, "前置数据生成完成",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    System.Diagnostics.Debug.WriteLine($"前置条件生成完成: {result.GetDetailedReport()}");
                }
                else if (!result.IsSuccess)
                {
                    System.Diagnostics.Debug.WriteLine($"前置条件生成失败: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检查前置条件时发生错误: {ex.Message}");
                // 不阻止AI编辑功能的使用，只是记录错误
            }
        }

        #endregion
    }

    /// <summary>
    /// 章节编辑数据模型
    /// </summary>
    public class ChapterEditData
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ImportanceLevel { get; set; } = 2;
        public string Characters { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public int TargetWordCount { get; set; } = 2800;
        public Guid VolumeId { get; set; }
        public int Order { get; set; } = 1;
    }

    /// <summary>
    /// 简单的命令实现
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        
        public RelayCommand(Action execute)
        {
            _execute = execute;
        }
        
        public event EventHandler CanExecuteChanged;
        
        public bool CanExecute(object parameter) => true;
        
        public void Execute(object parameter) => _execute?.Invoke();
    }
}
