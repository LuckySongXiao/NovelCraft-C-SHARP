using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Serilog;

namespace NovelManagement.WPF.Localization
{
    /// <summary>
    /// 本地化词条（中文 / 英文一对）。
    /// </summary>
    public readonly record struct LocEntry(string Zh, string En);

    /// <summary>
    /// 本地化管理器。
    /// 语言策略：跟随系统 UI 语言（zh-* 用中文，其余用英文）；可通过 user.json 的
    /// Localization:Language 节点强制覆盖（如 en-US / zh-CN），用于测试或特殊需求。
    /// XAML 通过 {DynamicResource Loc.键名} 绑定词条；代码层通过 T()/TF() 取词。
    /// 词条完整性策略：应用表 = 中文表打底 + 当前语言表覆盖，缺失条目自动回退中文，不会出现空白控件。
    /// </summary>
    public static class LocalizationManager
    {
        private const string ResourceKeyPrefix = "Loc.";
        private const string FontMarkerKey = "Loc.FontFamily";

        private static IReadOnlyDictionary<string, string>? _currentTable;
        private static IReadOnlyDictionary<string, string>? _zhTable;
        private static IReadOnlyDictionary<string, string>? _enTable;
        private static ResourceDictionary? _appliedDictionary;

        /// <summary>当前语言（zh-CN / en-US）。</summary>
        public static string CurrentLanguage { get; private set; } = "zh-CN";

        /// <summary>当前是否为英文界面。</summary>
        public static bool IsEnglish =>
            CurrentLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase);

        /// <summary>语言应用后触发（预留运行时切换能力）。</summary>
        public static event EventHandler? LanguageChanged;

        /// <summary>
        /// 初始化本地化：解析语言 → 构建词条表 → 应用到应用级资源字典。
        /// 必须在任何窗口显示之前调用（密码门之前）。
        /// </summary>
        /// <param name="userConfigPath">用户配置文件路径（appsettings.user.json），可为空。</param>
        /// <param name="logger">日志，可为空。</param>
        public static void Initialize(string? userConfigPath = null, ILogger? logger = null)
        {
            var language = ResolveLanguage(userConfigPath, logger);
            CurrentLanguage = language;
            BuildTables();
            Apply();
            logger?.Information("本地化已初始化，语言：{Language}", CurrentLanguage);
        }

        /// <summary>
        /// 解析生效语言：user.json Localization:Language 覆盖优先，否则跟随系统 UI 语言。
        /// </summary>
        private static string ResolveLanguage(string? userConfigPath, ILogger? logger)
        {
            var overrideLanguage = ReadLanguageOverride(userConfigPath, logger);
            if (!string.IsNullOrWhiteSpace(overrideLanguage))
            {
                var normalized = NormalizeLanguage(overrideLanguage);
                if (normalized != null)
                {
                    return normalized;
                }
            }

            var uiCulture = CultureInfo.CurrentUICulture;
            if (uiCulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                return "zh-CN";
            }

            return "en-US";
        }

