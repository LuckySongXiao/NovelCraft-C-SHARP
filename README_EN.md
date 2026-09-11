## English

### Overview

A fully featured book-writing and management system built on the C# WPF stack. It supports multi-agent AI collaborative writing, complete content management, and worldbuilding/setting management. The system is highly modular, extensible, and maintainable. Launch password: RWKV7_20260908

### Screenshots (English UI)

| | |
|---|---|
| ![Dashboard](docs/images_en/01-dashboard.png) | ![Project Overview](docs/images_en/03-project-overview.png) |
| *Dashboard* | *Project Overview* |
| ![Character Management](docs/images_en/07-character-management.png) | ![Plot Management](docs/images_en/06-plot-management.png) |
| *Character Management* | *Plot Management* |
| ![AI Copilot](docs/images_en/13-ai-collaboration.png) | ![Publishing & Maintenance](docs/images_en/18-publish-management.png) |
| *AI Copilot* | *Publishing & Maintenance* |

> Full English feature guide with 19 page screenshots: [docs/User_Guide_EN.md](docs/User_Guide_EN.md).
> Bilingual guide (Chinese-English): [项目功能说明书_双语_User_Guide.md](docs/项目功能说明书_双语_User_Guide.md).

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
> 📖 **English feature guide**: [docs/User_Guide_EN.md](docs/User_Guide_EN.md) (19 pages, English UI screenshots, incl. language switching & remote RWKV guide)
> 📖 **Bilingual feature guide**: [docs/项目功能说明书_双语_User_Guide.md](docs/项目功能说明书_双语_User_Guide.md) (19 pages, Chinese-English, with screenshots)

#### 🌐 Bilingual UI (new in v1.1.0)
- Full Chinese/English interface; follows the Windows display language by default
- **Manual language switch**: one click on the title-bar toggle (`EN` / `中`) — applies immediately and persists to `appsettings.user.json` (`Localization:Language`)
- Verified end-to-end via UIA: title bar, navigation, dialogs and persistence across restart (zh-CN ↔ en-US)

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
git clone https://github.com/LuckySongXiao/NovelCraft-C-SHARP.git
cd NovelCraft-C-SHARP

# 2. Restore
dotnet restore

# 3. Build
dotnet build

# 4. Run
src\NovelManagement.WPF\bin\Debug\net8.0-windows\NovelManagement.WPF.exe
```

> **Packaged build users**: no compilation needed — run `publish\NovelManagement_v1.0.0\NovelManagement.WPF.exe` and enter the launch password.

#### 📋 More
- See [docs/](docs/) for release notes, the Chinese user manual, the English feature guide ([User_Guide_EN.md](docs/User_Guide_EN.md)), and the bilingual feature guide.

**Project status**: 🎉 **99.9% complete, production ready!**

*This document is continuously updated as the project evolves.*
