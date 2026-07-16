using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using System.Text.Json;
using NovelManagement.AI.Services;
using NovelManagement.AI.Services.Ollama;
using NovelManagement.AI.Services.Ollama.Models;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// AI配置管理界面
    /// </summary>
    public partial class AIConfigurationView : UserControl
    {
        private readonly ILogger<AIConfigurationView> _logger;
        private readonly IConfiguration _configuration;
        private readonly ConfigurationService _configurationService;
        private readonly IOllamaApiService _ollamaService;
        private readonly InferenceRuntimeCoordinator _runtimeCoordinator;
        private readonly ModelManager _modelManager;
        private readonly ObservableCollection<OllamaModelViewModel> _models;
        private readonly List<LlamaModelOption> _llamaModels;
        private readonly List<string> _rwkvModels;
        private bool _isViewInitialized;

        /// <summary>
        /// 初始化 AIConfigurationView 的新实例，从应用服务容器获取日志、配置、模型管理器和 Ollama 服务
        /// </summary>
        public AIConfigurationView()
        {
            InitializeComponent();
            _isViewInitialized = false;
            
            // 获取服务
            var serviceProvider = App.ServiceProvider
                ?? throw new InvalidOperationException("应用服务尚未初始化，暂时无法打开 AI 模型配置。");
            _logger = serviceProvider.GetRequiredService<ILogger<AIConfigurationView>>();
            _configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _configurationService = serviceProvider.GetRequiredService<ConfigurationService>();
            _ollamaService = serviceProvider.GetRequiredService<IOllamaApiService>();
            _runtimeCoordinator = serviceProvider.GetRequiredService<InferenceRuntimeCoordinator>();
            _modelManager = serviceProvider.GetRequiredService<ModelManager>();
            
            _models = new ObservableCollection<OllamaModelViewModel>();
            _llamaModels = new List<LlamaModelOption>();
            _rwkvModels = new List<string>();

            if (ModelsDataGrid == null)
            {
                throw new InvalidOperationException("AI 模型配置界面初始化失败：模型列表控件未正确加载。");
            }

            ModelsDataGrid.ItemsSource = _models;
            
            _isViewInitialized = true;
            Loaded += AIConfigurationView_Loaded;
        }

        private async void AIConfigurationView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadConfigurationAsync();
                await LoadOllamaModelsAsync();
                await LoadLlamaModelsAsync();
                await LoadRwkvModelsAsync();
                await CheckProvidersStatusAsync();
                ConfigPathTextBlock.Text = GetUserConfigurationPath();

                #region debug-point A:ai-config-loaded
                await ReportDebugEventAsync(
                    "A",
                    "AIConfigurationView_Loaded",
                    "AI配置页加载完成",
                    new Dictionary<string, object?>
                    {
                        ["configPath"] = ConfigPathTextBlock.Text,
                        ["mainAgentProvider"] = (MainAgentProviderComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(),
                        ["subAgentProvider"] = (SubAgentProviderComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(),
                        ["mainAgentModel"] = MainAgentModelTextBox.Text,
                        ["subAgentModel"] = SubAgentModelTextBox.Text,
                        ["ollamaEnableGpuRow"] = Grid.GetRow(OllamaEnableGPUCheckBox),
                        ["ollamaVerboseRow"] = Grid.GetRow(OllamaVerboseLoggingCheckBox),
                        ["ollamaEnableGpuChecked"] = OllamaEnableGPUCheckBox.IsChecked,
                        ["ollamaVerboseChecked"] = OllamaVerboseLoggingCheckBox.IsChecked,
                        ["ollamaStatusText"] = OllamaStatusText.Text,
                        ["ollamaModelCount"] = OllamaModelComboBox.Items.Count
                    });
                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载AI配置界面失败");
                UpdateStatus($"加载失败: {ex.Message}", true);

                #region debug-point E:ai-config-load-failed
                await ReportDebugEventAsync(
                    "E",
                    "AIConfigurationView_Loaded",
                    $"AI配置页加载失败: {ex.Message}",
                    new Dictionary<string, object?>
                    {
                        ["exceptionType"] = ex.GetType().FullName,
                        ["stack"] = ex.ToString()
                    });
                #endregion
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

                var agentRoleConfig = _configuration.GetSection("AI:AgentRoles");
                EnableDualAgentWorkflowCheckBox.IsChecked = bool.Parse(agentRoleConfig["EnableDualAgentWorkflow"] ?? "true");
                EnableArchiveWriteCheckBox.IsChecked = bool.Parse(agentRoleConfig["EnableArchiveWrite"] ?? "true");
                MainAgentModelTextBox.Text = agentRoleConfig["MainAgentModel"] ?? "";
                SubAgentModelTextBox.Text = agentRoleConfig["SubAgentModel"] ?? "";
                MainAgentRoleDescriptionTextBox.Text = agentRoleConfig["MainAgentRoleDescription"]
                    ?? "结合 SubAgent 的需求简报撰写正式文案，专注内容创作。";
                SubAgentRoleDescriptionTextBox.Text = agentRoleConfig["SubAgentRoleDescription"]
                    ?? "总结写作需求，整理 MainAgent 草稿，并将纯净内容写入项目档案库。";
                SelectComboBoxItemByTag(MainAgentProviderComboBox, agentRoleConfig["MainAgentProvider"] ?? "DeepSeek");
                SelectComboBoxItemByTag(SubAgentProviderComboBox, agentRoleConfig["SubAgentProvider"] ?? "LlamaCpp");

                // 加载RWKV配置
                var rwkvConfig = _configuration.GetSection("AI:Providers:RWKV");
                RwkvBaseUrlTextBox.Text = rwkvConfig["BaseUrl"] ?? "http://localhost:8000";
                RwkvModelNameTextBox.Text = rwkvConfig["ModelName"] ?? "rwkv7";
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
                RwkvContextSizeTextBox.Text = rwkvConfig["ContextSize"] ?? "8192";
                RwkvMaxConcurrentRequestsTextBox.Text = rwkvConfig["MaxConcurrentRequests"] ?? "20";
                ApplyRwkvDefaultsIfNeeded();

                // 加载 llama.cpp 配置
                var llamaConfig = _configuration.GetSection("AI:Providers:LlamaCpp");
                LlamaBaseUrlTextBox.Text = llamaConfig["BaseUrl"] ?? "http://localhost:8081";
                LlamaExecutablePathTextBox.Text = llamaConfig["ExecutablePath"] ?? "";
                LlamaModelDirectoryTextBox.Text = llamaConfig["ModelDirectory"] ?? "";
                LlamaMmprojPathTextBox.Text = llamaConfig["MmprojPath"] ?? "";
                LlamaContextSizeTextBox.Text = llamaConfig["ContextSize"] ?? "262144";
                LlamaGpuLayersTextBox.Text = llamaConfig["GpuLayers"] ?? "-1";
                LlamaApiKeyPasswordBox.Password = llamaConfig["ApiKey"] ?? "";
                LlamaParallelSlotsTextBox.Text = llamaConfig["ParallelSlots"] ?? "1";
                LlamaEnableFlashAttentionCheckBox.IsChecked = bool.Parse(llamaConfig["EnableFlashAttention"] ?? "true");
                LlamaEnableCpuMoeCheckBox.IsChecked = bool.Parse(llamaConfig["EnableCpuMoE"] ?? "true");
                LlamaCpuMoeLayerCountTextBox.Text = llamaConfig["CpuMoELayerCount"] ?? "0";
                LlamaEnableAutoFitCheckBox.IsChecked = bool.Parse(llamaConfig["EnableAutoFit"] ?? "true");
                LlamaFitContextSizeTextBox.Text = llamaConfig["FitContextSize"] ?? "262144";
                LlamaDisableWarmupCheckBox.IsChecked = bool.Parse(llamaConfig["DisableWarmup"] ?? "true");

                SelectComboBoxItemByContent(LlamaCacheTypeKComboBox, llamaConfig["CacheTypeK"] ?? "q8_0");
                SelectComboBoxItemByContent(LlamaCacheTypeVComboBox, llamaConfig["CacheTypeV"] ?? "q8_0");

                var backend = llamaConfig["Backend"] ?? "cuda";
                foreach (ComboBoxItem item in LlamaBackendComboBox.Items)
                {
                    if (string.Equals(item.Tag?.ToString(), backend, StringComparison.OrdinalIgnoreCase))
                    {
                        LlamaBackendComboBox.SelectedItem = item;
                        break;
                    }
                }

                ApplyLlamaDefaultsIfNeeded();

                // 加载智谱AI配置
                var zhipuAIConfig = _configuration.GetSection("AI:Providers:ZhipuAI");
                ZhipuAIApiKeyPasswordBox.Password = zhipuAIConfig["ApiKey"] ?? "";
                ZhipuAIBaseUrlTextBox.Text = zhipuAIConfig["BaseUrl"] ?? "https://open.bigmodel.cn/api/paas/v4";
                ZhipuAITimeoutTextBox.Text = zhipuAIConfig["TimeoutSeconds"] ?? "120";
                ZhipuAIMaxRetriesTextBox.Text = zhipuAIConfig["MaxRetries"] ?? "3";
                ZhipuAITemperatureTextBox.Text = zhipuAIConfig["DefaultTemperature"] ?? "0.7";
                ZhipuAIEnableStreamingCheckBox.IsChecked = bool.Parse(zhipuAIConfig["EnableStreaming"] ?? "true");
                var zhipuAIModel = zhipuAIConfig["DefaultModel"] ?? "glm-4.7-flash";
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
                OllamaContextSizeTextBox.Text = ollamaConfig["ContextSize"] ?? "32768";
                OllamaMaxConcurrentRequestsTextBox.Text = ollamaConfig["MaxConcurrentRequests"] ?? "3";
                OllamaEnableGPUCheckBox.IsChecked = bool.Parse(ollamaConfig["EnableGPU"] ?? "true");
                OllamaGPUDeviceIdTextBox.Text = ollamaConfig["GPUDeviceId"] ?? "-1";
                OllamaVerboseLoggingCheckBox.IsChecked = bool.Parse(ollamaConfig["EnableVerboseLogging"] ?? "false");

                // 加载DeepSeek配置
                var deepSeekConfig = _configuration.GetSection("AI:Providers:DeepSeek");
                DeepSeekApiKeyPasswordBox.Password = deepSeekConfig["ApiKey"] ?? "";
                DeepSeekBaseUrlTextBox.Text = deepSeekConfig["BaseUrl"] ?? "https://api.deepseek.com";
                DeepSeekTimeoutTextBox.Text = deepSeekConfig["TimeoutSeconds"] ?? "60";
                DeepSeekEnableThinkingChainCheckBox.IsChecked = bool.Parse(deepSeekConfig["EnableThinkingChain"] ?? "true");
                
                // 设置DeepSeek模型
                var deepSeekModel = deepSeekConfig["DefaultModel"]
                    ?? deepSeekConfig["Model"]
                    ?? "deepseek-v4-flash";
                foreach (ComboBoxItem item in DeepSeekModelComboBox.Items)
                {
                    if (item.Content?.ToString() == deepSeekModel)
                    {
                        DeepSeekModelComboBox.SelectedItem = item;
                        break;
                    }
                }

                // 加载小米米模配置
                var xiaoMiMiMoConfig = _configuration.GetSection("AI:Providers:XiaoMiMiMo");
                XiaoMiMiMoApiKeyPasswordBox.Password = xiaoMiMiMoConfig["ApiKey"] ?? "";
                XiaoMiMiMoBaseUrlTextBox.Text = xiaoMiMiMoConfig["BaseUrl"] ?? "https://api.xiaomimimo.com/v1";
                XiaoMiMiMoTimeoutTextBox.Text = xiaoMiMiMoConfig["TimeoutSeconds"] ?? "120";
                XiaoMiMiMoMaxRetriesTextBox.Text = xiaoMiMiMoConfig["MaxRetries"] ?? "3";
                XiaoMiMiMoTemperatureTextBox.Text = xiaoMiMiMoConfig["DefaultTemperature"] ?? "1.0";
                XiaoMiMiMoEnableStreamingCheckBox.IsChecked = bool.Parse(xiaoMiMiMoConfig["EnableStreaming"] ?? "true");
                var xiaoMiMiMoModel = xiaoMiMiMoConfig["DefaultModel"] ?? "mimo-v2.5-pro";
                foreach (ComboBoxItem item in XiaoMiMiMoModelComboBox.Items)
                {
                    if (item.Content?.ToString() == xiaoMiMiMoModel)
                    {
                        XiaoMiMiMoModelComboBox.SelectedItem = item;
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

                #region debug-point E:load-config-failed
                await ReportDebugEventAsync(
                    "E",
                    "LoadConfigurationAsync",
                    $"加载配置失败: {ex.Message}",
                    new Dictionary<string, object?>
                    {
                        ["exceptionType"] = ex.GetType().FullName,
                        ["stack"] = ex.ToString()
                    });
                #endregion
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
                var status = await _runtimeCoordinator.GetRwkvStatusAsync(BuildRwkvRuntimeOptions());
                ApplyRuntimeStatus(RwkvStatusIcon, RwkvStatusText, status);
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

            // 检查 llama.cpp 状态
            try
            {
                var status = await _runtimeCoordinator.GetLlamaStatusAsync(BuildLlamaRuntimeOptions());
                ApplyRuntimeStatus(LlamaStatusIcon, LlamaStatusText, status);
            }
            catch
            {
                LlamaStatusIcon.Foreground = Brushes.Red;
                LlamaStatusText.Text = "离线";
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

            // 检查小米米模状态
            var xiaoMiMiMoApiKey = XiaoMiMiMoApiKeyPasswordBox.Password;
            if (string.IsNullOrEmpty(xiaoMiMiMoApiKey))
            {
                XiaoMiMiMoStatusIcon.Foreground = Brushes.Gray;
                XiaoMiMiMoStatusText.Text = "未配置API密钥";
            }
            else
            {
                XiaoMiMiMoStatusIcon.Foreground = Brushes.Orange;
                XiaoMiMiMoStatusText.Text = "已配置，未测试";
            }

            // 检查MCP状态
            MCPStatusIcon.Foreground = Brushes.Red;
            MCPStatusText.Text = "服务未接入";

            #region debug-point B:provider-status
            await ReportDebugEventAsync(
                "B",
                "CheckProvidersStatusAsync",
                "AI提供者状态检查完成",
                new Dictionary<string, object?>
                {
                    ["ollamaStatus"] = OllamaStatusText.Text,
                    ["rwkvStatus"] = RwkvStatusText.Text,
                    ["llamaStatus"] = LlamaStatusText.Text,
                    ["zhipuStatus"] = ZhipuAIStatusText.Text,
                    ["deepSeekStatus"] = DeepSeekStatusText.Text
                });
            #endregion
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

                #region debug-point B:ollama-model-load-failed
                await ReportDebugEventAsync(
                    "B",
                    "LoadOllamaModelsAsync",
                    $"加载Ollama模型列表失败: {ex.Message}",
                    new Dictionary<string, object?>
                    {
                        ["baseUrl"] = OllamaBaseUrlTextBox.Text,
                        ["exceptionType"] = ex.GetType().FullName,
                        ["stack"] = ex.ToString()
                    });
                #endregion
            }
        }

        private async Task LoadLlamaModelsAsync()
        {
            try
            {
                ApplyLlamaDefaultsIfNeeded();
                var configuredModelPath = _configuration["AI:Providers:LlamaCpp:ModelPath"] ?? "";
                var models = await _runtimeCoordinator.DiscoverLlamaModelsAsync(LlamaModelDirectoryTextBox.Text.Trim());

                _llamaModels.Clear();
                _llamaModels.AddRange(models);

                LlamaModelComboBox.Items.Clear();
                foreach (var model in _llamaModels)
                {
                    LlamaModelComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = model.DisplayName,
                        Tag = model.ModelPath
                    });
                }

                if (_llamaModels.Count == 0)
                {
                    LlamaMmprojPathTextBox.Text = "";
                    UpdateStatus("未在 GGUF 目录中发现可用模型", true);
                    return;
                }

                var selectedPath = string.IsNullOrWhiteSpace(configuredModelPath)
                    ? _llamaModels[0].ModelPath
                    : configuredModelPath;
                SelectLlamaModel(selectedPath);
                UpdateStatus($"已扫描 {_llamaModels.Count} 个 GGUF 模型");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "扫描 llama.cpp 模型目录失败");
                UpdateStatus($"扫描 GGUF 模型失败: {ex.Message}", true);
            }
        }

        private void SelectLlamaModel(string modelPath)
        {
            foreach (ComboBoxItem item in LlamaModelComboBox.Items)
            {
                if (string.Equals(item.Tag?.ToString(), modelPath, StringComparison.OrdinalIgnoreCase))
                {
                    LlamaModelComboBox.SelectedItem = item;
                    break;
                }
            }

            var matched = _llamaModels.FirstOrDefault(model =>
                string.Equals(model.ModelPath, modelPath, StringComparison.OrdinalIgnoreCase));
            if (matched != null)
            {
                LlamaMmprojPathTextBox.Text = matched.MmprojPath;
            }
        }

        private async Task LoadRwkvModelsAsync()
        {
            try
            {
                var modelsDirectory = Path.Combine(ResolveProjectRoot(), "rwkv_models");
                var configuredModelPath = _configuration["AI:Providers:RWKV:ModelPath"] ?? "";

                _rwkvModels.Clear();
                RwkvModelComboBox.Items.Clear();

                if (Directory.Exists(modelsDirectory))
                {
                    var modelFiles = Directory.EnumerateFiles(modelsDirectory, "*.st", SearchOption.TopDirectoryOnly)
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    foreach (var modelFile in modelFiles)
                    {
                        var fileName = Path.GetFileName(modelFile);
                        _rwkvModels.Add(modelFile);
                        RwkvModelComboBox.Items.Add(new ComboBoxItem
                        {
                            Content = fileName,
                            Tag = modelFile
                        });
                    }
                }

                if (_rwkvModels.Count == 0)
                {
                    UpdateStatus("未在 rwkv_models 目录中发现 RWKV 模型", true);
                    return;
                }

                var selectedPath = string.IsNullOrWhiteSpace(configuredModelPath)
                    ? _rwkvModels[0]
                    : configuredModelPath;

                SelectRwkvModel(selectedPath);
                UpdateStatus($"已扫描 {_rwkvModels.Count} 个 RWKV 模型");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "扫描 RWKV 模型目录失败");
                UpdateStatus($"扫描 RWKV 模型失败: {ex.Message}", true);
            }
        }

        private void SelectRwkvModel(string modelPath)
        {
            foreach (ComboBoxItem item in RwkvModelComboBox.Items)
            {
                if (string.Equals(item.Tag?.ToString(), modelPath, StringComparison.OrdinalIgnoreCase))
                {
                    RwkvModelComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void RwkvModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isViewInitialized)
            {
                return;
            }

            if (RwkvModelComboBox.SelectedItem is ComboBoxItem item)
            {
                var modelPath = item.Tag?.ToString() ?? "";
                var modelName = Path.GetFileNameWithoutExtension(modelPath);
                if (!string.IsNullOrWhiteSpace(modelName))
                {
                    RwkvModelNameTextBox.Text = modelName;
                }
            }
        }

        private async void RwkvRefreshModelsButton_Click(object sender, RoutedEventArgs e)
        {
            RwkvRefreshModelsButton.IsEnabled = false;
            try
            {
                await LoadRwkvModelsAsync();
            }
            finally
            {
                RwkvRefreshModelsButton.IsEnabled = true;
            }
        }

        private void ApplyRuntimeStatus(PackIcon statusIcon, TextBlock statusText, RuntimeStatusSnapshot snapshot)
        {
            if (snapshot.IsEndpointReachable)
            {
                statusIcon.Foreground = Brushes.Green;
            }
            else if (snapshot.IsProcessRunning)
            {
                statusIcon.Foreground = Brushes.Orange;
            }
            else
            {
                statusIcon.Foreground = Brushes.Red;
            }

            statusText.Text = snapshot.StatusText;
        }

        private void ApplyRwkvDefaultsIfNeeded()
        {
            if (string.IsNullOrWhiteSpace(RwkvServerScriptPathTextBox.Text))
            {
                var defaultExecutable = Path.Combine(ResolveProjectRoot(), "rwkv_lightning_libtorch_win", "rwkv_lightning.exe");
                if (File.Exists(defaultExecutable))
                {
                    RwkvServerScriptPathTextBox.Text = defaultExecutable;
                }
            }

        }

        private void ApplyLlamaDefaultsIfNeeded()
        {
            if (LlamaBackendComboBox == null
                || LlamaExecutablePathTextBox == null
                || LlamaModelDirectoryTextBox == null
                || LlamaContextSizeTextBox == null
                || LlamaParallelSlotsTextBox == null
                || LlamaFitContextSizeTextBox == null)
            {
                return;
            }

            var defaultExecutable = GetDefaultLlamaExecutablePath(GetSelectedLlamaBackend());
            if (string.IsNullOrWhiteSpace(LlamaExecutablePathTextBox.Text) || File.Exists(defaultExecutable))
            {
                if (string.IsNullOrWhiteSpace(LlamaExecutablePathTextBox.Text) || 
                    LlamaExecutablePathTextBox.Text.Contains("\\llama_cpp\\", StringComparison.OrdinalIgnoreCase))
                {
                    LlamaExecutablePathTextBox.Text = defaultExecutable;
                }
            }

            if (string.IsNullOrWhiteSpace(LlamaModelDirectoryTextBox.Text))
            {
                var defaultModelDirectory = Path.Combine(ResolveProjectRoot(), "gguf_models");
                if (Directory.Exists(defaultModelDirectory))
                {
                    LlamaModelDirectoryTextBox.Text = defaultModelDirectory;
                }
            }

            if (string.IsNullOrWhiteSpace(LlamaContextSizeTextBox.Text))
            {
                LlamaContextSizeTextBox.Text = "262144";
            }

            if (string.IsNullOrWhiteSpace(LlamaParallelSlotsTextBox.Text))
            {
                LlamaParallelSlotsTextBox.Text = "1";
            }

            if (string.IsNullOrWhiteSpace(LlamaFitContextSizeTextBox.Text))
            {
                LlamaFitContextSizeTextBox.Text = "262144";
            }
        }

        private string ResolveProjectRoot()
        {
            var current = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 8; i++)
            {
                if (Directory.Exists(Path.Combine(current, "rwkv_lightning_libtorch_win")) ||
                    Directory.Exists(Path.Combine(current, "llama_cpp")))
                {
                    return current;
                }

                var parent = Directory.GetParent(current);
                if (parent == null)
                {
                    break;
                }

                current = parent.FullName;
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private string GetSelectedLlamaBackend()
        {
            return (LlamaBackendComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString()?.ToLowerInvariant() ?? "cuda";
        }

        private string GetDefaultLlamaExecutablePath(string backend)
        {
            return Path.Combine(ResolveProjectRoot(), "llama_cpp", backend, "llama-server.exe");
        }

        private RwkvRuntimeLaunchOptions BuildRwkvRuntimeOptions()
        {
            return new RwkvRuntimeLaunchOptions
            {
                ExecutablePath = RwkvServerScriptPathTextBox.Text.Trim(),
                BaseUrl = RwkvBaseUrlTextBox.Text.Trim(),
                ModelPath = (RwkvModelComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "",
                ModelName = string.IsNullOrWhiteSpace(RwkvModelNameTextBox.Text) ? "rwkv7" : RwkvModelNameTextBox.Text.Trim(),
                Strategy = (RwkvStrategyComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "cuda fp16",
                VocabPath = _configuration["AI:Providers:RWKV:VocabPath"] ?? "",
                Password = _configuration["AI:Providers:RWKV:Password"] ?? "",
                ContextSize = int.TryParse(RwkvContextSizeTextBox.Text, out var rwkvContextSize) ? Math.Max(1024, rwkvContextSize) : 8192,
                MaxConcurrentRequests = int.TryParse(RwkvMaxConcurrentRequestsTextBox.Text, out var rwkvMaxConcurrentRequests) ? Math.Max(1, rwkvMaxConcurrentRequests) : 20,
                StartupTimeoutSeconds = int.TryParse(RwkvTimeoutTextBox.Text, out var timeout) ? Math.Max(15, timeout) : 45
            };
        }

        private LlamaRuntimeLaunchOptions BuildLlamaRuntimeOptions()
        {
            var selectedModelPath = (LlamaModelComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString()
                ?? _configuration["AI:Providers:LlamaCpp:ModelPath"]
                ?? "";

            return new LlamaRuntimeLaunchOptions
            {
                ExecutablePath = LlamaExecutablePathTextBox.Text.Trim(),
                BaseUrl = LlamaBaseUrlTextBox.Text.Trim(),
                ModelPath = selectedModelPath,
                MmprojPath = LlamaMmprojPathTextBox.Text.Trim(),
                ApiKey = LlamaApiKeyPasswordBox.Password,
                ContextSize = int.TryParse(LlamaContextSizeTextBox.Text, out var contextSize) ? contextSize : 262144,
                GpuLayers = int.TryParse(LlamaGpuLayersTextBox.Text, out var gpuLayers) ? gpuLayers : -1,
                ParallelSlots = int.TryParse(LlamaParallelSlotsTextBox.Text, out var parallelSlots) ? Math.Max(1, parallelSlots) : 1,
                EnableFlashAttention = LlamaEnableFlashAttentionCheckBox.IsChecked == true,
                CacheTypeK = (LlamaCacheTypeKComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "q8_0",
                CacheTypeV = (LlamaCacheTypeVComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "q8_0",
                EnableCpuMoE = LlamaEnableCpuMoeCheckBox.IsChecked == true,
                CpuMoELayerCount = int.TryParse(LlamaCpuMoeLayerCountTextBox.Text, out var cpuMoeLayerCount) ? Math.Max(0, cpuMoeLayerCount) : 0,
                EnableAutoFit = LlamaEnableAutoFitCheckBox.IsChecked == true,
                FitContextSize = int.TryParse(LlamaFitContextSizeTextBox.Text, out var fitContextSize) ? Math.Max(4096, fitContextSize) : 262144,
                DisableWarmup = LlamaDisableWarmupCheckBox.IsChecked == true,
                StartupTimeoutSeconds = 45
            };
        }

        private async void RwkvRefreshStatusButton_Click(object sender, RoutedEventArgs e)
        {
            await CheckProvidersStatusAsync();
            UpdateStatus("RWKV 状态已刷新");
        }

        private async void RwkvStartRuntimeButton_Click(object sender, RoutedEventArgs e)
        {
            RwkvStartRuntimeButton.IsEnabled = false;
            try
            {
                UpdateStatus("正在启动 RWKV 推理服务...");
                var result = await _runtimeCoordinator.StartRwkvAsync(BuildRwkvRuntimeOptions());
                UpdateStatus(result.Message, !result.Success);
                await CheckProvidersStatusAsync();
            }
            finally
            {
                RwkvStartRuntimeButton.IsEnabled = true;
            }
        }

        private async void RwkvStopRuntimeButton_Click(object sender, RoutedEventArgs e)
        {
            RwkvStopRuntimeButton.IsEnabled = false;
            try
            {
                UpdateStatus("正在停止 RWKV 推理服务...");
                var result = await _runtimeCoordinator.StopRwkvAsync(BuildRwkvRuntimeOptions());
                UpdateStatus(result.Message, !result.Success);
                await CheckProvidersStatusAsync();
            }
            finally
            {
                RwkvStopRuntimeButton.IsEnabled = true;
            }
        }

        private void LlamaBackendComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isViewInitialized)
            {
                return;
            }

            ApplyLlamaDefaultsIfNeeded();
        }

        private void LlamaModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LlamaModelComboBox.SelectedItem is not ComboBoxItem item)
            {
                return;
            }

            var selected = _llamaModels.FirstOrDefault(model =>
                string.Equals(model.ModelPath, item.Tag?.ToString(), StringComparison.OrdinalIgnoreCase));
            if (selected != null)
            {
                LlamaMmprojPathTextBox.Text = selected.MmprojPath;
            }
        }

        private async void LlamaRefreshModelsButton_Click(object sender, RoutedEventArgs e)
        {
            LlamaRefreshModelsButton.IsEnabled = false;
            try
            {
                await LoadLlamaModelsAsync();
            }
            finally
            {
                LlamaRefreshModelsButton.IsEnabled = true;
            }
        }

        private async void LlamaRefreshStatusButton_Click(object sender, RoutedEventArgs e)
        {
            await CheckProvidersStatusAsync();
            UpdateStatus("llama.cpp 状态已刷新");
        }

        private async void StartLlamaRuntimeButton_Click(object sender, RoutedEventArgs e)
        {
            StartLlamaRuntimeButton.IsEnabled = false;
            try
            {
                UpdateStatus("正在启动 llama.cpp 推理服务...");
                var result = await _runtimeCoordinator.StartLlamaAsync(BuildLlamaRuntimeOptions());
                UpdateStatus(result.Message, !result.Success);
                await CheckProvidersStatusAsync();
            }
            finally
            {
                StartLlamaRuntimeButton.IsEnabled = true;
            }
        }

        private async void StopLlamaRuntimeButton_Click(object sender, RoutedEventArgs e)
        {
            StopLlamaRuntimeButton.IsEnabled = false;
            try
            {
                UpdateStatus("正在停止 llama.cpp 推理服务...");
                var result = await _runtimeCoordinator.StopLlamaAsync(BuildLlamaRuntimeOptions());
                UpdateStatus(result.Message, !result.Success);
                await CheckProvidersStatusAsync();
            }
            finally
            {
                StopLlamaRuntimeButton.IsEnabled = true;
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
        private async void SaveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveConfigButton.IsEnabled = false;
                UpdateStatus("正在保存配置...");

                var configPath = GetUserConfigurationPath();
                Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);

                var configDict = File.Exists(configPath)
                    ? LoadJsonAsDictionary(configPath)
                    : new Dictionary<string, object>();

                if (!configDict.ContainsKey("AI") || configDict["AI"] is not Dictionary<string, object> aiDict)
                {
                    aiDict = new Dictionary<string, object>();
                    configDict["AI"] = aiDict;
                }

                // 更新默认提供者
                if (DefaultProviderComboBox.SelectedItem is ComboBoxItem defaultItem)
                    aiDict["DefaultProvider"] = defaultItem.Tag?.ToString() ?? "Ollama";

                if (DefaultCompletionProviderComboBox.SelectedItem is ComboBoxItem completionItem)
                    aiDict["DefaultCompletionProvider"] = completionItem.Tag?.ToString() ?? "RWKV";

                if (!aiDict.ContainsKey("Providers") || aiDict["Providers"] is not Dictionary<string, object> providersDict)
                {
                    providersDict = new Dictionary<string, object>();
                    aiDict["Providers"] = providersDict;
                }

                // 更新 RWKV 配置
                providersDict["RWKV"] = new Dictionary<string, object>
                {
                    ["BaseUrl"] = RwkvBaseUrlTextBox.Text.Trim(),
                    ["ModelPath"] = (RwkvModelComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "",
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
                    ["ContextSize"] = int.TryParse(RwkvContextSizeTextBox.Text, out var rwkvContextSize) ? Math.Clamp(rwkvContextSize, 1024, 32768) : 8192,
                    ["MaxConcurrentRequests"] = int.TryParse(RwkvMaxConcurrentRequestsTextBox.Text, out var rwkvMaxConcurrentRequests) ? Math.Clamp(rwkvMaxConcurrentRequests, 1, 20) : 20,
                    ["AutoStartServer"] = RwkvAutoStartServerCheckBox.IsChecked == true,
                    ["PythonPath"] = RwkvPythonPathTextBox.Text.Trim(),
                    ["ServerScriptPath"] = RwkvServerScriptPathTextBox.Text.Trim(),
                    ["GpuDeviceId"] = int.TryParse(RwkvGpuDeviceIdTextBox.Text, out var gpuId) ? gpuId : -1
                };

                providersDict["LlamaCpp"] = new Dictionary<string, object>
                {
                    ["ProviderKind"] = "LlamaCpp",
                    ["BaseUrl"] = LlamaBaseUrlTextBox.Text.Trim(),
                    ["ExecutablePath"] = LlamaExecutablePathTextBox.Text.Trim(),
                    ["Backend"] = GetSelectedLlamaBackend(),
                    ["ModelDirectory"] = LlamaModelDirectoryTextBox.Text.Trim(),
                    ["ModelPath"] = (LlamaModelComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "",
                    ["DefaultModel"] = GetSelectedLlamaDefaultModel(),
                    ["MmprojPath"] = LlamaMmprojPathTextBox.Text.Trim(),
                    ["ApiKey"] = LlamaApiKeyPasswordBox.Password,
                    ["ContextSize"] = int.TryParse(LlamaContextSizeTextBox.Text, out var llamaContextSize) ? llamaContextSize : 262144,
                    ["GpuLayers"] = int.TryParse(LlamaGpuLayersTextBox.Text, out var llamaGpuLayers) ? llamaGpuLayers : -1,
                    ["ParallelSlots"] = int.TryParse(LlamaParallelSlotsTextBox.Text, out var llamaParallelSlots) ? Math.Max(1, llamaParallelSlots) : 1,
                    ["EnableFlashAttention"] = LlamaEnableFlashAttentionCheckBox.IsChecked == true,
                    ["CacheTypeK"] = (LlamaCacheTypeKComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "q8_0",
                    ["CacheTypeV"] = (LlamaCacheTypeVComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "q8_0",
                    ["EnableCpuMoE"] = LlamaEnableCpuMoeCheckBox.IsChecked == true,
                    ["CpuMoELayerCount"] = int.TryParse(LlamaCpuMoeLayerCountTextBox.Text, out var llamaCpuMoeLayerCount) ? Math.Max(0, llamaCpuMoeLayerCount) : 0,
                    ["EnableAutoFit"] = LlamaEnableAutoFitCheckBox.IsChecked == true,
                    ["FitContextSize"] = int.TryParse(LlamaFitContextSizeTextBox.Text, out var llamaFitContextSize) ? Math.Max(4096, llamaFitContextSize) : 262144,
                    ["DisableWarmup"] = LlamaDisableWarmupCheckBox.IsChecked == true,
                    ["StartupTimeoutSeconds"] = 45
                };

                // 更新智谱AI配置
                providersDict["ZhipuAI"] = new Dictionary<string, object>
                {
                    ["ProviderKind"] = "ZhipuAI",
                    ["BaseUrl"] = ZhipuAIBaseUrlTextBox.Text.Trim(),
                    ["ApiKey"] = GetPersistedApiKey(ZhipuAIApiKeyPasswordBox.Password, "ZhiPu_API_KEY", "ZHIPU_API_KEY"),
                    ["DefaultModel"] = (ZhipuAIModelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "glm-4.7-flash",
                    ["TimeoutSeconds"] = int.TryParse(ZhipuAITimeoutTextBox.Text, out var zTimeout) ? zTimeout : 120,
                    ["MaxRetries"] = int.TryParse(ZhipuAIMaxRetriesTextBox.Text, out var zRetries) ? zRetries : 3,
                    ["DefaultTemperature"] = double.TryParse(ZhipuAITemperatureTextBox.Text, out var zTemp) ? zTemp : 0.7,
                    ["DefaultMaxTokens"] = 4000,
                    ["EnableStreaming"] = ZhipuAIEnableStreamingCheckBox.IsChecked == true
                };

                if (!providersDict.ContainsKey("Ollama") || providersDict["Ollama"] is not Dictionary<string, object> ollamaDict)
                {
                    ollamaDict = new Dictionary<string, object>();
                    providersDict["Ollama"] = ollamaDict;
                }

                // 更新 Ollama 配置
                {
                    ollamaDict["ProviderKind"] = "Ollama";
                    ollamaDict["BaseUrl"] = OllamaBaseUrlTextBox.Text.Trim();
                    ollamaDict["TimeoutSeconds"] = int.TryParse(OllamaTimeoutTextBox.Text, out var oTimeout) ? oTimeout : 120;
                    ollamaDict["MaxRetries"] = int.TryParse(OllamaMaxRetriesTextBox.Text, out var oRetries) ? oRetries : 3;
                    ollamaDict["ContextSize"] = int.TryParse(OllamaContextSizeTextBox.Text, out var oContextSize) ? Math.Clamp(oContextSize, 1024, 262144) : 32768;
                    ollamaDict["MaxConcurrentRequests"] = int.TryParse(OllamaMaxConcurrentRequestsTextBox.Text, out var oMaxConcurrent) ? Math.Clamp(oMaxConcurrent, 1, 20) : 3;
                    ollamaDict["EnableGPU"] = OllamaEnableGPUCheckBox.IsChecked == true;
                    ollamaDict["GPUDeviceId"] = int.TryParse(OllamaGPUDeviceIdTextBox.Text, out var oGpuId) ? oGpuId : -1;
                    ollamaDict["EnableVerboseLogging"] = OllamaVerboseLoggingCheckBox.IsChecked == true;
                    if (OllamaModelComboBox.SelectedItem is ComboBoxItem ollamaModelItem)
                        ollamaDict["DefaultModel"] = ollamaModelItem.Tag?.ToString() ?? "qwq:latest";
                }

                if (!providersDict.ContainsKey("DeepSeek") || providersDict["DeepSeek"] is not Dictionary<string, object> deepSeekDict)
                {
                    deepSeekDict = new Dictionary<string, object>();
                    providersDict["DeepSeek"] = deepSeekDict;
                }

                // 更新 DeepSeek 配置
                {
                    deepSeekDict["ProviderKind"] = "DeepSeek";
                    deepSeekDict["ApiKey"] = GetPersistedApiKey(DeepSeekApiKeyPasswordBox.Password, "DeepSeek_API_KEY", "DEEPSEEK_API_KEY");
                    deepSeekDict["BaseUrl"] = DeepSeekBaseUrlTextBox.Text.Trim();
                    deepSeekDict["TimeoutSeconds"] = int.TryParse(DeepSeekTimeoutTextBox.Text, out var dTimeout) ? dTimeout : 60;
                    deepSeekDict["EnableThinkingChain"] = DeepSeekEnableThinkingChainCheckBox.IsChecked == true;
                    if (DeepSeekModelComboBox.SelectedItem is ComboBoxItem deepSeekModelItem)
                    {
                        var selectedModel = deepSeekModelItem.Content?.ToString() ?? "deepseek-v4-flash";
                        deepSeekDict["DefaultModel"] = selectedModel;
                        deepSeekDict["Model"] = selectedModel;
                    }
                }

                providersDict["XiaoMiMiMo"] = new Dictionary<string, object>
                {
                    ["ProviderKind"] = "XiaoMiMiMo",
                    ["BaseUrl"] = XiaoMiMiMoBaseUrlTextBox.Text.Trim(),
                    ["ApiKey"] = GetPersistedApiKey(XiaoMiMiMoApiKeyPasswordBox.Password, "XiaoMiMiMo_API_KEY", "XIAOMIMIMO_API_KEY", "MIMO_API_KEY"),
                    ["DefaultModel"] = (XiaoMiMiMoModelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "mimo-v2.5-pro",
                    ["TimeoutSeconds"] = int.TryParse(XiaoMiMiMoTimeoutTextBox.Text, out var xTimeout) ? xTimeout : 120,
                    ["MaxRetries"] = int.TryParse(XiaoMiMiMoMaxRetriesTextBox.Text, out var xRetries) ? xRetries : 3,
                    ["DefaultTemperature"] = double.TryParse(XiaoMiMiMoTemperatureTextBox.Text, out var xTemp) ? xTemp : 1.0,
                    ["DefaultMaxTokens"] = 8192,
                    ["EnableStreaming"] = XiaoMiMiMoEnableStreamingCheckBox.IsChecked == true
                };

                aiDict["AgentRoles"] = new Dictionary<string, object>
                {
                    ["EnableDualAgentWorkflow"] = EnableDualAgentWorkflowCheckBox.IsChecked == true,
                    ["EnableArchiveWrite"] = EnableArchiveWriteCheckBox.IsChecked == true,
                    ["MainAgentProvider"] = (MainAgentProviderComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "DeepSeek",
                    ["MainAgentModel"] = MainAgentModelTextBox.Text.Trim(),
                    ["MainAgentRoleDescription"] = MainAgentRoleDescriptionTextBox.Text.Trim(),
                    ["SubAgentProvider"] = (SubAgentProviderComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "LlamaCpp",
                    ["SubAgentModel"] = SubAgentModelTextBox.Text.Trim(),
                    ["SubAgentRoleDescription"] = SubAgentRoleDescriptionTextBox.Text.Trim()
                };

                // 序列化并写入
                var options = new JsonSerializerOptions { WriteIndented = true };
                var newJson = JsonSerializer.Serialize(configDict, options);
                await SaveUserConfigurationAsync(configPath, newJson);

                ConfigPathTextBlock.Text = configPath;

                if (_configuration is IConfigurationRoot configurationRoot)
                {
                    configurationRoot.Reload();
                }

                try
                {
                    await _modelManager.ReinitializeAllProvidersAsync(_configuration);

                    var configuredDefaultProvider = _configuration["AI:DefaultProvider"];
                    if (!string.IsNullOrWhiteSpace(configuredDefaultProvider) &&
                        !_modelManager.SetDefaultProvider(configuredDefaultProvider))
                    {
                        var fallbackProvider = _modelManager.ResolvePreferredProviderName(configuredDefaultProvider);
                        if (!string.IsNullOrWhiteSpace(fallbackProvider))
                        {
                            _logger.LogWarning("配置的默认提供者 {ProviderName} 当前不可用，已回退到 {FallbackProvider}", configuredDefaultProvider, fallbackProvider);
                            _modelManager.SetDefaultProvider(fallbackProvider);
                        }
                    }
                }
                catch (Exception reloadEx)
                {
                    _logger.LogWarning(reloadEx, "热重载提供者时出现警告");
                }

                UpdateStatus("配置保存成功，已立即生效");
                MessageBox.Show($"配置已保存到用户配置文件：\n{configPath}\n\n配置已热重载，无需重启应用即可生效。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private string GetUserConfigurationPath()
        {
            return _configuration["Paths:UserConfigurationPath"]
                ?? Path.Combine(_configurationService.GetConfigurationDirectory(), "appsettings.user.json");
        }

        #region debug-point A:report-helper
        private static async Task ReportDebugEventAsync(string hypothesisId, string location, string message, Dictionary<string, object?>? data = null)
        {
            try
            {
                var debugServerUrl = "http://127.0.0.1:7777/event";
                var debugSessionId = "single-file-ai-ui";
                var envPath = FindDebugEnvPath();
                if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
                {
                    foreach (var line in File.ReadAllLines(envPath, Encoding.UTF8))
                    {
                        if (line.StartsWith("DEBUG_SERVER_URL=", StringComparison.OrdinalIgnoreCase))
                        {
                            debugServerUrl = line["DEBUG_SERVER_URL=".Length..].Trim();
                        }
                        else if (line.StartsWith("DEBUG_SESSION_ID=", StringComparison.OrdinalIgnoreCase))
                        {
                            debugSessionId = line["DEBUG_SESSION_ID=".Length..].Trim();
                        }
                    }
                }

                var payload = JsonSerializer.Serialize(new
                {
                    sessionId = debugSessionId,
                    runId = "pre-fix",
                    hypothesisId,
                    location,
                    msg = $"[DEBUG] {message}",
                    data,
                    ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });

                using var client = new System.Net.Http.HttpClient();
                using var content = new System.Net.Http.StringContent(payload, Encoding.UTF8, "application/json");
                await client.PostAsync(debugServerUrl, content);
            }
            catch
            {
                // ignore debug instrumentation failures
            }
        }

        private static string? FindDebugEnvPath()
        {
            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".dbg", "single-file-ai-ui.env"),
                Path.Combine(Directory.GetCurrentDirectory(), ".dbg", "single-file-ai-ui.env")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, ".dbg", "single-file-ai-ui.env");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return null;
        }
        #endregion

        private static Dictionary<string, object> LoadJsonAsDictionary(string path)
        {
            var json = File.ReadAllText(path);
            using var jsonDoc = JsonDocument.Parse(json);
            var root = jsonDoc.RootElement;
            var configDict = new Dictionary<string, object>();

            foreach (var prop in root.EnumerateObject())
            {
                configDict[prop.Name] = DeserializeElement(prop.Value);
            }

            return configDict;
        }

        private async Task SaveUserConfigurationAsync(string configPath, string jsonContent)
        {
            var directory = Path.GetDirectoryName(configPath)
                ?? throw new InvalidOperationException("用户配置文件目录无效。");

            Directory.CreateDirectory(directory);

            const int maxAttempts = 5;
            Exception? lastException = null;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var tempPath = Path.Combine(directory, $".{Path.GetFileName(configPath)}.{Guid.NewGuid():N}.tmp");

                try
                {
                    if (File.Exists(configPath))
                    {
                        var attributes = File.GetAttributes(configPath);
                        if ((attributes & FileAttributes.ReadOnly) != 0)
                        {
                            File.SetAttributes(configPath, attributes & ~FileAttributes.ReadOnly);
                        }
                    }

                    await File.WriteAllTextAsync(tempPath, jsonContent, new UTF8Encoding(false));

                    if (File.Exists(configPath))
                    {
                        File.Copy(tempPath, configPath, overwrite: true);
                    }
                    else
                    {
                        File.Move(tempPath, configPath);
                    }

                    return;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                {
                    lastException = ex;
                    _logger.LogWarning(ex,
                        "写入用户配置文件失败，正在重试 attempt={Attempt}/{MaxAttempts} path={ConfigPath}",
                        attempt,
                        maxAttempts,
                        configPath);

                    if (attempt == maxAttempts)
                    {
                        break;
                    }

                    await Task.Delay(200 * attempt);
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tempPath))
                        {
                            File.Delete(tempPath);
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogDebug(cleanupEx, "清理临时配置文件失败 path={TempPath}", tempPath);
                    }
                }
            }

            throw new IOException($"无法写入配置文件：{configPath}", lastException);
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

        private static string GetPersistedApiKey(string apiKey, params string[] environmentVariableNames)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return string.Empty;
            }

            foreach (var variableName in environmentVariableNames)
            {
                var envValue = Environment.GetEnvironmentVariable(variableName);
                if (!string.IsNullOrWhiteSpace(envValue) &&
                    string.Equals(apiKey, envValue, StringComparison.Ordinal))
                {
                    return string.Empty;
                }
            }

            return apiKey;
        }

        private string GetSelectedLlamaDefaultModel()
        {
            var selectedPath = (LlamaModelComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                return Path.GetFileNameWithoutExtension(selectedPath);
            }

            var configuredPath = _configuration["AI:Providers:LlamaCpp:ModelPath"] ?? "";
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return Path.GetFileNameWithoutExtension(configuredPath);
            }

            return "local-gguf";
        }

        private static void SelectComboBoxItemByContent(ComboBox comboBox, string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private static void SelectComboBoxItemByTag(ComboBox comboBox, string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (string.Equals(item.Tag?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
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
