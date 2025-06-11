using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.Mappings;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;
using NovelManagement.Infrastructure.Data;
using NovelManagement.Infrastructure.Data.Repositories;

namespace NovelManagement.Tests.EndToEnd;

/// <summary>
/// 内容管理流程端到端测试
/// </summary>
public class ContentManagementTest
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ContentManagementTest> _logger;

    public ContentManagementTest()
    {
        // 创建测试服务容器
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<ContentManagementTest>>();
    }

    /// <summary>
    /// 配置测试服务
    /// </summary>
    private void ConfigureServices(IServiceCollection services)
    {
        // 添加日志
        services.AddLogging(builder => builder.AddConsole());

        // 添加数据库上下文（使用内存数据库进行测试）
        services.AddDbContext<NovelManagementDbContext>(options =>
            options.UseInMemoryDatabase("ContentTestDatabase_" + Guid.NewGuid()));

        // 添加AutoMapper
        services.AddAutoMapper(typeof(WorldSettingMappingProfile));

        // 添加仓储和工作单元
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // 添加应用服务
        services.AddScoped<ProjectService>();
        services.AddScoped<WorldSettingService>();
        services.AddScoped<CultivationSystemService>();
        services.AddScoped<PoliticalSystemService>();
        services.AddScoped<CurrencySystemService>();
        services.AddScoped<FactionService>();
        services.AddScoped<PlotService>();
    }

    /// <summary>
    /// 测试完整的内容管理流程
    /// </summary>
    public async Task<bool> TestContentManagementFlowAsync()
    {
        try
        {
            _logger.LogInformation("开始执行内容管理流程测试");

            // 使用同一个scope确保数据在同一个数据库上下文中
            using var scope = _serviceProvider.CreateScope();
            var projectService = scope.ServiceProvider.GetRequiredService<ProjectService>();
            var worldSettingService = scope.ServiceProvider.GetRequiredService<WorldSettingService>();
            var cultivationSystemService = scope.ServiceProvider.GetRequiredService<CultivationSystemService>();
            var politicalSystemService = scope.ServiceProvider.GetRequiredService<PoliticalSystemService>();
            var currencySystemService = scope.ServiceProvider.GetRequiredService<CurrencySystemService>();
            var factionService = scope.ServiceProvider.GetRequiredService<FactionService>();
            var plotService = scope.ServiceProvider.GetRequiredService<PlotService>();

            // 首先创建一个测试项目
            var project = await CreateTestProjectAsync(projectService);
            if (project == null)
            {
                _logger.LogError("创建测试项目失败");
                return false;
            }

            // 2.1 世界设定测试
            var worldSettingsCreated = await TestWorldSettingsCreationAsync(worldSettingService, project);
            if (!worldSettingsCreated)
            {
                _logger.LogError("世界设定创建测试失败");
                return false;
            }

            // 2.2 势力关系测试
            var factionsCreated = await TestFactionCreationAsync(factionService, project);
            if (!factionsCreated)
            {
                _logger.LogError("势力创建测试失败");
                return false;
            }

            // 2.3 剧情规划测试
            var plotsCreated = await TestPlotCreationAsync(plotService, project);
            if (!plotsCreated)
            {
                _logger.LogError("剧情创建测试失败");
                return false;
            }

            _logger.LogInformation("内容管理流程测试全部通过！");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "内容管理流程测试发生异常");
            return false;
        }
    }

    /// <summary>
    /// 创建测试项目
    /// </summary>
    private async Task<Project?> CreateTestProjectAsync(ProjectService projectService)
    {
        _logger.LogInformation("创建内容管理测试项目");

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "内容管理测试项目",
            Description = "用于测试内容管理功能的项目",
            Type = "玄幻",
            Status = "规划中",
            Tags = "修仙,世界设定,势力",
            Progress = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdProject = await projectService.CreateProjectAsync(project);
        if (createdProject != null)
        {
            _logger.LogInformation("成功创建内容管理测试项目：{ProjectId}", createdProject.Id);
        }

        return createdProject;
    }

    /// <summary>
    /// 测试世界设定创建
    /// </summary>
    private async Task<bool> TestWorldSettingsCreationAsync(WorldSettingService worldSettingService, Project project)
    {
        _logger.LogInformation("开始测试世界设定创建");

        try
        {
            // 创建修炼体系设定
            var cultivationSetting = new WorldSetting
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Name = "九重天修炼体系",
                Type = "修炼体系",
                Category = "核心设定",
                Content = "分为九个境界：练气、筑基、金丹、元婴、化神、炼虚、合体、大乘、渡劫",
                Importance = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdCultivationSetting = await worldSettingService.CreateAsync(
                new NovelManagement.Application.DTOs.CreateWorldSettingDto
                {
                    ProjectId = project.Id,
                    Name = cultivationSetting.Name,
                    Type = cultivationSetting.Type,
                    Category = cultivationSetting.Category,
                    Content = cultivationSetting.Content,
                    Importance = cultivationSetting.Importance
                });

            if (createdCultivationSetting == null)
            {
                _logger.LogError("修炼体系设定创建失败");
                return false;
            }

            // 创建政治体系设定
            var politicalSetting = new WorldSetting
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Name = "修仙界政治体系",
                Type = "政治体系",
                Category = "社会设定",
                Content = "以宗门为主导，分为一流宗门、二流宗门、三流宗门，以及散修联盟",
                Importance = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdPoliticalSetting = await worldSettingService.CreateAsync(
                new NovelManagement.Application.DTOs.CreateWorldSettingDto
                {
                    ProjectId = project.Id,
                    Name = politicalSetting.Name,
                    Type = politicalSetting.Type,
                    Category = politicalSetting.Category,
                    Content = politicalSetting.Content,
                    Importance = politicalSetting.Importance
                });

            if (createdPoliticalSetting == null)
            {
                _logger.LogError("政治体系设定创建失败");
                return false;
            }

            // 创建货币体系设定
            var currencySetting = new WorldSetting
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Name = "修仙界货币体系",
                Type = "货币体系",
                Category = "经济设定",
                Content = "以灵石为主要货币：下品灵石、中品灵石、上品灵石、极品灵石",
                Importance = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdCurrencySetting = await worldSettingService.CreateAsync(
                new NovelManagement.Application.DTOs.CreateWorldSettingDto
                {
                    ProjectId = project.Id,
                    Name = currencySetting.Name,
                    Type = currencySetting.Type,
                    Category = currencySetting.Category,
                    Content = currencySetting.Content,
                    Importance = currencySetting.Importance
                });

            if (createdCurrencySetting == null)
            {
                _logger.LogError("货币体系设定创建失败");
                return false;
            }

            // 验证设定间的关联关系
            var allSettings = await worldSettingService.GetAllAsync(project.Id);
            if (allSettings.Count() < 3)
            {
                _logger.LogError("世界设定创建数量不足");
                return false;
            }

            _logger.LogInformation("世界设定创建测试通过：创建了 {Count} 个设定", allSettings.Count());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "世界设定创建测试发生异常");
            return false;
        }
    }

    /// <summary>
    /// 测试势力创建和关系管理
    /// </summary>
    private async Task<bool> TestFactionCreationAsync(FactionService factionService, Project project)
    {
        _logger.LogInformation("开始测试势力创建和关系管理");

        try
        {
            // 创建宗门势力
            var sect = new Faction
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Name = "天剑宗",
                Type = "宗门",
                Description = "修仙界一流宗门，以剑道闻名",
                Influence = 90,
                Territory = "天剑山脉",
                // Leader = "剑尊", // Faction实体没有Leader属性，使用LeaderId
                MemberCount = 5000,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdSect = await factionService.CreateFactionAsync(sect);
            if (createdSect == null)
            {
                _logger.LogError("宗门势力创建失败");
                return false;
            }

            // 创建家族势力
            var family = new Faction
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Name = "林家",
                Type = "家族",
                Description = "修仙世家，传承千年",
                Influence = 60,
                Territory = "青云城",
                // Leader = "林家主", // Faction实体没有Leader属性，使用LeaderId
                MemberCount = 200,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdFamily = await factionService.CreateFactionAsync(family);
            if (createdFamily == null)
            {
                _logger.LogError("家族势力创建失败");
                return false;
            }

            // 创建国家势力
            var kingdom = new Faction
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Name = "大周王朝",
                Type = "国家",
                Description = "凡人王朝，统治广阔疆域",
                Influence = 70,
                Territory = "大周疆域",
                // Leader = "周皇", // Faction实体没有Leader属性，使用LeaderId
                MemberCount = 100000000,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdKingdom = await factionService.CreateFactionAsync(kingdom);
            if (createdKingdom == null)
            {
                _logger.LogError("国家势力创建失败");
                return false;
            }

            // 验证势力创建结果
            var projectFactions = await factionService.GetFactionsByProjectIdAsync(project.Id);
            var factionList = projectFactions.ToList();

            if (factionList.Count < 3)
            {
                _logger.LogError("势力创建数量不足");
                return false;
            }

            _logger.LogInformation("势力创建测试通过：创建了 {Count} 个势力", factionList.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "势力创建测试发生异常");
            return false;
        }
    }

    /// <summary>
    /// 测试剧情创建
    /// </summary>
    private async Task<bool> TestPlotCreationAsync(PlotService plotService, Project project)
    {
        _logger.LogInformation("开始测试剧情创建");

        try
        {
            // 创建主线剧情
            var mainPlot = new Plot
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Title = "主角修仙之路",
                Type = "主线",
                Description = "主角从凡人开始，一步步踏上修仙之路的故事",
                Status = "规划中",
                Priority = "高", // Priority是string类型，不是int
                // StartTime = DateTime.UtcNow, // Plot实体没有StartTime属性
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdMainPlot = await plotService.CreatePlotAsync(mainPlot);
            if (createdMainPlot == null)
            {
                _logger.LogError("主线剧情创建失败");
                return false;
            }

            // 创建支线剧情
            var sidePlot = new Plot
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Title = "林家秘密",
                Type = "支线",
                Description = "主角发现林家隐藏的秘密，涉及千年前的恩怨",
                Status = "规划中",
                Priority = "中", // Priority是string类型，不是int
                // StartTime = DateTime.UtcNow.AddDays(30), // Plot实体没有StartTime属性
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdSidePlot = await plotService.CreatePlotAsync(sidePlot);
            if (createdSidePlot == null)
            {
                _logger.LogError("支线剧情创建失败");
                return false;
            }

            // 验证剧情创建结果
            var projectPlots = await plotService.GetPlotsByProjectIdAsync(project.Id);
            var plotList = projectPlots.ToList();

            if (plotList.Count < 2)
            {
                _logger.LogError("剧情创建数量不足");
                return false;
            }

            _logger.LogInformation("剧情创建测试通过：创建了 {Count} 个剧情", plotList.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "剧情创建测试发生异常");
            return false;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
