using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MaterialDesignThemes.Wpf;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// VolumeManagementView.xaml 的交互逻辑
    /// </summary>
    public partial class VolumeManagementView : UserControl, INavigationRefreshableView, INavigationAwareView
    {
        /// <summary>
        /// 树形节点数据模型
        /// </summary>
        public class TreeNodeViewModel
        {
            /// <summary>
            /// 节点显示名称。
            /// </summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// 节点附加信息。
            /// </summary>
            public string Info { get; set; } = string.Empty;

            /// <summary>
            /// 节点图标类型。
            /// </summary>
            public PackIconKind IconKind { get; set; }

            /// <summary>
            /// 节点类型。
            /// </summary>
            public string NodeType { get; set; } = string.Empty; // "Project", "Volume", "Chapter"

            /// <summary>
            /// 节点显示编号。
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// 对应实体标识。
            /// </summary>
            public Guid? EntityGuid { get; set; }

            /// <summary>
            /// 子节点集合。
            /// </summary>
            public ObservableCollection<TreeNodeViewModel> Children { get; set; } = new();
        }

        private ObservableCollection<TreeNodeViewModel> _treeData = new();
        private readonly ProjectService? _projectService;
        private readonly VolumeService? _volumeService;
        private readonly ChapterService? _chapterService;
        private readonly ProjectContextService? _projectContextService;
        private NavigationContext? _navigationContext;
        private Guid _currentProjectId;

        /// <summary>
        /// 构造函数
        /// </summary>
        public VolumeManagementView()
        {
            try
            {
                InitializeComponent();
                var serviceProvider = App.ServiceProvider;
                _projectService = serviceProvider?.GetService<ProjectService>();
                _volumeService = serviceProvider?.GetService<VolumeService>();
                _chapterService = serviceProvider?.GetService<ChapterService>();
                _projectContextService = serviceProvider?.GetService<ProjectContextService>();
                _ = RefreshTreeDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"卷宗管理界面初始化失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region 数据加载

        /// <summary>
        /// 加载树形数据
        /// </summary>
        private async Task RefreshTreeDataAsync()
        {
            try
            {
                _treeData.Clear();

                _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;
                if (_currentProjectId == Guid.Empty || _projectService == null || _volumeService == null || _chapterService == null)
                {
                    if (VolumeTreeView != null)
                    {
                        VolumeTreeView.ItemsSource = _treeData;
                    }

                    if (DetailArea != null && DefaultCard != null)
                    {
                        DetailArea.Children.Clear();
                        DetailArea.Children.Add(DefaultCard);
                    }

                    return;
                }

                var project = await _projectService.GetProjectByIdAsync(_currentProjectId);
                if (project == null)
                {
                    if (VolumeTreeView != null)
                    {
                        VolumeTreeView.ItemsSource = _treeData;
                    }

                    return;
                }

                var volumes = (await _volumeService.GetVolumeListAsync(_currentProjectId)).ToList();
                var chapters = (await _chapterService.GetChaptersByProjectIdAsync(_currentProjectId)).ToList();

                var projectNode = new TreeNodeViewModel
                {
                    Name = project.Name,
                    Info = string.IsNullOrWhiteSpace(project.Type) ? string.Empty : $"({project.Type})",
                    IconKind = PackIconKind.Book,
                    NodeType = "Project",
                    Id = 1,
                    EntityGuid = project.Id
                };

                foreach (var volume in volumes)
                {
                    var volumeNode = CreateVolumeNode(volume, chapters.Where(c => c.VolumeId == volume.Id));
                    projectNode.Children.Add(volumeNode);
                }

                // 设置树形数据
                _treeData.Add(projectNode);
                if (VolumeTreeView != null)
                {
                    VolumeTreeView.ItemsSource = _treeData;
                }

                ApplyNavigationContext();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载卷宗数据失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private TreeNodeViewModel CreateVolumeNode(Volume volume, IEnumerable<Chapter> chapters)
        {
            var chapterList = chapters
                .OrderBy(c => c.Order)
                .ToList();

            var volumeNode = new TreeNodeViewModel
            {
                Name = volume.Title,
                Info = $"({chapterList.Count}章, {volume.WordCount:N0}字)",
                IconKind = PackIconKind.BookOpen,
                NodeType = "Volume",
                Id = volume.Order,
                EntityGuid = volume.Id
            };

            foreach (var chapter in chapterList)
            {
                volumeNode.Children.Add(new TreeNodeViewModel
                {
                    Name = $"第{chapter.Order}章：{chapter.Title}",
                    Info = $"({chapter.WordCount:N0}字)",
                    IconKind = PackIconKind.FileDocument,
                    NodeType = "Chapter",
                    Id = chapter.Order,
                    EntityGuid = chapter.Id
                });
            }

            return volumeNode;
        }

        /// <summary>
        /// 获取章节标题（模拟数据）
        /// </summary>
        private string GetChapterTitle(int volume, int chapter)
        {
            var titles = new Dictionary<int, Dictionary<int, string>>
            {
                [1] = new Dictionary<int, string>
                {
                    [1] = "诡异的面具",
                    [2] = "好友相见分外嘴贱",
                    [3] = "被选中的孩子",
                    [4] = "恶向胆边生",
                    [5] = "魔影初现"
                },
                [2] = new Dictionary<int, string>
                {
                    [16] = "宗门试炼",
                    [17] = "秘境探险",
                    [18] = "古老传承"
                },
                [3] = new Dictionary<int, string>
                {
                    [44] = "血魔宗主",
                    [45] = "天劫降临"
                }
            };

            if (titles.ContainsKey(volume) && titles[volume].ContainsKey(chapter))
            {
                return titles[volume][chapter];
            }

            return $"章节标题{chapter}";
        }

        /// <summary>
        /// 在项目切换后刷新卷宗树数据。
        /// </summary>
        /// <param name="projectId">当前项目标识。</param>
        /// <param name="projectName">当前项目名称。</param>
        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            await RefreshTreeDataAsync();
        }

        /// <summary>
        /// 在导航到当前视图时应用导航上下文。
        /// </summary>
        /// <param name="context">导航上下文。</param>
        public void OnNavigatedTo(NavigationContext context)
        {
            _navigationContext = context;
            ApplyNavigationContext();
        }

        private void ApplyNavigationContext()
        {
            if (_navigationContext == null || _treeData.Count == 0)
            {
                return;
            }

            var projectNode = _treeData[0];
            var payload = _navigationContext.Payload as VolumeNavigationPayload;
            if (payload?.ChapterId != null)
            {
                var chapterNode = FindNodeByGuid(projectNode, payload.ChapterId.Value);
                if (chapterNode != null)
                {
                    _ = ShowNodeDetailsAsync(chapterNode);
                    return;
                }
            }

            if (payload?.VolumeId != null)
            {
                var volumeNode = FindNodeByGuid(projectNode, payload.VolumeId.Value);
                if (volumeNode != null)
                {
                    _ = ShowNodeDetailsAsync(volumeNode);
                    return;
                }
            }

            if (payload?.Action == "CreateChapter")
            {
                var firstVolume = projectNode.Children.FirstOrDefault();
                if (firstVolume != null)
                {
                    _ = ShowNodeDetailsAsync(firstVolume);
                    return;
                }
            }

            if (payload?.Action == "ProjectHome" || _navigationContext.Source == "ProjectManagement.OpenProject")
            {
                _ = ShowNodeDetailsAsync(projectNode);
            }
        }

        private TreeNodeViewModel? FindNodeByGuid(TreeNodeViewModel rootNode, Guid entityGuid)
        {
            if (rootNode.EntityGuid == entityGuid)
            {
                return rootNode;
            }

            foreach (var child in rootNode.Children)
            {
                var matchedNode = FindNodeByGuid(child, entityGuid);
                if (matchedNode != null)
                {
                    return matchedNode;
                }
            }

            return null;
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 树形视图选择项变化事件
        /// </summary>
        private void VolumeTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (e?.NewValue is TreeNodeViewModel selectedNode)
                {
                    _ = ShowNodeDetailsAsync(selectedNode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"选择项目失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示节点详细信息
        /// </summary>
        private async Task ShowNodeDetailsAsync(TreeNodeViewModel node)
        {
            try
            {
                // 清空详细区域
                DetailArea?.Children.Clear();

                switch (node?.NodeType)
                {
                    case "Project":
                        ShowProjectDetails(node);
                        break;
                    case "Volume":
                        await ShowVolumeDetailsAsync(node);
                        break;
                    case "Chapter":
                        await ShowChapterDetailsAsync(node);
                        break;
                    default:
                        if (DetailArea != null && DefaultCard != null)
                        {
                            DetailArea.Children.Add(DefaultCard);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示节点详情失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示项目详细信息
        /// </summary>
        private void ShowProjectDetails(TreeNodeViewModel node)
        {
            try
            {
                // 显示默认的项目统计卡片
                if (DetailArea != null && DefaultCard != null)
                {
                    DetailArea.Children.Add(DefaultCard);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示项目详情失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示卷宗详细信息
        /// </summary>
        private async Task ShowVolumeDetailsAsync(TreeNodeViewModel node)
        {
            try
            {
                if (DetailArea == null || node == null) return;
                if (_volumeService == null || _chapterService == null || node.EntityGuid == null) return;

                var volume = await _volumeService.GetVolumeByIdAsync(node.EntityGuid.Value);
                if (volume == null) return;

                var chapters = (await _chapterService.GetChapterListAsync(volume.Id)).ToList();
                var volumeCard = CreateVolumeDetailsCard(volume, chapters);
                DetailArea.Children.Add(volumeCard);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示卷宗详情失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示章节详细信息
        /// </summary>
        private async Task ShowChapterDetailsAsync(TreeNodeViewModel node)
        {
            try
            {
                if (DetailArea == null || node == null) return;
                if (_chapterService == null || node.EntityGuid == null) return;

                var chapter = await _chapterService.GetChapterByIdAsync(node.EntityGuid.Value);
                if (chapter == null) return;

                var chapterCard = CreateChapterDetailsCard(chapter);
                DetailArea.Children.Add(chapterCard);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示章节详情失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 视图模式切换事件
        /// </summary>
        private void ViewMode_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TreeViewToggle?.IsChecked == true)
                {
                    if (ListViewToggle != null)
                        ListViewToggle.IsChecked = false;
                    // TODO: 切换到树形视图
                }
                else if (ListViewToggle?.IsChecked == true)
                {
                    if (TreeViewToggle != null)
                        TreeViewToggle.IsChecked = false;
                    // TODO: 切换到列表视图
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"切换视图模式失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 新建卷宗按钮点击事件
        /// </summary>
        private async void NewVolume_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentProjectId == Guid.Empty || _volumeService == null)
                {
                    MessageBox.Show("请先选择有效项目后再创建卷宗。", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new NewVolumeDialog();
                dialog.Owner = Window.GetWindow(this);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                var result = dialog.ShowDialog();
                if (result == true && dialog.IsConfirmed)
                {
                    var volume = new Volume
                    {
                        ProjectId = _currentProjectId,
                        Title = dialog.VolumeData.Name,
                        Description = dialog.VolumeData.Description,
                        Type = dialog.VolumeData.Type,
                        Status = dialog.VolumeData.Status,
                        Order = dialog.VolumeData.Order,
                        EstimatedWordCount = dialog.VolumeData.EstimatedWordCount,
                        Tags = dialog.VolumeData.Tags,
                        Notes = BuildVolumeNotes(dialog.VolumeData)
                    };

                    var createdVolume = await _volumeService.CreateVolumeAsync(volume);
                    await RefreshTreeDataAsync();

                    var createdNode = _treeData.FirstOrDefault()?.Children
                        .FirstOrDefault(v => v.EntityGuid == createdVolume.Id);
                    if (createdNode != null)
                    {
                        VolumeTreeView.SelectedItemChanged -= VolumeTreeView_SelectedItemChanged;
                        VolumeTreeView.UpdateLayout();
                        VolumeTreeView.SelectedItemChanged += VolumeTreeView_SelectedItemChanged;
                        await ShowNodeDetailsAsync(createdNode);
                    }

                    MessageBox.Show($"卷宗 '{createdVolume.Title}' 创建成功！", "成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"新建卷宗操作失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 新建章节按钮点击事件
        /// </summary>
        private async void NewChapter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentProjectId == Guid.Empty || _chapterService == null)
                {
                    MessageBox.Show("请先选择有效项目后再创建章节。", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 获取当前选中的节点
                var selectedNode = VolumeTreeView.SelectedItem as TreeNodeViewModel;
                string selectedVolume = null;

                if (selectedNode?.NodeType == "Volume")
                {
                    selectedVolume = selectedNode.Name;
                }
                else if (selectedNode?.NodeType == "Chapter")
                {
                    selectedVolume = FindParentVolumeNode(selectedNode)?.Name;
                }

                var dialog = new NewChapterDialog(selectedVolume);
                dialog.Owner = Window.GetWindow(this);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                var result = dialog.ShowDialog();
                if (result == true && dialog.IsConfirmed)
                {
                    var targetVolume = selectedNode?.NodeType == "Volume"
                        ? selectedNode
                        : selectedNode?.NodeType == "Chapter"
                            ? FindParentVolumeNode(selectedNode)
                            : FindVolumeNodeByDialogSelection(dialog.ChapterData.Volume);

                    if (targetVolume?.EntityGuid != null)
                    {
                        var chapter = new Chapter
                        {
                            VolumeId = targetVolume.EntityGuid.Value,
                            Title = dialog.ChapterData.Title,
                            Order = dialog.ChapterData.Number,
                            Type = dialog.ChapterData.Type,
                            Summary = dialog.ChapterData.Summary,
                            Status = dialog.ChapterData.Status,
                            Tags = dialog.ChapterData.Tags,
                            Notes = BuildChapterNotes(dialog.ChapterData),
                            Importance = dialog.ChapterData.ImportanceLevel,
                            DifficultyLevel = dialog.ChapterData.DifficultyLevel,
                            Content = dialog.ChapterData.Summary
                        };

                        var createdChapter = await _chapterService.CreateChapterAsync(chapter);
                        await RefreshTreeDataAsync();

                        var targetNode = _treeData
                            .SelectMany(p => p.Children)
                            .SelectMany(v => v.Children)
                            .FirstOrDefault(c => c.EntityGuid == createdChapter.Id);
                        if (targetNode != null)
                        {
                            await ShowNodeDetailsAsync(targetNode);
                        }

                        MessageBox.Show($"章节 '{createdChapter.Title}' 创建成功！", "成功",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("未找到对应的卷宗", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"新建章节操作失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出卷宗按钮点击事件
        /// </summary>
        private void ExportVolume_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedNode = VolumeTreeView.SelectedItem as TreeNodeViewModel;

                if (selectedNode == null)
                {
                    MessageBox.Show("请先选择要导出的项目、卷宗或章节", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow == null)
                {
                    MessageBox.Show("无法获取主窗口，无法打开导入导出页面。", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var payload = selectedNode.NodeType switch
                {
                    "Project" => new ImportExportNavigationPayload
                    {
                        Action = "EntireProjectExport"
                    },
                    "Volume" => new ImportExportNavigationPayload
                    {
                        Action = "SelectedVolumeExport",
                        VolumeId = selectedNode.EntityGuid
                    },
                    "Chapter" => new ImportExportNavigationPayload
                    {
                        Action = "SelectedChapterExport",
                        ChapterId = selectedNode.EntityGuid
                    },
                    _ => null
                };

                if (payload == null)
                {
                    MessageBox.Show("当前所选内容不支持导出。", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                mainWindow.NavigateTo(NavigationTarget.ImportExport, new NavigationContext
                {
                    ProjectId = _projectContextService?.CurrentProjectId,
                    Source = "VolumeManagement.ExportCurrent",
                    Payload = payload
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出卷宗操作失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 详情界面创建

        /// <summary>
        /// 创建卷宗详情卡片
        /// </summary>
        private Card CreateVolumeDetailsCard(Volume volume, IEnumerable<Chapter> chapters)
        {
            var card = new Card
            {
                Margin = new Thickness(0, 0, 0, 16)
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            // 标题
            var titleBlock = new TextBlock
            {
                Text = volume.Title,
                Style = (Style)FindResource("MaterialDesignHeadline5TextBlock"),
                Margin = new Thickness(0, 0, 0, 16)
            };
            stackPanel.Children.Add(titleBlock);

            stackPanel.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(volume.Description) ? "暂无卷宗描述" : volume.Description,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16)
            });

            var actionPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, 16)
            };

            var editVolumeButton = new Button
            {
                Content = "编辑卷宗",
                Style = (Style)FindResource("MaterialDesignRaisedButton"),
                Margin = new Thickness(0, 0, 8, 0)
            };
            editVolumeButton.Click += async (_, _) => await EditVolumeAsync(volume);

            var deleteVolumeButton = new Button
            {
                Content = "删除卷宗",
                Style = (Style)FindResource("MaterialDesignOutlinedButton"),
                Margin = new Thickness(0, 0, 8, 0)
            };
            deleteVolumeButton.Click += async (_, _) => await DeleteVolumeAsync(volume);

            var exportContentButton = new Button
            {
                Content = "直接导出正文",
                Style = (Style)FindResource("MaterialDesignOutlinedButton")
            };
            exportContentButton.Click += async (_, _) => await ExportVolumeContentAsync(volume);

            actionPanel.Children.Add(editVolumeButton);
            actionPanel.Children.Add(deleteVolumeButton);
            actionPanel.Children.Add(exportContentButton);
            stackPanel.Children.Add(actionPanel);

            // 基本信息
            var infoGrid = new Grid();
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.RowDefinitions.Add(new RowDefinition());
            infoGrid.RowDefinitions.Add(new RowDefinition());
            infoGrid.RowDefinitions.Add(new RowDefinition());

            // 章节数量
            var chapterCountLabel = new TextBlock { Text = "章节数量:", Style = (Style)FindResource("MaterialDesignBody1TextBlock") };
            var chapterList = chapters.OrderBy(c => c.Order).ToList();
            var chapterCountValue = new TextBlock { Text = $"{chapterList.Count} 章", Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock") };
            Grid.SetRow(chapterCountLabel, 0);
            Grid.SetColumn(chapterCountLabel, 0);
            Grid.SetRow(chapterCountValue, 0);
            Grid.SetColumn(chapterCountValue, 1);
            infoGrid.Children.Add(chapterCountLabel);
            infoGrid.Children.Add(chapterCountValue);

            // 字数统计
            var wordCountLabel = new TextBlock { Text = "预估字数:", Style = (Style)FindResource("MaterialDesignBody1TextBlock") };
            var actualWords = chapterList.Sum(c => c.WordCount);
            var targetWords = volume.EstimatedWordCount ?? 0;
            var wordCountText = targetWords > 0 ? $"{actualWords:N0} / {targetWords:N0} 字" : $"{actualWords:N0} 字";
            var wordCountValue = new TextBlock { Text = wordCountText, Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock") };
            Grid.SetRow(wordCountLabel, 1);
            Grid.SetColumn(wordCountLabel, 0);
            Grid.SetRow(wordCountValue, 1);
            Grid.SetColumn(wordCountValue, 1);
            infoGrid.Children.Add(wordCountLabel);
            infoGrid.Children.Add(wordCountValue);

            // 状态
            var statusLabel = new TextBlock { Text = "状态:", Style = (Style)FindResource("MaterialDesignBody1TextBlock") };
            var statusValue = new TextBlock { Text = volume.Status, Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock") };
            Grid.SetRow(statusLabel, 2);
            Grid.SetColumn(statusLabel, 0);
            Grid.SetRow(statusValue, 2);
            Grid.SetColumn(statusValue, 1);
            infoGrid.Children.Add(statusLabel);
            infoGrid.Children.Add(statusValue);

            stackPanel.Children.Add(infoGrid);

            // 章节列表
            var chaptersLabel = new TextBlock
            {
                Text = "章节列表",
                Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock"),
                Margin = new Thickness(0, 20, 0, 10)
            };
            stackPanel.Children.Add(chaptersLabel);

            var chaptersListBox = new ListBox
            {
                MaxHeight = 300
            };

            foreach (var chapter in chapterList)
            {
                var chapterItem = new ListBoxItem
                {
                    Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new PackIcon { Kind = PackIconKind.FileDocument, Width = 16, Height = 16, Margin = new Thickness(0, 0, 8, 0) },
                            new TextBlock { Text = $"第{chapter.Order}章：{chapter.Title}", VerticalAlignment = VerticalAlignment.Center }
                        }
                    },
                    Tag = chapter.Id
                };
                chapterItem.Selected += async (_, _) =>
                {
                    var chapterNode = FindNodeByGuid(_treeData.First(), chapter.Id);
                    if (chapterNode != null)
                    {
                        await ShowNodeDetailsAsync(chapterNode);
                    }
                };
                chaptersListBox.Items.Add(chapterItem);
            }

            stackPanel.Children.Add(chaptersListBox);

            card.Content = stackPanel;
            return card;
        }

        /// <summary>
        /// 创建章节详情卡片
        /// </summary>
        private Card CreateChapterDetailsCard(Chapter chapter)
        {
            var card = new Card
            {
                Margin = new Thickness(0, 0, 0, 16)
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            // 标题
            var titleBlock = new TextBlock
            {
                Text = $"第{chapter.Order}章：{chapter.Title}",
                Style = (Style)FindResource("MaterialDesignHeadline5TextBlock"),
                Margin = new Thickness(0, 0, 0, 16)
            };
            stackPanel.Children.Add(titleBlock);

            // 基本信息
            var infoGrid = new Grid();
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.RowDefinitions.Add(new RowDefinition());
            infoGrid.RowDefinitions.Add(new RowDefinition());
            infoGrid.RowDefinitions.Add(new RowDefinition());
            infoGrid.RowDefinitions.Add(new RowDefinition());

            // 章节编号
            var chapterNumLabel = new TextBlock { Text = "章节编号:", Style = (Style)FindResource("MaterialDesignBody1TextBlock") };
            var chapterNumValue = new TextBlock { Text = $"第 {chapter.Order} 章", Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock") };
            Grid.SetRow(chapterNumLabel, 0);
            Grid.SetColumn(chapterNumLabel, 0);
            Grid.SetRow(chapterNumValue, 0);
            Grid.SetColumn(chapterNumValue, 1);
            infoGrid.Children.Add(chapterNumLabel);
            infoGrid.Children.Add(chapterNumValue);

            // 字数
            var wordCountLabel = new TextBlock { Text = "字数:", Style = (Style)FindResource("MaterialDesignBody1TextBlock") };
            var wordCountValue = new TextBlock { Text = $"{chapter.WordCount:N0} 字", Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock") };
            Grid.SetRow(wordCountLabel, 1);
            Grid.SetColumn(wordCountLabel, 0);
            Grid.SetRow(wordCountValue, 1);
            Grid.SetColumn(wordCountValue, 1);
            infoGrid.Children.Add(wordCountLabel);
            infoGrid.Children.Add(wordCountValue);

            // 状态
            var statusLabel = new TextBlock { Text = "状态:", Style = (Style)FindResource("MaterialDesignBody1TextBlock") };
            var statusValue = new TextBlock { Text = chapter.Status, Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock") };
            Grid.SetRow(statusLabel, 2);
            Grid.SetColumn(statusLabel, 0);
            Grid.SetRow(statusValue, 2);
            Grid.SetColumn(statusValue, 1);
            infoGrid.Children.Add(statusLabel);
            infoGrid.Children.Add(statusValue);

            // 最后修改时间
            var modifiedLabel = new TextBlock { Text = "最后修改:", Style = (Style)FindResource("MaterialDesignBody1TextBlock") };
            var modifiedValue = new TextBlock { Text = (chapter.LastEditedAt ?? chapter.UpdatedAt).ToLocalTime().ToString("yyyy-MM-dd HH:mm"), Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock") };
            Grid.SetRow(modifiedLabel, 3);
            Grid.SetColumn(modifiedLabel, 0);
            Grid.SetRow(modifiedValue, 3);
            Grid.SetColumn(modifiedValue, 1);
            infoGrid.Children.Add(modifiedLabel);
            infoGrid.Children.Add(modifiedValue);

            stackPanel.Children.Add(infoGrid);

            // 章节摘要
            var summaryLabel = new TextBlock
            {
                Text = "章节摘要",
                Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock"),
                Margin = new Thickness(0, 20, 0, 10)
            };
            stackPanel.Children.Add(summaryLabel);

            var summaryText = new TextBox
            {
                Text = GetChapterSummary(chapter),
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 100,
                Style = (Style)FindResource("MaterialDesignOutlinedTextBox")
            };
            stackPanel.Children.Add(summaryText);

            // 操作按钮
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var editButton = new Button
            {
                Content = "编辑章节",
                Style = (Style)FindResource("MaterialDesignRaisedButton"),
                Margin = new Thickness(0, 0, 8, 0)
            };
            editButton.Click += async (s, e) => await EditChapterAsync(chapter);

            var previewButton = new Button
            {
                Content = "预览",
                Style = (Style)FindResource("MaterialDesignOutlinedButton")
            };
            previewButton.Click += (s, e) => PreviewChapter(chapter);

            var deleteButton = new Button
            {
                Content = "删除章节",
                Style = (Style)FindResource("MaterialDesignOutlinedButton"),
                Margin = new Thickness(8, 0, 0, 0)
            };
            deleteButton.Click += async (s, e) => await DeleteChapterAsync(chapter);

            buttonPanel.Children.Add(editButton);
            buttonPanel.Children.Add(previewButton);
            buttonPanel.Children.Add(deleteButton);
            stackPanel.Children.Add(buttonPanel);

            card.Content = stackPanel;
            return card;
        }

        /// <summary>
        /// 获取章节摘要（模拟数据）
        /// </summary>
        private string GetChapterSummary(Chapter chapter)
        {
            if (!string.IsNullOrWhiteSpace(chapter.Summary))
            {
                return chapter.Summary;
            }

            if (!string.IsNullOrWhiteSpace(chapter.Content))
            {
                var content = chapter.Content.Trim();
                return content.Length > 180 ? content[..180] + "..." : content;
            }

            return "这是一个精彩的章节，详细内容请点击编辑查看。";
        }

        #endregion

        #region 章节操作方法

        /// <summary>
        /// 编辑章节
        /// </summary>
        private async Task EditChapterAsync(Chapter chapter)
        {
            try
            {
                if (_chapterService == null)
                {
                    MessageBox.Show("章节服务未初始化", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var latestChapter = await _chapterService.GetChapterByIdAsync(chapter.Id);
                if (latestChapter == null)
                {
                    MessageBox.Show("未找到要编辑的章节", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var editorDialog = new ChapterEditorDialog(latestChapter);
                editorDialog.Owner = Window.GetWindow(this);
                editorDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                var result = editorDialog.ShowDialog();
                if (result == true && editorDialog.IsSaved)
                {
                    await RefreshTreeDataAsync();
                    var chapterNode = FindNodeByGuid(_treeData.First(), latestChapter.Id);
                    if (chapterNode != null)
                    {
                        await ShowNodeDetailsAsync(chapterNode);
                    }

                    MessageBox.Show("章节编辑完成！", "成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"编辑章节失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteChapterAsync(Chapter chapter)
        {
            try
            {
                if (_chapterService == null)
                {
                    MessageBox.Show("章节服务未初始化", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show($"确定删除章节“第{chapter.Order}章：{chapter.Title}”吗？",
                    "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                var deleted = await _chapterService.DeleteChapterAsync(chapter.Id);
                if (!deleted)
                {
                    MessageBox.Show("章节删除失败或章节不存在。", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await RefreshTreeDataAsync();
                ShowProjectDetails(_treeData.First());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除章节失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 预览章节
        /// </summary>
        private void PreviewChapter(Chapter chapter)
        {
            try
            {
                var chapterData = new ChapterEditData
                {
                    Id = chapter.Id,
                    Title = chapter.Title,
                    Content = chapter.Content ?? string.Empty,
                    Summary = GetChapterSummary(chapter),
                    Status = chapter.Status,
                    ImportanceLevel = chapter.Importance ?? 2,
                    Characters = "待补充",
                    Tags = chapter.Tags ?? string.Empty,
                    Notes = chapter.Notes ?? string.Empty,
                    TargetWordCount = Math.Max(chapter.WordCount, 2800)
                };

                var previewDialog = new ChapterPreviewDialog(chapterData);
                previewDialog.Owner = Window.GetWindow(this);
                previewDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                previewDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"预览章节失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task EditVolumeAsync(Volume volume)
        {
            try
            {
                if (_volumeService == null)
                {
                    MessageBox.Show("卷宗服务未初始化", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var latestVolume = await _volumeService.GetVolumeByIdAsync(volume.Id);
                if (latestVolume == null)
                {
                    MessageBox.Show("未找到要编辑的卷宗。", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dialog = new NewVolumeDialog(new VolumeData
                {
                    Name = latestVolume.Title,
                    Description = latestVolume.Description ?? string.Empty,
                    Type = latestVolume.Type ?? string.Empty,
                    Order = latestVolume.Order,
                    EstimatedWordCount = latestVolume.EstimatedWordCount,
                    Status = latestVolume.Status,
                    Tags = latestVolume.Tags ?? string.Empty
                });
                dialog.Owner = Window.GetWindow(this);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (dialog.ShowDialog() != true || !dialog.IsConfirmed)
                {
                    return;
                }

                latestVolume.Title = dialog.VolumeData.Name;
                latestVolume.Description = dialog.VolumeData.Description;
                latestVolume.Type = dialog.VolumeData.Type;
                latestVolume.Order = dialog.VolumeData.Order;
                latestVolume.EstimatedWordCount = dialog.VolumeData.EstimatedWordCount;
                latestVolume.Status = dialog.VolumeData.Status;
                latestVolume.Tags = dialog.VolumeData.Tags;
                latestVolume.Notes = BuildVolumeNotes(dialog.VolumeData);

                await _volumeService.UpdateVolumeAsync(latestVolume);
                await RefreshTreeDataAsync();
                var volumeNode = FindNodeByGuid(_treeData.First(), latestVolume.Id);
                if (volumeNode != null)
                {
                    await ShowNodeDetailsAsync(volumeNode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"编辑卷宗失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteVolumeAsync(Volume volume)
        {
            try
            {
                if (_volumeService == null)
                {
                    MessageBox.Show("卷宗服务未初始化", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show($"确定删除卷宗“{volume.Title}”吗？其下章节也会一并移除。",
                    "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                var deleted = await _volumeService.DeleteVolumeAsync(volume.Id);
                if (!deleted)
                {
                    MessageBox.Show("卷宗删除失败或卷宗不存在。", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await RefreshTreeDataAsync();
                ShowProjectDetails(_treeData.First());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除卷宗失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExportVolumeContentAsync(Volume volume)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出卷宗正文",
                    Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                    FileName = $"{volume.Title}.txt"
                };

                if (saveDialog.ShowDialog() != true)
                {
                    return;
                }

                await ExportVolumeToFileAsync(volume.Id, saveDialog.FileName);
                MessageBox.Show($"卷宗正文已导出到：{saveDialog.FileName}", "导出成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出卷宗正文失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private TreeNodeViewModel? FindParentVolumeNode(TreeNodeViewModel chapterNode)
        {
            var projectNode = _treeData.FirstOrDefault();
            if (projectNode == null)
            {
                return null;
            }

            return projectNode.Children.FirstOrDefault(v => v.Children.Any(c => c.EntityGuid == chapterNode.EntityGuid));
        }

        private TreeNodeViewModel? FindVolumeNodeByDialogSelection(string? volumeSelection)
        {
            var projectNode = _treeData.FirstOrDefault();
            if (projectNode == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(volumeSelection))
            {
                return projectNode.Children.FirstOrDefault();
            }

            return projectNode.Children.FirstOrDefault(v =>
                volumeSelection.Contains(v.Name, StringComparison.OrdinalIgnoreCase) ||
                v.Name.Contains(volumeSelection, StringComparison.OrdinalIgnoreCase));
        }

        private static string BuildVolumeNotes(VolumeData data)
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(data.Theme))
            {
                builder.AppendLine($"主题：{data.Theme}");
            }
            if (!string.IsNullOrWhiteSpace(data.KeyCharacters))
            {
                builder.AppendLine($"关键角色：{data.KeyCharacters}");
            }
            if (!string.IsNullOrWhiteSpace(data.ImportantEvents))
            {
                builder.AppendLine($"重要事件：{data.ImportantEvents}");
            }

            return string.IsNullOrWhiteSpace(data.Tags) && builder.Length == 0
                ? string.Empty
                : builder.ToString().Trim();
        }

        private static string BuildChapterNotes(ChapterData data)
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(data.RelatedCharacters))
            {
                builder.AppendLine($"相关角色：{data.RelatedCharacters}");
            }
            if (!string.IsNullOrWhiteSpace(data.KeyEvents))
            {
                builder.AppendLine($"关键事件：{data.KeyEvents}");
            }
            if (!string.IsNullOrWhiteSpace(data.Notes))
            {
                builder.AppendLine($"备注：{data.Notes}");
            }

            return builder.ToString().Trim();
        }

        /// <summary>
        /// 导出卷宗到文件
        /// </summary>
        private async Task ExportVolumeToFileAsync(Guid volumeId, string filePath)
        {
            try
            {
                if (_volumeService == null || _chapterService == null)
                {
                    throw new InvalidOperationException("卷宗服务或章节服务未初始化。");
                }

                var volume = await _volumeService.GetVolumeByIdAsync(volumeId);
                if (volume == null)
                {
                    throw new InvalidOperationException("未找到要导出的卷宗。");
                }

                var chapters = (await _chapterService.GetChapterListAsync(volumeId)).OrderBy(c => c.Order).ToList();
                var content = new System.Text.StringBuilder();

                // 添加卷宗标题
                content.AppendLine(volume.Title);
                content.AppendLine(new string('=', volume.Title.Length));
                content.AppendLine();

                // 添加卷宗信息
                content.AppendLine($"章节数量：{chapters.Count}");
                content.AppendLine($"当前字数：{chapters.Sum(c => c.WordCount):N0}字");
                content.AppendLine($"导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                content.AppendLine();
                content.AppendLine(new string('-', 50));
                content.AppendLine();

                // 添加各章节内容
                foreach (var chapter in chapters)
                {
                    var chapterTitle = $"第{chapter.Order}章：{chapter.Title}";
                    content.AppendLine(chapterTitle);
                    content.AppendLine(new string('-', chapterTitle.Length));
                    content.AppendLine();

                    // 添加章节摘要
                    content.AppendLine("【章节摘要】");
                    content.AppendLine(GetChapterSummary(chapter));
                    content.AppendLine();

                    // 添加章节内容
                    content.AppendLine("【章节内容】");
                    content.AppendLine(string.IsNullOrWhiteSpace(chapter.Content) ? "暂无正文内容" : chapter.Content);
                    content.AppendLine();
                    content.AppendLine(new string('=', 50));
                    content.AppendLine();
                }

                // 写入文件
                System.IO.File.WriteAllText(filePath, content.ToString(), System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new Exception($"导出文件失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取示例章节内容
        /// </summary>
        private string GetSampleChapterContent(int chapterId)
        {
            var contents = new Dictionary<int, string>
            {
                [1] = @"    古董店内昏暗的灯光下，林轩小心翼翼地擦拭着一个古老的面具。这个面具造型奇特，似乎蕴含着某种神秘的力量。

    当他的手指触碰到面具的瞬间，一股奇异的能量涌入体内，让他感到前所未有的震撼。

    ""这是什么？""林轩喃喃自语，眼中闪过一丝惊讶和好奇。

    面具似乎在回应他的疑问，散发出微弱的光芒...",

                [2] = @"    ""林轩！你小子终于回来了！""张伟的声音从远处传来，带着熟悉的调侃语气。

    两人相视而笑，多年的友谊让他们之间没有丝毫的陌生感。但张伟敏锐地察觉到了林轩身上的变化。

    ""你...好像有什么不一样了。""张伟皱着眉头，仔细打量着眼前的好友。

    林轩心中一紧，面具的秘密绝不能让任何人知道...",

                [45] = @"    天空中乌云密布，雷声阵阵。林轩站在山峰之上，感受着天地间涌动的恐怖威压。

    ""天劫...终于来了。""他深吸一口气，眼中闪过一丝坚定。

    这是他修炼路上最重要的一关，成功便能突破到更高境界，失败则可能魂飞魄散。

    第一道雷劫从天而降，带着毁天灭地的威势。林轩不敢大意，立即运转体内真气，准备迎接这生死考验..."
            };

            return contents.ContainsKey(chapterId) ? contents[chapterId] :
                "这是一个精彩的章节，主角在这里经历了重要的成长和挑战。详细内容请通过编辑器进行编写。";
        }

        #endregion
    }
}
