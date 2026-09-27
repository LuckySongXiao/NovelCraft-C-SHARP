# 贡献指南 / Contributing Guide

感谢参与 NovelCraft C-SHARP 的开发！本文说明协作规范，让补丁更容易被合并。

## 分支与提交

- **分支命名**：`<type>/<简短主题]`，如 `fix/rwkv-template-leak`、`feature/force-layout`、`glm/contribution-v1`
- **提交信息**：`类型: 摘要`，类型取 `修复 / 功能 / 性能 / 安全 / 文档 / 重构 / 测试 / CI` 之一，正文说明动机与验证方式
- **提交粒度**：一个提交只做一件事；避免"诸多细节更新"式的混合提交（历史不可追溯）

## 构建与测试

```bash
# Windows（全量）
dotnet restore
dotnet build NovelManagementSystem.sln -c Release
dotnet test NovelManagementSystem.sln          # 单元测试（需 Windows Desktop 运行时）

# Linux / macOS（跨平台编译守门，不能跑 WPF 测试）
dotnet restore -p:EnableWindowsTargeting=true
dotnet build NovelManagementSystem.sln -c Debug -p:EnableWindowsTargeting=true
```

- 新增逻辑请配套单元测试（`src/NovelManagement.Tests/`，xunit）
- 纯算法/解析类逻辑建议放到 Application/AI 层并保持无 WPF 依赖，便于在 CI 的 Ubuntu 交叉编译任务中验证
- CI（`.github/workflows/ci.yml`）会在 windows-latest 构建并跑全量测试，ubuntu-latest 做跨平台编译守门

## AI 相关改动的特别约定（源自项目交接文档的既定原则）

1. **PromptSummary 约束链**：任何新增 AI 入口必须接入 `ProjectReadModelService` / `ProjectContextAssembler`，不允许"仅依赖弹窗局部文本"的裸 prompt
2. **失败绝不降级**：RWKV 调用失败要明确报错，不返回假数据
3. **公共层优先**：AI 输出解析问题先查 `AiAutoFillFormatter` / `RwkvThinkingStripper` 等公共层，再查页面 prompt
4. **RWKV 双后端**：改动 `RwkvLightningService` 时注意 libtorch / 纯 CUDA HTTP / llama.cpp 三种 flavor 的兼容；state 会话禁止并发写同一 session_id
5. **日志脱敏**：不把远程端点 URL、密码、CF 凭据写入日志或入库文件

## 本地化

- 界面文案：`src/NovelManagement.WPF/Localization/StringsCore.cs`（等`Strings*.cs`）+ `language-table.csv` 两处都要加，中英成对
- CSV 中含半角逗号的文本用英文双引号包裹（参考 `MW.BatchConfirm` 行）
- `LocalizationTests` 会强制 zh/en 键集一致

## 安全

- 启动密码、API 密钥、CF Access 凭据等**永不提交**；示例配置留空字段即可
- 远程推理端点凭据走 `appsettings.user.json`（不入库）或环境变量
