using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.DTOs;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 角色管理测试辅助类
    /// </summary>
    public static class CharacterManagementTestHelper
    {
        /// <summary>
        /// 测试角色引用检查功能
        /// </summary>
        public static async Task<bool> TestCharacterReferenceCheck(CharacterService characterService, ILogger logger)
        {
            try
            {
                logger.LogInformation("开始测试角色引用检查功能");

                // 创建测试角色
                var testCharacter = new Character
                {
                    Id = Guid.NewGuid(),
                    Name = "测试角色",
                    Type = "主角",
                    ProjectId = Guid.NewGuid(),
                    Background = "这是一个测试角色",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // 测试引用检查
                var referenceInfo = await characterService.CheckCharacterReferencesAsync(testCharacter.Id);
                
                logger.LogInformation($"角色引用检查结果: 是否被引用={referenceInfo.IsReferenced}, 引用数量={referenceInfo.ReferenceCount}");
                
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "测试角色引用检查功能失败");
                return false;
            }
        }

        /// <summary>
        /// 测试角色安全删除功能
        /// </summary>
        public static async Task<bool> TestSafeCharacterDelete(CharacterService characterService, ILogger logger)
        {
            try
            {
                logger.LogInformation("开始测试角色安全删除功能");

                // 使用一个不存在的角色ID进行测试
                var nonExistentId = Guid.NewGuid();
                var deleteResult = await characterService.SafeDeleteCharacterAsync(nonExistentId);
                
                logger.LogInformation($"删除不存在角色的结果: 成功={deleteResult.Success}, 消息={deleteResult.Message}");
                
                // 应该返回失败，因为角色不存在
                return !deleteResult.Success && deleteResult.Message.Contains("不存在");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "测试角色安全删除功能失败");
                return false;
            }
        }

        /// <summary>
        /// 验证角色数据完整性
        /// </summary>
        public static bool ValidateCharacterData(Character character, ILogger logger)
        {
            try
            {
                logger.LogInformation($"验证角色数据完整性: {character.Name}");

                // 检查必填字段
                if (string.IsNullOrEmpty(character.Name))
                {
                    logger.LogWarning("角色名称为空");
                    return false;
                }

                if (string.IsNullOrEmpty(character.Type))
                {
                    logger.LogWarning("角色类型为空");
                    return false;
                }

                if (character.ProjectId == Guid.Empty)
                {
                    logger.LogWarning("项目ID为空");
                    return false;
                }

                logger.LogInformation("角色数据验证通过");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "验证角色数据时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 创建测试角色数据
        /// </summary>
        public static Character CreateTestCharacter(Guid projectId)
        {
            return new Character
            {
                Id = Guid.NewGuid(),
                Name = "测试角色_" + DateTime.Now.Ticks,
                Type = "主角",
                ProjectId = projectId,
                Background = "这是一个用于测试的角色",
                Personality = "勇敢、正义",
                CultivationLevel = "筑基期",
                Importance = 5,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
