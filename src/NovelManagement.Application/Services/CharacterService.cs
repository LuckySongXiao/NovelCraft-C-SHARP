using Microsoft.Extensions.Logging;
using NovelManagement.Application.DTOs;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Application.Services;

/// <summary>
/// 人物管理服务
/// </summary>
public class CharacterService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CharacterService> _logger;

    public CharacterService(IUnitOfWork unitOfWork, ILogger<CharacterService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    public async Task<Character> CreateCharacterAsync(Character character, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始创建角色: {CharacterName}, 项目ID: {ProjectId}", character.Name, character.ProjectId);

            // 处理势力关系 - 从Notes字段中提取临时势力信息
            string factionName = null;
            if (!string.IsNullOrEmpty(character.Notes))
            {
                var notes = character.Notes;
                var factionMarkerStart = notes.IndexOf("[TEMP_FACTION:");
                if (factionMarkerStart >= 0)
                {
                    var factionMarkerEnd = notes.IndexOf("]", factionMarkerStart);
                    if (factionMarkerEnd > factionMarkerStart)
                    {
                        var factionMarker = notes.Substring(factionMarkerStart, factionMarkerEnd - factionMarkerStart + 1);
                        factionName = factionMarker.Substring("[TEMP_FACTION:".Length, factionMarker.Length - "[TEMP_FACTION:".Length - 1);

                        // 从Notes中移除临时标记
                        character.Notes = notes.Replace(factionMarker, "").Trim();
                        if (string.IsNullOrEmpty(character.Notes))
                        {
                            character.Notes = null;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(factionName))
            {
                var existingFaction = await _unitOfWork.Factions.GetByNameAsync(character.ProjectId, factionName, cancellationToken);
                if (existingFaction != null)
                {
                    character.FactionId = existingFaction.Id;
                    _logger.LogInformation("找到现有势力: {FactionName}, ID: {FactionId}", factionName, existingFaction.Id);
                }
                else
                {
                    character.FactionId = null;
                    _logger.LogWarning("势力 {FactionName} 不存在，将角色势力设置为空", factionName);
                }
            }
            else
            {
                character.FactionId = null;
            }

            // 确保导航属性为空，避免EF Core尝试保存不完整的关联对象
            character.Faction = null;

            // 处理种族关系 - 查找现有种族或清除关联
            if (character.Race != null && !string.IsNullOrEmpty(character.Race.Name))
            {
                var existingRace = await _unitOfWork.Races.GetByNameAsync(character.ProjectId, character.Race.Name, cancellationToken);
                if (existingRace != null)
                {
                    character.RaceId = existingRace.Id;
                    _logger.LogInformation("找到现有种族: {RaceName}, ID: {RaceId}", character.Race.Name, existingRace.Id);
                }
                else
                {
                    character.RaceId = null;
                    _logger.LogWarning("种族 {RaceName} 不存在，将角色种族设置为空", character.Race.Name);
                }
                // 清除Race导航属性，避免EF Core尝试保存不完整的Race对象
                character.Race = null;
            }
            else
            {
                character.RaceId = null;
                character.Race = null;
            }

            // 清除章节外键字段，避免外键约束失败
            character.FirstAppearanceChapterId = null;
            character.LastAppearanceChapterId = null;

            // 设置创建时间
            character.CreatedAt = DateTime.UtcNow;
            character.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Characters.AddAsync(character, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("成功创建角色: {CharacterName}, ID: {CharacterId}", character.Name, character.Id);
            return character;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建角色时发生错误: {CharacterName}", character.Name);
            throw;
        }
    }

    /// <summary>
    /// 更新角色信息
    /// </summary>
    public async Task<Character> UpdateCharacterAsync(Character character, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始更新角色: {CharacterName}, ID: {CharacterId}", character.Name, character.Id);

            // 获取现有实体（这会被EF Core跟踪）
            var existingCharacter = await _unitOfWork.Characters.GetByIdAsync(character.Id, cancellationToken);
            if (existingCharacter == null)
            {
                throw new ArgumentException($"角色不存在，ID: {character.Id}");
            }

            // 更新现有实体的属性，而不是尝试跟踪新的实体实例
            existingCharacter.Name = character.Name;
            existingCharacter.Type = character.Type;
            existingCharacter.Gender = character.Gender;
            existingCharacter.Age = character.Age;
            existingCharacter.CultivationLevel = character.CultivationLevel;
            existingCharacter.Background = character.Background;
            existingCharacter.Appearance = character.Appearance;
            existingCharacter.Personality = character.Personality;
            existingCharacter.Abilities = character.Abilities;
            existingCharacter.Notes = character.Notes;
            existingCharacter.Tags = character.Tags;
            existingCharacter.Status = character.Status;
            existingCharacter.Importance = character.Importance;

            // 处理势力关系 - 从Tags字段提取势力信息
            if (!string.IsNullOrEmpty(character.Tags))
            {
                // 尝试从Tags中提取势力信息（格式：势力:势力名称）
                var factionMatch = System.Text.RegularExpressions.Regex.Match(character.Tags, @"势力:([^,;]+)");
                if (factionMatch.Success)
                {
                    var factionName = factionMatch.Groups[1].Value.Trim();
                    var existingFaction = await _unitOfWork.Factions.GetByNameAsync(existingCharacter.ProjectId, factionName, cancellationToken);
                    if (existingFaction != null)
                    {
                        existingCharacter.FactionId = existingFaction.Id;
                        _logger.LogInformation("找到现有势力: {FactionName}, ID: {FactionId}", factionName, existingFaction.Id);
                    }
                    else
                    {
                        existingCharacter.FactionId = null;
                        _logger.LogWarning("势力 {FactionName} 不存在，将角色势力设置为空", factionName);
                    }
                }
                else
                {
                    existingCharacter.FactionId = null;
                }

                // 尝试从Tags中提取种族信息（格式：种族:种族名称）
                var raceMatch = System.Text.RegularExpressions.Regex.Match(character.Tags, @"种族:([^,;]+)");
                if (raceMatch.Success)
                {
                    var raceName = raceMatch.Groups[1].Value.Trim();
                    var existingRace = await _unitOfWork.Races.GetByNameAsync(existingCharacter.ProjectId, raceName, cancellationToken);
                    if (existingRace != null)
                    {
                        existingCharacter.RaceId = existingRace.Id;
                        _logger.LogInformation("找到现有种族: {RaceName}, ID: {RaceId}", raceName, existingRace.Id);
                    }
                    else
                    {
                        existingCharacter.RaceId = null;
                        _logger.LogWarning("种族 {RaceName} 不存在，将角色种族设置为空", raceName);
                    }
                }
                else
                {
                    existingCharacter.RaceId = null;
                }
            }
            else
            {
                existingCharacter.FactionId = null;
                existingCharacter.RaceId = null;
            }

            // 清除章节外键字段，避免外键约束失败
            existingCharacter.FirstAppearanceChapterId = null;
            existingCharacter.LastAppearanceChapterId = null;

            // 设置更新时间（CreatedAt和Version保持不变）
            existingCharacter.UpdatedAt = DateTime.UtcNow;

            // 确保清除所有导航属性，避免EF Core意外跟踪相关实体
            existingCharacter.Project = null!;
            existingCharacter.Faction = null;
            existingCharacter.Race = null;

            // 清除集合导航属性
            if (existingCharacter.RelationshipsAsSource != null)
                existingCharacter.RelationshipsAsSource.Clear();
            if (existingCharacter.RelationshipsAsTarget != null)
                existingCharacter.RelationshipsAsTarget.Clear();
            if (existingCharacter.RelatedPlots != null)
                existingCharacter.RelatedPlots.Clear();
            if (existingCharacter.ParticipatedNetworks != null)
                existingCharacter.ParticipatedNetworks.Clear();
            if (existingCharacter.CentralNetworks != null)
                existingCharacter.CentralNetworks.Clear();

            // 直接保存更改，不需要调用UpdateAsync，因为existingCharacter已被EF Core跟踪
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("成功更新角色: {CharacterName}, ID: {CharacterId}", existingCharacter.Name, existingCharacter.Id);
            return existingCharacter;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新角色时发生错误: {CharacterName}, ID: {CharacterId}", character.Name, character.Id);
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取角色
    /// </summary>
    public async Task<Character?> GetCharacterByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取角色，ID: {CharacterId}", id);
            var character = await _unitOfWork.Characters.GetByIdAsync(id, cancellationToken);
            if (character == null)
            {
                _logger.LogWarning("未找到角色，ID: {CharacterId}", id);
            }
            return character;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取角色时发生错误，ID: {CharacterId}", id);
            throw;
        }
    }

    /// <summary>
    /// 根据项目ID获取所有角色
    /// </summary>
    public async Task<IEnumerable<Character>> GetCharactersByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取项目角色列表，项目ID: {ProjectId}", projectId);
            
            var characters = await _unitOfWork.Characters.GetByProjectIdAsync(projectId, cancellationToken);
            var orderedCharacters = characters.OrderBy(c => c.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个角色", orderedCharacters.Count);
            return orderedCharacters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目角色列表时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 按类型获取角色
    /// </summary>
    public async Task<IEnumerable<Character>> GetCharactersByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按类型获取角色，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            
            var characters = await _unitOfWork.Characters.GetByTypeAsync(projectId, type, cancellationToken);
            var orderedCharacters = characters.OrderBy(c => c.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个 {Type} 类型的角色", orderedCharacters.Count, type);
            return orderedCharacters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类型获取角色时发生错误，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            throw;
        }
    }

    /// <summary>
    /// 按势力获取角色
    /// </summary>
    public async Task<IEnumerable<Character>> GetCharactersByFactionAsync(Guid projectId, Guid factionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按势力获取角色，势力ID: {FactionId}", factionId);

            var characters = await _unitOfWork.Characters.GetByFactionAsync(projectId, factionId, cancellationToken);
            var orderedCharacters = characters.OrderBy(c => c.Name).ToList();

            _logger.LogInformation("成功获取 {Count} 个属于该势力的角色", orderedCharacters.Count);
            return orderedCharacters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按势力获取角色时发生错误，势力ID: {FactionId}", factionId);
            throw;
        }
    }

    /// <summary>
    /// 获取角色关系
    /// </summary>
    public async Task<IEnumerable<CharacterRelationship>> GetCharacterRelationshipsAsync(Guid characterId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取角色关系，角色ID: {CharacterId}", characterId);
            
            var relationships = await _unitOfWork.CharacterRelationships.GetByCharacterIdAsync(characterId, cancellationToken);
            var orderedRelationships = relationships.OrderBy(r => r.RelationshipType).ToList();

            _logger.LogInformation("成功获取 {Count} 个角色关系", orderedRelationships.Count);
            return orderedRelationships;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取角色关系时发生错误，角色ID: {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    public async Task<bool> DeleteCharacterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始删除角色，ID: {CharacterId}", id);
            
            var character = await _unitOfWork.Characters.GetByIdAsync(id, cancellationToken);
            if (character == null)
            {
                _logger.LogWarning("要删除的角色不存在，ID: {CharacterId}", id);
                return false;
            }
            
            await _unitOfWork.Characters.DeleteAsync(character, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功删除角色，ID: {CharacterId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除角色时发生错误，ID: {CharacterId}", id);
            throw;
        }
    }

    /// <summary>
    /// 搜索角色
    /// </summary>
    public async Task<IEnumerable<Character>> SearchCharactersAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始搜索角色，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            
            var characters = await _unitOfWork.Characters.SearchAsync(projectId, searchTerm, cancellationToken);
            var orderedCharacters = characters.OrderBy(c => c.Name).ToList();
            
            _logger.LogInformation("搜索完成，找到 {Count} 个匹配的角色", orderedCharacters.Count);
            return orderedCharacters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索角色时发生错误，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            throw;
        }
    }

    /// <summary>
    /// 获取角色统计信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetCharacterStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取角色统计信息，项目ID: {ProjectId}", projectId);
            
            var characters = await _unitOfWork.Characters.GetByProjectIdAsync(projectId, cancellationToken);
            var characterList = characters.ToList();
            
            var typeStats = characterList
                .GroupBy(c => c.Type)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var genderStats = characterList
                .GroupBy(c => c.Gender ?? "未知")
                .ToDictionary(g => g.Key, g => g.Count());
            
            var statusStats = characterList
                .GroupBy(c => c.Status)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var statistics = new Dictionary<string, object>
            {
                ["TotalCharacters"] = characterList.Count,
                ["TypeStatistics"] = typeStats,
                ["GenderStatistics"] = genderStats,
                ["StatusStatistics"] = statusStats,
                ["MainCharacters"] = characterList.Count(c => c.Type == "主角"),
                ["SupportingCharacters"] = characterList.Count(c => c.Type == "配角"),
                ["MinorCharacters"] = characterList.Count(c => c.Type == "龙套"),
                ["AverageImportance"] = characterList.Any() ? characterList.Average(c => c.Importance) : 0
            };
            
            _logger.LogInformation("成功获取角色统计信息，项目ID: {ProjectId}", projectId);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取角色统计信息时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 根据重要性获取角色
    /// </summary>
    public async Task<IEnumerable<Character>> GetCharactersByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按重要性获取角色，项目ID: {ProjectId}, 最小重要性: {MinImportance}", projectId, minImportance);
            
            var characters = await _unitOfWork.Characters.GetByImportanceAsync(projectId, minImportance, cancellationToken);
            var orderedCharacters = characters.OrderByDescending(c => c.Importance).ThenBy(c => c.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个重要性 >= {MinImportance} 的角色", orderedCharacters.Count, minImportance);
            return orderedCharacters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按重要性获取角色时发生错误，项目ID: {ProjectId}, 最小重要性: {MinImportance}", projectId, minImportance);
            throw;
        }
    }

    /// <summary>
    /// 获取角色及其关系网络
    /// </summary>
    public async Task<Character?> GetCharacterWithRelationshipsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取角色及其关系网络，ID: {CharacterId}", id);

            var character = await _unitOfWork.Characters.GetWithRelationshipsAsync(id, cancellationToken);
            if (character == null)
            {
                _logger.LogWarning("未找到角色，ID: {CharacterId}", id);
            }

            return character;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取角色及其关系网络时发生错误，ID: {CharacterId}", id);
            throw;
        }
    }

    /// <summary>
    /// 检查角色是否被引用
    /// </summary>
    public async Task<CharacterReferenceInfo> CheckCharacterReferencesAsync(Guid characterId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始检查角色引用，ID: {CharacterId}", characterId);

            var referenceInfo = new CharacterReferenceInfo
            {
                CharacterId = characterId,
                IsReferenced = false,
                References = new List<string>()
            };

            // 检查角色关系
            var relationships = await _unitOfWork.CharacterRelationships.GetByCharacterIdAsync(characterId, cancellationToken);
            if (relationships.Any())
            {
                referenceInfo.IsReferenced = true;
                referenceInfo.References.Add($"存在 {relationships.Count()} 个角色关系");
            }

            // 检查剧情中的引用（通过RelatedCharacters导航属性）
            var plots = await _unitOfWork.Plots.FindAsync(p => p.RelatedCharacters.Any(c => c.Id == characterId), cancellationToken);
            if (plots.Any())
            {
                referenceInfo.IsReferenced = true;
                referenceInfo.References.Add($"在 {plots.Count()} 个剧情中被引用");
            }

            // 检查势力中的引用（如果角色属于某个势力）
            var character = await _unitOfWork.Characters.GetByIdAsync(characterId, cancellationToken);
            if (character?.FactionId.HasValue == true)
            {
                var faction = await _unitOfWork.Factions.GetByIdAsync(character.FactionId.Value, cancellationToken);
                if (faction != null)
                {
                    referenceInfo.IsReferenced = true;
                    referenceInfo.References.Add($"属于势力 '{faction.Name}'");
                }
            }

            _logger.LogInformation("角色引用检查完成，ID: {CharacterId}, 是否被引用: {IsReferenced}", characterId, referenceInfo.IsReferenced);
            return referenceInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查角色引用时发生错误，ID: {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// 安全删除角色（检查引用后删除）
    /// </summary>
    public async Task<CharacterDeleteResult> SafeDeleteCharacterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始安全删除角色，ID: {CharacterId}", id);

            var character = await _unitOfWork.Characters.GetByIdAsync(id, cancellationToken);
            if (character == null)
            {
                return new CharacterDeleteResult
                {
                    Success = false,
                    Message = "角色不存在"
                };
            }

            // 检查引用
            var referenceInfo = await CheckCharacterReferencesAsync(id, cancellationToken);
            if (referenceInfo.IsReferenced)
            {
                return new CharacterDeleteResult
                {
                    Success = false,
                    Message = "角色已被引用，无法删除",
                    ReferenceInfo = referenceInfo
                };
            }

            // 执行删除
            await _unitOfWork.Characters.DeleteAsync(character, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("成功删除角色，ID: {CharacterId}", id);
            return new CharacterDeleteResult
            {
                Success = true,
                Message = "角色删除成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安全删除角色时发生错误，ID: {CharacterId}", id);
            return new CharacterDeleteResult
            {
                Success = false,
                Message = $"删除角色时发生错误: {ex.Message}"
            };
        }
    }
}
