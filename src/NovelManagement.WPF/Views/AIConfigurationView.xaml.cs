using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Services;
using NovelManagement.AI.Services.Ollama;
using NovelManagement.AI.Services.Ollama.Models;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// AI配置管理界面
    /// </summary>
    public partial class AIConfigurationView : UserControl
    {
        private readonly ILogger<AIConfigurationView> _logger;
        private readonly IConfiguration _configuration;
        private readonly ModelManager _modelManager;
        private readonly IOllamaApiService _ollamaService;
        private readonly ObservableCollection<OllamaModelViewModel> _models;

        public AIConfigurationView()
        {
            InitializeComponent();
            
            // 获取服务
            var serviceProvider = App.ServiceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<AIConfigurationView>>();
            _configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _modelManager = serviceProvider.GetRequiredService<ModelManager>();
            _ollamaService = serviceProvider.GetRequiredService<IOllamaApiService>();
            
            _models = new ObservableCollection<OllamaModelViewModel>();
            ModelsDataGrid.ItemsSource = _models;
            
            Loaded += AIConfigurationView_Loaded;
        }

        private async void AIConfigurationView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadConfigurationAsync();
                await CheckProvidersStatusAsync();
                await LoadOllamaModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载AI配置界面失败");
                UpdateStatus($"加载失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        private async Task LoadConfigurationAsync()
        {
            try
            {
                // 加载默认提供者
                var defaultProvider = _configuration["AI:DefaultProvider"];
                foreach (ComboBoxItem item in DefaultProviderComboBox.Items)
                {
                    if (item.Tag?.ToString() == defaultProvider)
                    {
                        DefaultProviderComboBox.SelectedItem = item;
                        break;
                    }
                }

                // 加载Ollama配置
                var ollamaConfig = _configuration.GetSection("AI:Providers:Ollama");
                OllamaBaseUrlTextBox.Text = ollamaConfig["BaseUrl"] ?? "http://localhost:11434";
                OllamaTimeoutTextBox.Text = ollamaConfig["TimeoutSeconds"] ?? "120";
                OllamaMaxRetriesTextBox.Text = ollamaConfig["MaxRetries"] ?? "3";
                OllamaEnableGPUCheckBox.IsChecked = bool.Parse(ollamaConfig["EnableGPU"] ?? "true");
                OllamaGPUDeviceIdTextBox.Text = ollamaConfig["GPUDeviceId"] ?? "-1";
                OllamaVerboseLoggingCheckBox.IsChecked = bool.Parse(ollamaConfig["EnableVerboseLogging"] ?? "false");

                // 加载DeepSeek配置
                var deepSeekConfig = _configuration.GetSection("AI:Providers:DeepSeek");
                DeepSeekBaseUrlTextBox.Text = deepSeekConfig["BaseUrl"] ?? "https://api.deepseek.com";
                DeepSeekTimeoutTextBox.Text = deepSeekConfig["TimeoutSeconds"] ?? "60";
                DeepSeekEnableThinkingChainCheckBox.IsChecked = bool.Parse(deepSeekConfig["EnableThinkingChain"] ?? "true");
                
                // 设置DeepSeek模型
                var deepSeekModel = deepSeekConfig["DefaultModel"] ?? "deepseek-chat";
                foreach (ComboBoxItem item in DeepSeekModelComboBox.Items)
                {
                    if (item.Content?.ToString() == deepSeekModel)
                    {
                        DeepSeekModelComboBox.SelectedItem = item;
                        break;
                    }
                }

                // 加载MCP配置
                var mcpConfig = _configuration.GetSection("AI:Providers:MCP");
                MCPServerUrlTextBox.Text = mcpConfig["ServerUrl"] ?? "ws://localhost:8080/mcp";
                MCPTimeoutTextBox.Text = mcpConfig["ConnectionTimeoutSeconds"] ?? "30";
                MCPAutoReconnectCheckBox.IsChecked = bool.Parse(mcpConfig["EnableAutoReconnect"] ?? "true");
                MCPCompressionCheckBox.IsChecked = bool.Parse(mcpConfig["EnableCompression"] ?? "true");

                // 设置MCP连接类型
                var mcpConnType = mcpConfig["ConnectionType"] ?? "WebSocket";
                foreach (ComboBoxItem item in MCPConnectionTypeComboBox.Items)
                {
                    if (item.Content?.ToString() == mcpConnType)
                    {
                        MCPConnectionTypeComboBox.SelectedItem = item;
                        break;
                    }
                }

                // 设置MCP认证类型
                var mcpAuthType = mcpConfig["AuthenticationType"] ?? "None";
                foreach (ComboBoxItem item in MCPAuthTypeComboBox.Items)
                {
                    if (item.Content?.ToString() == mcpAuthType)
                    {
                        MCPAuthTypeComboBox.SelectedItem = item;
                        break;
                    }
                }

                UpdateStatus("配置加载完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载配置失败");
                UpdateStatus($"加载配置失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 检查提供者状态
        /// </summary>
        private async Task CheckProvidersStatusAsync()
        {
            // 检查Ollama状态
            try
            {
                var ollamaTest = await _ollamaService.TestConnectionAsync();
                if (ollamaTest.IsSuccess)
                {
                    OllamaStatusIcon.Foreground = Brushes.Green;
                    OllamaStatusText.Text = "在线";
                    
                    var version = await _ollamaService.GetVersionAsync();
                    OllamaStatusText.Text = $"在线 (v{version})";
                }
                else
                {
                    OllamaStatusIcon.Foreground = Brushes.Red;
                    OllamaStatusText.Text = "离线";
                }
            }
            catch
            {
                OllamaStatusIcon.Foreground = Brushes.Red;
                OllamaStatusText.Text = "离线";
            }

            // 检查DeepSeek状态
            var apiKey = DeepSeekApiKeyPasswordBox.Password;
            if (string.IsNullOrEmpty(apiKey))
            {
                DeepSeekStatusIcon.Foreground = Brushes.Gray;
                DeepSeekStatusText.Text = "未配置API密钥";
            }
            else
            {
                DeepSeekStatusIcon.Foreground = Brushes.Orange;
                DeepSeekStatusText.Text = "已配置，未测试";
            }

            // 检查MCP状态
            MCPStatusIcon.Foreground = Brushes.Gray;
            MCPStatusText.Text = "未实现";
        }

        /// <summary>
        /// 加载Ollama模型列表
        /// </summary>
        private async Task LoadOllamaModelsAsync()
        {
            try
            {
                var models = await _ollamaService.GetAvailableModelsAsync();
                
                _models.Clear();
                OllamaModelComboBox.Items.Clear();
                
                foreach (var model in models)
                {
                    var viewModel = new OllamaModelViewModel
                    {
                        Name = model.Id,
                        Size = model.Size,
                        SizeFormatted = FormatBytes(model.Size),
                        ModifiedAt = model.Parameters.TryGetValue("ModifiedAt", out var modifiedAt) && modifiedAt is DateTime dt ? dt : DateTime.MinValue
                    };
                    
                    _models.Add(viewModel);
                    OllamaModelComboBox.Items.Add(new ComboBoxItem { Content = model.Id, Tag = model.Id });
                }

                // 设置默认模型
                var defaultModel = _configuration["AI:Providers:Ollama:DefaultModel"] ?? "qwq:latest";
                foreach (ComboBoxItem item in OllamaModelComboBox.Items)
                {
                    if (item.Tag?.ToString() == defaultModel)
                    {
                        OllamaModelComboBox.SelectedItem = item;
                        break;
                    }
                }

                UpdateStatus($"已加载 {models.Count} 个Ollama模型");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载Ollama模型列表失败");
                UpdateStatus($"加载模型列表失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 格式化字节大小
        /// </summary>
        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        private void UpdateStatus(string message, bool isError = false)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = isError ? Brushes.Red : Brushes.Green;
        }

        /// <summary>
        /// 默认提供者变更
        /// </summary>
        private void DefaultProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DefaultProviderComboBox.SelectedItem is ComboBoxItem item)
            {
                CurrentProviderTextBlock.Text = item.Tag?.ToString() ?? "未知";
            }
        }

        /// <summary>
        /// 刷新模型列表
        /// </summary>
        private async void RefreshModelsButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshModelsButton.IsEnabled = false;
            try
            {
                await LoadOllamaModelsAsync();
            }
            finally
            {
                RefreshModelsButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// 下载模型
        /// </summary>
        private async void PullModelButton_Click(object sender, RoutedEventArgs e)
        {
            var modelName = ModelNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(modelName))
            {
                MessageBox.Show("请输入模型名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PullModelButton.IsEnabled = false;
            ModelDownloadProgressBar.Visibility = Visibility.Visible;
            ModelDownloadStatusText.Visibility = Visibility.Visible;
            ModelDownloadProgressBar.IsIndeterminate = true;

            try
            {
                UpdateStatus($"正在下载模型: {modelName}");
                
                var success = await _ollamaService.PullModelAsync(modelName, progress =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (progress.TotalBytes > 0)
                        {
                            ModelDownloadProgressBar.IsIndeterminate = false;
                            ModelDownloadProgressBar.Value = progress.ProgressPercentage;
                            ModelDownloadStatusText.Text = $"{progress.Status} - {progress.ProgressPercentage:F1}%";
                        }
                        else
                        {
                            ModelDownloadStatusText.Text = progress.Status;
                        }
                    });
                });

                if (success)
                {
                    UpdateStatus($"模型下载成功: {modelName}");
                    await LoadOllamaModelsAsync();
                    ModelNameTextBox.Clear();
                }
                else
                {
                    UpdateStatus($"模型下载失败: {modelName}", true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载模型失败: {ModelName}", modelName);
                UpdateStatus($"下载模型失败: {ex.Message}", true);
            }
            finally
            {
                PullModelButton.IsEnabled = true;
                ModelDownloadProgressBar.Visibility = Visibility.Collapsed;
                ModelDownloadStatusText.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 删除模型
        /// </summary>
        private async void DeleteModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string modelName)
            {
                var result = MessageBox.Show($"确定要删除模型 '{modelName}' 吗？", "确认删除", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var success = await _ollamaService.DeleteModelAsync(modelName);
                        if (success)
                        {
                            UpdateStatus($"模型删除成功: {modelName}");
                            await LoadOllamaModelsAsync();
                        }
                        else
                        {
                            UpdateStatus($"模型删除失败: {modelName}", true);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "删除模型失败: {ModelName}", modelName);
                        UpdateStatus($"删除模型失败: {ex.Message}", true);
                    }
                }
            }
        }

        /// <summary>
        /// 测试连接
        /// </summary>
        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            TestConnectionButton.IsEnabled = false;
            try
            {
                UpdateStatus("正在测试连接...");
                await CheckProvidersStatusAsync();
                UpdateStatus("连接测试完成");
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        private void SaveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 这里应该保存配置到文件
                // 由于配置文件是只读的，这里只是显示消息
                MessageBox.Show("配置保存功能需要重启应用程序后生效。\n请手动修改 appsettings.json 文件。", 
                    "配置保存", MessageBoxButton.OK, MessageBoxImage.Information);
                
                UpdateStatus("配置已准备保存（需要重启应用）");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存配置失败");
                UpdateStatus($"保存配置失败: {ex.Message}", true);
            }
        }
    }

    /// <summary>
    /// Ollama模型视图模型
    /// </summary>
    public class OllamaModelViewModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private long _size;
        private string _sizeFormatted = string.Empty;
        private DateTime _modifiedAt;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public long Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(); }
        }

        public string SizeFormatted
        {
            get => _sizeFormatted;
            set { _sizeFormatted = value; OnPropertyChanged(); }
        }

        public DateTime ModifiedAt
        {
            get => _modifiedAt;
            set { _modifiedAt = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
