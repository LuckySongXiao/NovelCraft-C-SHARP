using System.Collections.Generic;

namespace NovelManagement.WPF.Models
{
    /// <summary>
    /// 前置条件生成选项
    /// </summary>
    public class PrerequisiteGenerationOptions
    {
        /// <summary>
        /// 是否生成剧情大纲
        /// </summary>
        public bool GeneratePlotOutlines { get; set; } = true;

        /// <summary>
        /// 是否生成主要角色
        /// </summary>
        public bool GenerateMainCharacters { get; set; } = true;

        /// <summary>
        /// 是否生成世界设定
        /// </summary>
        public bool GenerateWorldSettings { get; set; } = true;

        /// <summary>
        /// 是否生成势力组织
        /// </summary>
        public bool GenerateFactions { get; set; } = true;

        /// <summary>
        /// 是否使用AI智能生成
        /// </summary>
        public bool UseAIGeneration { get; set; } = false;

        /// <summary>
        /// AI生成提示词
        /// </summary>
        public string AIPrompt { get; set; } = string.Empty;

        /// <summary>
        /// 小说类型/风格
        /// </summary>
        public string NovelGenre { get; set; } = "修仙";

        /// <summary>
        /// 世界观风格
        /// </summary>
        public string WorldStyle { get; set; } = "东方玄幻";

        /// <summary>
        /// 自定义剧情大纲模板
        /// </summary>
        public List<PlotTemplate> CustomPlotTemplates { get; set; } = new();

        /// <summary>
        /// 自定义角色模板
        /// </summary>
        public List<CharacterTemplate> CustomCharacterTemplates { get; set; } = new();

        /// <summary>
        /// 自定义世界设定模板
        /// </summary>
        public List<WorldSettingTemplate> CustomWorldSettingTemplates { get; set; } = new();

        /// <summary>
        /// 自定义势力模板
        /// </summary>
        public List<FactionTemplate> CustomFactionTemplates { get; set; } = new();

        /// <summary>
        /// 生成数量配置
        /// </summary>
        public GenerationQuantityConfig QuantityConfig { get; set; } = new();

        /// <summary>
        /// 是否允许用户后续编辑
        /// </summary>
        public bool AllowUserEditing { get; set; } = true;

        /// <summary>
        /// 是否保存为模板供后续使用
        /// </summary>
        public bool SaveAsTemplate { get; set; } = false;
    }

    /// <summary>
    /// 剧情大纲模板
    /// </summary>
    public class PlotTemplate
    {
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string SummaryTemplate { get; set; } = string.Empty;
        public int ImportanceLevel { get; set; } = 5;
        public List<string> PlotPointTemplates { get; set; } = new();
    }

    /// <summary>
    /// 角色模板
    /// </summary>
    public class CharacterTemplate
    {
        public string NameTemplate { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string AppearanceTemplate { get; set; } = string.Empty;
        public string PersonalityTemplate { get; set; } = string.Empty;
        public string BackgroundTemplate { get; set; } = string.Empty;
        public int ImportanceLevel { get; set; } = 5;
    }

    /// <summary>
    /// 世界设定模板
    /// </summary>
    public class WorldSettingTemplate
    {
        public string NameTemplate { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string ContentTemplate { get; set; } = string.Empty;
        public int Importance { get; set; } = 5;
    }

    /// <summary>
    /// 势力模板
    /// </summary>
    public class FactionTemplate
    {
        public string NameTemplate { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string DescriptionTemplate { get; set; } = string.Empty;
        public int PowerLevel { get; set; } = 50;
        public int Importance { get; set; } = 50;
        public List<string> TagTemplates { get; set; } = new();
    }

    /// <summary>
    /// 生成数量配置
    /// </summary>
    public class GenerationQuantityConfig
    {
        /// <summary>
        /// 剧情大纲生成数量
        /// </summary>
        public int PlotOutlinesCount { get; set; } = 3;

        /// <summary>
        /// 主要角色生成数量
        /// </summary>
        public int MainCharactersCount { get; set; } = 3;

        /// <summary>
        /// 世界设定生成数量
        /// </summary>
        public int WorldSettingsCount { get; set; } = 5;

        /// <summary>
        /// 势力组织生成数量
        /// </summary>
        public int FactionsCount { get; set; } = 3;
    }
}
