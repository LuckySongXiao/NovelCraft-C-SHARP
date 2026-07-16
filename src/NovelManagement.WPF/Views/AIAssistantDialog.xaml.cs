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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services;
using NovelManagement.AI.Utilities;

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
        private readonly IConfiguration? _configuration;
        private readonly ModelManager? _modelManager;
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
                _configuration = App.ServiceProvider?.GetService<IConfiguration>();
                _modelManager = App.ServiceProvider?.GetService<ModelManager>();
            }
            catch
            {
                // 忽略服务获取失败
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
            try
            {
                var configWindow = new Window
                {
                    Title = "AI配置管理",
                    Width = 1200,
                    Height = 800,
                    WindowStartupLocation = Owner != null
                        ? WindowStartupLocation.CenterOwner
                        : WindowStartupLocation.CenterScreen,
                    Owner = Owner ?? this,
                    Content = new AIConfigurationView()
                };

                configWindow.ShowDialog();
                UpdateStatus("已打开AI配置");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "打开 AI 配置窗口时发生错误");
                MessageBox.Show($"打开AI配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("错误");
            }
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

                var response = await GetAIResponseAsync(message);
                
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
        /// 调用真实 AI 服务
        /// </summary>
        private async Task<string> GetAIResponseAsync(string userMessage)
        {
            if (_modelManager == null)
            {
                throw new InvalidOperationException("AI 模型管理器未初始化。");
            }

            var preferredProvider = (ModelComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString()
                ?? _configuration?["AI:DefaultProvider"]
                ?? "DeepSeek";
            var providerName = _modelManager.ResolvePreferredProviderName(preferredProvider);
            if (string.IsNullOrWhiteSpace(providerName))
            {
                throw new InvalidOperationException("没有可用的 AI 提供者。");
            }

            var request = new ChatRequest
            {
                Model = ResolveModelName(providerName),
                SystemPrompt = BuildAssistantSystemPrompt(),
                Messages = new List<ChatMessage>
                {
                    new()
                    {
                        Role = "user",
                        Content = userMessage,
                        Timestamp = DateTime.UtcNow
                    }
                },
                Temperature = 0.7,
                MaxTokens = 3000
            };

            var response = await _modelManager.ChatAsync(providerName, request);
            if (!response.IsSuccess)
            {
                throw new InvalidOperationException(response.ErrorMessage ?? "AI 响应失败。");
            }

            var cleanContent = AIOutputSanitizer.ExtractCleanOutput(response.Content);
            if (string.IsNullOrWhiteSpace(cleanContent))
            {
                throw new InvalidOperationException("AI 返回空内容。");
            }

            return cleanContent;
        }

        private string BuildAssistantSystemPrompt()
        {
            var contextBlock = string.IsNullOrWhiteSpace(_contextInfo)
                ? "当前未提供额外上下文。"
                : _contextInfo;

            return
                "你是小说设定管理助手，负责帮助用户生成设定、分析设定、优化设定和做一致性检查。" +
                "允许内部 thinking，但最终输出必须是干净、直接、可执行的中文答复，不要暴露思考过程。" +
                $"{Environment.NewLine}{Environment.NewLine}当前上下文：{Environment.NewLine}{contextBlock}";
        }

        private string ResolveModelName(string providerName)
        {
            var defaultModel = _configuration?[$"AI:Providers:{providerName}:DefaultModel"];
            if (string.IsNullOrWhiteSpace(defaultModel) && string.Equals(providerName, "DeepSeek", StringComparison.OrdinalIgnoreCase))
            {
                defaultModel = _configuration?["AI:Providers:DeepSeek:Model"];
            }

            if (string.IsNullOrWhiteSpace(defaultModel) && string.Equals(providerName, "LlamaCpp", StringComparison.OrdinalIgnoreCase))
            {
                var modelPath = _configuration?["AI:Providers:LlamaCpp:ModelPath"];
                if (!string.IsNullOrWhiteSpace(modelPath))
                {
                    defaultModel = System.IO.Path.GetFileNameWithoutExtension(modelPath);
                }
            }

            return defaultModel ?? string.Empty;
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
        /// <summary>
        /// 将整数值转换为布尔值（大于零返回 true），或将字符串转换为布尔值（非空白返回 true）
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换器参数</param>
        /// <param name="culture">区域信息</param>
        /// <returns>转换结果布尔值</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
                return intValue > 0;
            if (value is string stringValue)
                return !string.IsNullOrWhiteSpace(stringValue);
            return false;
        }

        /// <summary>
        /// 反向转换，此转换器不支持
        /// </summary>
        /// <param name="value">要转换回的值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换器参数</param>
        /// <param name="culture">区域信息</param>
        /// <returns>未实现</returns>
        /// <exception cref="NotImplementedException">始终抛出</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
