using Microsoft.Extensions.Logging;
using NovelManagement.Application.Interfaces;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Application.Services;

/// <summary>
/// 存量角色修为等级回填服务：为修为为空的角色按剧情定位分配项目修炼体系的等级
/// </summary>
public class CultivationLevelBackfillService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICultivationSystemService _cultivationSystemService;
    private readonly ILogger<CultivationLevelBackfillService> _logger;

    public CultivationLevelBackfillService(
        IUnitOfWork unitOfWork,
        ICultivationSystemService cultivationSystemService,
        ILogger<CultivationLevelBackfillService> logger)
    {
        _unitOfWork = unitOfWork;
        _cultivationSystemService = cultivationSystemService;
        _logger = logger;
    }

    /// <summary>单条分配明细</summary>
    /// <param name="CharacterName">角色名</param>
    /// <param name="LevelName">分配的等级名</param>
    /// <param name="OrderIndex">阶位（1-based，从低到高）</param>
    /// <param name="Rule">命中分配规则的说明</param>
    public record LevelAssignment(string CharacterName, string LevelName, int OrderIndex, string Rule);

    /// <summary>
    /// 回填项目内修为为空的角色（已有修为的角色绝不覆盖）。
    /// 返回本次分配明细；项目无体系、体系无等级或无空修为角色时返回空列表。
    /// </summary>
    public async Task<IReadOnlyList<LevelAssignment>> BackfillAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var systems = await _cultivationSystemService.GetAllAsync(projectId, cancellationToken);
        var first = systems.FirstOrDefault();
        if (first == null)
        {
            _logger.LogWarning("项目 {ProjectId} 无修炼体系，跳过修为回填", projectId);
            return Array.Empty<LevelAssignment>();
        }

        var withLevels = await _cultivationSystemService.GetWithLevelsAsync(first.Id, cancellationToken);
        var levels = (withLevels?.Levels ?? first.Levels)
            .OrderBy(l => l.OrderIndex)
            .ToList();
        if (levels.Count == 0)
        {
            _logger.LogWarning("项目 {ProjectId} 修炼体系 {System} 无等级定义，跳过修为回填", projectId, first.Name);
            return Array.Empty<LevelAssignment>();
        }

        var characters = await _unitOfWork.Characters.GetByProjectIdAsync(projectId, cancellationToken);
        var assignments = new List<LevelAssignment>();
        foreach (var character in characters.Where(c => string.IsNullOrWhiteSpace(c.CultivationLevel)))
        {
            var orderIndex = SuggestLevelOrderIndex(character.Type, character.Importance, levels.Count);
            character.CultivationLevel = levels[orderIndex - 1].Name;
            await _unitOfWork.Characters.UpdateAsync(character, cancellationToken);
            assignments.Add(new LevelAssignment(character.Name, character.CultivationLevel, orderIndex, DescribeRule(character.Type)));
        }

        if (assignments.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("项目 {ProjectId} 回填 {Count} 名角色修为等级（体系：{System}）",
            projectId, assignments.Count, first.Name);
        return assignments;
    }

    /// <summary>
    /// 按剧情定位建议等级阶位（1-based，从低到高）。
    /// 规则优先级：女主（「女主角」含「主角」子串须先判）&gt; 主角（留顶阶突破空间）&gt; 对手/反派（紧咬主角）&gt; 师父/导师（深不可测）&gt; 按重要度线性兜底。
    /// </summary>
    public static int SuggestLevelOrderIndex(string? roleType, int importance, int levelCount)
    {
        if (levelCount <= 0)
        {
            return 0;
        }

        var type = roleType ?? string.Empty;
        int index;
        if (type.Contains("女主"))
        {
            // 「女主角」含「主角」子串，必须先于主角判断
            index = (int)Math.Round(levelCount * 0.33);
        }
        else if (type.Contains("主角"))
        {
            index = levelCount - 1;
        }
        else if (type.Contains("对手") || type.Contains("反派"))
        {
            index = levelCount - 2;
        }
        else if (type.Contains("师父") || type.Contains("导师"))
        {
            index = (int)Math.Round(levelCount * 0.58);
        }
        else
        {
            index = (int)Math.Ceiling(Math.Clamp(importance, 1, 10) / 10.0 * levelCount);
        }

        return Math.Clamp(index, 1, levelCount);
    }

    private static string DescribeRule(string? roleType)
    {
        var type = roleType ?? string.Empty;
        if (type.Contains("女主")) return "女主定位（成长期）";
        if (type.Contains("主角")) return "主角定位（倒数第二阶，留突破空间）";
        if (type.Contains("对手") || type.Contains("反派")) return "对手定位（紧咬主角）";
        if (type.Contains("师父") || type.Contains("导师")) return "师父定位（中高阶）";
        return "按重要度兜底分配";
    }
}
