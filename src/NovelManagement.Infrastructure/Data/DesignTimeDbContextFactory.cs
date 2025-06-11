using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NovelManagement.Infrastructure.Data;

/// <summary>
/// 设计时数据库上下文工厂
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<NovelManagementDbContext>
{
    public NovelManagementDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NovelManagementDbContext>();
        
        // 使用SQLite作为默认数据库
        optionsBuilder.UseSqlite("Data Source=novel_management.db");
        
        return new NovelManagementDbContext(optionsBuilder.Options);
    }
}
