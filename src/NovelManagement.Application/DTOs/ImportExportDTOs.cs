using System;
using System.Collections.Generic;

namespace NovelManagement.Application.DTOs
{
    /// <summary>
    /// 导出格式枚举
    /// </summary>
    public enum ExportFormat
    {
        /// <summary>
        /// 纯文本格式
        /// </summary>
        TXT,

        /// <summary>
        /// Excel格式
        /// </summary>
        EXCEL,

        /// <summary>
        /// Word文档格式
        /// </summary>
        DOCX,

        /// <summary>
        /// PDF格式
        /// </summary>
        PDF,

        /// <summary>
        /// 电子书格式
        /// </summary>
        EPUB,

        /// <summary>
        /// HTML格式
        /// </summary>
        HTML,

        /// <summary>
        /// Markdown格式
        /// </summary>
        MARKDOWN,

        /// <summary>
        /// JSON格式
        /// </summary>
        JSON
    }

    /// <summary>
    /// 导入格式枚举
    /// </summary>
    public enum ImportFormat
    {
        /// <summary>
        /// Excel格式
        /// </summary>
        EXCEL,
        
        /// <summary>
        /// Word文档格式
        /// </summary>
        DOCX,
        
        /// <summary>
        /// 纯文本格式
        /// </summary>
        TXT,
        
        /// <summary>
        /// CSV格式
        /// </summary>
        CSV,
        
        /// <summary>
        /// JSON格式
        /// </summary>
        JSON,
        
        /// <summary>
        /// XML格式
        /// </summary>
        XML
    }

    /// <summary>
    /// 导出范围枚举
    /// </summary>
    public enum ExportScope
    {
        /// <summary>
        /// 整个项目
        /// </summary>
        EntireProject,
        
        /// <summary>
        /// 指定卷宗
        /// </summary>
        SelectedVolumes,
        
        /// <summary>
        /// 指定章节
        /// </summary>
        SelectedChapters,
        
        /// <summary>
        /// 自定义范围
        /// </summary>
        CustomRange
    }

    /// <summary>
    /// 操作状态枚举
    /// </summary>
    public enum OperationStatus
    {
        /// <summary>
        /// 待处理
        /// </summary>
        Pending,
        
        /// <summary>
        /// 进行中
        /// </summary>
        InProgress,
        
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        
        /// <summary>
        /// 已失败
        /// </summary>
        Failed,
        
        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// 导出请求DTO
    /// </summary>
    public class ExportRequestDto
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        public Guid ProjectId { get; set; }
        
        /// <summary>
        /// 导出格式
        /// </summary>
        public ExportFormat Format { get; set; }
        
        /// <summary>
        /// 导出范围
        /// </summary>
        public ExportScope Scope { get; set; }
        
        /// <summary>
        /// 选中的卷宗ID列表
        /// </summary>
        public List<Guid> SelectedVolumeIds { get; set; } = new();
        
        /// <summary>
        /// 选中的章节ID列表
        /// </summary>
        public List<Guid> SelectedChapterIds { get; set; } = new();
        
        /// <summary>
        /// 输出文件路径
        /// </summary>
        public string OutputPath { get; set; } = string.Empty;
        
        /// <summary>
        /// 是否包含设定信息
        /// </summary>
        public bool IncludeSettings { get; set; } = true;
        
        /// <summary>
        /// 是否包含角色信息
        /// </summary>
        public bool IncludeCharacters { get; set; } = true;
        
        /// <summary>
        /// 是否包含势力信息
        /// </summary>
        public bool IncludeFactions { get; set; } = true;
        
        /// <summary>
        /// 是否包含剧情信息
        /// </summary>
        public bool IncludePlots { get; set; } = true;
        
        /// <summary>
        /// 导出配置
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    /// <summary>
    /// 导入请求DTO
    /// </summary>
    public class ImportRequestDto
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        public Guid ProjectId { get; set; }
        
        /// <summary>
        /// 导入格式
        /// </summary>
        public ImportFormat Format { get; set; }
        
