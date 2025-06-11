using System;
using NovelManagement.Core.Entities;

namespace NovelManagement.WPF.Events
{
    /// <summary>
    /// 角色更新事件参数
    /// </summary>
    public class CharacterUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        public Guid CharacterId { get; set; }

        /// <summary>
        /// 更新后的角色信息
        /// </summary>
        public Character UpdatedCharacter { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="characterId">角色ID</param>
        /// <param name="updatedCharacter">更新后的角色信息</param>
        public CharacterUpdatedEventArgs(Guid characterId, Character updatedCharacter)
        {
            CharacterId = characterId;
            UpdatedCharacter = updatedCharacter;
        }
    }
}
