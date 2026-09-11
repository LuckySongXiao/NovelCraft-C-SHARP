using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static NovelManagement.WPF.Localization.LocalizationManager;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views;

/// <summary>
/// 稳定版 AI 工作台，作为主导航中的 AI 助手入口。
/// </summary>
public class AIAssistantWorkspaceView : UserControl
{
    private readonly IAIAssistantService? _aiAssistantService;
    private readonly ProjectContextService? _projectContextService;
    private readonly ProjectReadModelService? _projectReadModelService;
    private readonly TextBox _promptTextBox;
    private readonly ComboBox _modeComboBox;
    private readonly TextBox _resultTextBox;
    private readonly TextBlock _statusTextBlock;
    private readonly Button _generateButton;

    public AIAssistantWorkspaceView()
    {
        _aiAssistantService = App.ServiceProvider?.GetService(typeof(IAIAssistantService)) as IAIAssistantService;
        _projectContextService = App.ServiceProvider?.GetService(typeof(ProjectContextService)) as ProjectContextService;
        _projectReadModelService = App.ServiceProvider?.GetService(typeof(ProjectReadModelService)) as ProjectReadModelService;

        _modeComboBox = new ComboBox
        {
            Margin = new Thickness(0, 0, 0, 12),
            ItemsSource = new[]
            {
                new ComboBoxItem { Tag = "角色补全", Content = T("AAW.ModeCharacter", "角色补全") },
                new ComboBoxItem { Tag = "剧情补全", Content = T("AAW.ModePlot", "剧情补全") },
                new ComboBoxItem { Tag = "世界设定补全", Content = T("AAW.ModeWorld", "世界设定补全") },
                new ComboBoxItem { Tag = "书籍大纲", Content = T("AAW.ModeOutline", "书籍大纲") },
            },
            SelectedIndex = 0
        };

        _promptTextBox = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 120,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 0, 0, 12),
            Text = T("AAW.PromptPlaceholder", "请输入你的创作需求，AI 会返回可直接用于继续编辑的内容。")
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
            Text = _aiAssistantService == null ? T("AAW.ServiceNotInit", "AI助手服务未初始化") : T("AAW.StatusReady", "就绪")
        };

        _generateButton = new Button
        {
            Content = T("AAW.StartGenerate", "开始生成"),
            Padding = new Thickness(16, 8, 16, 8),
            MinWidth = 120
        };
        _generateButton.Click += async (_, _) => await GenerateAsync();

        var outlineButton = new Button
        {
            Content = T("AAW.OpenOutlineGen", "打开AI大纲生成器"),
            Margin = new Thickness(12, 0, 0, 0),
            Padding = new Thickness(16, 8, 16, 8)
        };
        outlineButton.Click += (_, _) => OpenOutlineGenerator();

        var prerequisiteButton = new Button
        {
            Content = T("AAW.PrerequisiteGen", "前置条件生成"),
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
                        Text = T("AAW.Title", "AI 工作台"),
                        FontSize = 28,
                        FontWeight = FontWeights.Bold
                    },
                    new TextBlock
                    {
                        Margin = new Thickness(0, 8, 0, 16),
                        Text = T("AAW.Subtitle", "这里提供稳定版 AI 助手入口，可直接生成角色、剧情、世界设定或书籍大纲。"),
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock { Text = T("AAW.ModeLabel", "生成模式") },
                    _modeComboBox,
                    new TextBlock { Text = T("AAW.InputLabel", "输入需求") },
                    _promptTextBox,
                    actionPanel,
                    new TextBlock { Text = T("AAW.ResultLabel", "生成结果") },
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
            MessageBox.Show(T("AAW.ServiceNotInitMsg", "AI助手服务未初始化。"), T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var prompt = _promptTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            MessageBox.Show(T("AAW.EnterRequest", "请输入生成需求。"), T("Msg.Tip", "提示"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            _generateButton.IsEnabled = false;
            _statusTextBlock.Text = T("AAW.Generating", "AI 正在生成...");
            var mode = (_modeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "角色补全";
            var requirements = await BuildWorkspaceRequirementsAsync(mode, prompt);

            AIAssistantResult result = mode switch
            {
                "角色补全" => await _aiAssistantService.GenerateCharacterAsync(new Dictionary<string, object>
                {
                    ["characterType"] = "主配角",
                    ["backgroundStory"] = prompt,
                    ["requirements"] = requirements,
                    ["projectContext"] = requirements
                }),
                "剧情补全" => await _aiAssistantService.GeneratePlotAsync(new Dictionary<string, object>
                {
                    ["plotType"] = "主线",
                    ["theme"] = prompt,
                    ["requirements"] = requirements,
                    ["projectContext"] = requirements
                }),
                "世界设定补全" => await _aiAssistantService.GeneratePlotAsync(new Dictionary<string, object>
                {
                    ["plotType"] = "世界设定",
                    ["theme"] = prompt,
                    ["requirements"] = requirements,
                    ["projectContext"] = requirements
                }),
                "书籍大纲" => await _aiAssistantService.GenerateOutlineAsync(new Dictionary<string, object>
                {
                    ["theme"] = prompt,
                    ["novelType"] = "演示项目",
                    ["targetLength"] = "长篇",
                    ["requirements"] = requirements,
                    ["projectContext"] = requirements
                }),
                _ => await _aiAssistantService.GeneratePlotAsync(new Dictionary<string, object>
                {
                    ["theme"] = prompt,
                    ["requirements"] = requirements,
                    ["projectContext"] = requirements
                })
            };

            if (!result.IsSuccess)
            {
                _resultTextBox.Text = result.Message ?? T("AAW.FailedFallback", "AI 生成失败。");
                _statusTextBlock.Text = T("AAW.GenerateFailed", "生成失败");
                return;
            }

            _resultTextBox.Text = result.Data?.ToString() ?? result.Message ?? T("AAW.DoneFallback", "AI 已完成生成。");
            _statusTextBlock.Text = T("AAW.GenerateDone", "生成完成");
        }
        catch (Exception ex)
        {
            _resultTextBox.Text = ex.ToString();
            _statusTextBlock.Text = T("AAW.GenerateError", "生成异常");
        }
        finally
        {
            _generateButton.IsEnabled = true;
        }
    }

    private async Task<string> BuildWorkspaceRequirementsAsync(string mode, string prompt)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"当前模式：{mode}");
        builder.AppendLine("请先遵循项目基础信息、已有世界设定和已有大纲，再响应本次需求。");
        builder.AppendLine("不要输出思考过程、解释性前言或 Markdown 代码块。");
        builder.AppendLine();

        if (_projectReadModelService != null && _projectContextService?.CurrentProjectId is Guid projectId && projectId != Guid.Empty)
        {
            try
            {
                var projectContext = await _projectReadModelService.BuildAiContextDataAsync(projectId);
                if (!string.IsNullOrWhiteSpace(projectContext.PromptSummary))
                {
                    builder.AppendLine(projectContext.PromptSummary);
                    builder.AppendLine();
                }
            }
            catch
            {
            }
        }

        builder.AppendLine("当前需求：");
        builder.AppendLine(prompt);
        builder.AppendLine();
        builder.AppendLine("模式约束：");
        builder.AppendLine(mode switch
        {
            "角色补全" => "只输出角色相关内容，不要扩展成世界设定、剧情大纲或正文章节。",
            "剧情补全" => "只输出剧情/大纲层内容，不要扩展成世界设定表单或正文章节。",
            "世界设定补全" => "只输出世界设定相关内容，不要扩展成剧情大纲、角色小传或正文章节。",
            "书籍大纲" => "只输出书籍大纲相关内容，且必须建立在已有世界设定之上。",
            _ => "输出内容必须与当前模式一致。"
        });

        return builder.ToString().Trim();
    }

    private void OpenOutlineGenerator()
    {
        if (_projectContextService?.CurrentProjectId == null)
        {
            MessageBox.Show(T("AAW.OpenProjectFirstOutline", "请先打开一个项目，再使用 AI 大纲生成器。"), T("Msg.Tip", "提示"), MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show(T("AAW.OpenProjectFirstPrerequisite", "请先打开一个项目，再生成前置条件。"), T("Msg.Tip", "提示"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new PrerequisiteGenerationDialog(_projectContextService.CurrentProjectId.Value)
        {
            Owner = Window.GetWindow(this)
        };
        dialog.ShowDialog();
    }
}
