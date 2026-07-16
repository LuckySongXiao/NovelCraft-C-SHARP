using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NovelManagement.WPF.Services;

/// <summary>
/// 提供数据库备份、恢复和备份保留清理能力。
/// </summary>
public class DatabaseMaintenanceService
{
    private readonly ILogger<DatabaseMaintenanceService> _logger;
    private readonly string _databaseFilePath;
    private readonly string _backupDirectory;
    private readonly int _backupRetentionDays;

    public DatabaseMaintenanceService(
        IConfiguration configuration,
        ILogger<DatabaseMaintenanceService> logger)
    {
        _logger = logger;

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("未找到数据库连接字符串配置");

        var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
        _databaseFilePath = connectionStringBuilder.DataSource;

        _backupDirectory = configuration["Paths:BackupsDirectory"]
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NovelManagement",
                "backups");

        _backupRetentionDays = Math.Max(1, configuration.GetValue("Backup:RetentionDays", 14));

        Directory.CreateDirectory(_backupDirectory);
    }

    /// <summary>
    /// 获取数据库文件完整路径。
    /// </summary>
    public string GetDatabaseFilePath()
    {
        return _databaseFilePath;
    }

    /// <summary>
    /// 获取备份目录完整路径。
    /// </summary>
    public string GetBackupDirectory()
    {
        return _backupDirectory;
    }

    /// <summary>
    /// 创建数据库备份。
    /// </summary>
    public DatabaseBackupResult CreateBackup(string reason)
    {
        if (!File.Exists(_databaseFilePath))
        {
            _logger.LogInformation("数据库文件不存在，跳过备份: {DatabaseFilePath}", _databaseFilePath);
            return new DatabaseBackupResult(false, null, "数据库文件不存在");
        }

        Directory.CreateDirectory(_backupDirectory);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var safeReason = string.IsNullOrWhiteSpace(reason) ? "manual" : SanitizeFileName(reason);
        var fileName = $"{Path.GetFileNameWithoutExtension(_databaseFilePath)}_{safeReason}_{timestamp}{Path.GetExtension(_databaseFilePath)}";
        var backupFilePath = Path.Combine(_backupDirectory, fileName);

        File.Copy(_databaseFilePath, backupFilePath, overwrite: false);
        _logger.LogInformation("数据库备份完成: {BackupFilePath}", backupFilePath);

        return new DatabaseBackupResult(true, backupFilePath, null);
    }

    /// <summary>
    /// 恢复数据库文件。
    /// </summary>
    public DatabaseRestoreResult RestoreBackup(string backupFilePath, bool overwriteExisting = false)
    {
        if (!File.Exists(backupFilePath))
        {
            return new DatabaseRestoreResult(false, "指定的备份文件不存在");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_databaseFilePath)!);

        if (File.Exists(_databaseFilePath) && !overwriteExisting)
        {
            return new DatabaseRestoreResult(false, "目标数据库已存在，请先确认是否覆盖");
        }

        File.Copy(backupFilePath, _databaseFilePath, overwrite: overwriteExisting);
        _logger.LogInformation("数据库恢复完成，来源: {BackupFilePath}", backupFilePath);

        return new DatabaseRestoreResult(true, null);
    }

    /// <summary>
    /// 清理超过保留期的备份文件。
    /// </summary>
    public int CleanupExpiredBackups()
    {
        if (!Directory.Exists(_backupDirectory))
        {
            return 0;
        }

        var cutoff = DateTime.Now.AddDays(-_backupRetentionDays);
        var deletedCount = 0;

        foreach (var file in Directory.EnumerateFiles(_backupDirectory, "*.db", SearchOption.TopDirectoryOnly))
        {
            var fileInfo = new FileInfo(file);
            if (fileInfo.CreationTime < cutoff)
            {
                fileInfo.Delete();
                deletedCount++;
            }
        }

        if (deletedCount > 0)
        {
            _logger.LogInformation("已清理 {DeletedCount} 个过期数据库备份", deletedCount);
        }

        return deletedCount;
    }

    /// <summary>
    /// 列出当前可用的数据库备份文件。
    /// </summary>
    public IReadOnlyList<string> ListBackups()
    {
        if (!Directory.Exists(_backupDirectory))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(_backupDirectory, "*.db", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetCreationTime)
            .ToList();
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = value.Trim();

        foreach (var invalidChar in invalidChars)
        {
            sanitized = sanitized.Replace(invalidChar, '_');
        }

        return string.IsNullOrWhiteSpace(sanitized) ? "manual" : sanitized;
    }
}

/// <summary>
/// 数据库备份结果。
/// </summary>
public sealed record DatabaseBackupResult(bool Success, string? BackupFilePath, string? ErrorMessage);

/// <summary>
/// 数据库恢复结果。
/// </summary>
public sealed record DatabaseRestoreResult(bool Success, string? ErrorMessage);
