using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using NovelManagement.WPF.Services;
using NovelManagement.WPF.Models;
using NovelManagement.Application.Services;
using NovelManagement.Application.Interfaces;
using System.Linq;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 前置条件生成对话框
    /// </summary>
    public partial class PrerequisiteGenerationDialog : Window
    {
        private readonly Guid _projectId;
        private PrerequisiteGenerationService? _prerequisiteService;
        private bool _isGenerating;

        public PrerequisiteGenerationDialog(Guid projectId)
        {
            InitializeComponent();
            _projectId = projectId;
            InitializeServices();
            
            // 窗口加载完成后检查状态
            Loaded += async (s, e) => await CheckCurrentStatusAsync();
        }

        /// <summary>
        /// 初始化服务
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                _prerequisiteService = App.ServiceProvider?.GetService<PrerequisiteGenerationService>();
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

        /// <summary>
        /// 检查状态按钮点击事件
        /// </summary>
        private async void CheckStatus_Click(object sender, RoutedEventArgs e)
        {
            await CheckCurrentStatusAsync();
        }

        /// <summary>
        /// 生成按钮点击事件
        /// </summary>
        private async void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (_isGenerating) return;

            try
            {
                _isGenerating = true;
                GenerateButton.IsEnabled = false;
                CheckStatusButton.IsEnabled = false;
                
                // 显示进度卡片
                ProgressCard.Visibility = Visibility.Visible;
                ResultCard.Visibility = Visibility.Collapsed;
                
                // 开始生成
                await GeneratePrerequisitesAsync();
            }
            finally
            {
                _isGenerating = false;
                GenerateButton.IsEnabled = true;
                CheckStatusButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (_isGenerating)
            {
                var result = MessageBox.Show("正在生成前置数据，确定要关闭吗？", "确认", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;
            }
            
            Close();
        }

        /// <summary>
        /// 检查当前状态
        /// </summary>
        private async Task CheckCurrentStatusAsync()
        {
            try
            {
                // 检查剧情大纲
                var plotService = App.ServiceProvider?.GetService<PlotService>();
                if (plotService != null)
                {
                    var plots = await plotService.GetPlotsByProjectIdAsync(_projectId);
                    var plotCount = plots.Count();
                    PlotsStatusTextBlock.Text = $"剧情大纲: {plotCount} 个 {(plotCount >= 3 ? "✓" : "需要生成")}";
                    GeneratePlotsCheckBox.IsChecked = plotCount < 3;
                }

                // 检查主要角色
                var characterService = App.ServiceProvider?.GetService<CharacterService>();
                if (characterService != null)
                {
                    var characters = await characterService.GetCharactersByProjectIdAsync(_projectId);
                    var mainCharacters = characters;
                    var characterCount = mainCharacters.Count();
                    CharactersStatusTextBlock.Text = $"主要角色: {characterCount} 个 {(characterCount >= 3 ? "✓" : "需要生成")}";
                    GenerateCharactersCheckBox.IsChecked = characterCount < 3;
                }

                // 检查世界设定
                var worldSettingService = App.ServiceProvider?.GetService<IWorldSettingService>();
                if (worldSettingService != null)
                {
                    var settings = await worldSettingService.GetAllAsync(_projectId);
                    var settingCount = settings.Count();
                    WorldSettingsStatusTextBlock.Text = $"世界设定: {settingCount} 个 {(settingCount >= 5 ? "✓" : "需要生成")}";
                    GenerateWorldSettingsCheckBox.IsChecked = settingCount < 5;
                }

                // 检查势力
                var factionService = App.ServiceProvider?.GetService<FactionService>();
                if (factionService != null)
                {
                    var factions = await factionService.GetFactionsByProjectIdAsync(_projectId);
                    var factionCount = factions.Count();
                    FactionsStatusTextBlock.Text = $"势力组织: {factionCount} 个 {(factionCount >= 3 ? "✓" : "需要生成")}";
                    GenerateFactionsCheckBox.IsChecked = factionCount < 3;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"检查状态失败：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 生成前置条件
        /// </summary>
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
                // 更新进度
                GenerationProgressBar.Value = 0;
                ProgressStatusTextBlock.Text = "开始生成前置条件...";

                // 构建生成选项
                var options = BuildGenerationOptions();

                PrerequisiteGenerationResult result;

                if (options.UseAIGeneration && !string.IsNullOrWhiteSpace(options.AIPrompt))
                {
                    // 使用AI生成
                    ProgressStatusTextBlock.Text = "使用AI智能生成中...";
                    result = await _prerequisiteService.GenerateWithAIAsync(_projectId, options.AIPrompt);
                }
                else
                {
                    // 使用模板生成
                    ProgressStatusTextBlock.Text = "使用模板生成中...";
                    result = await _prerequisiteService.GeneratePrerequisitesAsync(_projectId, options);
                }

                // 更新进度
                GenerationProgressBar.Value = 100;
                ProgressStatusTextBlock.Text = "生成完成";

                // 显示结果
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

                    // 重新检查状态
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

        /// <summary>
        /// 构建生成选项
        /// </summary>
        private PrerequisiteGenerationOptions BuildGenerationOptions()
        {
            var options = new PrerequisiteGenerationOptions
            {
                GeneratePlotOutlines = GeneratePlotsCheckBox.IsChecked == true,
                GenerateMainCharacters = GenerateCharactersCheckBox.IsChecked == true,
                GenerateWorldSettings = GenerateWorldSettingsCheckBox.IsChecked == true,
                GenerateFactions = GenerateFactionsCheckBox.IsChecked == true,
                UseAIGeneration = UseAIGenerationCheckBox.IsChecked == true,
                AIPrompt = AIPromptTextBox.Text?.Trim() ?? string.Empty,
                NovelGenre = ((ComboBoxItem)NovelGenreComboBox.SelectedItem)?.Content?.ToString() ?? "修仙",
                WorldStyle = ((ComboBoxItem)WorldStyleComboBox.SelectedItem)?.Content?.ToString() ?? "东方玄幻",
                AllowUserEditing = AllowUserEditingCheckBox.IsChecked == true
            };

            return options;
        }
    }
}
