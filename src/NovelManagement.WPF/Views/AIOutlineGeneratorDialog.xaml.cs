using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using NovelManagement.WPF.Services;
using NovelManagement.Core.Entities;
using NovelManagement.Application.Services;
using NovelManagement.WPF.Models;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// AI大纲生成器对话框
    /// </summary>
    public partial class AIOutlineGeneratorDialog : Window
    {
        private readonly IAIAssistantService _aiAssistantService;
        private readonly PlotService _plotService;
        private readonly VolumeService _volumeService;
        private readonly ProjectService _projectService;
        private readonly ProjectReadModelService _projectReadModelService;
        private string _generatedOutline = "";
        private Guid _currentProjectId;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="projectId">当前项目ID</param>
        public AIOutlineGeneratorDialog(Guid projectId)
        {
            InitializeComponent();
            
            _currentProjectId = projectId;
            
            // 从依赖注入容器获取服务
            _aiAssistantService = App.ServiceProvider?.GetService<IAIAssistantService>()
                ?? throw new InvalidOperationException("AI助手服务未注册");
            _plotService = App.ServiceProvider?.GetService<PlotService>()
                ?? throw new InvalidOperationException("剧情服务未注册");
            _volumeService = App.ServiceProvider?.GetService<VolumeService>()
                ?? throw new InvalidOperationException("卷宗服务未注册");
            _projectService = App.ServiceProvider?.GetService<ProjectService>()
                ?? throw new InvalidOperationException("项目服务未注册");
            _projectReadModelService = App.ServiceProvider?.GetService<ProjectReadModelService>()
                ?? throw new InvalidOperationException("项目上下文服务未注册");

            // 设置默认值
            InitializeDefaults();
        }

        /// <summary>
        /// 初始化默认值
        /// </summary>
        private void InitializeDefaults()
        {
            NovelTypeComboBox.SelectedIndex = 0; // 修仙书籍
            TargetLengthComboBox.SelectedIndex = 2; // 长篇
        }

        /// <summary>
        /// 生成按钮点击事件
        /// </summary>
        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            await GenerateOutlineAsync();
        }

        /// <summary>
        /// 重新生成按钮点击事件
        /// </summary>
        private async void RegenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            await GenerateOutlineAsync();
        }

        /// <summary>
        /// 保存按钮点击事件
        /// </summary>
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_generatedOutline))
            {
                MessageBox.Show("请先生成大纲", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await SaveOutlineToDatabase();
        }

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        private async void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_generatedOutline))
            {
                await SaveOutlineToDatabase();
            }
            
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 验证输入
        /// </summary>
        /// <returns>是否验证通过</returns>
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(ThemeTextBox.Text))
            {
                MessageBox.Show("请输入书籍主题", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                ThemeTextBox.Focus();
                return false;
            }

            if (NovelTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("请选择书籍类型", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                NovelTypeComboBox.Focus();
                return false;
            }

            if (TargetLengthComboBox.SelectedItem == null)
            {
                MessageBox.Show("请选择目标长度", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                TargetLengthComboBox.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 生成大纲
        /// </summary>
        private async Task GenerateOutlineAsync()
        {
            try
            {
                // 禁用生成按钮并显示进度条
                GenerateButton.IsEnabled = false;
                GenerateButton.Content = "生成中...";
                ProgressBar.Visibility = Visibility.Visible;
                ProgressBar.IsIndeterminate = true;

                var projectContext = await _projectReadModelService.BuildAiContextDataAsync(_currentProjectId);

                // 构建生成参数
                var parameters = new Dictionary<string, object>
                {
                    ["theme"] = ThemeTextBox.Text,
                    ["novelType"] = ((ComboBoxItem)NovelTypeComboBox.SelectedItem).Content.ToString(),
                    ["targetLength"] = ((ComboBoxItem)TargetLengthComboBox.SelectedItem).Content.ToString(),
                    ["mainCharacter"] = MainCharacterTextBox.Text,
                    ["setting"] = SettingTextBox.Text,
                    ["requirements"] = BuildOutlineRequirements(projectContext),
                    ["projectId"] = _currentProjectId,
                    ["projectContext"] = projectContext.PromptSummary
                };

                // 调用AI服务生成大纲
                var result = await _aiAssistantService.GenerateOutlineAsync(parameters);

                if (result.IsSuccess && result.Data != null)
                {
                    _generatedOutline = result.Data.ToString() ?? "";
                    ResultTextBox.Text = _generatedOutline;

                    MessageBox.Show("大纲生成完成！", "成功", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"大纲生成失败：{result.Message}", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成大纲时发生错误：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 恢复生成按钮状态
                GenerateButton.IsEnabled = true;
                GenerateButton.Content = "开始生成";
                ProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 保存大纲到数据库
        /// </summary>
        private async Task SaveOutlineToDatabase()
        {
            try
            {
                // 创建剧情记录
                var plot = new Plot
                {
                    Id = Guid.NewGuid(),
                    ProjectId = _currentProjectId,
                    Title = "AI生成的主要大纲",
                    Outline = _generatedOutline,
                    Description = "通过AI生成的书籍主要大纲",
                    Type = "主线剧情",
                    Status = "草稿",
                    Priority = "高",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _plotService.CreatePlotAsync(plot);

                MessageBox.Show("大纲已保存到剧情管理中", "成功", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存大纲失败：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string BuildOutlineRequirements(ProjectContextData projectContext)
        {
            var userRequirements = RequirementsTextBox.Text?.Trim();
            return $@"请基于当前项目上下文生成书籍大纲。

{projectContext.PromptSummary}

【本次创作输入】
主题：{ThemeTextBox.Text}
书籍类型：{((ComboBoxItem)NovelTypeComboBox.SelectedItem).Content}
目标篇幅：{((ComboBoxItem)TargetLengthComboBox.SelectedItem).Content}
核心主角：{(string.IsNullOrWhiteSpace(MainCharacterTextBox.Text) ? "未提供" : MainCharacterTextBox.Text.Trim())}
补充设定：{(string.IsNullOrWhiteSpace(SettingTextBox.Text) ? "未提供" : SettingTextBox.Text.Trim())}
额外要求：{(string.IsNullOrWhiteSpace(userRequirements) ? "无" : userRequirements)}

【生成要求】
1. 如果项目已有世界设定，大纲必须严格建立在现有世界观之上。
2. 若项目基础信息与用户输入冲突，优先遵循项目基础信息与已有世界设定。
3. 输出应包含主线、关键阶段、核心冲突、角色推进与收束方向。
4. 不要输出思考过程、Markdown 标题或解释性前言。";
        }
    }
}
