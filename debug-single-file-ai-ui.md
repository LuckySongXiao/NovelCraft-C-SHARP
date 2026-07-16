# [OPEN] single-file-ai-ui

## 问题概述
- AI 模型配置页面中，Ollama 模型配置区域的勾选框文本标签发生重叠。
- 单文件版应用中复现了测试阶段出现的各类 Bug，说明封装未改变业务缺陷表现，且可能存在发布环境与开发环境差异导致的问题放大。

## 当前症状
- 进入 `AIConfigurationView` 后，Ollama 配置区域部分勾选框文本显示重叠。
- 单文件版应用在运行时出现与测试阶段一致的 AI 功能异常与相关页面异常。

## 初始假设
- 假设 A：Ollama 配置区域的 `Grid` 行列定义或 `CheckBox` 样式导致内容在同一布局槽位叠加。
- 假设 B：单文件发布后资源、配置或运行目录变化，导致 AI 页面加载和服务初始化路径与开发态不一致。
- 假设 C：问题并非由单文件发布引入，而是原有业务 Bug 在发布版中原样存在，需逐项基于运行时证据确认。
- 假设 D：AI 配置页面的通用样式或全局 `CheckBox` 模板在单文件版运行时表现异常，影响 Ollama 区域文本布局。
- 假设 E：AI 服务初始化、配置读取或页面绑定存在未处理异常，导致多个 AI 功能在单文件版中集中暴露。

## 证据采集计划
- 检查 `AIConfigurationView.xaml` 中 Ollama 区域布局结构与样式引用。
- 对 AI 配置页加载、Ollama 区域初始化、关键 AI 服务入口添加最小化运行时插桩。
- 在单文件版中复现页面重叠与主要 AI Bug，收集前置日志和异常上下文。

## 当前状态
- 已创建调试会话记录文件，待开始插桩与复现。

## 已采集证据
- `pre-fix` 日志文件：`.dbg/trae-debug-log-single-file-ai-ui.ndjson`
- 关键日志：
  - 宿主窗口加载成功：`AIConfigurationHostWindow.OnLoaded`
  - AI 配置页实例化成功：`AIConfigurationHostWindow.TryRenderConfigurationView`
  - AI 配置页加载完成：`AIConfigurationView_Loaded`
  - 提供者状态检查完成：`CheckProvidersStatusAsync`

## 假设验证
| ID | 假设 | 状态 | 证据摘要 |
|----|------|------|----------|
| A | Ollama 区域布局槽位错误导致重叠 | 已确认 | 日志显示 `ollamaVerboseRow = 4`，而 XAML 当前只定义了 4 行（索引 0-3），说明最后一个勾选框被放到了未定义行。 |
| B | 单文件发布后资源/配置路径异常导致 AI 配置页失效 | 已否定 | 单文件版 `--ai-config-only` 能正常启动，AI 配置页实例化成功并完成加载。 |
| C | 单文件版只是复现原有业务 Bug | 初步确认 | AI 配置页在单文件版中可正常工作，说明发布方式本身不是总根因，测试期业务 Bug 很可能被原样带入发布版。 |
| D | 全局 CheckBox 样式导致 Ollama 区域文本重叠 | 待补充 | 当前更强证据指向局部 Grid 行定义错误，暂未发现全局样式异常证据。 |
| E | AI 服务初始化/页面绑定异常导致多个 AI 功能失败 | 待继续验证 | 目前仅确认 AI 配置页与提供者状态检查完成；角色自动补全、大纲生成、设定页异常仍需继续复现取证。 |

## 下一步
- 先最小修复 Ollama 配置区行定义错误，消除勾选框文本重叠。
- 保留插桩，继续在单文件版中复现：
  - 角色自动补全
  - 大纲生成 404
  - 设定管理页异常

## 追加证据
- 大纲生成日志显示：
  - `GenerateOutlineAsync` 已进入
  - 双代理流程已进入
  - 运行时记录为 `mainProvider = Ollama`、`subProvider = Ollama`
  - SubAgent 第一步需求总结直接返回 `404 (Not Found)`
- 角色自动补全日志显示：
  - 入口已进入
  - 调用结束耗时极短（约 14ms）
  - `success = false` 且 `hasData = false`

## 新结论
- 角色自动补全存在确定性代码缺口：
  - `AIAssistantService.GenerateCharacterAsync()` 调用了 `DirectorAgent.ExecuteAsync("GenerateCharacter", ...)`
  - 但 `DirectorAgent` 原先未注册 `GenerateCharacter` 任务分支
  - 已开始补上真实角色生成任务，并修正失败提示文案
- 大纲生成 404 已确认发生在双代理流程第一步：
  - 不是按钮无响应
  - 不是窗口层报错
  - 是 SubAgent 实际发起模型请求后返回 404
- 双代理读取到的运行时提供者值与当前用户配置文件存在不一致风险：
  - 当前磁盘配置文件中 `SubAgentProvider = RWKV`
  - 但某次运行时日志中记录 `subProvider = Ollama`
  - 已增加更细粒度插桩，准备继续确认是配置未生效还是运行时被覆盖
