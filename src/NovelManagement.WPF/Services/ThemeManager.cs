using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Serilog;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 主题皮肤管理器。
    /// 支持三种内置皮肤（白昼/黑夜/红粉花漾少女风）与用户自定义皮肤；
    /// 可跟随系统时间自动切换（19:00–07:00 黑夜）；检测到女频文时自动临时切换为红粉花漾少女风。
    /// 切换内容：MaterialDesign Light/Dark 资源字典 + HandyControl Skin + 画刷覆盖（含窗口框架颜色）。
    /// </summary>
    public static class ThemeManager
    {
        // 黑夜主题自动切换时段（本地时间，24 小时制）：[19:00, 次日 07:00)
        public const int DarkStartHour = 19;
        public const int DarkEndHour = 7;

        private static ILogger? _logger;
        private static string _selectedSkinId = ThemeSkins.AutoSkinId;
        private static bool _femaleModeActive;
        private static string? _skinIdBeforeFemaleMode;
        private static ThemeSkin? _currentSkin;

        /// <summary>当前已应用皮肤的 Id（未知时为所选皮肤 Id）。</summary>
        public static string CurrentSkinId => _currentSkin?.Id ?? _selectedSkinId;

        /// <summary>女频模式是否激活（激活时临时使用红粉花漾少女风皮肤）。</summary>
        public static bool IsFemaleModeActive => _femaleModeActive;

        public static void Initialize(ILogger? logger)
        {
            _logger = logger;
            var (skinId, _) = SkinStore.LoadSettings();
            _selectedSkinId = string.IsNullOrWhiteSpace(skinId) ? ThemeSkins.AutoSkinId : skinId;
            _femaleModeActive = false;
            _skinIdBeforeFemaleMode = null;
        }

        /// <summary>
        /// 当前本地时间是否应使用黑夜主题（DateTime.Now 已按系统时区换算）。
        /// </summary>
        public static bool IsDarkTime()
        {
            var hour = DateTime.Now.Hour;
            return hour >= DarkStartHour || hour < DarkEndHour;
        }

        /// <summary>
        /// 启动应用主题：用户自选皮肤优先，否则按时间自动切换。
        /// </summary>
        public static void ApplyStartupTheme()
        {
            if (_selectedSkinId != ThemeSkins.AutoSkinId && TryGetSkin(_selectedSkinId, out var skin))
            {
                ApplySkin(skin);
                return;
            }

            ApplyByTime();
        }

        /// <summary>
        /// 按当前时间应用主题（仅「跟随时间自动」模式且未进入女频模式时生效；状态不变时不重复应用）。
        /// </summary>
        public static void ApplyByTime()
        {
            if (_selectedSkinId != ThemeSkins.AutoSkinId || _femaleModeActive)
            {
                return;
            }

            var targetId = IsDarkTime() ? ThemeSkins.DarkSkinId : ThemeSkins.LightSkinId;
            if (string.Equals(CurrentSkinId, targetId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ApplySkin(IsDarkTime() ? ThemeSkins.CreateDarkSkin() : ThemeSkins.CreateLightSkin());
        }

        /// <summary>
        /// 设置用户所选皮肤并持久化（传 <see cref="ThemeSkins.AutoSkinId"/> 表示恢复跟随时间自动切换）。
        /// </summary>
        public static void SetSelectedSkin(string skinId)
        {
            _skinIdBeforeFemaleMode = null;
            if (string.IsNullOrWhiteSpace(skinId) || skinId == ThemeSkins.AutoSkinId)
            {
                _selectedSkinId = ThemeSkins.AutoSkinId;
                _femaleModeActive = false;
                SkinStore.SaveSettings(ThemeSkins.AutoSkinId, true);
                ApplyByTime();
                return;
            }

            if (!TryGetSkin(skinId, out var skin))
            {
                return;
            }

            _selectedSkinId = skin.Id;
            _femaleModeActive = false;
            SkinStore.SaveSettings(skin.Id, false);
            ApplySkin(skin);
        }

        /// <summary>
        /// 依据书籍类型/简介等文本自动应用女频换肤：命中女频关键词 → 红粉花漾少女风，否则恢复用户所选。
        /// </summary>
        public static void ApplyNovelGenre(string? text)
        {
            SetFemaleMode(FemaleNovelDetector.IsFemaleOriented(text));
        }

        /// <summary>
        /// 进入/退出女频模式：进入时临时切换红粉花漾少女风皮肤，退出时恢复用户所选皮肤。
        /// </summary>
        public static void SetFemaleMode(bool active)
        {
            if (active == _femaleModeActive)
            {
                return;
            }

            _femaleModeActive = active;
            if (active)
            {
                _skinIdBeforeFemaleMode = _selectedSkinId;
                _logger?.Information("检测到女频文，自动切换为「红粉花漾少女风」皮肤");
                ApplySkin(ThemeSkins.CreatePinkSkin());
                return;
            }

            _selectedSkinId = string.IsNullOrWhiteSpace(_skinIdBeforeFemaleMode)
                ? ThemeSkins.AutoSkinId
                : _skinIdBeforeFemaleMode;
            _skinIdBeforeFemaleMode = null;
            _logger?.Information("退出女频模式，恢复主题皮肤 {SkinId}", _selectedSkinId);
            if (_selectedSkinId == ThemeSkins.AutoSkinId)
            {
                ApplyByTime();
            }
            else if (TryGetSkin(_selectedSkinId, out var skin))
            {
                ApplySkin(skin);
            }
        }

        /// <summary>
        /// 获取全部皮肤（3 种内置 + 自定义）。
        /// </summary>
        public static IReadOnlyList<ThemeSkin> GetAllSkins()
        {
            var list = new List<ThemeSkin>
            {
                ThemeSkins.CreateLightSkin(),
                ThemeSkins.CreateDarkSkin(),
                ThemeSkins.CreatePinkSkin()
            };
            list.AddRange(SkinStore.LoadCustomSkins());
            return list;
        }

        /// <summary>
        /// 按 Id 查找皮肤（内置 + 自定义）。
        /// </summary>
        public static bool TryGetSkin(string skinId, out ThemeSkin skin)
        {
            skin = GetAllSkins().FirstOrDefault(s => string.Equals(s.Id, skinId, StringComparison.OrdinalIgnoreCase))
                   ?? new ThemeSkin();
            return skin.Id == skinId;
        }

        /// <summary>
        /// 新增或更新自定义皮肤并持久化（内置皮肤不可覆盖）。
        /// </summary>
        public static void SaveCustomSkin(ThemeSkin skin)
        {
            if (skin == null || string.IsNullOrWhiteSpace(skin.Id))
            {
                return;
            }

            skin.IsBuiltin = false;
            var customs = SkinStore.LoadCustomSkins();
            var index = customs.FindIndex(s => string.Equals(s.Id, skin.Id, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                customs[index] = skin;
            }
            else
            {
                customs.Add(skin);
            }

            SkinStore.SaveCustomSkins(customs);
            _logger?.Information("自定义皮肤已保存：{SkinName}（{SkinId}）", skin.Name, skin.Id);

            if (_currentSkin != null && string.Equals(_currentSkin.Id, skin.Id, StringComparison.OrdinalIgnoreCase))
            {
                ApplySkin(skin);
            }
        }

        /// <summary>
        /// 删除自定义皮肤；若删除的是当前使用中的皮肤则回落到跟随时间自动切换。
        /// </summary>
        public static bool DeleteCustomSkin(string skinId)
        {
            var customs = SkinStore.LoadCustomSkins();
            var target = customs.FirstOrDefault(s => string.Equals(s.Id, skinId, StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                return false;
            }

            customs.Remove(target);
            SkinStore.SaveCustomSkins(customs);
            _logger?.Information("自定义皮肤已删除：{SkinName}（{SkinId}）", target.Name, target.Id);

            if (_currentSkin != null && string.Equals(_currentSkin.Id, skinId, StringComparison.OrdinalIgnoreCase))
            {
                _currentSkin = null;
                SetSelectedSkin(ThemeSkins.AutoSkinId);
            }

            return true;
        }

        /// <summary>
        /// 应用指定皮肤（线程安全：后台线程调用时自动切换到 UI 线程执行）。
        /// </summary>
        public static void ApplySkin(ThemeSkin skin)
        {
            if (skin == null)
            {
                return;
            }

            var app = System.Windows.Application.Current;
            if (app != null && !app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.Invoke(() => ApplySkin(skin));
                return;
            }

            try
            {
                // 缺省画刷回落到同基调的内置皮肤值
                var baseSkin = skin.IsDark ? ThemeSkins.CreateDarkSkin() : ThemeSkins.CreateLightSkin();
                var uri = skin.IsDark ? "MaterialDesignTheme.Dark.xaml" : "MaterialDesignTheme.Light.xaml";
                var hcSkin = skin.IsDark ? "SkinDark.xaml" : "SkinDefault.xaml";
                ReplaceMergedDictionary("MaterialDesignTheme", uri);
                ReplaceMergedDictionary("Skin", hcSkin);

                var resources = app?.Resources;
                if (resources != null)
                {
                    foreach (var (key, _) in ThemeSkinBrushKeys.All)
                    {
                        var hex = skin.Colors.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                            ? value
                            : baseSkin.Colors.TryGetValue(key, out var fallback) ? fallback : null;
                        if (string.IsNullOrWhiteSpace(hex))
                        {
                            continue;
                        }

                        resources[key] = Brush(hex);
                    }
                }

                _currentSkin = skin;
                ApplyWindowFrame(skin);
                _logger?.Information("主题皮肤已应用：{SkinName}（{SkinKind}·{Tone}）", skin.Name, skin.SkinKind, skin.ToneText);
            }
            catch (Exception ex)
            {
                _logger?.Warning(ex, "应用主题皮肤失败：{SkinName}", skin.Name);
            }
        }

        private static SolidColorBrush Brush(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }

        // ---- 窗口原生外框（系统标题栏/边框）随皮肤深浅：黑夜深色、白昼跟随皮肤标题栏色 ----
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_CAPTION_COLOR = 35;
        private static bool _windowFrameClassHandlerRegistered;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int sizeOfValue);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_NCACTIVATE = 0x0086;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;

        /// <summary>
        /// 对当前所有已打开窗口应用原生外框颜色，并注册类级处理器保证之后新开窗口（对话框等）同样生效。
        /// </summary>
        private static void ApplyWindowFrame(ThemeSkin skin)
        {
            EnsureWindowFrameClassHandler();
            var app = System.Windows.Application.Current;
            if (app == null)
            {
                return;
            }

            var titleColor = ParseColor(GetSkinColor(skin, "AppTitleBarBackgroundBrush"));
            foreach (var window in app.Windows.OfType<Window>().ToArray())
            {
                ApplyWindowFrameCore(window, skin.IsDark, titleColor);
            }
        }

        private static void EnsureWindowFrameClassHandler()
        {
            if (_windowFrameClassHandlerRegistered)
            {
                return;
            }

            _windowFrameClassHandlerRegistered = true;
            // Loaded：句柄已存在，配合下方强制非客户区重绘，Win10 上深色标题栏可靠生效
            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
                new RoutedEventHandler((sender, _) =>
                {
                    if (sender is Window window && _currentSkin != null)
                    {
                        ApplyWindowFrameCore(window, _currentSkin.IsDark, ParseColor(GetSkinColor(_currentSkin, "AppTitleBarBackgroundBrush")));
                    }
                }));
        }

        private static void ApplyWindowFrameCore(Window window, bool isDark, Color? titleBarColor)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero)
                {
                    return;
                }

                // 深色基调：原生标题栏/边框走系统深色（Win10 旧版本回退属性 19）
                var dark = isDark ? 1 : 0;
                var hr20 = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
                var hrApplied = hr20 == 0 ? DWMWA_USE_IMMERSIVE_DARK_MODE : -1;
                if (hr20 != 0)
                {
                    var hr19 = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref dark, sizeof(int));
                    hrApplied = hr19 == 0 ? DWMWA_USE_IMMERSIVE_DARK_MODE_OLD : -1;
                }

                // Win11 支持直接指定标题栏/边框颜色与皮肤标题栏一致；Win10 返回错误码忽略
                var hrCaption = 0;
                var hrBorder = 0;
                if (titleBarColor.HasValue)
                {
                    var colorRef = titleBarColor.Value.R | (titleBarColor.Value.G << 8) | (titleBarColor.Value.B << 16);
                    hrCaption = DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref colorRef, sizeof(int));
                    hrBorder = DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref colorRef, sizeof(int));
                }

                _logger?.Information(
                    "窗口原生外框应用：hwnd={Hwnd} isDark={IsDark} immersiveAttr={Attr} hrCaption={HrCaption} hrBorder={HrBorder} title={Title}",
                    hwnd, isDark, hrApplied, hrCaption, hrBorder, window.Title);

                // 窗口已显示时强制非客户区重绘，否则 Win10 上深色标题栏不立即生效
                if (window.IsVisible)
                {
                    SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
                    SendMessage(hwnd, WM_NCACTIVATE, IntPtr.Zero, IntPtr.Zero);
                    SendMessage(hwnd, WM_NCACTIVATE, (IntPtr)1, IntPtr.Zero);
                }
            }
            catch (Exception ex)
            {
                _logger?.Information(ex, "应用窗口原生外框颜色失败");
            }
        }

        private static string? GetSkinColor(ThemeSkin skin, string key)
        {
            return skin.Colors.TryGetValue(key, out var hex) ? hex : null;
        }

        private static Color? ParseColor(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                return null;
            }

            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 替换合并资源字典中匹配前缀的 Source（保持字典顺序，避免依赖关系断裂）。
        /// </summary>
        private static void ReplaceMergedDictionary(string sourceNamePrefix, string newFileName)
        {
            var dictionaries = System.Windows.Application.Current?.Resources.MergedDictionaries;
            if (dictionaries == null)
            {
                return;
            }

            for (var i = 0; i < dictionaries.Count; i++)
            {
                var source = dictionaries[i].Source?.OriginalString;
                if (source == null || !source.Contains(sourceNamePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var newSource = source;
                if (sourceNamePrefix == "MaterialDesignTheme")
                {
                    newSource = System.Text.RegularExpressions.Regex.Replace(
                        source, "MaterialDesignTheme\\.(Dark|Light)\\.xaml", newFileName);
                }
                else if (sourceNamePrefix == "Skin")
                {
                    newSource = System.Text.RegularExpressions.Regex.Replace(
                        source, "Skin(Dark|Default)\\.xaml", newFileName);
                }

                if (!string.Equals(newSource, source, StringComparison.OrdinalIgnoreCase))
                {
                    dictionaries[i] = new ResourceDictionary { Source = new Uri(newSource) };
                }

                return;
            }
        }
    }
}
