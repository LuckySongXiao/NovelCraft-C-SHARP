using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
using NovelManagement.AI.Services.Zhipu;
using NovelManagement.AI.Services.ThinkingChain;

namespace NovelManagement.WPF;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : System.Windows.Application
{
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
            // 配置日志
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("logs/app-.txt",
                    rollingInterval: RollingInterval.Day,
                    encoding: System.Text.Encoding.UTF8,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("应用程序启动中...");

            // 构建配置
            var configuration = BuildConfiguration();

            // 构建主机和依赖注入容器
            _host = CreateHostBuilder(configuration).Build();

            // 设置全局服务提供者
            ServiceProvider = _host.Services;

            // 确保数据库已创建
            EnsureDatabaseCreated();

            // 确保基础数据已创建
            try
            {
                await EnsureBasicDataCreatedAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "基础数据初始化失败");
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
        return new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    /// <summary>
    /// 创建主机构建器
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <returns>主机构建器</returns>
    private static IHostBuilder CreateHostBuilder(IConfiguration configuration)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // 注册配置
                services.AddSingleton(configuration);

                // 注册数据库上下文
                services.AddDbContext<NovelManagementDbContext>(options =>
                {
                    var connectionString = configuration.GetConnectionString("DefaultConnection");
                    options.UseSqlite(connectionString);
                    options.EnableSensitiveDataLogging(); // 启用敏感数据日志记录
                    options.EnableDetailedErrors(); // 启用详细错误信息
                });

                // 注册仓储和工作单元
                services.AddScoped<IUnitOfWork, UnitOfWork>();

                // 注册应用服务
                RegisterApplicationServices(services);

                // 注册AI服务
                RegisterAIServices(services);

                // 注册WPF服务
                services.AddSingleton<ProjectContextService>();
                services.AddSingleton<AIServiceStatusChecker>();
                services.AddTransient<PrerequisiteGenerationService>();

                // 注册界面
                services.AddTransient<MainWindow>();
            })
            .UseSerilog();
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
        services.AddScoped<CharacterService>();
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
    }

    /// <summary>
    /// 注册AI服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void RegisterAIServices(IServiceCollection services)
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
            services.AddSingleton<IZhipuApiService, ZhipuApiService>();
            services.AddSingleton<IThinkingChainProcessor, ThinkingChainProcessor>();

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

                // 注册Zhipu提供者
                var zhipuService = serviceProvider.GetService<IZhipuApiService>();
                if (zhipuService != null)
                {
                    modelManager.RegisterProvider(zhipuService);
                }

                // TODO: 注册DeepSeek提供者（需要实现IModelProvider接口）
                // var deepSeekService = serviceProvider.GetService<IDeepSeekApiService>();
                // if (deepSeekService != null)
                // {
                //     modelManager.RegisterProvider(deepSeekService);
                // }

                // 设置默认提供者
                var defaultProvider = configuration["AI:DefaultProvider"] ?? "Zhipu";
                modelManager.SetDefaultProvider(defaultProvider);

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

            Log.Information("正在检查数据库状态...");

            // 检查是否有待处理的迁移
            var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();
            if (pendingMigrations.Any())
            {
                Log.Information($"发现 {pendingMigrations.Count} 个待处理的迁移，正在应用...");
                try
                {
                    dbContext.Database.Migrate();
                    Log.Information("数据库迁移应用完成");
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChangesWarning"))
                {
                    Log.Warning("检测到模型变更警告，使用EnsureCreated方法");
                    dbContext.Database.EnsureCreated();
                }
            }
            else
            {
                Log.Information("数据库已是最新状态，无需迁移");
                // 确保数据库已创建
                dbContext.Database.EnsureCreated();
            }

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