        /// <summary>
        /// 源文件路径
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;
        
        /// <summary>
        /// 是否覆盖现有数据
        /// </summary>
        public bool OverwriteExisting { get; set; } = false;
        
        /// <summary>
        /// 是否创建备份
        /// </summary>
        public bool CreateBackup { get; set; } = true;
        
        /// <summary>
        /// 字段映射配置
        /// </summary>
        public Dictionary<string, string> FieldMapping { get; set; } = new();
        
        /// <summary>
        /// 导入配置
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    /// <summary>
    /// 操作结果DTO
    /// </summary>
    public class OperationResultDto
    {
        /// <summary>
        /// 操作ID
        /// </summary>
        public Guid OperationId { get; set; }
        
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// 操作状态
        /// </summary>
        public OperationStatus Status { get; set; }
        
        /// <summary>
        /// 进度百分比 (0-100)
        /// </summary>
        public int Progress { get; set; }
        
        /// <summary>
        /// 当前步骤描述
        /// </summary>
        public string CurrentStep { get; set; } = string.Empty;
        
        /// <summary>
        /// 处理的项目数量
        /// </summary>
        public int ProcessedItems { get; set; }
        
        /// <summary>
        /// 总项目数量
        /// </summary>
        public int TotalItems { get; set; }
        
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// 输出文件路径
        /// </summary>
        public string OutputPath { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// 警告信息列表
        /// </summary>
        public List<string> Warnings { get; set; } = new();
        
        /// <summary>
        /// 附加信息
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// 操作历史记录DTO
    /// </summary>
    public class OperationHistoryDto
    {
        /// <summary>
        /// 操作ID
        /// </summary>
        public Guid OperationId { get; set; }
        
        /// <summary>
        /// 项目ID
        /// </summary>
        public Guid ProjectId { get; set; }
        
        /// <summary>
        /// 项目名称
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;
        
        /// <summary>
        /// 操作类型
        /// </summary>
        public string OperationType { get; set; } = string.Empty; // "Export" 或 "Import"
        
        /// <summary>
        /// 格式
        /// </summary>
        public string Format { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件大小
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// 操作状态
        /// </summary>
        public OperationStatus Status { get; set; }
        
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// 耗时（秒）
        /// </summary>
        public double Duration => EndTime.HasValue ? (EndTime.Value - StartTime).TotalSeconds : 0;
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// 备注
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// 导入预览数据DTO
    /// </summary>
    public class ImportPreviewDto
    {
        /// <summary>
        /// 文件信息
        /// </summary>
        public FileInfoDto FileInfo { get; set; } = new();
        
        /// <summary>
        /// 检测到的数据类型
        /// </summary>
        public List<string> DetectedDataTypes { get; set; } = new();
        
        /// <summary>
        /// 预览数据行
        /// </summary>
        public List<Dictionary<string, object>> PreviewRows { get; set; } = new();
        
        /// <summary>
        /// 字段信息
        /// </summary>
        public List<FieldInfoDto> Fields { get; set; } = new();
        
        /// <summary>
        /// 建议的字段映射
        /// </summary>
        public Dictionary<string, string> SuggestedMapping { get; set; } = new();
    }

    /// <summary>
    /// 文件信息DTO
    /// </summary>
    public class FileInfoDto
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 文件格式
        /// </summary>
        public string Format { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime ModifiedTime { get; set; }

        /// <summary>
        /// 最后修改时间（别名）
        /// </summary>
        public DateTime LastModified
        {
            get => ModifiedTime;
            set => ModifiedTime = value;
        }

        /// <summary>
        /// 行数/记录数
        /// </summary>
        public int RecordCount { get; set; }
    }

    /// <summary>
    /// 字段信息DTO
    /// </summary>
    public class FieldInfoDto
    {
        /// <summary>
        /// 字段名
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 字段类型
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// 是否必填
        /// </summary>
        public bool IsRequired { get; set; }
        
        /// <summary>
        /// 示例值
        /// </summary>
        public string? SampleValue { get; set; }
        
        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }
    }
}
