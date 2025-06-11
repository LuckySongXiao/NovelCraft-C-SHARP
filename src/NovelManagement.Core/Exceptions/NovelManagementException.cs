namespace NovelManagement.Core.Exceptions;

/// <summary>
/// 小说管理系统基础异常类
/// </summary>
public class NovelManagementException : Exception
{
    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public NovelManagementException()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">错误消息</param>
    public NovelManagementException(string message) : base(message)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public NovelManagementException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="errorCode">错误代码</param>
    public NovelManagementException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="errorCode">错误代码</param>
    /// <param name="innerException">内部异常</param>
    public NovelManagementException(string message, string errorCode, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// 实体未找到异常
/// </summary>
public class EntityNotFoundException : NovelManagementException
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="entityName">实体名称</param>
    /// <param name="entityId">实体ID</param>
    public EntityNotFoundException(string entityName, object entityId)
        : base($"{entityName} with id '{entityId}' was not found.", "ENTITY_NOT_FOUND")
    {
    }
}

/// <summary>
/// 业务规则违反异常
/// </summary>
public class BusinessRuleViolationException : NovelManagementException
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">错误消息</param>
    public BusinessRuleViolationException(string message)
        : base(message, "BUSINESS_RULE_VIOLATION")
    {
    }
}

/// <summary>
/// 验证异常
/// </summary>
public class ValidationException : NovelManagementException
{
    /// <summary>
    /// 验证错误列表
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="errors">验证错误</param>
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.", "VALIDATION_ERROR")
    {
        Errors = errors;
    }
}
