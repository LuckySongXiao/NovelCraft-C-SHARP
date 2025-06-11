using System.Collections.Concurrent;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Services.ThinkingChain.Models;
using NovelManagement.AI.Services.ThinkingChain;
using NovelManagement.WPF.Controls.ThinkingChain;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 悬浮文本管理器
    /// </summary>
    public class FloatingTextManager : INotifyPropertyChanged
    {
        private readonly ILogger<FloatingTextManager> _logger;
        private readonly IThinkingChainProcessor _thinkingChainProcessor;
        private readonly ConcurrentDictionary<string, ThinkingChainWindow> _activeWindows;
        private readonly DispatcherTimer _cleanupTimer;
        private bool _isEnabled = true;
        private FloatingTextPosition _defaultPosition = FloatingTextPosition.TopRight;
        private double _opacity = 0.9;
        private int _maxActiveWindows = 3;

        /// <summary>
        /// 是否启用悬浮文本
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                    
                    if (!_isEnabled)
                    {
                        HideAllWindows();
                    }
                }
            }
        }

        /// <summary>
        /// 默认位置
        /// </summary>
        public FloatingTextPosition DefaultPosition
        {
            get => _defaultPosition;
            set
            {
                if (_defaultPosition != value)
                {
                    _defaultPosition = value;
                    OnPropertyChanged(nameof(DefaultPosition));
                }
            }
        }

        /// <summary>
        /// 透明度
        /// </summary>
        public double Opacity
        {
            get => _opacity;
            set
            {
                var newValue = Math.Max(0.1, Math.Min(1.0, value));
                if (Math.Abs(_opacity - newValue) > 0.01)
                {
                    _opacity = newValue;
                    OnPropertyChanged(nameof(Opacity));
                    UpdateAllWindowsOpacity();
                }
            }
        }

        /// <summary>
        /// 最大活动窗口数
        /// </summary>
        public int MaxActiveWindows
        {
            get => _maxActiveWindows;
            set
            {
                var newValue = Math.Max(1, Math.Min(10, value));
                if (_maxActiveWindows != newValue)
                {
                    _maxActiveWindows = newValue;
                    OnPropertyChanged(nameof(MaxActiveWindows));
                    EnforceMaxWindows();
                }
            }
        }

        /// <summary>
        /// 当前活动窗口数
        /// </summary>
        public int ActiveWindowCount => _activeWindows.Count;

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 窗口创建事件
        /// </summary>
        public event EventHandler<ThinkingChainWindow>? WindowCreated;

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        public event EventHandler<string>? WindowClosed;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="thinkingChainProcessor">思维链处理器</param>
        public FloatingTextManager(
            ILogger<FloatingTextManager> logger,
            IThinkingChainProcessor thinkingChainProcessor)
        {
            _logger = logger;
            _thinkingChainProcessor = thinkingChainProcessor;
            _activeWindows = new ConcurrentDictionary<string, ThinkingChainWindow>();

            // 设置清理定时器
            _cleanupTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };
            _cleanupTimer.Tick += CleanupTimer_Tick;
            _cleanupTimer.Start();

            _logger.LogInformation("悬浮文本管理器已初始化");
        }

        /// <summary>
        /// 显示思维链
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        /// <param name="position">显示位置</param>
        /// <returns>窗口ID</returns>
        public string ShowThinkingChain(NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain thinkingChain, FloatingTextPosition? position = null)
        {
            if (!IsEnabled)
            {
                _logger.LogDebug("悬浮文本已禁用，跳过显示");
                return string.Empty;
            }

            try
            {
                var windowId = thinkingChain.Id.ToString();
                
                // 检查是否已存在
                if (_activeWindows.ContainsKey(windowId))
                {
                    _logger.LogDebug("思维链窗口已存在: {WindowId}", windowId);
                    return windowId;
                }

                // 强制执行最大窗口数限制
                EnforceMaxWindows();

                // 创建新窗口
                var window = CreateThinkingChainWindow(thinkingChain, position ?? DefaultPosition);
                
                if (_activeWindows.TryAdd(windowId, window))
                {
                    // 显示窗口
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        window.Show();
                        PositionWindow(window, position ?? DefaultPosition);
                    });

                    WindowCreated?.Invoke(this, window);
                    OnPropertyChanged(nameof(ActiveWindowCount));

                    _logger.LogDebug("思维链窗口已创建: {WindowId}", windowId);
                    return windowId;
                }
                else
                {
                    window.Close();
                    _logger.LogWarning("无法添加思维链窗口: {WindowId}", windowId);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示思维链窗口失败");
                return string.Empty;
            }
        }

        /// <summary>
        /// 隐藏思维链窗口
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        public void HideThinkingChain(string windowId)
        {
            try
            {
                if (_activeWindows.TryRemove(windowId, out var window))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        window.Close();
                    });

                    WindowClosed?.Invoke(this, windowId);
                    OnPropertyChanged(nameof(ActiveWindowCount));

                    _logger.LogDebug("思维链窗口已关闭: {WindowId}", windowId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "隐藏思维链窗口失败: {WindowId}", windowId);
            }
        }

        /// <summary>
        /// 隐藏所有窗口
        /// </summary>
        public void HideAllWindows()
        {
            try
            {
                var windowIds = _activeWindows.Keys.ToList();
                foreach (var windowId in windowIds)
                {
                    HideThinkingChain(windowId);
                }

                _logger.LogDebug("所有思维链窗口已关闭");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "隐藏所有窗口失败");
            }
        }

        /// <summary>
        /// 更新思维链
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        public void UpdateThinkingChain(NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain thinkingChain)
        {
            var windowId = thinkingChain.Id.ToString();
            
            if (_activeWindows.TryGetValue(windowId, out var window))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    window.UpdateThinkingChain(thinkingChain);
                });
            }
        }

        /// <summary>
        /// 获取窗口
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <returns>窗口对象</returns>
        public ThinkingChainWindow? GetWindow(string windowId)
        {
            _activeWindows.TryGetValue(windowId, out var window);
            return window;
        }

        /// <summary>
        /// 获取所有活动窗口
        /// </summary>
        /// <returns>窗口列表</returns>
        public List<ThinkingChainWindow> GetAllWindows()
        {
            return _activeWindows.Values.ToList();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _cleanupTimer?.Stop();
            HideAllWindows();
            _logger.LogInformation("悬浮文本管理器已释放");
        }

        #region 私有方法

        /// <summary>
        /// 创建思维链窗口
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        /// <param name="position">位置</param>
        /// <returns>窗口对象</returns>
        private ThinkingChainWindow CreateThinkingChainWindow(NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain thinkingChain, FloatingTextPosition position)
        {
            var window = new ThinkingChainWindow(_thinkingChainProcessor)
            {
                Opacity = Opacity,
                Topmost = true,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                ResizeMode = ResizeMode.CanResize
            };

            // 设置思维链
            window.ShowThinkingChain(thinkingChain);

            // 绑定事件
            window.Closed += (s, e) =>
            {
                var windowId = thinkingChain.Id.ToString();
                _activeWindows.TryRemove(windowId, out _);
                WindowClosed?.Invoke(this, windowId);
                OnPropertyChanged(nameof(ActiveWindowCount));
            };

            return window;
        }

        /// <summary>
        /// 定位窗口
        /// </summary>
        /// <param name="window">窗口</param>
        /// <param name="position">位置</param>
        private void PositionWindow(ThinkingChainWindow window, FloatingTextPosition position)
        {
            var workArea = SystemParameters.WorkArea;
            var windowWidth = window.Width;
            var windowHeight = window.Height;

            // 计算偏移量（避免重叠）
            var offset = _activeWindows.Count * 20;

            switch (position)
            {
                case FloatingTextPosition.TopLeft:
                    window.Left = workArea.Left + 20 + offset;
                    window.Top = workArea.Top + 20 + offset;
                    break;

                case FloatingTextPosition.TopRight:
                    window.Left = workArea.Right - windowWidth - 20 - offset;
                    window.Top = workArea.Top + 20 + offset;
                    break;

                case FloatingTextPosition.BottomLeft:
                    window.Left = workArea.Left + 20 + offset;
                    window.Top = workArea.Bottom - windowHeight - 20 - offset;
                    break;

                case FloatingTextPosition.BottomRight:
                    window.Left = workArea.Right - windowWidth - 20 - offset;
                    window.Top = workArea.Bottom - windowHeight - 20 - offset;
                    break;

                case FloatingTextPosition.Center:
                    window.Left = workArea.Left + (workArea.Width - windowWidth) / 2 + offset;
                    window.Top = workArea.Top + (workArea.Height - windowHeight) / 2 + offset;
                    break;
            }

            // 确保窗口在屏幕范围内
            window.Left = Math.Max(workArea.Left, Math.Min(window.Left, workArea.Right - windowWidth));
            window.Top = Math.Max(workArea.Top, Math.Min(window.Top, workArea.Bottom - windowHeight));
        }

        /// <summary>
        /// 强制执行最大窗口数限制
        /// </summary>
        private void EnforceMaxWindows()
        {
            while (_activeWindows.Count >= MaxActiveWindows)
            {
                // 关闭最旧的窗口
                var oldestWindow = _activeWindows.Values.OrderBy(w => w.CreatedTime).FirstOrDefault();
                if (oldestWindow != null)
                {
                    var windowId = _activeWindows.FirstOrDefault(kvp => kvp.Value == oldestWindow).Key;
                    if (!string.IsNullOrEmpty(windowId))
                    {
                        HideThinkingChain(windowId);
                    }
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 更新所有窗口透明度
        /// </summary>
        private void UpdateAllWindowsOpacity()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var window in _activeWindows.Values)
                {
                    window.Opacity = Opacity;
                }
            });
        }

        /// <summary>
        /// 清理定时器事件
        /// </summary>
        private void CleanupTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // 清理已完成超过5分钟的窗口
                var cutoffTime = DateTime.Now.AddMinutes(-5);
                var windowsToClose = new List<string>();

                foreach (var kvp in _activeWindows)
                {
                    var window = kvp.Value;
                    if (window.ThinkingChain?.IsCompleted == true && 
                        window.ThinkingChain.EndTime.HasValue && 
                        window.ThinkingChain.EndTime.Value < cutoffTime)
                    {
                        windowsToClose.Add(kvp.Key);
                    }
                }

                foreach (var windowId in windowsToClose)
                {
                    HideThinkingChain(windowId);
                }

                if (windowsToClose.Count > 0)
                {
                    _logger.LogDebug("清理了 {Count} 个过期的思维链窗口", windowsToClose.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理思维链窗口时发生错误");
            }
        }

        /// <summary>
        /// 触发属性变更事件
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// 悬浮文本位置
    /// </summary>
    public enum FloatingTextPosition
    {
        /// <summary>
        /// 左上角
        /// </summary>
        TopLeft,

        /// <summary>
        /// 右上角
        /// </summary>
        TopRight,

        /// <summary>
        /// 左下角
        /// </summary>
        BottomLeft,

        /// <summary>
        /// 右下角
        /// </summary>
        BottomRight,

        /// <summary>
        /// 中心
        /// </summary>
        Center
    }
}
