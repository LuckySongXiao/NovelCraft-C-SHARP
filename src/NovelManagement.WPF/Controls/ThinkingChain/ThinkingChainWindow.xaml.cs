using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NovelManagement.AI.Services.ThinkingChain.Models;
using NovelManagement.AI.Services.ThinkingChain;

namespace NovelManagement.WPF.Controls.ThinkingChain
{
    /// <summary>
    /// 思维链悬浮窗口
    /// </summary>
    public partial class ThinkingChainWindow : Window, INotifyPropertyChanged
    {
        private readonly IThinkingChainProcessor _thinkingChainProcessor;
        private ThinkingChainPopup? _popup;
        private NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain? _thinkingChain;
        private bool _isMinimized = false;
        private bool _isTopMost = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; } = DateTime.Now;

        /// <summary>
        /// 当前思维链
        /// </summary>
        public NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain? ThinkingChain
        {
            get => _thinkingChain;
            private set
            {
                if (_thinkingChain != value)
                {
                    _thinkingChain = value;
                    OnPropertyChanged(nameof(ThinkingChain));
                }
            }
        }

        /// <summary>
        /// 是否最小化
        /// </summary>
        public bool IsMinimized
        {
            get => _isMinimized;
            set
            {
                if (_isMinimized != value)
                {
                    _isMinimized = value;
                    OnPropertyChanged(nameof(IsMinimized));
                    UpdateMinimizedState();
                }
            }
        }

        /// <summary>
        /// 是否置顶
        /// </summary>
        public bool IsTopMost
        {
            get => _isTopMost;
            set
            {
                if (_isTopMost != value)
                {
                    _isTopMost = value;
                    Topmost = value;
                    OnPropertyChanged(nameof(IsTopMost));
                    UpdateTopMostIcon();
                }
            }
        }

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="thinkingChainProcessor">思维链处理器</param>
        public ThinkingChainWindow(IThinkingChainProcessor thinkingChainProcessor)
        {
            InitializeComponent();
            
            _thinkingChainProcessor = thinkingChainProcessor;
            DataContext = this;

            // 初始化悬浮控件
            InitializePopup();

            // 设置初始状态
            UpdateTopMostIcon();
        }

        /// <summary>
        /// 显示思维链
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        public void ShowThinkingChain(NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain thinkingChain)
        {
            ThinkingChain = thinkingChain;
            
            // 更新标题
            TitleTextBlock.Text = thinkingChain.Title;
            Title = thinkingChain.Title;

            // 显示在悬浮控件中
            _popup?.ShowThinkingChain(thinkingChain);

            // 监听思维链变化
            if (thinkingChain != null)
            {
                thinkingChain.PropertyChanged += OnThinkingChainPropertyChanged;
            }
        }

        /// <summary>
        /// 更新思维链
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        public void UpdateThinkingChain(NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain thinkingChain)
        {
            ThinkingChain = thinkingChain;
            _popup?.ShowThinkingChain(thinkingChain);
        }

        #region 事件处理

        /// <summary>
        /// 标题栏鼠标按下事件
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                // 双击切换最小化状态
                IsMinimized = !IsMinimized;
            }
            else
            {
                // 单击拖拽窗口
                DragMove();
            }
        }

        /// <summary>
        /// 置顶按钮点击
        /// </summary>
        private void TopMostButton_Click(object sender, RoutedEventArgs e)
        {
            IsTopMost = !IsTopMost;
        }

        /// <summary>
        /// 最小化按钮点击
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            IsMinimized = !IsMinimized;
        }

        /// <summary>
        /// 关闭按钮点击
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 调整大小手柄拖拽
        /// </summary>
        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var newWidth = Width + e.HorizontalChange;
            var newHeight = Height + e.VerticalChange;

            // 限制最小尺寸
            if (newWidth >= MinWidth)
                Width = newWidth;
            
            if (newHeight >= MinHeight)
                Height = newHeight;
        }

        /// <summary>
        /// 思维链属性变更
        /// </summary>
        private void OnThinkingChainPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain.Title):
                        TitleTextBlock.Text = ThinkingChain?.Title ?? "AI思维过程";
                        Title = ThinkingChain?.Title ?? "AI思维过程";
                        break;
                    case nameof(NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain.Progress):
                        ProgressBar.Value = (ThinkingChain?.Progress ?? 0) * 100;
                        break;
                }
            });
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            // 清理事件订阅
            if (ThinkingChain != null)
            {
                ThinkingChain.PropertyChanged -= OnThinkingChainPropertyChanged;
            }

            base.OnClosing(e);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化悬浮控件
        /// </summary>
        private void InitializePopup()
        {
            _popup = new ThinkingChainPopup(_thinkingChainProcessor);
            
            // 绑定事件
            _popup.CloseRequested += (s, e) => Close();
            _popup.MinimizeRequested += (s, minimized) => IsMinimized = minimized;
            _popup.PauseRequested += (s, paused) => HandlePauseRequest(paused);
            _popup.ExportRequested += (s, chain) => HandleExportRequest(chain);

            // 设置为内容
            ContentPresenter.Content = _popup;
        }

        /// <summary>
        /// 更新最小化状态
        /// </summary>
        private void UpdateMinimizedState()
        {
            if (IsMinimized)
            {
                // 最小化：只显示标题栏
                Height = 32;
                ResizeMode = ResizeMode.NoResize;
                
                // 隐藏内容
                if (_popup != null)
                {
                    _popup.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                // 恢复：显示完整内容
                Height = 500;
                ResizeMode = ResizeMode.CanResize;
                
                // 显示内容
                if (_popup != null)
                {
                    _popup.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// 更新置顶图标
        /// </summary>
        private void UpdateTopMostIcon()
        {
            if (TopMostIcon != null)
            {
                TopMostIcon.Kind = IsTopMost ? 
                    MaterialDesignThemes.Wpf.PackIconKind.Pin : 
                    MaterialDesignThemes.Wpf.PackIconKind.PinOutline;
            }
        }

        /// <summary>
        /// 处理暂停请求
        /// </summary>
        /// <param name="paused">是否暂停</param>
        private void HandlePauseRequest(bool paused)
        {
            // 这里可以实现暂停逻辑
            // 例如暂停思维链处理
        }

        /// <summary>
        /// 处理导出请求
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        private void HandleExportRequest(NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain thinkingChain)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出思维链",
                    Filter = "Markdown文件 (*.md)|*.md|JSON文件 (*.json)|*.json|文本文件 (*.txt)|*.txt|HTML文件 (*.html)|*.html",
                    DefaultExt = ".md",
                    FileName = $"思维链_{thinkingChain.Title}_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var format = Path.GetExtension(saveFileDialog.FileName).ToLower() switch
                    {
                        ".md" => ThinkingChainExportFormat.Markdown,
                        ".json" => ThinkingChainExportFormat.Json,
                        ".txt" => ThinkingChainExportFormat.PlainText,
                        ".html" => ThinkingChainExportFormat.Html,
                        _ => ThinkingChainExportFormat.Markdown
                    };

                    Task.Run(async () =>
                    {
                        try
                        {
                            var content = await _thinkingChainProcessor.ExportThinkingChainAsync(thinkingChain, format);
                            await File.WriteAllTextAsync(saveFileDialog.FileName, content);

                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show($"思维链已导出到: {saveFileDialog.FileName}", "导出成功", 
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            });
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show($"导出失败: {ex.Message}", "导出错误", 
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "导出错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
}
