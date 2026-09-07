using Microsoft.Extensions.Logging.Abstractions;
using NovelManagement.Application.DTOs;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.Tests.Fakes;
using Xunit;

namespace NovelManagement.Tests;

/// <summary>
/// 存量角色修为等级回填逻辑单元测试
/// 决策核心 SuggestLevelOrderIndex 为纯函数，与《数据牢笼》人工回填结果（12 阶）对齐：
/// 林默(主角)→11、凯斯(对手)→10、老周(师父)→7、苏晴(女主)→4
/// </summary>
public class CultivationLevelBackfillTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();

    private static CultivationLevelBackfillService CreateService(
        FakeUnitOfWork uow, FakeCultivationSystemService cultivation)
        => new(uow, cultivation, NullLogger<CultivationLevelBackfillService>.Instance);

    /// <summary>预置「灵脉共鸣体系」12 级（与生产库一致）</summary>
    private static FakeCultivationSystemService CreateSystemWith12Levels()
    {
        var cultivation = new FakeCultivationSystemService();
        var levelNames = new[]
        {
            "共鸣初启", "共鸣稳固", "共鸣深化", "共鸣精进", "共鸣突破", "共鸣转型",
            "灵脉觉醒", "神魂共鸣", "法则感知", "法则掌握", "法则领悟", "法则化身"
        };
        cultivation.CreateAsync(new CreateCultivationSystemDto
        {
            Name = "灵脉共鸣体系", Type = "灵能共鸣型", ProjectId = ProjectId
        }).GetAwaiter().GetResult();
        var created = cultivation.InternalSystem!;
        created.Levels = levelNames.Select((n, i) => new CultivationLevelDto
        {
            Name = n, OrderIndex = i + 1, CultivationSystemId = created.Id
        }).ToList();
        return cultivation;
    }

    private static Character CreateCharacter(string name, string type, int importance, string? cultivationLevel = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Importance = importance,
            CultivationLevel = cultivationLevel,
            ProjectId = ProjectId
        };

    #region SuggestLevelOrderIndex（纯决策函数）

    [Theory]
    [InlineData("主角", 11)]
    [InlineData("主角之一", 11)]
    [InlineData("对手", 10)]
    [InlineData("对手之一", 10)]
    [InlineData("反派", 10)]
    [InlineData("师父", 7)]
    [InlineData("师父之一", 7)]
    [InlineData("导师", 7)]
    [InlineData("女主角之一", 4)]
    public void Suggest_PlotRoleTypes_MatchManualAssignment(string type, int expected)
    {
        // 12 阶下与人工回填《数据牢笼》四角色结果一致
        Assert.Equal(expected, CultivationLevelBackfillService.SuggestLevelOrderIndex(type, 5, 12));
    }

    [Theory]
    [InlineData(1, 2)]   // ceil(0.1*12)=2
    [InlineData(5, 6)]   // ceil(0.5*12)=6
    [InlineData(10, 12)] // 重要度最高 → 顶阶
    [InlineData(99, 12)] // 超界 clamp 到 10
    [InlineData(0, 2)]   // 下界 clamp 到 1
    public void Suggest_FallbackByImportance_LinearMapping(int importance, int expected)
    {
        Assert.Equal(expected, CultivationLevelBackfillService.SuggestLevelOrderIndex("配角", importance, 12));
    }

    [Fact]
    public void Suggest_ProtagonistBeatsOtherKeywords()
    {
        // 优先级：主角 > 师父（主角兼师父按主角规则）
        Assert.Equal(11, CultivationLevelBackfillService.SuggestLevelOrderIndex("主角兼师父", 10, 12));
    }

    [Fact]
    public void Suggest_ClampsToValidRange()
    {
        // 1 阶体系下主角规则算出 0，必须 clamp 到 1
        Assert.Equal(1, CultivationLevelBackfillService.SuggestLevelOrderIndex("主角", 10, 1));
        // 2 阶体系下对手规则算出 0，clamp 到 1
        Assert.Equal(1, CultivationLevelBackfillService.SuggestLevelOrderIndex("对手", 5, 2));
    }

    [Fact]
    public void Suggest_NullOrEmptyType_UsesImportanceFallback()
    {
        Assert.Equal(6, CultivationLevelBackfillService.SuggestLevelOrderIndex(null, 5, 12));
        Assert.Equal(6, CultivationLevelBackfillService.SuggestLevelOrderIndex("", 5, 12));
    }

    [Fact]
    public void Suggest_ZeroLevels_ReturnsZero()
    {
        Assert.Equal(0, CultivationLevelBackfillService.SuggestLevelOrderIndex("主角", 10, 0));
    }

    #endregion

    #region BackfillAsync（端到端回填链路）

    [Fact]
    public async Task Backfill_AssignsLevelsByPlotRole_AndPersists()
    {
        var uow = new FakeUnitOfWork();
        uow.CharacterRepo.Characters.Add(CreateCharacter("林默", "主角之一", 10));
        uow.CharacterRepo.Characters.Add(CreateCharacter("凯斯", "对手之一", 5));
        uow.CharacterRepo.Characters.Add(CreateCharacter("老周", "师父之一", 5));
        uow.CharacterRepo.Characters.Add(CreateCharacter("苏晴", "女主角之一", 5));
        var service = CreateService(uow, CreateSystemWith12Levels());

        var result = await service.BackfillAsync(ProjectId);

        // 四个角色按定位分配，与人工回填结果一致
        Assert.Equal(4, result.Count);
        Assert.Contains(result, a => a.CharacterName == "林默" && a.LevelName == "法则领悟" && a.OrderIndex == 11);
        Assert.Contains(result, a => a.CharacterName == "凯斯" && a.LevelName == "法则掌握" && a.OrderIndex == 10);
        Assert.Contains(result, a => a.CharacterName == "老周" && a.LevelName == "灵脉觉醒" && a.OrderIndex == 7);
        Assert.Contains(result, a => a.CharacterName == "苏晴" && a.LevelName == "共鸣精进" && a.OrderIndex == 4);
        // 落库：4 次实体更新 + 1 次 SaveChanges
        Assert.Equal(4, uow.CharacterRepo.Updated.Count);
        Assert.Equal(1, uow.SaveChangesCount);
        Assert.All(uow.CharacterRepo.Updated, c => Assert.False(string.IsNullOrWhiteSpace(c.CultivationLevel)));
    }

    [Fact]
    public async Task Backfill_SkipsCharactersWithExistingLevel()
    {
        var uow = new FakeUnitOfWork();
        uow.CharacterRepo.Characters.Add(CreateCharacter("已有修为者", "主角之一", 10, "法则化身"));
        uow.CharacterRepo.Characters.Add(CreateCharacter("空修为者", "配角", 3));
        var service = CreateService(uow, CreateSystemWith12Levels());

        var result = await service.BackfillAsync(ProjectId);

        // 只回填空修为角色，已有修为绝不覆盖
        Assert.Single(result);
        Assert.Equal("空修为者", result[0].CharacterName);
        Assert.Single(uow.CharacterRepo.Updated);
        Assert.Equal("法则化身", uow.CharacterRepo.Characters.Single(c => c.Name == "已有修为者").CultivationLevel);
    }

    [Fact]
    public async Task Backfill_NoSystem_ReturnsEmptyAndDoesNotTouchCharacters()
    {
        var uow = new FakeUnitOfWork();
        uow.CharacterRepo.Characters.Add(CreateCharacter("林默", "主角之一", 10));
        var service = CreateService(uow, new FakeCultivationSystemService());

        var result = await service.BackfillAsync(ProjectId);

        Assert.Empty(result);
        Assert.Empty(uow.CharacterRepo.Updated);
        Assert.Equal(0, uow.SaveChangesCount);
    }

    [Fact]
    public async Task Backfill_NoBlankLevelCharacters_SavesNothing()
    {
        var uow = new FakeUnitOfWork();
        uow.CharacterRepo.Characters.Add(CreateCharacter("已有修为者", "主角之一", 10, "法则领悟"));
        var service = CreateService(uow, CreateSystemWith12Levels());

        var result = await service.BackfillAsync(ProjectId);

        Assert.Empty(result);
        Assert.Empty(uow.CharacterRepo.Updated);
        Assert.Equal(0, uow.SaveChangesCount); // 无改动不触发 SaveChanges
    }

    [Fact]
    public async Task Backfill_OtherProjectCharacters_NotTouched()
    {
        var uow = new FakeUnitOfWork();
        var otherProject = CreateCharacter("别的项目角色", "主角之一", 10);
        otherProject.ProjectId = Guid.NewGuid();
        uow.CharacterRepo.Characters.Add(otherProject);
        var service = CreateService(uow, CreateSystemWith12Levels());

        var result = await service.BackfillAsync(ProjectId);

        // 项目隔离：只处理本项目角色
        Assert.Empty(result);
        Assert.Null(otherProject.CultivationLevel);
    }

    #endregion
}
