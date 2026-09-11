using System;
using System.IO;
using System.Linq;
using NovelManagement.WPF.Localization;
using Xunit;

namespace NovelManagement.Tests;

/// <summary>
/// 本地化词条完整性测试：强制 zh/en 键集一致、文本非空、T() 回退链正确。
/// 新增词条若只写了一列语言，这里会立即失败。
/// </summary>
public class LocalizationTests
{
    [Fact]
    public void ZhAndEn_KeySetsAreIdentical()
    {
        var zh = LocalizationManager.GetZhTable();
        var en = LocalizationManager.GetEnTable();

        var onlyInZh = zh.Keys.Except(en.Keys, StringComparer.Ordinal).ToList();
        var onlyInEn = en.Keys.Except(zh.Keys, StringComparer.Ordinal).ToList();

        Assert.True(onlyInZh.Count == 0, $"仅中文表存在的键：{string.Join(", ", onlyInZh)}");
        Assert.True(onlyInEn.Count == 0, $"仅英文表存在的键：{string.Join(", ", onlyInEn)}");
    }

    [Fact]
    public void Entries_TextIsNeverEmpty()
    {
        foreach (var (key, value) in LocalizationManager.GetZhTable())
        {
            Assert.False(string.IsNullOrWhiteSpace(value), $"键 {key} 的中文文本为空");
        }

        foreach (var (key, value) in LocalizationManager.GetEnTable())
        {
            Assert.False(string.IsNullOrWhiteSpace(value), $"键 {key} 的英文文本为空");
        }
    }

