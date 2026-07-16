using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using NovelManagement.AI.Agents;
using NovelManagement.AI.Interfaces;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.WPF.Services;
using MaterialDesignThemes.Wpf;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// DialogGenerationView.xaml 的交互逻辑
    /// </summary>
    public partial class DialogGenerationView : UserControl
    {
        #region 字段和属性

        private readonly ILogger<DialogGenerationView>? _logger;
        private readonly DialogGenerationService _dialogGenerationService;
        private readonly ProjectContextService? _projectContextService;
        private readonly CurrentProjectGuard? _currentProjectGuard;
        private readonly CharacterService? _characterService;
        private readonly VolumeService? _volumeService;
        private readonly ChapterService? _chapterService;
        private DialogGenerationResult? _currentResult;
        private bool _isGenerating = false;

        /// <summary>
        /// 对话生成结果
        /// </summary>
        public class DialogGenerationResult
        {
            /// <summary>
            /// 生成的对话内容。
            /// </summary>
            public string Content { get; set; } = string.Empty;

            /// <summary>
            /// 对话质量评分。
            /// </summary>
            public double QualityScore { get; set; }

            /// <summary>
            /// 参与角色。
            /// </summary>
            public string Characters { get; set; } = string.Empty;

            /// <summary>
            /// 对话发生情境。
            /// </summary>
            public string Situation { get; set; } = string.Empty;

            /// <summary>
            /// 情绪基调。
            /// </summary>
            public string Emotion { get; set; } = string.Empty;

            /// <summary>
            /// 对话风格。
            /// </summary>
            public string Style { get; set; } = string.Empty;

            /// <summary>
            /// 生成时间。
            /// </summary>
            public DateTime GeneratedAt { get; set; }

            /// <summary>
            /// 关联的思维链标识。
            /// </summary>
            public string? ThinkingChainId { get; set; }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化对话生成视图。
        /// </summary>
        public DialogGenerationView()
        {
            InitializeComponent();
            InitializeControls();

            // 在实际应用中，这些服务应该通过依赖注入获取
            try
            {
                _logger = App.ServiceProvider?.GetService(typeof(ILogger<DialogGenerationView>)) as ILogger<DialogGenerationView>;
                var serviceLogger = App.ServiceProvider?.GetService(typeof(ILogger<DialogGenerationService>)) as ILogger<DialogGenerationService>;
                _projectContextService = App.ServiceProvider?.GetService<ProjectContextService>();
                _currentProjectGuard = App.ServiceProvider?.GetService<CurrentProjectGuard>();
                _characterService = App.ServiceProvider?.GetService<CharacterService>();
                _volumeService = App.ServiceProvider?.GetService<VolumeService>();
                _chapterService = App.ServiceProvider?.GetService<ChapterService>();
                _dialogGenerationService = new DialogGenerationService(serviceLogger);
            }
            catch (Exception ex)
            {
                _dialogGenerationService = new DialogGenerationService();
                MessageBox.Show($"服务初始化失败，使用默认服务: {ex.Message}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化控件
        /// </summary>
        private void InitializeControls()
        {
            // 设置默认值
            RelationshipComboBox.SelectedIndex = 0;
            PurposeComboBox.SelectedIndex = 0;
            EmotionComboBox.SelectedIndex = 0;
            StyleComboBox.SelectedIndex = 0;

            // 绑定滑块事件
            LengthSlider.ValueChanged += LengthSlider_ValueChanged;
            UpdateLengthLabel();
        }

        /// <summary>
        /// 更新长度标签
        /// </summary>
        private void UpdateLengthLabel()
        {
            var value = (int)LengthSlider.Value;
            LengthLabel.Text = value switch
            {
                1 or 2 => "很短",
                3 or 4 => "较短", 
                5 or 6 => "中等",
                7 or 8 => "较长",
                9 or 10 => "很长",
                _ => "中等"
            };
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 长度滑块值改变事件
        /// </summary>
        private void LengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateLengthLabel();
        }

        /// <summary>
        /// 生成对话按钮点击事件
        /// </summary>
        private async void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (_isGenerating) return;

            try
            {
                _isGenerating = true;
                ShowLoading("正在生成对话...");

                var parameters = GetGenerationParameters();
                var result = await GenerateDialogueAsync(parameters);

                if (result != null)
                {
                    DisplayResult(result);
                    _logger?.LogInformation("对话生成成功，质量评分: {QualityScore}", result.QualityScore);
                }
                else
                {
                    MessageBox.Show("对话生成失败，请检查参数设置", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "对话生成过程中发生错误");
                MessageBox.Show($"生成失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isGenerating = false;
                HideLoading();
            }
        }

        /// <summary>
        /// 优化对话按钮点击事件
        /// </summary>
        private async void Optimize_Click(object sender, RoutedEventArgs e)
        {
            if (_isGenerating || string.IsNullOrEmpty(ResultTextBox.Text)) return;

            try
            {
                _isGenerating = true;
                ShowLoading("正在优化对话...");

                var parameters = GetGenerationParameters();
                parameters["existingDialogue"] = ResultTextBox.Text;
                parameters["optimizationMode"] = true;

                var result = await GenerateDialogueAsync(parameters);

                if (result != null)
                {
                    DisplayResult(result);
                    _logger?.LogInformation("对话优化成功，质量评分: {QualityScore}", result.QualityScore);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "对话优化过程中发生错误");
                MessageBox.Show($"优化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isGenerating = false;
                HideLoading();
            }
        }

        /// <summary>
        /// 从角色库选择按钮点击事件
        /// </summary>
        private void SelectCharacters_Click(object sender, RoutedEventArgs e)
        {
            _ = SelectCharactersAsync();
        }

        /// <summary>
        /// 复制按钮点击事件
        /// </summary>
        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ResultTextBox.Text))
            {
                Clipboard.SetText(ResultTextBox.Text);
                MessageBox.Show("对话内容已复制到剪贴板", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 保存按钮点击事件
        /// </summary>
        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResultTextBox.Text))
            {
                MessageBox.Show("当前没有可保存的对话内容。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (_currentProjectGuard == null || !_currentProjectGuard.TryGetCurrentProjectId(Window.GetWindow(this), "保存对话到项目", out var projectId))
                {
                    return;
                }

                if (_volumeService == null || _chapterService == null)
                {
                    MessageBox.Show("章节或卷宗服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var volume = await EnsureDialogueVolumeAsync(projectId);
                var chapter = new Chapter
                {
                    VolumeId = volume.Id,
                    Title = BuildDialogueDraftTitle(),
                    Summary = SituationTextBox.Text?.Trim(),
                    Content = ResultTextBox.Text,
                    Status = "Draft",
                    Type = "DialogueDraft",
                    Tags = BuildDialogueTags(),
                    Notes = BuildDialogueNotes()
                };

                await _chapterService.CreateChapterAsync(chapter);
                MessageBox.Show($"已保存到项目章节草稿：{chapter.Title}", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存对话到项目失败");
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出按钮点击事件
        /// </summary>
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResultTextBox.Text))
            {
                MessageBox.Show("当前没有可导出的对话内容。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "导出对话",
                    Filter = "Markdown 文件|*.md|文本文件|*.txt|JSON 文件|*.json",
                    FileName = $"{BuildDialogueDraftTitle().Replace(' ', '_')}.md",
                    AddExtension = true
                };

                if (dialog.ShowDialog(Window.GetWindow(this)) != true)
                {
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(dialog.FileName)!);
                var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
                var content = extension switch
                {
                    ".json" => JsonSerializer.Serialize(new
                    {
                        Characters = CharactersTextBox.Text?.Trim(),
                        Situation = SituationTextBox.Text?.Trim(),
                        Purpose = (PurposeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                        Emotion = (EmotionComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                        Style = (StyleComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                        QualityScore = _currentResult?.QualityScore,
                        Content = ResultTextBox.Text,
                        ExportedAt = DateTime.Now
                    }, new JsonSerializerOptions { WriteIndented = true }),
                    ".txt" => ResultTextBox.Text,
                    _ => BuildMarkdownExport()
                };

                File.WriteAllText(dialog.FileName, content);
                MessageBox.Show($"已导出到：{dialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出对话失败");
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存模板按钮点击事件
        /// </summary>
        private void SaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var inputDialog = new TextInputDialog("保存对话模板", "模板名称", $"对话模板_{DateTime.Now:yyyyMMdd_HHmmss}")
                {
                    Owner = Window.GetWindow(this)
                };

                if (inputDialog.ShowDialog() != true)
                {
                    return;
                }

                var template = BuildTemplate(inputDialog.InputText);
                var templatePath = Path.Combine(GetDialogTemplateDirectory(), $"{SanitizeFileName(template.Name)}.json");
                Directory.CreateDirectory(Path.GetDirectoryName(templatePath)!);
                File.WriteAllText(templatePath, JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true }));
                MessageBox.Show($"模板已保存：{templatePath}", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存对话模板失败");
                MessageBox.Show($"模板保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载模板按钮点击事件
        /// </summary>
        private void LoadTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var templateDirectory = GetDialogTemplateDirectory();
                Directory.CreateDirectory(templateDirectory);

                var dialog = new OpenFileDialog
                {
                    Title = "加载对话模板",
                    Filter = "对话模板|*.json",
                    InitialDirectory = templateDirectory
                };

                if (dialog.ShowDialog(Window.GetWindow(this)) != true)
                {
                    return;
                }

                var template = JsonSerializer.Deserialize<DialogTemplate>(File.ReadAllText(dialog.FileName));
                if (template == null)
                {
                    MessageBox.Show("模板内容无效。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ApplyTemplate(template);
                MessageBox.Show($"模板已加载：{template.Name}", "加载成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载对话模板失败");
                MessageBox.Show($"模板加载失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 帮助按钮点击事件
        /// </summary>
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            var helpText = @"AI对话生成器使用说明:

1. 角色设置: 输入参与对话的角色名称, 选择角色关系
2. 情境设置: 描述对话场景和目的
3. 风格设置: 选择情感基调、语言风格和对话长度
4. 点击""生成对话""按钮开始生成
5. 可以对生成的对话进行优化
6. 支持复制、保存和导出功能

提示:
- 角色名称用逗号分隔
- 详细的场景描述有助于生成更好的对话
- 可以多次优化以获得满意的结果";

            MessageBox.Show(helpText, "帮助", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region 核心功能

        /// <summary>
        /// 获取生成参数
        /// </summary>
        /// <returns>参数字典</returns>
        private Dictionary<string, object> GetGenerationParameters()
        {
            return new Dictionary<string, object>
            {
                ["characters"] = CharactersTextBox.Text?.Trim() ?? "",
                ["relationship"] = (RelationshipComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
                ["situation"] = SituationTextBox.Text?.Trim() ?? "",
                ["purpose"] = (PurposeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
                ["emotion"] = (EmotionComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
                ["style"] = (StyleComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
                ["length"] = (int)LengthSlider.Value
            };
        }

        /// <summary>
        /// 生成对话
        /// </summary>
        /// <param name="parameters">生成参数</param>
        /// <returns>生成结果</returns>
        private async Task<DialogGenerationResult?> GenerateDialogueAsync(Dictionary<string, object> parameters)
        {
            try
            {
                // 检查是否为优化模式
                var isOptimization = parameters.ContainsKey("optimizationMode") &&
                                   Convert.ToBoolean(parameters["optimizationMode"]);

                DialogGenerationService.DialogGenerationResult result;

                if (isOptimization && parameters.ContainsKey("existingDialogue"))
                {
                    var existingDialogue = parameters["existingDialogue"].ToString() ?? "";
                    result = await _dialogGenerationService.OptimizeDialogueAsync(existingDialogue, parameters);
                }
                else
                {
                    result = await _dialogGenerationService.GenerateDialogueAsync(parameters);
                }

                // 转换为本地结果类型
                return new DialogGenerationResult
                {
                    Content = result.Content,
                    QualityScore = result.QualityScore,
                    Characters = result.Characters,
                    Situation = result.Situation,
                    Emotion = result.Emotion,
                    Style = result.Style,
                    GeneratedAt = result.GeneratedAt,
                    ThinkingChainId = result.ThinkingChainId
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "对话生成失败");
                throw;
            }
        }

        /// <summary>
        /// 显示生成结果
        /// </summary>
        /// <param name="result">生成结果</param>
        private void DisplayResult(DialogGenerationResult result)
        {
            _currentResult = result;
            ResultTextBox.Text = result.Content;
            
            // 更新质量评分显示
            QualityChip.Content = $"质量评分: {result.QualityScore:F1}/10";
            
            // 根据评分设置颜色
            if (result.QualityScore >= 8.0)
            {
                QualityChip.Background = new SolidColorBrush(Colors.Green);
            }
            else if (result.QualityScore >= 6.0)
            {
                QualityChip.Background = new SolidColorBrush(Colors.Orange);
            }
            else
            {
                QualityChip.Background = new SolidColorBrush(Colors.Red);
            }
        }

        /// <summary>
        /// 显示加载状态
        /// </summary>
        /// <param name="message">加载消息</param>
        private void ShowLoading(string message)
        {
            LoadingText.Text = message;
            LoadingOverlay.Visibility = Visibility.Visible;
            GenerateButton.IsEnabled = false;
            OptimizeButton.IsEnabled = false;
        }

        /// <summary>
        /// 隐藏加载状态
        /// </summary>
        private void HideLoading()
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            GenerateButton.IsEnabled = true;
            OptimizeButton.IsEnabled = true;
        }

        #endregion

        private async Task SelectCharactersAsync()
        {
            try
            {
                if (_characterService == null)
                {
                    MessageBox.Show("角色服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_currentProjectGuard == null || !_currentProjectGuard.TryGetCurrentProjectId(Window.GetWindow(this), "从角色库选择", out var projectId))
                {
                    return;
                }

                var characters = (await _characterService.GetCharactersByProjectIdAsync(projectId)).ToList();
                if (characters.Count == 0)
                {
                    MessageBox.Show("当前项目还没有角色，请先创建角色。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new CharacterSelectionDialog(characters.Select(c => c.Name).ToList())
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    CharactersTextBox.Text = string.Join(",", dialog.SelectedCharacters);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "从角色库选择失败");
                MessageBox.Show($"加载角色失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<Volume> EnsureDialogueVolumeAsync(Guid projectId)
        {
            var volumes = (await _volumeService!.GetVolumeListAsync(projectId)).ToList();
            var existing = volumes.FirstOrDefault(v => v.Title == "AI对话草稿");
            if (existing != null)
            {
                return existing;
            }

            return await _volumeService.CreateVolumeAsync(new Volume
            {
                ProjectId = projectId,
                Title = "AI对话草稿",
                Description = "用于存放 AI 对话生成器保存的章节草稿",
                Status = "Planning",
                Type = "AI",
                Notes = "系统自动创建"
            });
        }

        private string BuildDialogueDraftTitle()
        {
            var characters = CharactersTextBox.Text?.Trim();
            var purpose = (PurposeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "对话";
            return string.IsNullOrWhiteSpace(characters)
                ? $"AI对话草稿_{DateTime.Now:yyyyMMdd_HHmmss}"
                : $"{characters} - {purpose}";
        }

        private string BuildDialogueTags()
        {
            return string.Join(",", new[]
            {
                "AI对话",
                (EmotionComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                (StyleComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                (RelationshipComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()
            }.Where(v => !string.IsNullOrWhiteSpace(v)));
        }

        private string BuildDialogueNotes()
        {
            return $"场景：{SituationTextBox.Text?.Trim()}{Environment.NewLine}" +
                   $"角色：{CharactersTextBox.Text?.Trim()}{Environment.NewLine}" +
                   $"情绪：{(EmotionComboBox.SelectedItem as ComboBoxItem)?.Content}{Environment.NewLine}" +
                   $"风格：{(StyleComboBox.SelectedItem as ComboBoxItem)?.Content}{Environment.NewLine}" +
                   $"质量评分：{_currentResult?.QualityScore:F1}";
        }

        private string BuildMarkdownExport()
        {
            return $"# {BuildDialogueDraftTitle()}{Environment.NewLine}{Environment.NewLine}" +
                   $"- 角色：{CharactersTextBox.Text?.Trim()}{Environment.NewLine}" +
                   $"- 关系：{(RelationshipComboBox.SelectedItem as ComboBoxItem)?.Content}{Environment.NewLine}" +
                   $"- 场景：{SituationTextBox.Text?.Trim()}{Environment.NewLine}" +
                   $"- 目的：{(PurposeComboBox.SelectedItem as ComboBoxItem)?.Content}{Environment.NewLine}" +
                   $"- 情绪：{(EmotionComboBox.SelectedItem as ComboBoxItem)?.Content}{Environment.NewLine}" +
                   $"- 风格：{(StyleComboBox.SelectedItem as ComboBoxItem)?.Content}{Environment.NewLine}" +
                   $"- 质量评分：{_currentResult?.QualityScore:F1}{Environment.NewLine}{Environment.NewLine}" +
                   $"## 对话内容{Environment.NewLine}{Environment.NewLine}{ResultTextBox.Text}";
        }

        private DialogTemplate BuildTemplate(string name)
        {
            return new DialogTemplate
            {
                Name = name,
                Characters = CharactersTextBox.Text?.Trim() ?? string.Empty,
                Relationship = (RelationshipComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty,
                Situation = SituationTextBox.Text?.Trim() ?? string.Empty,
                Purpose = (PurposeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty,
                Emotion = (EmotionComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty,
                Style = (StyleComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty,
                Length = (int)LengthSlider.Value,
                SavedAt = DateTime.Now
            };
        }

        private void ApplyTemplate(DialogTemplate template)
        {
            CharactersTextBox.Text = template.Characters;
            SituationTextBox.Text = template.Situation;
            SetComboBoxSelection(RelationshipComboBox, template.Relationship);
            SetComboBoxSelection(PurposeComboBox, template.Purpose);
            SetComboBoxSelection(EmotionComboBox, template.Emotion);
            SetComboBoxSelection(StyleComboBox, template.Style);
            LengthSlider.Value = Math.Clamp(template.Length, (int)LengthSlider.Minimum, (int)LengthSlider.Maximum);
            UpdateLengthLabel();
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

        private static string GetDialogTemplateDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NovelManagement",
                "config",
                "dialog-templates");
        }

        private static string SanitizeFileName(string name)
        {
            return string.Concat(name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        }
    }

    internal sealed class DialogTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string Characters { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public string Situation { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string Emotion { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;
        public int Length { get; set; }
        public DateTime SavedAt { get; set; }
    }

    internal sealed class TextInputDialog : Window
    {
        private readonly TextBox _textBox;
        public string InputText => _textBox.Text.Trim();

        public TextInputDialog(string title, string label, string defaultText = "")
        {
            Title = title;
            Width = 420;
            Height = 180;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _textBox = new TextBox { Text = defaultText };

            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 8) });
            panel.Children.Add(_textBox);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };

            var okButton = new Button { Content = "确定", Width = 84, Margin = new Thickness(0, 0, 12, 0), IsDefault = true };
            okButton.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(InputText))
                {
                    MessageBox.Show("请输入内容。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DialogResult = true;
            };

            var cancelButton = new Button { Content = "取消", Width = 84, IsCancel = true };
            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);
            panel.Children.Add(buttons);
            Content = panel;
        }
    }

    internal sealed class CharacterSelectionDialog : Window
    {
        private readonly List<CheckBox> _checkBoxes = new();
        public IReadOnlyList<string> SelectedCharacters => _checkBoxes.Where(cb => cb.IsChecked == true)
            .Select(cb => cb.Content?.ToString() ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        public CharacterSelectionDialog(IReadOnlyList<string> characterNames)
        {
            Title = "从角色库选择";
            Width = 360;
            Height = 460;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock { Text = "请选择参与对话的角色：", Margin = new Thickness(0, 0, 0, 12) });

            var listPanel = new StackPanel();
            foreach (var name in characterNames)
            {
                var checkbox = new CheckBox { Content = name, Margin = new Thickness(0, 4, 0, 0) };
                _checkBoxes.Add(checkbox);
                listPanel.Children.Add(checkbox);
            }

            panel.Children.Add(new ScrollViewer
            {
                Height = 320,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = listPanel
            });

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };

            var okButton = new Button { Content = "确定", Width = 84, Margin = new Thickness(0, 0, 12, 0), IsDefault = true };
            okButton.Click += (_, _) =>
            {
                if (SelectedCharacters.Count == 0)
                {
                    MessageBox.Show("请至少选择一个角色。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DialogResult = true;
            };

            var cancelButton = new Button { Content = "取消", Width = 84, IsCancel = true };
            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);
            panel.Children.Add(buttons);
            Content = panel;
        }
    }
}
