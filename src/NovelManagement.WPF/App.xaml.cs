using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Markup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Serilog;
using NovelManagement.WPF.Views;
using NovelManagement.WPF.Services;
using NovelManagement.Infrastructure.Data;
using NovelManagement.Infrastructure.Repositories;
using NovelManagement.Application.Services;
using NovelManagement.Application.Interfaces;
using NovelManagement.Core.Interfaces;
using NovelManagement.AI.Agents;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Memory;
using NovelManagement.AI.Workflow;
using NovelManagement.AI.Services;
using NovelManagement.AI.Services.Ollama;
using NovelManagement.AI.Services.DeepSeek;
using NovelManagement.AI.Services.ThinkingChain;

namespace NovelManagement.WPF;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : System.Windows.Application
{
    private const string ApplicationDirectoryName = "NovelManagement";
    private const string DefaultConfigurationResourceName = "NovelManagement.WPF.appsettings.json";
    private static bool _languageMetadataApplied;
    private static readonly string AppDataRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ApplicationDirectoryName);
    private static readonly string ConfigDirectory = Path.Combine(AppDataRoot, "config");
    private static readonly string DataDirectory = Path.Combine(AppDataRoot, "data");
    private static readonly string LogsDirectory = Path.Combine(AppDataRoot, "logs");
    private static readonly string BackupsDirectory = Path.Combine(AppDataRoot, "backups");
    private static readonly string UserConfigurationPath = Path.Combine(ConfigDirectory, "appsettings.user.json");
    private static readonly string DefaultDatabasePath = Path.Combine(DataDirectory, "NovelManagement.db");

    private IHost? _host;

    /// <summary>
    /// 全局服务提供者
    /// </summary>
    public static IServiceProvider? ServiceProvider { get; private set; }

    /// <summary>
    /// 应用程序启动时的处理
    /// </summary>
    /// <param name="e">启动事件参数</param>
    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            EnsureUtf8ConsoleEncoding();
            ApplyApplicationCulture();
            EnsureApplicationDirectories();
            RegisterGlobalExceptionHandlers();

            // 配置日志
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(Path.Combine(LogsDirectory, "app-.txt"),
                    rollingInterval: RollingInterval.Day,
                    encoding: System.Text.Encoding.UTF8,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("应用程序启动中...");

            // 构建配置
            var configuration = BuildConfiguration();

            var isAiConfigurationStandaloneMode = IsAiConfigurationStandaloneMode(e.Args);

            // 构建主机和依赖注入容器
            _host = CreateHostBuilder(configuration, isAiConfigurationStandaloneMode).Build();

            // 设置全局服务提供者
            ServiceProvider = _host.Services;

            if (isAiConfigurationStandaloneMode)
            {
                Log.Information("以 AI 模型配置独立测试模式启动");
                var aiConfigurationWindow = new AIConfigurationHostWindow();
                aiConfigurationWindow.Show();
                base.OnStartup(e);
                return;
            }

            // 记录启动配置校验结果
            LogStartupValidationResults();

            // 首次启动引导
            await ShowFirstRunOnboardingIfNeededAsync();

            // 确保数据库已创建
            EnsureDatabaseCreated();

            // 仅在显式开启时写入示例数据，避免生产环境污染正式数据
            if (ShouldSeedSampleDataOnStartup(configuration))
            {
                try
                {
                    await EnsureBasicDataCreatedAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "基础数据初始化失败");
                }
            }

            // 获取主窗口并显示
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            Log.Information("应用程序启动成功");
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "应用程序启动失败");
            MessageBox.Show($"应用程序启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    /// <summary>
    /// 应用程序退出时的处理
    /// </summary>
    /// <param name="e">退出事件参数</param>
    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            UnregisterGlobalExceptionHandlers();
            _host?.Dispose();
            Log.Information("应用程序已退出");
            Log.CloseAndFlush();
        }
        finally
        {
            base.OnExit(e);
        }
    }

    /// <summary>
    /// 构建配置
    /// </summary>
    /// <returns>配置对象</returns>
    private static IConfiguration BuildConfiguration()
    {
        EnsureApplicationDirectories();

        var providerEnvironmentOverrides = BuildProviderEnvironmentOverrides();
        var baseConfigurationBuilder = new ConfigurationBuilder();
        var runtimeConfigurationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        if (File.Exists(runtimeConfigurationPath))
        {
            baseConfigurationBuilder
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        }
        else
        {
            var embeddedConfigurationJson = GetEmbeddedDefaultConfigurationJson();
            var embeddedConfigurationBytes = Encoding.UTF8.GetBytes(embeddedConfigurationJson);
            baseConfigurationBuilder.AddJsonStream(new MemoryStream(embeddedConfigurationBytes));
        }

        var baseConfiguration = baseConfigurationBuilder
            .AddJsonFile(UserConfigurationPath, optional: true, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: "NOVELMANAGEMENT_")
            .AddInMemoryCollection(providerEnvironmentOverrides)
            .Build();

        var normalizedConnectionString = NormalizeConnectionString(baseConfiguration.GetConnectionString("DefaultConnection"));

        return new ConfigurationBuilder()
            .AddConfiguration(baseConfiguration)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = normalizedConnectionString,
                ["Paths:AppDataRoot"] = AppDataRoot,
                ["Paths:ConfigDirectory"] = ConfigDirectory,
                ["Paths:DataDirectory"] = DataDirectory,
                ["Paths:LogsDirectory"] = LogsDirectory,
                ["Paths:BackupsDirectory"] = BackupsDirectory,
                ["Paths:UserConfigurationPath"] = UserConfigurationPath
            })
            .Build();
    }

    private static string GetEmbeddedDefaultConfigurationJson()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(DefaultConfigurationResourceName);

        if (stream == null)
        {
            throw new FileNotFoundException(
                $"未找到内嵌默认配置资源: {DefaultConfigurationResourceName}");
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static IDictionary<string, string?> BuildProviderEnvironmentOverrides()
    {
        var overrides = new Dictionary<string, string?>();

        AddEnvironmentOverride(overrides, "AI:Providers:DeepSeek:ApiKey", "DeepSeek_API_KEY", "DEEPSEEK_API_KEY");
        AddEnvironmentOverride(overrides, "AI:Providers:ZhipuAI:ApiKey", "ZhiPu_API_KEY", "ZHIPU_API_KEY", "ZAI_API_KEY");
        AddEnvironmentOverride(overrides, "AI:Providers:XiaoMiMiMo:ApiKey", "XiaoMiMiMo_API_KEY", "XIAOMIMIMO_API_KEY", "MIMO_API_KEY");

        return overrides;
    }

    private static void AddEnvironmentOverride(IDictionary<string, string?> overrides, string configKey, params string[] environmentVariableNames)
    {
        foreach (var variableName in environmentVariableNames)
        {
            var value = Environment.GetEnvironmentVariable(variableName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                overrides[configKey] = value.Trim();
                return;
            }
        }
    }

    /// <summary>
    /// 创建主机构建器
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <param name="isAiConfigurationStandaloneMode">是否以 AI 配置独立模式启动</param>
    /// <returns>主机构建器</returns>
    private static IHostBuilder CreateHostBuilder(IConfiguration configuration, bool isAiConfigurationStandaloneMode)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, builder) =>
            {
                builder.Sources.Clear();
                builder.AddConfiguration(configuration);
            })
            .ConfigureServices((context, services) =>
            {
                // 注册配置
                services.AddSingleton(configuration);

                // 注册数据库上下文
                services.AddDbContext<NovelManagementDbContext>(options =>
                {
                    var connectionString = configuration.GetConnectionString("DefaultConnection");
                    options.UseSqlite(connectionString);

                    if (configuration.GetValue<bool>("Diagnostics:EnableSensitiveDataLogging"))
                    {
                        options.EnableSensitiveDataLogging();
                    }

                    if (configuration.GetValue("Diagnostics:EnableDetailedErrors", true))
                    {
                        options.EnableDetailedErrors();
                    }
                });

                // 注册仓储和工作单元
                services.AddScoped<IUnitOfWork, UnitOfWork>();

                // 注册应用服务
                RegisterApplicationServices(services);

                // 注册AI服务
                RegisterAIServices(services, isAiConfigurationStandaloneMode);

                // 注册WPF服务
                services.AddSingleton<ProjectContextService>();
                services.AddSingleton<CurrentProjectGuard>();
                services.AddSingleton<NavigationService>();
                services.AddSingleton<ChapterContentSyncNotificationService>();
                services.AddSingleton<ProjectCatalogService>();
                services.AddSingleton<DatabaseMaintenanceService>();
                services.AddSingleton<StartupConfigurationValidationService>();
                services.AddSingleton<AIConnectivityCheckService>();
                services.AddSingleton<ProductionHealthCheckService>();
                services.AddSingleton<DiagnosticBundleService>();
            services.AddScoped<JudicialDataService>();
            services.AddScoped<MapDataService>();
            services.AddScoped<DimensionDataService>();
            services.AddScoped<BusinessDataService>();
            services.AddScoped<PetDataService>();
            services.AddScoped<EquipmentDataService>();
            services.AddScoped<ProfessionDataService>();
            services.AddScoped<PopulationDataService>();
            services.AddScoped<TreasureDataService>();
            services.AddScoped<TechniqueDataService>();
            services.AddScoped<TimelineDataService>();
            services.AddScoped<ProjectReadModelService>();
            services.AddScoped<ProjectStatisticsService>();
                services.AddSingleton<AIServiceStatusChecker>();
                services.AddTransient<PrerequisiteGenerationService>();
                services.AddTransient<ProjectContextAssembler>();

                // 注册界面
                services.AddTransient<MainWindow>();
                services.AddTransient<OperationsManagementWindow>();
                services.AddTransient<FirstRunOnboardingWindow>();
            })
            .UseSerilog();
    }

    private static void EnsureApplicationDirectories()
    {
        Directory.CreateDirectory(AppDataRoot);
        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(BackupsDirectory);
    }

    private static string NormalizeConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return $"Data Source={DefaultDatabasePath}";
        }

        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);

        if (string.IsNullOrWhiteSpace(builder.DataSource))
        {
            builder.DataSource = DefaultDatabasePath;
        }
        else if (!Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.Combine(DataDirectory, builder.DataSource);
        }

        return builder.ToString();
    }

    private static bool ShouldSeedSampleDataOnStartup(IConfiguration configuration)
    {
        return configuration.GetValue<bool>("Startup:SeedSampleDataOnStartup");
    }

    private static bool IsAiConfigurationStandaloneMode(string[] args)
    {
        return args.Any(arg => string.Equals(arg, "--ai-config-only", StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureUtf8ConsoleEncoding()
    {
        try
        {
            Console.InputEncoding = new UTF8Encoding(false);
            Console.OutputEncoding = new UTF8Encoding(false);
        }
        catch
        {
            // 忽略无控制台窗口等场景下的编码初始化异常
        }
    }

    private static void ApplyApplicationCulture()
    {
        var systemCulture = CultureInfo.InstalledUICulture;
        var applicationCulture = ResolveApplicationCulture(systemCulture);

        CultureInfo.DefaultThreadCurrentCulture = applicationCulture;
        CultureInfo.DefaultThreadCurrentUICulture = applicationCulture;
        Thread.CurrentThread.CurrentCulture = applicationCulture;
        Thread.CurrentThread.CurrentUICulture = applicationCulture;

        if (_languageMetadataApplied)
        {
            return;
        }

        var language = XmlLanguage.GetLanguage(applicationCulture.IetfLanguageTag);
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(language));
        _languageMetadataApplied = true;
    }

    private static CultureInfo ResolveApplicationCulture(CultureInfo systemCulture)
    {
        if (systemCulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return CultureInfo.GetCultureInfo("zh-CN");
        }

        return systemCulture;
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void LogStartupValidationResults()
    {
        try
        {
            using var scope = _host!.Services.CreateScope();
            var validationService = scope.ServiceProvider.GetRequiredService<StartupConfigurationValidationService>();
            var validationItems = validationService.Validate();

            foreach (var item in validationItems)
            {
                switch (item.Severity)
                {
                    case ValidationSeverity.Error:
                        Log.Error("启动配置校验失败 - {Name}: {Message}", item.Name, item.Message);
                        break;
                    case ValidationSeverity.Warning:
                        Log.Warning("启动配置校验警告 - {Name}: {Message}", item.Name, item.Message);
                        break;
                    default:
                        Log.Information("启动配置校验 - {Name}: {Message}", item.Name, item.Message);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "执行启动配置校验时出现异常");
        }
    }

    private async Task ShowFirstRunOnboardingIfNeededAsync()
    {
        try
        {
            using var scope = _host!.Services.CreateScope();
            var configurationService = scope.ServiceProvider.GetRequiredService<ConfigurationService>();
            var runtimeState = await configurationService.LoadAppStateAsync();

            if (runtimeState.FirstRunCompleted)
            {
                return;
            }

            var onboardingWindow = scope.ServiceProvider.GetRequiredService<FirstRunOnboardingWindow>();
            onboardingWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "显示首次启动向导失败，继续常规启动流程");
        }
    }

    private void UnregisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "捕获到未处理的UI线程异常");
        MessageBox.Show(
            "程序遇到未处理错误，详细信息已写入日志目录，请重试或联系技术支持。",
            "应用程序错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Log.Fatal(exception, "捕获到未处理的应用程序域异常");
        }
        else
        {
            Log.Fatal("捕获到未处理的应用程序域异常，异常对象不是 Exception 类型");
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "捕获到未观察的任务异常");
        e.SetObserved();
    }

    /// <summary>
    /// 注册应用服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void RegisterApplicationServices(IServiceCollection services)
    {
        // 注册具体的服务类（暂时不使用接口，直接注册实现类）
        services.AddScoped<ProjectService>();
        services.AddScoped<VolumeService>();
        services.AddScoped<ChapterService>();
        services.AddScoped<ChapterContentSyncService>();
        services.AddScoped<CharacterService>();
        services.AddScoped<CharacterRelationshipService>();
        services.AddScoped<FactionService>();
        services.AddScoped<PlotService>();
        services.AddScoped<ResourceService>();
        services.AddScoped<RaceService>();
        services.AddScoped<SecretRealmService>();
        services.AddScoped<RelationshipNetworkService>();
        services.AddScoped<CultivationSystemService>();
        services.AddScoped<PoliticalSystemService>();
        services.AddScoped<CurrencySystemService>();
        services.AddScoped<WorldSettingService>();
        services.AddScoped<IWorldSettingService>(provider => provider.GetRequiredService<WorldSettingService>());
        services.AddScoped<ExcelProcessingService>();
        services.AddScoped<WordProcessingService>();
        services.AddScoped<ExportService>();
        services.AddScoped<ImportService>();
    }

    /// <summary>
    /// 注册AI服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="isAiConfigurationStandaloneMode">是否以 AI 配置独立模式启动</param>
    private static void RegisterAIServices(IServiceCollection services, bool isAiConfigurationStandaloneMode)
    {
        try
        {
            // 注册HTTP客户端
            services.AddHttpClient();

            // 注册记忆管理
            services.AddSingleton<ICompressionEngine, CompressionEngine>();
            services.AddSingleton<IMemoryManager, MemoryManager>();

            // 注册AI API服务
            services.AddSingleton<IOllamaApiService, OllamaApiService>();
            services.AddSingleton<IDeepSeekApiService, DeepSeekApiService>();
            services.AddSingleton<IThinkingChainProcessor, ThinkingChainProcessor>();

            // 注册 RWKV 推理服务
            services.AddSingleton<NovelManagement.AI.Services.RWKV.IRwkvLightningService>(serviceProvider =>
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<NovelManagement.AI.Services.RWKV.RwkvLightningService>();
                var httpClient = new System.Net.Http.HttpClient();
                var service = new NovelManagement.AI.Services.RWKV.RwkvLightningService(logger, httpClient);

                // 异步初始化
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var rwkvConfig = new NovelManagement.AI.Services.RWKV.Models.RwkvConfiguration();
                configuration.GetSection("AI:Providers:RWKV").Bind(rwkvConfig);

                if (isAiConfigurationStandaloneMode && rwkvConfig.AutoStartServer)
                {
                    rwkvConfig.AutoStartServer = false;
                    logger.LogInformation("AI 配置独立模式已禁用 RWKV 自动启动");
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        logger.LogInformation("开始初始化 RWKV 推理服务...");
                        var success = await service.InitializeAsync(rwkvConfig);
                        if (success)
                            logger.LogInformation("RWKV 推理服务初始化成功");
                        else
                            logger.LogWarning("RWKV 推理服务不可用，续写将降级为决策类AI模型");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "初始化 RWKV 推理服务异常");
                    }
                });

                return service;
            });

            // 注册 OpenAI 兼容提供者（智谱/Ollama/自定义）
            services.AddSingleton<NovelManagement.AI.Services.OpenAICompatible.OpenAICompatibleProvider>(serviceProvider =>
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<NovelManagement.AI.Services.OpenAICompatible.OpenAICompatibleProvider>();
                var httpClient = new System.Net.Http.HttpClient();
                return new NovelManagement.AI.Services.OpenAICompatible.OpenAICompatibleProvider(logger, httpClient);
            });

            // 注册模型管理器
            services.AddSingleton<ModelManager>(serviceProvider =>
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<ModelManager>();
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var modelManager = new ModelManager(logger);

                // 注册Ollama提供者
                var ollamaService = serviceProvider.GetService<IOllamaApiService>();
                if (ollamaService != null)
                {
                    // 初始化Ollama配置
                    var ollamaConfig = new NovelManagement.AI.Services.Ollama.Models.OllamaConfiguration();
                    configuration.GetSection("AI:Providers:Ollama").Bind(ollamaConfig);

                    // 先注册提供者
                    modelManager.RegisterProvider(ollamaService);

                    // 异步初始化（不等待，避免阻塞启动）
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            logger.LogInformation("开始初始化Ollama服务...");
                            var success = await ollamaService.InitializeAsync(ollamaConfig);
                            if (success)
                            {
                                logger.LogInformation("Ollama服务初始化成功");
                            }
                            else
                            {
                                logger.LogWarning("Ollama服务初始化失败，可能是Ollama服务器未启动");
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "初始化Ollama服务异常");
                        }
                    });
                }

                // TODO: 注册DeepSeek提供者（需要实现IModelProvider接口）
                // var deepSeekService = serviceProvider.GetService<IDeepSeekApiService>();
                // if (deepSeekService != null)
                // {
                //     modelManager.RegisterProvider(deepSeekService);
                // }

                // 注册 OpenAI 兼容提供者（智谱AI / OpenAI / 自定义 OpenAI 端点）
                void RegisterOpenAiCompatibleProvider(string providerKey, string displayName)
                {
                    var providerConfig = new NovelManagement.AI.Services.OpenAICompatible.Models.OpenAICompatibleConfiguration();
                    var providerSection = configuration.GetSection($"AI:Providers:{providerKey}");
                    providerSection.Bind(providerConfig);
                    providerConfig.ProviderName = providerKey;
                    providerConfig.ProviderKind = providerKey;

                    if (string.Equals(providerKey, "DeepSeek", StringComparison.OrdinalIgnoreCase))
                    {
                        providerConfig.DefaultModel = providerSection["DefaultModel"]
                            ?? providerSection["Model"]
                            ?? providerConfig.DefaultModel;
                    }
                    else if (string.Equals(providerKey, "RWKV", StringComparison.OrdinalIgnoreCase))
                    {
                        providerConfig.BaseUrl = $"{(providerSection["BaseUrl"] ?? "http://localhost:8000").TrimEnd('/')}/openai/v1";
                        providerConfig.DefaultModel = providerSection["DefaultModel"]
                            ?? providerSection["ModelName"]
                            ?? Path.GetFileNameWithoutExtension(providerSection["ModelPath"] ?? string.Empty)
                            ?? "rwkv7";
                    }
                    else if (string.Equals(providerKey, "LlamaCpp", StringComparison.OrdinalIgnoreCase))
                    {
                        providerConfig.DefaultModel = providerSection["DefaultModel"]
                            ?? Path.GetFileNameWithoutExtension(providerSection["ModelPath"] ?? string.Empty)
                            ?? "local-gguf";
                    }

                    if (!providerConfig.IsValid())
                    {
                        return;
                    }

                    var providerLogger = loggerFactory.CreateLogger<NovelManagement.AI.Services.OpenAICompatible.OpenAICompatibleProvider>();
                    var providerInstance = new NovelManagement.AI.Services.OpenAICompatible.OpenAICompatibleProvider(
                        providerLogger,
                        new System.Net.Http.HttpClient(),
                        providerKey);

                    modelManager.RegisterProvider(providerInstance);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            logger.LogInformation("开始初始化{ProviderName}服务...", displayName);
                            var success = await providerInstance.InitializeAsync(providerConfig);
                            if (success)
                                logger.LogInformation("{ProviderName}服务初始化成功", displayName);
                            else
                                logger.LogWarning("{ProviderName}服务初始化失败", displayName);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "初始化{ProviderName}服务异常", displayName);
                        }
                    });
                }

                RegisterOpenAiCompatibleProvider("ZhipuAI", "智谱AI");
                RegisterOpenAiCompatibleProvider("OpenAI", "OpenAI");
                RegisterOpenAiCompatibleProvider("DeepSeek", "DeepSeek");
                RegisterOpenAiCompatibleProvider("XiaoMiMiMo", "小米米模");
                RegisterOpenAiCompatibleProvider("LlamaCpp", "llama.cpp");
                RegisterOpenAiCompatibleProvider("RWKV", "RWKV");

                // 按配置设置默认提供者；若配置项未注册，则回退到当前可解析的可用提供者。
                var configuredDefaultProvider = configuration["AI:DefaultProvider"];
                if (!string.IsNullOrWhiteSpace(configuredDefaultProvider))
                {
                    if (!modelManager.SetDefaultProvider(configuredDefaultProvider))
                    {
                        var fallbackProvider = modelManager.ResolvePreferredProviderName(configuredDefaultProvider);
                        if (!string.IsNullOrWhiteSpace(fallbackProvider))
                        {
                            logger.LogWarning("配置的默认提供者 {ProviderName} 不可用，回退到 {FallbackProvider}", configuredDefaultProvider, fallbackProvider);
                            modelManager.SetDefaultProvider(fallbackProvider);
                        }
                    }
                }

                return modelManager;
            });

            // 注册AI Agent（直接注册实现类）
            services.AddScoped<DirectorAgent>();
            services.AddScoped<WriterAgent>();
            services.AddScoped<EditorAgent>();
            services.AddScoped<CriticAgent>();
            services.AddScoped<ResearcherAgent>();
            services.AddScoped<SummarizerAgent>();
            services.AddScoped<ReaderAgent>();
            services.AddScoped<SettingManagerAgent>();

            // 注册Agent工厂
            services.AddSingleton<NovelManagement.AI.Extensions.IAgentFactory, NovelManagement.AI.Extensions.AgentFactory>();

            // 注册工作流引擎
            services.AddScoped<NovelWorkflowEngine>();
            services.AddScoped<TaskQueue>();

            // 注册WPF服务
            services.AddSingleton<ConfigurationService>();
            services.AddSingleton<AIUsageStatisticsService>();
            services.AddSingleton<FloatingTextManager>();
            services.AddSingleton<InferenceRuntimeCoordinator>();
            services.AddSingleton<ProjectArchiveService>();
            services.AddScoped<IAIAgentRoleWorkflowService, AIAgentRoleWorkflowService>();

            // 确保AI助手服务正确注册
            services.AddScoped<AIAssistantService>();
            services.AddScoped<IAIAssistantService>(provider => provider.GetRequiredService<AIAssistantService>());


        }
        catch (Exception ex)
        {
            Log.Warning(ex, "注册AI服务时出现警告，某些服务可能不可用");
        }
    }

    /// <summary>
    /// 确保数据库已创建并应用迁移
    /// </summary>
    private void EnsureDatabaseCreated()
    {
        try
        {
            using var scope = _host!.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NovelManagementDbContext>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var databaseMaintenanceService = scope.ServiceProvider.GetRequiredService<DatabaseMaintenanceService>();
            var connectionString = dbContext.Database.GetConnectionString();
            var databaseFilePath = databaseMaintenanceService.GetDatabaseFilePath();
            var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();

            if (configuration.GetValue("Backup:EnableCleanupOnStartup", true))
            {
                databaseMaintenanceService.CleanupExpiredBackups();
            }

            if (pendingMigrations.Any() &&
                configuration.GetValue("Startup:BackupBeforeMigration", true) &&
                File.Exists(databaseFilePath))
            {
                var backupResult = databaseMaintenanceService.CreateBackup("pre_migration");
                if (!backupResult.Success)
                {
                    Log.Warning("数据库迁移前备份未成功: {ErrorMessage}", backupResult.ErrorMessage);
                }
            }

            Log.Information(
                "正在应用数据库迁移，连接: {ConnectionString}，待处理迁移数: {PendingMigrationCount}",
                connectionString,
                pendingMigrations.Count);
            dbContext.Database.Migrate();
            Log.Information("数据库迁移应用完成");

            // 验证数据库连接
            if (dbContext.Database.CanConnect())
            {
                Log.Information("数据库连接验证成功");
            }
            else
            {
                Log.Warning("数据库连接验证失败");
            }

            Log.Information("数据库初始化完成");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "数据库初始化失败");
            throw;
        }
    }

    /// <summary>
    /// 确保基础数据已创建
    /// </summary>
    private async Task EnsureBasicDataCreatedAsync()
    {
        try
        {
            using var scope = _host!.Services.CreateScope();
            var factionService = scope.ServiceProvider.GetRequiredService<FactionService>();

            Log.Information("正在检查基础数据状态...");

            // 检查是否有势力数据
            var projectId = Guid.Parse("12345678-1234-1234-1234-123456789012"); // 默认项目ID
            var existingFactions = await factionService.GetFactionsByProjectIdAsync(projectId);

            if (!existingFactions.Any())
            {
                Log.Information("未发现势力数据，正在创建示例势力...");
                await CreateSampleFactionsAsync(factionService, projectId);
                Log.Information("示例势力创建完成");
            }
            else
            {
                Log.Information($"发现 {existingFactions.Count()} 个现有势力，跳过创建");
            }

            Log.Information("基础数据检查完成");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "基础数据初始化失败，将在运行时创建");
        }
    }

    /// <summary>
    /// 创建示例势力数据
    /// </summary>
    private async Task CreateSampleFactionsAsync(FactionService factionService, Guid projectId)
    {
        var sampleFactions = new List<NovelManagement.Core.Entities.Faction>
        {
            new NovelManagement.Core.Entities.Faction
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = "玄天宗",
                Type = "宗门",
                PowerLevel = 95,
                Description = "修仙界第一大宗门，拥有悠久历史和强大实力",
                Territory = "玄天山脉",
                MemberCount = 50000,
                Status = "Active",
                PowerRating = 95,
                Influence = 90,
                Importance = 95,
                Tags = "正道,修仙,济世救民"
            },
            new NovelManagement.Core.Entities.Faction
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = "血魔宗",
                Type = "宗门",
                PowerLevel = 75,
                Description = "邪恶的血魔宗，以血祭修炼为主",
                Territory = "血魔谷",
                MemberCount = 15000,
                Status = "Active",
                PowerRating = 75,
                Influence = 60,
                Importance = 70,
                Tags = "邪道,血祭,弱肉强食"
            },
            new NovelManagement.Core.Entities.Faction
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = "苏家",
                Type = "家族",
                PowerLevel = 55,
                Description = "修仙世家，以炼丹闻名",
                Territory = "苏家庄园",
                MemberCount = 800,
                Status = "Active",
                PowerRating = 55,
                Influence = 50,
                Importance = 60,
                Tags = "家族,炼丹,济世"
            },
            new NovelManagement.Core.Entities.Faction
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = "天机阁",
                Type = "组织",
                PowerLevel = 80,
                Description = "神秘的情报组织，掌握天下秘密",
                Territory = "各大城市",
                MemberCount = 5000,
                Status = "Active",
                PowerRating = 80,
                Influence = 85,
                Importance = 75,
                Tags = "情报,中立,神秘"
            },
            new NovelManagement.Core.Entities.Faction
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = "大燕王朝",
                Type = "国家",
                PowerLevel = 100,
                Description = "修仙界最强大的王朝",
                Territory = "大燕疆域",
                MemberCount = 100000000,
                Status = "Active",
                PowerRating = 100,
                Influence = 100,
                Importance = 100,
                Tags = "王朝,统治,强大"
            },
            new NovelManagement.Core.Entities.Faction
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = "万宝商会",
                Type = "商会",
                PowerLevel = 70,
                Description = "修仙界最大的商业组织",
                Territory = "各大商城",
                MemberCount = 20000,
                Status = "Active",
                PowerRating = 70,
                Influence = 80,
                Importance = 65,
                Tags = "商业,财富,贸易"
            }
        };

        foreach (var faction in sampleFactions)
        {
            await factionService.CreateFactionAsync(faction);
        }
    }
}
