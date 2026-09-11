using System;
using System.Globalization;
using System.Windows.Data;

namespace NovelManagement.WPF.Localization
{
    /// <summary>
    /// 存储值显示转换器：XAML 中绑定的中文规范存储值（角色类型 / 势力类型 / 品质等级 / 关系类型等）
    /// 在英文界面下映射为英文显示文本；未命中映射时原样返回。
    /// 仅用于展示层（单向），不回写数据。
    /// </summary>
    public sealed class StoredValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stored = value?.ToString();
            return string.IsNullOrEmpty(stored)
                ? stored ?? string.Empty
                : LocalizationManager.LocalizeStoredValue(stored);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException("StoredValueConverter 仅支持单向显示转换。");
    }
}
