using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 关系网络视图
    /// </summary>
    public partial class RelationshipNetworkView : UserControl, INavigationRefreshableView
    {
        #region 字段

        private List<CharacterNodeViewModel> _allCharacters;
        private List<RelationshipViewModel> _allRelationships;
        private List<CharacterNodeViewModel> _filteredCharacters;
        private CharacterNodeViewModel? _selectedCharacter;
        private RelationshipViewModel? _selectedRelationship;
        private bool _isDragging;
        private Point _lastMousePosition;
        private ToolTip? _activeTooltip;
        private bool _showCharacterLabels = true;
        private bool _dimUnselectedElements = true;
        private double _nodeDiameter = 60;
        private double _canvasWidth = 800;
        private double _canvasHeight = 600;

        // 服务
        private CharacterService? _characterService;
        private CharacterRelationshipService? _characterRelationshipService;
        private AIAssistantService? _aiAssistantService;
        private ProjectContextService? _projectContextService;
        private CurrentProjectGuard? _currentProjectGuard;
        private ILogger<RelationshipNetworkView>? _logger;

        #endregion

        #region 属性

        /// <summary>
        /// 选择角色命令
        /// </summary>
        public ICommand SelectCharacterCommand { get; set; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public RelationshipNetworkView()
        {
            InitializeComponent();
            InitializeServices();
            InitializeData();
            InitializeCommands();

            // 设置DataContext为当前实例，以便XAML中的命令绑定能够工作
            DataContext = this;

            // 异步加载真实数据
            LoadCharactersAsync();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化服务
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                var serviceProvider = App.ServiceProvider;
                if (serviceProvider != null)
                {
                    _characterService = serviceProvider.GetService<CharacterService>();
                    _characterRelationshipService = serviceProvider.GetService<CharacterRelationshipService>();
                    _aiAssistantService = serviceProvider.GetService<AIAssistantService>();
                    _projectContextService = serviceProvider.GetService<ProjectContextService>();
                    _currentProjectGuard = serviceProvider.GetService<CurrentProjectGuard>();
                    _logger = serviceProvider.GetService<ILogger<RelationshipNetworkView>>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化服务失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            _allCharacters = new List<CharacterNodeViewModel>();
            _allRelationships = new List<RelationshipViewModel>();
            _filteredCharacters = new List<CharacterNodeViewModel>();
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectCharacterCommand = new RelayCommand<CharacterNodeViewModel>(SelectCharacter);
        }

        /// <summary>
        /// 异步加载角色数据
        /// </summary>
        private async void LoadCharactersAsync()
        {
            await LoadCharactersDataAsync();
        }

        /// <summary>
        /// 加载角色和关系数据。
        /// </summary>
        private async Task LoadCharactersDataAsync()
        {
            try
            {
                _logger?.LogInformation("开始加载关系网络角色数据");

                // 获取当前项目ID
                var currentProjectId = GetCurrentProjectId();
                if (currentProjectId == Guid.Empty)
                {
                    _logger?.LogWarning("没有当前项目，关系网络进入空状态");
                    _allCharacters = new List<CharacterNodeViewModel>();
                    _allRelationships = new List<RelationshipViewModel>();
                    _filteredCharacters = new List<CharacterNodeViewModel>();
                    UpdateCharacterList();
                    DrawNetworkGraph();
                    EnsureCurrentProject("关系网络", out _);
                    return;
                }

                if (_characterService != null)
                {
                    // 从数据库加载真实角色数据
                    var characters = await _characterService.GetCharactersByProjectIdAsync(currentProjectId);
                    var characterList = characters.ToList();

                    _logger?.LogInformation("成功获取 {Count} 个角色", characterList.Count);

                    // 转换为关系网络视图模型
                    _allCharacters = ConvertToCharacterNodeViewModels(characterList);

                    // 加载角色关系数据
                    await LoadCharacterRelationshipsAsync(characterList);

                    _filteredCharacters = new List<CharacterNodeViewModel>(_allCharacters);

                    _logger?.LogInformation("成功加载 {Count} 个角色到关系网络", _allCharacters.Count);
                }
                else
                {
                    _logger?.LogWarning("CharacterService 不可用，关系网络进入空状态");
                    _allCharacters = new List<CharacterNodeViewModel>();
                    _allRelationships = new List<RelationshipViewModel>();
                    _filteredCharacters = new List<CharacterNodeViewModel>();
                }

                UpdateCharacterList();
                DrawNetworkGraph();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载关系网络角色数据失败");

                _allCharacters = new List<CharacterNodeViewModel>();
                _allRelationships = new List<RelationshipViewModel>();
                _filteredCharacters = new List<CharacterNodeViewModel>();
                UpdateCharacterList();
                DrawNetworkGraph();

                MessageBox.Show($"加载角色数据失败：{ex.Message}", "警告",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 获取当前项目ID
        /// </summary>
        private Guid GetCurrentProjectId()
        {
            try
            {
                if (_projectContextService?.CurrentProjectId != null)
                {
                    return _projectContextService.CurrentProjectId.Value;
                }

                _logger?.LogWarning("项目上下文服务不可用或没有当前项目");
                return Guid.Empty;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取当前项目ID失败");
                return Guid.Empty;
            }
        }

        private bool EnsureCurrentProject(string featureName, out Guid projectId)
        {
            if (_currentProjectGuard != null)
            {
                return _currentProjectGuard.TryGetCurrentProjectId(Window.GetWindow(this), featureName, out projectId);
            }

            projectId = GetCurrentProjectId();
            return projectId != Guid.Empty;
        }

        /// <summary>
        /// 转换角色实体为关系网络视图模型
        /// </summary>
        private List<CharacterNodeViewModel> ConvertToCharacterNodeViewModels(List<Character> characters)
        {
            var viewModels = new List<CharacterNodeViewModel>();
            var random = new Random();

            for (int i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                var viewModel = new CharacterNodeViewModel
                {
                    Id = i + 1, // 使用索引作为显示ID
                    CharacterId = character.Id, // 保存真实的数据库ID
                    Name = character.Name,
                    Type = character.Type,
                    Faction = character.Faction?.Name ?? "无势力",
                    InitialLetter = character.Name.Length > 0 ? character.Name.Substring(0, 1) : "?",
                    ConnectionStrengthColor = GetCharacterColor(character.Type),
                    ConnectionCount = 0, // 将在加载关系时更新
                    RelationshipCount = 0, // 将在加载关系时更新
                    X = 200 + (i % 5) * 120 + random.Next(-30, 30), // 网格布局加随机偏移
                    Y = 200 + (i / 5) * 120 + random.Next(-30, 30)
                };

                viewModels.Add(viewModel);
            }

            return viewModels;
        }

        /// <summary>
        /// 根据角色类型获取颜色
        /// </summary>
        private SolidColorBrush GetCharacterColor(string type)
        {
            return type switch
            {
                "主角" => new SolidColorBrush(Colors.Red),
                "女主角" => new SolidColorBrush(Colors.Pink),
                "主配角" => new SolidColorBrush(Colors.Gold),
                "反派" => new SolidColorBrush(Colors.DarkRed),
                "配角" => new SolidColorBrush(Colors.Green),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        /// <summary>
        /// 加载角色关系数据
        /// </summary>
        private async Task LoadCharacterRelationshipsAsync(List<Character> characters)
        {
            try
            {
                _allRelationships = new List<RelationshipViewModel>();
                var loadedRelationshipIds = new HashSet<Guid>();

                // 为每个角色加载其关系
                foreach (var character in characters)
                {
                    if (_characterService != null)
                    {
                        var relationships = await _characterService.GetCharacterRelationshipsAsync(character.Id);

                        foreach (var relationship in relationships)
                        {
                            if (!loadedRelationshipIds.Add(relationship.Id))
                            {
                                continue;
                            }

                            // 查找对应的视图模型
                            var sourceViewModel = _allCharacters.FirstOrDefault(c => c.CharacterId == relationship.SourceCharacterId);
                            var targetViewModel = _allCharacters.FirstOrDefault(c => c.CharacterId == relationship.TargetCharacterId);

                            if (sourceViewModel != null && targetViewModel != null)
                            {
                                var relationshipViewModel = new RelationshipViewModel
                                {
                                    RelationshipId = relationship.Id,
                                    Id = _allRelationships.Count + 1,
                                    SourceCharacterGuid = relationship.SourceCharacterId,
                                    TargetCharacterGuid = relationship.TargetCharacterId,
                                    FromCharacterId = sourceViewModel.Id,
                                    ToCharacterId = targetViewModel.Id,
                                    FromCharacterName = sourceViewModel.Name,
                                    ToCharacterName = targetViewModel.Name,
                                    RelationshipType = relationship.RelationshipType,
                                    Strength = relationship.Intensity * 10, // 转换强度范围
                                    Description = relationship.Description ?? "",
                                    Status = relationship.Status,
                                    Color = GetRelationshipColor(relationship.RelationshipType)
                                };

                                _allRelationships.Add(relationshipViewModel);

                                // 更新角色的关系计数
                                sourceViewModel.RelationshipCount++;
                                sourceViewModel.ConnectionCount++;
                                targetViewModel.ConnectionCount++;
                            }
                        }
                    }
                }

                _logger?.LogInformation("成功加载 {Count} 个角色关系", _allRelationships.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载角色关系失败");
                _allRelationships = new List<RelationshipViewModel>();
            }
        }

        /// <summary>
        /// 根据关系类型获取颜色
        /// </summary>
        private SolidColorBrush GetRelationshipColor(string relationshipType)
        {
            return relationshipType switch
            {
                "爱情关系" => new SolidColorBrush(Colors.Red),
                "师徒关系" => new SolidColorBrush(Colors.Gold),
                "敌对关系" => new SolidColorBrush(Colors.DarkRed),
                "兄弟关系" => new SolidColorBrush(Colors.Green),
                "师友关系" => new SolidColorBrush(Colors.Silver),
                "朋友关系" => new SolidColorBrush(Colors.Blue),
                "恩人关系" => new SolidColorBrush(Colors.White),
                "宿敌关系" => new SolidColorBrush(Colors.Black),
                "同门关系" => new SolidColorBrush(Colors.LightGreen),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        /// <summary>
        /// 加载模拟数据
        /// </summary>
        private void LoadMockData()
        {
            // 模拟角色数据 - 与角色管理界面保持一致
            _allCharacters = new List<CharacterNodeViewModel>
            {
                new CharacterNodeViewModel
                {
                    Id = 1,
                    Name = "林轩",
                    Type = "主角",
                    Faction = "玄天宗",
                    InitialLetter = "林",
                    ConnectionStrengthColor = new SolidColorBrush(Colors.Red),
                    ConnectionCount = 7,
                    RelationshipCount = 7,
                    X = 400,
                    Y = 300
                },
                new CharacterNodeViewModel
                {
                    Id = 2,
                    Name = "苏梦瑶",
                    Type = "女主角",
                    Faction = "天音阁",
                    InitialLetter = "苏",
                    ConnectionStrengthColor = new SolidColorBrush(Colors.Pink),
                    ConnectionCount = 4,
                    RelationshipCount = 4,
                    X = 300,
                    Y = 200
                },
                new CharacterNodeViewModel
                {
                    Id = 3,
                    Name = "玄天老祖",
                    Type = "主配角",
                    Faction = "玄天宗",
                    InitialLetter = "玄",
                    ConnectionStrengthColor = new SolidColorBrush(Colors.Gold),
                    ConnectionCount = 3,
                    RelationshipCount = 3,
                    X = 500,
                    Y = 200
                },
                new CharacterNodeViewModel
                {
                    Id = 4,
                    Name = "血魔尊者",
                    Type = "反派",
                    Faction = "血魔宗",
                    InitialLetter = "血",
                    ConnectionStrengthColor = new SolidColorBrush(Colors.DarkRed),
                    ConnectionCount = 3,
                    RelationshipCount = 3,
                    X = 600,
                    Y = 300
                },
                new CharacterNodeViewModel
                {
                    Id = 5,
                    Name = "王铁柱",
                    Type = "配角",
                    Faction = "玄天宗",
                    InitialLetter = "王",
                    ConnectionStrengthColor = new SolidColorBrush(Colors.Green),
                    ConnectionCount = 2,
                    RelationshipCount = 2,
                    X = 350,
                    Y = 400
                },
                new CharacterNodeViewModel
                {
                    Id = 6,
                    Name = "剑痴",
                    Type = "配角",
                    Faction = "散修",
                    InitialLetter = "剑",
                    ConnectionStrengthColor = new SolidColorBrush(Colors.Silver),
                    ConnectionCount = 2,
                    RelationshipCount = 2,
                    X = 450,
                    Y = 400
                },
                new CharacterNodeViewModel
                {
                    Id = 7,
                    Name = "诸葛明",
                    Type = "配角",
                    Faction = "天机阁",
                    InitialLetter = "诸",
                    ConnectionStrengthColor = new SolidColorBrush(Colors.Blue),
                    ConnectionCount = 3,
                    RelationshipCount = 3,
                    X = 250,
                    Y = 350
                },
                new CharacterNodeViewModel
                {
                    Id = 8,
                    Name = "白狐仙子",
                    Type = "配角",
                    Faction = "妖族",
                    InitialLetter = "白",
                    ConnectionStrengthColor = new SolidColorBrush(Colors.White),
                    ConnectionCount = 2,
                    RelationshipCount = 2,
                    X = 550,
                    Y = 400
                }
            };

            // 模拟关系数据 - 基于预写的八个角色
            _allRelationships = new List<RelationshipViewModel>
            {
                // 林轩与苏梦瑶的爱情关系
                new RelationshipViewModel
                {
                    Id = 1,
                    FromCharacterId = 1,
                    ToCharacterId = 2,
                    FromCharacterName = "林轩",
                    ToCharacterName = "苏梦瑶",
                    RelationshipType = "爱情关系",
                    Strength = 90,
                    Description = "相遇后逐渐产生情愫，两情相悦",
                    Status = "稳定",
                    Color = new SolidColorBrush(Colors.Red)
                },
                // 林轩与玄天老祖的师徒关系
                new RelationshipViewModel
                {
                    Id = 2,
                    FromCharacterId = 1,
                    ToCharacterId = 3,
                    FromCharacterName = "林轩",
                    ToCharacterName = "玄天老祖",
                    RelationshipType = "师徒关系",
                    Strength = 95,
                    Description = "师父收徒，传授修仙之道",
                    Status = "稳定",
                    Color = new SolidColorBrush(Colors.Gold)
                },
                // 林轩与血魔尊者的敌对关系
                new RelationshipViewModel
                {
                    Id = 3,
                    FromCharacterId = 1,
                    ToCharacterId = 4,
                    FromCharacterName = "林轩",
                    ToCharacterName = "血魔尊者",
                    RelationshipType = "敌对关系",
                    Strength = 85,
                    Description = "正邪不两立，宿命之敌",
                    Status = "紧张",
                    Color = new SolidColorBrush(Colors.DarkRed)
                },
                // 林轩与王铁柱的兄弟关系
                new RelationshipViewModel
                {
                    Id = 4,
                    FromCharacterId = 1,
                    ToCharacterId = 5,
                    FromCharacterName = "林轩",
                    ToCharacterName = "王铁柱",
                    RelationshipType = "兄弟关系",
                    Strength = 80,
                    Description = "师兄弟情深，可以托付后背",
                    Status = "稳定",
                    Color = new SolidColorBrush(Colors.Green)
                },
                // 林轩与剑痴的师友关系
                new RelationshipViewModel
                {
                    Id = 5,
                    FromCharacterId = 1,
                    ToCharacterId = 6,
                    FromCharacterName = "林轩",
                    ToCharacterName = "剑痴",
                    RelationshipType = "师友关系",
                    Strength = 70,
                    Description = "剑道指导，亦师亦友",
                    Status = "友好",
                    Color = new SolidColorBrush(Colors.Silver)
                },
                // 林轩与诸葛明的朋友关系
                new RelationshipViewModel
                {
                    Id = 6,
                    FromCharacterId = 1,
                    ToCharacterId = 7,
                    FromCharacterName = "林轩",
                    ToCharacterName = "诸葛明",
                    RelationshipType = "朋友关系",
                    Strength = 75,
                    Description = "智者相助，提供策略建议",
                    Status = "友好",
                    Color = new SolidColorBrush(Colors.Blue)
                },
                // 林轩与白狐仙子的恩人关系
                new RelationshipViewModel
                {
                    Id = 7,
                    FromCharacterId = 1,
                    ToCharacterId = 8,
                    FromCharacterName = "林轩",
                    ToCharacterName = "白狐仙子",
                    RelationshipType = "恩人关系",
                    Strength = 65,
                    Description = "救命之恩，心存感激",
                    Status = "友好",
                    Color = new SolidColorBrush(Colors.White)
                },
                // 玄天老祖与血魔尊者的宿敌关系
                new RelationshipViewModel
                {
                    Id = 8,
                    FromCharacterId = 3,
                    ToCharacterId = 4,
                    FromCharacterName = "玄天老祖",
                    ToCharacterName = "血魔尊者",
                    RelationshipType = "宿敌关系",
                    Strength = 90,
                    Description = "正邪对立，有旧怨",
                    Status = "敌对",
                    Color = new SolidColorBrush(Colors.Black)
                },
                // 苏梦瑶与白狐仙子的朋友关系
                new RelationshipViewModel
                {
                    Id = 9,
                    FromCharacterId = 2,
                    ToCharacterId = 8,
                    FromCharacterName = "苏梦瑶",
                    ToCharacterName = "白狐仙子",
                    RelationshipType = "朋友关系",
                    Strength = 60,
                    Description = "同为女性，惺惺相惜",
                    Status = "友好",
                    Color = new SolidColorBrush(Colors.LightPink)
                },
                // 王铁柱与剑痴的同门关系
                new RelationshipViewModel
                {
                    Id = 10,
                    FromCharacterId = 5,
                    ToCharacterId = 6,
                    FromCharacterName = "王铁柱",
                    ToCharacterName = "剑痴",
                    RelationshipType = "同门关系",
                    Strength = 55,
                    Description = "修炼路上的同道中人",
                    Status = "友好",
                    Color = new SolidColorBrush(Colors.LightGreen)
                }
            };

            _filteredCharacters = new List<CharacterNodeViewModel>(_allCharacters);
        }

        #endregion

        #region 界面更新

        /// <summary>
        /// 更新角色列表显示
        /// </summary>
        private void UpdateCharacterList()
        {
            CharacterListControl.ItemsSource = _filteredCharacters;
            CharacterCountLabel.Text = $"({_filteredCharacters.Count}个角色)";
        }

        /// <summary>
        /// 筛选与指定角色相关的关系
        /// </summary>
        /// <param name="selectedCharacter">选中的角色</param>
        private void FilterRelationshipsByCharacter(CharacterNodeViewModel? selectedCharacter)
        {
            if (selectedCharacter == null)
            {
                // 如果没有选中角色，显示所有角色和关系
                _filteredCharacters = new List<CharacterNodeViewModel>(_allCharacters);
                UpdateCharacterList();
                DrawNetworkGraph();
                return;
            }

            _logger?.LogInformation("筛选与角色 {CharacterName} 相关的关系", selectedCharacter.Name);

            // 获取与选中角色相关的所有关系
            var relatedRelationships = _allRelationships.Where(r =>
                r.FromCharacterId == selectedCharacter.Id ||
                r.ToCharacterId == selectedCharacter.Id).ToList();

            // 获取所有相关角色的ID
            var relatedCharacterIds = new HashSet<int> { selectedCharacter.Id };
            foreach (var relationship in relatedRelationships)
            {
                relatedCharacterIds.Add(relationship.FromCharacterId);
                relatedCharacterIds.Add(relationship.ToCharacterId);
            }

            // 筛选相关角色
            _filteredCharacters = _allCharacters.Where(c => relatedCharacterIds.Contains(c.Id)).ToList();

            _logger?.LogInformation("找到 {RelationshipCount} 个相关关系，{CharacterCount} 个相关角色",
                relatedRelationships.Count, _filteredCharacters.Count);

            // 重新绘制网络图，只显示相关的角色和关系
            UpdateCharacterList();
            DrawNetworkGraph(relatedRelationships);
        }

        /// <summary>
        /// 绘制网络图
        /// </summary>
        private void DrawNetworkGraph()
        {
            DrawNetworkGraph(_allRelationships);
        }

        /// <summary>
        /// 绘制网络图（指定关系列表）
        /// </summary>
        /// <param name="relationshipsToShow">要显示的关系列表</param>
        private void DrawNetworkGraph(List<RelationshipViewModel> relationshipsToShow)
        {
            ApplyCanvasSettings();
            NetworkCanvas.Children.Clear();

            // 绘制关系连线
            foreach (var relationship in relationshipsToShow)
            {
                var fromChar = _filteredCharacters.FirstOrDefault(c => c.Id == relationship.FromCharacterId);
                var toChar = _filteredCharacters.FirstOrDefault(c => c.Id == relationship.ToCharacterId);

                if (fromChar != null && toChar != null)
                {
                    DrawRelationshipLine(fromChar, toChar, relationship);
                }
            }

            // 绘制角色节点（只绘制筛选后的角色）
            foreach (var character in _filteredCharacters)
            {
                DrawCharacterNode(character);
            }
        }

        /// <summary>
        /// 绘制关系连线
        /// </summary>
        private void DrawRelationshipLine(CharacterNodeViewModel from, CharacterNodeViewModel to, RelationshipViewModel relationship)
        {
            var isConnectedToSelectedCharacter = _selectedCharacter != null &&
                (relationship.FromCharacterId == _selectedCharacter.Id || relationship.ToCharacterId == _selectedCharacter.Id);
            var isSelectedRelationship = _selectedRelationship?.RelationshipId == relationship.RelationshipId;
            var shouldDim = _dimUnselectedElements &&
                ((_selectedRelationship != null && !isSelectedRelationship) ||
                 (_selectedCharacter != null && !isConnectedToSelectedCharacter));

            var line = new Line
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = relationship.Color,
                StrokeThickness = Math.Max(1, relationship.Strength / 20.0) + (isSelectedRelationship ? 2 : 0),
                Opacity = shouldDim ? 0.18 : 0.92,
                Tag = relationship
            };

            line.MouseEnter += (s, e) => ShowRelationshipTooltip(relationship, e.GetPosition(NetworkCanvas));
            line.MouseLeave += (s, e) => HideTooltip();
            line.MouseLeftButtonDown += (s, e) => SelectRelationship(relationship);

            NetworkCanvas.Children.Add(line);
        }

        /// <summary>
        /// 绘制角色节点
        /// </summary>
        private void DrawCharacterNode(CharacterNodeViewModel character)
        {
            var isSelectedCharacter = _selectedCharacter?.CharacterId == character.CharacterId;
            var isConnectedToSelectedRelationship = _selectedRelationship != null &&
                (character.CharacterId == _selectedRelationship.SourceCharacterGuid ||
                 character.CharacterId == _selectedRelationship.TargetCharacterGuid);
            var isConnectedToSelectedCharacter = _selectedCharacter != null &&
                _allRelationships.Any(r =>
                    (r.FromCharacterId == _selectedCharacter.Id && r.ToCharacterId == character.Id) ||
                    (r.ToCharacterId == _selectedCharacter.Id && r.FromCharacterId == character.Id));
            var shouldDim = _dimUnselectedElements &&
                ((_selectedRelationship != null && !isConnectedToSelectedRelationship) ||
                 (_selectedCharacter != null && !isSelectedCharacter && !isConnectedToSelectedCharacter));
            var diameter = isSelectedCharacter ? _nodeDiameter + 10 : _nodeDiameter;

            // 创建节点圆圈
            var circle = new Ellipse
            {
                Width = diameter,
                Height = diameter,
                Fill = character.ConnectionStrengthColor,
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = isSelectedCharacter ? 5 : 3,
                Opacity = shouldDim ? 0.28 : 1.0,
                Tag = character
            };

            Canvas.SetLeft(circle, character.X - diameter / 2);
            Canvas.SetTop(circle, character.Y - diameter / 2);

            // 创建角色名称标签
            var nameLabel = new TextBlock
            {
                Text = character.Name,
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = _showCharacterLabels || isSelectedCharacter ? Visibility.Visible : Visibility.Collapsed,
                Opacity = shouldDim ? 0.35 : 1.0,
                Tag = character
            };

            Canvas.SetLeft(nameLabel, character.X - 25);
            Canvas.SetTop(nameLabel, character.Y - 8);

            // 添加事件处理
            circle.MouseEnter += (s, e) => ShowCharacterTooltip(character, e.GetPosition(NetworkCanvas));
            circle.MouseLeave += (s, e) => HideTooltip();
            circle.MouseLeftButtonDown += (s, e) => SelectCharacter(character);

            nameLabel.MouseEnter += (s, e) => ShowCharacterTooltip(character, e.GetPosition(NetworkCanvas));
            nameLabel.MouseLeave += (s, e) => HideTooltip();
            nameLabel.MouseLeftButtonDown += (s, e) => SelectCharacter(character);

            NetworkCanvas.Children.Add(circle);
            NetworkCanvas.Children.Add(nameLabel);
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 选择角色
        /// </summary>
        private void SelectCharacter(CharacterNodeViewModel character)
        {
            try
            {
                if (character == null)
                {
                    MessageBox.Show("角色数据为空", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _selectedCharacter = character;
                _selectedRelationship = null;
                ShowCharacterDetails(character);
                HighlightCharacterConnections(character);

                // 筛选与选中角色相关的关系
                FilterRelationshipsByCharacter(character);

                // 调试信息
                System.Diagnostics.Debug.WriteLine($"选中角色: {character.Name}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"选择角色时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 选择关系
        /// </summary>
        private void SelectRelationship(RelationshipViewModel relationship)
        {
            _selectedRelationship = relationship;
            _selectedCharacter = null;
            ShowRelationshipDetails(relationship);
            HighlightRelationship(relationship);
        }

        /// <summary>
        /// 显示角色详情
        /// </summary>
        private void ShowCharacterDetails(CharacterNodeViewModel character)
        {
            try
            {
                if (RelationshipDetailPanel == null)
                {
                    System.Diagnostics.Debug.WriteLine("RelationshipDetailPanel 为空");
                    return;
                }

                RelationshipDetailPanel.Children.Clear();
                System.Diagnostics.Debug.WriteLine($"显示角色详情: {character.Name}");

            // 角色基本信息
            var titleBlock = new TextBlock
            {
                Text = character.Name,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 12)
            };
            RelationshipDetailPanel.Children.Add(titleBlock);

            var typeBlock = new TextBlock
            {
                Text = $"类型：{character.Type}",
                Margin = new Thickness(0, 0, 0, 8)
            };
            RelationshipDetailPanel.Children.Add(typeBlock);

            var factionBlock = new TextBlock
            {
                Text = $"势力：{character.Faction}",
                Margin = new Thickness(0, 0, 0, 8)
            };
            RelationshipDetailPanel.Children.Add(factionBlock);

            var connectionBlock = new TextBlock
            {
                Text = $"关系数量：{character.RelationshipCount}",
                Margin = new Thickness(0, 0, 0, 16)
            };
            RelationshipDetailPanel.Children.Add(connectionBlock);

            // 相关关系列表
            var relationsTitle = new TextBlock
            {
                Text = "相关关系：",
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 8)
            };
            RelationshipDetailPanel.Children.Add(relationsTitle);

            var relatedRelationships = _allRelationships.Where(r => 
                r.FromCharacterId == character.Id || r.ToCharacterId == character.Id).ToList();

            foreach (var rel in relatedRelationships)
            {
                var relBlock = new TextBlock
                {
                    Text = $"• {rel.RelationshipType}: {(rel.FromCharacterId == character.Id ? rel.ToCharacterName : rel.FromCharacterName)}",
                    Margin = new Thickness(8, 0, 0, 4),
                    FontSize = 12
                };
                RelationshipDetailPanel.Children.Add(relBlock);
            }

                // 启用按钮
                ViewCharacterButton.IsEnabled = true;
                EditRelationshipButton.IsEnabled = false;
                DeleteRelationshipButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示角色详情时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"ShowCharacterDetails 错误: {ex}");
            }
        }

        /// <summary>
        /// 显示关系详情
        /// </summary>
        private void ShowRelationshipDetails(RelationshipViewModel relationship)
        {
            RelationshipDetailPanel.Children.Clear();

            // 关系基本信息
            var titleBlock = new TextBlock
            {
                Text = $"{relationship.FromCharacterName} ↔ {relationship.ToCharacterName}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 12)
            };
            RelationshipDetailPanel.Children.Add(titleBlock);

            var typeBlock = new TextBlock
            {
                Text = $"关系类型：{relationship.RelationshipType}",
                Margin = new Thickness(0, 0, 0, 8)
            };
            RelationshipDetailPanel.Children.Add(typeBlock);

            var strengthBlock = new TextBlock
            {
                Text = $"关系强度：{relationship.Strength}%",
                Margin = new Thickness(0, 0, 0, 8)
            };
            RelationshipDetailPanel.Children.Add(strengthBlock);

            var statusBlock = new TextBlock
            {
                Text = $"状态：{relationship.Status}",
                Margin = new Thickness(0, 0, 0, 8)
            };
            RelationshipDetailPanel.Children.Add(statusBlock);

            var descBlock = new TextBlock
            {
                Text = $"描述：{relationship.Description}",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
            RelationshipDetailPanel.Children.Add(descBlock);

            // 启用按钮
            EditRelationshipButton.IsEnabled = true;
            DeleteRelationshipButton.IsEnabled = true;
            ViewCharacterButton.IsEnabled = false;
        }

        /// <summary>
        /// 高亮角色连接
        /// </summary>
        private void HighlightCharacterConnections(CharacterNodeViewModel character)
        {
            _selectedCharacter = character;
            _selectedRelationship = null;
            DrawNetworkGraph();
        }

        /// <summary>
        /// 高亮关系
        /// </summary>
        private void HighlightRelationship(RelationshipViewModel relationship)
        {
            _selectedRelationship = relationship;
            _selectedCharacter = null;
            DrawNetworkGraph();
        }

        /// <summary>
        /// 显示角色提示
        /// </summary>
        private void ShowCharacterTooltip(CharacterNodeViewModel character, Point position)
        {
            var content = $"角色：{character.Name}\n类型：{character.Type}\n势力：{character.Faction}\n关系数：{character.RelationshipCount}";
            ShowTooltip(content, position);
        }

        /// <summary>
        /// 显示关系提示
        /// </summary>
        private void ShowRelationshipTooltip(RelationshipViewModel relationship, Point position)
        {
            var content = $"{relationship.FromCharacterName} -> {relationship.ToCharacterName}\n类型：{relationship.RelationshipType}\n强度：{relationship.Strength}\n状态：{relationship.Status}";
            ShowTooltip(content, position);
        }

        /// <summary>
        /// 隐藏提示
        /// </summary>
        private void HideTooltip()
        {
            if (_activeTooltip != null)
            {
                _activeTooltip.IsOpen = false;
                _activeTooltip = null;
            }
        }

        #endregion

        #region UI事件处理

        /// <summary>
        /// 关系类型筛选变化事件
        /// </summary>
        private void RelationshipTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                ApplyFilters();
            }
        }

        /// <summary>
        /// 势力筛选变化事件
        /// </summary>
        private void FactionFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                ApplyFilters();
            }
        }

        /// <summary>
        /// 搜索文本变化事件
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                ApplyFilters();
            }
        }

        /// <summary>
        /// 应用筛选条件
        /// </summary>
        private void ApplyFilters()
        {
            if (!IsLoaded || _allCharacters == null)
                return;

            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            var selectedType = ((ComboBoxItem)RelationshipTypeFilter?.SelectedItem)?.Content?.ToString() ?? "全部关系";
            var selectedFaction = ((ComboBoxItem)FactionFilter?.SelectedItem)?.Content?.ToString() ?? "全部势力";

            _filteredCharacters = _allCharacters.Where(c =>
                (string.IsNullOrEmpty(searchText) || c.Name.ToLower().Contains(searchText)) &&
                (selectedFaction == "全部势力" || c.Faction == selectedFaction)
            ).ToList();

            UpdateCharacterList();
            DrawNetworkGraph();
        }

        /// <summary>
        /// 添加关系按钮点击事件
        /// </summary>
        private void AddRelationship_Click(object sender, RoutedEventArgs e)
        {
            _ = AddRelationshipAsync();
        }

        private async Task AddRelationshipAsync()
        {
            try
            {
                var dialog = new AddRelationshipDialog(_allCharacters);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                {
                    if (_characterRelationshipService == null)
                    {
                        MessageBox.Show("角色关系服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var relationship = new CharacterRelationship
                    {
                        SourceCharacterId = dialog.SourceCharacterGuid,
                        TargetCharacterId = dialog.TargetCharacterGuid,
                        RelationshipType = dialog.RelationshipType,
                        Intensity = Math.Max(1, Math.Min(10, dialog.Strength / 10)),
                        Description = dialog.Description,
                        Status = dialog.Status,
                        RelationshipName = $"{dialog.FromCharacterName}-{dialog.RelationshipType}-{dialog.ToCharacterName}"
                    };

                    await _characterRelationshipService.CreateCharacterRelationshipAsync(relationship);
                    await LoadCharactersDataAsync();
                    MessageBox.Show("关系添加成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加关系失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 智能检测关系按钮点击事件
        /// </summary>
        private async void AutoDetectRelationships_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_allCharacters.Count < 2)
                {
                    MessageBox.Show("至少需要两个角色才能进行关系检测。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (_characterRelationshipService == null)
                {
                    MessageBox.Show("角色关系服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var candidates = await DetectRelationshipCandidatesAsync();
                if (candidates.Count == 0)
                {
                    MessageBox.Show("本次没有检测到新的可写入关系。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var confirmDialog = new RelationshipDetectionResultDialog(candidates)
                {
                    Owner = Window.GetWindow(this)
                };

                if (confirmDialog.ShowDialog() == true)
                {
                    var selectedCandidates = confirmDialog.GetSelectedCandidates();
                    if (selectedCandidates.Count == 0)
                    {
                        MessageBox.Show("未选择任何候选关系。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    foreach (var candidate in selectedCandidates)
                    {
                        var relationship = new CharacterRelationship
                        {
                            SourceCharacterId = candidate.SourceCharacterId,
                            TargetCharacterId = candidate.TargetCharacterId,
                            RelationshipType = candidate.RelationshipType,
                            Intensity = Math.Max(1, Math.Min(10, candidate.Strength / 10)),
                            Description = candidate.Description,
                            Status = candidate.Status,
                            RelationshipName = $"{candidate.SourceCharacterName}-{candidate.RelationshipType}-{candidate.TargetCharacterName}"
                        };

                        await _characterRelationshipService.CreateCharacterRelationshipAsync(relationship);
                    }

                    await LoadCharactersDataAsync();
                    var summary = string.Join(Environment.NewLine, selectedCandidates.Select(c =>
                        $"• {c.SourceCharacterName} - {c.TargetCharacterName}（{c.RelationshipType}）"));
                    MessageBox.Show($"已写入 {selectedCandidates.Count} 条关系：\n\n{summary}", "智能检测完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"智能检测失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示全部角色按钮点击事件
        /// </summary>
        private void ShowAllCharacters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger?.LogInformation("用户点击显示全部角色");

                // 清除选中状态
                _selectedCharacter = null;
                _selectedRelationship = null;

                // 显示所有角色和关系
                FilterRelationshipsByCharacter(null);

                _logger?.LogInformation("已显示全部 {CharacterCount} 个角色和 {RelationshipCount} 个关系",
                    _allCharacters.Count, _allRelationships.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "显示全部角色失败");
                MessageBox.Show($"显示全部角色失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 布局模式切换事件
        /// </summary>
        private void LayoutMode_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LayoutModeToggle?.IsChecked == true)
                {
                    // 切换到力导向布局
                    ApplyForceDirectedLayout();
                }
                else
                {
                    // 切换到圆形布局
                    ApplyCircularLayout();
                }
                DrawNetworkGraph();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"切换布局失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 网络分析按钮点击事件
        /// </summary>
        private void NetworkAnalysis_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var analysisDialog = new NetworkAnalysisDialog(_allCharacters, _allRelationships);
                analysisDialog.Owner = Window.GetWindow(this);
                analysisDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"网络分析失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出网络图按钮点击事件
        /// </summary>
        private void ExportNetwork_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var bitmap = CaptureNetworkBitmap();
                if (bitmap == null)
                {
                    MessageBox.Show("当前没有可导出的网络图内容。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出网络图",
                    Filter = "PNG 图片 (*.png)|*.png",
                    FileName = $"关系网络图_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using var fileStream = new FileStream(saveDialog.FileName, FileMode.Create, FileAccess.Write);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(fileStream);
                    MessageBox.Show($"网络图已导出到：{saveDialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 设置按钮点击事件
        /// </summary>
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new NetworkSettingsDialog(_showCharacterLabels, _dimUnselectedElements, _nodeDiameter, _canvasWidth, _canvasHeight)
                {
                    Owner = Window.GetWindow(this)
                };
                if (dialog.ShowDialog() == true)
                {
                    _showCharacterLabels = dialog.ShowCharacterLabels;
                    _dimUnselectedElements = dialog.DimUnselectedElements;
                    _nodeDiameter = dialog.NodeDiameter;
                    _canvasWidth = dialog.CanvasWidth;
                    _canvasHeight = dialog.CanvasHeight;
                    ApplyCanvasSettings();
                    DrawNetworkGraph();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开设置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 重新布局按钮点击事件
        /// </summary>
        private void RelayoutNetwork_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyForceDirectedLayout();
                DrawNetworkGraph();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重新布局失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 适应窗口按钮点击事件
        /// </summary>
        private void FitToWindow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 重置滚动位置到中心
                NetworkScrollViewer.ScrollToHorizontalOffset(NetworkScrollViewer.ScrollableWidth / 2);
                NetworkScrollViewer.ScrollToVerticalOffset(NetworkScrollViewer.ScrollableHeight / 2);
                MessageBox.Show("已重置视图到中心位置", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"适应窗口失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 全屏显示按钮点击事件
        /// </summary>
        private void FullScreen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var bitmap = CaptureNetworkBitmap();
                if (bitmap == null)
                {
                    MessageBox.Show("当前没有可显示的网络图内容。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var fullScreenWindow = new NetworkFullScreenWindow(bitmap, _allCharacters.Count, _allRelationships.Count);
                fullScreenWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"全屏显示失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 编辑关系按钮点击事件
        /// </summary>
        private void EditRelationship_Click(object sender, RoutedEventArgs e)
        {
            _ = EditRelationshipAsync();
        }

        private async Task EditRelationshipAsync()
        {
            try
            {
                if (_selectedRelationship != null)
                {
                    var dialog = new EditRelationshipDialog(_selectedRelationship);
                    dialog.Owner = Window.GetWindow(this);
                    if (dialog.ShowDialog() == true)
                    {
                        if (_characterRelationshipService == null)
                        {
                            MessageBox.Show("角色关系服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        var relationship = await _characterRelationshipService.GetCharacterRelationshipByIdAsync(_selectedRelationship.RelationshipId);
                        if (relationship == null)
                        {
                            MessageBox.Show("未找到要编辑的关系。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        relationship.RelationshipType = dialog.RelationshipType;
                        relationship.Intensity = Math.Max(1, Math.Min(10, dialog.Strength / 10));
                        relationship.Description = dialog.Description;
                        relationship.Status = dialog.Status;
                        await _characterRelationshipService.UpdateCharacterRelationshipAsync(relationship);

                        await LoadCharactersDataAsync();
                        _selectedRelationship = _allRelationships.FirstOrDefault(r => r.RelationshipId == relationship.Id);
                        if (_selectedRelationship != null)
                        {
                            ShowRelationshipDetails(_selectedRelationship);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"编辑关系失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除关系按钮点击事件
        /// </summary>
        private void DeleteRelationship_Click(object sender, RoutedEventArgs e)
        {
            _ = DeleteRelationshipAsync();
        }

        private async Task DeleteRelationshipAsync()
        {
            try
            {
                if (_selectedRelationship != null)
                {
                    var result = MessageBox.Show($"确定要删除关系\"{_selectedRelationship.RelationshipType}\"吗？",
                        "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (_characterRelationshipService == null)
                        {
                            MessageBox.Show("角色关系服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        var deleted = await _characterRelationshipService.DeleteCharacterRelationshipAsync(_selectedRelationship.RelationshipId);
                        if (!deleted)
                        {
                            MessageBox.Show("关系删除失败或关系不存在。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        _selectedRelationship = null;
                        await LoadCharactersDataAsync();
                        RelationshipDetailPanel.Children.Clear();
                        RelationshipDetailPanel.Children.Add(new TextBlock
                        {
                            Text = "请选择角色或关系查看详情",
                            FontStyle = FontStyles.Italic,
                            Foreground = new SolidColorBrush(Colors.Gray),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 50, 0, 0)
                        });

                        EditRelationshipButton.IsEnabled = false;
                        DeleteRelationshipButton.IsEnabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除关系失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 查看角色详情按钮点击事件
        /// </summary>
        private void ViewCharacterDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedCharacter != null)
                {
                    // 跳转到角色管理界面并选中该角色
                    var mainWindow = Window.GetWindow(this) as MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.ShowCharacterManagement();
                        MessageBox.Show($"已跳转到角色管理界面，请查看角色\"{_selectedCharacter.Name}\"的详细信息",
                            "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查看角色详情失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 网络画布鼠标按下事件
        /// </summary>
        private void NetworkCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _lastMousePosition = e.GetPosition(NetworkCanvas);
            NetworkCanvas.CaptureMouse();
        }

        /// <summary>
        /// 网络画布鼠标移动事件
        /// </summary>
        private void NetworkCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(NetworkCanvas);
                var deltaX = currentPosition.X - _lastMousePosition.X;
                var deltaY = currentPosition.Y - _lastMousePosition.Y;

                // TODO: 实现节点拖拽功能
                _lastMousePosition = currentPosition;
            }
        }

        /// <summary>
        /// 网络画布鼠标释放事件
        /// </summary>
        private void NetworkCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            NetworkCanvas.ReleaseMouseCapture();
        }

        private RenderTargetBitmap? CaptureNetworkBitmap()
        {
            try
            {
                NetworkCanvas.UpdateLayout();

                var width = (int)Math.Max(NetworkCanvas.ActualWidth, NetworkCanvas.Width);
                var height = (int)Math.Max(NetworkCanvas.ActualHeight, NetworkCanvas.Height);
                if (width <= 0 || height <= 0)
                {
                    return null;
                }

                NetworkCanvas.Measure(new Size(width, height));
                NetworkCanvas.Arrange(new Rect(0, 0, width, height));
                NetworkCanvas.UpdateLayout();

                var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(NetworkCanvas);
                return renderBitmap;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "捕获关系网络图像失败");
                return null;
            }
        }

        private void ShowTooltip(string content, Point position)
        {
            HideTooltip();
            _activeTooltip = new ToolTip
            {
                Content = new TextBlock
                {
                    Text = content,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 260,
                    LineHeight = 18
                },
                Placement = PlacementMode.Relative,
                PlacementTarget = NetworkCanvas,
                HorizontalOffset = position.X + 12,
                VerticalOffset = position.Y + 12,
                StaysOpen = true,
                IsOpen = true
            };
        }

        private void ApplyCanvasSettings()
        {
            NetworkCanvas.Width = Math.Max(600, _canvasWidth);
            NetworkCanvas.Height = Math.Max(400, _canvasHeight);
        }

        private async Task<List<DetectedRelationshipCandidate>> DetectRelationshipCandidatesAsync()
        {
            var existingKeys = new HashSet<string>(_allRelationships.Select(BuildRelationshipKey), StringComparer.OrdinalIgnoreCase);
            var candidates = new List<DetectedRelationshipCandidate>();

            if (_aiAssistantService != null)
            {
                var parameters = new Dictionary<string, object>
                {
                    ["characters"] = _allCharacters
                        .Select(c => new { c.Name, c.Type, c.Faction, c.RelationshipCount })
                        .Cast<object>()
                        .ToList(),
                    ["existingRelationships"] = _allRelationships
                        .Select(r => new { r.FromCharacterName, r.ToCharacterName, r.RelationshipType, r.Status })
                        .Cast<object>()
                        .ToList()
                };

                var result = await _aiAssistantService.AnalyzeCharacterRelationshipsAsync(parameters);
                if (result.IsSuccess && result.Data != null)
                {
                    candidates.AddRange(ParseRelationshipCandidates(result.Data, existingKeys));
                }
            }

            if (candidates.Count == 0)
            {
                candidates.AddRange(BuildHeuristicRelationshipCandidates(existingKeys));
            }

            return candidates
                .GroupBy(c => BuildRelationshipKey(c))
                .Select(g => g.First())
                .ToList();
        }

        private List<DetectedRelationshipCandidate> ParseRelationshipCandidates(object data, HashSet<string> existingKeys)
        {
            var text = data?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<DetectedRelationshipCandidate>();
            }

            var candidates = new List<DetectedRelationshipCandidate>();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim().TrimStart('•', '-', '*', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', '、', ' ');
                var matchedCharacters = _allCharacters
                    .Where(c => line.Contains(c.Name, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(c => c.Name.Length)
                    .Take(2)
                    .ToList();

                if (matchedCharacters.Count < 2)
                {
                    continue;
                }

                var relationshipType = InferRelationshipType(line, matchedCharacters[0], matchedCharacters[1]);
                var candidate = BuildRelationshipCandidate(matchedCharacters[0], matchedCharacters[1], relationshipType, line);
                if (candidate != null && existingKeys.Add(BuildRelationshipKey(candidate)))
                {
                    candidates.Add(candidate);
                }
            }

            return candidates;
        }

        private List<DetectedRelationshipCandidate> BuildHeuristicRelationshipCandidates(HashSet<string> existingKeys)
        {
            var candidates = new List<DetectedRelationshipCandidate>();
            var protagonist = _allCharacters.FirstOrDefault(c => c.Type == "主角");
            var femaleLead = _allCharacters.FirstOrDefault(c => c.Type == "女主角");
            if (protagonist != null && femaleLead != null)
            {
                var candidate = BuildRelationshipCandidate(protagonist, femaleLead, "爱情关系", "基于角色定位推断为潜在爱情关系");
                if (candidate != null && existingKeys.Add(BuildRelationshipKey(candidate)))
                {
                    candidates.Add(candidate);
                }
            }

            foreach (var sameFactionPair in _allCharacters
                .Where(c => !string.IsNullOrWhiteSpace(c.Faction) && c.Faction != "无势力")
                .GroupBy(c => c.Faction)
                .SelectMany(g => g.Take(3).SelectMany((left, index) => g.Skip(index + 1).Take(2).Select(right => (left, right)))))
            {
                var candidate = BuildRelationshipCandidate(sameFactionPair.left, sameFactionPair.right, "同门关系", "基于同势力推断为同门或阵营关系");
                if (candidate != null && existingKeys.Add(BuildRelationshipKey(candidate)))
                {
                    candidates.Add(candidate);
                }
            }

            var antagonist = _allCharacters.FirstOrDefault(c => c.Type == "反派");
            if (protagonist != null && antagonist != null)
            {
                var candidate = BuildRelationshipCandidate(protagonist, antagonist, "敌对关系", "基于主角与反派定位推断为潜在敌对关系");
                if (candidate != null && existingKeys.Add(BuildRelationshipKey(candidate)))
                {
                    candidates.Add(candidate);
                }
            }

            return candidates.Take(8).ToList();
        }

        private DetectedRelationshipCandidate? BuildRelationshipCandidate(CharacterNodeViewModel source, CharacterNodeViewModel target, string relationshipType, string description)
        {
            if (source.CharacterId == target.CharacterId)
            {
                return null;
            }

            return new DetectedRelationshipCandidate
            {
                SourceCharacterId = source.CharacterId,
                TargetCharacterId = target.CharacterId,
                SourceCharacterName = source.Name,
                TargetCharacterName = target.Name,
                RelationshipType = relationshipType,
                Strength = InferRelationshipStrength(relationshipType),
                Status = InferRelationshipStatus(relationshipType),
                Description = description
            };
        }

        private static string InferRelationshipType(string text, CharacterNodeViewModel source, CharacterNodeViewModel target)
        {
            if (Regex.IsMatch(text, "师徒|老师|徒弟"))
            {
                return "师徒关系";
            }
            if (Regex.IsMatch(text, "爱情|恋人|情侣|夫妻|爱慕|闺蜜"))
            {
                return "爱情关系";
            }
            if (Regex.IsMatch(text, "敌对|仇敌|死敌|对立|宿敌"))
            {
                return "敌对关系";
            }
            if (Regex.IsMatch(text, "亲属|父子|母女|兄妹|姐妹|兄弟|家人"))
            {
                return "亲属关系";
            }
            if (Regex.IsMatch(text, "从属|下属|上级|追随"))
            {
                return "从属关系";
            }
            if (source.Faction == target.Faction && !string.IsNullOrWhiteSpace(source.Faction) && source.Faction != "无势力")
            {
                return "同门关系";
            }

            return "朋友关系";
        }

        private static int InferRelationshipStrength(string relationshipType)
        {
            return relationshipType switch
            {
                "爱情关系" => 85,
                "师徒关系" => 80,
                "敌对关系" => 75,
                "亲属关系" => 90,
                "同门关系" => 70,
                "从属关系" => 68,
                _ => 60
            };
        }

        private static string InferRelationshipStatus(string relationshipType)
        {
            return relationshipType switch
            {
                "敌对关系" => "紧张",
                "爱情关系" => "友好",
                _ => "稳定"
            };
        }

        private static string BuildRelationshipKey(RelationshipViewModel relationship)
        {
            return $"{relationship.SourceCharacterGuid:N}|{relationship.TargetCharacterGuid:N}|{relationship.RelationshipType}";
        }

        private static string BuildRelationshipKey(DetectedRelationshipCandidate relationship)
        {
            return $"{relationship.SourceCharacterId:N}|{relationship.TargetCharacterId:N}|{relationship.RelationshipType}";
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 刷新关系网络数据
        /// </summary>
        public async Task RefreshNetworkDataAsync()
        {
            try
            {
                _logger?.LogInformation("开始刷新关系网络数据");
                await LoadCharactersDataAsync();
                _logger?.LogInformation("关系网络数据刷新完成");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "刷新关系网络数据失败");
                MessageBox.Show($"刷新关系网络数据失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 在项目切换后刷新关系网络数据。
        /// </summary>
        /// <param name="projectId">当前项目标识。</param>
        /// <param name="projectName">当前项目名称。</param>
        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            await RefreshNetworkDataAsync();
        }

        /// <summary>
        /// 更新指定角色的信息
        /// </summary>
        /// <param name="characterId">角色ID</param>
        /// <param name="updatedCharacter">更新后的角色信息</param>
        public void UpdateCharacterInfo(Guid characterId, Character updatedCharacter)
        {
            try
            {
                var characterNode = _allCharacters.FirstOrDefault(c => c.CharacterId == characterId);
                if (characterNode != null)
                {
                    // 更新角色信息
                    characterNode.Name = updatedCharacter.Name;
                    characterNode.Type = updatedCharacter.Type;
                    characterNode.Faction = updatedCharacter.Faction?.Name ?? "无势力";
                    characterNode.InitialLetter = updatedCharacter.Name.Length > 0 ?
                        updatedCharacter.Name.Substring(0, 1) : "?";
                    characterNode.ConnectionStrengthColor = GetCharacterColor(updatedCharacter.Type);

                    // 更新筛选列表
                    var filteredCharacter = _filteredCharacters.FirstOrDefault(c => c.CharacterId == characterId);
                    if (filteredCharacter != null)
                    {
                        filteredCharacter.Name = updatedCharacter.Name;
                        filteredCharacter.Type = updatedCharacter.Type;
                        filteredCharacter.Faction = updatedCharacter.Faction?.Name ?? "无势力";
                        filteredCharacter.InitialLetter = updatedCharacter.Name.Length > 0 ?
                            updatedCharacter.Name.Substring(0, 1) : "?";
                        filteredCharacter.ConnectionStrengthColor = GetCharacterColor(updatedCharacter.Type);
                    }

                    // 更新关系中的角色名称
                    foreach (var relationship in _allRelationships)
                    {
                        if (relationship.FromCharacterId == characterNode.Id)
                        {
                            relationship.FromCharacterName = updatedCharacter.Name;
                        }
                        if (relationship.ToCharacterId == characterNode.Id)
                        {
                            relationship.ToCharacterName = updatedCharacter.Name;
                        }
                    }

                    // 刷新界面
                    UpdateCharacterList();
                    DrawNetworkGraph();

                    _logger?.LogInformation("成功更新关系网络中的角色信息: {CharacterName}", updatedCharacter.Name);
                }
                else
                {
                    _logger?.LogWarning("在关系网络中未找到角色: {CharacterId}", characterId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "更新关系网络中的角色信息失败: {CharacterId}", characterId);
            }
        }

        #endregion

        #region 布局算法

        /// <summary>
        /// 应用力导向布局
        /// </summary>
        private void ApplyForceDirectedLayout()
        {
            // 简单的力导向布局算法
            var random = new Random();
            var centerX = NetworkCanvas.Width / 2;
            var centerY = NetworkCanvas.Height / 2;
            var radius = Math.Min(centerX, centerY) * 0.8;

            for (int i = 0; i < _allCharacters.Count; i++)
            {
                var angle = 2 * Math.PI * i / _allCharacters.Count;
                _allCharacters[i].X = centerX + radius * Math.Cos(angle);
                _allCharacters[i].Y = centerY + radius * Math.Sin(angle);
            }
        }

        /// <summary>
        /// 应用圆形布局
        /// </summary>
        private void ApplyCircularLayout()
        {
            var centerX = NetworkCanvas.Width / 2;
            var centerY = NetworkCanvas.Height / 2;
            var radius = Math.Min(centerX, centerY) * 0.6;

            for (int i = 0; i < _allCharacters.Count; i++)
            {
                var angle = 2 * Math.PI * i / _allCharacters.Count;
                _allCharacters[i].X = centerX + radius * Math.Cos(angle);
                _allCharacters[i].Y = centerY + radius * Math.Sin(angle);
            }
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// 角色节点视图模型
    /// </summary>
    public class CharacterNodeViewModel
    {
        /// <summary>
        /// 用于界面展示的角色编号。
        /// </summary>
        public int Id { get; set; } // 显示用的ID

        /// <summary>
        /// 角色的真实数据库标识。
        /// </summary>
        public Guid CharacterId { get; set; } // 真实的数据库ID

        /// <summary>
        /// 角色名称。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 角色类型。
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 所属势力。
        /// </summary>
        public string Faction { get; set; } = string.Empty;

        /// <summary>
        /// 名称首字母。
        /// </summary>
        public string InitialLetter { get; set; } = string.Empty;

        /// <summary>
        /// 节点连接强度对应的颜色。
        /// </summary>
        public SolidColorBrush ConnectionStrengthColor { get; set; } = new SolidColorBrush(Colors.Gray);

        /// <summary>
        /// 连接数量。
        /// </summary>
        public int ConnectionCount { get; set; }

        /// <summary>
        /// 关系数量。
        /// </summary>
        public int RelationshipCount { get; set; }

        /// <summary>
        /// 节点横坐标。
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// 节点纵坐标。
        /// </summary>
        public double Y { get; set; }
    }

    /// <summary>
    /// 关系视图模型
    /// </summary>
    public class RelationshipViewModel
    {
        /// <summary>
        /// 关系实体标识。
        /// </summary>
        public Guid RelationshipId { get; set; }

        /// <summary>
        /// 用于界面展示的关系编号。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 源角色真实标识。
        /// </summary>
        public Guid SourceCharacterGuid { get; set; }

        /// <summary>
        /// 目标角色真实标识。
        /// </summary>
        public Guid TargetCharacterGuid { get; set; }

        /// <summary>
        /// 源角色显示编号。
        /// </summary>
        public int FromCharacterId { get; set; }

        /// <summary>
        /// 目标角色显示编号。
        /// </summary>
        public int ToCharacterId { get; set; }

        /// <summary>
        /// 源角色名称。
        /// </summary>
        public string FromCharacterName { get; set; } = string.Empty;

        /// <summary>
        /// 目标角色名称。
        /// </summary>
        public string ToCharacterName { get; set; } = string.Empty;

        /// <summary>
        /// 关系类型。
        /// </summary>
        public string RelationshipType { get; set; } = string.Empty;

        /// <summary>
        /// 关系强度。
        /// </summary>
        public int Strength { get; set; }

        /// <summary>
        /// 关系描述。
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 关系状态。
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 连线显示颜色。
        /// </summary>
        public SolidColorBrush Color { get; set; } = new SolidColorBrush(Colors.Gray);
    }

    /// <summary>
    /// 智能检测到的关系候选。
    /// </summary>
    public sealed class DetectedRelationshipCandidate
    {
        /// <summary>
        /// 源角色标识。
        /// </summary>
        public Guid SourceCharacterId { get; init; }

        /// <summary>
        /// 目标角色标识。
        /// </summary>
        public Guid TargetCharacterId { get; init; }

        /// <summary>
        /// 源角色名称。
        /// </summary>
        public string SourceCharacterName { get; init; } = string.Empty;

        /// <summary>
        /// 目标角色名称。
        /// </summary>
        public string TargetCharacterName { get; init; } = string.Empty;

        /// <summary>
        /// 检测到的关系类型。
        /// </summary>
        public string RelationshipType { get; init; } = string.Empty;

        /// <summary>
        /// 关系强度。
        /// </summary>
        public int Strength { get; init; }

        /// <summary>
        /// 关系状态。
        /// </summary>
        public string Status { get; init; } = string.Empty;

        /// <summary>
        /// 候选关系描述。
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// 是否选中该候选关系。
        /// </summary>
        public bool IsSelected { get; set; } = true;
    }

    /// <summary>
    /// 简单的命令实现
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        /// <summary>
        /// 初始化泛型命令。
        /// </summary>
        /// <param name="execute">执行逻辑。</param>
        /// <param name="canExecute">是否允许执行的判断逻辑。</param>
        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// 可执行状态变化事件。
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// 判断当前命令是否可以执行。
        /// </summary>
        /// <param name="parameter">命令参数。</param>
        /// <returns>可以执行时返回 <see langword="true"/>。</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="parameter">命令参数。</param>
        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
    }

    #endregion

    #region 对话框类（简化版，实际应该单独创建文件）

    /// <summary>
    /// 添加关系对话框
    /// </summary>
    public class AddRelationshipDialog : Window
    {
        /// <summary>
        /// 源角色标识。
        /// </summary>
        public Guid SourceCharacterGuid { get; private set; }

        /// <summary>
        /// 目标角色标识。
        /// </summary>
        public Guid TargetCharacterGuid { get; private set; }

        /// <summary>
        /// 源角色名称。
        /// </summary>
        public string FromCharacterName { get; private set; } = string.Empty;

        /// <summary>
        /// 目标角色名称。
        /// </summary>
        public string ToCharacterName { get; private set; } = string.Empty;

        /// <summary>
        /// 关系类型。
        /// </summary>
        public string RelationshipType { get; private set; } = "朋友关系";

        /// <summary>
        /// 关系强度。
        /// </summary>
        public int Strength { get; private set; } = 50;

        /// <summary>
        /// 关系描述。
        /// </summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>
        /// 关系状态。
        /// </summary>
        public string Status { get; private set; } = "稳定";

        /// <summary>
        /// 初始化添加关系对话框。
        /// </summary>
        /// <param name="characters">可用于建立关系的角色列表。</param>
        public AddRelationshipDialog(List<CharacterNodeViewModel> characters)
        {
            Title = "添加关系";
            Width = 460;
            Height = 420;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var sourceComboBox = new ComboBox
            {
                ItemsSource = characters,
                DisplayMemberPath = "Name",
                SelectedIndex = characters.Count > 0 ? 0 : -1
            };
            var targetComboBox = new ComboBox
            {
                ItemsSource = characters,
                DisplayMemberPath = "Name",
                SelectedIndex = characters.Count > 1 ? 1 : (characters.Count > 0 ? 0 : -1)
            };
            var typeComboBox = new ComboBox
            {
                ItemsSource = new[] { "朋友关系", "爱情关系", "师徒关系", "敌对关系", "同门关系", "亲属关系" },
                SelectedIndex = 0
            };
            var strengthSlider = new Slider
            {
                Minimum = 10,
                Maximum = 100,
                TickFrequency = 10,
                IsSnapToTickEnabled = true,
                Value = 50
            };
            var statusComboBox = new ComboBox
            {
                ItemsSource = new[] { "稳定", "友好", "紧张", "敌对", "复杂" },
                SelectedIndex = 0
            };
            var descriptionTextBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 100
            };

            Content = RelationshipDialogFactory.CreateRelationshipDialogContent(
                sourceComboBox,
                targetComboBox,
                typeComboBox,
                strengthSlider,
                statusComboBox,
                descriptionTextBox,
                confirmAction: () =>
                {
                    if (sourceComboBox.SelectedItem is not CharacterNodeViewModel source
                        || targetComboBox.SelectedItem is not CharacterNodeViewModel target)
                    {
                        MessageBox.Show("请选择关系双方角色。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return false;
                    }

                    if (source.CharacterId == target.CharacterId)
                    {
                        MessageBox.Show("关系双方不能是同一个角色。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return false;
                    }

                    SourceCharacterGuid = source.CharacterId;
                    TargetCharacterGuid = target.CharacterId;
                    FromCharacterName = source.Name;
                    ToCharacterName = target.Name;
                    RelationshipType = typeComboBox.SelectedItem?.ToString() ?? "朋友关系";
                    Strength = (int)strengthSlider.Value;
                    Status = statusComboBox.SelectedItem?.ToString() ?? "稳定";
                    Description = descriptionTextBox.Text.Trim();
                    return true;
                });
        }
    }

    /// <summary>
    /// 编辑关系对话框
    /// </summary>
    public class EditRelationshipDialog : Window
    {
        /// <summary>
        /// 编辑后的关系类型。
        /// </summary>
        public string RelationshipType { get; private set; } = string.Empty;

        /// <summary>
        /// 编辑后的关系强度。
        /// </summary>
        public int Strength { get; private set; }

        /// <summary>
        /// 编辑后的关系描述。
        /// </summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>
        /// 编辑后的关系状态。
        /// </summary>
        public string Status { get; private set; } = string.Empty;

        /// <summary>
        /// 初始化编辑关系对话框。
        /// </summary>
        /// <param name="relationship">待编辑的关系。</param>
        public EditRelationshipDialog(RelationshipViewModel relationship)
        {
            Title = "编辑关系";
            Width = 460;
            Height = 380;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var sourceComboBox = new ComboBox
            {
                ItemsSource = new[] { relationship.FromCharacterName },
                SelectedIndex = 0,
                IsEnabled = false
            };
            var targetComboBox = new ComboBox
            {
                ItemsSource = new[] { relationship.ToCharacterName },
                SelectedIndex = 0,
                IsEnabled = false
            };
            var typeComboBox = new ComboBox
            {
                ItemsSource = new[] { "朋友关系", "爱情关系", "师徒关系", "敌对关系", "同门关系", "亲属关系" },
                SelectedItem = relationship.RelationshipType
            };
            var strengthSlider = new Slider
            {
                Minimum = 10,
                Maximum = 100,
                TickFrequency = 10,
                IsSnapToTickEnabled = true,
                Value = relationship.Strength
            };
            var statusComboBox = new ComboBox
            {
                ItemsSource = new[] { "稳定", "友好", "紧张", "敌对", "复杂" },
                SelectedItem = relationship.Status
            };
            var descriptionTextBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 100,
                Text = relationship.Description
            };

            Content = RelationshipDialogFactory.CreateRelationshipDialogContent(
                sourceComboBox,
                targetComboBox,
                typeComboBox,
                strengthSlider,
                statusComboBox,
                descriptionTextBox,
                confirmAction: () =>
                {
                    RelationshipType = typeComboBox.SelectedItem?.ToString() ?? relationship.RelationshipType;
                    Strength = (int)strengthSlider.Value;
                    Status = statusComboBox.SelectedItem?.ToString() ?? relationship.Status;
                    Description = descriptionTextBox.Text.Trim();
                    return true;
                });
        }
    }

    /// <summary>
    /// 智能关系检测结果确认对话框。
    /// </summary>
    public class RelationshipDetectionResultDialog : Window
    {
        private readonly List<DetectedRelationshipCandidate> _candidates;
        private readonly List<CheckBox> _checkBoxes = new();

        /// <summary>
        /// 初始化关系检测结果确认对话框。
        /// </summary>
        /// <param name="candidates">待确认的候选关系列表。</param>
        public RelationshipDetectionResultDialog(List<DetectedRelationshipCandidate> candidates)
        {
            _candidates = candidates;
            Title = "智能关系检测结果";
            Width = 680;
            Height = 520;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock
            {
                Text = $"检测到 {candidates.Count} 条可写入候选关系，请确认需要保存的项：",
                Margin = new Thickness(0, 0, 0, 12),
                FontWeight = FontWeights.Medium
            });

            var listPanel = new StackPanel();
            foreach (var candidate in candidates)
            {
                var checkBox = new CheckBox
                {
                    IsChecked = true,
                    Margin = new Thickness(0, 0, 0, 12),
                    Content = new TextBlock
                    {
                        Text = BuildCandidateText(candidate),
                        TextWrapping = TextWrapping.Wrap,
                        LineHeight = 18
                    }
                };
                _checkBoxes.Add(checkBox);
                listPanel.Children.Add(checkBox);
            }

            panel.Children.Add(new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = listPanel
            });

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            var okButton = new Button
            {
                Content = "写入选中关系",
                Width = 110,
                Margin = new Thickness(0, 0, 8, 0),
                IsDefault = true
            };
            okButton.Click += (_, _) => DialogResult = true;

            var cancelButton = new Button
            {
                Content = "取消",
                Width = 84,
                IsCancel = true
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            panel.Children.Add(buttonPanel);
            Content = panel;
        }

        /// <summary>
        /// 获取当前勾选的候选关系。
        /// </summary>
        /// <returns>用户选择写入的候选关系列表。</returns>
        public List<DetectedRelationshipCandidate> GetSelectedCandidates()
        {
            for (var i = 0; i < _candidates.Count; i++)
            {
                _candidates[i].IsSelected = _checkBoxes[i].IsChecked == true;
            }

            return _candidates.Where(c => c.IsSelected).ToList();
        }

        private static string BuildCandidateText(DetectedRelationshipCandidate candidate)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"{candidate.SourceCharacterName} -> {candidate.TargetCharacterName}");
            builder.AppendLine($"类型：{candidate.RelationshipType}    强度：{candidate.Strength}    状态：{candidate.Status}");
            builder.Append(candidate.Description);
            return builder.ToString();
        }
    }

    internal static class RelationshipDialogFactory
    {
        public static FrameworkElement CreateRelationshipDialogContent(
            ComboBox sourceComboBox,
            ComboBox targetComboBox,
            ComboBox typeComboBox,
            Slider strengthSlider,
            ComboBox statusComboBox,
            TextBox descriptionTextBox,
            Func<bool> confirmAction)
        {
            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock { Text = "关系发起角色" });
            panel.Children.Add(sourceComboBox);
            panel.Children.Add(new TextBlock { Text = "关系目标角色", Margin = new Thickness(0, 12, 0, 0) });
            panel.Children.Add(targetComboBox);
            panel.Children.Add(new TextBlock { Text = "关系类型", Margin = new Thickness(0, 12, 0, 0) });
            panel.Children.Add(typeComboBox);
            panel.Children.Add(new TextBlock { Text = "关系强度", Margin = new Thickness(0, 12, 0, 0) });
            panel.Children.Add(strengthSlider);
            panel.Children.Add(new TextBlock { Text = "关系状态", Margin = new Thickness(0, 12, 0, 0) });
            panel.Children.Add(statusComboBox);
            panel.Children.Add(new TextBlock { Text = "关系描述", Margin = new Thickness(0, 12, 0, 0) });
            panel.Children.Add(descriptionTextBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };

            var okButton = new Button { Content = "确定", Width = 80, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            var cancelButton = new Button { Content = "取消", Width = 80, IsCancel = true };

            okButton.Click += (_, e) =>
            {
                if (confirmAction())
                {
                    if (Window.GetWindow((DependencyObject)e.Source) is Window window)
                    {
                        window.DialogResult = true;
                    }
                }
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            panel.Children.Add(buttonPanel);
            return panel;
        }
    }

    /// <summary>
    /// 网络图设置对话框
    /// </summary>
    public class NetworkSettingsDialog : Window
    {
        /// <summary>
        /// 是否显示角色标签。
        /// </summary>
        public bool ShowCharacterLabels { get; private set; }

        /// <summary>
        /// 是否弱化未选中元素。
        /// </summary>
        public bool DimUnselectedElements { get; private set; }

        /// <summary>
        /// 节点直径。
        /// </summary>
        public double NodeDiameter { get; private set; }

        /// <summary>
        /// 画布宽度。
        /// </summary>
        public double CanvasWidth { get; private set; }

        /// <summary>
        /// 画布高度。
        /// </summary>
        public double CanvasHeight { get; private set; }

        /// <summary>
        /// 初始化网络图设置对话框。
        /// </summary>
        /// <param name="showCharacterLabels">是否显示角色标签。</param>
        /// <param name="dimUnselectedElements">是否弱化未选中元素。</param>
        /// <param name="nodeDiameter">节点直径。</param>
        /// <param name="canvasWidth">画布宽度。</param>
        /// <param name="canvasHeight">画布高度。</param>
        public NetworkSettingsDialog(bool showCharacterLabels, bool dimUnselectedElements, double nodeDiameter, double canvasWidth, double canvasHeight)
        {
            Title = "网络图设置";
            Width = 420;
            Height = 340;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var showLabelsCheckBox = new CheckBox
            {
                Content = "显示角色名称标签",
                IsChecked = showCharacterLabels
            };
            var dimElementsCheckBox = new CheckBox
            {
                Content = "弱化未选中元素",
                IsChecked = dimUnselectedElements,
                Margin = new Thickness(0, 10, 0, 0)
            };
            var nodeDiameterBox = new TextBox { Text = nodeDiameter.ToString("F0") };
            var canvasWidthBox = new TextBox { Text = canvasWidth.ToString("F0") };
            var canvasHeightBox = new TextBox { Text = canvasHeight.ToString("F0") };

            var panel = new StackPanel { Margin = new Thickness(24) };
            panel.Children.Add(showLabelsCheckBox);
            panel.Children.Add(dimElementsCheckBox);
            panel.Children.Add(new TextBlock { Text = "节点直径", Margin = new Thickness(0, 16, 0, 6) });
            panel.Children.Add(nodeDiameterBox);
            panel.Children.Add(new TextBlock { Text = "画布宽度", Margin = new Thickness(0, 12, 0, 6) });
            panel.Children.Add(canvasWidthBox);
            panel.Children.Add(new TextBlock { Text = "画布高度", Margin = new Thickness(0, 12, 0, 6) });
            panel.Children.Add(canvasHeightBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var okButton = new Button
            {
                Content = "确定",
                Width = 84,
                Margin = new Thickness(0, 0, 8, 0),
                IsDefault = true
            };
            okButton.Click += (_, _) =>
            {
                if (!double.TryParse(nodeDiameterBox.Text, out var parsedNodeDiameter) ||
                    !double.TryParse(canvasWidthBox.Text, out var parsedCanvasWidth) ||
                    !double.TryParse(canvasHeightBox.Text, out var parsedCanvasHeight))
                {
                    MessageBox.Show("请输入有效的数值设置。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                ShowCharacterLabels = showLabelsCheckBox.IsChecked == true;
                DimUnselectedElements = dimElementsCheckBox.IsChecked == true;
                NodeDiameter = Math.Max(36, parsedNodeDiameter);
                CanvasWidth = Math.Max(600, parsedCanvasWidth);
                CanvasHeight = Math.Max(400, parsedCanvasHeight);
                DialogResult = true;
            };

            var cancelButton = new Button
            {
                Content = "取消",
                Width = 84,
                IsCancel = true
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            panel.Children.Add(buttonPanel);
            Content = panel;
        }
    }

    /// <summary>
    /// 网络分析对话框
    /// </summary>
    public class NetworkAnalysisDialog : Window
    {
        /// <summary>
        /// 初始化网络分析对话框。
        /// </summary>
        /// <param name="characters">网络中的角色列表。</param>
        /// <param name="relationships">网络中的关系列表。</param>
        public NetworkAnalysisDialog(List<CharacterNodeViewModel> characters, List<RelationshipViewModel> relationships)
        {
            Title = "网络分析";
            Width = 600;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var content = new StackPanel { Margin = new Thickness(20) };

            content.Children.Add(new TextBlock
            {
                Text = "关系网络分析报告",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 16)
            });

            content.Children.Add(new TextBlock
            {
                Text = $"总角色数：{characters.Count}",
                Margin = new Thickness(0, 0, 0, 8)
            });

            content.Children.Add(new TextBlock
            {
                Text = $"总关系数：{relationships.Count}",
                Margin = new Thickness(0, 0, 0, 8)
            });

            content.Children.Add(new TextBlock
            {
                Text = $"网络密度：{(double)relationships.Count / (characters.Count * (characters.Count - 1) / 2):P2}",
                Margin = new Thickness(0, 0, 0, 8)
            });

            var closeButton = new Button
            {
                Content = "关闭",
                Width = 80,
                Margin = new Thickness(0, 20, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            closeButton.Click += (s, e) => Close();
            content.Children.Add(closeButton);

            Content = content;
        }
    }

    /// <summary>
    /// 网络全屏窗口
    /// </summary>
    public class NetworkFullScreenWindow : Window
    {
        /// <summary>
        /// 初始化网络全屏查看窗口。
        /// </summary>
        /// <param name="networkImage">当前网络图像快照。</param>
        /// <param name="characterCount">角色数量。</param>
        /// <param name="relationshipCount">关系数量。</param>
        public NetworkFullScreenWindow(ImageSource networkImage, int characterCount, int relationshipCount)
        {
            Title = "关系网络 - 全屏模式";
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
            Background = Brushes.Black;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var header = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(220, 20, 20, 20)),
                Padding = new Thickness(20)
            };
            header.Child = new DockPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = $"关系网络全屏视图  角色：{characterCount}  关系：{relationshipCount}",
                        Foreground = Brushes.White,
                        FontSize = 20,
                        FontWeight = FontWeights.Medium
                    },
                    new TextBlock
                    {
                        Text = "按 ESC 退出全屏",
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Right
                    }
                }
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            var image = new Image
            {
                Source = networkImage,
                Stretch = Stretch.Uniform
            };
            var viewer = new ScrollViewer
            {
                Content = image,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = Brushes.Black
            };
            Grid.SetRow(viewer, 1);
            grid.Children.Add(viewer);

            Content = grid;

            KeyDown += (s, e) => { if (e.Key == Key.Escape) Close(); };
        }
    }

    #endregion
}
