using System.ComponentModel;

namespace NovelManagement.AI.Services.ThinkingChain.Models
{
    /// <summary>
    /// 思维步骤模型
    /// </summary>
    public class ThinkingStep : INotifyPropertyChanged
    {
        private string _content = string.Empty;
        private ThinkingStepType _type = ThinkingStepType.Analysis;
        private ThinkingStepStatus _status = ThinkingStepStatus.Pending;
        private double _confidence = 0.0;
        private TimeSpan _duration = TimeSpan.Zero;

        /// <summary>
        /// 步骤ID
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 步骤序号
        /// </summary>
        public int StepNumber { get; set; }

        /// <summary>
        /// 步骤标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 步骤内容
        /// </summary>
        public string Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged(nameof(Content));
                }
            }
        }

        /// <summary>
        /// 步骤类型
        /// </summary>
        public ThinkingStepType Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged(nameof(Type));
                    OnPropertyChanged(nameof(TypeDescription));
                    OnPropertyChanged(nameof(TypeIcon));
                }
            }
        }

        /// <summary>
        /// 步骤状态
        /// </summary>
        public ThinkingStepStatus Status
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
        /// 置信度 (0-1)
        /// </summary>
        public double Confidence
        {
            get => _confidence;
            set
            {
                var newValue = Math.Max(0, Math.Min(1, value));
                if (Math.Abs(_confidence - newValue) > 0.001)
                {
                    _confidence = newValue;
                    OnPropertyChanged(nameof(Confidence));
                    OnPropertyChanged(nameof(ConfidencePercentage));
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
        /// 相关标签
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// 子步骤
        /// </summary>
        public List<ThinkingStep> SubSteps { get; set; } = new();

        /// <summary>
        /// 父步骤ID
        /// </summary>
        public Guid? ParentStepId { get; set; }

        /// <summary>
        /// 类型描述
        /// </summary>
        public string TypeDescription => Type switch
        {
            ThinkingStepType.Analysis => "分析",
            ThinkingStepType.Planning => "规划",
            ThinkingStepType.Reasoning => "推理",
            ThinkingStepType.Evaluation => "评估",
            ThinkingStepType.Synthesis => "综合",
            ThinkingStepType.Verification => "验证",
            ThinkingStepType.Conclusion => "结论",
            _ => "未知"
        };

        /// <summary>
        /// 状态描述
        /// </summary>
        public string StatusDescription => Status switch
        {
            ThinkingStepStatus.Pending => "等待中",
            ThinkingStepStatus.Processing => "处理中",
            ThinkingStepStatus.Completed => "已完成",
            ThinkingStepStatus.Failed => "失败",
            ThinkingStepStatus.Skipped => "已跳过",
            _ => "未知"
        };

        /// <summary>
        /// 类型图标
        /// </summary>
        public string TypeIcon => Type switch
        {
            ThinkingStepType.Analysis => "ChartLine",
            ThinkingStepType.Planning => "CalendarCheck",
            ThinkingStepType.Reasoning => "Brain",
            ThinkingStepType.Evaluation => "CheckCircle",
            ThinkingStepType.Synthesis => "Merge",
            ThinkingStepType.Verification => "ShieldCheck",
            ThinkingStepType.Conclusion => "Flag",
            _ => "Help"
        };

        /// <summary>
        /// 状态颜色
        /// </summary>
        public string StatusColor => Status switch
        {
            ThinkingStepStatus.Pending => "#FFA500",
            ThinkingStepStatus.Processing => "#1E90FF",
            ThinkingStepStatus.Completed => "#32CD32",
            ThinkingStepStatus.Failed => "#FF4500",
            ThinkingStepStatus.Skipped => "#808080",
            _ => "#000000"
        };

        /// <summary>
        /// 置信度百分比
        /// </summary>
        public string ConfidencePercentage => $"{Confidence * 100:F1}%";

        /// <summary>
        /// 是否有子步骤
        /// </summary>
        public bool HasSubSteps => SubSteps.Count > 0;

        /// <summary>
        /// 是否正在处理
        /// </summary>
        public bool IsProcessing => Status == ThinkingStepStatus.Processing;

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted => Status == ThinkingStepStatus.Completed;

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性变更事件
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 标记步骤开始
        /// </summary>
        public void Start()
        {
            StartTime = DateTime.Now;
            Status = ThinkingStepStatus.Processing;
        }

        /// <summary>
        /// 标记步骤完成
        /// </summary>
        public void Complete()
        {
            EndTime = DateTime.Now;
            Status = ThinkingStepStatus.Completed;
        }

        /// <summary>
        /// 标记步骤失败
        /// </summary>
        public void Fail()
        {
            EndTime = DateTime.Now;
            Status = ThinkingStepStatus.Failed;
        }

        /// <summary>
        /// 添加子步骤
        /// </summary>
        /// <param name="subStep">子步骤</param>
        public void AddSubStep(ThinkingStep subStep)
        {
            subStep.ParentStepId = Id;
            subStep.StepNumber = SubSteps.Count + 1;
            SubSteps.Add(subStep);
            OnPropertyChanged(nameof(HasSubSteps));
        }
    }

    /// <summary>
    /// 思维步骤类型
    /// </summary>
    public enum ThinkingStepType
    {
        /// <summary>
        /// 分析
        /// </summary>
        Analysis,

        /// <summary>
        /// 规划
        /// </summary>
        Planning,

        /// <summary>
        /// 推理
        /// </summary>
        Reasoning,

        /// <summary>
        /// 评估
        /// </summary>
        Evaluation,

        /// <summary>
        /// 综合
        /// </summary>
        Synthesis,

        /// <summary>
        /// 验证
        /// </summary>
        Verification,

        /// <summary>
        /// 结论
        /// </summary>
        Conclusion
    }

    /// <summary>
    /// 思维步骤状态
    /// </summary>
    public enum ThinkingStepStatus
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
        /// 已跳过
        /// </summary>
        Skipped
    }
}
