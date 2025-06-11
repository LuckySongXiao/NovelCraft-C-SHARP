# C#环境检测脚本
# 用于验证小说管理系统的开发和运行环境

Write-Host "=== 小说管理系统 C# 环境检测 ===" -ForegroundColor Green
Write-Host ""

# 检测结果统计
$checksPassed = 0
$checksTotal = 0

function Test-Requirement {
    param(
        [string]$Name,
        [scriptblock]$Test,
        [string]$SuccessMessage,
        [string]$FailureMessage,
        [string]$Recommendation = ""
    )
    
    $script:checksTotal++
    Write-Host "检测 $Name..." -ForegroundColor Yellow
    
    try {
        $result = & $Test
        if ($result) {
            Write-Host "  ✅ $SuccessMessage" -ForegroundColor Green
            $script:checksPassed++
            return $true
        } else {
            Write-Host "  ❌ $FailureMessage" -ForegroundColor Red
            if ($Recommendation) {
                Write-Host "  💡 建议: $Recommendation" -ForegroundColor Cyan
            }
            return $false
        }
    } catch {
        Write-Host "  ❌ $FailureMessage" -ForegroundColor Red
        Write-Host "  🔍 错误详情: $($_.Exception.Message)" -ForegroundColor DarkRed
        if ($Recommendation) {
            Write-Host "  💡 建议: $Recommendation" -ForegroundColor Cyan
        }
        return $false
    }
}

# 1. 检测 .NET SDK
Test-Requirement -Name ".NET SDK" -Test {
    $version = dotnet --version 2>$null
    if ($version -and $version -match "^8\.") {
        Write-Host "    版本: $version" -ForegroundColor White
        return $true
    }
    return $false
} -SuccessMessage ".NET 8.0 SDK 已安装" -FailureMessage ".NET 8.0 SDK 未安装或版本不正确" -Recommendation "请从 https://dotnet.microsoft.com/download/dotnet/8.0 下载安装"

# 2. 检测 .NET 运行时
Test-Requirement -Name ".NET 运行时" -Test {
    $runtimes = dotnet --list-runtimes 2>$null
    if ($runtimes -match "Microsoft\.NETCore\.App 8\.") {
        $netcoreVersion = ($runtimes | Where-Object { $_ -match "Microsoft\.NETCore\.App 8\." } | Select-Object -First 1)
        Write-Host "    .NET Core: $netcoreVersion" -ForegroundColor White
        return $true
    }
    return $false
} -SuccessMessage ".NET 8.0 运行时已安装" -FailureMessage ".NET 8.0 运行时未安装" -Recommendation "安装 .NET 8.0 Runtime"

# 3. 检测 Windows Desktop 运行时 (WPF支持)
Test-Requirement -Name "Windows Desktop 运行时" -Test {
    $runtimes = dotnet --list-runtimes 2>$null
    if ($runtimes -match "Microsoft\.WindowsDesktop\.App 8\.") {
        $desktopVersion = ($runtimes | Where-Object { $_ -match "Microsoft\.WindowsDesktop\.App 8\." } | Select-Object -First 1)
        Write-Host "    Desktop Runtime: $desktopVersion" -ForegroundColor White
        return $true
    }
    return $false
} -SuccessMessage "Windows Desktop 运行时已安装" -FailureMessage "Windows Desktop 运行时未安装" -Recommendation "安装 .NET 8.0 Desktop Runtime (WPF应用程序必需)"

# 4. 检测 MSBuild
Test-Requirement -Name "MSBuild" -Test {
    $msbuild = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($msbuild) {
        Write-Host "    路径: $($msbuild.Source)" -ForegroundColor White
        return $true
    }
    # 尝试通过dotnet调用MSBuild
    $buildResult = dotnet build --help 2>$null
    return $buildResult -ne $null
} -SuccessMessage "MSBuild 可用" -FailureMessage "MSBuild 未找到" -Recommendation "安装 Visual Studio 或 .NET SDK"

# 5. 检测 Git (可选)
Test-Requirement -Name "Git 版本控制" -Test {
    $git = Get-Command git -ErrorAction SilentlyContinue
    if ($git) {
        $version = git --version 2>$null
        Write-Host "    版本: $version" -ForegroundColor White
        return $true
    }
    return $false
} -SuccessMessage "Git 已安装" -FailureMessage "Git 未安装 (可选)" -Recommendation "从 https://git-scm.com/ 下载安装 (推荐)"