    [Fact]
    public void Initialize_WithEnOverride_CurrentTableIsEnglish()
    {
        var path = Path.Combine(Path.GetTempPath(), $"loc_test_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{\"Localization\":{\"Language\":\"en-US\"}}");
            LocalizationManager.Initialize(path, null);

            Assert.Equal("en-US", LocalizationManager.CurrentLanguage);
            Assert.True(LocalizationManager.IsEnglish);
            Assert.Equal("Dashboard", LocalizationManager.T("Nav.Dashboard"));
            // zh 打底表不受语言切换影响
            Assert.Equal("仪表盘", LocalizationManager.GetZhTable()["Nav.Dashboard"]);
        }
        finally
        {
            // 恢复默认 zh 状态，避免污染其他测试（LocalizationManager 为全局单例）
            LocalizationManager.Initialize(null, null);
            File.Delete(path);
        }
    }

    [Fact]
    public void Initialize_WithZhOverride_CurrentTableIsChinese()
    {
        var path = Path.Combine(Path.GetTempPath(), $"loc_test_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{\"Localization\":{\"Language\":\"zh-CN\"}}");
            LocalizationManager.Initialize(path, null);

            Assert.Equal("zh-CN", LocalizationManager.CurrentLanguage);
            Assert.False(LocalizationManager.IsEnglish);
            Assert.Equal("仪表盘", LocalizationManager.T("Nav.Dashboard"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void T_MissingKey_FallsBackThenKey()
    {
        LocalizationManager.Initialize(null, null);

        // 未注册键：有兜底文案用兜底，无兜底返回键名本身
        Assert.Equal("兜底文案", LocalizationManager.T("No.Such.Key", "兜底文案"));
        Assert.Equal("No.Such.Key", LocalizationManager.T("No.Such.Key"));
    }

    [Fact]
    public void TF_FormatsWithArguments()
    {
        LocalizationManager.Initialize(null, null);

        var zh = LocalizationManager.TF("Common.TotalWords", null, 123);
        Assert.Contains("123", zh);

        var bad = LocalizationManager.TF("No.Such.Key", "共 {0} 条", 5);
        Assert.Equal("共 5 条", bad);
    }

    [Fact]
    public void LocalizeStoredValue_EnMode_MapsKnownStoredValues()
    {
        var path = Path.Combine(Path.GetTempPath(), $"loc_test_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{\"Localization\":{\"Language\":\"en-US\"}}");
            LocalizationManager.Initialize(path, null);

            Assert.Equal("Urban book", LocalizationManager.LocalizeStoredValue("都市书籍"));
            Assert.Equal("In Progress", LocalizationManager.LocalizeStoredValue("进行中"));
            Assert.Equal("All Relationships", LocalizationManager.LocalizeStoredValue("全部关系"));
            Assert.Equal("Long-form book", LocalizationManager.LocalizeStoredValue("长篇书籍"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LocalizeStoredValue_UnknownValue_ReturnsAsIs()
    {
        LocalizationManager.Initialize(null, null);
        Assert.Equal("自定义势力名", LocalizationManager.LocalizeStoredValue("自定义势力名"));
        Assert.Equal(string.Empty, LocalizationManager.LocalizeStoredValue(""));
    }

    [Fact]
    public void LocalizeStoredValue_EnMode_MapsEntityEnumValues()
    {
        var path = Path.Combine(Path.GetTempPath(), $"loc_test_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{\"Localization\":{\"Language\":\"en-US\"}}");
            LocalizationManager.Initialize(path, null);

            // 角色类型
            Assert.Equal("Protagonist", LocalizationManager.LocalizeStoredValue("主角"));
            Assert.Equal("Female Lead", LocalizationManager.LocalizeStoredValue("女主角"));
            Assert.Equal("Mentor", LocalizationManager.LocalizeStoredValue("师父"));
            // 势力类型与等级
            Assert.Equal("Cultivation Sect", LocalizationManager.LocalizeStoredValue("修仙宗门"));
            Assert.Equal("Commercial Guild", LocalizationManager.LocalizeStoredValue("商业组织"));
            Assert.Equal("Supreme", LocalizationManager.LocalizeStoredValue("超级"));
            Assert.Equal("First-Rate", LocalizationManager.LocalizeStoredValue("一流"));
            // 修炼体系品质与境界
            Assert.Equal("Mortal", LocalizationManager.LocalizeStoredValue("凡级"));
            Assert.Equal("Divine", LocalizationManager.LocalizeStoredValue("神级"));
            Assert.Equal("Foundation Building", LocalizationManager.LocalizeStoredValue("筑基期"));
            // 关系类型与状态
            Assert.Equal("Master-Disciple", LocalizationManager.LocalizeStoredValue("师徒关系"));
            Assert.Equal("Master-Disciple", LocalizationManager.LocalizeStoredValue("师徒"));
            Assert.Equal("Stable", LocalizationManager.LocalizeStoredValue("稳定"));
            // 占位
            Assert.Equal("No Faction", LocalizationManager.LocalizeStoredValue("无势力"));
            Assert.Equal("Not set", LocalizationManager.LocalizeStoredValue("未设置"));
        }
        finally
        {
            LocalizationManager.Initialize(null, null);
            File.Delete(path);
        }
    }

    [Fact]
    public void FactionPanelLabels_EnMode_AreEnglish()
    {
        var path = Path.Combine(Path.GetTempPath(), $"loc_test_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{\"Localization\":{\"Language\":\"en-US\"}}");
            LocalizationManager.Initialize(path, null);

            Assert.Equal("Territory", LocalizationManager.T("FM.FieldTerritory"));
            Assert.Equal("Headquarters", LocalizationManager.T("FM.FieldHeadquarters"));
            Assert.Equal("Members", LocalizationManager.T("FM.FieldMemberCount"));
            Assert.Equal("Resources", LocalizationManager.T("FM.FieldResources"));
            Assert.Equal("Back to Statistics", LocalizationManager.T("FM.BackToStats"));
            Assert.Equal("Faction List", LocalizationManager.T("FM.List"));
            Assert.Equal("Edit Faction", LocalizationManager.T("FM.EditTitle"));
            Assert.Equal("Allies", LocalizationManager.T("FM.InfoAllies"));
            Assert.Equal("Hostile Factions", LocalizationManager.T("FM.InfoEnemies"));
            Assert.Equal("Description", LocalizationManager.T("SV.InfoDescription"));
            Assert.Equal("Status: Stable", LocalizationManager.TF("RN.StatusFmt", null, LocalizationManager.LocalizeStoredValue("稳定")));
        }
        finally
        {
            LocalizationManager.Initialize(null, null);
            File.Delete(path);
        }
    }

    [Fact]
    public void ThemeAndProjectList_EnMode_AreEnglish()
    {
        var path = Path.Combine(Path.GetTempPath(), $"loc_test_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{\"Localization\":{\"Language\":\"en-US\"}}");
            LocalizationManager.Initialize(path, null);

            // 主题皮肤名称与描述
            Assert.Equal("Daylight (Light)", LocalizationManager.T("TS.SkinLight"));
            Assert.Equal("Nightfall (Dark)", LocalizationManager.T("TS.SkinDark"));
            Assert.Equal("Pink Blossom", LocalizationManager.T("TS.SkinPink"));
            Assert.Equal("Built-in", LocalizationManager.T("TS.KindBuiltin"));
            Assert.Equal("Light tone", LocalizationManager.T("TS.ToneLight"));
            Assert.Equal("Dark tone", LocalizationManager.T("TS.ToneDark"));
            // 皮肤颜色项标签
            Assert.Equal("Panel background", LocalizationManager.T("TS.C.AppPanelBackgroundBrush"));
            Assert.Equal("Title bar text", LocalizationManager.T("TS.C.AppTitleBarForegroundBrush"));
            Assert.Equal("Table alternating row background", LocalizationManager.T("TS.C.DataGridAltRowBackgroundBrush"));
            Assert.Equal("Panel background", NovelManagement.WPF.Services.ThemeSkinBrushKeys.LocalizeBrushLabel("AppPanelBackgroundBrush"));
            // 项目列表状态与相对时间
            Assert.Equal("In Progress", LocalizationManager.LocalizeStoredValue("进行中"));
            Assert.Equal("11 min ago", LocalizationManager.TF("VM.MinutesAgo", null, 11));
            Assert.Equal("Just now", LocalizationManager.T("PM.JustNow"));
        }
        finally
        {
            LocalizationManager.Initialize(null, null);
            File.Delete(path);
        }
    }
}
