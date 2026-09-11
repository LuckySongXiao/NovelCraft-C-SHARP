using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NovelManagement.WPF.Services;
using static NovelManagement.WPF.Localization.LocalizationManager;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// AIModelConfigView.xaml 的交互逻辑
    /// </summary>
    public partial class AIModelConfigView : UserControl
    {
        #region 字段和属性

        private readonly ILogger<AIModelConfigView>? _logger;
        private readonly AIModelConfigService _configService;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化 AIModelConfigView 的新实例，从应用服务容器获取日志和配置服务
        /// </summary>
        public AIModelConfigView()
        {
            InitializeComponent();
            InitializeControls();
            
            try
            {
                _logger = App.ServiceProvider?.GetService(typeof(ILogger<AIModelConfigView>)) as ILogger<AIModelConfigView>;
                var configLogger = App.ServiceProvider?.GetService(typeof(ILogger<AIModelConfigService>)) as ILogger<AIModelConfigService>;
                _configService = new AIModelConfigService(configLogger);
            }
            catch (Exception ex)
            {
                _configService = new AIModelConfigService();
                MessageBox.Show(TF("AMC.InitFallbackFmt", "配置服务初始化失败，使用默认服务: {0}", ex.Message), T("Msg.Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
            LoadConfiguration();
            UpdatePerformanceMetrics();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化控件
        /// </summary>
        private void InitializeControls()
        {
            // 设置默认值
            DefaultModelComboBox.SelectedIndex = 0;
            DialogueModelComboBox.SelectedIndex = 0;
            AnalysisModelComboBox.SelectedIndex = 1;
            FactionModelComboBox.SelectedIndex = 0;
            AnalysisDepthComboBox.SelectedIndex = 1;
            PredictionRangeComboBox.SelectedIndex = 1;

            // 绑定滑块事件
            TimeoutSlider.ValueChanged += TimeoutSlider_ValueChanged;
            DialogueCreativitySlider.ValueChanged += (s, e) => UpdateSliderLabel(T("AMC.Creativity"), DialogueCreativitySlider.Value.ToString("F1"));
            DialogueMaxLengthSlider.ValueChanged += (s, e) => UpdateSliderLabel(T("AMC.MaxLength"), TF("AMC.MaxLengthLiveFmt", "{0:F0}字符", DialogueMaxLengthSlider.Value));
            
            UpdateTimeoutLabel();
        }

        /// <summary>
        /// 更新超时时间标签
        /// </summary>
        private void UpdateTimeoutLabel()
        {
            TimeoutLabel.Text = TF("AMC.TimeoutLiveFmt", "{0:F0}秒", TimeoutSlider.Value);
        }

        /// <summary>
        /// 更新滑块标签
        /// </summary>
        private void UpdateSliderLabel(string type, string value)
        {
            // 这里可以根据需要更新相应的标签
            _logger?.LogDebug("更新{Type}设置: {Value}", type, value);
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 超时时间滑块值改变事件
        /// </summary>
        private void TimeoutSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateTimeoutLabel();
        }

        /// <summary>
        /// 测试连接按钮点击事件
        /// </summary>
        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TestConnectionButton.IsEnabled = false;
                StatusText.Text = T("AMC.TestingConnection", "正在测试连接...");

                var success = await _configService.TestConnectionAsync();
                
                if (success)
                {
                    StatusText.Text = T("AMC.TestSuccessStatus", "连接测试成功，所有模型响应正常");
                    MessageBox.Show(T("AMC.TestSuccessMsg", "连接测试成功！"), T("AMC.TestResultTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusText.Text = T("AMC.TestFailStatus", "连接测试失败，请检查配置");
                    MessageBox.Show(T("AMC.TestFailMsg", "连接测试失败，请检查API密钥和网络连接"), T("AMC.TestResultTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "连接测试失败");
                StatusText.Text = T("AMC.TestErrorStatus", "连接测试出现错误");
                MessageBox.Show(TF("AMC.TestErrorFmt", "测试过程中发生错误: {0}", ex.Message), T("Msg.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// 保存配置按钮点击事件
        /// </summary>
        private async void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveConfigButton.IsEnabled = false;
                StatusText.Text = T("AMC.SavingConfig", "正在保存配置...");

                var config = GetCurrentConfiguration();
                await _configService.SaveConfigurationAsync(config);
                
                StatusText.Text = T("AMC.ConfigSaved", "配置已保存");
                MessageBox.Show(T("AMC.SaveSuccessMsg", "配置保存成功！"), T("AMC.SaveResultTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                
                _logger?.LogInformation("AI模型配置已保存");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存配置失败");
                StatusText.Text = T("AMC.SaveFailStatus", "保存配置失败");
                MessageBox.Show(TF("AMC.SaveFailFmt", "保存配置失败: {0}", ex.Message), T("Msg.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SaveConfigButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// 重置默认按钮点击事件
        /// </summary>
        private void ResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(T("AMC.ResetConfirmMsg", "确定要重置为默认配置吗？这将覆盖当前所有设置。"), 
                T("AMC.ResetConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                ResetToDefaults();
                StatusText.Text = T("AMC.ResetDone", "已重置为默认配置");
            }
        }

        /// <summary>
        /// 导出配置按钮点击事件
        /// </summary>
        private void ExportConfig_Click(object sender, RoutedEventArgs e)
        {
            _ = ExportConfigAsync();
        }

        /// <summary>
        /// 导入配置按钮点击事件
        /// </summary>
        private void ImportConfig_Click(object sender, RoutedEventArgs e)
        {
            _ = ImportConfigAsync();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载配置
        /// </summary>
        private async void LoadConfiguration()
        {
            try
            {
                var config = await _configService.LoadConfigurationAsync();
                ApplyConfiguration(config);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载配置失败");
                ResetToDefaults();
            }
        }

        /// <summary>
        /// 应用配置
        /// </summary>
        private void ApplyConfiguration(AIModelConfigService.ModelConfiguration config)
        {
            // 应用全局设置
            SelectComboBoxItem(DefaultModelComboBox, config.DefaultModel);
            TimeoutSlider.Value = config.TimeoutSeconds;
            EnableCacheCheckBox.IsChecked = config.EnableCache;
            EnableLoggingCheckBox.IsChecked = config.EnableLogging;
            ApiKeyPasswordBox.Password = config.ApiKey ?? string.Empty;

            // 应用功能特定设置
            SelectComboBoxItem(DialogueModelComboBox, config.DialogueModel);
            DialogueCreativitySlider.Value = config.DialogueCreativity;
            DialogueMaxLengthSlider.Value = config.DialogueMaxLength;
            SelectComboBoxItem(AnalysisModelComboBox, config.AnalysisModel);
            SelectComboBoxItem(FactionModelComboBox, config.FactionModel);
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        private AIModelConfigService.ModelConfiguration GetCurrentConfiguration()
        {
            return new AIModelConfigService.ModelConfiguration
            {
                DefaultModel = (DefaultModelComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? (DefaultModelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "DeepSeek-V3",
                ApiKey = ApiKeyPasswordBox.Password,
                TimeoutSeconds = (int)TimeoutSlider.Value,
                EnableCache = EnableCacheCheckBox.IsChecked ?? true,
                EnableLogging = EnableLoggingCheckBox.IsChecked ?? false,
                DialogueModel = (DialogueModelComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? (DialogueModelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "DeepSeek-V3",
                DialogueCreativity = DialogueCreativitySlider.Value,
                DialogueMaxLength = (int)DialogueMaxLengthSlider.Value,
                AnalysisModel = (AnalysisModelComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? (AnalysisModelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "GPT-4",
                FactionModel = (FactionModelComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? (FactionModelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Claude-3"
            };
        }

        /// <summary>
        /// 重置为默认值
        /// </summary>
        private void ResetToDefaults()
        {
            DefaultModelComboBox.SelectedIndex = 0;
            ApiKeyPasswordBox.Password = "";
            TimeoutSlider.Value = 30;
            EnableCacheCheckBox.IsChecked = true;
            EnableLoggingCheckBox.IsChecked = false;
            DialogueModelComboBox.SelectedIndex = 0;
            DialogueCreativitySlider.Value = 0.7;
            DialogueMaxLengthSlider.Value = 800;
            AnalysisModelComboBox.SelectedIndex = 1;
            FactionModelComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// 更新性能指标
        /// </summary>
        private async void UpdatePerformanceMetrics()
        {
            try
            {
                var metrics = await _configService.GetPerformanceMetricsAsync();
                
                ResponseTimeText.Text = $"{metrics.AverageResponseTime:F1}s";
                SuccessRateText.Text = $"{metrics.SuccessRate:F1}%";
                CacheHitRateText.Text = $"{metrics.CacheHitRate:F0}%";
                TodayRequestsText.Text = metrics.TodayRequests.ToString();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "更新性能指标失败");
            }
        }

        private async Task ExportConfigAsync()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = T("AMC.ExportDialogTitle", "导出AI模型配置"),
                    Filter = T("AMC.FilterJson", "JSON文件|*.json"),
                    DefaultExt = "json",
                    FileName = $"ai-model-config_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                SaveConfigButton.IsEnabled = false;
                StatusText.Text = T("AMC.Exporting", "正在导出配置...");
                await _configService.ExportConfigurationAsync(GetCurrentConfiguration(), dialog.FileName);
                StatusText.Text = TF("AMC.ExportedStatusFmt", "配置已导出到 {0}", Path.GetFileName(dialog.FileName));
                MessageBox.Show(TF("AMC.ExportedFmt", "配置已导出到：{0}", dialog.FileName), T("AMC.ExportSuccessTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出配置失败");
                StatusText.Text = T("AMC.ExportFailStatus", "导出配置失败");
                MessageBox.Show(TF("AMC.ExportFailFmt", "导出配置失败: {0}", ex.Message), T("Msg.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SaveConfigButton.IsEnabled = true;
            }
        }

        private async Task ImportConfigAsync()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = T("AMC.ImportDialogTitle", "导入AI模型配置"),
                    Filter = T("AMC.FilterJsonAll", "JSON文件|*.json|所有文件|*.*"),
                    DefaultExt = "json",
                    CheckFileExists = true,
                    Multiselect = false
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                SaveConfigButton.IsEnabled = false;
                StatusText.Text = T("AMC.Importing", "正在导入配置...");
                var config = await _configService.ImportConfigurationAsync(dialog.FileName);
                ApplyConfiguration(config);
                await _configService.SaveConfigurationAsync(config);
                StatusText.Text = TF("AMC.ImportedStatusFmt", "已导入并应用配置: {0}", Path.GetFileName(dialog.FileName));
                MessageBox.Show(T("AMC.ImportSuccessMsg", "配置导入成功，已自动应用并保存到本地。"), T("AMC.ImportSuccessTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导入配置失败");
                StatusText.Text = T("AMC.ImportFailStatus", "导入配置失败");
                MessageBox.Show(TF("AMC.ImportFailFmt", "导入配置失败: {0}", ex.Message), T("Msg.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SaveConfigButton.IsEnabled = true;
            }
        }

        private static void SelectComboBoxItem(ComboBox comboBox, string? expectedValue)
        {
            if (string.IsNullOrWhiteSpace(expectedValue))
            {
                if (comboBox.Items.Count > 0)
                {
                    comboBox.SelectedIndex = 0;
                }

                return;
            }

            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem)
                {
                    var content = comboBoxItem.Tag?.ToString() ?? comboBoxItem.Content?.ToString();
                    if (string.Equals(content, expectedValue, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(content) && content.StartsWith(expectedValue, StringComparison.OrdinalIgnoreCase)))
                    {
                        comboBox.SelectedItem = comboBoxItem;
                        return;
                    }
                }
            }

            if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
            }
        }

        #endregion
    }
}