# 6. 检测项目文件
Write-Host ""
Write-Host "检测项目文件..." -ForegroundColor Yellow

$projectFiles = @(
    "NovelManagementSystem.sln",
    "src\NovelManagement.WPF\NovelManagement.WPF.csproj",
    "src\NovelManagement.Core\NovelManagement.Core.csproj",
    "src\NovelManagement.Infrastructure\NovelManagement.Infrastructure.csproj"
)

$projectFilesFound = 0
foreach ($file in $projectFiles) {
    if (Test-Path $file) {
        Write-Host "  ✅ $file" -ForegroundColor Green
        $projectFilesFound++
    } else {
        Write-Host "  ❌ $file" -ForegroundColor Red
    }
}

if ($projectFilesFound -eq $projectFiles.Count) {
    Write-Host "  ✅ 所有项目文件完整" -ForegroundColor Green
    $checksPassed++
} else {
    Write-Host "  ⚠️  部分项目文件缺失" -ForegroundColor Yellow
}
$checksTotal++

# 7. 尝试编译项目
Write-Host ""
Write-Host "尝试编译项目..." -ForegroundColor Yellow

if (Test-Path "NovelManagementSystem.sln") {
    try {
        $buildOutput = dotnet build NovelManagementSystem.sln --verbosity quiet 2>&1
        $buildExitCode = $LASTEXITCODE
        
        if ($buildExitCode -eq 0) {
            Write-Host "  ✅ 项目编译成功" -ForegroundColor Green
            $checksPassed++
        } else {
            Write-Host "  ⚠️  项目编译有警告或错误" -ForegroundColor Yellow
            Write-Host "  🔍 建议运行: dotnet build 查看详细信息" -ForegroundColor Cyan
        }
    } catch {
        Write-Host "  ❌ 编译失败" -ForegroundColor Red
        Write-Host "  🔍 错误: $($_.Exception.Message)" -ForegroundColor DarkRed
    }
} else {
    Write-Host "  ❌ 解决方案文件未找到" -ForegroundColor Red
}
$checksTotal++

# 8. 检测系统信息
Write-Host ""
Write-Host "系统信息:" -ForegroundColor Yellow
Write-Host "  操作系统: $([System.Environment]::OSVersion.VersionString)" -ForegroundColor White
Write-Host "  .NET 版本: $([System.Environment]::Version)" -ForegroundColor White
Write-Host "  处理器架构: $([System.Environment]::ProcessorCount) 核心, $([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture)" -ForegroundColor White
Write-Host "  工作目录: $(Get-Location)" -ForegroundColor White

# 总结
Write-Host ""
Write-Host "=== 检测结果总结 ===" -ForegroundColor Green
Write-Host "通过检测: $checksPassed / $checksTotal" -ForegroundColor White

$percentage = [math]::Round(($checksPassed / $checksTotal) * 100, 1)

if ($percentage -ge 90) {
    Write-Host "🎉 环境配置优秀 ($percentage%)！可以开始开发。" -ForegroundColor Green
} elseif ($percentage -ge 70) {
    Write-Host "✅ 环境配置良好 ($percentage%)，建议完善缺失项。" -ForegroundColor Yellow
} elseif ($percentage -ge 50) {
    Write-Host "⚠️  环境配置基本 ($percentage%)，需要安装缺失组件。" -ForegroundColor Yellow
} else {
    Write-Host "❌ 环境配置不足 ($percentage%)，请安装必需组件。" -ForegroundColor Red
}

Write-Host ""
Write-Host "📋 下一步操作建议:" -ForegroundColor Cyan
if ($checksPassed -lt $checksTotal) {
    Write-Host "1. 根据上述建议安装缺失组件" -ForegroundColor White
    Write-Host "2. 重新运行此检测脚本验证" -ForegroundColor White
    Write-Host "3. 查看 'C#环境要求.md' 获取详细信息" -ForegroundColor White
} else {
    Write-Host "1. 运行: dotnet run --project src/NovelManagement.WPF" -ForegroundColor White
    Write-Host "2. 开始开发或测试应用程序" -ForegroundColor White
}

Write-Host ""
Write-Host "📖 更多信息请参考: C#环境要求.md" -ForegroundColor Cyan
Write-Host "🔧 如有问题，请检查项目文档或联系开发团队" -ForegroundColor Cyan

# 暂停以便查看结果
Write-Host ""
Write-Host "按任意键退出..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
