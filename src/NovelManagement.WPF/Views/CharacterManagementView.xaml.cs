using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.DTOs;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.WPF.Commands;
using NovelManagement.WPF.Events;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{

    /// <summary>
    /// CharacterManagementView.xaml 的交互逻辑
    /// </summary>
    public partial class CharacterManagementView : UserControl
    {
        private List<Character> _allCharacters = new();
        private List<Character> _filteredCharacters = new();
        private Character? _selectedCharacter;

        // 服务
        private CharacterService? _characterService;
        private AIAssistantService? _aiAssistantService;
        private ProjectContextService? _projectContextService;
        private ILogger<CharacterManagementView>? _logger;

        /// <summary>
        /// 选择角色命令
        /// </summary>
        public ICommand SelectCharacterCommand { get; private set; }

        /// <summary>
        /// 角色更新事件
        /// </summary>
        public event EventHandler<CharacterUpdatedEventArgs>? CharacterUpdated;

        /// <summary>
        /// 构造函数
        /// </summary>
        public CharacterManagementView()
        {
            InitializeComponent();
            InitializeServices();
            InitializeCommands();
            LoadCharactersAsync();

            // 设置DataContext为当前实例，以便XAML中的命令绑定能够工作
            DataContext = this;
        }

        /// <summary>
        /// 初始化服务
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                // 从依赖注入容器获取服务
                var serviceProvider = App.ServiceProvider;
                if (serviceProvider != null)
                {
                    _characterService = serviceProvider.GetService<CharacterService>();
                    _aiAssistantService = serviceProvider.GetService<AIAssistantService>();
                    _projectContextService = serviceProvider.GetService<ProjectContextService>();
                    _logger = serviceProvider.GetService<ILogger<CharacterManagementView>>();
                }

                // 如果服务获取失败，创建备用日志记录器
                if (_logger == null)
                {
                    var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                    _logger = loggerFactory.CreateLogger<CharacterManagementView>();
                }

                _logger.LogInformation("角色管理界面服务初始化完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"服务初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region 初始化

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectCharacterCommand = new RelayCommand<Character>(SelectCharacter);
        }

        /// <summary>
        /// 异步加载角色数据
        /// </summary>
        private async void LoadCharactersAsync()
        {
            try
            {
                // 获取当前项目ID
                var currentProjectId = GetCurrentProjectId();
                if (currentProjectId == Guid.Empty)
                {
                    _logger?.LogWarning("没有当前项目，使用模拟数据");
                    _allCharacters = CreateMockCharacters();
                    _filteredCharacters = new List<Character>(_allCharacters);
                    UpdateCharacterList();
                    return;
                }

                if (_characterService != null)
                {
                    var characters = await _characterService.GetCharactersByProjectIdAsync(currentProjectId);
                    _allCharacters = characters.ToList();

                    // 如果数据库中没有角色数据，使用模拟数据
                    if (_allCharacters.Count == 0)
                    {
                        _logger?.LogInformation("数据库中没有角色数据，使用模拟数据进行演示");
                        _allCharacters = CreateMockCharacters();

                        // 可选：将模拟数据保存到数据库
                        await SaveMockCharactersToDatabase();
                    }
                }
                else
                {
                    // 如果服务不可用，使用模拟数据
                    _logger?.LogWarning("角色服务不可用，使用模拟数据");
                    _allCharacters = CreateMockCharacters();
                }

                _filteredCharacters = new List<Character>(_allCharacters);
                UpdateCharacterList();

                _logger?.LogInformation($"成功加载 {_allCharacters.Count} 个角色");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载角色数据失败");
                MessageBox.Show($"加载角色数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);

                // 使用模拟数据作为备用
                _allCharacters = CreateMockCharacters();
                _filteredCharacters = new List<Character>(_allCharacters);
                UpdateCharacterList();
            }
        }

        /// <summary>
        /// 获取当前项目ID
        /// </summary>
        /// <returns>当前项目ID，如果没有则返回空GUID</returns>
        private Guid GetCurrentProjectId()
        {
            if (_projectContextService?.HasCurrentProject == true)
            {
                return _projectContextService.CurrentProjectId!.Value;
            }

            // 如果没有项目上下文服务或没有当前项目，尝试创建一个默认项目
            _logger?.LogWarning("没有当前项目上下文，将创建默认项目");
            return CreateDefaultProjectIfNeeded();
        }

        /// <summary>
        /// 创建默认项目（如果需要）
        /// </summary>
        /// <returns>默认项目ID</returns>
        private Guid CreateDefaultProjectIfNeeded()
        {
            try
            {
                // 尝试通过ProjectService创建一个默认项目
                var serviceProvider = App.ServiceProvider;
                var projectService = serviceProvider?.GetService<ProjectService>();

                if (projectService != null)
                {
                    // 首先尝试查找现有的默认项目
                    try
                    {
                        var existingProjects = projectService.GetAllProjectsAsync().Result;
                        var defaultProject = existingProjects.FirstOrDefault(p => p.Name == "默认项目");

                        if (defaultProject != null)
                        {
                            _logger?.LogInformation("找到现有默认项目: {ProjectId}", defaultProject.Id);

                            if (_projectContextService != null)
                            {
                                _projectContextService.SetCurrentProject(defaultProject.Id, defaultProject.Name);
                            }

                            return defaultProject.Id;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "查找现有默认项目失败，将尝试创建新的");
                    }

                    // 如果没有找到现有的默认项目，创建一个新的
                    var newDefaultProject = new Project
                    {
                        Id = Guid.NewGuid(),
                        Name = "默认项目",
                        Description = "系统自动创建的默认项目",
                        Type = "小说",
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    // 同步创建项目，避免并发访问DbContext
                    try
                    {
                        _logger?.LogInformation("开始创建项目: {ProjectName}", newDefaultProject.Name);
                        var createTask = projectService.CreateProjectAsync(newDefaultProject);
                        createTask.Wait(); // 等待完成，避免并发
                        _logger?.LogInformation("成功创建默认项目: {ProjectId}", newDefaultProject.Id);

                        if (_projectContextService != null)
                        {
                            _projectContextService.SetCurrentProject(newDefaultProject.Id, newDefaultProject.Name);
                        }

                        return newDefaultProject.Id;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "创建默认项目失败");

                        // 如果创建失败，可能是因为已经存在，尝试再次查找
                        try
                        {
                            var retryProjects = projectService.GetAllProjectsAsync().Result;
                            var retryDefaultProject = retryProjects.FirstOrDefault(p => p.Name == "默认项目");

                            if (retryDefaultProject != null)
                            {
                                _logger?.LogInformation("创建失败后找到现有默认项目: {ProjectId}", retryDefaultProject.Id);

                                if (_projectContextService != null)
                                {
                                    _projectContextService.SetCurrentProject(retryDefaultProject.Id, retryDefaultProject.Name);
                                }

                                return retryDefaultProject.Id;
                            }
                        }
                        catch (Exception retryEx)
                        {
                            _logger?.LogError(retryEx, "重试查找默认项目也失败");
                        }

                        // 最后的备用方案：返回新创建的项目ID，即使保存失败
                        return newDefaultProject.Id;
                    }
                }
                else
                {
                    // 如果ProjectService不可用，使用固定GUID
                    var defaultProjectId = new Guid("00000000-0000-0000-0000-000000000001");

                    if (_projectContextService != null)
                    {
                        _projectContextService.SetCurrentProject(defaultProjectId, "默认项目");
                    }

                    return defaultProjectId;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建默认项目时发生错误");

                // 发生错误时使用固定GUID
                var fallbackProjectId = new Guid("00000000-0000-0000-0000-000000000001");

                if (_projectContextService != null)
                {
                    _projectContextService.SetCurrentProject(fallbackProjectId, "默认项目");
                }

                return fallbackProjectId;
            }
        }

        /// <summary>
        /// 创建模拟角色数据
        /// </summary>
        private List<Character> CreateMockCharacters()
        {
            var projectId = GetCurrentProjectId();
            return new List<Character>
            {
                // 主角
                new Character
                {
                    Id = Guid.NewGuid(),
                    Name = "林轩",
                    Type = "主角",
                    Faction = new Faction { Name = "玄天宗" },
                    Background = "出身平凡的少年，因意外获得上古传承《千面劫经》，踏上修仙之路。拥有罕见的千面劫体质，能够吸收他人的修为和记忆。",
                    Appearance = "身高一米八，面容清秀，眼神坚毅。黑发如墨，常穿一袭青衫。修炼后气质愈发出尘。",
                    Personality = "坚韧不拔，重情重义，面对强敌从不退缩。内心善良但不迂腐，对朋友真诚，对敌人无情。",
                    CultivationLevel = "筑基期",
                    Abilities = "千面劫体、剑法精通、阵法天赋、炼丹基础",
                    Age = 18,
                    Gender = "男",
                    Importance = 10,
                    Status = "Active",
                    ProjectId = projectId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // 师父/引路人
                new Character
                {
                    Id = Guid.NewGuid(),
                    Name = "玄天老祖",
                    Type = "主配角",
                    Faction = new Faction { Name = "玄天宗" },
                    Background = "玄天宗开山祖师，已活了三千年。曾是修仙界的传奇人物，后因重伤隐居。发现林轩的天赋后决定收其为徒。",
                    Appearance = "仙风道骨的老者，白发白须，眼神深邃如星空。身穿道袍，手持拂尘，举手投足间透露着超然气质。",
                    Personality = "深不可测，慈祥睿智。看似和蔼可亲，实则心思深沉。对弟子要求严格，但关键时刻总会出手相助。",
                    CultivationLevel = "化神期",
                    Abilities = "玄天神功、万剑归宗、空间法则、时间感悟",
                    Age = 3000,
                    Gender = "男",
                    Importance = 9,
                    Status = "Active",
                    ProjectId = projectId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // 女主角
                new Character
                {
                    Id = Guid.NewGuid(),
                    Name = "苏梦瑶",
                    Type = "女主角",
                    Faction = new Faction { Name = "天音阁" },
                    Background = "天音阁圣女，天生音律天赋，能以音攻敌。出身高贵但不骄纵，与林轩相遇后逐渐产生情愫。",
                    Appearance = "绝世美女，肌肤如雪，眉目如画。一头青丝如瀑，常穿白色长裙。气质清雅脱俗，如仙女下凡。",
                    Personality = "温柔善良，聪慧过人。外表柔弱但内心坚强，关键时刻能独当一面。对感情专一，一旦认定便不会改变。",
                    CultivationLevel = "金丹期",
                    Abilities = "天音神功、治愈术、幻音迷神、琴剑双修",
                    Age = 19,
                    Gender = "女",
                    Importance = 9,
                    Status = "Active",
                    ProjectId = projectId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // 反派BOSS
                new Character
                {
                    Id = Guid.NewGuid(),
                    Name = "血魔尊者",
                    Type = "反派",
                    Faction = new Faction { Name = "血魔宗" },
                    Background = "血魔宗宗主，修炼邪功，以吞噬他人精血为乐。曾经也是正道弟子，因走火入魔堕入魔道。与玄天老祖有旧怨。",
                    Appearance = "身材高大，面容阴鸷。一头血红长发，双眼如血。身穿血色长袍，周身散发着浓郁的血腥气息。",
                    Personality = "残忍嗜血，心狠手辣。为达目的不择手段，视人命如草芥。但也有枭雄气质，能屈能伸。",
                    CultivationLevel = "元婴期",
                    Abilities = "血魔大法、血海滔天、魔音摄魂、血遁术",
                    Age = 800,
                    Gender = "男",
                    Importance = 8,
                    Status = "Active",
                    ProjectId = projectId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // 好友/兄弟
                new Character
                {
                    Id = Guid.NewGuid(),
                    Name = "王铁柱",
                    Type = "配角",
                    Faction = new Faction { Name = "玄天宗" },
                    Background = "林轩的师兄，憨厚老实的修仙者。虽然天赋一般，但勤奋刻苦。对林轩忠心耿耿，是可以托付后背的兄弟。",
                    Appearance = "身材魁梧，相貌憨厚。浓眉大眼，总是憨笑。肌肉发达，给人可靠的感觉。",
                    Personality = "憨厚老实，忠诚可靠。虽然不够聪明，但心地善良。对朋友两肋插刀，对敌人也毫不手软。",
                    CultivationLevel = "练气期",
                    Abilities = "铁布衫、巨力术、防御专精、锻造技能",
                    Age = 20,
                    Gender = "男",
                    Importance = 6,
                    Status = "Active",
                    ProjectId = projectId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // 神秘强者
                new Character
                {
                    Id = Guid.NewGuid(),
                    Name = "剑痴",
                    Type = "配角",
                    Faction = new Faction { Name = "散修" },
                    Background = "神秘的剑修，一生只为剑道而活。曾是某个大宗门的天才弟子，后因理念不合离开宗门，成为散修。",
                    Appearance = "中年男子，面容冷峻。背负一柄古剑，眼神锐利如剑。身穿简朴的灰色长袍，但气质超凡。",
                    Personality = "沉默寡言，专注剑道。对剑法有着偏执的追求，不喜欢与人交往。但内心正直，会在关键时刻出手相助。",
                    CultivationLevel = "元婴期",
                    Abilities = "万剑诀、剑意通神、剑气纵横、一剑破万法",
                    Age = 150,
                    Gender = "男",
                    Importance = 7,
                    Status = "Active",
                    ProjectId = projectId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // 智者/军师
                new Character
                {
                    Id = Guid.NewGuid(),
                    Name = "诸葛明",
                    Type = "配角",
                    Faction = new Faction { Name = "天机阁" },
                    Background = "天机阁长老，精通推演之术，能窥探天机。年纪轻轻就有着超凡的智慧，常常能在关键时刻提供重要建议。",
                    Appearance = "文质彬彬的青年，面容清秀，眼神睿智。常穿白色儒衫，手持折扇，颇有书生气质。",
                    Personality = "聪明睿智，深谋远虑。说话总是点到为止，喜欢用隐晦的方式表达。虽然年轻但老成持重。",
                    CultivationLevel = "金丹期",
                    Abilities = "天机推演、奇门遁甲、预知术、阵法大师",
                    Age = 25,
                    Gender = "男",
                    Importance = 6,
                    Status = "Active",
                    ProjectId = projectId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // 妖族角色
                new Character
                {
                    Id = Guid.NewGuid(),
                    Name = "白狐仙子",
                    Type = "配角",
                    Faction = new Faction { Name = "妖族" },
                    Background = "千年白狐修炼成精，天性善良。因为一次意外被林轩救下，从此对他心存感激。在妖族中地位尊崇。",
                    Appearance = "绝美的女子，有着狐族特有的魅惑气质。银白色长发，碧绿色眼眸。人形时穿白色长裙，妖形时是雪白的九尾狐。",
                    Personality = "聪慧狡黠，但心地善良。有着狐族的天性，喜欢恶作剧，但从不害人。对恩人极其忠诚。",
                    CultivationLevel = "金丹期",
                    Abilities = "魅惑术、幻术精通、妖族神通、变化之术",
                    Age = 1000,
                    Gender = "女",
                    Importance = 5,
                    Status = "Active",
                    ProjectId = projectId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };
        }

        /// <summary>
        /// 将模拟角色数据保存到数据库
        /// </summary>
        private async Task SaveMockCharactersToDatabase()
        {
            try
            {
                if (_characterService == null)
                {
                    _logger?.LogWarning("角色服务不可用，无法保存模拟数据到数据库");
                    return;
                }

                _logger?.LogInformation("开始将模拟角色数据保存到数据库");

                foreach (var character in _allCharacters)
                {
                    try
                    {
                        // 清除导航属性以避免外键约束问题
                        var characterToSave = new Character
                        {
                            Id = character.Id,
                            Name = character.Name,
                            Type = character.Type,
                            Background = character.Background,
                            Appearance = character.Appearance,
                            Personality = character.Personality,
                            Abilities = character.Abilities,
                            CultivationLevel = character.CultivationLevel,
                            Age = character.Age,
                            Gender = character.Gender,
                            Importance = character.Importance,
                            Status = character.Status,
                            ProjectId = character.ProjectId,
                            CreatedAt = character.CreatedAt,
                            UpdatedAt = character.UpdatedAt,
                            // 暂时不设置外键关系
                            FactionId = null,
                            RaceId = null
                        };

                        await _characterService.CreateCharacterAsync(characterToSave);
                        _logger?.LogInformation("成功保存角色到数据库: {CharacterName}", character.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "保存角色到数据库失败: {CharacterName}", character.Name);
                    }
                }

                _logger?.LogInformation("模拟角色数据保存完成");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存模拟角色数据到数据库时发生错误");
            }
        }

        /// <summary>
        /// 更新角色列表显示
        /// </summary>
        private void UpdateCharacterList()
        {
            CharacterListControl.ItemsSource = _filteredCharacters;
        }

        #endregion

        #region 筛选和搜索

        /// <summary>
        /// 角色类型筛选变化事件
        /// </summary>
        private void CharacterTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// 搜索文本变化事件
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// 应用筛选条件
        /// </summary>
        private void ApplyFilters()
        {
            var searchText = SearchTextBox.Text?.Trim().ToLower() ?? string.Empty;
            var selectedType = ((ComboBoxItem)CharacterTypeFilter.SelectedItem)?.Content?.ToString();

            _filteredCharacters = _allCharacters.Where(c =>
            {
                // 搜索筛选
                var matchesSearch = string.IsNullOrEmpty(searchText) ||
                                  c.Name.ToLower().Contains(searchText) ||
                                  (c.Background != null && c.Background.ToLower().Contains(searchText)) ||
                                  (c.Faction?.Name != null && c.Faction.Name.ToLower().Contains(searchText));

                // 类型筛选
                var matchesType = string.IsNullOrEmpty(selectedType) ||
                                selectedType == "全部" ||
                                c.Type == selectedType;

                return matchesSearch && matchesType;
            }).ToList();

            UpdateCharacterList();
        }

        #endregion

        #region 角色操作

        /// <summary>
        /// 选择角色
        /// </summary>
        private void SelectCharacter(Character character)
        {
            if (character != null)
            {
                _selectedCharacter = character;
                ShowCharacterDetails(character);
            }
        }

        /// <summary>
        /// 显示角色详细信息
        /// </summary>
        private void ShowCharacterDetails(Character character)
        {
            try
            {
                // 隐藏默认卡片，显示角色详情卡片
                DefaultCard.Visibility = Visibility.Collapsed;
                CharacterDetailCard.Visibility = Visibility.Visible;

                // 填充角色信息
                CharacterNameText.Text = character.Name;
                CharacterTypeChip.Content = character.Type;
                CharacterFactionChip.Content = character.Faction?.Name ?? "无";
                CharacterCultivationText.Text = character.CultivationLevel ?? "未知";
                CharacterDescriptionText.Text = !string.IsNullOrEmpty(character.Background)
                    ? character.Background
                    : "暂无描述";
                CharacterAppearanceText.Text = !string.IsNullOrEmpty(character.Appearance)
                    ? character.Appearance
                    : "暂无外貌描述";
                CharacterPersonalityText.Text = !string.IsNullOrEmpty(character.Personality)
                    ? character.Personality
                    : "暂无性格描述";
                CharacterBackgroundText.Text = !string.IsNullOrEmpty(character.Background)
                    ? character.Background
                    : "暂无背景故事";
                CharacterAbilitiesText.Text = !string.IsNullOrEmpty(character.Abilities)
                    ? character.Abilities
                    : "暂无能力描述";

                _logger?.LogInformation($"显示角色详情: {character.Name}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "显示角色详情失败");
                MessageBox.Show($"显示角色详情失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 新建角色按钮点击事件
        /// </summary>
        private async void NewCharacter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newCharacterDialog = new CharacterEditDialog();
                newCharacterDialog.Owner = Window.GetWindow(this);

                if (newCharacterDialog.ShowDialog() == true)
                {
                    // 获取当前项目ID
                    var currentProjectId = GetCurrentProjectId();
                    if (currentProjectId == Guid.Empty)
                    {
                        MessageBox.Show("没有当前项目，无法创建角色", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // 获取新角色信息
                    var newCharacter = newCharacterDialog.Character;
                    newCharacter.ProjectId = currentProjectId;

                    // 处理势力信息 - 如果势力不存在，则不设置FactionId
                    if (newCharacter.Faction != null && !string.IsNullOrEmpty(newCharacter.Faction.Name))
                    {
                        // 这里应该查找或创建势力，暂时设置为null避免外键约束错误
                        newCharacter.FactionId = null;
                        newCharacter.Faction = null;
                    }

                    // 调用服务层创建角色
                    if (_characterService != null)
                    {
                        var createdCharacter = await _characterService.CreateCharacterAsync(newCharacter);

                        // 添加到本地集合
                        _allCharacters.Add(createdCharacter);
                        ApplyFilters();

                        // 选中新创建的角色
                        SelectCharacter(createdCharacter);

                        MessageBox.Show($"角色 '{createdCharacter.Name}' 创建成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        _logger?.LogInformation($"新建角色成功: {createdCharacter.Name}");
                    }
                    else
                    {
                        // 如果服务不可用，只添加到本地
                        newCharacter.Id = Guid.NewGuid();
                        _allCharacters.Add(newCharacter);
                        ApplyFilters();
                        SelectCharacter(newCharacter);

                        MessageBox.Show($"角色 '{newCharacter.Name}' 创建成功（仅本地）！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        _logger?.LogWarning("角色服务不可用，仅创建本地数据");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "新建角色失败");
                MessageBox.Show($"新建角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导入角色按钮点击事件
        /// </summary>
        private void ImportCharacters_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导入角色功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 导出角色按钮点击事件
        /// </summary>
        private void ExportCharacters_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导出角色功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 编辑角色按钮点击事件
        /// </summary>
        private async void EditCharacter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedCharacter == null)
                {
                    MessageBox.Show("请先选择要编辑的角色", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var editDialog = new CharacterEditDialog(_selectedCharacter);
                editDialog.Owner = Window.GetWindow(this);

                if (editDialog.ShowDialog() == true)
                {
                    // 获取编辑后的角色信息
                    var editedCharacter = editDialog.Character;

                    // 调用服务层更新数据库
                    if (_characterService != null)
                    {
                        try
                        {
                            _logger?.LogInformation($"开始更新角色: {editedCharacter.Name}, ID: {editedCharacter.Id}");

                            // 确保编辑的角色有正确的ID和项目ID
                            if (editedCharacter.Id == Guid.Empty)
                            {
                                editedCharacter.Id = _selectedCharacter.Id;
                            }

                            if (editedCharacter.ProjectId == Guid.Empty)
                            {
                                editedCharacter.ProjectId = _selectedCharacter.ProjectId;
                            }

                            // 保留原始的创建时间
                            editedCharacter.CreatedAt = _selectedCharacter.CreatedAt;

                            var updatedCharacter = await _characterService.UpdateCharacterAsync(editedCharacter);

                            // 更新本地集合
                            var index = _allCharacters.FindIndex(c => c.Id == _selectedCharacter.Id);
                            if (index >= 0)
                            {
                                _allCharacters[index] = updatedCharacter;
                                ApplyFilters();

                                // 更新详情显示
                                ShowCharacterDetails(updatedCharacter);
                                _selectedCharacter = updatedCharacter;
                            }

                            MessageBox.Show("角色信息更新成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            _logger?.LogInformation($"角色编辑成功: {updatedCharacter.Name}");

                            // 触发角色更新事件，通知其他界面
                            CharacterUpdated?.Invoke(this, new CharacterUpdatedEventArgs(updatedCharacter.Id, updatedCharacter));
                        }
                        catch (Exception updateEx)
                        {
                            _logger?.LogError(updateEx, $"更新角色失败: {editedCharacter.Name}");
                            throw new InvalidOperationException($"更新角色失败: {updateEx.Message}", updateEx);
                        }
                    }
                    else
                    {
                        // 如果服务不可用，只更新本地数据
                        var index = _allCharacters.FindIndex(c => c.Id == _selectedCharacter.Id);
                        if (index >= 0)
                        {
                            _allCharacters[index] = editedCharacter;
                            ApplyFilters();
                            ShowCharacterDetails(editedCharacter);
                            _selectedCharacter = editedCharacter;
                        }

                        MessageBox.Show("角色信息更新成功（仅本地）！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        _logger?.LogWarning("角色服务不可用，仅更新本地数据");

                        // 即使是本地更新，也触发事件通知其他界面
                        CharacterUpdated?.Invoke(this, new CharacterUpdatedEventArgs(editedCharacter.Id, editedCharacter));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "编辑角色失败");
                MessageBox.Show($"编辑角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除角色按钮点击事件
        /// </summary>
        private async void DeleteCharacter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedCharacter == null)
                {
                    MessageBox.Show("请先选择要删除的角色", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 如果有角色服务，先检查引用
                if (_characterService != null)
                {
                    // 检查角色是否被引用
                    var referenceInfo = await _characterService.CheckCharacterReferencesAsync(_selectedCharacter.Id);

                    if (referenceInfo.IsReferenced)
                    {
                        var referenceDetails = string.Join("\n", referenceInfo.References);
                        var result = MessageBox.Show(
                            $"角色 '{_selectedCharacter.Name}' 已被以下内容引用：\n\n{referenceDetails}\n\n" +
                            "已被引用的角色只能编辑，不能删除。是否要编辑此角色？",
                            "角色已被引用",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            EditCharacter_Click(sender, e);
                        }
                        return;
                    }
                }

                // 显示删除确认对话框
                var deleteDialog = new CharacterDeleteDialog(_selectedCharacter);
                deleteDialog.Owner = Window.GetWindow(this);

                if (deleteDialog.ShowDialog() == true)
                {
                    var characterName = _selectedCharacter.Name;
                    var characterId = _selectedCharacter.Id;

                    // 调用服务层安全删除
                    if (_characterService != null)
                    {
                        var deleteResult = await _characterService.SafeDeleteCharacterAsync(characterId);

                        if (deleteResult.Success)
                        {
                            // 从本地集合中移除
                            _allCharacters.Remove(_selectedCharacter);
                            ApplyFilters();
                            BackToOverview_Click(sender, e);

                            MessageBox.Show($"角色 '{characterName}' 删除成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            _logger?.LogInformation($"角色删除成功: {characterName}");
                        }
                        else
                        {
                            MessageBox.Show($"删除失败: {deleteResult.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            _logger?.LogWarning($"角色删除失败: {characterName}, 原因: {deleteResult.Message}");
                        }
                    }
                    else
                    {
                        // 如果服务不可用，只删除本地数据
                        _allCharacters.Remove(_selectedCharacter);
                        ApplyFilters();
                        BackToOverview_Click(sender, e);

                        MessageBox.Show($"角色 '{characterName}' 删除成功（仅本地）！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        _logger?.LogWarning("角色服务不可用，仅删除本地数据");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "删除角色失败");
                MessageBox.Show($"删除角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 返回概览按钮点击事件
        /// </summary>
        private void BackToOverview_Click(object sender, RoutedEventArgs e)
        {
            // 隐藏角色详情卡片，显示默认卡片
            CharacterDetailCard.Visibility = Visibility.Collapsed;
            DefaultCard.Visibility = Visibility.Visible;
            _selectedCharacter = null;
        }

        #endregion

        #region AI辅助功能

        /// <summary>
        /// AI生成角色按钮点击事件
        /// </summary>
        private async void AIGenerateCharacter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger?.LogInformation("开始AI生成角色");

                // 显示生成参数对话框
                var dialog = new CharacterGenerationDialog();
                if (dialog.ShowDialog() == true)
                {
                    var parameters = new Dictionary<string, object>
                    {
                        ["characterType"] = dialog.CharacterType,
                        ["faction"] = dialog.Faction,
                        ["cultivationLevel"] = dialog.CultivationLevel,
                        ["personalityTraits"] = dialog.PersonalityTraits,
                        ["backgroundStory"] = dialog.BackgroundStory,
                        ["specialAbilities"] = dialog.SpecialAbilities,
                        ["existingCharacters"] = _allCharacters.Select(c => new { c.Name, c.Type, Faction = c.Faction?.Name }).ToList()
                    };

                    if (_aiAssistantService != null)
                    {
                        var result = await _aiAssistantService.GenerateCharacterAsync(parameters);

                        if (result.IsSuccess && result.Data != null)
                        {
                            // 解析生成的角色数据
                            var generatedCharacter = ParseGeneratedCharacter(result.Data);
                            if (generatedCharacter != null)
                            {
                                // 添加到角色列表
                                generatedCharacter.Id = Guid.NewGuid();
                                generatedCharacter.ProjectId = GetCurrentProjectId();
                                _allCharacters.Add(generatedCharacter);
                                ApplyFilters();

                                _aiAssistantService.ShowSuccess($"成功生成角色: {generatedCharacter.Name}");
                                _logger?.LogInformation($"AI生成角色成功: {generatedCharacter.Name}");
                            }
                            else
                            {
                                _aiAssistantService.ShowError("生成的角色数据格式不正确");
                            }
                        }
                        else
                        {
                            _aiAssistantService.ShowError(result.Message ?? "AI生成角色失败");
                        }
                    }
                    else
                    {
                        MessageBox.Show("AI助手服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AI生成角色异常");
                MessageBox.Show($"AI生成角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// AI优化角色按钮点击事件
        /// </summary>
        private async void AIOptimizeCharacter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedCharacter == null)
                {
                    MessageBox.Show("请先选择要优化的角色", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _logger?.LogInformation($"开始AI优化角色: {_selectedCharacter.Name}");

                // 显示优化选项对话框
                var dialog = new CharacterOptimizationDialog(_selectedCharacter);
                if (dialog.ShowDialog() == true)
                {
                    var optimizationGoals = dialog.SelectedOptimizationGoals;

                    if (_aiAssistantService != null)
                    {
                        var parameters = new Dictionary<string, object>
                        {
                            ["characterData"] = _selectedCharacter,
                            ["optimizationGoals"] = optimizationGoals
                        };
                        var result = await _aiAssistantService.OptimizeCharacterAsync(parameters);

                        if (result.IsSuccess && result.Data != null)
                        {
                            // 解析优化后的角色数据
                            var optimizedCharacter = ParseGeneratedCharacter(result.Data);
                            if (optimizedCharacter != null)
                            {
                                // 更新角色信息
                                var index = _allCharacters.FindIndex(c => c.Id == _selectedCharacter.Id);
                                if (index >= 0)
                                {
                                    optimizedCharacter.Id = _selectedCharacter.Id;
                                    _allCharacters[index] = optimizedCharacter;
                                    ApplyFilters();

                                    _aiAssistantService.ShowSuccess($"成功优化角色: {optimizedCharacter.Name}");
                                    _logger?.LogInformation($"AI优化角色成功: {optimizedCharacter.Name}");
                                }
                            }
                            else
                            {
                                _aiAssistantService.ShowError("优化后的角色数据格式不正确");
                            }
                        }
                        else
                        {
                            _aiAssistantService.ShowError(result.Message ?? "AI优化角色失败");
                        }
                    }
                    else
                    {
                        MessageBox.Show("AI助手服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AI优化角色异常");
                MessageBox.Show($"AI优化角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// AI分析角色关系按钮点击事件
        /// </summary>
        private async void AIAnalyzeRelationships_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_allCharacters.Count < 2)
                {
                    MessageBox.Show("至少需要2个角色才能进行关系分析", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _logger?.LogInformation("开始AI分析角色关系");

                if (_aiAssistantService != null)
                {
                    var parameters = new Dictionary<string, object>
                    {
                        ["characters"] = _allCharacters.Cast<object>().ToList()
                    };
                    var result = await _aiAssistantService.AnalyzeCharacterRelationshipsAsync(parameters);

                    if (result.IsSuccess && result.Data != null)
                    {
                        // 显示关系分析结果
                        var analysisDialog = new RelationshipAnalysisDialog(result.Data);
                        analysisDialog.ShowDialog();

                        _aiAssistantService.ShowSuccess("角色关系分析完成");
                        _logger?.LogInformation("AI分析角色关系成功");
                    }
                    else
                    {
                        _aiAssistantService.ShowError(result.Message ?? "AI分析角色关系失败");
                    }
                }
                else
                {
                    MessageBox.Show("AI助手服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AI分析角色关系异常");
                MessageBox.Show($"AI分析角色关系失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 解析生成的角色数据
        /// </summary>
        /// <param name="data">生成的数据</param>
        /// <returns>角色实体</returns>
        private Character? ParseGeneratedCharacter(object data)
        {
            try
            {
                // 这里应该根据实际的AI返回数据格式进行解析
                // 暂时返回模拟数据
                return new Character
                {
                    Id = Guid.NewGuid(),
                    Name = "AI生成角色",
                    Type = "主配角",
                    Faction = new Faction { Name = "未知势力" },
                    Background = "由AI生成的角色，具有独特的背景和能力",
                    CultivationLevel = "筑基期",
                    Personality = "神秘莫测，实力不凡",
                    ProjectId = GetCurrentProjectId()
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "解析生成的角色数据失败");
                return null;
            }
        }

        #endregion
    }

    #region 对话框类（暂时放在这里，实际应该单独创建文件）

    /// <summary>
    /// 角色生成对话框
    /// </summary>
    public class CharacterGenerationDialog : Window
    {
        public string CharacterType { get; set; } = "主配角";
        public string Faction { get; set; } = "";
        public string CultivationLevel { get; set; } = "筑基期";
        public List<string> PersonalityTraits { get; set; } = new();
        public string BackgroundStory { get; set; } = "";
        public List<string> SpecialAbilities { get; set; } = new();

        public CharacterGenerationDialog()
        {
            Title = "AI角色生成参数";
            Width = 400;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // 延迟设置DialogResult，在窗口加载后
            Loaded += (s, e) =>
            {
                var result = MessageBox.Show("是否使用默认参数生成角色？", "角色生成", MessageBoxButton.YesNo, MessageBoxImage.Question);
                DialogResult = result == MessageBoxResult.Yes;
            };
        }
    }

    /// <summary>
    /// 角色优化对话框
    /// </summary>
    public class CharacterOptimizationDialog : Window
    {
        public List<string> SelectedOptimizationGoals { get; set; } = new();

        public CharacterOptimizationDialog(Character character)
        {
            Title = $"优化角色: {character.Name}";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // 延迟设置DialogResult，在窗口加载后
            Loaded += (s, e) =>
            {
                SelectedOptimizationGoals = new List<string> { "完善背景故事", "优化性格描述", "增强能力设定" };
                var result = MessageBox.Show("是否使用默认优化目标？", "角色优化", MessageBoxButton.YesNo, MessageBoxImage.Question);
                DialogResult = result == MessageBoxResult.Yes;
            };
        }
    }

    /// <summary>
    /// 关系分析对话框
    /// </summary>
    public class RelationshipAnalysisDialog : Window
    {
        public RelationshipAnalysisDialog(object analysisData)
        {
            Title = "角色关系分析结果";
            Width = 600;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // 这里应该显示具体的分析结果，暂时简化
            MessageBox.Show("角色关系分析结果已生成", "分析完成", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
    }

    #endregion
}
