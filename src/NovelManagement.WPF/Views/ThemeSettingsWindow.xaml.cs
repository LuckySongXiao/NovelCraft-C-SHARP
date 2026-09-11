using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using NovelManagement.WPF.Localization;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// ThemeSettingsWindow.xaml 的交互逻辑：主题配置界面（皮肤列表/应用/自定义皮肤编辑，含窗口框架颜色）。
    /// </summary>
    public partial class ThemeSettingsWindow : Window
    {
        private readonly ObservableCollection<ThemeSkin> _skins = new();
        private ThemeSkin? _editingSkin;

        public ThemeSettingsWindow()
        {
            InitializeComponent();
            ReloadSkins();
        }

        private ThemeSkin? SelectedSkin => SkinListBox.SelectedItem as ThemeSkin;

        /// <summary>重新加载皮肤列表并定位当前使用中的皮肤。</summary>
        private void ReloadSkins(ThemeSkin? select = null)
        {
            _skins.Clear();
            foreach (var skin in ThemeManager.GetAllSkins())
            {
                _skins.Add(skin);
            }

            var target = select ?? _skins.FirstOrDefault(s => s.Id == ThemeManager.CurrentSkinId)
                         ?? _skins.FirstOrDefault();
            SkinListBox.ItemsSource = _skins;
            SkinListBox.SelectedItem = target;
            UpdatePreview();
        }

        private void SkinListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var skin = SelectedSkin;
            var isCurrent = skin != null && skin.Id == ThemeManager.CurrentSkinId;
            SkinTitleText.Text = skin?.Name ?? LocalizationManager.T("TS.SelectSkin", "请选择皮肤");
            SkinInfoText.Text = skin == null
                ? string.Empty
                : $"{skin.SkinKind} · {skin.ToneText}{(isCurrent ? $" · {LocalizationManager.T("TS.CurrentlyActive", "当前使用中")}" : string.Empty)}{(ThemeManager.IsFemaleModeActive ? $" · {LocalizationManager.T("TS.FemaleModeActive", "女频模式生效中")}" : string.Empty)}";

            var entries = new List<ColorEntry>();
            if (skin != null)
            {
                var baseSkin = skin.IsDark ? ThemeSkins.CreateDarkSkin() : ThemeSkins.CreateLightSkin();
                foreach (var (key, label) in ThemeSkinBrushKeys.All)
                {
                    var hex = skin.Colors.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                        ? value
                        : baseSkin.Colors.TryGetValue(key, out var fallback) ? fallback : "#808080";
                    entries.Add(new ColorEntry { Key = key, Label = ThemeSkinBrushKeys.LocalizeBrushLabel(key), Hex = hex });
                }
            }

            ColorPreviewList.ItemsSource = entries;
            EditSkinButton.IsEnabled = skin != null && !skin.IsBuiltin;
            DeleteSkinButton.IsEnabled = skin != null && !skin.IsBuiltin;
            ModeStateText.Text = LocalizationManager.TF("TS.CurrentSkinFmt", "当前皮肤：{0}",
                ThemeManager.IsFemaleModeActive ? LocalizationManager.T("TS.FemaleSkinName", "红粉花漾少女风（女频自动）") : SkinTitleText.Text);
        }

        private void ApplySkin_Click(object sender, RoutedEventArgs e)
        {
            var skin = SelectedSkin;
            if (skin == null)
            {
                return;
            }

            ThemeManager.SetSelectedSkin(skin.Id);
            UpdatePreview();
        }

        private void AutoByTime_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.SetSelectedSkin(ThemeSkins.AutoSkinId);
            ReloadSkins();
            MessageBox.Show(this, LocalizationManager.T("TS.AutoTimeRestored", "已恢复跟随时间自动切换：19:00–07:00 使用黑夜主题，其余时间白昼主题。"),
                LocalizationManager.T("TS.AutoByTime", "跟随时间自动切换"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddCustomSkin_Click(object sender, RoutedEventArgs e)
        {
            var baseSkin = SelectedSkin
                ?? (ThemeManager.IsDarkTime() ? ThemeSkins.CreateDarkSkin() : ThemeSkins.CreateLightSkin());
            var skin = baseSkin.Clone();
            skin.Id = $"custom-{Guid.NewGuid():N}";
            skin.Name = LocalizationManager.TF("TS.CustomSuffix", "{0}·自定义", baseSkin.Name);
            skin.IsBuiltin = false;

            _editingSkin = skin;
            OpenEditor();
        }

        private void EditSkin_Click(object sender, RoutedEventArgs e)
        {
            var skin = SelectedSkin;
            if (skin == null || skin.IsBuiltin)
            {
                return;
            }

            _editingSkin = skin.Clone();
            OpenEditor();
        }

        private void DeleteSkin_Click(object sender, RoutedEventArgs e)
        {
            var skin = SelectedSkin;
            if (skin == null || skin.IsBuiltin)
            {
                return;
            }

            if (MessageBox.Show(this, LocalizationManager.TF("TS.DeleteCustomConfirm", "确定删除自定义皮肤「{0}」？", skin.Name), LocalizationManager.T("Msg.DeleteConfirmTitle", "删除确认"),
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            ThemeManager.DeleteCustomSkin(skin.Id);
            ReloadSkins();
        }

        private void OpenEditor()
        {
            if (_editingSkin == null)
            {
                return;
            }

            SkinNameTextBox.Text = _editingSkin.Name;
            SkinIsDarkCheckBox.IsChecked = _editingSkin.IsDark;

            var baseSkin = _editingSkin.IsDark ? ThemeSkins.CreateDarkSkin() : ThemeSkins.CreateLightSkin();
            var entries = new List<ColorEntry>();
            foreach (var (key, label) in ThemeSkinBrushKeys.All)
            {
                var hex = _editingSkin.Colors.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                    ? value
                    : baseSkin.Colors.TryGetValue(key, out var fallback) ? fallback : "#808080";
                entries.Add(new ColorEntry { Key = key, Label = label, Hex = hex });
            }

            ColorEditList.ItemsSource = entries;
            EditorPanel.Visibility = Visibility.Visible;
        }

        private void CloseEditor()
        {
            EditorPanel.Visibility = Visibility.Collapsed;
            _editingSkin = null;
        }

        private void SaveSkin_Click(object sender, RoutedEventArgs e)
        {
            if (_editingSkin == null || ColorEditList.ItemsSource is not List<ColorEntry> entries)
            {
                return;
            }

            var name = SkinNameTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this, LocalizationManager.T("TS.NameRequired", "请输入皮肤名称"), LocalizationManager.T("TS.ValidationFailed", "验证失败"), MessageBoxButton.OK, MessageBoxImage.Warning);
                SkinNameTextBox.Focus();
                return;
            }

            foreach (var entry in entries)
            {
                if (!TryParseHexColor(entry.Hex, out _))
                {
                    MessageBox.Show(this, LocalizationManager.TF("TS.InvalidColor", "「{0}」的颜色值无效：{1}\n请使用 #RRGGBB 或 #AARRGGBB 格式。", entry.Label, entry.Hex),
                        LocalizationManager.T("TS.ValidationFailed", "验证失败"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            _editingSkin.Name = name;
            _editingSkin.IsDark = SkinIsDarkCheckBox.IsChecked == true;
            _editingSkin.Colors = ThemeSkinBrushKeys.BuildColorSet(
                key => entries.FirstOrDefault(x => x.Key == key)?.Hex, null);
            ThemeManager.SaveCustomSkin(_editingSkin);

            CloseEditor();
            ReloadSkins(_editingSkin);
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            CloseEditor();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>解析 #RRGGBB / #AARRGGBB 颜色值。</summary>
        private static bool TryParseHexColor(string? hex, out Color color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(hex))
            {
                return false;
            }

            try
            {
                color = (Color)ColorConverter.ConvertFromString(hex.Trim());
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }

    /// <summary>皮肤颜色条目（预览与编辑共用）。</summary>
    public class ColorEntry
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Hex { get; set; } = "#FFFFFF";
    }

    /// <summary>Hex 字符串 → SolidColorBrush 转换器（无效值回落透明）。</summary>
    public class HexToBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
            {
                try
                {
                    var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex.Trim()));
                    brush.Freeze();
                    return brush;
                }
                catch (FormatException)
                {
                    // 无效颜色值时显示透明，由保存时校验提示
                }
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
