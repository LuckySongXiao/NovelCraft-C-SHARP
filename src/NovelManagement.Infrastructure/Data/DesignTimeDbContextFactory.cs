using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.IO;

namespace NovelManagement.Infrastructure.Data;

/// <summary>
/// 设计时数据库上下文工厂
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<NovelManagementDbContext>
{
    public NovelManagementDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NovelManagementDbContext>();

        var appDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NovelManagement",
            "data");

        Directory.CreateDirectory(appDataRoot);

        // 与运行时保持一致，避免迁移和正式库落到不同位置
        optionsBuilder.UseSqlite($"Data Source={Path.Combine(appDataRoot, "NovelManagement.db")}");
        
        return new NovelManagementDbContext(optionsBuilder.Options);
    }
}