        /// <summary>
        /// 运行时切换语言：归一化 → 重建应用表 → 重新应用资源字典 → 持久化到 user.json → 触发事件。
        /// XAML 中 {DynamicResource Loc.*} 绑定会即时刷新；已打开的对话框在重新打开后刷新。
        /// </summary>
        /// <param name="language">目标语言（zh-CN / en-US，接受 zh*/en* 前缀）。</param>
        /// <param name="userConfigPath">用户配置文件路径（appsettings.user.json），为空则不持久化。</param>
        /// <param name="logger">日志，可为空。</param>
        public static void SetLanguage(string language, string? userConfigPath = null, ILogger? logger = null)
        {
            var normalized = NormalizeLanguage(language ?? string.Empty);
            if (normalized == null || normalized == CurrentLanguage)
            {
                return;
            }

            CurrentLanguage = normalized;
            RebuildCurrentTable();
            Apply();
            PersistLanguageOverride(userConfigPath, normalized, logger);
            logger?.Information("本地化语言已切换为 {Language}", CurrentLanguage);
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// 中文存储值 → 当前语言显示（项目类型/状态等枚举型数据；未知值原样返回）。
        /// 仅用于展示层，数据库存储值保持中文不变。
        /// </summary>
        public static string LocalizeStoredValue(string stored)
        {
            if (string.IsNullOrWhiteSpace(stored))
            {
                return stored ?? string.Empty;
            }

            // 去除首尾空白后再比对（历史数据可能带空格），未命中时返回原值
            var trimmed = stored.Trim();
            var key = trimmed switch
            {
                "修仙书籍" => "OG.TypeXianxia",
                "都市书籍" => "OG.TypeUrban",
                "玄幻书籍" => "OG.TypeFantasy",
                "科幻书籍" => "OG.TypeScifi",
                "历史书籍" => "OG.TypeHistory",
                "武侠书籍" => "OG.TypeWuxia",
                "言情书籍" => "OG.TypeRomance",
                "悬疑书籍" => "OG.TypeMystery",
                "军事书籍" => "NPD.TypeMilitary",
                "奇幻书籍" => "NPD.TypeFantasy",
                "进行中" => "PLM.StatusInProgress",
                "已完成" => "PLM.StatusCompleted",
                "计划中" => "PLM.StatusPlanned",
                // 剧情线类型
                "主线" => "SV.PlotMain",
                "支线" => "SV.PlotSub",
                "情感线" => "SV.PlotEmotional",
                "感情线" => "SV.PlotEmotional",
                "剧情事件" => "TL.Data.剧情事件",
                "规划中" => "PLM.StatusPlanned",
                "暂停" => "PLM.StatusPaused",
                "全部关系" => "RN.TypeAll",
                "全部势力" => "RN.FactionAll",
                "结合 SubAgent 的需求简报撰写正式文案，专注内容创作。" => "AICfg.MainAgentRoleText",
                "总结写作需求，整理 MainAgent 草稿，并将纯净内容写入项目档案库。" => "AICfg.SubAgentRoleText",
                "长篇书籍" => "OG.TypeLongForm",
                // 角色类型
                "主角" => "CM.TypeProtagonist",
                "女主角" => "SV.CharFemaleLead",
                "主配角" => "CM.TypeSecondaryLead",
                "次配角" => "CM.TypeSupporting",
                "配角" => "SV.CharSupporting",
                "反派" => "CM.TypeAntagonist",
                "路人" => "CM.TypeExtra",
                "师父" => "SV.CharMentor",
                // 势力类型
                "宗门" => "FM.Data.宗门",
                "修仙宗门" => "SV.FactionSectCultivation",
                "家族" => "FM.Data.家族",
                "组织" => "FM.Data.组织",
                "商业组织" => "SV.FactionCommercial",
                "国家" => "FM.Data.国家",
                "商会" => "FM.Data.商会",
                // 势力等级
                "超级" => "FM.Data.超级",
                "一流" => "FM.Data.一流",
                "二流" => "FM.Data.二流",
                "三流" => "FM.Data.三流",
                "普通" => "FM.Data.普通",
                // 修炼体系品质 / 类型
                "凡级" => "WS.Tec.Data.凡级",
                "黄级" => "WS.Tec.Data.黄级",
                "玄级" => "WS.Tec.Data.玄级",
                "地级" => "WS.Tec.Data.地级",
                "天级" => "WS.Tec.Data.天级",
                "神级" => "SV.GradeDivine",
                "仙级" => "WS.Tec.Data.仙级",
                "内功心法" => "WS.Cult.TypeInternal",
                "外功招式" => "WS.Cult.TypeExternal",
                "身法轻功" => "WS.Cult.TypeMovement",
                "炼体功法" => "WS.Cult.TypeBody",
                "神识功法" => "WS.Cult.TypeMind",
                // 角色境界默认值
                "练气期" => "CED.Data.练气期",
                "筑基期" => "CED.Data.筑基期",
                "金丹期" => "CED.Data.金丹期",
                "元婴期" => "CED.Data.元婴期",
                "化神期" => "CED.Data.化神期",
                "炼虚期" => "CED.Data.炼虚期",
                "合体期" => "CED.Data.合体期",
                "大乘期" => "CED.Data.大乘期",
                "渡劫期" => "CED.Data.渡劫期",
                // 人际关系类型
                "师徒关系" => "SV.RelMasterDisciple",
                "师徒" => "SV.RelMasterDiscipleShort",
                "朋友关系" => "SV.RelFriend",
                "爱情关系" => "SV.RelRomance",
                "敌对关系" => "SV.RelHostile",
                "同门关系" => "SV.RelSectFellow",
                "亲属关系" => "SV.RelKinship",
                // 其它占位
                "无势力" => "SV.NoFaction",
                "未设置" => "SV.NotSet",
                "全部" => "CM.TypeAll",
                // 关系状态
                "稳定" => "SV.StatusStable",
                "友好" => "SV.StatusFriendly",
                "紧张" => "SV.StatusTense",
                "敌对" => "SV.StatusHostile",
                "复杂" => "SV.StatusComplex",
                _ => null,
            };
            return key == null ? stored : T(key, trimmed);
        }

        /// <summary>按当前语言重建应用表（中文打底 + 当前语言覆盖）。</summary>
        private static void RebuildCurrentTable()
        {
            if (_zhTable == null || _enTable == null)
            {
                BuildTables();
                return;
            }

            var merged = new Dictionary<string, string>(_zhTable, StringComparer.Ordinal);
            if (IsEnglish)
            {
                foreach (var (key, value) in _enTable)
                {
                    merged[key] = value;
                }
            }

            _currentTable = merged;
        }

        /// <summary>把 Localization:Language 写入 user.json（保留其余节点；写失败仅记日志，不中断切换）。</summary>
        private static void PersistLanguageOverride(string? userConfigPath, string language, ILogger? logger)
        {
            if (string.IsNullOrWhiteSpace(userConfigPath))
            {
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(userConfigPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var root = new Dictionary<string, object>();
                if (File.Exists(userConfigPath))
                {
                    using var stream = File.OpenRead(userConfigPath);
                    using var document = JsonDocument.Parse(stream);
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        root[property.Name] = property.Value.Clone();
                    }
                }

                root["Localization"] = new Dictionary<string, object>
                {
                    ["Language"] = language,
                };

                var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                File.WriteAllText(userConfigPath, JsonSerializer.Serialize(root, options));
            }
            catch (Exception ex)
            {
                logger?.Warning(ex, "写入本地化语言配置失败：{Path}", userConfigPath);
            }
        }

        /// <summary>读取 user.json 的 Localization:Language 覆盖值（异常/缺失时返回 null）。</summary>
        private static string? ReadLanguageOverride(string? userConfigPath, ILogger? logger)
        {
            if (string.IsNullOrWhiteSpace(userConfigPath) || !File.Exists(userConfigPath))
            {
                return null;
            }

            try
            {
                using var stream = File.OpenRead(userConfigPath);
                using var document = JsonDocument.Parse(stream);
                if (!document.RootElement.TryGetProperty("Localization", out var section) ||
                    section.ValueKind != JsonValueKind.Object ||
                    !section.TryGetProperty("Language", out var languageNode))
                {
                    return null;
                }

                return languageNode.ValueKind == JsonValueKind.String
                    ? languageNode.GetString()
                    : null;
            }
            catch (Exception ex)
            {
                logger?.Warning(ex, "读取本地化语言覆盖配置失败，回退跟随系统语言");
                return null;
            }
        }

        /// <summary>把覆盖值归一化为 zh-CN / en-US；无法识别时返回 null（交由系统语言决定）。</summary>
        private static string? NormalizeLanguage(string value)
        {
            if (value.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                return "zh-CN";
            }

            if (value.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                return "en-US";
            }

            return null;
        }

        /// <summary>
        /// 构建 zh / en / 当前语言三张表。应用表 = 中文打底 + 当前语言覆盖。
        /// </summary>
        private static void BuildTables()
        {
            var zh = new Dictionary<string, string>(StringComparer.Ordinal);
            var en = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var source in AllSources())
            {
                foreach (var (key, entry) in source)
                {
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(entry.Zh))
                    {
                        zh[key] = entry.Zh;
                    }

                    if (!string.IsNullOrEmpty(entry.En))
                    {
                        en[key] = entry.En;
                    }
                }
            }

