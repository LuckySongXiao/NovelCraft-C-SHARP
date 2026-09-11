using System.Collections.Generic;

namespace NovelManagement.WPF.Localization
{
    /// <summary>
    /// 词条表：共享对话框与全局消息（所有视图复用的通用词条）。
    /// 键前缀约定：Dlg.* 通用控件文案；Msg.* 通用弹窗消息。
    /// 各视图专属词条必须使用视图前缀（如 PM.* / CM.*），禁止在此文件添加视图专属词条。
    /// </summary>
    internal static class StringsDialogs
    {
        internal static readonly IReadOnlyDictionary<string, LocEntry> Entries = new Dictionary<string, LocEntry>
        {
            // ---- 通用控件文案 ----
            ["Dlg.Save"] = new("保存", "Save"),
            ["Dlg.Cancel"] = new("取消", "Cancel"),
            ["Dlg.Close"] = new("关闭", "Close"),
            ["Dlg.Delete"] = new("删除", "Delete"),
            ["Dlg.Edit"] = new("编辑", "Edit"),
            ["Dlg.Add"] = new("添加", "Add"),
            ["Dlg.Create"] = new("创建", "Create"),
            ["Dlg.Update"] = new("更新", "Update"),
            ["Dlg.Confirm"] = new("确认", "Confirm"),
            ["Dlg.OK"] = new("确定", "OK"),
            ["Dlg.Yes"] = new("是", "Yes"),
            ["Dlg.No"] = new("否", "No"),
            ["Dlg.Search"] = new("搜索", "Search"),
            ["Dlg.Refresh"] = new("刷新", "Refresh"),
            ["Dlg.Export"] = new("导出", "Export"),
            ["Dlg.Import"] = new("导入", "Import"),
            ["Dlg.Generate"] = new("生成", "Generate"),
            ["Dlg.Stop"] = new("停止", "Stop"),
            ["Dlg.Details"] = new("详情", "Details"),
            ["Dlg.Apply"] = new("应用", "Apply"),
            ["Dlg.Browse"] = new("浏览…", "Browse…"),
            ["Dlg.SelectAll"] = new("全选", "Select All"),
            ["Dlg.Clear"] = new("清空", "Clear"),
            ["Dlg.Name"] = new("名称", "Name"),
            ["Dlg.Title"] = new("标题", "Title"),
            ["Dlg.Type"] = new("类型", "Type"),
            ["Dlg.Status"] = new("状态", "Status"),
            ["Dlg.Description"] = new("描述", "Description"),
            ["Dlg.Remarks"] = new("备注", "Remarks"),
            ["Dlg.Content"] = new("内容", "Content"),
            ["Dlg.Category"] = new("分类", "Category"),
            ["Dlg.Count"] = new("数量", "Count"),
            ["Dlg.Date"] = new("日期", "Date"),
            ["Dlg.Author"] = new("作者", "Author"),
            ["Dlg.Source"] = new("来源", "Source"),
            ["Dlg.Target"] = new("目标", "Target"),
            ["Dlg.Option"] = new("选项", "Options"),
            ["Dlg.Settings"] = new("设置", "Settings"),
            ["Dlg.Preview"] = new("预览", "Preview"),
            ["Dlg.Copy"] = new("复制", "Copy"),
            ["Dlg.Rename"] = new("重命名", "Rename"),
            ["Dlg.MoveNext"] = new("下一步", "Next"),
            ["Dlg.MovePrev"] = new("上一步", "Previous"),
            ["Dlg.Finish"] = new("完成", "Finish"),
            ["Dlg.Reset"] = new("重置", "Reset"),
            ["Dlg.Enabled"] = new("启用", "Enabled"),
            ["Dlg.Disabled"] = new("禁用", "Disabled"),
            ["Dlg.SearchPlaceholder"] = new("输入关键词搜索…", "Type to search…"),
            ["Dlg.SelectPlaceholder"] = new("请选择", "Select"),

            // ---- 通用弹窗标题/消息 ----
            ["Msg.Tip"] = new("提示", "Tip"),
            ["Msg.Error"] = new("错误", "Error"),
            ["Msg.Warning"] = new("警告", "Warning"),
            ["Msg.Info"] = new("信息", "Information"),
            ["Msg.Success"] = new("成功", "Success"),
            ["Msg.Failed"] = new("失败", "Failed"),
            ["Msg.Loading"] = new("加载中…", "Loading…"),
            ["Msg.Processing"] = new("处理中…", "Processing…"),
            ["Msg.PleaseWait"] = new("请稍候…", "Please wait…"),
            ["Msg.RequiredField"] = new("必填项不能为空", "Required fields cannot be empty"),
            ["Msg.DeleteConfirmTitle"] = new("删除确认", "Confirm Deletion"),
            ["Msg.DeleteSuccess"] = new("删除成功", "Deleted successfully"),
            ["Msg.SaveConfirm"] = new("是否保存当前更改？", "Save current changes?"),
            ["Msg.UnsavedChanges"] = new("有未保存的更改", "There are unsaved changes"),
            ["Msg.NetworkError"] = new("网络请求失败，请检查网络连接", "Network request failed. Please check your connection"),
            ["Msg.Retry"] = new("重试", "Retry"),
            ["Msg.Done"] = new("完成", "Done"),
            ["Msg.NoData"] = new("暂无数据", "No data"),
            ["Msg.EmptyList"] = new("列表为空", "The list is empty"),
            ["Msg.InvalidInput"] = new("输入无效，请检查后重试", "Invalid input. Please check and try again"),
            ["NPD.TypeFantasy"] = new("奇幻书籍", "Fantasy book"),
            ["NPD.TypeMilitary"] = new("军事书籍", "Military book"),
            ["NPD.StdTemplate"] = new("标准模板", "Standard Template"),
            ["NVD.TypeMain"] = new("主线剧情", "Main Plot"),
            ["NVD.TypeSub"] = new("支线剧情", "Subplot"),
            ["NVD.TypeExtra"] = new("番外篇", "Extra"),
            ["NVD.TypePrologue"] = new("序章", "Prologue"),
            ["NVD.TypeFinale"] = new("终章", "Finale"),
            ["NCD.SampleVol1"] = new("第一卷：面具觉醒", "Volume 1: The Mask Awakens"),
            ["NCD.SampleVol2"] = new("第二卷：力量觉醒", "Volume 2: The Power Awakens"),
            ["NCD.SampleVol3"] = new("第三卷：天劫降临", "Volume 3: The Tribulation Descends"),
        };
    }
}
