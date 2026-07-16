using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views;

/// <summary>
/// 稳定版 AI 工作台，作为主导航中的 AI 助手入口。
/// </summary>
public class AIAssistantWorkspaceView : UserControl
{
    private readonly IAIAssistantService? _aiAssistantService;
    private readonly ProjectContextService? _projectContextService;
    private readonly TextBox _promptTextBox;
    private readonly ComboBox _modeComboBox;
    private readonly TextBox _resultTextBox;
    private readonly TextBlock _statusTextBlock;
    private readonly Button _generateButton;

    public AIAssistantWorkspaceView()
    {
        _aiAssistantService = App.ServiceProvider?.GetService(typeof(IAIAssistantService)) as IAIAssistantService;
        _projectContextService = App.ServiceProvider?.GetService(typeof(ProjectContextService)) as ProjectContextService;

        _modeComboBox = new ComboBox
        {
            Margin = new Thickness(0, 0, 0, 12),
            ItemsSource = new[] { "角色补全", "剧情补全", "世界设定补全", "小说大纲" },
            SelectedIndex = 0
        };

        _promptTextBox = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 120,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 0, 0, 12),
            Text = "请输入你的创作需求，AI 会返回可直接用于继续编辑的内容。"
        };

        _resultTextBox = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MinHeight = 260
        };

        _statusTextBlock = new TextBlock
        {
            Margin = new Thickness(0, 8, 0, 0),
            Text = _aiAssistantService == null ? "AI助手服务未初始化" : "就绪"
        };

        _generateButton = new Button
        {
            Content = "开始生成",
            Padding = new Thickness(16, 8, 16, 8),
            MinWidth = 120
        };
        _generateButton.Click += async (_, _) => await GenerateAsync();

        var outlineButton = new Button
        {
            Content = "打开AI大纲生成器",
            Margin = new Thickness(12, 0, 0, 0),
            Padding = new Thickness(16, 8, 16, 8)
        };
        outlineButton.Click += (_, _) => OpenOutlineGenerator();

        var prerequisiteButton = new Button
        {
            Content = "前置条件生成",
            Margin = new Thickness(12, 0, 0, 0),
            Padding = new Thickness(16, 8, 16, 8)
        };
        prerequisiteButton.Click += (_, _) => OpenPrerequisiteGeneration();

        var actionPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 12)
        };
        actionPanel.Children.Add(_generateButton);
        actionPanel.Children.Add(outlineButton);
        actionPanel.Children.Add(prerequisiteButton);

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                Children =
                {
                    new TextBlock
                    {
                        Text = "AI 工作台",
                        FontSize = 28,
                        FontWeight = FontWeights.Bold
                    },
                    new TextBlock
                    {
                        Margin = new Thickness(0, 8, 0, 16),
                        Text = "这里提供稳定版 AI 助手入口，可直接生成角色、剧情、世界设定或小说大纲。",
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock { Text = "生成模式" },
                    _modeComboBox,
                    new TextBlock { Text = "输入需求" },
                    _promptTextBox,
                    actionPanel,
                    new TextBlock { Text = "生成结果" },
                    _resultTextBox,
                    _statusTextBlock
                }
            }
        };
    }

    private async Task GenerateAsync()
    {
        if (_aiAssistantService == null)
        {
            MessageBox.Show("AI助手服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var prompt = _promptTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            MessageBox.Show("请输入生成需求。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            _generateButton.IsEnabled = false;
            _statusTextBlock.Text = "AI 正在生成...";

            AIAssistantResult result = _modeComboBox.SelectedItem?.ToString() switch
            {
                "角色补全" => await _aiAssistantService.GenerateCharacterAsync(new Dictionary<string, object>
                {
                    ["characterType"] = "主配角",
                    ["backgroundStory"] = prompt,
                    ["requirements"] = prompt
                }),
                "剧情补全" => await _aiAssistantService.GeneratePlotAsync(new Dictionary<string, object>
                {
                    ["plotType"] = "主线",
                    ["theme"] = prompt,
                    ["requirements"] = prompt
                }),
                "世界设定补全" => await _aiAssistantService.GeneratePlotAsync(new Dictionary<string, object>
                {
                    ["plotType"] = "世界设定",
                    ["theme"] = prompt,
                    ["requirements"] = $"请以世界设定形式补全以下需求：{prompt}"
                }),
                "小说大纲" => await _aiAssistantService.GenerateOutlineAsync(new Dictionary<string, object>
                {
                    ["theme"] = prompt,
                    ["novelType"] = "演示项目",
                    ["targetLength"] = "长篇",
                    ["requirements"] = prompt
                }),
                _ => await _aiAssistantService.GeneratePlotAsync(new Dictionary<string, object> { ["theme"] = prompt })
            };

            if (!result.IsSuccess)
            {
                _resultTextBox.Text = result.Message ?? "AI 生成失败。";
                _statusTextBlock.Text = "生成失败";
                return;
            }

            _resultTextBox.Text = result.Data?.ToString() ?? result.Message ?? "AI 已完成生成。";
            _statusTextBlock.Text = "生成完成";
        }
        catch (Exception ex)
        {
            _resultTextBox.Text = ex.ToString();
            _statusTextBlock.Text = "生成异常";
        }
        finally
        {
            _generateButton.IsEnabled = true;
        }
    }

    private void OpenOutlineGenerator()
    {
        if (_projectContextService?.CurrentProjectId == null)
        {
            MessageBox.Show("请先打开一个项目，再使用 AI 大纲生成器。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new AIOutlineGeneratorDialog(_projectContextService.CurrentProjectId.Value)
        {
            Owner = Window.GetWindow(this)
        };
        dialog.ShowDialog();
    }

    private void OpenPrerequisiteGeneration()
    {
        if (_projectContextService?.CurrentProjectId == null)
        {
            MessageBox.Show("请先打开一个项目，再生成前置条件。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new PrerequisiteGenerationDialog(_projectContextService.CurrentProjectId.Value)
        {
            Owner = Window.GetWindow(this)
        };
        dialog.ShowDialog();
    }
}