            _zhTable = zh;
            _enTable = en;

            var merged = new Dictionary<string, string>(zh, StringComparer.Ordinal);
            if (IsEnglish)
            {
                foreach (var (key, value) in en)
                {
                    merged[key] = value;
                }
            }

            _currentTable = merged;
        }

        /// <summary>所有词条来源（按领域拆分的静态表）。</summary>
        private static IEnumerable<IReadOnlyDictionary<string, LocEntry>> AllSources()
        {
            yield return StringsCore.Entries;
            yield return StringsContent.Entries;
            yield return StringsWorldbuilding.Entries;
            yield return StringsAI.Entries;
            yield return StringsAIExtra.Entries;
            yield return StringsOperations.Entries;
            yield return StringsDialogs.Entries;
        }

        /// <summary>
        /// 把当前语言词条写入应用级资源字典（键名带 Loc. 前缀），并按语言设置界面字体。
        /// 重复调用会替换旧字典（预留运行时切换能力）。
        /// </summary>
        private static void Apply()
        {
            var app = System.Windows.Application.Current;
            if (app == null)
            {
                return;
            }

            // 语种切换表：应用目录基础表 + 用户配置目录覆盖表（二次开发者追加语种列即可扩展）
            var userConfigDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NovelManagement", "config");
            LanguageTableStore.Load(AppContext.BaseDirectory, userConfigDir);

            if (_appliedDictionary != null)
            {
                app.Resources.MergedDictionaries.Remove(_appliedDictionary);
                _appliedDictionary = null;
            }

            var dictionary = new ResourceDictionary();
            if (_currentTable != null)
            {
                foreach (var (key, value) in _currentTable)
                {
                    dictionary[ResourceKeyPrefix + key] = value;
                }
            }

            // 生成链路语言开关：英文界面必须整段英文提示词，模型才会输出英文内容
            NovelManagement.AI.Utilities.AIPromptLanguage.UseEnglish = IsEnglish;

            var fontFamily = new FontFamily(IsEnglish ? "Segoe UI" : "SimSun");
            dictionary[FontMarkerKey] = fontFamily;
            dictionary["MaterialDesignFont"] = fontFamily;

            app.Resources.MergedDictionaries.Insert(0, dictionary);
            _appliedDictionary = dictionary;
        }

        /// <summary>
        /// 取词：当前语言 → 中文回退 → fallbackZh → 键名本身。
        /// </summary>
        /// <param name="key">词条键（不含 Loc. 前缀）。</param>
        /// <param name="fallbackZh">兜底中文（可选）。</param>
        public static string T(string key, string? fallbackZh = null)
        {
            if (!string.IsNullOrEmpty(key))
            {
                // 语种切换表覆盖层：二次开发者在 language-table.csv 中追加/覆盖的条目优先
                if (LanguageTableStore.TryGet(key, CurrentLanguage, out var overrideValue))
                {
                    return overrideValue;
                }

                if (_currentTable != null && _currentTable.TryGetValue(key, out var value))
                {
                    return value;
                }

                if (_zhTable != null && _zhTable.TryGetValue(key, out var zhValue))
                {
                    return zhValue;
                }
            }

            return fallbackZh ?? key ?? string.Empty;
        }

        /// <summary>
        /// 取词并格式化（带参数的文本，如「共 {0} 个项目」）。
        /// </summary>
        public static string TF(string key, string? fallbackZh, params object[] args)
        {
            var template = T(key, fallbackZh);
            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }

        /// <summary>中文表（键 → 中文文本），供测试与工具使用。</summary>
        public static IReadOnlyDictionary<string, string> GetZhTable() =>
            _zhTable ?? new Dictionary<string, string>();

        /// <summary>英文表（键 → 英文文本），供测试与工具使用。</summary>
        public static IReadOnlyDictionary<string, string> GetEnTable() =>
            _enTable ?? new Dictionary<string, string>();

        /// <summary>当前应用表（键 → 当前语言文本），供测试与工具使用。</summary>
        public static IReadOnlyDictionary<string, string> GetCurrentTable() =>
            _currentTable ?? new Dictionary<string, string>();
    }
}
