# AI 入口 PromptSummary 约束链审计报告

**审计日期**：2026-09-27
**审计人**：GLM
**审计目标**（源自项目交接文档 P1 任务）：
> 排查所有 AI 自动补全/AI 生成入口，统一接到 `ProjectReadModelService` 或 `ProjectContextAssembler`，不允许仍有"仅依赖弹窗局部文本"的裸 prompt 入口。

## 审计方法

以三重证据交叉定位：
1. AI 调用点：grep 所有引用 `AIAssistantService` / `WriterAgent` / `RwkvLightningService` / `DirectorAgent` 的视图
2. 合规通道：grep `PromptSummary` / `BuildAiContextDataAsync` / `BuildSubsystemPromptContextAsync` / `ProjectContextAssembler` 的文件
3. 人工复核：对未命中合规通道的视图，逐一检查其参数构建代码

## 审计结论总览

**26 个含 AI 调用的视图 + 若干服务中，仅 1 个裸 prompt 入口（AIPolishDialog），本次已修复。其余全部合规。**

### ✅ 项目级上下文合规（PromptSummary / BuildAiContextDataAsync / Assembler）— 12 个视图

| 视图 | 通道 |
|---|---|
| AIOutlineGeneratorDialog | PromptSummary + Assembler |
| PrerequisiteGenerationDialog | PromptSummary + Assembler |
| PoliticalSystemView | PromptSummary + Assembler |
| CultivationSystemView | PromptSummary + Assembler |
| FactionManagementView | PromptSummary + Assembler |
| WorldSettingManagementView | PromptSummary + Assembler |
| PlotManagementView | PromptSummary + Assembler |
| AIChapterWriteDialog | PromptSummary + Assembler |
| AIContinueWriteDialog | PromptSummary + Assembler |
| AICollaborationView | PromptSummary |
| ConsistencyCheckDialog | PromptSummary |
| AIAssistantWorkspaceView | Assembler |

### ✅ 子系统级上下文合规（BuildSubsystemPromptContextAsync）— 13 个视图

CharacterManagementView、PopulationSystemView、ProfessionSystemView、JudicialSystemView、EquipmentSystemView、RelationshipNetworkView、MapStructureView、TechniqueSystemView、DimensionStructureView、CharacterEditDialog、BusinessSystemView、PetSystemView、TreasureSystemView

> 说明：这类视图走 `AIAssistantService → DirectorAgent`，但在参数字典中注入了 `BuildSubsystemPromptContextAsync(projectId, 子系统名, 领域限定约束)` 构建的上下文（含 PromptSummary 与"只能产出 X"的领域约束），符合交接文档的子项主题一致性要求。

### ✅ 间接合规（经 PrerequisiteGenerationService）— 1 个视图

| 视图 | 说明 |
|---|---|
| ChapterEditorDialog | AI 编辑前先调 `PrerequisiteGenerationService.GeneratePrerequisitesAsync` 确保前置数据（该服务本身走 PromptSummary 约束链） |

### 🔧 本次修复：AIPolishDialog（原唯一裸 prompt 入口）

**问题**：润色对话框的参数字典完全来自对话框本地控件（风格/强度/要求等），不含任何项目上下文——正是交接文档警示的"仅依赖弹窗局部文本"形态。润色长文时模型不感知书名/类型/世界观，可能产出与项目设定冲突的风格。

**修复**（本次提交）：
- 新增 `EnsurePolishProjectContextAsync()`：经 `ProjectReadModelService.BuildSubsystemPromptContextAsync(projectId, "正文润色", "润色必须保持原文情节与人物设定不变…")` 构建上下文并缓存
- `PolishContent` / `BatchPolishContentAsync` 两个入口在调用 AI 前初始化上下文（失败不阻断，按无上下文润色）
- 参数字典（内联 + `BuildPolishParameters` 共三处）统一注入 `ProjectContext` 键——`BaseAgent.BuildUserPrompt` 与 `AIAgentRoleWorkflowService.SerializeParameters` 均泛化序列化参数字典，无需改动 AI 层

### ➖ 范围外（自成体系的新书流水线）

`FullNovelBatchGenerationService` / `OneClickNovelGenerationService`：从零创建新书，无既有项目上下文可注入，以自身大纲链驱动，不适用 PromptSummary 规则。

### SmartSuggestions / QualityAnalysis（AIPolishDialog 内）

智能建议与质量分析针对**已润色文本**的后处理，输入即输出对象，暂不注入项目上下文；如后续发现建议与世界观冲突可按同一模式补齐。

## 维护检查项（新增 AI 入口时自查）

1. 参数字典中是否有 `ProjectContext`（或等效的 Assembler 产物）？
2. 是否经 `BuildSubsystemPromptContextAsync` 附加领域限定约束（"只能产出 X"）？
3. 失败路径是否"明确报错不降级"（不伪造上下文）？
4. 参见 `CONTRIBUTING.md`「AI 相关改动的特别约定」。
