using System;
using System.Collections.Generic;

namespace NovelManagement.WPF.Models
{
    /// <summary>
    /// 项目上下文数据
    /// </summary>
    public class ProjectContextData
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        public Guid ProjectId { get; set; } = Guid.Empty;

        /// <summary>
        /// 剧情大纲列表
        /// </summary>
        public List<object> PlotOutlines { get; set; } = new();

        /// <summary>
        /// 主要角色列表
        /// </summary>
        public List<object> MainCharacters { get; set; } = new();

        /// <summary>
        /// 世界设定列表
        /// </summary>
        public List<object> WorldSettings { get; set; } = new();
    }
}
