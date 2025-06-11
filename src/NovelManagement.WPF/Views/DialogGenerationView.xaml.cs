using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Agents;
using NovelManagement.AI.Interfaces;
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
        private bool _isGenerating = false;

        /// <summary>
        /// 对话生成结果
        /// </summary>
        public class DialogGenerationResult
        {
            public string Content { get; set; } = string.Empty;
            public double QualityScore { get; set; }
            public string Characters { get; set; } = string.Empty;
            public string Situation { get; set; } = string.Empty;
            public string Emotion { get; set; } = string.Empty;
            public string Style { get; set; } = string.Empty;
            public DateTime GeneratedAt { get; set; }
            public string? ThinkingChainId { get; set; }
        }

        #endregion

        #region 构造函数

        public DialogGenerationView()
        {
            InitializeComponent();
            InitializeControls();

            // 在实际应用中，这些服务应该通过依赖注入获取
            try
            {
                _logger = App.ServiceProvider?.GetService(typeof(ILogger<DialogGenerationView>)) as ILogger<DialogGenerationView>;
                var serviceLogger = App.ServiceProvider?.GetService(typeof(ILogger<DialogGenerationService>)) as ILogger<DialogGenerationService>;
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
            // TODO: 实现角色选择对话框
            MessageBox.Show("角色选择功能正在开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现保存到项目功能
            MessageBox.Show("保存功能正在开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 导出按钮点击事件
        /// </summary>
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现导出功能
            MessageBox.Show("导出功能正在开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 保存模板按钮点击事件
        /// </summary>
        private void SaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现模板保存功能
            MessageBox.Show("模板保存功能正在开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 加载模板按钮点击事件
        /// </summary>
        private void LoadTemplate_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现模板加载功能
            MessageBox.Show("模板加载功能正在开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
    }
}
