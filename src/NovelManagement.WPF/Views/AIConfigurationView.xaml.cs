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
using System.IO;
using System.Text.Json;
using NovelManagement.AI.Services;
using NovelManagement.AI.Services.Ollama;
using NovelManagement.AI.Services.Ollama.Models;
using NovelManagement.AI.Services.RWKV;

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
        private readonly IRwkvLightningService? _rwkvService;
        private readonly ObservableCollection<OllamaModelViewModel> _models;

        /// <summary>
        /// 初始化 AIConfigurationView 的新实例，从应用服务容器获取日志、配置、模型管理器和 Ollama 服务
        /// </summary>
        public AIConfigurationView()
        {
            InitializeComponent();
            
            // 获取服务
            var serviceProvider = App.ServiceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<AIConfigurationView>>();
            _configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _modelManager = serviceProvider.GetRequiredService<ModelManager>();
            _ollamaService = serviceProvider.GetRequiredService<IOllamaApiService>();
            _rwkvService = serviceProvider.GetService<IRwkvLightningService>();
            
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

                // 加载默认续写提供者
                var defaultCompletionProvider = _configuration["AI:DefaultCompletionProvider"];
                foreach (ComboBoxItem item in DefaultCompletionProviderComboBox.Items)
                {
                    if (item.Tag?.ToString() == defaultCompletionProvider)
                    {
                        DefaultCompletionProviderComboBox.SelectedItem = item;
                        break;
                    }
                }

                // 加载RWKV配置
                var rwkvConfig = _configuration.GetSection("AI:Providers:RWKV");
                RwkvBaseUrlTextBox.Text = rwkvConfig["BaseUrl"] ?? "http://localhost:8765";
                RwkvModelNameTextBox.Text = rwkvConfig["ModelName"] ?? "RWKV-7B";
                RwkvModelPathTextBox.Text = rwkvConfig["ModelPath"] ?? "";
                var rwkvStrategy = rwkvConfig["Strategy"] ?? "cuda fp16";
                foreach (ComboBoxItem item in RwkvStrategyComboBox.Items)
                {
                    if (item.Content?.ToString() == rwkvStrategy)
                    {
                        RwkvStrategyComboBox.SelectedItem = item;
                        break;
                    }
                }
                RwkvMaxTokensTextBox.Text = rwkvConfig["MaxTokensPerCompletion"] ?? "200";
                RwkvTemperatureTextBox.Text = rwkvConfig["Temperature"] ?? "1.0";
                RwkvTopPTextBox.Text = rwkvConfig["TopP"] ?? "0.7";
                RwkvTopKTextBox.Text = rwkvConfig["TopK"] ?? "50";
                RwkvFrequencyPenaltyTextBox.Text = rwkvConfig["FrequencyPenalty"] ?? "0.0";
                RwkvPresencePenaltyTextBox.Text = rwkvConfig["PresencePenalty"] ?? "0.0";
                RwkvTimeoutTextBox.Text = rwkvConfig["TimeoutSeconds"] ?? "120";
                RwkvMaxRetriesTextBox.Text = rwkvConfig["MaxRetries"] ?? "3";
                RwkvAutoStartServerCheckBox.IsChecked = bool.Parse(rwkvConfig["AutoStartServer"] ?? "false");
                RwkvGpuDeviceIdTextBox.Text = rwkvConfig["GpuDeviceId"] ?? "-1";
                RwkvPythonPathTextBox.Text = rwkvConfig["PythonPath"] ?? "python";
                RwkvServerScriptPathTextBox.Text = rwkvConfig["ServerScriptPath"] ?? "";

                // 加载智谱AI配置
                var zhipuAIConfig = _configuration.GetSection("AI:Providers:ZhipuAI");
                ZhipuAIBaseUrlTextBox.Text = zhipuAIConfig["BaseUrl"] ?? "https://open.bigmodel.cn/api/paas/v4";
                ZhipuAITimeoutTextBox.Text = zhipuAIConfig["TimeoutSeconds"] ?? "120";
                ZhipuAIMaxRetriesTextBox.Text = zhipuAIConfig["MaxRetries"] ?? "3";
                ZhipuAITemperatureTextBox.Text = zhipuAIConfig["DefaultTemperature"] ?? "0.7";
                ZhipuAIEnableStreamingCheckBox.IsChecked = bool.Parse(zhipuAIConfig["EnableStreaming"] ?? "true");
                var zhipuAIModel = zhipuAIConfig["DefaultModel"] ?? "glm-4-flash";
                foreach (ComboBoxItem item in ZhipuAIModelComboBox.Items)
                {
                    if (item.Content?.ToString() == zhipuAIModel)
                    {
                        ZhipuAIModelComboBox.SelectedItem = item;
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
            // 检查RWKV状态
            try
            {
                if (_rwkvService != null && _rwkvService.IsAvailable)
                {
                    var status = await _rwkvService.GetStatusAsync();
                    if (status.Ready)
                    {
                        RwkvStatusIcon.Foreground = Brushes.Green;
                        RwkvStatusText.Text = $"在线 ({status.Model})";
                    }
                    else
                    {
                        RwkvStatusIcon.Foreground = Brushes.Orange;
                        RwkvStatusText.Text = "服务在线，模型未加载";
                    }
                }
                else if (_rwkvService != null)
                {
                    var testResult = await _rwkvService.TestConnectionAsync();
                    if (testResult)
                    {
                        RwkvStatusIcon.Foreground = Brushes.Orange;
                        RwkvStatusText.Text = "服务在线，未初始化";
                    }
                    else
                    {
                        RwkvStatusIcon.Foreground = Brushes.Red;
                        RwkvStatusText.Text = "离线";
                    }
                }
                else
                {
                    RwkvStatusIcon.Foreground = Brushes.Gray;
                    RwkvStatusText.Text = "服务未注册";
                }
            }
            catch
            {
                RwkvStatusIcon.Foreground = Brushes.Red;
                RwkvStatusText.Text = "离线";
            }

            // 检查智谱AI状态
            var zhipuAIApiKey = ZhipuAIApiKeyPasswordBox.Password;
            if (string.IsNullOrEmpty(zhipuAIApiKey))
            {
                ZhipuAIStatusIcon.Foreground = Brushes.Gray;
                ZhipuAIStatusText.Text = "未配置API密钥";
            }
            else
            {
                ZhipuAIStatusIcon.Foreground = Brushes.Orange;
                ZhipuAIStatusText.Text = "已配置，未测试";
            }

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
        /// 默认续写提供者变更
        /// </summary>
        private void DefaultCompletionProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 续写提供者变更处理
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
                SaveConfigButton.IsEnabled = false;
                UpdateStatus("正在保存配置...");

                // 读取当前 appsettings.json 文件路径
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                // 尝试多个可能的位置
                var configPaths = new[]
                {
                    Path.Combine(appDir, "appsettings.json"),
                    Path.Combine(Directory.GetParent(appDir)?.Parent?.Parent?.Parent?.FullName ?? appDir, "appsettings.json"),
                };

                string? configPath = null;
                foreach (var p in configPaths)
                {
                    if (File.Exists(p))
                    {
                        configPath = p;
                        break;
                    }
                }

                if (configPath == null)
                {
                    // 尝试在项目源码目录中查找
                    var dir = appDir;
                    for (int i = 0; i < 6; i++)
                    {
                        var parentDir = Directory.GetParent(dir);
                        if (parentDir == null) break;
                        dir = parentDir.FullName;
                        var candidate = Path.Combine(dir, "src", "NovelManagement.WPF", "appsettings.json");
                        if (File.Exists(candidate))
                        {
                            configPath = candidate;
                            break;
                        }
                    }
                }

                if (configPath == null)
                {
                    MessageBox.Show("无法找到 appsettings.json 文件，请手动修改配置。", "保存失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    UpdateStatus("保存失败：找不到配置文件", true);
                    return;
                }

                // 读取并解析 JSON
                var json = File.ReadAllText(configPath);
                var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                // 构建新的配置字典
                var configDict = new Dictionary<string, object>();
                foreach (var prop in root.EnumerateObject())
                {
                    configDict[prop.Name] = DeserializeElement(prop.Value);
                }

                // 确保 AI 节点存在
                if (!configDict.ContainsKey("AI"))
                    configDict["AI"] = new Dictionary<string, object>();

                var aiDict = (Dictionary<string, object>)configDict["AI"];

                // 更新默认提供者
                if (DefaultProviderComboBox.SelectedItem is ComboBoxItem defaultItem)
                    aiDict["DefaultProvider"] = defaultItem.Tag?.ToString() ?? "Ollama";

                if (DefaultCompletionProviderComboBox.SelectedItem is ComboBoxItem completionItem)
                    aiDict["DefaultCompletionProvider"] = completionItem.Tag?.ToString() ?? "RWKV";

                // 确保 Providers 节点存在
                if (!aiDict.ContainsKey("Providers"))
                    aiDict["Providers"] = new Dictionary<string, object>();

                var providersDict = (Dictionary<string, object>)aiDict["Providers"];

                // 更新 RWKV 配置
                providersDict["RWKV"] = new Dictionary<string, object>
                {
                    ["BaseUrl"] = RwkvBaseUrlTextBox.Text.Trim(),
                    ["ModelPath"] = RwkvModelPathTextBox.Text.Trim(),
                    ["ModelName"] = RwkvModelNameTextBox.Text.Trim(),
                    ["Strategy"] = (RwkvStrategyComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "cuda fp16",
                    ["MaxTokensPerCompletion"] = int.TryParse(RwkvMaxTokensTextBox.Text, out var maxTokens) ? maxTokens : 200,
                    ["Temperature"] = double.TryParse(RwkvTemperatureTextBox.Text, out var temp) ? temp : 1.0,
                    ["TopP"] = double.TryParse(RwkvTopPTextBox.Text, out var topP) ? topP : 0.7,
                    ["TopK"] = int.TryParse(RwkvTopKTextBox.Text, out var topK) ? topK : 50,
                    ["FrequencyPenalty"] = double.TryParse(RwkvFrequencyPenaltyTextBox.Text, out var fp) ? fp : 0.0,
                    ["PresencePenalty"] = double.TryParse(RwkvPresencePenaltyTextBox.Text, out var pp) ? pp : 0.0,
                    ["TimeoutSeconds"] = int.TryParse(RwkvTimeoutTextBox.Text, out var rTimeout) ? rTimeout : 120,
                    ["MaxRetries"] = int.TryParse(RwkvMaxRetriesTextBox.Text, out var rRetries) ? rRetries : 3,
                    ["AutoStartServer"] = RwkvAutoStartServerCheckBox.IsChecked == true,
                    ["PythonPath"] = RwkvPythonPathTextBox.Text.Trim(),
                    ["ServerScriptPath"] = RwkvServerScriptPathTextBox.Text.Trim(),
                    ["GpuDeviceId"] = int.TryParse(RwkvGpuDeviceIdTextBox.Text, out var gpuId) ? gpuId : -1
                };

                // 更新智谱AI配置
                providersDict["ZhipuAI"] = new Dictionary<string, object>
                {
                    ["ProviderKind"] = "ZhipuAI",
                    ["BaseUrl"] = ZhipuAIBaseUrlTextBox.Text.Trim(),
                    ["ApiKey"] = ZhipuAIApiKeyPasswordBox.Password,
                    ["DefaultModel"] = (ZhipuAIModelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "glm-4-flash",
                    ["TimeoutSeconds"] = int.TryParse(ZhipuAITimeoutTextBox.Text, out var zTimeout) ? zTimeout : 120,
                    ["MaxRetries"] = int.TryParse(ZhipuAIMaxRetriesTextBox.Text, out var zRetries) ? zRetries : 3,
                    ["DefaultTemperature"] = double.TryParse(ZhipuAITemperatureTextBox.Text, out var zTemp) ? zTemp : 0.7,
                    ["DefaultMaxTokens"] = 4000,
                    ["EnableStreaming"] = ZhipuAIEnableStreamingCheckBox.IsChecked == true
                };

                // 更新 Ollama 配置
                if (providersDict.ContainsKey("Ollama") && providersDict["Ollama"] is Dictionary<string, object> ollamaDict)
                {
                    ollamaDict["BaseUrl"] = OllamaBaseUrlTextBox.Text.Trim();
                    ollamaDict["TimeoutSeconds"] = int.TryParse(OllamaTimeoutTextBox.Text, out var oTimeout) ? oTimeout : 120;
                    ollamaDict["MaxRetries"] = int.TryParse(OllamaMaxRetriesTextBox.Text, out var oRetries) ? oRetries : 3;
                    ollamaDict["EnableGPU"] = OllamaEnableGPUCheckBox.IsChecked == true;
                    ollamaDict["GPUDeviceId"] = int.TryParse(OllamaGPUDeviceIdTextBox.Text, out var oGpuId) ? oGpuId : -1;
                    ollamaDict["EnableVerboseLogging"] = OllamaVerboseLoggingCheckBox.IsChecked == true;
                    if (OllamaModelComboBox.SelectedItem is ComboBoxItem ollamaModelItem)
                        ollamaDict["DefaultModel"] = ollamaModelItem.Tag?.ToString() ?? "qwq:latest";
                }

                // 更新 DeepSeek 配置
                if (providersDict.ContainsKey("DeepSeek") && providersDict["DeepSeek"] is Dictionary<string, object> deepSeekDict)
                {
                    deepSeekDict["BaseUrl"] = DeepSeekBaseUrlTextBox.Text.Trim();
                    deepSeekDict["TimeoutSeconds"] = int.TryParse(DeepSeekTimeoutTextBox.Text, out var dTimeout) ? dTimeout : 60;
                    deepSeekDict["EnableThinkingChain"] = DeepSeekEnableThinkingChainCheckBox.IsChecked == true;
                    if (DeepSeekModelComboBox.SelectedItem is ComboBoxItem deepSeekModelItem)
                        deepSeekDict["Model"] = deepSeekModelItem.Content?.ToString() ?? "deepseek-chat";
                }

                // 序列化并写入
                var options = new JsonSerializerOptions { WriteIndented = true };
                var newJson = JsonSerializer.Serialize(configDict, options);
                File.WriteAllText(configPath, newJson);

                UpdateStatus("配置保存成功（重启应用后生效）");
                MessageBox.Show("配置已保存到 appsettings.json。\n部分配置需要重启应用程序后生效。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存配置失败");
                UpdateStatus($"保存配置失败: {ex.Message}", true);
                MessageBox.Show($"保存配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SaveConfigButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// 递归反序列化 JsonElement 为对象
        /// </summary>
        private static object DeserializeElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => DeserializeElement(p.Value)),
                JsonValueKind.Array => element.EnumerateArray().Select(DeserializeElement).ToList(),
                JsonValueKind.String => element.GetString() ?? "",
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => "",
                _ => element.ToString()
            };
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

        /// <summary>
        /// 模型名称
        /// </summary>
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 模型文件大小（字节）
        /// </summary>
        public long Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 格式化后的模型大小显示文本
        /// </summary>
        public string SizeFormatted
        {
            get => _sizeFormatted;
            set { _sizeFormatted = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 模型最后修改时间
        /// </summary>
        public DateTime ModifiedAt
        {
            get => _modifiedAt;
            set { _modifiedAt = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 当属性值更改时发生
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发 PropertyChanged 事件，通知绑定客户端给定属性已更改
        /// </summary>
        /// <param name="propertyName">已更改属性的名称，由编译器自动推断</param>
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
