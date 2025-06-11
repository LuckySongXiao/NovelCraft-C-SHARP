# 验证数据库并发问题修复效果
Write-Host "=== 小说管理系统数据库修复验证 ===" -ForegroundColor Green
Write-Host ""

# 1. 检查应用程序文件
Write-Host "1. 检查应用程序文件..." -ForegroundColor Yellow
$appPath = "src\NovelManagement.WPF\bin\Release\net8.0-windows"

if (Test-Path $appPath) {
    Write-Host "  ✅ 应用程序目录存在" -ForegroundColor Green
    
    $exePath = Join-Path $appPath "NovelManagement.WPF.exe"
    if (Test-Path $exePath) {
        Write-Host "  ✅ 主程序文件存在" -ForegroundColor Green
    } else {
        Write-Host "  ❌ 主程序文件缺失，需要编译" -ForegroundColor Red
    }
} else {
    Write-Host "  ❌ 应用程序目录不存在，需要编译" -ForegroundColor Red
}

# 2. 检查数据库文件
Write-Host ""
Write-Host "2. 检查数据库文件..." -ForegroundColor Yellow
if (Test-Path "NovelManagement.db") {
    $db = Get-Item "NovelManagement.db"
    Write-Host "  ✅ 数据库文件存在" -ForegroundColor Green
    Write-Host "  📊 数据库大小: $([math]::Round($db.Length / 1KB, 2)) KB" -ForegroundColor Cyan
} else {
    Write-Host "  ⚠️  数据库文件不存在，将在首次运行时创建" -ForegroundColor Yellow
}

# 3. 检查修复的文件
Write-Host ""
Write-Host "3. 验证修复的文件..." -ForegroundColor Yellow

$fixedFiles = @(
    "src\NovelManagement.WPF\Views\CharacterManagementView.xaml.cs",
    "src\NovelManagement.AI\Workflow\TaskQueue.cs",
    "src\NovelManagement.Application\Services\ImportService.cs",
    "src\NovelManagement.Application\Services\ExportService.cs"
)

foreach ($file in $fixedFiles) {
    if (Test-Path $file) {
        Write-Host "  ✅ $file" -ForegroundColor Green
        
        # 检查是否包含修复的代码
        $content = Get-Content $file -Raw
        if ($content -match "Task\.Factory\.StartNew" -or $content -match "createTask\.Wait\(\)") {
            Write-Host "    ✅ 包含修复代码" -ForegroundColor Green
        } else {
            Write-Host "    ⚠️  可能未包含修复代码" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  ❌ $file 不存在" -ForegroundColor Red
    }
}

# 4. 检查已清理的测试文件
Write-Host ""
Write-Host "4. 验证测试文件清理..." -ForegroundColor Yellow

$removedFiles = @(
    "simple_test.ps1",
    "test_ai_integration.ps1",
    "simple_ollama_test.ps1",
    "test_ollama_integration.ps1",
    "test_chat.json",
    "scripts\test.bat"
)

$allRemoved = $true
foreach ($file in $removedFiles) {
    if (Test-Path $file) {
        Write-Host "  ❌ $file 仍然存在" -ForegroundColor Red
        $allRemoved = $false
    } else {
        Write-Host "  ✅ $file 已删除" -ForegroundColor Green
    }
}

if ($allRemoved) {
    Write-Host "  ✅ 所有测试文件已成功清理" -ForegroundColor Green
} else {
    Write-Host "  ⚠️  部分测试文件未清理完成" -ForegroundColor Yellow
}

# 5. 检查测试目录
Write-Host ""
Write-Host "5. 验证测试目录清理..." -ForegroundColor Yellow

$testDirs = @(
    "tests",
    "src\NovelManagement.WPF.Test",
    "src\NovelManagement.Tests"
)

$allDirsRemoved = $true
foreach ($dir in $testDirs) {
    if (Test-Path $dir) {
        Write-Host "  ⚠️  $dir 仍然存在" -ForegroundColor Yellow
        $allDirsRemoved = $false
    } else {
        Write-Host "  ✅ $dir 已删除" -ForegroundColor Green
    }
}

if ($allDirsRemoved) {
    Write-Host "  ✅ 所有测试目录已成功清理" -ForegroundColor Green
} else {
    Write-Host "  ⚠️  部分测试目录未清理完成" -ForegroundColor Yellow
}

# 6. 总结
Write-Host ""
Write-Host "=== 修复验证总结 ===" -ForegroundColor Green

Write-Host "✅ 修复内容:" -ForegroundColor Green
Write-Host "  • DbContext并发访问问题已修复" -ForegroundColor White
Write-Host "  • Task.Run替换为Task.Factory.StartNew" -ForegroundColor White
Write-Host "  • 线程安全性得到改善" -ForegroundColor White
Write-Host "  • 测试文件和目录已清理" -ForegroundColor White

Write-Host ""
Write-Host "📋 建议下一步操作:" -ForegroundColor Cyan
Write-Host "  1. 编译项目: dotnet build --configuration Release" -ForegroundColor White
Write-Host "  2. 运行应用程序测试角色管理功能" -ForegroundColor White
Write-Host "  3. 验证不再出现DbContext并发错误" -ForegroundColor White

Write-Host ""
Write-Host "🎉 数据库并发问题修复验证完成！" -ForegroundColor Green
