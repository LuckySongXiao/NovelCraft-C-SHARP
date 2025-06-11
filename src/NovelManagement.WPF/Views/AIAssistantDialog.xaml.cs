using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// AI助手对话框
    /// </summary>
    public partial class AIAssistantDialog : Window
    {
        #region 字段和属性

        private readonly string _contextInfo;
        private readonly ILogger<AIAssistantDialog>? _logger;
        private bool _isProcessing = false;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="contextInfo">上下文信息</param>
        public AIAssistantDialog(string title, string contextInfo)
        {
            InitializeComponent();
            
            _contextInfo = contextInfo;
            
            // 设置标题
            TitleTextBlock.Text = $"AI助手 - {title}";
            
            // 设置上下文信息
            ContextTextBlock.Text = contextInfo;
            
            // 设置默认模型
            ModelComboBox.SelectedIndex = 0;
            
            // 获取日志记录器
            try
            {
                _logger = App.ServiceProvider?.GetService<ILogger<AIAssistantDialog>>();
            }
            catch
            {
                // 忽略日志记录器获取失败
            }
            
            // 设置焦点到输入框
            Loaded += (s, e) => InputTextBox.Focus();
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 模型选择变化事件
        /// </summary>
        private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModelComboBox.SelectedItem is ComboBoxItem item)
            {
                var modelName = item.Tag?.ToString() ?? "未知模型";
                UpdateStatus($"已选择模型: {modelName}");
            }
        }

        /// <summary>
        /// 设置按钮点击事件
        /// </summary>
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("AI设置功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 生成设定按钮点击事件
        /// </summary>
        private void GenerateSetting_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = "请帮我生成一个新的设定，要求：";
            InputTextBox.Focus();
            InputTextBox.CaretIndex = InputTextBox.Text.Length;
        }

        /// <summary>
        /// 分析设定按钮点击事件
        /// </summary>
        private void AnalyzeSetting_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = "请分析当前选中的设定，包括其合理性、完整性和与其他设定的关联性：";
            InputTextBox.Focus();
            InputTextBox.CaretIndex = InputTextBox.Text.Length;
        }

        /// <summary>
        /// 优化设定按钮点击事件
        /// </summary>
        private void OptimizeSetting_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = "请为当前设定提供优化建议，包括如何增强其吸引力和逻辑性：";
            InputTextBox.Focus();
            InputTextBox.CaretIndex = InputTextBox.Text.Length;
        }

        /// <summary>
        /// 一致性检查按钮点击事件
        /// </summary>
        private void CheckConsistency_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = "请检查当前设定与整个世界观的一致性，指出可能的冲突或矛盾：";
            InputTextBox.Focus();
            InputTextBox.CaretIndex = InputTextBox.Text.Length;
        }

        /// <summary>
        /// 输入框按键事件
        /// </summary>
        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                if (!string.IsNullOrWhiteSpace(InputTextBox.Text) && !_isProcessing)
                {
                    SendMessage();
                }
            }
        }

        /// <summary>
        /// 发送按钮点击事件
        /// </summary>
        private void Send_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(InputTextBox.Text) && !_isProcessing)
            {
                SendMessage();
            }
        }

        /// <summary>
        /// 清空对话按钮点击事件
        /// </summary>
        private void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要清空对话历史吗？", "确认", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                ClearChatHistory();
            }
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 发送消息
        /// </summary>
        private async void SendMessage()
        {
            var message = InputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            try
            {
                _isProcessing = true;
                UpdateStatus("正在处理...");
                SendButton.IsEnabled = false;

                // 添加用户消息到对话
                AddUserMessage(message);
                
                // 清空输入框
                InputTextBox.Clear();

                // 模拟AI响应（实际应用中应该调用真实的AI服务）
                var response = await SimulateAIResponse(message);
                
                // 添加AI响应到对话
                AddAIMessage(response);
                
                UpdateStatus("就绪");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "发送消息时发生错误");
                AddAIMessage($"抱歉，处理您的请求时发生错误：{ex.Message}");
                UpdateStatus("错误");
            }
            finally
            {
                _isProcessing = false;
                SendButton.IsEnabled = true;
                InputTextBox.Focus();
            }
        }

        /// <summary>
        /// 模拟AI响应
        /// </summary>
        private async Task<string> SimulateAIResponse(string userMessage)
        {
            // 模拟网络延迟
            await Task.Delay(1000);

            // 根据用户消息生成模拟响应
            if (userMessage.Contains("生成") || userMessage.Contains("创建"))
            {
                return "我理解您想要生成新的设定。基于当前上下文，我建议创建一个具有以下特点的设定：\n\n" +
                       "1. 与现有世界观保持一致\n" +
                       "2. 具有独特的特色和吸引力\n" +
                       "3. 为后续剧情发展留有空间\n\n" +
                       "请告诉我您希望生成什么类型的设定，我会为您提供详细的建议。";
            }
            else if (userMessage.Contains("分析"))
            {
                return "根据您提供的上下文信息，我对当前设定进行了分析：\n\n" +
                       "**优点：**\n" +
                       "- 设定逻辑清晰，符合世界观\n" +
                       "- 具有良好的扩展性\n\n" +
                       "**建议改进：**\n" +
                       "- 可以增加更多细节描述\n" +
                       "- 考虑与其他设定的关联性\n\n" +
                       "您希望我重点分析哪个方面？";
            }
            else if (userMessage.Contains("优化"))
            {
                return "基于当前设定，我提供以下优化建议：\n\n" +
                       "1. **增强独特性**：添加更多独特元素，使设定更加出色\n" +
                       "2. **完善细节**：补充必要的背景信息和具体描述\n" +
                       "3. **平衡性调整**：确保设定在整个体系中保持平衡\n" +
                       "4. **关联性强化**：与其他相关设定建立更紧密的联系\n\n" +
                       "您希望我详细展开哪个方面的建议？";
            }
            else if (userMessage.Contains("一致性") || userMessage.Contains("检查"))
            {
                return "我已对当前设定进行一致性检查：\n\n" +
                       "**检查结果：**\n" +
                       "✅ 与核心世界观一致\n" +
                       "✅ 时间线逻辑正确\n" +
                       "⚠️ 建议检查与相关设定的关联性\n\n" +
                       "**发现的潜在问题：**\n" +
                       "- 某些细节可能需要进一步明确\n" +
                       "- 建议补充相关的背景设定\n\n" +
                       "需要我详细说明任何特定的问题吗？";
            }
            else
            {
                return "感谢您的问题！我正在分析您的需求。基于当前的上下文信息，我可以为您提供以下帮助：\n\n" +
                       "- 生成新的创意设定\n" +
                       "- 分析现有设定的合理性\n" +
                       "- 提供优化和改进建议\n" +
                       "- 检查设定间的一致性\n\n" +
                       "请告诉我您具体需要什么帮助，我会为您提供详细的建议和方案。";
            }
        }

        /// <summary>
        /// 添加用户消息
        /// </summary>
        private void AddUserMessage(string message)
        {
            var messagePanel = CreateMessagePanel(message, true);
            ChatPanel.Children.Add(messagePanel);
            ScrollToBottom();
        }

        /// <summary>
        /// 添加AI消息
        /// </summary>
        private void AddAIMessage(string message)
        {
            var messagePanel = CreateMessagePanel(message, false);
            ChatPanel.Children.Add(messagePanel);
            ScrollToBottom();
        }

        /// <summary>
        /// 创建消息面板
        /// </summary>
        private Border CreateMessagePanel(string message, bool isUser)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 400
            };

            if (isUser)
            {
                border.Background = (Brush)FindResource("PrimaryHueMidBrush");
            }
            else
            {
                border.Background = (Brush)FindResource("MaterialDesignCardBackground");
            }

            var stackPanel = new StackPanel();

            // 添加发送者标识
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 4)
            };

            var icon = new PackIcon
            {
                Kind = isUser ? PackIconKind.Account : PackIconKind.Robot,
                Width = 16,
                Height = 16,
                Foreground = isUser ? Brushes.White : (Brush)FindResource("PrimaryHueMidBrush")
            };

            var senderText = new TextBlock
            {
                Text = isUser ? "您" : "AI助手",
                Style = (Style)FindResource("MaterialDesignCaptionTextBlock"),
                Foreground = isUser ? Brushes.White : (Brush)FindResource("PrimaryHueMidBrush"),
                Margin = new Thickness(4, 0, 0, 0)
            };

            headerPanel.Children.Add(icon);
            headerPanel.Children.Add(senderText);
            stackPanel.Children.Add(headerPanel);

            // 添加消息内容
            var messageText = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Style = (Style)FindResource("MaterialDesignBody2TextBlock"),
                Foreground = isUser ? Brushes.White : (Brush)FindResource("MaterialDesignBody")
            };

            stackPanel.Children.Add(messageText);
            border.Child = stackPanel;

            return border;
        }

        /// <summary>
        /// 滚动到底部
        /// </summary>
        private void ScrollToBottom()
        {
            ChatScrollViewer.ScrollToEnd();
        }

        /// <summary>
        /// 清空对话历史
        /// </summary>
        private void ClearChatHistory()
        {
            // 保留欢迎消息，清除其他消息
            var welcomeMessage = ChatPanel.Children[0];
            ChatPanel.Children.Clear();
            ChatPanel.Children.Add(welcomeMessage);
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        private void UpdateStatus(string status)
        {
            StatusTextBlock.Text = status;
            
            switch (status)
            {
                case "就绪":
                    StatusIcon.Kind = PackIconKind.CheckCircle;
                    StatusIcon.Foreground = Brushes.Green;
                    break;
                case "正在处理...":
                    StatusIcon.Kind = PackIconKind.Loading;
                    StatusIcon.Foreground = Brushes.Orange;
                    break;
                case "错误":
                    StatusIcon.Kind = PackIconKind.AlertCircle;
                    StatusIcon.Foreground = Brushes.Red;
                    break;
                default:
                    StatusIcon.Kind = PackIconKind.Information;
                    StatusIcon.Foreground = Brushes.Blue;
                    break;
            }
        }

        #endregion
    }

    /// <summary>
    /// 大于零转换器
    /// </summary>
    public class GreaterThanZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
                return intValue > 0;
            if (value is string stringValue)
                return !string.IsNullOrWhiteSpace(stringValue);
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
