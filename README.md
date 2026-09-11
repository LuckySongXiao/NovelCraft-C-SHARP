# 书籍管理系统 NovelCraft C-SHARP

**中文** | [English](#english)

---

## 中文

### 项目概述

这是一个功能完整的书籍创作和管理系统，采用C# WPF技术栈开发，支持多Agent AI协作创作、完整的内容管理、设定管理等功能。系统设计高度模块化，具有良好的可扩展性和维护性。应用启动密码：RWKV7_20260908

### 应用截图

| | |
|---|---|
| ![仪表盘](docs/images/01-dashboard.png) | ![项目概览](docs/images/03-project-overview.png) |
| *仪表盘 Dashboard* | *项目概览 Project Overview* |
| ![角色管理](docs/images/07-character-management.png) | ![剧情管理](docs/images/06-plot-management.png) |
| *角色管理 Character Management* | *剧情管理 Plot Management* |
| ![AI 创作助手](docs/images/13-ai-collaboration.png) | ![发布与运维管理](docs/images/18-publish-management.png) |
| *AI 创作助手 AI Copilot* | *发布与运维管理 Publishing & Maintenance* |

> 更多页面截图（19 个功能页中英双语说明）见 [项目功能说明书_双语_User_Guide.md](docs/项目功能说明书_双语_User_Guide.md)。

### 系统架构

#### 技术栈
- **开发语言**: C# (.NET 8.0)
- **UI框架**: WPF (Windows Presentation Foundation)
- **数据库**: SQLite (本地存储) + Entity Framework Core
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **日志系统**: Serilog
- **配置管理**: Microsoft.Extensions.Configuration
- **AI集成**: 支持多种AI提供商 (OpenAI, DeepSeek, RWKV等)
- **文档处理**: DocumentFormat.OpenXml, iTextSharp
- **数据序列化**: Newtonsoft.Json

#### 架构分层

```
┌─────────────────────────────────────────────────────────────┐
│                    表现层 (Presentation Layer)                │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   主界面模块     │  │   项目管理界面   │  │   内容管理界面   │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   设定管理界面   │  │   AI助手界面    │  │   统计报告界面   │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│                   应用服务层 (Application Layer)               │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   项目应用服务   │  │   内容应用服务   │  │   AI协作服务    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   导入导出服务   │  │   统计报告服务   │  │   配置管理服务   │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│                   业务逻辑层 (Domain Layer)                    │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   项目管理服务   │  │   卷宗管理服务   │  │   章节管理服务   │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   人物管理服务   │  │   势力管理服务   │  │   设定管理服务   │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   AI Agent引擎   │  │   工作流引擎    │  │   记忆管理系统   │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│                   基础设施层 (Infrastructure Layer)            │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   数据访问层     │  │   文件系统服务   │  │   AI提供商集成   │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   日志系统       │  │   配置系统      │  │   缓存系统      │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└────────────────────────────────────────────────────────────────┘
```

### 核心功能模块

#### 1. 项目管理系统
- **项目创建与配置**: 支持创建新的书籍项目，配置基本信息
- **项目概览**: 显示项目统计信息、创作进度、最近活动
- **多项目管理**: 支持同时管理多个书籍项目
- **项目模板**: 提供不同类型书籍的项目模板

#### 2. 卷宗管理系统
- **卷宗结构管理**: 支持多层级的卷宗结构
- **章节管理**: 章节的创建、编辑、排序、统计
- **内容编辑器**: 富文本编辑器，支持格式化、插入图片等
- **版本控制**: 章节内容的版本历史和回滚功能

#### 3. 内容管理系统
- **人物管理**: 角色档案、关系网络、发展轨迹
- **势力管理**: 组织架构、势力关系、地盘分布
- **剧情管理**: 主线支线、伏笔设定、时间线管理
- **资源管理**: 各类资源的分布和统计
- **关系网络**: 可视化的关系图谱

#### 4. 设定管理系统
- **世界观设定**: 地理、历史、文化、政治体系、自然法则、社会结构
- **修炼体系**: 完整的修炼等级体系、力量来源、境界划分、能力分类、禁忌之术
- **政治体系**: 政府类型、权力结构、法律体系、政治关系
- **货币体系**: 货币制度、基础货币、发行机构、经济指标、金融服务
- **商业体系**: 经济制度、市场结构、贸易体系、商业组织、市场机制
- **种族类别**: 种族分类、生理特征、文化特征、社会结构、种族关系
- **功法体系**: 功法分类、修炼要求、技能招式、传承信息
- **装备体系**: 装备分类、品级系统、属性系统、强化系统、制作信息
- **宠物体系**: 宠物分类、稀有度、技能系统、进化系统、培养系统
- **地图结构**: 多层级地图、地形类型、气候类型、资源分布、特殊区域
- **维度结构**: 维度分类、稳定性、访问等级、环境特征、连接传送
- **灵宝体系**: 灵宝分类、品级等级、灵性等级、炼制信息、器灵系统
- **时间线管理**: 时间线类型、事件管理、关联关系、时间特性
- **生民体系**: 人口统计、社会阶层、教育体系、医疗体系、文化生活
- **司法体系**: 法律体系、法院体系、审判程序、执法机构、刑罚制度
- **职业体系**: 职业分类、技能体系、晋升路径、薪酬体系、工作环境

#### 5. AI Agent协作系统
- **多Agent工作流**: 编剧、作家、总结、读者、设定管理Agent协作创作
- **智能对话创作**: 多轮对话引擎、上下文记忆、意图识别、智能问题生成
- **内容生成引擎**: 世界设定生成、人物角色生成、剧情故事生成、章节续写
- **一致性检查**: 人物设定一致性、世界观逻辑性、时间线合理性、剧情连贯性
- **AI协作工作流**: 任务调度管理、AI协作协调、人工干预支持、质量控制机制
- **智能辅助功能**: 模板库管理、批量生成、历史记录、思维链展示、参数调节
- **项目数据集成**: 全项目数据读取、智能数据分析、自动数据更新、跨模块协作
- **超长文本处理**: 分层记忆架构、智能记忆压缩、渐进式处理、上下文传递
- **多AI提供商支持**: OpenAI、Claude、智谱AI、硅基流动、谷歌AI、GROK3、Ollama本地
- **AI功能测试**: 基础API测试、功能模块测试、性能压力测试、质量评估、调试诊断

#### 6. 导入导出系统
- **多格式支持**: Excel(.xlsx)、Word(.docx)、TXT、JSON、Markdown、PDF、EPUB
- **智能解析**: 自动识别文档结构、内容类型、章节划分、人物信息提取
- **成建制导出**: 完整项目数据导出、目录结构生成、统计报告、说明文档
- **部分内容导出**: 选择性模块导出、按维度导出、保持关联关系
- **批量操作**: 支持批量导入导出、格式转换、增量导入、冲突解决
- **发布格式**: 起点中文网、晋江文学网、纵横中文网、通用发布格式
- **智能导入**: 书籍文本解析、信息提取、数据结构化、交互式完善
- **版本控制**: 导入导出历史、版本回滚、数据备份、完整性检查

#### 7. 统计报告系统
- **创作统计**: 字数统计、章节统计、卷宗统计、创作进度、效率分析
- **内容分析**: 人物出场频率、势力影响力、剧情发展分析、关系网络分析
- **设定统计**: 世界观完整性、修炼体系统计、装备分布、资源统计
- **质量评估**: 内容质量评分、一致性检查、逻辑性分析、改进建议
- **数据可视化**: 统计图表、关系图谱、地图分布、发展趋势
- **报告生成**: 项目概览报告、详细统计报告、质量分析报告、导出报告
- **实时监控**: 创作进度监控、数据变化追踪、异常检测、性能监控

### 项目结构

```
06_NovelCraft-C-SHARP/
├── src/
│   ├── NovelManagement.Core/              # 核心领域模型
│   │   ├── Entities/                      # 实体类
│   │   ├── ValueObjects/                  # 值对象
│   │   ├── Enums/                         # 枚举类型
│   │   ├── Interfaces/                    # 核心接口
│   │   └── Exceptions/                    # 自定义异常
│   │
│   ├── NovelManagement.Infrastructure/    # 基础设施层
│   │   ├── Data/                          # 数据访问（SQLite + EF Core）
│   │   ├── FileSystem/                    # 文件系统服务
│   │   ├── Logging/                       # 日志系统
│   │   ├── Configuration/                 # 配置管理
│   │   └── Cache/                         # 缓存系统
│   │
│   ├── NovelManagement.Application/       # 应用服务层
│   │   ├── Services/                      # 应用服务（含批量生成/前置条件生成）
│   │   ├── DTOs/                          # 数据传输对象
│   │   ├── Interfaces/                    # 应用接口
│   │   └── Validators/                    # 数据验证
│   │
│   ├── NovelManagement.Domain/            # 业务逻辑层
│   │   ├── Services/                      # 领域服务
│   │   └── Repositories/                  # 仓储接口
│   │
│   ├── NovelManagement.AI/                # AI集成层
│   │   ├── Agents/                        # AI Agent（编剧/作家/总结/读者/设定管理）
│   │   ├── Services/RWKV/                 # RWKV 推理服务（llama.cpp / CUDA / libtorch 三形态）
│   │   ├── Workflows/                     # 工作流引擎
│   │   ├── Memory/                        # 记忆管理
│   │   └── Providers/                     # AI 提供商
│   │
│   ├── NovelManagement.WPF/               # WPF表现层
│   │   ├── Views/                         # 视图（含 Copilot 创作助手抽屉）
│   │   ├── Services/                      # UI 服务（含 Copilot 会话/意图/流水线）
│   │   ├── Controls/                      # 自定义控件
│   │   ├── Converters/                    # 值转换器
│   │   └── Styles/                        # 样式资源（多主题/多皮肤）
│   │
│   └── NovelManagement.Tests/             # 单元测试（120 用例全通过）
│
├── rwkv_models/                           # RWKV 模型文件（.gguf，不入库）
├── llama_cpp/                             # llama-server 本地推理运行时
├── RWKV_lightning_CUDA_win/               # RWKV CUDA 推理运行时（备用）
├── rwkv_lightning_libtorch_win/           # RWKV libtorch 推理运行时（备用）
├── scripts/                               # 构建和辅助脚本
├── docs/                                  # 文档（功能说明书/使用手册）
├── 项目交接.md                            # 开发交接文档（增量更新日志）
└── NovelManagementSystem.sln              # 解决方案文件
```

### 开发环境要求

#### 系统要求
- **操作系统**: Windows 10 或更高版本
- **开发环境**: Visual Studio 2022 或 Visual Studio Code
- **.NET版本**: .NET 8.0 或更高版本
- **数据库**: SQLite (无需额外安装)
- **AI推理（可选）**: 本地 llama-server（`llama_cpp\` 内置）或任意兼容 OpenAI completions 的服务

### 安装和运行

#### 1. 克隆项目
```bash
git clone [项目地址]
cd 06_NovelCraft-C-SHARP
```

#### 2. 还原依赖包
```bash
dotnet restore
```

#### 3. 构建项目
```bash
dotnet build
```

#### 4. 运行应用
```bash
dotnet run --project src/NovelManagement.WPF
```

#### 5. 运行测试
```bash
dotnet test
```

### 核心特性

#### 高度模块化设计
- 采用分层架构，各层职责清晰
- 使用依赖注入，降低耦合度
- 支持插件式扩展，便于功能增强

#### 智能AI协作
- 多Agent协作工作流
- 智能内容生成和优化
- 上下文感知的创作辅助
- 质量评估和改进建议

#### 完整的内容管理
- 结构化的内容组织
- 丰富的关系管理
- 可视化的数据展示
- 灵活的查询和筛选

#### 强大的导入导出
- 多种文件格式支持
- 智能内容解析
- 批量操作功能
- 发布格式适配

#### 数据安全保障
- 本地数据存储
- 自动备份机制
- 版本控制功能
- 数据完整性检查

### 扩展性设计

#### 插件系统
系统支持插件式扩展，可以轻松添加新功能：
- AI提供商插件
- 导入导出格式插件
- 自定义报表插件
- 第三方集成插件

#### 配置系统
灵活的配置管理，支持：
- 用户个性化设置
- AI模型配置
- 界面主题配置
- 功能开关配置

#### 国际化支持
内置国际化框架，支持：
- 中英双语界面一键切换
- 本地化资源
- 区域化设置
- 文化适配

### 安全性考虑

#### 数据安全
- 本地数据存储，避免隐私泄露
- 数据加密存储选项
- 访问权限控制
- 操作日志记录

#### AI安全
- API密钥安全管理
- 请求频率限制
- 内容过滤机制
- 错误处理和恢复

### 性能优化

#### 内存管理
- 大文本分段处理
- 智能缓存策略
- 内存使用监控
- 垃圾回收优化

#### 响应性能
- 异步操作处理
- 后台任务调度
- 进度反馈机制
- 用户体验优化

### AI Agent协作工作流程详解

#### 核心Agent角色定义

##### 1. 编剧Agent (Director AI)
- **职责**: 总体剧情架构师，负责大纲、设定、主线规划
- **推荐模型**: rwkv7-g1j-13.3b-20260831-ctx16384-FP16 (推理能力强，适合逻辑构建)
- **核心功能**: 大纲生成、设定构建、主线规划、章节安排、剧情调整

##### 2. 作家Agent (Writer AI)
- **职责**: 章节内容创作者，负责具体文本生成
- **推荐模型**: rwkv7-g1j-13.3b-20260831-ctx16384-FP16 (文本生成能力强，创意丰富)
- **核心功能**: 章节内容生成、对话描写、场景描述、心理刻画、文风保持

##### 3. 总结Agent (Summarizer AI)
- **职责**: 内容整理和承上启下，负责总结和前言
- **推荐模型**: rwkv7-g1j-13.3b-20260831-ctx16384-FP16 (长文本处理能力强)
- **核心功能**: 章节总结、卷宗总结、前言生成、关键信息提取、连贯性维护

##### 4. 读者Agent (Reader AI)
- **职责**: 模拟读者视角，提供评价和建议
- **推荐模型**: rwkv7-g1j-13.3b-20260831-ctx16384-FP16 (理解能力强，评价客观)
- **核心功能**: 内容评价、问题识别、反馈建议、质量评估、吸引力分析

##### 5. 设定管理Agent (Setting Manager AI)
- **职责**: 维护设定一致性，管理世界观数据
- **推荐模型**: rwkv7-g1j-13.3b-20260831-ctx16384-FP16 (逻辑严密，细节处理好)
- **核心功能**: 一致性维护、冲突检查、设定更新、查询服务、关联分析

#### 多Agent协作工作流程

##### 阶段1: 项目初始化和大纲创建
```
用户输入命题 → 编剧Agent分析 → 生成初步大纲 → 设定管理Agent建立基础设定库 → 读者Agent初步评估 → 编剧Agent优化大纲
```

##### 阶段2: 章节创作循环
```
编剧Agent提供章节要求 → 作家Agent生成章节内容 → 设定管理Agent一致性检查 → 读者Agent评价反馈 → 总结Agent章节总结 → 进入下一章节
```

##### 阶段3: 卷宗完成和总结
```
总结Agent卷宗总结 → 编剧Agent下卷规划 → 总结Agent生成前言 → 读者Agent整体评估 → 编剧Agent调整后续规划
```

#### 超长文本分段记忆处理机制

##### 分层记忆架构
- **全局记忆层**: 核心世界观设定、主要人物信息、整体剧情大纲
- **卷宗记忆层**: 当前卷剧情线、人物发展轨迹、重要事件
- **章节记忆层**: 当前章节内容、人物状态、场景对话
- **段落记忆层**: 当前处理文本、局部上下文、临时状态

##### 智能记忆压缩策略
- **重要性评分**: 对信息进行1-10分重要性评分
- **动态管理**: 自动压缩低重要性信息，保留核心内容
- **检索优化**: 基于任务需求智能检索相关记忆

#### 导入导出功能详解

##### 支持的文件格式
- **Excel文件 (.xlsx)**: 结构化数据导入导出，支持多工作表
- **Word文档 (.docx)**: 富文本内容导入导出，保持格式
- **纯文本 (.txt)**: 简单文本导入导出，兼容性最好
- **JSON格式 (.json)**: 完整项目数据导入导出，保持结构
- **Markdown (.md)**: 文档化导出，便于阅读和分享

##### 导出文件夹结构
```
{项目名称}_{导出时间}_{导出类型}/
├── 项目概览/
├── 卷宗管理/
├── 内容管理/
├── 设定管理/
├── 统计报告/
└── 说明文档/
```

### 📊 项目完成状态 (2026年9月8日更新 · V1.0.0 正式版)

#### 🎯 总体进度: 100% ✅

> 📦 **V1.0.0 封装版**：单文件 EXE（自包含 .NET 8 运行时，无需安装依赖）位于 `publish\NovelManagement_v1.0.0\NovelManagement.WPF.exe`（https://github.com/LuckySongXiao/NovelCraft-C-SHARP/releases）；启动时需输入**启动密码**（由交付通知提供，密码以 SHA-256 哈希校验，源码与文档不存明文）。
> 📖 **发布说明**：详见 [docs/发布说明_v1.0.0.md](docs/发布说明_v1.0.0.md)（版本更新亮点、已知问题、安装部署与升级迁移）。
> 📖 **使用手册**：详见 [docs/使用手册_v1.0.0.md](docs/使用手册_v1.0.0.md)（中文 11 章详解：安装配置、创作工作流、AI 进阶、FAQ）。
> 📖 **功能说明书与使用手册（双语）**：详见 [docs/项目功能说明书_双语_User_Guide.md](docs/项目功能说明书_双语_User_Guide.md)（中英双语，19 个功能页面使用说明，附真实运行截图）。

| 模块 | 状态 | 完成度 | 说明 |
|------|------|--------|------|
| 架构层次 | ✅ | 100% (6/6) | 所有架构层完成 |
| WPF界面 | ✅ | 100% (21/21) | 所有界面完成 |
| 应用服务 | ✅ | 100% (20/20) | 所有服务完成 |
| AI Agent系统 | ✅ | 100% (7/7) | AI系统完整 |
| AI创作助手 | ✅ | 100% | 对话式五级创作流水线（总纲→剧情线→分卷→章节草稿→章节正文） |
| 导入导出 | ✅ | 100% (7/7) | 多格式支持 |
| 测试项目 | ✅ | 120/120 | 单元测试全部通过 |

#### 🔧 最新更新 (2026年9月7-8日)
- ✅ **V1.0.0 封装版发布**: 自包含单文件 EXE（.NET 8 运行时内置、压缩后约 85MB），双击即用
- ✅ **启动密码保护**: 启动时密码门验证（SHA-256 哈希比对，不明文存储），验证未通过不加载任何数据；支持在「发布管理 → 修改启动密码」自助修改密码（自定义哈希存于用户配置，可恢复默认）
- ✅ **角色唯一性保障**: 服务层幂等防重——同项目内「同名+性格+背景」完全一致的角色自动复用，杜绝 AI 生成重复角色；存量重复数据已全量清理
- ✅ **AI 创作助手**: 对话式创作流水线上线——自然语言驱动五级流水线，确认卡采纳/修改/重生成，状态持久化支持重启续创
- ✅ **章节关联处理**: 创作助手可关联已有书籍章节，直接对目标章节改写/续写/问答，落库与流水线解耦
- ✅ **章节续写/润色修复**: 模型下拉框动态加载真实模型文件；RWKV 两级在线探测（状态 3 秒 + 推理探测 6 秒）杜绝假在线误判与长时间无响应；支持选中文本段精准润色
- ✅ **单元测试 120 全通过**: Copilot 意图/流水线/会话三套测试，并抓出章节草稿编号丢弃真 bug
- ✅ **侧栏导航持久化**: 展开状态跨重启精确恢复（`sidebar_state.json`）
- ✅ **修为等级自定义体系**: AI 自上而下设计修炼体系，角色编辑/前置生成/批量生成全链路生效

#### 📈 项目统计
- **总代码文件数**: 300+ 个文件
- **总代码行数**: 40,000+ 行
- **编译状态**: ✅ 成功 (0 错误, 0 警告)
- **单元测试**: ✅ 120/120 通过
- **UIA 回归**: ✅ 五大功能页全链路实测通过
- **启动状态**: ✅ 正常启动
- **数据库状态**: ✅ 正常连接和操作

#### 🚀 快速启动
```bash
# 1. 克隆项目
git clone [项目地址]
cd 06_NovelCraft-C-SHARP

# 2. 恢复依赖
dotnet restore

# 3. 编译项目
dotnet build

# 4. 运行应用
src\NovelManagement.WPF\bin\Debug\net8.0-windows\NovelManagement.WPF.exe
```

> **封装版用户**：无需编译，直接运行 `publish\NovelManagement_v1.0.0\NovelManagement.WPF.exe`，输入启动密码即可（密码由交付通知提供）。

#### 📋 详细进度
- 查看 [docs/](docs/) 目录下的发布说明、中文使用手册与中英双语功能说明书。

**项目状态**: 🎉 **100% 完成，可投入生产使用！**

---

## English

### Overview

A fully featured book-writing and management system built on the C# WPF stack. It supports multi-agent AI collaborative writing, complete content management, and worldbuilding/setting management. The system is highly modular, extensible, and maintainable. Launch password: RWKV7_20260908

### Screenshots

| | |
|---|---|
| ![Dashboard](docs/images/01-dashboard.png) | ![Project Overview](docs/images/03-project-overview.png) |
| *Dashboard 仪表盘* | *Project Overview 项目概览* |
| ![Character Management](docs/images/07-character-management.png) | ![Plot Management](docs/images/06-plot-management.png) |
| *Character Management 角色管理* | *Plot Management 剧情管理* |
| ![AI Copilot](docs/images/13-ai-collaboration.png) | ![Publishing & Maintenance](docs/images/18-publish-management.png) |
| *AI Copilot AI 创作助手* | *Publishing & Maintenance 发布与运维管理* |

> More page screenshots (19 feature pages, bilingual guide): [项目功能说明书_双语_User_Guide.md](docs/项目功能说明书_双语_User_Guide.md).

### System Architecture

#### Tech Stack
- **Language**: C# (.NET 8.0)
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Database**: SQLite (local storage) + Entity Framework Core
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Logging**: Serilog
- **Configuration**: Microsoft.Extensions.Configuration
- **AI Integration**: Multiple AI providers (OpenAI, DeepSeek, RWKV, etc.)
- **Document Processing**: DocumentFormat.OpenXml, iTextSharp
- **Serialization**: Newtonsoft.Json

#### Layered Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                  Presentation Layer (WPF Views)              │
│   Main Shell │ Project Mgmt │ Content Mgmt │ Settings │ AI   │
├─────────────────────────────────────────────────────────────┤
│                  Application Layer (Services)                │
│   Project │ Content │ AI Collaboration │ Import/Export │ ... │
├─────────────────────────────────────────────────────────────┤
│                     Domain Layer (Business)                  │
│   Project │ Volume │ Chapter │ Character │ Faction │ Setting │
│        AI Agent Engine │ Workflow │ Memory Management        │
├─────────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                        │
│   Data Access (SQLite+EF) │ File System │ AI Providers       │
│          Logging │ Configuration │ Caching                   │
└─────────────────────────────────────────────────────────────┘
```

### Core Feature Modules

#### 1. Project Management
- **Project creation & configuration**: create new book projects with basic metadata
- **Project overview**: statistics, writing progress, recent activity
- **Multi-project management**: manage multiple book projects side by side
- **Project templates**: templates for different book genres

#### 2. Volume & Chapter Management
- **Volume structure**: multi-level volume organization
- **Chapter management**: create, edit, reorder, and track chapters
- **Content editor**: rich-text editing with formatting and image insertion
- **Version control**: chapter history and rollback

#### 3. Content Management
- **Characters**: profiles, relationship network, development arcs
- **Factions**: organization structure, faction relations, territories
- **Plot**: main/side storylines, foreshadowing, timeline management
- **Resources**: distribution and statistics of in-story resources
- **Relationship network**: visual relationship graph

#### 4. Worldbuilding / Settings Management
- **World setting**: geography, history, culture, politics, natural laws, society
- **Cultivation system**: complete level system, power sources, realms, abilities, taboos
- **Political system**: government types, power structures, legal systems
- **Currency system**: monetary systems, issuing bodies, economic indicators
- **Commerce system**: economic institutions, markets, trade, organizations
- **Races**: classification, physiology, culture, social structure, relations
- **Techniques**: categories, requirements, skills, lineage
- **Equipment**: classification, grades, attributes, enhancement, crafting
- **Pets**: classification, rarity, skills, evolution, training
- **Maps**: multi-level maps, terrain, climate, resources, special zones
- **Dimensions**: classification, stability, access levels, connections
- **Treasures**: classification, grades, spirituality, refining, artifact spirits
- **Timeline**: event management, associations, temporal properties
- **Population**: demographics, social classes, education, healthcare
- **Judicial system**: laws, courts, procedures, enforcement, penalties
- **Occupations**: classification, skills, career paths, compensation

#### 5. AI Agent Collaboration
- **Multi-agent workflow**: Director, Writer, Summarizer, Reader, and Setting Manager agents
- **Conversational writing**: multi-turn dialogue engine, context memory, intent recognition
- **Content generation**: world settings, characters, plots, chapter continuation
- **Consistency checking**: character, worldview, timeline, and plot coherence
- **AI collaboration workflow**: task scheduling, coordination, human-in-the-loop, quality control
- **Smart assistance**: template library, batch generation, history, chain-of-thought, parameters
- **Project data integration**: whole-project data access, smart analysis, cross-module collaboration
- **Ultra-long text handling**: layered memory, smart compression, progressive processing
- **Multiple AI providers**: OpenAI, Claude, Zhipu AI, SiliconFlow, Google AI, GROK3, local Ollama
- **AI testing**: basic API tests, module tests, stress tests, quality evaluation, diagnostics

#### 6. Import / Export
- **Formats**: Excel (.xlsx), Word (.docx), TXT, JSON, Markdown, PDF, EPUB
- **Smart parsing**: document structure, content types, chapter splits, character extraction
- **Full-project export**: complete data export with directory structure and reports
- **Partial export**: selective module export preserving relations
- **Batch operations**: bulk import/export, format conversion, incremental import, conflict resolution
- **Publishing formats**: Qidian, Jinjiang, Zongheng, and generic formats
- **Smart import**: book text parsing, info extraction, structuring, interactive refinement
- **Version control**: import/export history, rollback, backup, integrity checks

#### 7. Statistics & Reporting
- **Writing stats**: word counts, chapter/volume stats, progress, efficiency
- **Content analysis**: character appearances, faction influence, plot development
- **Setting stats**: worldview completeness, equipment distribution
- **Quality evaluation**: scoring, consistency checks, logic analysis, suggestions
- **Visualization**: charts, relationship graphs, map distribution, trends
- **Report generation**: overview, detailed stats, quality analysis, export reports
- **Real-time monitoring**: progress tracking, data change tracking, anomaly detection

### Project Structure

```
06_NovelCraft-C-SHARP/
├── src/
│   ├── NovelManagement.Core/              # Core domain models
│   │   ├── Entities/                      # Entities
│   │   ├── ValueObjects/                  # Value objects
│   │   ├── Enums/                         # Enums
│   │   ├── Interfaces/                    # Core interfaces
│   │   └── Exceptions/                    # Custom exceptions
│   │
│   ├── NovelManagement.Infrastructure/    # Infrastructure layer
│   │   ├── Data/                          # Data access (SQLite + EF Core)
│   │   ├── FileSystem/                    # File system services
│   │   ├── Logging/                       # Logging
│   │   ├── Configuration/                 # Configuration
│   │   └── Cache/                         # Caching
│   │
│   ├── NovelManagement.Application/       # Application service layer
│   │   ├── Services/                      # App services (batch/prerequisite generation)
│   │   ├── DTOs/                          # DTOs
│   │   ├── Interfaces/                    # Interfaces
│   │   └── Validators/                    # Validation
│   │
│   ├── NovelManagement.Domain/            # Domain layer
│   │   ├── Services/                      # Domain services
│   │   └── Repositories/                  # Repository interfaces
│   │
│   ├── NovelManagement.AI/                # AI integration layer
│   │   ├── Agents/                        # Agents (Director/Writer/Summarizer/Reader/Settings)
│   │   ├── Services/RWKV/                 # RWKV inference (llama.cpp / CUDA / libtorch)
│   │   ├── Workflows/                     # Workflow engine
│   │   ├── Memory/                        # Memory management
│   │   └── Providers/                     # AI providers
│   │
│   ├── NovelManagement.WPF/               # WPF presentation layer
│   │   ├── Views/                         # Views (incl. Copilot drawer)
│   │   ├── Services/                      # UI services (Copilot session/intent/pipeline)
│   │   ├── Controls/                      # Custom controls
│   │   ├── Converters/                    # Value converters
│   │   └── Styles/                        # Styles (multi-theme/skins)
│   │
│   └── NovelManagement.Tests/             # Unit tests (120/120 passing)
│
├── rwkv_models/                           # RWKV model files (.gguf, not committed)
├── llama_cpp/                             # llama-server local inference runtime
├── RWKV_lightning_CUDA_win/               # RWKV CUDA runtime (backup)
├── rwkv_lightning_libtorch_win/           # RWKV libtorch runtime (backup)
├── scripts/                               # Build & helper scripts
├── docs/                                  # Docs (user guide / manuals)
├── 项目交接.md                            # Development handover log (internal)
└── NovelManagementSystem.sln              # Solution file
```

### Requirements

- **OS**: Windows 10 or later
- **IDE**: Visual Studio 2022 or Visual Studio Code
- **.NET**: .NET 8.0 SDK or later
- **Database**: SQLite (no extra installation)
- **AI inference (optional)**: local llama-server (bundled in `llama_cpp\`) or any OpenAI-compatible completions endpoint

### Build & Run

```bash
# 1. Clone
git clone [repository-url]
cd 06_NovelCraft-C-SHARP

# 2. Restore
dotnet restore

# 3. Build
dotnet build

# 4. Run
dotnet run --project src/NovelManagement.WPF

# 5. Test
dotnet test
```

> **Packaged build users**: no compilation needed — run `publish\NovelManagement_v1.0.0\NovelManagement.WPF.exe` and enter the launch password (provided via delivery notice).

### Key Highlights

#### Highly Modular Design
- Layered architecture with clear responsibilities
- Dependency injection for low coupling
- Plugin-style extensibility

#### Intelligent AI Collaboration
- Multi-agent collaborative workflow
- Smart content generation and refinement
- Context-aware writing assistance
- Quality evaluation and improvement suggestions

#### Complete Content Management
- Structured content organization
- Rich relationship management
- Visualized data presentation
- Flexible query and filtering

#### Powerful Import / Export
- Multiple file formats
- Smart content parsing
- Batch operations
- Publishing format adapters

#### Data Security
- Local data storage
- Automatic backups
- Version control
- Integrity checks

### Extensibility

#### Plugin System
The system supports plugin-style extension:
- AI provider plugins
- Import/export format plugins
- Custom report plugins
- Third-party integration plugins

#### Configuration
Flexible configuration for:
- User preferences
- AI model settings
- UI theme configuration
- Feature toggles

#### Internationalization
Built-in i18n framework:
- One-click Chinese/English UI switching
- Localized resources
- Regional settings
- Culture adaptation

### Security Considerations

#### Data Security
- Local data storage to protect privacy
- Optional encryption at rest
- Access control
- Operation audit logging

#### AI Security
- Safe API key management
- Request rate limiting
- Content filtering
- Error handling and recovery

### Performance

#### Memory Management
- Large-text segmentation
- Smart caching strategy
- Memory usage monitoring
- GC-friendly processing

#### Responsiveness
- Async operations
- Background task scheduling
- Progress feedback
- UX optimization

### AI Agent Workflow in Detail

#### Core Agent Roles

##### 1. Director Agent
- **Role**: overall story architect — outline, settings, main-line planning
- **Recommended model**: rwkv7-g1j-13.3b-20260831-ctx16384-FP16 (strong reasoning for logical construction)
- **Core functions**: outline generation, setting construction, main-line planning, chapter arrangement, plot adjustment

##### 2. Writer Agent
- **Role**: chapter content creator
- **Recommended model**: rwkv7-g1j-13.3b-20260831-ctx16384-FP16 (strong generation, rich creativity)
- **Core functions**: chapter writing, dialogue, scene description, psychology, style consistency

##### 3. Summarizer Agent
- **Role**: content consolidation and transitions
- **Recommended model**: rwkv7-g1j-13.3b-20260831-ctx16384-FP16 (strong long-text handling)
- **Core functions**: chapter/volume summaries, prefaces, key info extraction, coherence

##### 4. Reader Agent
- **Role**: simulates reader perspective, gives feedback
- **Recommended model**: rwkv7-g1j-13.3b-20260831-ctx16384-FP16 (strong comprehension, objective evaluation)
- **Core functions**: content evaluation, issue spotting, suggestions, engagement analysis

##### 5. Setting Manager Agent
- **Role**: maintains setting consistency, manages worldview data
- **Recommended model**: rwkv7-g1j-13.3b-20260831-ctx16384-FP16 (rigorous logic, detail-oriented)
- **Core functions**: consistency maintenance, conflict checks, setting updates, queries, relation analysis

#### Multi-Agent Workflow

##### Phase 1: Project initialization & outline
```
User prompt → Director analysis → draft outline → Setting Manager builds base settings → Reader evaluation → Director refines outline
```

##### Phase 2: Chapter writing loop
```
Director chapter brief → Writer generates content → Setting Manager consistency check → Reader feedback → Summarizer chapter summary → next chapter
```

##### Phase 3: Volume completion & summary
```
Summarizer volume summary → Director next-volume planning → Summarizer preface → Reader overall evaluation → Director adjusts plan
```

#### Layered Memory for Ultra-Long Texts

##### Memory architecture
- **Global memory**: core worldview, main characters, overall outline
- **Volume memory**: current volume storyline, character arcs, key events
- **Chapter memory**: current chapter content, character states, scene dialogue
- **Passage memory**: text being processed, local context, temporary state

##### Smart memory compression
- **Importance scoring**: 1–10 rating per piece of information
- **Dynamic management**: low-importance info compressed, core content preserved
- **Retrieval optimization**: task-driven smart memory retrieval

#### Import / Export Details

##### Supported formats
- **Excel (.xlsx)**: structured data with multi-sheet support
- **Word (.docx)**: rich text with format preservation
- **Plain text (.txt)**: simplest, best compatibility
- **JSON (.json)**: full project data with structure preserved
- **Markdown (.md)**: document-style export for reading and sharing

##### Export folder structure
```
{ProjectName}_{ExportTime}_{ExportType}/
├── ProjectOverview/
├── VolumeManagement/
├── ContentManagement/
├── SettingsManagement/
├── StatisticsReports/
└── Documentation/
```

### 📊 Project Status (updated 2026-09-08 · V1.0.0)

#### 🎯 Overall progress: 100% ✅

> 📦 **V1.0.0 packaged build**: single-file EXE (self-contained .NET 8 runtime, no dependencies) at `publish\NovelManagement_v1.0.0\NovelManagement.WPF.exe` (https://github.com/LuckySongXiao/NovelCraft-C-SHARP/releases). A **launch password** is required at startup (delivered separately; verified via SHA-256 hash, never stored in plain text).
> 📖 **Release notes**: [docs/发布说明_v1.0.0.md](docs/发布说明_v1.0.0.md) (Chinese)
> 📖 **User manual**: [docs/使用手册_v1.0.0.md](docs/使用手册_v1.0.0.md) (Chinese)
> 📖 **Bilingual feature guide**: [docs/项目功能说明书_双语_User_Guide.md](docs/项目功能说明书_双语_User_Guide.md) (19 pages, Chinese-English, with screenshots)

| Module | Status | Progress | Notes |
|--------|--------|----------|-------|
| Architecture layers | ✅ | 100% (6/6) | All layers complete |
| WPF UI | ✅ | 100% (21/21) | All views complete |
| Application services | ✅ | 100% (20/20) | All services complete |
| AI Agent system | ✅ | 100% (7/7) | Full AI system |
| AI Copilot | ✅ | 100% | Conversational 5-stage pipeline (outline → plotline → volumes → drafts → chapters) |
| Import / Export | ✅ | 100% (7/7) | Multi-format support |
| Tests | ✅ | 120/120 | All unit tests passing |

#### 🔧 Latest updates (2026-09-07/08)
- ✅ **V1.0.0 packaged release**: self-contained single-file EXE (~85MB compressed), ready to run
- ✅ **Launch password protection**: password gate at startup (SHA-256 verification, no plaintext); self-service password change via Publishing & Maintenance → Change Launch Password
- ✅ **Character uniqueness**: idempotent de-duplication at service level — identical name+personality+background characters are reused, eliminating AI-generated duplicates; existing duplicates cleaned up
- ✅ **AI Copilot**: conversational 5-stage writing pipeline with confirmation cards (accept/edit/regenerate) and restartable state
- ✅ **Chapter association**: link existing book chapters to the copilot for direct rewrite/continuation/Q&A, isolated from the pipeline state
- ✅ **Continue/polish fixes**: dynamic model dropdown loading; two-stage RWKV online detection (3s status + 6s inference) eliminating false-online misjudgment; selection-scoped polishing
- ✅ **120 unit tests passing**: Copilot intent/pipeline/session suites; caught a real chapter-draft numbering bug
- ✅ **Sidebar state persistence**: expand states restored precisely across restarts (`sidebar_state.json`)
- ✅ **Custom cultivation system**: AI-designed cultivation hierarchy applied across character editing / prerequisite / batch generation

#### 📈 Statistics
- **Code files**: 300+
- **Lines of code**: 40,000+
- **Build**: ✅ success (0 errors, 0 warnings)
- **Unit tests**: ✅ 120/120 passing
- **UIA regression**: ✅ five major feature pages verified end-to-end
- **Startup**: ✅ normal
- **Database**: ✅ connected and operational

#### 🚀 Quick start
```bash
# 1. Clone
git clone [repository-url]
cd 06_NovelCraft-C-SHARP

# 2. Restore
dotnet restore

# 3. Build
dotnet build

# 4. Run
src\NovelManagement.WPF\bin\Debug\net8.0-windows\NovelManagement.WPF.exe
```

> **Packaged build users**: no compilation needed — run `publish\NovelManagement_v1.0.0\NovelManagement.WPF.exe` and enter the launch password.

#### 📋 More
- See [docs/](docs/) for release notes, the Chinese user manual, and the bilingual feature guide.

**Project status**: 🎉 **100% complete, production ready!**

*This document is continuously updated as the project evolves.*
