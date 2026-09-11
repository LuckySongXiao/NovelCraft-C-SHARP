using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using NovelManagement.WPF.Localization;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 主题皮肤画刷键与中文标签定义（用户可自定义的画刷范围，含窗口框架颜色）。
    /// </summary>
    public static class ThemeSkinBrushKeys
    {
        /// <summary>全部可自定义画刷键（键, 中文标签）。</summary>
        public static readonly IReadOnlyList<(string Key, string Label)> All = new List<(string, string)>
        {
            ("AppTitleBarBackgroundBrush", "标题栏背景（窗口框架）"),
            ("AppTitleBarForegroundBrush", "标题栏文字"),
            ("AppNavPanelBackgroundBrush", "侧边导航背景"),
            ("AppWindowBackgroundBrush", "窗口背景"),
            ("AppPanelBackgroundBrush", "面板背景"),
            ("AppCardBackgroundBrush", "卡片背景"),
            ("AppInputBackgroundBrush", "输入框/下拉框背景"),
            ("AppSubtleBorderBrush", "边框颜色"),
            ("AppMutedTextBrush", "次要文字"),
            ("AppSuccessBrush", "成功/建议文字"),
            ("AppForegroundTextBrush", "正文文字"),
            ("PrimaryTextBrush", "主文字（MaterialDesign）"),
            ("MaterialDesignBody", "正文文字（MaterialDesign）"),
            ("MaterialDesignBodyLight", "次要文字（MaterialDesign）"),
            ("MaterialDesignToolForeground", "工具栏文字（MaterialDesign）"),
            ("MaterialDesignToolTipForeground", "提示文字（MaterialDesign）"),
            ("MaterialDesignPaper", "对话框纸面（MaterialDesign）"),
            ("MaterialDesignPrimaryKeyBrush", "主按钮颜色（MaterialDesign）"),
            ("PrimaryBrush", "主色（HandyControl）"),
            ("PrimaryHueMidBrush", "页面头部/强调底色（其上文字为白色）"),
            ("PrimaryHueLightBrush", "徽章/浅强调底色（其上文字为白色）"),
            ("PrimaryHueDarkBrush", "深强调底色"),
            ("DataGridRowBackgroundBrush", "表格行背景"),
            ("DataGridAltRowBackgroundBrush", "表格交替行背景"),
        };

        /// <summary>画刷标签按当前语言显示（词条键 TS.C.&lt;BrushKey&gt;，未登记时回退中文标签）。</summary>
        public static string LocalizeBrushLabel(string brushKey)
        {
            var zh = All.FirstOrDefault(item => string.Equals(item.Key, brushKey, StringComparison.OrdinalIgnoreCase)).Label;
            return LocalizationManager.T("TS.C." + brushKey, string.IsNullOrEmpty(zh) ? brushKey : zh);
        }

        /// <summary>按 All 顺序填充颜色字典（缺失处用 fallback 补齐）。</summary>
        public static Dictionary<string, string> BuildColorSet(Func<string, string?> lookup, Dictionary<string, string>? fallback)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, _) in All)
            {
                var value = lookup(key);
                if (string.IsNullOrWhiteSpace(value) && fallback != null)
                {
                    fallback.TryGetValue(key, out value);
                }

                if (!string.IsNullOrWhiteSpace(value))
                {
                    dict[key] = value;
                }
            }

            return dict;
        }
    }

    /// <summary>
    /// 主题皮肤定义：以「画刷键 → #RRGGBB」形式存储颜色集合。
    /// </summary>
    public class ThemeSkin
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        /// <summary>明暗基调：true=深色（加载 MaterialDesign Dark / HandyControl SkinDark）。</summary>
        public bool IsDark { get; set; }

        /// <summary>是否内置皮肤（内置皮肤不可删除）。</summary>
        public bool IsBuiltin { get; set; }

        public Dictionary<string, string> Colors { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>皮肤来源（内置/自定义）—— 展示层按语言映射。</summary>
        public string SkinKind => IsBuiltin
            ? LocalizationManager.T("TS.KindBuiltin", "内置")
            : LocalizationManager.T("TS.KindCustom", "自定义");

        /// <summary>明暗基调 —— 展示层按语言映射。</summary>
        public string ToneText => IsDark
            ? LocalizationManager.T("TS.ToneDark", "深色基调")
            : LocalizationManager.T("TS.ToneLight", "浅色基调");

        public ThemeSkin Clone()
        {
            return new ThemeSkin
            {
                Id = Id,
                Name = Name,
                IsDark = IsDark,
                IsBuiltin = IsBuiltin,
                Colors = new Dictionary<string, string>(Colors, StringComparer.OrdinalIgnoreCase)
            };
        }
    }

    /// <summary>
    /// 内置皮肤：白昼（浅色）、黑夜（深色）、红粉花漾少女风（女频自动换肤）。
    /// </summary>
    public static class ThemeSkins
    {
        /// <summary>跟随时间自动切换（19:00–07:00 黑夜）。</summary>
        public const string AutoSkinId = "auto";
        public const string LightSkinId = "light";
        public const string DarkSkinId = "dark";
        public const string PinkSkinId = "pink";

        public static ThemeSkin CreateLightSkin() => new()
        {
            Id = LightSkinId,
            Name = LocalizationManager.T("TS.SkinLight", "白昼（浅色）"),
            IsDark = false,
            IsBuiltin = true,
            Colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["AppTitleBarBackgroundBrush"] = "#3F51B5",
                ["AppTitleBarForegroundBrush"] = "#FFFFFF",
                ["AppNavPanelBackgroundBrush"] = "#FAFBFF",
                ["AppWindowBackgroundBrush"] = "#F4F6FB",
                ["AppPanelBackgroundBrush"] = "#FAFBFF",
                ["AppCardBackgroundBrush"] = "#FFFFFF",
                ["AppInputBackgroundBrush"] = "#FFFFFF",
                ["AppSubtleBorderBrush"] = "#D9E1F2",
                ["AppMutedTextBrush"] = "#667085",
                ["AppSuccessBrush"] = "#2E7D32",
                ["AppForegroundTextBrush"] = "#1F2430",
                ["PrimaryTextBrush"] = "#200E32",
                ["MaterialDesignBody"] = "#212121",
                ["MaterialDesignBodyLight"] = "#545454",
                ["MaterialDesignToolForeground"] = "#212121",
                ["MaterialDesignToolTipForeground"] = "#FFFFFF",
                ["MaterialDesignPaper"] = "#F4F6FB",
                ["MaterialDesignPrimaryKeyBrush"] = "#3F51B5",
                ["PrimaryBrush"] = "#3F51B5",
                ["DataGridRowBackgroundBrush"] = "#FCFDFF",
                ["DataGridAltRowBackgroundBrush"] = "#F5F8FE",
                ["PrimaryHueMidBrush"] = "#3F51B5",
                ["PrimaryHueLightBrush"] = "#7986CB",
                ["PrimaryHueDarkBrush"] = "#303F9F"
            }
        };

        public static ThemeSkin CreateDarkSkin() => new()
        {
            Id = DarkSkinId,
            Name = LocalizationManager.T("TS.SkinDark", "黑夜（深色）"),
            IsDark = true,
            IsBuiltin = true,
            Colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["AppTitleBarBackgroundBrush"] = "#2A3441",
                ["AppTitleBarForegroundBrush"] = "#E6EDF6",
                ["AppNavPanelBackgroundBrush"] = "#262E38",
                ["AppWindowBackgroundBrush"] = "#1F262F",
                ["AppPanelBackgroundBrush"] = "#262E38",
                ["AppCardBackgroundBrush"] = "#2A323D",
                ["AppInputBackgroundBrush"] = "#242C36",
                ["AppSubtleBorderBrush"] = "#3F4B5B",
                ["AppMutedTextBrush"] = "#8A97A8",
                ["AppSuccessBrush"] = "#66BB6A",
                ["AppForegroundTextBrush"] = "#E6EDF6",
                ["PrimaryTextBrush"] = "#E6EDF6",
                ["MaterialDesignBody"] = "#E6EDF6",
                ["MaterialDesignBodyLight"] = "#B8C4D4",
                ["MaterialDesignToolForeground"] = "#E6EDF6",
                ["MaterialDesignToolTipForeground"] = "#E6EDF6",
                ["MaterialDesignPaper"] = "#1F262F",
                ["MaterialDesignPrimaryKeyBrush"] = "#5C6BC0",
                ["PrimaryBrush"] = "#3F51B5",
                ["DataGridRowBackgroundBrush"] = "#242C36",
                ["DataGridAltRowBackgroundBrush"] = "#2A323D",
                ["PrimaryHueMidBrush"] = "#3F51B5",
                ["PrimaryHueLightBrush"] = "#5C6BC0",
                ["PrimaryHueDarkBrush"] = "#303F9F"
            }
        };

        /// <summary>红粉花漾少女风：浅色粉系底 + 玫红强调（女频文自动换肤目标）。</summary>
        public static ThemeSkin CreatePinkSkin() => new()
        {
            Id = PinkSkinId,
            Name = LocalizationManager.T("TS.SkinPink", "红粉花漾少女风"),
            IsDark = false,
            IsBuiltin = true,
            Colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["AppTitleBarBackgroundBrush"] = "#F8A5C2",
                ["AppTitleBarForegroundBrush"] = "#6D214F",
                ["AppNavPanelBackgroundBrush"] = "#FFF0F5",
                ["AppWindowBackgroundBrush"] = "#FFF7FA",
                ["AppPanelBackgroundBrush"] = "#FFEFF5",
                ["AppCardBackgroundBrush"] = "#FFFFFF",
                ["AppInputBackgroundBrush"] = "#FFFFFF",
                ["AppSubtleBorderBrush"] = "#F8C8DC",
                ["AppMutedTextBrush"] = "#B4738F",
                ["AppSuccessBrush"] = "#2E7D32",
                ["AppForegroundTextBrush"] = "#5C2E42",
                ["PrimaryTextBrush"] = "#5C2E42",
                ["MaterialDesignBody"] = "#5C2E42",
                ["MaterialDesignBodyLight"] = "#96687D",
                ["MaterialDesignToolForeground"] = "#5C2E42",
                ["MaterialDesignToolTipForeground"] = "#FFFFFF",
                ["MaterialDesignPaper"] = "#FFF7FA",
                ["MaterialDesignPrimaryKeyBrush"] = "#EC5F95",
                ["PrimaryBrush"] = "#EC5F95",
                ["DataGridRowBackgroundBrush"] = "#FFF7FA",
                ["DataGridAltRowBackgroundBrush"] = "#FFEFF5",
                ["PrimaryHueMidBrush"] = "#D1508A",
                ["PrimaryHueLightBrush"] = "#B03A62",
                ["PrimaryHueDarkBrush"] = "#8E2C55"
            }
        };
    }

    /// <summary>
    /// 女频文关键词检测：命中任一关键词即判定为女频文并触发红粉花漾少女风皮肤。
    /// </summary>
    public static class FemaleNovelDetector
    {
        private static readonly string[] Keywords =
        {
            "女频", "言情", "古言", "现言", "甜宠", "宠文", "总裁", "霸总", "宅斗", "宫斗",
            "婚恋", "百合", "嫡女", "王妃", "太子妃", "世子妃", "追妻", "萌宝", "闪婚", "豪门",
            "千金", "闺秀", "女强", "女尊", "宫廷", "后宅", "红妆", "花漾", "凤逆", "凤舞"
        };

        private static readonly char[] SingleChars = { '凤', '妃', '妆', '绣', '闺' };

        /// <summary>判断书籍类型/简介/书名等文本是否为女频文。</summary>
        public static bool IsFemaleOriented(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            foreach (var keyword in Keywords)
            {
                if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            foreach (var ch in SingleChars)
            {
                if (text.Contains(ch))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 皮肤持久化：自定义皮肤存 exe 目录 custom_skins.json，当前选择存 theme_settings.json。
    /// </summary>
    public static class SkinStore
    {
        private static readonly string SkinsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "custom_skins.json");
        private static readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "theme_settings.json");
        private static readonly object SyncRoot = new();

        private sealed class ThemeSettings
        {
            public string SelectedSkinId { get; set; } = string.Empty;
            public bool AutoByTime { get; set; } = true;
        }

        public static List<ThemeSkin> LoadCustomSkins()
        {
            try
            {
                if (!File.Exists(SkinsPath))
                {
                    return new List<ThemeSkin>();
                }

                var json = File.ReadAllText(SkinsPath);
                var skins = JsonSerializer.Deserialize<List<ThemeSkin>>(json);
                return skins?
                    .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Id))
                    .Select(s => { s.IsBuiltin = false; return s; })
                    .ToList() ?? new List<ThemeSkin>();
            }
            catch (Exception)
            {
                return new List<ThemeSkin>();
            }
        }

        public static void SaveCustomSkins(IEnumerable<ThemeSkin> skins)
        {
            lock (SyncRoot)
            {
                var json = JsonSerializer.Serialize(skins, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SkinsPath, json);
            }
        }

        public static (string SelectedSkinId, bool AutoByTime) LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var settings = JsonSerializer.Deserialize<ThemeSettings>(File.ReadAllText(SettingsPath));
                    if (settings != null)
                    {
                        return (settings.SelectedSkinId ?? string.Empty, settings.AutoByTime);
                    }
                }
            }
            catch (Exception)
            {
                // 忽略损坏的设置文件，回落默认
            }

            return (ThemeSkins.AutoSkinId, true);
        }

        public static void SaveSettings(string selectedSkinId, bool autoByTime)
        {
            lock (SyncRoot)
            {
                var json = JsonSerializer.Serialize(new ThemeSettings
                {
                    SelectedSkinId = selectedSkinId,
                    AutoByTime = autoByTime
                }, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
        }
    }
}
