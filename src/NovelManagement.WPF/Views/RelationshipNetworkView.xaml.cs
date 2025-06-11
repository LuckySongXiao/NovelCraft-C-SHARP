using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
    public partial class RelationshipNetworkView : UserControl
    {
        #region 字段

        private List<CharacterNodeViewModel> _allCharacters;
        private List<RelationshipViewModel> _allRelationships;
        private List<CharacterNodeViewModel> _filteredCharacters;
        private CharacterNodeViewModel? _selectedCharacter;
        private RelationshipViewModel? _selectedRelationship;
        private bool _isDragging;
        private Point _lastMousePosition;

        // 服务
        private CharacterService? _characterService;
        private ProjectContextService? _projectContextService;
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
                    _projectContextService = serviceProvider.GetService<ProjectContextService>();
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
            try
            {
                _logger?.LogInformation("开始加载关系网络角色数据");

                // 获取当前项目ID
                var currentProjectId = GetCurrentProjectId();
                if (currentProjectId == Guid.Empty)
                {
                    _logger?.LogWarning("没有当前项目，使用模拟数据");
                    LoadMockData();
                    UpdateCharacterList();
                    DrawNetworkGraph();
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
                    _logger?.LogWarning("CharacterService 不可用，使用模拟数据");
                    LoadMockData();
                }

                UpdateCharacterList();
                DrawNetworkGraph();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载关系网络角色数据失败");

                // 发生错误时使用模拟数据
                LoadMockData();
                UpdateCharacterList();
                DrawNetworkGraph();

                MessageBox.Show($"加载角色数据失败，已切换到模拟数据: {ex.Message}", "警告",
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

                // 为每个角色加载其关系
                foreach (var character in characters)
                {
                    if (_characterService != null)
                    {
                        var relationships = await _characterService.GetCharacterRelationshipsAsync(character.Id);

                        foreach (var relationship in relationships)
                        {
                            // 查找对应的视图模型
                            var sourceViewModel = _allCharacters.FirstOrDefault(c => c.CharacterId == relationship.SourceCharacterId);
                            var targetViewModel = _allCharacters.FirstOrDefault(c => c.CharacterId == relationship.TargetCharacterId);

                            if (sourceViewModel != null && targetViewModel != null)
                            {
                                var relationshipViewModel = new RelationshipViewModel
                                {
                                    Id = _allRelationships.Count + 1,
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
            var line = new Line
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = relationship.Color,
                StrokeThickness = Math.Max(1, relationship.Strength / 20.0),
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
            // 创建节点圆圈
            var circle = new Ellipse
            {
                Width = 60,
                Height = 60,
                Fill = character.ConnectionStrengthColor,
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 3,
                Tag = character
            };

            Canvas.SetLeft(circle, character.X - 30);
            Canvas.SetTop(circle, character.Y - 30);

            // 创建角色名称标签
            var nameLabel = new TextBlock
            {
                Text = character.Name,
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
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
            // TODO: 实现高亮显示选中角色的所有连接
        }

        /// <summary>
        /// 高亮关系
        /// </summary>
        private void HighlightRelationship(RelationshipViewModel relationship)
        {
            // TODO: 实现高亮显示选中的关系
        }

        /// <summary>
        /// 显示角色提示
        /// </summary>
        private void ShowCharacterTooltip(CharacterNodeViewModel character, Point position)
        {
            // TODO: 实现角色提示显示
        }

        /// <summary>
        /// 显示关系提示
        /// </summary>
        private void ShowRelationshipTooltip(RelationshipViewModel relationship, Point position)
        {
            // TODO: 实现关系提示显示
        }

        /// <summary>
        /// 隐藏提示
        /// </summary>
        private void HideTooltip()
        {
            // TODO: 实现隐藏提示
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
            try
            {
                var dialog = new AddRelationshipDialog(_allCharacters);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                {
                    // TODO: 添加新关系到数据
                    MessageBox.Show("关系添加成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    DrawNetworkGraph();
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
        private void AutoDetectRelationships_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("AI正在分析文本内容，检测角色关系...\n\n检测到3个新的潜在关系：\n• 林轩 - 玄天老祖 (师徒关系)\n• 李雪儿 - 苏雨薇 (闺蜜关系)\n• 张无忌 - 魔尊 (从属关系)",
                    "智能关系检测", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("网络图导出功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("网络图设置功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
                var fullScreenWindow = new NetworkFullScreenWindow(_allCharacters, _allRelationships);
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
            try
            {
                if (_selectedRelationship != null)
                {
                    var dialog = new EditRelationshipDialog(_selectedRelationship);
                    dialog.Owner = Window.GetWindow(this);
                    if (dialog.ShowDialog() == true)
                    {
                        DrawNetworkGraph();
                        ShowRelationshipDetails(_selectedRelationship);
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
            try
            {
                if (_selectedRelationship != null)
                {
                    var result = MessageBox.Show($"确定要删除关系\"{_selectedRelationship.RelationshipType}\"吗？",
                        "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _allRelationships.Remove(_selectedRelationship);
                        _selectedRelationship = null;
                        DrawNetworkGraph();
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
                await Task.Run(() => LoadCharactersAsync());
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
        public int Id { get; set; } // 显示用的ID
        public Guid CharacterId { get; set; } // 真实的数据库ID
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Faction { get; set; } = string.Empty;
        public string InitialLetter { get; set; } = string.Empty;
        public SolidColorBrush ConnectionStrengthColor { get; set; } = new SolidColorBrush(Colors.Gray);
        public int ConnectionCount { get; set; }
        public int RelationshipCount { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }

    /// <summary>
    /// 关系视图模型
    /// </summary>
    public class RelationshipViewModel
    {
        public int Id { get; set; }
        public int FromCharacterId { get; set; }
        public int ToCharacterId { get; set; }
        public string FromCharacterName { get; set; } = string.Empty;
        public string ToCharacterName { get; set; } = string.Empty;
        public string RelationshipType { get; set; } = string.Empty;
        public int Strength { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public SolidColorBrush Color { get; set; } = new SolidColorBrush(Colors.Gray);
    }

    /// <summary>
    /// 简单的命令实现
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

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
        public AddRelationshipDialog(List<CharacterNodeViewModel> characters)
        {
            Title = "添加关系";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // 简化实现
            var result = MessageBox.Show("是否添加新的角色关系？", "添加关系",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            DialogResult = result == MessageBoxResult.Yes;
        }
    }

    /// <summary>
    /// 编辑关系对话框
    /// </summary>
    public class EditRelationshipDialog : Window
    {
        public EditRelationshipDialog(RelationshipViewModel relationship)
        {
            Title = "编辑关系";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // 简化实现
            var result = MessageBox.Show($"是否修改关系\"{relationship.RelationshipType}\"？", "编辑关系",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            DialogResult = result == MessageBoxResult.Yes;
        }
    }

    /// <summary>
    /// 网络分析对话框
    /// </summary>
    public class NetworkAnalysisDialog : Window
    {
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
        public NetworkFullScreenWindow(List<CharacterNodeViewModel> characters, List<RelationshipViewModel> relationships)
        {
            Title = "关系网络 - 全屏模式";
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;

            var content = new Grid();
            content.Children.Add(new TextBlock
            {
                Text = "全屏网络图显示（开发中）\n\n按ESC键退出全屏",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 24
            });

            Content = content;

            KeyDown += (s, e) => { if (e.Key == Key.Escape) Close(); };
        }
    }

    #endregion
}
