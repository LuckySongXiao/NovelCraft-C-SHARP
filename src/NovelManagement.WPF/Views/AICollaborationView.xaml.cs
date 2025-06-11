using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using System.Net.Http;
using NovelManagement.AI.Agents;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Workflow;
using NovelManagement.AI.Services;
using NovelManagement.AI.Services.DeepSeek;
using NovelManagement.AI.Services.DeepSeek.Models;
using NovelManagement.AI.Services.Ollama;
using NovelManagement.AI.Services.ThinkingChain;
using NovelManagement.AI.Services.ThinkingChain.Models;
using NovelManagement.WPF.Services;
using Microsoft.Extensions.Configuration;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// AICollaborationView.xaml 的交互逻辑
    /// </summary>
    public partial class AICollaborationView : UserControl
    {
        #region 数据模型

        /// <summary>
        /// Agent视图模型
        /// </summary>
        public class AgentViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string StatusDescription { get; set; } = string.Empty;
            public string CurrentTask { get; set; } = string.Empty;
            public int Progress { get; set; }
            public PackIconKind IconKind { get; set; }
            public SolidColorBrush StatusColor { get; set; } = new SolidColorBrush(Colors.Gray);
            public bool HasCurrentTask => !string.IsNullOrEmpty(CurrentTask);
            public bool IsWorking => Progress > 0 && Progress < 100;
        }

        /// <summary>
        /// 任务视图模型
        /// </summary>
        public class TaskViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string TaskType { get; set; } = string.Empty;
            public string TargetAgentId { get; set; } = string.Empty;
            public WorkflowStatus Status { get; set; }
            public int Progress { get; set; }
            public PackIconKind StatusIcon { get; set; }
            public SolidColorBrush StatusColor { get; set; } = new SolidColorBrush(Colors.Gray);
            public bool IsRunning => Status == WorkflowStatus.Running;
            public bool CanCancel => Status == WorkflowStatus.Pending || Status == WorkflowStatus.Running;
        }

        #endregion

        #region 字段和属性

        private readonly ObservableCollection<AgentViewModel> _agents;
        private readonly ObservableCollection<TaskViewModel> _tasks;
        private readonly StringBuilder _logBuilder;
        private readonly ILogger<AICollaborationView> _logger;
        private NovelWorkflowEngine _workflowEngine = null!;
        private TaskQueue _taskQueue = null!;

        // 模拟的Agent实例
        private List<IAgent> _agentInstances = null!;

        // AI服务相关
        private ModelManager? _modelManager;
        private IOllamaApiService? _ollamaApiService;
        private IDeepSeekApiService? _deepSeekApiService;
        private IThinkingChainProcessor? _thinkingChainProcessor;
        private FloatingTextManager? _floatingTextManager;
        private ConfigurationService? _configurationService;
        private AIUsageStatisticsService? _statisticsService;
        private IConfiguration? _configuration;
        private DeepSeekConfiguration _currentConfiguration;
        private UIConfiguration _uiConfiguration;
        private string _currentProvider = "Ollama";

        // 模型刷新相关
        private DateTime _lastModelRefresh = DateTime.MinValue;
        private readonly TimeSpan _modelCacheTimeout = TimeSpan.FromMinutes(5); // 5分钟缓存
        private readonly Dictionary<string, List<ModelInfo>> _modelCache = new();
        private bool _isRefreshingModels = false;
        private System.Windows.Threading.DispatcherTimer? _autoRefreshTimer;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public AICollaborationView()
        {
            try
            {
                InitializeComponent();

                // 初始化集合
                _agents = new ObservableCollection<AgentViewModel>();
                _tasks = new ObservableCollection<TaskViewModel>();
                _logBuilder = new StringBuilder();

                // 创建模拟的日志记录器
                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                _logger = loggerFactory.CreateLogger<AICollaborationView>();

                // 初始化配置
                _currentConfiguration = new DeepSeekConfiguration();
                _uiConfiguration = new UIConfiguration();

                // 设置数据绑定
                AgentListView.ItemsSource = _agents;
                TaskQueueListView.ItemsSource = _tasks;

                // 延迟初始化，确保UI完全加载后再执行
                this.Loaded += (s, e) => InitializeAfterLoaded(loggerFactory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI协作界面构造失败: {ex.Message}\n\n详细信息: {ex}",
                    "初始化错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 在UI加载完成后进行初始化
        /// </summary>
        private void InitializeAfterLoaded(ILoggerFactory loggerFactory)
        {
            try
            {
                // 创建任务队列和工作流引擎
                var taskQueueLogger = loggerFactory.CreateLogger<TaskQueue>();
                _taskQueue = new TaskQueue(taskQueueLogger);

                var workflowEngineLogger = loggerFactory.CreateLogger<NovelWorkflowEngine>();
                _workflowEngine = new NovelWorkflowEngine(workflowEngineLogger, _taskQueue);

                // 初始化AI服务
                InitializeAIServices(loggerFactory);

                // 初始化UI
                InitializeUI();

                // 从服务提供者获取Agent实例
                _agentInstances = new List<IAgent>();

                try
                {
                    var serviceProvider = App.ServiceProvider;
                    if (serviceProvider != null)
                    {
                        // 尝试从服务提供者获取Agent实例
                        var directorAgent = serviceProvider.GetService<DirectorAgent>();
                        var writerAgent = serviceProvider.GetService<WriterAgent>();
                        var editorAgent = serviceProvider.GetService<EditorAgent>();
                        var criticAgent = serviceProvider.GetService<CriticAgent>();
                        var researcherAgent = serviceProvider.GetService<ResearcherAgent>();
                        var summarizerAgent = serviceProvider.GetService<SummarizerAgent>();
                        var readerAgent = serviceProvider.GetService<ReaderAgent>();
                        var settingManagerAgent = serviceProvider.GetService<SettingManagerAgent>();

                        // 添加可用的Agent
                        if (directorAgent != null) _agentInstances.Add(directorAgent);
                        if (writerAgent != null) _agentInstances.Add(writerAgent);
                        if (editorAgent != null) _agentInstances.Add(editorAgent);
                        if (criticAgent != null) _agentInstances.Add(criticAgent);
                        if (researcherAgent != null) _agentInstances.Add(researcherAgent);
                        if (summarizerAgent != null) _agentInstances.Add(summarizerAgent);
                        if (readerAgent != null) _agentInstances.Add(readerAgent);
                        if (settingManagerAgent != null) _agentInstances.Add(settingManagerAgent);

                        AddLog($"成功加载 {_agentInstances.Count} 个Agent实例");
                    }
                    else
                    {
                        AddLog("服务提供者未初始化，无法加载Agent实例");
                    }
                }
                catch (Exception ex)
                {
                    AddLog($"加载Agent实例失败: {ex.Message}");
                    // 如果加载失败，创建空列表避免后续错误
                    _agentInstances = new List<IAgent>();
                }

                // 订阅事件
                _taskQueue.TaskStatusChanged += OnTaskStatusChanged;
                _workflowEngine.WorkflowStatusChanged += OnWorkflowStatusChanged;
                _workflowEngine.TaskStatusChanged += OnTaskStatusChanged;

                // 初始化数据
                InitializeAsync();
            }
            catch (Exception ex)
            {
                AddLog($"延迟初始化失败: {ex.Message}");
                MessageBox.Show($"AI协作系统初始化失败: {ex.Message}",
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 初始化AI服务
        /// </summary>
        /// <param name="loggerFactory">日志工厂</param>
        private void InitializeAIServices(ILoggerFactory loggerFactory)
        {
            try
            {
                // 获取服务
                var serviceProvider = App.ServiceProvider;
                if (serviceProvider == null)
                {
                    AddLog("服务提供者未初始化，跳过AI服务初始化");
                    return;
                }

                // 安全地获取服务
                _configuration = serviceProvider.GetService<IConfiguration>();
                _modelManager = serviceProvider.GetService<ModelManager>();
                _ollamaApiService = serviceProvider.GetService<IOllamaApiService>();

                // 获取或创建配置服务
                _configurationService = serviceProvider.GetService<ConfigurationService>() ??
                    new ConfigurationService(loggerFactory.CreateLogger<ConfigurationService>());

                // 获取或创建统计服务
                _statisticsService = serviceProvider.GetService<AIUsageStatisticsService>() ??
                    new AIUsageStatisticsService(loggerFactory.CreateLogger<AIUsageStatisticsService>());

                // 获取或创建思维链处理器
                _thinkingChainProcessor = serviceProvider.GetService<IThinkingChainProcessor>() ??
                    new ThinkingChainProcessor(loggerFactory.CreateLogger<ThinkingChainProcessor>());

                // 获取或创建DeepSeek API服务
                _deepSeekApiService = serviceProvider.GetService<IDeepSeekApiService>();
                if (_deepSeekApiService == null)
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    if (httpClientFactory == null)
                    {
                        // 如果没有注册HttpClientFactory，创建一个简单的实现
                        var services = new ServiceCollection();
                        services.AddHttpClient();
                        var provider = services.BuildServiceProvider();
                        httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                    }

                    _deepSeekApiService = new DeepSeekApiService(
                        loggerFactory.CreateLogger<DeepSeekApiService>(),
                        httpClientFactory,
                        _thinkingChainProcessor);
                }

                // 获取或创建悬浮文本管理器
                _floatingTextManager = serviceProvider.GetService<FloatingTextManager>() ??
                    new FloatingTextManager(
                        loggerFactory.CreateLogger<FloatingTextManager>(),
                        _thinkingChainProcessor);

                // 订阅事件
                if (_deepSeekApiService != null)
                {
                    _deepSeekApiService.ThinkingChainUpdated += OnThinkingChainUpdated;
                }

                if (_floatingTextManager != null)
                {
                    _floatingTextManager.WindowCreated += OnThinkingChainWindowCreated;
                    _floatingTextManager.WindowClosed += OnThinkingChainWindowClosed;
                }

                // 加载配置
                LoadConfigurationAsync();

                AddLog("AI服务初始化完成");
            }
            catch (Exception ex)
            {
                AddLog($"AI服务初始化失败: {ex.Message}");
                MessageBox.Show($"AI服务初始化失败：{ex.Message}\n\n应用程序将继续运行，但AI功能可能不可用。",
                    "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 初始化UI
        /// </summary>
        private void InitializeUI()
        {
            try
            {
                // 设置默认提供者
                var defaultProvider = _configuration?["AI:DefaultProvider"] ?? "Ollama";
                _currentProvider = defaultProvider;

                // 设置提供者选择
                if (ProviderComboBox != null)
                {
                    foreach (ComboBoxItem item in ProviderComboBox.Items)
                    {
                        if (item.Tag?.ToString() == defaultProvider)
                        {
                            ProviderComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // 异步加载模型列表和检查连接状态（在UI线程上执行）
                _ = Dispatcher.BeginInvoke(async () =>
                {
                    try
                    {
                        await LoadModelsAsync();
                        await CheckConnectionStatusAsync();
                    }
                    catch (Exception ex)
                    {
                        AddLog($"异步初始化失败: {ex.Message}");
                    }
                });

                // 启动自动刷新定时器（每10分钟检查一次）
                InitializeAutoRefreshTimer();
            }
            catch (Exception ex)
            {
                AddLog($"UI初始化失败: {ex.Message}");
                MessageBox.Show($"UI初始化失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 初始化自动刷新定时器
        /// </summary>
        private void InitializeAutoRefreshTimer()
        {
            try
            {
                _autoRefreshTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMinutes(10) // 每10分钟自动检查一次
                };

                _autoRefreshTimer.Tick += async (sender, e) =>
                {
                    try
                    {
                        // 只有当前提供者是Ollama时才自动刷新（因为Ollama模型可能会变化）
                        if (_currentProvider == "Ollama" && !_isRefreshingModels)
                        {
                            AddLog("自动检查Ollama模型更新...");
                            await RefreshModelsAsync(forceRefresh: false);
                            await CheckConnectionStatusAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLog($"自动刷新失败: {ex.Message}");
                    }
                };

                _autoRefreshTimer.Start();
                AddLog("自动刷新定时器已启动（每10分钟检查一次）");
            }
            catch (Exception ex)
            {
                AddLog($"初始化自动刷新定时器失败: {ex.Message}");
            }
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 异步初始化
        /// </summary>
        private async void InitializeAsync()
        {
            try
            {
                // 注册所有Agent
                foreach (var agent in _agentInstances)
                {
                    await _workflowEngine.RegisterAgentAsync(agent);
                    await agent.InitializeAsync(new Dictionary<string, object>());
                    
                    // 订阅Agent事件
                    agent.StatusChanged += OnAgentStatusChanged;
                    agent.TaskCompleted += OnAgentTaskCompleted;
                }

                // 加载Agent数据
                LoadAgents();

                // 更新任务队列状态
                UpdateTaskQueueStatus();

                // 检查Ollama服务状态
                await CheckOllamaServiceAsync();

                AddLog("AI协作系统初始化完成");
            }
            catch (Exception ex)
            {
                AddLog($"初始化失败: {ex.Message}");
                MessageBox.Show($"AI协作系统初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载Agent数据
        /// </summary>
        private void LoadAgents()
        {
            _agents.Clear();

            foreach (var agent in _agentInstances)
            {
                var agentViewModel = new AgentViewModel
                {
                    Id = agent.Id,
                    Name = agent.Name,
                    StatusDescription = GetStatusDescription(agent.Status),
                    CurrentTask = string.Empty,
                    Progress = 0,
                    IconKind = GetAgentIcon(agent.Name),
                    StatusColor = GetStatusColor(agent.Status)
                };

                _agents.Add(agentViewModel);
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            try
            {
                // 停止自动刷新定时器
                if (_autoRefreshTimer != null)
                {
                    _autoRefreshTimer.Stop();
                    _autoRefreshTimer = null;
                    AddLog("自动刷新定时器已停止");
                }

                // 清理模型缓存
                _modelCache.Clear();

                AddLog("资源清理完成");
            }
            catch (Exception ex)
            {
                AddLog($"清理资源时出错: {ex.Message}");
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 刷新Agent状态
        /// </summary>
        private async void RefreshAgents_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var agentViewModel in _agents)
                {
                    var agent = _agentInstances.FirstOrDefault(a => a.Id == agentViewModel.Id);
                    if (agent != null)
                    {
                        var statusInfo = await agent.GetStatusAsync();
                        agentViewModel.StatusDescription = statusInfo.StatusDescription;
                        agentViewModel.CurrentTask = statusInfo.CurrentTask ?? string.Empty;
                        agentViewModel.Progress = statusInfo.Progress;
                        agentViewModel.StatusColor = GetStatusColor(statusInfo.Status);
                    }
                }

                AddLog("Agent状态已刷新");
            }
            catch (Exception ex)
            {
                AddLog($"刷新Agent状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 查看Agent详情
        /// </summary>
        private async void ViewAgentDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is AgentViewModel agentViewModel)
                {
                    var agent = _agentInstances?.FirstOrDefault(a => a.Id == agentViewModel.Id);
                    if (agent != null)
                    {
                        var capabilities = await agent.GetCapabilitiesAsync();
                        var capabilityText = string.Join("\n", capabilities.Select(c => $"• {c.Name}: {c.Description}"));

                        MessageBox.Show($"Agent: {agent.Name}\n描述: {agent.Description}\n版本: {agent.Version}\n\n能力列表:\n{capabilityText}",
                                      "Agent详情", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"查看Agent详情失败: {ex.Message}");
                MessageBox.Show($"查看Agent详情失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 重置Agent
        /// </summary>
        private async void ResetAgent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is AgentViewModel agentViewModel)
                {
                    var agent = _agentInstances?.FirstOrDefault(a => a.Id == agentViewModel.Id);
                    if (agent != null)
                    {
                        await agent.ResetAsync();
                        AddLog($"Agent {agent.Name} 已重置");
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"重置Agent失败: {ex.Message}");
                MessageBox.Show($"重置Agent失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 创建项目初始化工作流
        /// </summary>
        private async void CreateProjectInitWorkflow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["theme"] = "修仙小说",
                    ["genre"] = "古典仙侠",
                    ["requirements"] = "创建一个完整的修仙世界"
                };

                var workflow = await _workflowEngine.CreatePredefinedWorkflowAsync("ProjectInitialization", parameters);
                var result = await _workflowEngine.ExecuteWorkflowAsync(workflow);

                AddLog($"项目初始化工作流已创建并执行: {(result.IsSuccess ? "成功" : "失败")}");
            }
            catch (Exception ex)
            {
                AddLog($"创建项目初始化工作流失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建章节创建工作流
        /// </summary>
        private async void CreateChapterWorkflow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["chapterNumber"] = 1,
                    ["outline"] = "主角觉醒，踏入修仙之路"
                };

                var workflow = await _workflowEngine.CreatePredefinedWorkflowAsync("ChapterCreation", parameters);
                var result = await _workflowEngine.ExecuteWorkflowAsync(workflow);

                AddLog($"章节创建工作流已创建并执行: {(result.IsSuccess ? "成功" : "失败")}");
            }
            catch (Exception ex)
            {
                AddLog($"创建章节创建工作流失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建内容审查工作流
        /// </summary>
        private async void CreateReviewWorkflow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["content"] = "待审查的内容"
                };

                var workflow = await _workflowEngine.CreatePredefinedWorkflowAsync("ContentReview", parameters);
                AddLog("内容审查工作流已创建");
            }
            catch (Exception ex)
            {
                AddLog($"创建内容审查工作流失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建一致性检查工作流
        /// </summary>
        private async void CreateConsistencyWorkflow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["newContent"] = "新增内容",
                    ["existingSettings"] = "现有设定"
                };

                var workflow = await _workflowEngine.CreatePredefinedWorkflowAsync("ConsistencyCheck", parameters);
                AddLog("一致性检查工作流已创建");
            }
            catch (Exception ex)
            {
                AddLog($"创建一致性检查工作流失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        private void CancelTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TaskViewModel taskViewModel)
            {
                _taskQueue.CancelTask(taskViewModel.Id);
                AddLog($"任务 {taskViewModel.Name} 已取消");
            }
        }

        /// <summary>
        /// 清空任务队列
        /// </summary>
        private void ClearQueue_Click(object sender, RoutedEventArgs e)
        {
            _taskQueue.ClearQueue();
            _tasks.Clear();
            UpdateTaskQueueStatus();
            AddLog("任务队列已清空");
        }

        /// <summary>
        /// 清空日志
        /// </summary>
        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            _logBuilder.Clear();
            LogTextBlock.Text = string.Empty;
        }

        #endregion

        #region Agent和任务事件处理

        /// <summary>
        /// Agent状态变化事件处理
        /// </summary>
        private void OnAgentStatusChanged(object? sender, AgentStatusInfo statusInfo)
        {
            Dispatcher.Invoke(() =>
            {
                var agentViewModel = _agents.FirstOrDefault(a => a.Id == statusInfo.AgentId);
                if (agentViewModel != null)
                {
                    agentViewModel.StatusDescription = statusInfo.StatusDescription;
                    agentViewModel.CurrentTask = statusInfo.CurrentTask ?? string.Empty;
                    agentViewModel.Progress = statusInfo.Progress;
                    agentViewModel.StatusColor = GetStatusColor(statusInfo.Status);
                }

                AddLog($"Agent {statusInfo.Name}: {statusInfo.StatusDescription}");
            });
        }

        /// <summary>
        /// Agent任务完成事件处理
        /// </summary>
        private void OnAgentTaskCompleted(object? sender, AgentTaskResult result)
        {
            Dispatcher.Invoke(() =>
            {
                AddLog($"Agent任务完成: {(result.IsSuccess ? "成功" : "失败")}, 耗时: {result.ExecutionTime.TotalSeconds:F2}秒");
            });
        }

        /// <summary>
        /// 任务状态变化事件处理
        /// </summary>
        private void OnTaskStatusChanged(object? sender, WorkflowTask task)
        {
            Dispatcher.Invoke(() =>
            {
                var taskViewModel = _tasks.FirstOrDefault(t => t.Id == task.Id);
                if (taskViewModel == null)
                {
                    taskViewModel = new TaskViewModel
                    {
                        Id = task.Id,
                        Name = task.Name,
                        TaskType = task.TaskType,
                        TargetAgentId = task.TargetAgentId
                    };
                    _tasks.Add(taskViewModel);
                }

                taskViewModel.Status = task.Status;
                taskViewModel.Progress = task.Progress;
                taskViewModel.StatusIcon = GetTaskStatusIcon(task.Status);
                taskViewModel.StatusColor = GetTaskStatusColor(task.Status);

                UpdateTaskQueueStatus();
                AddLog($"任务 {task.Name}: {GetTaskStatusDescription(task.Status)}");
            });
        }

        /// <summary>
        /// 工作流状态变化事件处理
        /// </summary>
        private void OnWorkflowStatusChanged(object? sender, WorkflowDefinition workflow)
        {
            Dispatcher.Invoke(() =>
            {
                AddLog($"工作流 {workflow.Name}: {GetWorkflowStatusDescription(workflow.Status)}, 进度: {workflow.Progress}%");
            });
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取Agent图标
        /// </summary>
        private PackIconKind GetAgentIcon(string agentName)
        {
            return agentName switch
            {
                "编剧Agent" => PackIconKind.MovieEdit,
                "作家Agent" => PackIconKind.Pencil,
                "总结Agent" => PackIconKind.FileDocument,
                "读者Agent" => PackIconKind.AccountEye,
                "设定管理Agent" => PackIconKind.Cog,
                _ => PackIconKind.Robot
            };
        }

        /// <summary>
        /// 获取状态颜色
        /// </summary>
        private SolidColorBrush GetStatusColor(AgentStatus status)
        {
            return status switch
            {
                AgentStatus.Idle => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                AgentStatus.Working => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                AgentStatus.Waiting => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                AgentStatus.Error => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                AgentStatus.Offline => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        /// <summary>
        /// 获取状态描述
        /// </summary>
        private string GetStatusDescription(AgentStatus status)
        {
            return status switch
            {
                AgentStatus.Idle => "空闲中",
                AgentStatus.Working => "工作中",
                AgentStatus.Waiting => "等待中",
                AgentStatus.Error => "错误状态",
                AgentStatus.Offline => "离线",
                _ => "未知状态"
            };
        }

        /// <summary>
        /// 获取任务状态图标
        /// </summary>
        private PackIconKind GetTaskStatusIcon(WorkflowStatus status)
        {
            return status switch
            {
                WorkflowStatus.Pending => PackIconKind.Clock,
                WorkflowStatus.Running => PackIconKind.Play,
                WorkflowStatus.Completed => PackIconKind.Check,
                WorkflowStatus.Paused => PackIconKind.Pause,
                WorkflowStatus.Cancelled => PackIconKind.Cancel,
                WorkflowStatus.Failed => PackIconKind.Alert,
                _ => PackIconKind.Help
            };
        }

        /// <summary>
        /// 获取任务状态颜色
        /// </summary>
        private SolidColorBrush GetTaskStatusColor(WorkflowStatus status)
        {
            return status switch
            {
                WorkflowStatus.Pending => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                WorkflowStatus.Running => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                WorkflowStatus.Completed => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                WorkflowStatus.Paused => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                WorkflowStatus.Cancelled => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                WorkflowStatus.Failed => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        /// <summary>
        /// 获取任务状态描述
        /// </summary>
        private string GetTaskStatusDescription(WorkflowStatus status)
        {
            return status switch
            {
                WorkflowStatus.Pending => "等待执行",
                WorkflowStatus.Running => "执行中",
                WorkflowStatus.Completed => "已完成",
                WorkflowStatus.Paused => "已暂停",
                WorkflowStatus.Cancelled => "已取消",
                WorkflowStatus.Failed => "执行失败",
                _ => "未知状态"
            };
        }

        /// <summary>
        /// 获取工作流状态描述
        /// </summary>
        private string GetWorkflowStatusDescription(WorkflowStatus status)
        {
            return GetTaskStatusDescription(status);
        }

        /// <summary>
        /// 更新任务队列状态
        /// </summary>
        private void UpdateTaskQueueStatus()
        {
            var queueStatus = _taskQueue.GetQueueStatus();
            TaskQueueStatusText.Text = $"({queueStatus.PendingTasks} 待处理, {queueStatus.RunningTasks} 运行中, {queueStatus.CompletedTasks} 已完成)";
        }

        /// <summary>
        /// 添加日志
        /// </summary>
        private void AddLog(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                _logBuilder.AppendLine($"[{timestamp}] {message}");

                // 安全地更新UI控件
                if (LogTextBlock != null)
                {
                    LogTextBlock.Text = _logBuilder.ToString();
                }

                if (LogScrollViewer != null)
                {
                    LogScrollViewer.ScrollToEnd();
                }
            }
            catch (Exception ex)
            {
                // 如果日志记录失败，至少记录到调试输出
                System.Diagnostics.Debug.WriteLine($"AddLog failed: {ex.Message}");
            }
        }

        #endregion

        #region DeepSeek API配置事件

        /// <summary>
        /// 测试连接按钮点击
        /// </summary>
        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddLog($"正在测试{_currentProvider}连接...");

                if (_currentProvider == "Ollama" && _ollamaApiService != null)
                {
                    // 测试Ollama连接
                    var testResult = await _ollamaApiService.TestConnectionAsync();

                    if (testResult.IsSuccess)
                    {
                        AddLog($"✅ Ollama连接测试成功 (响应时间: {testResult.ResponseTime.TotalMilliseconds:F0}ms)");
                        if (testResult.ServerInfo != null && testResult.ServerInfo.ContainsKey("Version"))
                        {
                            AddLog($"   服务器版本: {testResult.ServerInfo["Version"]}");
                        }
                        ShowSnackbar("Ollama连接测试成功", PackIconKind.CheckCircle, Brushes.Green);

                        // 连接成功后刷新模型列表
                        await RefreshModelsAsync();

                        // 更新连接状态显示
                        UpdateConnectionStatus();
                    }
                    else
                    {
                        AddLog($"❌ Ollama连接测试失败: {testResult.ErrorMessage}");
                        ShowSnackbar($"Ollama连接测试失败: {testResult.ErrorMessage}", PackIconKind.AlertCircle, Brushes.Red);
                    }
                }
                else if (_currentProvider == "DeepSeek" && _deepSeekApiService != null)
                {
                    // 更新配置
                    UpdateConfigurationFromUI();
                    await _deepSeekApiService.UpdateConfigurationAsync(_currentConfiguration);

                    // 测试连接
                    var isConnected = await _deepSeekApiService.TestConnectionAsync();

                    if (isConnected)
                    {
                        AddLog("✅ DeepSeek API连接测试成功");
                        ShowSnackbar("DeepSeek API连接测试成功", PackIconKind.CheckCircle, Brushes.Green);
                    }
                    else
                    {
                        AddLog("❌ DeepSeek API连接测试失败");
                        ShowSnackbar("DeepSeek API连接测试失败", PackIconKind.AlertCircle, Brushes.Red);
                    }
                }
                else
                {
                    AddLog($"❌ {_currentProvider}服务未初始化或不支持");
                    ShowSnackbar($"{_currentProvider}服务未初始化", PackIconKind.Alert, Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ 连接测试异常: {ex.Message}");
                ShowSnackbar($"连接测试异常: {ex.Message}", PackIconKind.Alert, Brushes.Red);
            }
        }

        /// <summary>
        /// 保存配置按钮点击
        /// </summary>
        private async void SaveConfiguration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddLog($"开始保存{_currentProvider}配置...");

                if (_currentProvider == "Ollama" && _ollamaApiService != null)
                {
                    // 获取当前Ollama配置
                    var ollamaConfig = _ollamaApiService.GetConfiguration();

                    // 验证配置
                    if (!ollamaConfig.IsValid())
                    {
                        var errors = ollamaConfig.GetValidationErrors();
                        AddLog($"❌ Ollama配置验证失败: {string.Join(", ", errors)}");
                        ShowSnackbar("配置验证失败", PackIconKind.Alert, Brushes.Red);
                        return;
                    }

                    // 保存Ollama配置
                    var success = await _ollamaApiService.UpdateConfigurationAsync(ollamaConfig);
                    if (success)
                    {
                        AddLog("✅ Ollama配置已保存");
                        ShowSnackbar("Ollama配置保存成功", PackIconKind.ContentSave, Brushes.Green);

                        // 重新初始化AI服务
                        await ReinitializeAIServicesAsync();
                    }
                    else
                    {
                        AddLog("❌ Ollama配置保存失败");
                        ShowSnackbar("Ollama配置保存失败", PackIconKind.Alert, Brushes.Red);
                    }
                }
                else if (_currentProvider == "DeepSeek" && _deepSeekApiService != null)
                {
                    // 更新DeepSeek配置
                    UpdateConfigurationFromUI();

                    // 验证配置
                    if (!_currentConfiguration.IsValid())
                    {
                        var errors = _currentConfiguration.GetValidationErrors();
                        AddLog($"❌ DeepSeek配置验证失败: {string.Join(", ", errors)}");
                        ShowSnackbar("配置验证失败", PackIconKind.Alert, Brushes.Red);
                        return;
                    }

                    // 保存DeepSeek配置
                    var success = await _deepSeekApiService.UpdateConfigurationAsync(_currentConfiguration);
                    if (success)
                    {
                        SaveConfigurationAsync();
                        AddLog("✅ DeepSeek配置已保存");
                        ShowSnackbar("DeepSeek配置保存成功", PackIconKind.ContentSave, Brushes.Green);

                        // 重新初始化AI服务
                        await ReinitializeAIServicesAsync();
                    }
                    else
                    {
                        AddLog("❌ DeepSeek配置保存失败");
                        ShowSnackbar("DeepSeek配置保存失败", PackIconKind.Alert, Brushes.Red);
                    }
                }
                else
                {
                    AddLog($"❌ 不支持的提供者: {_currentProvider}");
                    ShowSnackbar($"不支持的提供者: {_currentProvider}", PackIconKind.Alert, Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ 保存配置异常: {ex.Message}");
                ShowSnackbar($"保存配置异常: {ex.Message}", PackIconKind.Alert, Brushes.Red);
            }
        }

        #endregion

        #region 思维链控制事件

        /// <summary>
        /// 隐藏所有思维链按钮点击
        /// </summary>
        private void HideAllThinkingChains_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _floatingTextManager?.HideAllWindows();
                AddLog("已隐藏所有思维链窗口");
                UpdateActiveThinkingChainsText();
            }
            catch (Exception ex)
            {
                AddLog($"隐藏思维链窗口失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示测试思维链按钮点击
        /// </summary>
        private async void ShowTestThinkingChain_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_floatingTextManager == null || _thinkingChainProcessor == null)
                {
                    AddLog("思维链服务未初始化");
                    return;
                }

                AddLog("创建测试思维链...");

                // 创建测试思维链
                var testChain = new ThinkingChain
                {
                    Title = "测试思维链",
                    Description = "这是一个测试思维链，展示AI的思考过程",
                    TaskId = Guid.NewGuid().ToString(),
                    AgentId = "TestAgent"
                };

                testChain.Start();

                // 添加测试步骤
                var steps = new[]
                {
                    new { Title = "分析问题", Content = "首先分析用户提出的问题，理解需求的核心要点", Type = ThinkingStepType.Analysis },
                    new { Title = "制定计划", Content = "根据问题分析结果，制定解决方案的具体步骤", Type = ThinkingStepType.Planning },
                    new { Title = "推理过程", Content = "运用逻辑推理，逐步推导出可能的解决方案", Type = ThinkingStepType.Reasoning },
                    new { Title = "方案评估", Content = "评估各种解决方案的可行性和优缺点", Type = ThinkingStepType.Evaluation },
                    new { Title = "综合结论", Content = "综合所有分析结果，得出最终的解决方案", Type = ThinkingStepType.Conclusion }
                };

                // 模拟流式添加步骤
                foreach (var stepData in steps)
                {
                    var step = new ThinkingStep
                    {
                        Title = stepData.Title,
                        Content = stepData.Content,
                        Type = stepData.Type,
                        Confidence = 0.8 + Random.Shared.NextDouble() * 0.2
                    };

                    step.Start();
                    testChain.AddStep(step);
                    await Task.Delay(500); // 模拟思考时间
                    step.Complete();
                    testChain.UpdateProgress();
                }

                testChain.FinalOutput = "测试思维链执行完成，所有步骤都已成功处理。";
                testChain.Complete();

                // 显示思维链
                var windowId = _floatingTextManager.ShowThinkingChain(testChain);
                if (!string.IsNullOrEmpty(windowId))
                {
                    AddLog($"✅ 测试思维链已显示 (ID: {windowId})");
                    UpdateActiveThinkingChainsText();
                }
                else
                {
                    AddLog("❌ 显示测试思维链失败");
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ 创建测试思维链失败: {ex.Message}");
            }
        }

        #endregion

        #region 思维链事件处理

        /// <summary>
        /// 思维链更新事件处理
        /// </summary>
        private void OnThinkingChainUpdated(object? sender, ThinkingChain thinkingChain)
        {
            Dispatcher.Invoke(() =>
            {
                AddLog($"思维链更新: {thinkingChain.Title} - {thinkingChain.StatusDescription}");

                // 更新悬浮窗口
                _floatingTextManager?.UpdateThinkingChain(thinkingChain);
            });
        }

        /// <summary>
        /// 思维链窗口创建事件处理
        /// </summary>
        private void OnThinkingChainWindowCreated(object? sender, NovelManagement.WPF.Controls.ThinkingChain.ThinkingChainWindow window)
        {
            Dispatcher.Invoke(() =>
            {
                AddLog($"思维链窗口已创建: {window.Title}");
                UpdateActiveThinkingChainsText();
            });
        }

        /// <summary>
        /// 思维链窗口关闭事件处理
        /// </summary>
        private void OnThinkingChainWindowClosed(object? sender, string windowId)
        {
            Dispatcher.Invoke(() =>
            {
                AddLog($"思维链窗口已关闭: {windowId}");
                UpdateActiveThinkingChainsText();
            });
        }

        #endregion

        #region 配置和UI辅助方法

        /// <summary>
        /// 从UI更新配置
        /// </summary>
        private void UpdateConfigurationFromUI()
        {
            // 暂时注释掉，因为UI结构已更改
            // TODO: 根据新的UI结构重新实现
            /*
            // 暂时注释掉，UI结构已更改
            /*
            _currentConfiguration.ApiKey = ApiKeyTextBox.Text;
            _currentConfiguration.BaseUrl = BaseUrlTextBox.Text;
            _currentConfiguration.DefaultModel = (DefaultModelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "deepseek-chat";
            _currentConfiguration.DefaultTemperature = TemperatureSlider.Value;
            _currentConfiguration.DefaultMaxTokens = int.TryParse(MaxTokensTextBox.Text, out var maxTokens) ? maxTokens : 4000;
            _currentConfiguration.EnableThinkingChain = EnableThinkingChainCheckBox.IsChecked ?? true;
            _currentConfiguration.EnableStreaming = EnableStreamingCheckBox.IsChecked ?? true;
            */

            // 更新悬浮文本管理器设置
            /*
            if (_floatingTextManager != null)
            {
                _floatingTextManager.Opacity = OpacitySlider.Value;
                _floatingTextManager.MaxActiveWindows = int.TryParse(MaxWindowsTextBox.Text, out var maxWindows) ? maxWindows : 3;

                var selectedPosition = (DefaultPositionComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                if (Enum.TryParse<FloatingTextPosition>(selectedPosition, out var position))
                {
                    _floatingTextManager.DefaultPosition = position;
                }
            }
            */
        }

        /// <summary>
        /// 更新UI配置
        /// </summary>
        private void UpdateUIFromConfiguration()
        {
            // 暂时注释掉，UI结构已更改
            /*
            ApiKeyTextBox.Text = _currentConfiguration.ApiKey;
            BaseUrlTextBox.Text = _currentConfiguration.BaseUrl;
            TemperatureSlider.Value = _currentConfiguration.DefaultTemperature;
            MaxTokensTextBox.Text = _currentConfiguration.DefaultMaxTokens.ToString();
            EnableThinkingChainCheckBox.IsChecked = _currentConfiguration.EnableThinkingChain;
            EnableStreamingCheckBox.IsChecked = _currentConfiguration.EnableStreaming;

            // 设置默认模型
            foreach (ComboBoxItem item in DefaultModelComboBox.Items)
            {
                if (item.Content?.ToString() == _currentConfiguration.DefaultModel)
                {
                    DefaultModelComboBox.SelectedItem = item;
                    break;
                }
            }
            */
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        private async void LoadConfigurationAsync()
        {
            try
            {
                if (_configurationService != null)
                {
                    // 加载DeepSeek配置
                    _currentConfiguration = await _configurationService.LoadDeepSeekConfigurationAsync();

                    // 加载UI配置
                    _uiConfiguration = await _configurationService.LoadUIConfigurationAsync();

                    // 更新UI
                    UpdateUIFromConfiguration();

                    AddLog("配置已加载");
                }
                else
                {
                    AddLog("配置服务未初始化");
                }
            }
            catch (Exception ex)
            {
                AddLog($"加载配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        private async void SaveConfigurationAsync()
        {
            try
            {
                if (_configurationService != null)
                {
                    // 保存DeepSeek配置
                    var deepSeekSaved = await _configurationService.SaveDeepSeekConfigurationAsync(_currentConfiguration);

                    // 保存UI配置
                    var uiSaved = await _configurationService.SaveUIConfigurationAsync(_uiConfiguration);

                    if (deepSeekSaved && uiSaved)
                    {
                        AddLog("配置已保存到本地");
                    }
                    else
                    {
                        AddLog("部分配置保存失败");
                    }
                }
                else
                {
                    AddLog("配置服务未初始化");
                }
            }
            catch (Exception ex)
            {
                AddLog($"保存配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新活动思维链数量显示
        /// </summary>
        private void UpdateActiveThinkingChainsText()
        {
            var count = _floatingTextManager?.ActiveWindowCount ?? 0;
            ActiveThinkingChainsText.Text = $"({count} 个活动窗口)";
        }

        /// <summary>
        /// 重新初始化AI服务
        /// </summary>
        private async Task ReinitializeAIServicesAsync()
        {
            try
            {
                AddLog("开始重新初始化AI服务...");

                // 获取AI助手服务并重新初始化
                var serviceProvider = App.ServiceProvider;
                if (serviceProvider != null)
                {
                    var aiAssistantService = serviceProvider.GetService<AIAssistantService>();
                    if (aiAssistantService != null)
                    {
                        // 触发AI助手服务的重新初始化
                        var initMethod = aiAssistantService.GetType().GetMethod("InitializeServiceAsync",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (initMethod != null)
                        {
                            await (Task)initMethod.Invoke(aiAssistantService, null);
                            AddLog("✅ AI助手服务重新初始化完成");
                        }
                    }

                    // 重新检查服务状态
                    var statusChecker = serviceProvider.GetService<AIServiceStatusChecker>();
                    if (statusChecker != null)
                    {
                        var status = await statusChecker.CheckServiceStatusAsync();
                        AddLog($"AI服务状态: {status.GetStatusDescription()}");
                    }
                }

                AddLog("AI服务重新初始化完成");
            }
            catch (Exception ex)
            {
                AddLog($"❌ 重新初始化AI服务失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示Snackbar消息
        /// </summary>
        private void ShowSnackbar(string message, PackIconKind icon, Brush color)
        {
            // 这里可以实现Snackbar显示逻辑
            // 暂时使用MessageBox代替
            MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region 统计监控事件

        /// <summary>
        /// 统计时间范围选择变化事件
        /// </summary>
        private void StatisticsTimeRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                RefreshStatisticsDisplay();
            }
            catch (Exception ex)
            {
                AddLog($"更新统计显示失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 刷新统计按钮点击事件
        /// </summary>
        private void RefreshStatistics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshStatisticsDisplay();
                AddLog("统计数据已刷新");
            }
            catch (Exception ex)
            {
                AddLog($"刷新统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 导出统计按钮点击事件
        /// </summary>
        private void ExportStatistics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_statisticsService == null)
                {
                    MessageBox.Show("统计服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var timeRange = GetSelectedTimeRange();
                var csvData = _statisticsService.ExportStatistics(timeRange);

                if (!string.IsNullOrEmpty(csvData))
                {
                    var saveDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "CSV文件|*.csv",
                        FileName = $"AI使用统计_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        System.IO.File.WriteAllText(saveDialog.FileName, csvData, System.Text.Encoding.UTF8);
                        MessageBox.Show($"统计数据已导出到: {saveDialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        AddLog($"统计数据已导出: {saveDialog.FileName}");
                    }
                }
                else
                {
                    MessageBox.Show("没有可导出的统计数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                AddLog($"导出统计失败: {ex.Message}");
                MessageBox.Show($"导出统计失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 刷新统计显示
        /// </summary>
        private void RefreshStatisticsDisplay()
        {
            try
            {
                if (_statisticsService == null) return;

                var timeRange = GetSelectedTimeRange();
                var statistics = _statisticsService.GetUsageStatistics(timeRange);
                var functionRanking = _statisticsService.GetFunctionUsageRanking(timeRange, 10);

                // 更新基础统计
                TotalRequestsText.Text = statistics.TotalRequests.ToString();
                SuccessRateText.Text = $"{statistics.SuccessRate:F1}%";
                AverageTimeText.Text = $"{statistics.AverageExecutionTime.TotalSeconds:F2}s";
                TokenUsageText.Text = statistics.TotalTokensUsed.ToString();

                // 更新功能使用排行
                FunctionRankingListView.ItemsSource = functionRanking;

                // 更新最近错误
                RecentErrorsListView.ItemsSource = statistics.RecentErrors;
            }
            catch (Exception ex)
            {
                AddLog($"刷新统计显示失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取选中的时间范围
        /// </summary>
        /// <returns>时间范围</returns>
        private TimeRange GetSelectedTimeRange()
        {
            var selectedItem = StatisticsTimeRangeComboBox.SelectedItem as ComboBoxItem;
            var tag = selectedItem?.Tag?.ToString();

            return tag switch
            {
                "Today" => TimeRange.Today,
                "Yesterday" => TimeRange.Yesterday,
                "LastWeek" => TimeRange.LastWeek,
                "LastMonth" => TimeRange.LastMonth,
                _ => TimeRange.Today
            };
        }

        #endregion

        #region 新增的AI模型管理事件处理

        /// <summary>
        /// 提供者选择变更
        /// </summary>
        private async void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProviderComboBox.SelectedItem is ComboBoxItem item)
            {
                var newProvider = item.Tag?.ToString() ?? "Ollama";

                // 如果提供者没有变化，跳过
                if (newProvider == _currentProvider)
                    return;

                _currentProvider = newProvider;
                AddLog($"正在切换到提供者: {_currentProvider}");

                try
                {
                    // 切换提供者时使用缓存（如果有的话）
                    await RefreshModelsAsync(forceRefresh: false);
                    await CheckConnectionStatusAsync();
                    AddLog($"已成功切换到提供者: {_currentProvider}");
                }
                catch (Exception ex)
                {
                    AddLog($"切换提供者失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 打开配置窗口
        /// </summary>
        private void OpenConfigWindow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var configWindow = new Window
                {
                    Title = "AI配置管理",
                    Width = 1200,
                    Height = 800,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = new AIConfigurationView()
                };
                configWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                AddLog($"打开配置窗口失败: {ex.Message}");
                MessageBox.Show($"打开配置窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 刷新连接状态和模型列表
        /// </summary>
        private async void RefreshConnection_Click(object sender, RoutedEventArgs e)
        {
            if (RefreshConnectionButton != null)
                RefreshConnectionButton.IsEnabled = false;

            try
            {
                AddLog("开始刷新连接状态和模型列表...");

                // 强制刷新模型列表（忽略缓存）
                await RefreshModelsAsync(forceRefresh: true);

                // 刷新连接状态
                await CheckConnectionStatusAsync();

                AddLog("刷新完成");
            }
            catch (Exception ex)
            {
                AddLog($"刷新失败: {ex.Message}");
                MessageBox.Show($"刷新失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (RefreshConnectionButton != null)
                    RefreshConnectionButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// 刷新模型列表
        /// </summary>
        private async void RefreshModels_Click(object sender, RoutedEventArgs e)
        {
            if (RefreshModelsButton != null)
                RefreshModelsButton.IsEnabled = false;

            try
            {
                AddLog("开始刷新模型列表...");

                // 强制刷新模型列表（忽略缓存）
                await RefreshModelsAsync(forceRefresh: true);

                AddLog("模型列表刷新完成");
            }
            catch (Exception ex)
            {
                AddLog($"刷新模型列表失败: {ex.Message}");
                MessageBox.Show($"刷新模型列表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (RefreshModelsButton != null)
                    RefreshModelsButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// 发送测试
        /// </summary>
        private async void SendTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var prompt = TestPromptTextBox?.Text?.Trim();
                if (string.IsNullOrEmpty(prompt))
                {
                    MessageBox.Show("请输入测试提示词", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SendTestButton != null)
                    SendTestButton.IsEnabled = false;

                if (TestResponseTextBox != null)
                    TestResponseTextBox.Text = "正在处理...";

                // 检查必要的服务是否已初始化
                if (_modelManager == null)
                {
                    if (TestResponseTextBox != null)
                        TestResponseTextBox.Text = "错误: 模型管理器未初始化";
                    AddLog("错误: 模型管理器未初始化");
                    return;
                }

                var request = new ChatRequest
                {
                    Model = ModelComboBox?.SelectedItem is ComboBoxItem modelItem ? modelItem.Tag?.ToString() ?? "" : "",
                    Messages = new List<ChatMessage>
                    {
                        new ChatMessage { Role = "user", Content = prompt }
                    },
                    Temperature = 0.7,
                    MaxTokens = 1000,
                    Stream = StreamingTestCheckBox?.IsChecked == true
                };

                if (StreamingTestCheckBox?.IsChecked == true)
                {
                    if (TestResponseTextBox != null)
                        TestResponseTextBox.Text = "";

                    var response = await _modelManager.ChatStreamAsync(_currentProvider, request, chunk =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (TestResponseTextBox != null)
                                TestResponseTextBox.Text += chunk.Content;
                        });
                    });

                    if (!response.IsSuccess && TestResponseTextBox != null)
                    {
                        TestResponseTextBox.Text = $"错误: {response.ErrorMessage}";
                    }
                }
                else
                {
                    var response = await _modelManager.ChatAsync(_currentProvider, request);
                    if (TestResponseTextBox != null)
                        TestResponseTextBox.Text = response.IsSuccess ? response.Content : $"错误: {response.ErrorMessage}";
                }

                AddLog($"测试完成 - 提供者: {_currentProvider}");
            }
            catch (Exception ex)
            {
                if (TestResponseTextBox != null)
                    TestResponseTextBox.Text = $"测试失败: {ex.Message}";
                AddLog($"测试失败: {ex.Message}");
                MessageBox.Show($"测试失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (SendTestButton != null)
                    SendTestButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// 加载模型列表（支持缓存）
        /// </summary>
        private async Task LoadModelsAsync()
        {
            await RefreshModelsAsync(forceRefresh: false);
        }

        /// <summary>
        /// 刷新模型列表
        /// </summary>
        /// <param name="forceRefresh">是否强制刷新（忽略缓存）</param>
        private async Task RefreshModelsAsync(bool forceRefresh = false)
        {
            // 防止重复刷新
            if (_isRefreshingModels)
            {
                await Dispatcher.InvokeAsync(() => AddLog("模型列表正在刷新中，跳过重复请求"));
                return;
            }

            try
            {
                _isRefreshingModels = true;

                // 安全检查UI控件是否已初始化（在UI线程上检查）
                bool uiReady = false;
                await Dispatcher.InvokeAsync(() =>
                {
                    uiReady = ModelComboBox != null;
                    if (!uiReady)
                    {
                        AddLog("模型选择框未初始化，跳过加载模型列表");
                    }
                });

                if (!uiReady) return;

                // 更新UI状态指示
                await Dispatcher.InvokeAsync(() =>
                {
                    if (RefreshModelsButton != null)
                        RefreshModelsButton.IsEnabled = false;

                    // 在模型下拉框中显示加载状态
                    if (ModelComboBox.Items.Count == 0 || forceRefresh)
                    {
                        ModelComboBox.Items.Clear();
                        ModelComboBox.Items.Add(new ComboBoxItem
                        {
                            Content = "正在加载模型列表...",
                            IsEnabled = false
                        });
                        ModelComboBox.SelectedIndex = 0;
                    }
                });

                // 检查缓存是否有效
                var cacheKey = _currentProvider;
                var now = DateTime.Now;
                var cacheValid = !forceRefresh &&
                                _modelCache.ContainsKey(cacheKey) &&
                                (now - _lastModelRefresh) < _modelCacheTimeout;

                List<ModelInfo> models;

                if (cacheValid)
                {
                    await Dispatcher.InvokeAsync(() => AddLog($"使用缓存的{_currentProvider}模型列表"));
                    models = _modelCache[cacheKey];
                }
                else
                {
                    await Dispatcher.InvokeAsync(() => AddLog($"正在获取{_currentProvider}模型列表..."));

                    // 在后台线程获取模型数据
                    models = await Task.Run(async () => await GetModelsFromProviderAsync());

                    // 更新缓存
                    _modelCache[cacheKey] = models;
                    _lastModelRefresh = now;

                    await Dispatcher.InvokeAsync(() => AddLog($"获取到{models.Count}个{_currentProvider}模型"));
                }

                // 在UI线程中更新界面
                await Dispatcher.InvokeAsync(() =>
                {
                    UpdateModelComboBox(models);
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => AddLog($"刷新模型列表失败: {ex.Message}"));

                // 在UI线程中显示错误状态
                await Dispatcher.InvokeAsync(() =>
                {
                    if (ModelComboBox != null)
                    {
                        ModelComboBox.Items.Clear();
                        ModelComboBox.Items.Add(new ComboBoxItem
                        {
                            Content = $"加载失败: {ex.Message}",
                            IsEnabled = false
                        });
                        ModelComboBox.SelectedIndex = 0;
                    }
                });

                // 只在强制刷新时显示错误对话框，避免自动刷新时打扰用户
                if (forceRefresh)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"刷新模型列表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
            finally
            {
                _isRefreshingModels = false;

                // 恢复UI状态
                await Dispatcher.InvokeAsync(() =>
                {
                    if (RefreshModelsButton != null)
                        RefreshModelsButton.IsEnabled = true;
                });
            }
        }

        /// <summary>
        /// 从提供者获取模型列表
        /// </summary>
        private async Task<List<ModelInfo>> GetModelsFromProviderAsync()
        {
            var models = new List<ModelInfo>();

            if (_currentProvider == "Ollama" && _ollamaApiService != null)
            {
                models = await _ollamaApiService.GetAvailableModelsAsync();
            }
            else if (_currentProvider == "DeepSeek")
            {
                // DeepSeek的固定模型列表
                var deepSeekModels = new[] { "deepseek-chat", "deepseek-coder", "deepseek-reasoner" };
                models = deepSeekModels.Select(model => new ModelInfo
                {
                    Id = model,
                    Name = model,
                    Description = $"DeepSeek {model} 模型",
                    Size = 0,
                    IsDownloaded = true,
                    Capabilities = new List<string> { "chat", "completion" }
                }).ToList();
            }

            return models;
        }

        /// <summary>
        /// 更新模型下拉框
        /// </summary>
        private void UpdateModelComboBox(List<ModelInfo> models)
        {
            if (ModelComboBox == null) return;

            var selectedModel = ModelComboBox.SelectedItem as ComboBoxItem;
            var selectedModelId = selectedModel?.Tag?.ToString();

            ModelComboBox.Items.Clear();

            foreach (var model in models)
            {
                var displayText = _currentProvider == "Ollama"
                    ? $"{model.Id} ({FormatBytes(model.Size)})"
                    : model.Id;

                var item = new ComboBoxItem
                {
                    Content = displayText,
                    Tag = model.Id,
                    ToolTip = $"模型: {model.Name}\n描述: {model.Description}\n大小: {FormatBytes(model.Size)}"
                };

                ModelComboBox.Items.Add(item);
            }

            // 恢复之前选择的模型，或设置默认模型
            var defaultModel = selectedModelId ?? GetDefaultModelForProvider();
            foreach (ComboBoxItem item in ModelComboBox.Items)
            {
                if (item.Tag?.ToString() == defaultModel)
                {
                    ModelComboBox.SelectedItem = item;
                    break;
                }
            }

            // 如果没有找到匹配的模型，选择第一个
            if (ModelComboBox.SelectedItem == null && ModelComboBox.Items.Count > 0)
            {
                ModelComboBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 获取提供者的默认模型
        /// </summary>
        private string GetDefaultModelForProvider()
        {
            return _currentProvider switch
            {
                "Ollama" => _configuration?["AI:Providers:Ollama:DefaultModel"] ?? "qwq:latest",
                "DeepSeek" => _configuration?["AI:Providers:DeepSeek:DefaultModel"] ?? "deepseek-chat",
                _ => ""
            };
        }

        /// <summary>
        /// 检查Ollama服务状态
        /// </summary>
        private async Task CheckOllamaServiceAsync()
        {
            try
            {
                if (_ollamaApiService != null)
                {
                    AddLog("正在检查Ollama服务状态...");

                    // 尝试重新初始化Ollama服务
                    var config = new NovelManagement.AI.Services.Ollama.Models.OllamaConfiguration
                    {
                        BaseUrl = "http://localhost:11434",
                        TimeoutSeconds = 30
                    };

                    var initResult = await _ollamaApiService.InitializeAsync(config);
                    if (initResult)
                    {
                        AddLog("✅ Ollama服务初始化成功");

                        // 测试连接
                        var testResult = await _ollamaApiService.TestConnectionAsync();
                        if (testResult.IsSuccess)
                        {
                            AddLog($"✅ Ollama连接测试成功 (响应时间: {testResult.ResponseTime.TotalMilliseconds:F0}ms)");

                            // 刷新模型列表
                            if (_currentProvider == "Ollama")
                            {
                                await RefreshModelsAsync();
                            }
                        }
                        else
                        {
                            AddLog($"❌ Ollama连接测试失败: {testResult.ErrorMessage}");
                        }
                    }
                    else
                    {
                        AddLog("❌ Ollama服务初始化失败");
                    }
                }
                else
                {
                    AddLog("❌ Ollama服务未注册");
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ 检查Ollama服务状态异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新连接状态显示
        /// </summary>
        private void UpdateConnectionStatus()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await CheckConnectionStatusAsync();
                }
                catch (Exception ex)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        AddLog($"更新连接状态失败: {ex.Message}");
                    });
                }
            });
        }

        /// <summary>
        /// 检查连接状态
        /// </summary>
        private async Task CheckConnectionStatusAsync()
        {
            try
            {
                // 安全检查UI控件是否已初始化（在UI线程上检查）
                bool uiReady = false;
                await Dispatcher.InvokeAsync(() =>
                {
                    uiReady = ConnectionStatusIcon != null && ConnectionStatusText != null;
                    if (!uiReady)
                    {
                        AddLog("连接状态控件未初始化，跳过检查连接状态");
                    }
                    else
                    {
                        ConnectionStatusIcon.Foreground = Brushes.Orange;
                        ConnectionStatusText.Text = "检查中...";
                    }
                });

                if (!uiReady) return;

                if (_currentProvider == "Ollama" && _ollamaApiService != null)
                {
                    // 在后台线程执行网络请求
                    var testResult = await Task.Run(async () => await _ollamaApiService.TestConnectionAsync());

                    // 在UI线程更新界面
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (testResult.IsSuccess)
                        {
                            ConnectionStatusIcon.Foreground = Brushes.Green;
                            var version = testResult.ServerInfo.TryGetValue("Version", out var ver) ? ver.ToString() : "Unknown";
                            ConnectionStatusText.Text = $"Ollama在线 (v{version})";
                        }
                        else
                        {
                            ConnectionStatusIcon.Foreground = Brushes.Red;
                            ConnectionStatusText.Text = "Ollama离线";
                        }
                    });
                }
                else if (_currentProvider == "DeepSeek")
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        ConnectionStatusIcon.Foreground = Brushes.Gray;
                        ConnectionStatusText.Text = "需要配置API密钥";
                    });
                }
                else
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        ConnectionStatusIcon.Foreground = Brushes.Gray;
                        ConnectionStatusText.Text = "未实现";
                    });
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (ConnectionStatusIcon != null && ConnectionStatusText != null)
                    {
                        ConnectionStatusIcon.Foreground = Brushes.Red;
                        ConnectionStatusText.Text = "连接失败";
                    }
                    AddLog($"检查连接状态失败: {ex.Message}");
                });
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

        #endregion
    }
}
