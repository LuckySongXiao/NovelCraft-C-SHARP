@echo off
echo ========================================
echo 小说管理系统 - 构建脚本
echo ========================================

:: 设置变量
set SOLUTION_FILE=NovelManagementSystem.sln
set BUILD_CONFIG=Release
set OUTPUT_DIR=bin\Release

:: 检查 .NET SDK
echo 检查 .NET SDK...
dotnet --version
if %ERRORLEVEL% neq 0 (
    echo 错误: 未找到 .NET SDK，请先安装 .NET 8.0 SDK
    pause
    exit /b 1
)

:: 清理之前的构建
echo.
echo 清理之前的构建...
dotnet clean %SOLUTION_FILE% --configuration %BUILD_CONFIG%
if %ERRORLEVEL% neq 0 (
    echo 错误: 清理失败
    pause
    exit /b 1
)

:: 还原 NuGet 包
echo.
echo 还原 NuGet 包...
dotnet restore %SOLUTION_FILE%
if %ERRORLEVEL% neq 0 (
    echo 错误: NuGet 包还原失败
    pause
    exit /b 1
)

:: 构建解决方案
echo.
echo 构建解决方案...
dotnet build %SOLUTION_FILE% --configuration %BUILD_CONFIG% --no-restore
if %ERRORLEVEL% neq 0 (
    echo 错误: 构建失败
    pause
    exit /b 1
)

:: 运行测试
echo.
echo 运行测试...
dotnet test %SOLUTION_FILE% --configuration %BUILD_CONFIG% --no-build --verbosity normal
if %ERRORLEVEL% neq 0 (
    echo 警告: 测试失败，但构建继续
)

:: 发布应用程序
echo.
echo 发布应用程序...
dotnet publish src\NovelManagement.WPF\NovelManagement.WPF.csproj --configuration %BUILD_CONFIG% --output publish --no-build
if %ERRORLEVEL% neq 0 (
    echo 错误: 发布失败
    pause
    exit /b 1
)

echo.
echo ========================================
echo 构建完成！
echo 输出目录: publish
echo ========================================
pause
