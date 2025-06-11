# C#环境要求文档

## 📋 系统环境检测结果

### 🎯 .NET 版本要求

#### 目标框架
- **主要项目**: .NET 8.0 (`net8.0`)
- **WPF项目**: .NET 8.0 Windows (`net8.0-windows`)
- **C# 语言版本**: Latest (C# 12.0)

#### 当前检测到的环境
- **MSBuild版本**: 17.9.4+90725d08d
- **Visual Studio版本**: Visual Studio 2022 (Version 17)
- **最低Visual Studio版本**: 10.0.40219.1

## 🔧 开发环境要求

### 必需软件

#### 1. .NET SDK
- **版本**: .NET 8.0 SDK 或更高版本
- **下载地址**: https://dotnet.microsoft.com/download/dotnet/8.0
- **验证命令**: `dotnet --version`

#### 2. Visual Studio (推荐)
- **版本**: Visual Studio 2022 (17.0 或更高)
- **版本**: Visual Studio 2019 (16.11 或更高，支持.NET 8.0)
- **必需工作负载**:
  - .NET 桌面开发
  - ASP.NET 和 Web 开发
  - .NET 跨平台开发

#### 3. 替代IDE选项
- **Visual Studio Code** + C# 扩展
- **JetBrains Rider** 2023.2 或更高版本
- **Visual Studio for Mac** (如果在macOS上开发)

### 运行时要求

#### Windows 系统
- **操作系统**: Windows 10 版本 1607 或更高版本
- **操作系统**: Windows 11 (推荐)
- **架构**: x64, x86, ARM64

#### .NET 运行时
- **.NET 8.0 Runtime** (用户机器)
- **.NET 8.0 Desktop Runtime** (WPF应用程序)

## 📦 项目依赖包版本

### 核心框架包
```xml
<!-- Entity Framework Core -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.5" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.5" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.5" />

<!-- Microsoft Extensions -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.5" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.5" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.5" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.5" />
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.5" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.5" />
```

### UI框架包
```xml
<!-- WPF UI 框架 -->
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
<PackageReference Include="MaterialDesignThemes" Version="4.9.0" />
<PackageReference Include="MaterialDesignColors" Version="2.1.4" />
<PackageReference Include="HandyControl" Version="3.4.0" />
```

### 日志和工具包
```xml
<!-- 日志框架 -->
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.Extensions.Hosting" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />

<!-- 文档处理 -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="DocumentFormat.OpenXml" Version="3.0.1" />
<PackageReference Include="iTextSharp" Version="5.5.13.3" />
<PackageReference Include="BouncyCastle" Version="1.8.9" />
```

## ⚙️ 项目配置

### 编译器设置
```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
  <LangVersion>latest</LangVersion>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
</PropertyGroup>
```

### WPF特定配置
```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net8.0-windows</TargetFramework>
  <UseWPF>true</UseWPF>
</PropertyGroup>
```

## 🚀 环境验证

### 验证步骤

#### 1. 检查.NET版本
```bash
dotnet --version
# 应显示: 8.0.x 或更高版本
```

#### 2. 检查.NET信息
```bash
dotnet --info
# 查看完整的.NET安装信息
```

#### 3. 验证项目编译
```bash
# 在项目根目录执行
dotnet restore
dotnet build
```

#### 4. 验证项目运行
```bash
# 运行WPF应用程序
cd src/NovelManagement.WPF
dotnet run
```

### 常见问题解决

#### 问题1: .NET 8.0 未安装
**解决方案**: 
- 下载并安装.NET 8.0 SDK
- 重启开发环境

#### 问题2: Visual Studio版本过低
**解决方案**:
- 更新到Visual Studio 2022
- 或安装.NET 8.0支持组件

#### 问题3: 包兼容性警告
**现象**: BouncyCastle和iTextSharp包的.NET Framework兼容性警告
**影响**: 不影响功能，仅为兼容性提示
**解决方案**: 可以忽略，或考虑升级到.NET 8.0兼容版本

## 📊 性能要求

### 最低硬件要求
- **内存**: 4GB RAM (推荐8GB或更多)
- **存储**: 2GB可用磁盘空间
- **处理器**: 1.8 GHz或更快的处理器

### 推荐硬件配置
- **内存**: 16GB RAM或更多
- **存储**: SSD硬盘，10GB可用空间
- **处理器**: 多核处理器，2.5 GHz或更快

## 🔍 开发工具推荐

### 必备扩展 (Visual Studio)
- **C# Dev Kit** (如果使用VS Code)
- **NuGet Package Manager**
- **Entity Framework Core Power Tools**
- **XAML Styler**

### 可选工具
- **Git** (版本控制)
- **Windows Terminal** (命令行工具)
- **Postman** (API测试)
- **DB Browser for SQLite** (数据库查看)

## 📝 注意事项

### 重要提醒
1. **确保安装.NET 8.0 SDK**，不仅仅是运行时
2. **WPF应用程序需要Windows系统**，不支持跨平台
3. **某些包可能显示兼容性警告**，但不影响功能
4. **建议使用Visual Studio 2022**获得最佳开发体验

### 版本兼容性
- **向下兼容**: 支持.NET 8.0及更高版本
- **向上兼容**: 可以升级到.NET 9.0（发布后）
- **长期支持**: .NET 8.0是LTS版本，支持到2026年

---

**📅 文档更新日期**: 2024年12月19日  
**🔄 检测环境**: Windows + .NET 8.0 + Visual Studio 2022  
**✅ 验证状态**: 已验证编译和运行成功
