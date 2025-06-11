using System.ComponentModel;

namespace NovelManagement.AI.Services.ThinkingChain.Models
{
    /// <summary>
    /// 思维链模型
    /// </summary>
    public class ThinkingChain : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string _description = string.Empty;
        private ThinkingChainStatus _status = ThinkingChainStatus.Pending;
        private double _progress = 0.0;

        /// <summary>
        /// 思维链ID
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 标题
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        /// <summary>
        /// 状态
        /// </summary>
        public ThinkingChainStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(StatusDescription));
                    OnPropertyChanged(nameof(StatusColor));
                }
            }
        }

        /// <summary>
        /// 进度 (0-1)
        /// </summary>
        public double Progress
        {
            get => _progress;
            set
            {
                var newValue = Math.Max(0, Math.Min(1, value));
                if (Math.Abs(_progress - newValue) > 0.001)
                {
                    _progress = newValue;
                    OnPropertyChanged(nameof(Progress));
                    OnPropertyChanged(nameof(ProgressPercentage));
                }
            }
        }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 思维步骤列表
        /// </summary>
        public List<ThinkingStep> Steps { get; set; } = new();

        /// <summary>
        /// 相关任务ID
        /// </summary>
        public string? TaskId { get; set; }

        /// <summary>
        /// 相关Agent ID
        /// </summary>
        public string? AgentId { get; set; }

        /// <summary>
        /// 原始输入
        /// </summary>
        public string? OriginalInput { get; set; }

        /// <summary>
        /// 最终输出
        /// </summary>
        public string? FinalOutput { get; set; }

        /// <summary>
        /// 元数据
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// 状态描述
        /// </summary>
        public string StatusDescription => Status switch
        {
            ThinkingChainStatus.Pending => "等待中",
            ThinkingChainStatus.Processing => "处理中",
            ThinkingChainStatus.Completed => "已完成",
            ThinkingChainStatus.Failed => "失败",
            ThinkingChainStatus.Cancelled => "已取消",
            _ => "未知"
        };

        /// <summary>
        /// 状态颜色
        /// </summary>
        public string StatusColor => Status switch
        {
            ThinkingChainStatus.Pending => "#FFA500",
            ThinkingChainStatus.Processing => "#1E90FF",
            ThinkingChainStatus.Completed => "#32CD32",
            ThinkingChainStatus.Failed => "#FF4500",
            ThinkingChainStatus.Cancelled => "#808080",
            _ => "#000000"
        };

        /// <summary>
        /// 进度百分比
        /// </summary>
        public string ProgressPercentage => $"{Progress * 100:F1}%";

        /// <summary>
        /// 持续时间
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                if (EndTime.HasValue)
                {
                    return EndTime.Value - StartTime;
                }
                return DateTime.Now - StartTime;
            }
        }

        /// <summary>
        /// 总步骤数
        /// </summary>
        public int TotalSteps => Steps.Count;

        /// <summary>
        /// 已完成步骤数
        /// </summary>
        public int CompletedSteps => Steps.Count(s => s.Status == ThinkingStepStatus.Completed);

        /// <summary>
        /// 当前步骤
        /// </summary>
        public ThinkingStep? CurrentStep => Steps.FirstOrDefault(s => s.Status == ThinkingStepStatus.Processing);

        /// <summary>
        /// 是否正在处理
        /// </summary>
        public bool IsProcessing => Status == ThinkingChainStatus.Processing;

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted => Status == ThinkingChainStatus.Completed;

        /// <summary>
        /// 是否失败
        /// </summary>
        public bool IsFailed => Status == ThinkingChainStatus.Failed;

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 步骤变更事件
        /// </summary>
        public event EventHandler<ThinkingStepEventArgs>? StepChanged;

        /// <summary>
        /// 触发属性变更事件
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 添加思维步骤
        /// </summary>
        /// <param name="step">思维步骤</param>
        public void AddStep(ThinkingStep step)
        {
            step.StepNumber = Steps.Count + 1;
            Steps.Add(step);
            OnPropertyChanged(nameof(TotalSteps));
            OnStepChanged(new ThinkingStepEventArgs(step, ThinkingStepEventType.Added));
        }

        /// <summary>
        /// 开始处理
        /// </summary>
        public void Start()
        {
            StartTime = DateTime.Now;
            Status = ThinkingChainStatus.Processing;
        }

        /// <summary>
        /// 完成处理
        /// </summary>
        public void Complete()
        {
            EndTime = DateTime.Now;
            Status = ThinkingChainStatus.Completed;
            Progress = 1.0;
        }

        /// <summary>
        /// 标记失败
        /// </summary>
        public void Fail()
        {
            EndTime = DateTime.Now;
            Status = ThinkingChainStatus.Failed;
        }

        /// <summary>
        /// 取消处理
        /// </summary>
        public void Cancel()
        {
            EndTime = DateTime.Now;
            Status = ThinkingChainStatus.Cancelled;
        }

        /// <summary>
        /// 更新进度
        /// </summary>
        public void UpdateProgress()
        {
            if (TotalSteps > 0)
            {
                Progress = (double)CompletedSteps / TotalSteps;
            }
            OnPropertyChanged(nameof(CompletedSteps));
        }

        /// <summary>
        /// 触发步骤变更事件
        /// </summary>
        /// <param name="args">事件参数</param>
        protected virtual void OnStepChanged(ThinkingStepEventArgs args)
        {
            StepChanged?.Invoke(this, args);
        }
    }

    /// <summary>
    /// 思维链状态
    /// </summary>
    public enum ThinkingChainStatus
    {
        /// <summary>
        /// 等待中
        /// </summary>
        Pending,

        /// <summary>
        /// 处理中
        /// </summary>
        Processing,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed,

        /// <summary>
        /// 失败
        /// </summary>
        Failed,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// 思维步骤事件参数
    /// </summary>
    public class ThinkingStepEventArgs : EventArgs
    {
        /// <summary>
        /// 思维步骤
        /// </summary>
        public ThinkingStep Step { get; }

        /// <summary>
        /// 事件类型
        /// </summary>
        public ThinkingStepEventType EventType { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="step">思维步骤</param>
        /// <param name="eventType">事件类型</param>
        public ThinkingStepEventArgs(ThinkingStep step, ThinkingStepEventType eventType)
        {
            Step = step;
            EventType = eventType;
        }
    }

    /// <summary>
    /// 思维步骤事件类型
    /// </summary>
    public enum ThinkingStepEventType
    {
        /// <summary>
        /// 添加
        /// </summary>
        Added,

        /// <summary>
        /// 更新
        /// </summary>
        Updated,

        /// <summary>
        /// 完成
        /// </summary>
        Completed,

        /// <summary>
        /// 失败
        /// </summary>
        Failed
    }
}
