# 简单验证角色编辑修复
Write-Host "=== 角色编辑修复验证 ===" -ForegroundColor Green

# 检查修复的文件
Write-Host "检查CharacterService.cs修复..." -ForegroundColor Yellow
$file = "src\NovelManagement.Application\Services\CharacterService.cs"

if (Test-Path $file) {
    Write-Host "✅ 文件存在" -ForegroundColor Green
    
    $content = Get-Content $file -Raw
    
    if ($content -match "清除导航属性") {
        Write-Host "✅ 包含导航属性清除修复" -ForegroundColor Green
    }
    
    if ($content -match "ProjectId = existingCharacter.ProjectId") {
        Write-Host "✅ 包含ProjectId保护修复" -ForegroundColor Green
    }
    
    if ($content -match "UpdateAsync.*character.*cancellationToken") {
        Write-Host "✅ 包含UpdateAsync调用修复" -ForegroundColor Green
    }
} else {
    Write-Host "❌ 文件不存在" -ForegroundColor Red
}

# 检查最新日志
Write-Host ""
Write-Host "检查最新错误日志..." -ForegroundColor Yellow
$logFile = "logs\app-20250606.txt"

if (Test-Path $logFile) {
    $errors = Get-Content $logFile | Select-String -Pattern "编辑角色失败|UNIQUE constraint failed.*Projects.Name" | Select-Object -Last 3
    
    if ($errors) {
        Write-Host "⚠️ 发现最近错误:" -ForegroundColor Yellow
        foreach ($error in $errors) {
            Write-Host "  $error" -ForegroundColor Red
        }
    } else {
        Write-Host "✅ 未发现最近的角色编辑错误" -ForegroundColor Green
    }
} else {
    Write-Host "⚠️ 日志文件不存在" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🎯 修复要点:" -ForegroundColor Cyan
Write-Host "1. 清除导航属性避免级联更新" -ForegroundColor White
Write-Host "2. 保护ProjectId避免唯一约束冲突" -ForegroundColor White
Write-Host "3. 正确使用UpdateAsync方法" -ForegroundColor White

Write-Host ""
Write-Host "📋 测试建议:" -ForegroundColor Cyan
Write-Host "1. 启动应用程序" -ForegroundColor White
Write-Host "2. 进入角色管理界面" -ForegroundColor White
Write-Host "3. 编辑现有角色并保存" -ForegroundColor White
Write-Host "4. 观察是否还有错误" -ForegroundColor White

Write-Host ""
Write-Host "✅ 验证完成！" -ForegroundColor Green
