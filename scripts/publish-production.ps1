param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Framework = "net8.0-windows"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ProjectFile = Join-Path $ProjectRoot "src\NovelManagement.WPF\NovelManagement.WPF.csproj"
$PublishRoot = Join-Path $ProjectRoot "artifacts\publish"
$PublishDirectory = Join-Path $PublishRoot "$Configuration-$Runtime"
$ExecutableName = "NovelManagement.WPF.exe"

function Assert-LastExitCode {
    param(
        [string]$StepName
    )

    if ($LASTEXITCODE -ne 0) {
        throw "$StepName failed with exit code $LASTEXITCODE"
    }
}

Write-Host "========================================"
Write-Host "Novel Management - Production Publish"
Write-Host "========================================"
Write-Host "Project: $ProjectFile"
Write-Host "Output:  $PublishDirectory"

if (-not (Test-Path $ProjectFile)) {
    throw "Project file not found: $ProjectFile"
}

if (Test-Path $PublishDirectory) {
    Remove-Item $PublishDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $PublishDirectory -Force | Out-Null

Write-Host ""
Write-Host "1. Restoring packages..."
dotnet restore $ProjectFile -r $Runtime
Assert-LastExitCode "dotnet restore"

Write-Host ""
Write-Host "2. Publishing project..."
dotnet publish $ProjectFile `
    -c $Configuration `
    -r $Runtime `
    -f $Framework `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:UseAppHost=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $PublishDirectory
Assert-LastExitCode "dotnet publish"

Get-ChildItem -Path $PublishDirectory -File |
    Where-Object { $_.Name -ne $ExecutableName } |
    Remove-Item -Force

Get-ChildItem -Path $PublishDirectory -Directory |
    Remove-Item -Recurse -Force

$ExecutablePath = Join-Path $PublishDirectory $ExecutableName
if (-not (Test-Path $ExecutablePath)) {
    throw "Published executable not found: $ExecutablePath"
}

Write-Host ""
Write-Host "Publish completed."
Write-Host "Executable: $ExecutablePath"
