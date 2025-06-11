using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NovelManagement.Application.DTOs;
using NovelManagement.Application.Services;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// ImportExportView.xaml 的交互逻辑
    /// </summary>
    public partial class ImportExportView : UserControl
    {
        #region 数据模型

        /// <summary>
        /// 操作历史视图模型
        /// </summary>
        public class OperationHistoryViewModel
        {
            public Guid OperationId { get; set; }
            public string Format { get; set; } = string.Empty;
            public string FilePath { get; set; } = string.Empty;
            public long FileSize { get; set; }
            public OperationStatus Status { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public string? Notes { get; set; }
            
            public PackIconKind StatusIcon => Status switch
            {
                OperationStatus.Completed => PackIconKind.CheckCircle,
                OperationStatus.Failed => PackIconKind.AlertCircle,
                OperationStatus.Cancelled => PackIconKind.Cancel,
                OperationStatus.InProgress => PackIconKind.Loading,
                _ => PackIconKind.Clock
            };
            
            public SolidColorBrush StatusColor => Status switch
            {
                OperationStatus.Completed => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                OperationStatus.Failed => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                OperationStatus.Cancelled => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                OperationStatus.InProgress => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        #endregion

        #region 字段和属性

        private readonly ObservableCollection<OperationHistoryViewModel> _exportHistory;
        private readonly ObservableCollection<OperationHistoryViewModel> _importHistory;
        private readonly ExportService _exportService;
        private readonly ImportService _importService;
        private readonly ILogger<ImportExportView> _logger;

        private Guid? _currentExportOperationId;
        private Guid? _currentImportOperationId;
        private readonly System.Windows.Threading.DispatcherTimer _progressTimer;

        // 模拟的当前项目ID
        private readonly Guid _currentProjectId = Guid.NewGuid();

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public ImportExportView()
        {
            InitializeComponent();

            // 初始化集合
            _exportHistory = new ObservableCollection<OperationHistoryViewModel>();
            _importHistory = new ObservableCollection<OperationHistoryViewModel>();

            // 创建模拟的服务
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<ImportExportView>();
            
            // 这里应该通过依赖注入获取服务，现在创建模拟实例
            var unitOfWork = new MockUnitOfWork(); // 模拟的UnitOfWork
            var excelService = new ExcelProcessingService(loggerFactory.CreateLogger<ExcelProcessingService>());
            var wordService = new WordProcessingService(loggerFactory.CreateLogger<WordProcessingService>());
            _exportService = new ExportService(unitOfWork, loggerFactory.CreateLogger<ExportService>(), excelService, wordService);
            _importService = new ImportService(unitOfWork, loggerFactory.CreateLogger<ImportService>());

            // 设置数据绑定
            ExportHistoryListView.ItemsSource = _exportHistory;
            ImportHistoryListView.ItemsSource = _importHistory;

            // 订阅服务事件
            _exportService.ProgressUpdated += OnExportProgressUpdated;
            _importService.ProgressUpdated += OnImportProgressUpdated;

            // 创建进度更新定时器
            _progressTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _progressTimer.Tick += OnProgressTimerTick;

            // 初始化数据
            InitializeAsync();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 异步初始化
        /// </summary>
        private async void InitializeAsync()
        {
            try
            {
                // 设置默认输出路径
                var defaultExportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "小说导出");
                OutputPathTextBox.Text = Path.Combine(defaultExportPath, "千面劫·宿命轮回.txt");

                // 加载历史记录
                await LoadExportHistoryAsync();
                await LoadImportHistoryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化导入导出界面失败");
                MessageBox.Show($"初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载导出历史记录
        /// </summary>
        private async Task LoadExportHistoryAsync()
        {
            try
            {
                var history = await _exportService.GetExportHistoryAsync(_currentProjectId);
                _exportHistory.Clear();
                
                foreach (var item in history)
                {
                    _exportHistory.Add(new OperationHistoryViewModel
                    {
                        OperationId = item.OperationId,
                        Format = item.Format,
                        FilePath = item.FilePath,
                        FileSize = item.FileSize,
                        Status = item.Status,
                        StartTime = item.StartTime,
                        EndTime = item.EndTime,
                        Notes = item.Notes
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载导出历史记录失败");
            }
        }

        /// <summary>
        /// 加载导入历史记录
        /// </summary>
        private async Task LoadImportHistoryAsync()
        {
            try
            {
                var history = await _importService.GetImportHistoryAsync(_currentProjectId);
                _importHistory.Clear();
                
                foreach (var item in history)
                {
                    _importHistory.Add(new OperationHistoryViewModel
                    {
                        OperationId = item.OperationId,
                        Format = item.Format,
                        FilePath = item.FilePath,
                        FileSize = item.FileSize,
                        Status = item.Status,
                        StartTime = item.StartTime,
                        EndTime = item.EndTime,
                        Notes = item.Notes
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载导入历史记录失败");
            }
        }

        #endregion

        #region 导出相关事件

        /// <summary>
        /// 导出范围选择变化
        /// </summary>
        private void ExportScope_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ExportScopeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var scope = selectedItem.Tag?.ToString();
                SelectionPanel.Visibility = (scope == "SelectedVolumes" || scope == "SelectedChapters") 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 浏览输出路径
        /// </summary>
        private void BrowseOutputPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "选择导出文件保存位置",
                Filter = "所有支持的格式|*.txt;*.docx;*.pdf;*.epub;*.html;*.md|" +
                        "文本文件|*.txt|" +
                        "Word文档|*.docx|" +
                        "PDF文档|*.pdf|" +
                        "电子书|*.epub|" +
                        "网页文件|*.html|" +
                        "Markdown文件|*.md",
                FileName = "千面劫·宿命轮回"
            };

            if (dialog.ShowDialog() == true)
            {
                OutputPathTextBox.Text = dialog.FileName;
            }
        }

        /// <summary>
        /// 开始导出
        /// </summary>
        private async void StartExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(OutputPathTextBox.Text))
                {
                    MessageBox.Show("请选择输出文件路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (ExportFormatComboBox.SelectedItem is not ComboBoxItem formatItem ||
                    ExportScopeComboBox.SelectedItem is not ComboBoxItem scopeItem)
                {
                    MessageBox.Show("请选择导出格式和范围", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 创建导出请求
                var request = new ExportRequestDto
                {
                    ProjectId = _currentProjectId,
                    Format = Enum.Parse<ExportFormat>(formatItem.Tag.ToString()!),
                    Scope = Enum.Parse<ExportScope>(scopeItem.Tag.ToString()!),
                    OutputPath = OutputPathTextBox.Text,
                    IncludeSettings = IncludeSettingsCheckBox.IsChecked == true,
                    IncludeCharacters = IncludeCharactersCheckBox.IsChecked == true,
                    IncludeFactions = IncludeFactionsCheckBox.IsChecked == true,
                    IncludePlots = IncludePlotsCheckBox.IsChecked == true
                };

                // 如果选择了特定范围，添加选中的项目
                if (request.Scope == ExportScope.SelectedVolumes || request.Scope == ExportScope.SelectedChapters)
                {
                    var selectedItems = SelectionListBox.SelectedItems.Cast<ListBoxItem>().ToList();
                    if (selectedItems.Count == 0)
                    {
                        MessageBox.Show("请选择要导出的项目", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    // 这里应该根据实际选择的项目设置ID，现在使用模拟数据
                    for (int i = 0; i < selectedItems.Count; i++)
                    {
                        if (request.Scope == ExportScope.SelectedVolumes)
                            request.SelectedVolumeIds.Add(Guid.NewGuid());
                        else
                            request.SelectedChapterIds.Add(Guid.NewGuid());
                    }
                }

                // 开始导出
                var result = await _exportService.StartExportAsync(request);
                _currentExportOperationId = result.OperationId;

                // 显示进度面板
                ExportProgressCard.Visibility = Visibility.Visible;
                _progressTimer.Start();

                _logger.LogInformation($"开始导出操作: {result.OperationId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动导出失败");
                MessageBox.Show($"启动导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 取消导出
        /// </summary>
        private void CancelExport_Click(object sender, RoutedEventArgs e)
        {
            if (_currentExportOperationId.HasValue)
            {
                _exportService.CancelExport(_currentExportOperationId.Value);
                _logger.LogInformation($"用户取消导出操作: {_currentExportOperationId}");
            }
        }

        /// <summary>
        /// 刷新导出历史记录
        /// </summary>
        private async void RefreshExportHistory_Click(object sender, RoutedEventArgs e)
        {
            await LoadExportHistoryAsync();
        }

        /// <summary>
        /// 打开导出文件
        /// </summary>
        private void OpenExportFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is OperationHistoryViewModel history)
            {
                try
                {
                    if (File.Exists(history.FilePath))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = history.FilePath,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        MessageBox.Show("文件不存在", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"打开文件失败: {history.FilePath}");
                    MessageBox.Show($"打开文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 打开导出文件夹
        /// </summary>
        private void OpenExportFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is OperationHistoryViewModel history)
            {
                try
                {
                    var directory = Path.GetDirectoryName(history.FilePath);
                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = directory,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        MessageBox.Show("文件夹不存在", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"打开文件夹失败: {history.FilePath}");
                    MessageBox.Show($"打开文件夹失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region 导入相关事件

        /// <summary>
        /// 浏览导入文件
        /// </summary>
        private void BrowseImportFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择要导入的文件",
                Filter = "所有支持的格式|*.xlsx;*.xls;*.csv;*.txt;*.json;*.xml|" +
                        "Excel文件|*.xlsx;*.xls|" +
                        "CSV文件|*.csv|" +
                        "文本文件|*.txt|" +
                        "JSON文件|*.json|" +
                        "XML文件|*.xml",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                ImportFilePathTextBox.Text = dialog.FileName;
                
                // 根据文件扩展名自动选择格式
                var extension = Path.GetExtension(dialog.FileName).ToLower();
                var formatIndex = extension switch
                {
                    ".xlsx" or ".xls" => 0, // EXCEL
                    ".csv" => 1, // CSV
                    ".txt" => 2, // TXT
                    ".json" => 3, // JSON
                    ".xml" => 4, // XML
                    _ => 0
                };
                ImportFormatComboBox.SelectedIndex = formatIndex;
            }
        }

        /// <summary>
        /// 预览导入文件
        /// </summary>
        private async void PreviewImportFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ImportFilePathTextBox.Text))
                {
                    MessageBox.Show("请选择要预览的文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (ImportFormatComboBox.SelectedItem is not ComboBoxItem formatItem)
                {
                    MessageBox.Show("请选择文件格式", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var format = Enum.Parse<ImportFormat>(formatItem.Tag.ToString()!);
                var preview = await _importService.PreviewImportFileAsync(ImportFilePathTextBox.Text, format);

                // 显示预览对话框
                ShowImportPreviewDialog(preview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "预览导入文件失败");
                MessageBox.Show($"预览文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 开始导入
        /// </summary>
        private async void StartImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(ImportFilePathTextBox.Text))
                {
                    MessageBox.Show("请选择要导入的文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (ImportFormatComboBox.SelectedItem is not ComboBoxItem formatItem)
                {
                    MessageBox.Show("请选择文件格式", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 创建导入请求
                var request = new ImportRequestDto
                {
                    ProjectId = _currentProjectId,
                    Format = Enum.Parse<ImportFormat>(formatItem.Tag.ToString()!),
                    SourcePath = ImportFilePathTextBox.Text,
                    OverwriteExisting = OverwriteExistingCheckBox.IsChecked == true,
                    CreateBackup = CreateBackupCheckBox.IsChecked == true
                };

                // 开始导入
                var result = await _importService.StartImportAsync(request);
                _currentImportOperationId = result.OperationId;

                // 显示进度面板
                ImportProgressCard.Visibility = Visibility.Visible;
                _progressTimer.Start();

                _logger.LogInformation($"开始导入操作: {result.OperationId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动导入失败");
                MessageBox.Show($"启动导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 取消导入
        /// </summary>
        private void CancelImport_Click(object sender, RoutedEventArgs e)
        {
            if (_currentImportOperationId.HasValue)
            {
                _importService.CancelImport(_currentImportOperationId.Value);
                _logger.LogInformation($"用户取消导入操作: {_currentImportOperationId}");
            }
        }

        /// <summary>
        /// 刷新导入历史记录
        /// </summary>
        private async void RefreshImportHistory_Click(object sender, RoutedEventArgs e)
        {
            await LoadImportHistoryAsync();
        }

        /// <summary>
        /// 查看导入详情
        /// </summary>
        private void ViewImportDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is OperationHistoryViewModel history)
            {
                var details = $"操作ID: {history.OperationId}\n" +
                             $"格式: {history.Format}\n" +
                             $"文件: {history.FilePath}\n" +
                             $"大小: {history.FileSize:N0} 字节\n" +
                             $"状态: {GetStatusDescription(history.Status)}\n" +
                             $"开始时间: {history.StartTime:yyyy-MM-dd HH:mm:ss}\n" +
                             $"完成时间: {history.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "未完成"}\n" +
                             $"备注: {history.Notes ?? "无"}";

                MessageBox.Show(details, "导入详情", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region 进度更新

        /// <summary>
        /// 导出进度更新事件处理
        /// </summary>
        private void OnExportProgressUpdated(object? sender, OperationResultDto result)
        {
            Dispatcher.Invoke(() =>
            {
                if (result.OperationId == _currentExportOperationId)
                {
                    UpdateExportProgress(result);
                }
            });
        }

        /// <summary>
        /// 导入进度更新事件处理
        /// </summary>
        private void OnImportProgressUpdated(object? sender, OperationResultDto result)
        {
            Dispatcher.Invoke(() =>
            {
                if (result.OperationId == _currentImportOperationId)
                {
                    UpdateImportProgress(result);
                }
            });
        }

        /// <summary>
        /// 进度定时器事件处理
        /// </summary>
        private void OnProgressTimerTick(object? sender, EventArgs e)
        {
            // 检查导出进度
            if (_currentExportOperationId.HasValue)
            {
                var exportResult = _exportService.GetOperationStatus(_currentExportOperationId.Value);
                if (exportResult != null)
                {
                    UpdateExportProgress(exportResult);
                }
            }

            // 检查导入进度
            if (_currentImportOperationId.HasValue)
            {
                var importResult = _importService.GetOperationStatus(_currentImportOperationId.Value);
                if (importResult != null)
                {
                    UpdateImportProgress(importResult);
                }
            }

            // 如果没有活动操作，停止定时器
            if (!_currentExportOperationId.HasValue && !_currentImportOperationId.HasValue)
            {
                _progressTimer.Stop();
            }
        }

        /// <summary>
        /// 更新导出进度
        /// </summary>
        private void UpdateExportProgress(OperationResultDto result)
        {
            ExportStatusText.Text = result.CurrentStep;
            ExportProgressBar.Value = result.Progress;
            ExportProgressText.Text = $"{result.ProcessedItems} / {result.TotalItems} 项目已处理";

            if (result.Status == OperationStatus.Completed || 
                result.Status == OperationStatus.Failed || 
                result.Status == OperationStatus.Cancelled)
            {
                _currentExportOperationId = null;
                ExportProgressCard.Visibility = Visibility.Collapsed;
                
                // 刷新历史记录
                _ = LoadExportHistoryAsync();

                if (result.Status == OperationStatus.Completed)
                {
                    MessageBox.Show($"导出完成！\n文件保存在: {result.OutputPath}", "导出成功", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (result.Status == OperationStatus.Failed)
                {
                    MessageBox.Show($"导出失败: {result.ErrorMessage}", "导出失败", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 更新导入进度
        /// </summary>
        private void UpdateImportProgress(OperationResultDto result)
        {
            ImportStatusText.Text = result.CurrentStep;
            ImportProgressBar.Value = result.Progress;
            ImportProgressText.Text = $"{result.ProcessedItems} / {result.TotalItems} 项目已处理";

            if (result.Status == OperationStatus.Completed || 
                result.Status == OperationStatus.Failed || 
                result.Status == OperationStatus.Cancelled)
            {
                _currentImportOperationId = null;
                ImportProgressCard.Visibility = Visibility.Collapsed;
                
                // 刷新历史记录
                _ = LoadImportHistoryAsync();

                if (result.Status == OperationStatus.Completed)
                {
                    var message = $"导入完成！\n处理了 {result.ProcessedItems} 条记录";
                    if (result.Warnings.Count > 0)
                    {
                        message += $"\n警告: {string.Join(", ", result.Warnings)}";
                    }
                    MessageBox.Show(message, "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (result.Status == OperationStatus.Failed)
                {
                    MessageBox.Show($"导入失败: {result.ErrorMessage}", "导入失败", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 显示导入预览对话框
        /// </summary>
        private void ShowImportPreviewDialog(ImportPreviewDto preview)
        {
            var message = $"文件信息:\n" +
                         $"文件名: {preview.FileInfo.FileName}\n" +
                         $"大小: {preview.FileInfo.FileSize:N0} 字节\n" +
                         $"格式: {preview.FileInfo.Format}\n" +
                         $"记录数: {preview.FileInfo.RecordCount}\n\n" +
                         $"检测到的数据类型: {string.Join(", ", preview.DetectedDataTypes)}\n\n" +
                         $"字段信息:\n{string.Join("\n", preview.Fields.Select(f => $"- {f.Name} ({f.Type})"))}";

            MessageBox.Show(message, "文件预览", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 获取状态描述
        /// </summary>
        private string GetStatusDescription(OperationStatus status)
        {
            return status switch
            {
                OperationStatus.Pending => "等待中",
                OperationStatus.InProgress => "进行中",
                OperationStatus.Completed => "已完成",
                OperationStatus.Failed => "已失败",
                OperationStatus.Cancelled => "已取消",
                _ => "未知"
            };
        }

        #endregion
    }

    /// <summary>
    /// 模拟的UnitOfWork实现
    /// </summary>
    internal class MockUnitOfWork : IUnitOfWork
    {
        // Repository属性 - 返回null，因为这只是演示用的Mock
        public IProjectRepository Projects => null!;
        public IVolumeRepository Volumes => null!;
        public IChapterRepository Chapters => null!;
        public ICharacterRepository Characters => null!;
        public IFactionRepository Factions => null!;
        public ICharacterRelationshipRepository CharacterRelationships => null!;
        public IFactionRelationshipRepository FactionRelationships => null!;
        public IWorldSettingRepository WorldSettings => null!;
        public ICultivationSystemRepository CultivationSystems => null!;
        public ICultivationLevelRepository CultivationLevels => null!;
        public IPoliticalSystemRepository PoliticalSystems => null!;
        public IPoliticalPositionRepository PoliticalPositions => null!;
        public IPlotRepository Plots => null!;
        public IResourceRepository Resources => null!;
        public IRaceRepository Races => null!;
        public IRaceRelationshipRepository RaceRelationships => null!;
        public ISecretRealmRepository SecretRealms => null!;
        public IRelationshipNetworkRepository RelationshipNetworks => null!;
        public ICurrencySystemRepository CurrencySystems => null!;

        // 方法实现 - 简单的空实现
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
    }
}
