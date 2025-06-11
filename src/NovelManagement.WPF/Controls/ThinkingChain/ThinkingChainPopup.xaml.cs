using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using NovelManagement.AI.Services.ThinkingChain.Models;
using NovelManagement.AI.Services.ThinkingChain;

namespace NovelManagement.WPF.Controls.ThinkingChain
{
    /// <summary>
    /// 思维链悬浮窗口
    /// </summary>
    public partial class ThinkingChainPopup : UserControl, INotifyPropertyChanged
    {
        private readonly IThinkingChainProcessor _thinkingChainProcessor;
        private NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain? _currentThinkingChain;
        private bool _isMinimized = false;
        private bool _isPaused = false;
        private double _originalHeight;

        /// <summary>
        /// 思维步骤集合
        /// </summary>
        public ObservableCollection<ThinkingStep> Steps { get; } = new();

        /// <summary>
        /// 当前思维链
        /// </summary>
        public NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain? CurrentThinkingChain
        {
            get => _currentThinkingChain;
            set
            {
                if (_currentThinkingChain != value)
                {
                    if (_currentThinkingChain != null)
                    {
                        _currentThinkingChain.StepChanged -= OnStepChanged;
                        _currentThinkingChain.PropertyChanged -= OnThinkingChainPropertyChanged;
                    }

                    _currentThinkingChain = value;

                    if (_currentThinkingChain != null)
                    {
                        _currentThinkingChain.StepChanged += OnStepChanged;
                        _currentThinkingChain.PropertyChanged += OnThinkingChainPropertyChanged;
                        
                        // 更新UI
                        UpdateUI();
                    }

                    OnPropertyChanged(nameof(CurrentThinkingChain));
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
        /// 是否暂停
        /// </summary>
        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                if (_isPaused != value)
                {
                    _isPaused = value;
                    OnPropertyChanged(nameof(IsPaused));
                    UpdatePauseState();
                }
            }
        }

        /// <summary>
        /// 关闭事件
        /// </summary>
        public event EventHandler? CloseRequested;

        /// <summary>
        /// 最小化事件
        /// </summary>
        public event EventHandler<bool>? MinimizeRequested;

        /// <summary>
        /// 暂停/继续事件
        /// </summary>
        public event EventHandler<bool>? PauseRequested;

        /// <summary>
        /// 导出事件
        /// </summary>
        public event EventHandler<NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain>? ExportRequested;

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="thinkingChainProcessor">思维链处理器</param>
        public ThinkingChainPopup(IThinkingChainProcessor thinkingChainProcessor)
        {
            InitializeComponent();
            
            _thinkingChainProcessor = thinkingChainProcessor;
            DataContext = this;
            
            // 保存原始高度
            _originalHeight = MaxHeight;
            
            // 启用拖拽
            EnableDragging();
            
            // 播放淡入动画
            PlayFadeInAnimation();
        }

        /// <summary>
        /// 显示思维链
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        public void ShowThinkingChain(NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain thinkingChain)
        {
            CurrentThinkingChain = thinkingChain;

            // 确保窗口可见
            Visibility = Visibility.Visible;

            // 根据用户偏好设置初始状态（默认折叠显示）
            IsMinimized = true;

            // 播放淡入动画
            PlayFadeInAnimation();

            // 更新UI显示
            UpdateUI();
        }

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        public void Hide()
        {
            PlayFadeOutAnimation(() =>
            {
                Visibility = Visibility.Collapsed;
            });
        }

        /// <summary>
        /// 添加思维步骤
        /// </summary>
        /// <param name="step">思维步骤</param>
        public void AddStep(ThinkingStep step)
        {
            Dispatcher.Invoke(() =>
            {
                Steps.Add(step);
                
                // 自动滚动到底部
                StepsScrollViewer.ScrollToBottom();
                
                // 播放步骤添加动画
                PlayStepAddedAnimation();
            });
        }

        /// <summary>
        /// 更新步骤
        /// </summary>
        /// <param name="step">思维步骤</param>
        public void UpdateStep(ThinkingStep step)
        {
            Dispatcher.Invoke(() =>
            {
                var existingStep = Steps.FirstOrDefault(s => s.Id == step.Id);
                if (existingStep != null)
                {
                    var index = Steps.IndexOf(existingStep);
                    Steps[index] = step;
                }
            });
        }

        /// <summary>
        /// 清空步骤
        /// </summary>
        public void ClearSteps()
        {
            Dispatcher.Invoke(() =>
            {
                Steps.Clear();
            });
        }

        #region 事件处理

        /// <summary>
        /// 关闭按钮点击
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 最小化按钮点击
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            IsMinimized = !IsMinimized;
            MinimizeRequested?.Invoke(this, IsMinimized);
        }

        /// <summary>
        /// 暂停按钮点击
        /// </summary>
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            IsPaused = !IsPaused;
            PauseRequested?.Invoke(this, IsPaused);
        }

