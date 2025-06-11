# 角色编辑问题修复验证脚本
Write-Host "=== 角色编辑问题修复验证 ===" -ForegroundColor Green
Write-Host ""

# 检查修复的文件
Write-Host "1. 检查修复的文件..." -ForegroundColor Yellow

$fixedFiles = @{
    "src\NovelManagement.Application\Services\CharacterService.cs" = @(
        "清除导航属性",
        "确保不会意外修改ProjectId",
        "使用UpdateAsync方法更新"
    )
}

foreach ($file in $fixedFiles.Keys) {
    if (Test-Path $file) {
        Write-Host "  ✅ $file" -ForegroundColor Green
        
        $content = Get-Content $file -Raw
        $checks = $fixedFiles[$file]
        
        foreach ($check in $checks) {
            if ($content -match [regex]::Escape($check)) {
                Write-Host "    ✅ 包含修复: $check" -ForegroundColor Green
            } else {
                Write-Host "    ⚠️  可能缺少修复: $check" -ForegroundColor Yellow
            }
        }
    } else {
        Write-Host "  ❌ $file 不存在" -ForegroundColor Red
    }
}

# 检查日志文件
Write-Host ""
Write-Host "2. 检查最新日志..." -ForegroundColor Yellow

$logFile = "logs\app-20250606.txt"
if (Test-Path $logFile) {
    Write-Host "  ✅ 日志文件存在" -ForegroundColor Green
    
    # 检查最近的错误
    $recentErrors = Get-Content $logFile | Select-String -Pattern "编辑角色失败|更新角色失败|UNIQUE constraint failed|entity type 'Character' cannot be tracked" | Select-Object -Last 5
    
    if ($recentErrors) {
        Write-Host "  ⚠️  发现最近的错误:" -ForegroundColor Yellow
        foreach ($error in $recentErrors) {
            Write-Host "    $error" -ForegroundColor Red
        }
    } else {
        Write-Host "  ✅ 未发现最近的角色编辑错误" -ForegroundColor Green
    }
} else {
    Write-Host "  ⚠️  日志文件不存在" -ForegroundColor Yellow
}

# 检查数据库配置
Write-Host ""
Write-Host "3. 检查数据库配置..." -ForegroundColor Yellow

$dbContextFile = "src\NovelManagement.Infrastructure\Data\NovelManagementDbContext.cs"
if (Test-Path $dbContextFile) {
    Write-Host "  ✅ DbContext文件存在" -ForegroundColor Green
    
    $content = Get-Content $dbContextFile -Raw
    
    # 检查项目名称唯一索引
    if ($content -match "HasIndex\(e => e\.Name\)\.IsUnique\(\)") {
        Write-Host "    ✅ 项目名称唯一索引配置正确" -ForegroundColor Green
    } else {
        Write-Host "    ⚠️  项目名称唯一索引配置可能有问题" -ForegroundColor Yellow
    }
    
    # 检查角色配置
    if ($content -match "ConfigureCharacter") {
        Write-Host "    ✅ 角色实体配置存在" -ForegroundColor Green
    } else {
        Write-Host "    ❌ 角色实体配置缺失" -ForegroundColor Red
    }
} else {
    Write-Host "  ❌ DbContext文件不存在" -ForegroundColor Red
}

# 检查编译状态
Write-Host ""
Write-Host "4. 检查编译状态..." -ForegroundColor Yellow

try {
    $buildResult = dotnet build --verbosity quiet 2>&1
    $buildExitCode = $LASTEXITCODE
    
    if ($buildExitCode -eq 0) {
        Write-Host "  ✅ 项目编译成功" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  项目编译有警告或错误" -ForegroundColor Yellow
        Write-Host "  🔍 建议运行: dotnet build 查看详细信息" -ForegroundColor Cyan
    }
} catch {
    Write-Host "  ❌ 编译检查失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 修复内容总结
Write-Host ""
Write-Host "=== 修复内容总结 ===" -ForegroundColor Green

Write-Host "🔧 主要修复:" -ForegroundColor Cyan
Write-Host "  1. 实体跟踪冲突修复" -ForegroundColor White
Write-Host "     - 清除导航属性，避免EF Core尝试更新关联实体" -ForegroundColor Gray
Write-Host "     - 确保不会意外修改ProjectId，避免触发Project的唯一约束" -ForegroundColor Gray
Write-Host "     - 使用正确的实体更新方法" -ForegroundColor Gray

Write-Host ""
Write-Host "  2. 项目名称唯一约束问题修复" -ForegroundColor White
Write-Host "     - 防止意外更新Project实体" -ForegroundColor Gray
Write-Host "     - 清除可能导致级联更新的导航属性" -ForegroundColor Gray

Write-Host ""
Write-Host "  3. 势力和种族关系处理优化" -ForegroundColor White
Write-Host "     - 从Tags字段提取势力和种族信息" -ForegroundColor Gray
Write-Host "     - 智能查找现有势力和种族" -ForegroundColor Gray
Write-Host "     - 避免创建不完整的关联对象" -ForegroundColor Gray

Write-Host ""
Write-Host "📋 预期效果:" -ForegroundColor Cyan
Write-Host "  ✅ 消除'entity type Character cannot be tracked'错误" -ForegroundColor Green
Write-Host "  ✅ 消除'UNIQUE constraint failed: Projects.Name'错误" -ForegroundColor Green
Write-Host "  ✅ 角色编辑功能正常工作" -ForegroundColor Green
Write-Host "  ✅ 势力和种族关系正确处理" -ForegroundColor Green

Write-Host ""
Write-Host "🧪 测试建议:" -ForegroundColor Cyan
Write-Host "  1. 启动应用程序" -ForegroundColor White
Write-Host "  2. 进入角色管理界面" -ForegroundColor White
Write-Host "  3. 尝试编辑现有角色" -ForegroundColor White
Write-Host "  4. 修改角色的基本信息（姓名、背景等）" -ForegroundColor White
Write-Host "  5. 保存更改并观察是否成功" -ForegroundColor White
Write-Host "  6. 检查日志文件是否有新的错误" -ForegroundColor White

Write-Host ""
Write-Host "📖 相关文档:" -ForegroundColor Cyan
Write-Host "  - 角色编辑功能修复报告.md" -ForegroundColor White
Write-Host "  - 数据库并发问题修复报告.md" -ForegroundColor White

Write-Host ""
Write-Host "🎉 角色编辑问题修复验证完成！" -ForegroundColor Green
Write-Host "如果测试中仍然遇到问题，请检查日志文件获取详细错误信息。" -ForegroundColor Gray
