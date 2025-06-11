using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// VolumeManagementView.xaml 的交互逻辑
    /// </summary>
    public partial class VolumeManagementView : UserControl
    {
        /// <summary>
        /// 树形节点数据模型
        /// </summary>
        public class TreeNodeViewModel
        {
            public string Name { get; set; } = string.Empty;
            public string Info { get; set; } = string.Empty;
            public PackIconKind IconKind { get; set; }
            public string NodeType { get; set; } = string.Empty; // "Project", "Volume", "Chapter"
            public int Id { get; set; }
            public ObservableCollection<TreeNodeViewModel> Children { get; set; } = new();
        }

        private ObservableCollection<TreeNodeViewModel> _treeData = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        public VolumeManagementView()
        {
            try
            {
                InitializeComponent();
                LoadTreeData();
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
        private void LoadTreeData()
        {
            try
            {
                // 创建项目根节点
                var projectNode = new TreeNodeViewModel
                {
                    Name = "千面劫·宿命轮回",
                    Info = "(修仙小说)",
                    IconKind = PackIconKind.Book,
                    NodeType = "Project",
                    Id = 1
                };

            // 第一卷
            var volume1 = new TreeNodeViewModel
            {
                Name = "第一卷：面具觉醒",
                Info = "(15章, 42,000字)",
                IconKind = PackIconKind.BookOpen,
                NodeType = "Volume",
                Id = 1
            };

            // 第一卷的章节
            for (int i = 1; i <= 15; i++)
            {
                volume1.Children.Add(new TreeNodeViewModel
                {
                    Name = $"第{i}章：{GetChapterTitle(1, i)}",
                    Info = "(2,800字)",
                    IconKind = PackIconKind.FileDocument,
                    NodeType = "Chapter",
                    Id = i
                });
            }

            // 第二卷
            var volume2 = new TreeNodeViewModel
            {
                Name = "第二卷：力量觉醒",
                Info = "(18章, 50,400字)",
                IconKind = PackIconKind.BookOpen,
                NodeType = "Volume",
                Id = 2
            };

            // 第二卷的章节
            for (int i = 16; i <= 33; i++)
            {
                volume2.Children.Add(new TreeNodeViewModel
                {
                    Name = $"第{i}章：{GetChapterTitle(2, i)}",
                    Info = "(2,800字)",
                    IconKind = PackIconKind.FileDocument,
                    NodeType = "Chapter",
                    Id = i
                });
            }

            // 第三卷
            var volume3 = new TreeNodeViewModel
            {
                Name = "第三卷：天劫降临",
                Info = "(12章, 32,600字)",
                IconKind = PackIconKind.BookOpen,
                NodeType = "Volume",
                Id = 3
            };

            // 第三卷的章节
            for (int i = 34; i <= 45; i++)
            {
                volume3.Children.Add(new TreeNodeViewModel
                {
                    Name = $"第{i}章：{GetChapterTitle(3, i)}",
                    Info = "(2,800字)",
                    IconKind = PackIconKind.FileDocument,
                    NodeType = "Chapter",
                    Id = i
                });
            }

            // 添加卷宗到项目
            projectNode.Children.Add(volume1);
            projectNode.Children.Add(volume2);
            projectNode.Children.Add(volume3);

                // 设置树形数据
                _treeData.Add(projectNode);
                if (VolumeTreeView != null)
                {
                    VolumeTreeView.ItemsSource = _treeData;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载卷宗数据失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    ShowNodeDetails(selectedNode);
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
        private void ShowNodeDetails(TreeNodeViewModel node)
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
                        ShowVolumeDetails(node);
                        break;
                    case "Chapter":
                        ShowChapterDetails(node);
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
        private void ShowVolumeDetails(TreeNodeViewModel node)
        {
            try
            {
                if (DetailArea == null || node == null) return;

                // 创建卷宗详情界面
                var volumeCard = CreateVolumeDetailsCard(node);
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
        private void ShowChapterDetails(TreeNodeViewModel node)
        {
            try
            {
                if (DetailArea == null || node == null) return;

                // 创建章节详情界面
                var chapterCard = CreateChapterDetailsCard(node);
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
        private void NewVolume_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new NewVolumeDialog();
                dialog.Owner = Window.GetWindow(this);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                var result = dialog.ShowDialog();
                if (result == true && dialog.IsConfirmed)
                {
                    // 创建新的卷宗节点
                    var newVolume = new TreeNodeViewModel
                    {
                        Name = dialog.VolumeData.Name,
                        Info = $"(0章, 0字)",
                        IconKind = PackIconKind.BookOpen,
                        NodeType = "Volume",
                        Id = _treeData[0].Children.Count + 1
                    };

                    // 添加到项目节点
                    _treeData[0].Children.Add(newVolume);

                    // 刷新树形视图
                    VolumeTreeView.ItemsSource = null;
                    VolumeTreeView.ItemsSource = _treeData;

                    MessageBox.Show($"卷宗 '{dialog.VolumeData.Name}' 创建成功！", "成功",
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
        private void NewChapter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取当前选中的节点
                var selectedNode = VolumeTreeView.SelectedItem as TreeNodeViewModel;
                string selectedVolume = null;

                if (selectedNode?.NodeType == "Volume")
                {
                    selectedVolume = selectedNode.Name;
                }

                var dialog = new NewChapterDialog(selectedVolume);
                dialog.Owner = Window.GetWindow(this);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                var result = dialog.ShowDialog();
                if (result == true && dialog.IsConfirmed)
                {
                    // 找到对应的卷宗节点
                    TreeNodeViewModel targetVolume = null;
                    foreach (var volume in _treeData[0].Children)
                    {
                        if (volume.Name.Contains(dialog.ChapterData.Volume.Split('：')[0]))
                        {
                            targetVolume = volume;
                            break;
                        }
                    }

                    if (targetVolume != null)
                    {
                        // 创建新的章节节点
                        var newChapter = new TreeNodeViewModel
                        {
                            Name = $"第{dialog.ChapterData.Number}章：{dialog.ChapterData.Title}",
                            Info = "(0字)",
                            IconKind = PackIconKind.FileDocument,
                            NodeType = "Chapter",
                            Id = dialog.ChapterData.Number
                        };

                        // 添加到卷宗节点
                        targetVolume.Children.Add(newChapter);

                        // 更新卷宗信息
                        var chapterCount = targetVolume.Children.Count;
                        var estimatedWords = chapterCount * 2800;
                        targetVolume.Info = $"({chapterCount}章, {estimatedWords:N0}字)";

                        // 刷新树形视图
                        VolumeTreeView.ItemsSource = null;
                        VolumeTreeView.ItemsSource = _treeData;

                        MessageBox.Show($"章节 '{dialog.ChapterData.Title}' 创建成功！", "成功",
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

                if (selectedNode?.NodeType != "Volume")
                {
                    MessageBox.Show("请先选择要导出的卷宗", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出卷宗",
                    Filter = "文本文件 (*.txt)|*.txt|Word文档 (*.docx)|*.docx|PDF文件 (*.pdf)|*.pdf|所有文件 (*.*)|*.*",
                    FileName = $"{selectedNode.Name}.txt"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportVolumeToFile(selectedNode, saveDialog.FileName);
                    MessageBox.Show($"卷宗已导出到：{saveDialog.FileName}", "导出成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
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
        private Card CreateVolumeDetailsCard(TreeNodeViewModel node)
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
                Text = node.Name,
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

            // 章节数量
            var chapterCountLabel = new TextBlock { Text = "章节数量:", Style = (Style)FindResource("MaterialDesignBody1TextBlock") };
            var chapterCountValue = new TextBlock { Text = $"{node.Children.Count} 章", Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock") };
            Grid.SetRow(chapterCountLabel, 0);
            Grid.SetColumn(chapterCountLabel, 0);
            Grid.SetRow(chapterCountValue, 0);
            Grid.SetColumn(chapterCountValue, 1);
            infoGrid.Children.Add(chapterCountLabel);
            infoGrid.Children.Add(chapterCountValue);

            // 字数统计
            var wordCountLabel = new TextBlock { Text = "预估字数:", Style = (Style)FindResource("MaterialDesignBody1TextBlock") };
            var wordCountValue = new TextBlock { Text = $"{node.Children.Count * 2800:N0} 字", Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock") };
            Grid.SetRow(wordCountLabel, 1);
            Grid.SetColumn(wordCountLabel, 0);
            Grid.SetRow(wordCountValue, 1);
            Grid.SetColumn(wordCountValue, 1);
            infoGrid.Children.Add(wordCountLabel);
            infoGrid.Children.Add(wordCountValue);

            // 状态
            var statusLabel = new TextBlock { Text = "状态:", Style = (Style)FindResource("MaterialDesignBody1TextBlock") };
            var statusValue = new TextBlock { Text = "进行中", Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock") };
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

            foreach (var chapter in node.Children)
            {
                var chapterItem = new ListBoxItem
                {
                    Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new PackIcon { Kind = PackIconKind.FileDocument, Width = 16, Height = 16, Margin = new Thickness(0, 0, 8, 0) },
                            new TextBlock { Text = chapter.Name, VerticalAlignment = VerticalAlignment.Center }
                        }
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
        private Card CreateChapterDetailsCard(TreeNodeViewModel node)
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
                Text = node.Name,
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
            var chapterNumValue = new TextBlock { Text = $"第 {node.Id} 章", Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock") };
            Grid.SetRow(chapterNumLabel, 0);
            Grid.SetColumn(chapterNumLabel, 0);
            Grid.SetRow(chapterNumValue, 0);
            Grid.SetColumn(chapterNumValue, 1);
            infoGrid.Children.Add(chapterNumLabel);
            infoGrid.Children.Add(chapterNumValue);

            // 字数
            var wordCountLabel = new TextBlock { Text = "字数:", Style = (Style)FindResource("MaterialDesignBody1TextBlock") };
            var wordCountValue = new TextBlock { Text = "2,800 字", Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock") };
            Grid.SetRow(wordCountLabel, 1);
            Grid.SetColumn(wordCountLabel, 0);
            Grid.SetRow(wordCountValue, 1);
            Grid.SetColumn(wordCountValue, 1);
            infoGrid.Children.Add(wordCountLabel);
            infoGrid.Children.Add(wordCountValue);

            // 状态
            var statusLabel = new TextBlock { Text = "状态:", Style = (Style)FindResource("MaterialDesignBody1TextBlock") };
            var statusValue = new TextBlock { Text = "已完成", Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock") };
            Grid.SetRow(statusLabel, 2);
            Grid.SetColumn(statusLabel, 0);
            Grid.SetRow(statusValue, 2);
            Grid.SetColumn(statusValue, 1);
            infoGrid.Children.Add(statusLabel);
            infoGrid.Children.Add(statusValue);

            // 最后修改时间
            var modifiedLabel = new TextBlock { Text = "最后修改:", Style = (Style)FindResource("MaterialDesignBody1TextBlock") };
            var modifiedValue = new TextBlock { Text = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd HH:mm"), Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock") };
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
                Text = GetChapterSummary(node.Id),
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
            editButton.Click += (s, e) => EditChapter(node);

            var previewButton = new Button
            {
                Content = "预览",
                Style = (Style)FindResource("MaterialDesignOutlinedButton")
            };
            previewButton.Click += (s, e) => PreviewChapter(node);

            buttonPanel.Children.Add(editButton);
            buttonPanel.Children.Add(previewButton);
            stackPanel.Children.Add(buttonPanel);

            card.Content = stackPanel;
            return card;
        }

        /// <summary>
        /// 获取章节摘要（模拟数据）
        /// </summary>
        private string GetChapterSummary(int chapterId)
        {
            var summaries = new Dictionary<int, string>
            {
                [1] = "主角林轩在古董店中发现了一个神秘的面具，当他戴上面具后，发现自己获得了特殊的能力。这个面具似乎有着悠久的历史和神秘的力量。",
                [2] = "林轩与好友张伟重逢，两人之间的友谊依然深厚。张伟发现了林轩的变化，开始怀疑他身上发生了什么特殊的事情。",
                [3] = "林轩逐渐意识到自己被某种神秘力量选中，面具的力量开始显现，他必须学会控制这种力量，否则将面临巨大的危险。",
                [4] = "面对突如其来的危险，林轩内心的恶念开始滋生。面具的力量诱惑着他走向黑暗，他必须在善恶之间做出选择。",
                [5] = "神秘的魔影开始在城市中出现，林轩意识到这与他的面具有着密切的关系。一场关于光明与黑暗的战斗即将开始。"
            };

            return summaries.ContainsKey(chapterId) ? summaries[chapterId] : "这是一个精彩的章节，详细内容请点击编辑查看。";
        }

        #endregion

        #region 章节操作方法

        /// <summary>
        /// 编辑章节
        /// </summary>
        private void EditChapter(TreeNodeViewModel chapterNode)
        {
            try
            {
                if (chapterNode?.NodeType != "Chapter")
                {
                    MessageBox.Show("请选择要编辑的章节", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var editorDialog = new ChapterEditorDialog(chapterNode.Name);
                editorDialog.Owner = Window.GetWindow(this);
                editorDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                var result = editorDialog.ShowDialog();
                if (result == true && editorDialog.IsSaved)
                {
                    // 更新章节信息
                    chapterNode.Name = editorDialog.ChapterData.Title;
                    var wordCount = editorDialog.ChapterData.Content?.Length ?? 0;
                    chapterNode.Info = $"({wordCount:N0}字)";

                    // 刷新树形视图
                    VolumeTreeView.ItemsSource = null;
                    VolumeTreeView.ItemsSource = _treeData;

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

        /// <summary>
        /// 预览章节
        /// </summary>
        private void PreviewChapter(TreeNodeViewModel chapterNode)
        {
            try
            {
                if (chapterNode?.NodeType != "Chapter")
                {
                    MessageBox.Show("请选择要预览的章节", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 创建模拟的章节数据
                var chapterData = new ChapterEditData
                {
                    Title = chapterNode.Name,
                    Content = GetSampleChapterContent(chapterNode.Id),
                    Summary = GetChapterSummary(chapterNode.Id),
                    Status = "已完成",
                    ImportanceLevel = 2,
                    Characters = "林轩, 张伟, 神秘老者",
                    Tags = "修炼, 突破, 危机",
                    Notes = "注意描写主角的心理变化",
                    TargetWordCount = 2800
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

        /// <summary>
        /// 导出卷宗到文件
        /// </summary>
        private void ExportVolumeToFile(TreeNodeViewModel volumeNode, string filePath)
        {
            try
            {
                var content = new System.Text.StringBuilder();

                // 添加卷宗标题
                content.AppendLine(volumeNode.Name);
                content.AppendLine(new string('=', volumeNode.Name.Length));
                content.AppendLine();

                // 添加卷宗信息
                content.AppendLine($"章节数量：{volumeNode.Children.Count}");
                content.AppendLine($"预估字数：{volumeNode.Children.Count * 2800:N0}字");
                content.AppendLine($"导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                content.AppendLine();
                content.AppendLine(new string('-', 50));
                content.AppendLine();

                // 添加各章节内容
                foreach (var chapter in volumeNode.Children)
                {
                    content.AppendLine(chapter.Name);
                    content.AppendLine(new string('-', chapter.Name.Length));
                    content.AppendLine();

                    // 添加章节摘要
                    content.AppendLine("【章节摘要】");
                    content.AppendLine(GetChapterSummary(chapter.Id));
                    content.AppendLine();

                    // 添加章节内容
                    content.AppendLine("【章节内容】");
                    content.AppendLine(GetSampleChapterContent(chapter.Id));
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
