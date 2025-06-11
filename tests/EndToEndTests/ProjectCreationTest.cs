using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;
using NovelManagement.Infrastructure.Data;
using NovelManagement.Infrastructure.Data.Repositories;

namespace NovelManagement.Tests.EndToEnd;

/// <summary>
/// 项目创建流程端到端测试
/// </summary>
public class ProjectCreationTest
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProjectCreationTest> _logger;

    public ProjectCreationTest()
    {
        // 创建测试服务容器
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<ProjectCreationTest>>();
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
            options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid()));

        // 添加仓储和工作单元
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // 添加应用服务
        services.AddScoped<ProjectService>();
        services.AddScoped<VolumeService>();
        services.AddScoped<ChapterService>();
        services.AddScoped<CharacterService>();
    }

    /// <summary>
    /// 测试完整的项目创建流程
    /// </summary>
    public async Task<bool> TestProjectCreationFlowAsync()
    {
        try
        {
            _logger.LogInformation("开始执行项目创建流程测试");

            // 使用同一个scope确保数据在同一个数据库上下文中
            using var scope = _serviceProvider.CreateScope();
            var projectService = scope.ServiceProvider.GetRequiredService<ProjectService>();
            var volumeService = scope.ServiceProvider.GetRequiredService<VolumeService>();
            var chapterService = scope.ServiceProvider.GetRequiredService<ChapterService>();
            var characterService = scope.ServiceProvider.GetRequiredService<CharacterService>();

            // 1.1.1 项目创建测试
            var project = await TestProjectCreationAsync(projectService);
            if (project == null)
            {
                _logger.LogError("项目创建测试失败");
                return false;
            }

            // 1.2.1 卷宗创建测试
            var volume = await TestVolumeCreationAsync(volumeService, project);
            if (volume == null)
            {
                _logger.LogError("卷宗创建测试失败");
                return false;
            }

            // 1.3.1 章节创建测试
            var chapter = await TestChapterCreationAsync(chapterService, volume);
            if (chapter == null)
            {
                _logger.LogError("章节创建测试失败");
                return false;
            }

            // 1.4.1 角色创建测试
            var character = await TestCharacterCreationAsync(characterService, project);
            if (character == null)
            {
                _logger.LogError("角色创建测试失败");
                return false;
            }

            _logger.LogInformation("项目创建流程测试全部通过！");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "项目创建流程测试发生异常");
            return false;
        }
    }

    /// <summary>
    /// 测试项目创建
    /// </summary>
    private async Task<Project?> TestProjectCreationAsync(ProjectService projectService)
    {
        _logger.LogInformation("开始测试项目创建");

        // 创建测试项目
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "测试小说项目",
            Description = "这是一个用于端到端测试的小说项目",
            Type = "玄幻",
            Status = "规划中",
            Tags = "修仙,玄幻,热血",
            Progress = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 执行创建
        var createdProject = await projectService.CreateProjectAsync(project);

        // 验证创建结果
        if (createdProject == null || createdProject.Id == Guid.Empty)
        {
            _logger.LogError("项目创建失败：返回的项目对象无效");
            return null;
        }

        // 验证项目是否保存到数据库
        var retrievedProject = await projectService.GetProjectByIdAsync(createdProject.Id);
        if (retrievedProject == null)
        {
            _logger.LogError("项目创建失败：无法从数据库中检索到创建的项目");
            return null;
        }

        // 验证项目信息
        if (retrievedProject.Name != project.Name ||
            retrievedProject.Description != project.Description ||
            retrievedProject.Type != project.Type)
        {
            _logger.LogError("项目创建失败：项目信息不匹配");
            return null;
        }

        _logger.LogInformation("项目创建测试通过：项目ID = {ProjectId}", createdProject.Id);
        return createdProject;
    }

    /// <summary>
    /// 测试卷宗创建
    /// </summary>
    private async Task<Volume?> TestVolumeCreationAsync(VolumeService volumeService, Project project)
    {
        _logger.LogInformation("开始测试卷宗创建");

        // 创建测试卷宗
        var volume = new Volume
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "第一卷：初入修仙界",
            Description = "主角初入修仙界的故事",
            Order = 1,
            Status = "规划中",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 执行创建
        var createdVolume = await volumeService.CreateVolumeAsync(volume);

        // 验证创建结果
        if (createdVolume == null || createdVolume.Id == Guid.Empty)
        {
            _logger.LogError("卷宗创建失败：返回的卷宗对象无效");
            return null;
        }

        // 验证卷宗与项目的关联关系
        if (createdVolume.ProjectId != project.Id)
        {
            _logger.LogError("卷宗创建失败：卷宗与项目的关联关系不正确");
            return null;
        }

        _logger.LogInformation("卷宗创建测试通过：卷宗ID = {VolumeId}", createdVolume.Id);
        return createdVolume;
    }

    /// <summary>
    /// 测试章节创建
    /// </summary>
    private async Task<Chapter?> TestChapterCreationAsync(ChapterService chapterService, Volume volume)
    {
        _logger.LogInformation("开始测试章节创建");

        // 创建测试章节
        var chapter = new Chapter
        {
            Id = Guid.NewGuid(),
            VolumeId = volume.Id,
            Title = "第一章：觉醒",
            Content = "这是第一章的内容，主角开始觉醒修仙天赋...",
            Order = 1,
            Status = "草稿",
            WordCount = 3000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 执行创建
        var createdChapter = await chapterService.CreateChapterAsync(chapter);

        // 验证创建结果
        if (createdChapter == null || createdChapter.Id == Guid.Empty)
        {
            _logger.LogError("章节创建失败：返回的章节对象无效");
            return null;
        }

        // 验证章节与卷宗的关联关系
        if (createdChapter.VolumeId != volume.Id)
        {
            _logger.LogError("章节创建失败：章节与卷宗的关联关系不正确");
            return null;
        }

        _logger.LogInformation("章节创建测试通过：章节ID = {ChapterId}", createdChapter.Id);
        return createdChapter;
    }

    /// <summary>
    /// 测试角色创建
    /// </summary>
    private async Task<Character?> TestCharacterCreationAsync(CharacterService characterService, Project project)
    {
        _logger.LogInformation("开始测试角色创建");

        // 创建测试角色
        var character = new Character
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Name = "林天",
            Type = "主角",
            // Character实体没有Description属性，使用Background代替
            Background = "天赋异禀的修仙者，拥有特殊体质",
            Age = 18,
            Gender = "男",
            Appearance = "身材修长，面容俊朗",
            Personality = "坚韧不拔，重情重义",
            // Background已经在上面设置了，这里移除重复的设置
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 执行创建
        var createdCharacter = await characterService.CreateCharacterAsync(character);

        // 验证创建结果
        if (createdCharacter == null || createdCharacter.Id == Guid.Empty)
        {
            _logger.LogError("角色创建失败：返回的角色对象无效");
            return null;
        }

        // 验证角色与项目的关联关系
        if (createdCharacter.ProjectId != project.Id)
        {
            _logger.LogError("角色创建失败：角色与项目的关联关系不正确");
            return null;
        }

        _logger.LogInformation("角色创建测试通过：角色ID = {CharacterId}", createdCharacter.Id);
        return createdCharacter;
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
