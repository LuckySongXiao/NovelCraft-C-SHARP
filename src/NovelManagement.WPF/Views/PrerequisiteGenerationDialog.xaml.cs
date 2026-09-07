using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using NovelManagement.Application.Interfaces;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.WPF.Models;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 前置条件生成对话框
    /// </summary>
    public partial class PrerequisiteGenerationDialog : Window
    {
        private readonly Guid _initialProjectId;
        private Guid _selectedProjectId;
        private PrerequisiteGenerationService? _prerequisiteService;
        private ProjectService? _projectService;
        private ProjectReadModelService? _projectReadModelService;
        private bool _isGenerating;
        private bool _isLoadingProjects;

        public PrerequisiteGenerationDialog(Guid projectId)
        {
            InitializeComponent();
            _initialProjectId = projectId;
            _selectedProjectId = projectId;
            InitializeServices();
            Loaded += async (_, _) => await InitializeAsync();
        }

        private void InitializeServices()
        {
            try
            {
                _prerequisiteService = App.ServiceProvider?.GetService<PrerequisiteGenerationService>();
                _projectService = App.ServiceProvider?.GetService<ProjectService>();
                _projectReadModelService = App.ServiceProvider?.GetService<ProjectReadModelService>();
                if (_prerequisiteService == null)
                {
                    MessageBox.Show("前置条件生成服务未初始化", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化服务失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task InitializeAsync()
        {
            await LoadProjectsAsync();
            await CheckCurrentStatusAsync();
        }

        private async Task LoadProjectsAsync()
        {
            if (_projectService == null)
            {
                return;
            }

            try
            {
                _isLoadingProjects = true;
                var projects = (await _projectService.GetAllProjectsAsync())
                    .OrderByDescending(p => p.UpdatedAt)
                    .Select(p => new ProjectSelectionItem
                    {
                        ProjectId = p.Id,
                        DisplayName = $"{p.Name}（{(string.IsNullOrWhiteSpace(p.Type) ? "未分类" : p.Type)}）"
                    })
                    .ToList();

                ProjectComboBox.ItemsSource = projects;
                ProjectComboBox.SelectedValue = _initialProjectId;
                if (!projects.Any(p => p.ProjectId == _initialProjectId) && projects.Count > 0)
                {
                    _selectedProjectId = projects[0].ProjectId;
                    ProjectComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载项目列表失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoadingProjects = false;
            }
        }

        private async void CheckStatus_Click(object sender, RoutedEventArgs e)
        {
            await CheckCurrentStatusAsync();
        }

        private async void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (_isGenerating)
            {
                return;
            }

            if (!EnsureSelectedProject())
            {
                return;
            }

            try
            {
                _isGenerating = true;
                GenerateButton.IsEnabled = false;
                CheckStatusButton.IsEnabled = false;
                ProjectComboBox.IsEnabled = false;
                ProgressCard.Visibility = Visibility.Visible;
                ResultCard.Visibility = Visibility.Collapsed;
                await GeneratePrerequisitesAsync();
            }
            finally
            {
                _isGenerating = false;
                GenerateButton.IsEnabled = true;
                CheckStatusButton.IsEnabled = true;
                ProjectComboBox.IsEnabled = true;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (_isGenerating)
            {
                var result = MessageBox.Show("正在生成前置数据，确定要关闭吗？", "确认",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            Close();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingProjects)
            {
                return;
            }

            if (ProjectComboBox.SelectedValue is Guid projectId)
            {
                _selectedProjectId = projectId;
                await CheckCurrentStatusAsync();
            }
        }

        private async Task CheckCurrentStatusAsync()
        {
            if (!EnsureSelectedProject())
            {
                ResetStatusDisplay();
                return;
            }

            try
            {
                var plotService = App.ServiceProvider?.GetService<PlotService>();
                var characterService = App.ServiceProvider?.GetService<CharacterService>();
                var worldSettingService = App.ServiceProvider?.GetService<IWorldSettingService>();
                var factionService = App.ServiceProvider?.GetService<FactionService>();
                var volumeService = App.ServiceProvider?.GetService<VolumeService>();
                var chapterService = App.ServiceProvider?.GetService<ChapterService>();

                var plotCount = 0;
                var characterCount = 0;
                var settingCount = 0;
                var factionCount = 0;
                var volumeCount = 0;
                var chapterCount = 0;

                if (plotService != null)
                {
                    var plots = await plotService.GetPlotsByProjectIdAsync(_selectedProjectId);
                    plotCount = plots.Count();
                    PlotsStatusTextBlock.Text = $"剧情大纲: {plotCount} 个 {(plotCount >= 3 ? "✓" : "需要生成")}";
                    GeneratePlotsCheckBox.IsChecked = plotCount < 3;
                }

                if (characterService != null)
                {
                    var characters = await characterService.GetCharactersByProjectIdAsync(_selectedProjectId);
                    characterCount = characters.Count();
                    CharactersStatusTextBlock.Text = $"主要角色: {characterCount} 个 {(characterCount >= 3 ? "✓" : "需要生成")}";
                    GenerateCharactersCheckBox.IsChecked = characterCount < 3;
                }

                if (worldSettingService != null)
                {
                    var settings = await worldSettingService.GetAllAsync(_selectedProjectId);
                    settingCount = settings.Count();
                    WorldSettingsStatusTextBlock.Text = $"世界设定: {settingCount} 个 {(settingCount >= 5 ? "✓" : "需要生成")}";
                    GenerateWorldSettingsCheckBox.IsChecked = settingCount < 5;
                }

                if (factionService != null)
                {
                    var factions = await factionService.GetFactionsByProjectIdAsync(_selectedProjectId);
                    factionCount = factions.Count();
                    FactionsStatusTextBlock.Text = $"势力组织: {factionCount} 个 {(factionCount >= 3 ? "✓" : "需要生成")}";
                    GenerateFactionsCheckBox.IsChecked = factionCount < 3;
                }

                var cultivationSystemService = App.ServiceProvider?.GetService<ICultivationSystemService>();
                if (cultivationSystemService != null)
                {
                    var systems = await cultivationSystemService.GetAllAsync(_selectedProjectId);
                    var systemCount = systems.Count();
                    CultivationSystemStatusTextBlock.Text = systemCount > 0
                        ? $"修炼体系: {systemCount} 套 ✓"
                        : "修炼体系: 未设定，AI 可自上而下生成自定义等级体系";
                    GenerateCultivationSystemCheckBox.IsChecked = systemCount == 0;
                }

                if (volumeService != null)
                {
                    volumeCount = (await volumeService.GetVolumeListAsync(_selectedProjectId)).Count();
                }

                if (chapterService != null)
                {
                    chapterCount = (await chapterService.GetChaptersByProjectIdAsync(_selectedProjectId)).Count();
                }

                await UpdateProjectSummaryAsync();
                await UpdateContextSummaryAsync();
                await UpdateFlowStatusAsync(plotCount, characterCount, settingCount, factionCount, volumeCount, chapterCount);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"检查状态失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateProjectSummaryAsync()
        {
            if (_projectService == null || _selectedProjectId == Guid.Empty)
            {
                ProjectSummaryTextBlock.Text = "未选择项目。";
                return;
            }

            var project = await _projectService.GetProjectByIdAsync(_selectedProjectId);
            if (project == null)
            {
                ProjectSummaryTextBlock.Text = "未找到当前项目。";
                return;
            }

            ProjectSummaryTextBlock.Text =
                $"项目：{project.Name}\n" +
                $"类型：{(string.IsNullOrWhiteSpace(project.Type) ? "未设置" : project.Type)}\n" +
                $"描述：{(string.IsNullOrWhiteSpace(project.Description) ? "未设置" : project.Description)}";
        }

        private async Task UpdateContextSummaryAsync()
        {
            if (_projectReadModelService == null || _selectedProjectId == Guid.Empty)
            {
                ContextSummaryTextBox.Text = "未选择项目。";
                return;
            }

            var contextData = await _projectReadModelService.BuildAiContextDataAsync(_selectedProjectId);
            ContextSummaryTextBox.Text = string.IsNullOrWhiteSpace(contextData.PromptSummary)
                ? "当前项目暂无可用的 AI 上下文摘要。"
                : contextData.PromptSummary;
        }

        private async Task UpdateFlowStatusAsync(
            int plotCount,
            int characterCount,
            int settingCount,
            int factionCount,
            int volumeCount,
            int chapterCount)
        {
            if (_projectService == null || _selectedProjectId == Guid.Empty)
            {
                ProjectInfoStageTextBlock.Text = "未选择项目";
                WorldStageTextBlock.Text = "未开始";
                OutlineStageTextBlock.Text = "未开始";
                SupportStageTextBlock.Text = "未开始";
                WritingStageTextBlock.Text = "未开始";
                return;
            }

            var project = await _projectService.GetProjectByIdAsync(_selectedProjectId);
            var hasProjectBase = project != null &&
                                 !string.IsNullOrWhiteSpace(project.Name) &&
                                 !string.IsNullOrWhiteSpace(project.Type);

            ProjectInfoStageTextBlock.Text = hasProjectBase
                ? $"已就绪\n名称与类型已配置"
                : "待补充\n至少需要名称与类型";

            WorldStageTextBlock.Text = settingCount > 0
                ? $"进行中/已完成\n当前 {settingCount} 条世界设定"
                : "待开始\n请先补充世界观";

            OutlineStageTextBlock.Text = plotCount > 0
                ? $"进行中/已完成\n当前 {plotCount} 条大纲"
                : "待开始\n应基于世界观生成";

            SupportStageTextBlock.Text = (characterCount + factionCount) > 0
                ? $"进行中/已完成\n角色 {characterCount} / 势力 {factionCount}"
                : "待开始\n建议在大纲后补齐";

            WritingStageTextBlock.Text = (volumeCount + chapterCount) > 0
                ? $"进行中/已完成\n卷 {volumeCount} / 章 {chapterCount}"
                : "未开始\n请在基础设定后进入正文";
        }

        private void ResetStatusDisplay()
        {
            ProjectSummaryTextBlock.Text = "请选择项目后查看当前写作流程状态。";
            ContextSummaryTextBox.Text = "请选择项目后查看 AI 将遵循的上位设定与生成顺序。";
            PlotsStatusTextBlock.Text = "剧情大纲: 未选择项目";
            CharactersStatusTextBlock.Text = "主要角色: 未选择项目";
            WorldSettingsStatusTextBlock.Text = "世界设定: 未选择项目";
            FactionsStatusTextBlock.Text = "势力组织: 未选择项目";
            CultivationSystemStatusTextBlock.Text = "修炼体系: 未选择项目";
            ProjectInfoStageTextBlock.Text = "未选择项目";
            WorldStageTextBlock.Text = "未选择项目";
            OutlineStageTextBlock.Text = "未选择项目";
            SupportStageTextBlock.Text = "未选择项目";
            WritingStageTextBlock.Text = "未选择项目";
        }

        private bool EnsureSelectedProject()
        {
            if (_selectedProjectId != Guid.Empty)
            {
                return true;
            }

            MessageBox.Show("请先选择一个项目。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        private async Task GeneratePrerequisitesAsync()
        {
            if (_prerequisiteService == null)
            {
                MessageBox.Show("前置条件生成服务不可用", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                GenerationProgressBar.Value = 0;
                ProgressStatusTextBlock.Text = "开始生成前置条件...";

                var options = BuildGenerationOptions();
                PrerequisiteGenerationResult result;

                if (options.UseAIGeneration && !string.IsNullOrWhiteSpace(options.AIPrompt))
                {
                    ProgressStatusTextBlock.Text = "使用AI智能生成中...";
                    result = await _prerequisiteService.GenerateWithAIAsync(_selectedProjectId, options.AIPrompt);
                }
                else
                {
                    ProgressStatusTextBlock.Text = "使用模板生成中...";
                    result = await _prerequisiteService.GeneratePrerequisitesAsync(_selectedProjectId, options);
                }

                GenerationProgressBar.Value = 100;
                ProgressStatusTextBlock.Text = "生成完成";
                ResultCard.Visibility = Visibility.Visible;
                ResultTextBlock.Text = result.GetDetailedReport();

                if (result.IsSuccess)
                {
                    if (result.TotalGeneratedCount > 0)
                    {
                        var message = $"前置条件生成完成！\n\n{result.GetGenerationSummary()}";
                        if (options.AllowUserEditing)
                        {
                            message += "\n\n✅ 您可以随时编辑、删减或增加这些数据";
                            message += "\n✅ AI也可以根据需要自主生成和写入新内容";
                        }

                        MessageBox.Show(message, "生成成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("项目已有足够的前置数据，无需生成。",
                            "无需生成", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    await CheckCurrentStatusAsync();
                }
                else
                {
                    MessageBox.Show($"前置条件生成失败：{result.Message}",
                        "生成失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成前置条件时发生错误：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ProgressCard.Visibility = Visibility.Collapsed;
            }
        }

        private PrerequisiteGenerationOptions BuildGenerationOptions()
        {
            return new PrerequisiteGenerationOptions
            {
                GeneratePlotOutlines = GeneratePlotsCheckBox.IsChecked == true,
                GenerateMainCharacters = GenerateCharactersCheckBox.IsChecked == true,
                GenerateWorldSettings = GenerateWorldSettingsCheckBox.IsChecked == true,
                GenerateFactions = GenerateFactionsCheckBox.IsChecked == true,
                GenerateCultivationSystem = GenerateCultivationSystemCheckBox.IsChecked == true,
                UseAIGeneration = UseAIGenerationCheckBox.IsChecked == true,
                AIPrompt = AIPromptTextBox.Text?.Trim() ?? string.Empty,
                NovelGenre = ((ComboBoxItem)NovelGenreComboBox.SelectedItem)?.Content?.ToString() ?? "修仙",
                WorldStyle = ((ComboBoxItem)WorldStyleComboBox.SelectedItem)?.Content?.ToString() ?? "东方玄幻",
                AllowUserEditing = AllowUserEditingCheckBox.IsChecked == true
            };
        }

        private sealed class ProjectSelectionItem
        {
            public Guid ProjectId { get; init; }
            public string DisplayName { get; init; } = string.Empty;
        }
    }
}
