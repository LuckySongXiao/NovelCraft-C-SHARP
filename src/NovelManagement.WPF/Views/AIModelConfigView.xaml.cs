using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using NovelManagement.WPF.Services;

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
                MessageBox.Show($"配置服务初始化失败，使用默认服务: {ex.Message}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            DialogueCreativitySlider.ValueChanged += (s, e) => UpdateSliderLabel("创造性", DialogueCreativitySlider.Value.ToString("F1"));
            DialogueMaxLengthSlider.ValueChanged += (s, e) => UpdateSliderLabel("最大长度", $"{DialogueMaxLengthSlider.Value:F0}字符");
            
            UpdateTimeoutLabel();
        }

        /// <summary>
        /// 更新超时时间标签
        /// </summary>
        private void UpdateTimeoutLabel()
        {
            TimeoutLabel.Text = $"{TimeoutSlider.Value:F0}秒";
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
                StatusText.Text = "正在测试连接...";

                var success = await _configService.TestConnectionAsync();
                
                if (success)
                {
                    StatusText.Text = "连接测试成功，所有模型响应正常";
                    MessageBox.Show("连接测试成功！", "测试结果", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusText.Text = "连接测试失败，请检查配置";
                    MessageBox.Show("连接测试失败，请检查API密钥和网络连接", "测试结果", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "连接测试失败");
                StatusText.Text = "连接测试出现错误";
                MessageBox.Show($"测试过程中发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                StatusText.Text = "正在保存配置...";

                var config = GetCurrentConfiguration();
                await _configService.SaveConfigurationAsync(config);
                
                StatusText.Text = "配置已保存";
                MessageBox.Show("配置保存成功！", "保存结果", MessageBoxButton.OK, MessageBoxImage.Information);
                
                _logger?.LogInformation("AI模型配置已保存");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存配置失败");
                StatusText.Text = "保存配置失败";
                MessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            var result = MessageBox.Show("确定要重置为默认配置吗？这将覆盖当前所有设置。", 
                "确认重置", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                ResetToDefaults();
                StatusText.Text = "已重置为默认配置";
            }
        }

        /// <summary>
        /// 导出配置按钮点击事件
        /// </summary>
        private void ExportConfig_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导出配置功能正在开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 导入配置按钮点击事件
        /// </summary>
        private void ImportConfig_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导入配置功能正在开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
            DefaultModelComboBox.SelectedItem = config.DefaultModel;
            TimeoutSlider.Value = config.TimeoutSeconds;
            EnableCacheCheckBox.IsChecked = config.EnableCache;
            EnableLoggingCheckBox.IsChecked = config.EnableLogging;

            // 应用功能特定设置
            DialogueCreativitySlider.Value = config.DialogueCreativity;
            DialogueMaxLengthSlider.Value = config.DialogueMaxLength;
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        private AIModelConfigService.ModelConfiguration GetCurrentConfiguration()
        {
            return new AIModelConfigService.ModelConfiguration
            {
                DefaultModel = (DefaultModelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "DeepSeek-V3",
                ApiKey = ApiKeyPasswordBox.Password,
                TimeoutSeconds = (int)TimeoutSlider.Value,
                EnableCache = EnableCacheCheckBox.IsChecked ?? true,
                EnableLogging = EnableLoggingCheckBox.IsChecked ?? false,
                DialogueModel = (DialogueModelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "DeepSeek-V3",
                DialogueCreativity = DialogueCreativitySlider.Value,
                DialogueMaxLength = (int)DialogueMaxLengthSlider.Value,
                AnalysisModel = (AnalysisModelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "GPT-4",
                FactionModel = (FactionModelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Claude-3"
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

        #endregion
    }
}