        /// <summary>
        /// 导出按钮点击
        /// </summary>
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentThinkingChain != null)
            {
                ExportRequested?.Invoke(this, CurrentThinkingChain);
            }
        }

        /// <summary>
        /// 思维链步骤变更
        /// </summary>
        private void OnStepChanged(object? sender, ThinkingStepEventArgs e)
        {
            switch (e.EventType)
            {
                case ThinkingStepEventType.Added:
                    AddStep(e.Step);
                    break;
                case ThinkingStepEventType.Updated:
                    UpdateStep(e.Step);
                    break;
            }
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
                        TitleTextBlock.Text = CurrentThinkingChain?.Title ?? "AI思维过程";
                        break;
                    case nameof(NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain.Progress):
                        ProgressBar.Value = (CurrentThinkingChain?.Progress ?? 0) * 100;
                        break;
                    case nameof(NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain.Status):
                        UpdateStatusText();
                        break;
                }
            });
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新UI
        /// </summary>
        private void UpdateUI()
        {
            if (CurrentThinkingChain == null) return;

            Dispatcher.Invoke(() =>
            {
                // 更新标题
                TitleTextBlock.Text = CurrentThinkingChain.Title;

                // 更新进度
                ProgressBar.Value = CurrentThinkingChain.Progress * 100;

                // 更新步骤
                Steps.Clear();
                foreach (var step in CurrentThinkingChain.Steps)
                {
                    Steps.Add(step);
                }

                // 更新状态
                UpdateStatusText();

                // 更新最小化状态
                UpdateMinimizedState();
            });
        }

        /// <summary>
        /// 更新状态文本
        /// </summary>
        private void UpdateStatusText()
        {
            if (CurrentThinkingChain == null) return;

            var statusText = CurrentThinkingChain.Status switch
            {
                ThinkingChainStatus.Pending => "准备中...",
                ThinkingChainStatus.Processing => $"处理中... ({CurrentThinkingChain.CompletedSteps}/{CurrentThinkingChain.TotalSteps})",
                ThinkingChainStatus.Completed => $"已完成 ({CurrentThinkingChain.Duration.TotalSeconds:F1}秒)",
                ThinkingChainStatus.Failed => "处理失败",
                ThinkingChainStatus.Cancelled => "已取消",
                _ => "未知状态"
            };

            StatusTextBlock.Text = statusText;
        }

        /// <summary>
        /// 更新最小化状态
        /// </summary>
        private void UpdateMinimizedState()
        {
            if (IsMinimized)
            {
                // 最小化：只显示标题栏
                var storyboard = new Storyboard();
                var heightAnimation = new DoubleAnimation
                {
                    To = 40, // 只显示标题栏的高度
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(heightAnimation, this);
                Storyboard.SetTargetProperty(heightAnimation, new PropertyPath("Height"));
                storyboard.Children.Add(heightAnimation);
                storyboard.Begin();

                // 隐藏内容区域
                StepsScrollViewer.Visibility = Visibility.Collapsed;
            }
            else
            {
                // 恢复：显示完整内容
                var storyboard = new Storyboard();
                var heightAnimation = new DoubleAnimation
                {
                    To = _originalHeight,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(heightAnimation, this);
                Storyboard.SetTargetProperty(heightAnimation, new PropertyPath("Height"));
                storyboard.Children.Add(heightAnimation);
                storyboard.Begin();

                // 显示内容区域
                StepsScrollViewer.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 更新暂停状态
        /// </summary>
        private void UpdatePauseState()
        {
            if (IsPaused)
            {
                PauseIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Play;
                PauseButton.ToolTip = "继续";
            }
            else
            {
                PauseIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Pause;
                PauseButton.ToolTip = "暂停";
            }
        }

        /// <summary>
        /// 启用拖拽功能
        /// </summary>
        private void EnableDragging()
        {
            var titleBar = MainCard.FindName("TitleTextBlock") as FrameworkElement;
            if (titleBar != null)
            {
                titleBar.MouseLeftButtonDown += (s, e) =>
                {
                    if (e.ClickCount == 1)
                    {
                        var window = Window.GetWindow(this);
                        window?.DragMove();
                    }
                };
            }
        }

        /// <summary>
        /// 播放淡入动画
        /// </summary>
        private void PlayFadeInAnimation()
        {
            var storyboard = FindResource("FadeInAnimation") as Storyboard;
            storyboard?.Begin(this);
        }

        /// <summary>
        /// 播放淡出动画
        /// </summary>
        /// <param name="onCompleted">完成回调</param>
        private void PlayFadeOutAnimation(Action? onCompleted = null)
        {
            var storyboard = FindResource("FadeOutAnimation") as Storyboard;
            if (storyboard != null)
            {
                if (onCompleted != null)
                {
                    storyboard.Completed += (s, e) => onCompleted();
                }
                storyboard.Begin(this);
            }
            else
            {
                onCompleted?.Invoke();
            }
        }

        /// <summary>
        /// 播放步骤添加动画
        /// </summary>
        private void PlayStepAddedAnimation()
        {
            // 简单的脉冲效果
            var storyboard = new Storyboard();
            var scaleAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.05,
                Duration = TimeSpan.FromMilliseconds(150),
                AutoReverse = true,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            Storyboard.SetTarget(scaleAnimation, MainCard);
            Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleAnimation);

            var scaleAnimationY = new DoubleAnimation
            {
                From = 1.0,
                To = 1.05,
                Duration = TimeSpan.FromMilliseconds(150),
                AutoReverse = true,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            Storyboard.SetTarget(scaleAnimationY, MainCard);
            Storyboard.SetTargetProperty(scaleAnimationY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleAnimationY);

            storyboard.Begin();
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
