using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using NovelManagement.Core.Entities;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// CharacterEditDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CharacterEditDialog : Window
    {
        private readonly IAIAssistantService? _aiAssistantService;
        private CharacterFormSnapshot _initialSnapshot = CharacterFormSnapshot.Empty;

        /// <summary>
        /// 编辑的角色
        /// </summary>
        public Character Character { get; private set; }

        /// <summary>
        /// 是否是新建模式
        /// </summary>
        public bool IsNewCharacter { get; private set; }

        /// <summary>
        /// 原始势力名称（用于界面显示）
        /// </summary>
        private string _originalFactionName = "";

        /// <summary>
        /// 原始种族名称（用于界面显示）
        /// </summary>
        private string _originalRaceName = "";

        /// <summary>
        /// 构造函数 - 新建角色
        /// </summary>
        public CharacterEditDialog()
        {
            InitializeComponent();
            _aiAssistantService = App.ServiceProvider?.GetService<IAIAssistantService>();
            IsNewCharacter = true;
            Character = new Character();
            Title = "新建角色";
            InitializeForm();
            _initialSnapshot = CaptureSnapshot();
        }

        /// <summary>
        /// 构造函数 - 编辑现有角色
        /// </summary>
        /// <param name="character">要编辑的角色</param>
        public CharacterEditDialog(Character character)
        {
            InitializeComponent();
            _aiAssistantService = App.ServiceProvider?.GetService<IAIAssistantService>();
            IsNewCharacter = false;
            Character = new Character
            {
                Id = character.Id,
                Name = character.Name,
                Type = character.Type,
                FactionId = character.FactionId,
                // 不复制导航属性，避免EF Core跟踪问题
                Faction = null,
                RaceId = character.RaceId,
                Race = null,
                Background = character.Background,
                Appearance = character.Appearance,
                Personality = character.Personality,
                CultivationLevel = character.CultivationLevel,
                Abilities = character.Abilities,
                ProjectId = character.ProjectId,
                Age = character.Age,
                Gender = character.Gender,
                Importance = character.Importance,
                Status = character.Status,
                CreatedAt = character.CreatedAt,
                UpdatedAt = character.UpdatedAt,
                IsDeleted = character.IsDeleted,
                DeletedAt = character.DeletedAt,
                Notes = character.Notes,
                Tags = character.Tags,
                History = character.History,
                KeyEvents = character.KeyEvents
            };

            // 保存原始势力和种族信息用于界面显示
            _originalFactionName = character.Faction?.Name ?? "";
            _originalRaceName = character.Race?.Name ?? "";
            Title = $"编辑角色 - {character.Name}";
            InitializeForm();
            LoadCharacterData();
            _initialSnapshot = CaptureSnapshot();
        }

        /// <summary>
        /// 初始化表单
        /// </summary>
        private void InitializeForm()
        {
            // 设置默认选中项
            if (TypeComboBox.Items.Count > 0)
                TypeComboBox.SelectedIndex = 0;
            
            if (FactionComboBox.Items.Count > 0)
                FactionComboBox.SelectedIndex = 0;
                
            if (CultivationLevelComboBox.Items.Count > 0)
                CultivationLevelComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// 加载角色数据到表单
        /// </summary>
        private void LoadCharacterData()
        {
            try
            {
                NameTextBox.Text = Character.Name;
                DescriptionTextBox.Text = Character.Notes;
                AppearanceTextBox.Text = Character.Appearance;
                PersonalityTextBox.Text = Character.Personality;
                BackgroundTextBox.Text = Character.Background;
                AbilitiesTextBox.Text = Character.Abilities;
                HistoryTextBox.Text = Character.History;
                KeyEventsTextBox.Text = Character.KeyEvents;

                // 设置ComboBox选中项
                SetComboBoxSelection(TypeComboBox, Character.Type);
                SetComboBoxSelection(CultivationLevelComboBox, Character.CultivationLevel);

                // 势力ComboBox支持自定义输入，使用原始势力名称
                if (FactionComboBox.Items.Cast<ComboBoxItem>().Any(item => item.Content.ToString() == _originalFactionName))
                {
                    SetComboBoxSelection(FactionComboBox, _originalFactionName);
                }
                else
                {
                    FactionComboBox.Text = _originalFactionName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载角色数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 设置ComboBox选中项
        /// </summary>
        /// <param name="comboBox">ComboBox控件</param>
        /// <param name="value">要选中的值</param>
        private void SetComboBoxSelection(ComboBox comboBox, string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        /// <summary>
        /// 验证表单数据
        /// </summary>
        /// <returns>验证是否通过</returns>
        private bool ValidateForm()
        {
            // 验证必填字段
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("请输入角色姓名", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return false;
            }

            if (TypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("请选择角色类型", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                TypeComboBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(GetComboBoxValue(FactionComboBox)))
            {
                MessageBox.Show("请输入或选择所属势力", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                FactionComboBox.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取ComboBox的值（支持自定义输入）
        /// </summary>
        /// <param name="comboBox">ComboBox控件</param>
        /// <returns>ComboBox的值</returns>
        private string GetComboBoxValue(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Content.ToString();
            }
            return comboBox.Text;
        }

        /// <summary>
        /// 保存角色数据
        /// </summary>
        private void SaveCharacterData()
        {
            try
            {
                // 基本信息
                Character.Name = AiAutoFillFormatter.ExtractSingleLineValue(
                    NameTextBox.Text.Trim(),
                    "姓名", "名字", "角色名", "名称");
                Character.Type = ((ComboBoxItem)TypeComboBox.SelectedItem)?.Content.ToString() ?? "";

                // 处理势力信息 - 使用Tags字段传递势力名称
                var factionName = GetComboBoxValue(FactionComboBox);

                // 构建Tags字段，包含势力信息
                var tags = new List<string>();

                // 保留现有的非势力标签
                if (!string.IsNullOrEmpty(Character.Tags))
                {
                    var existingTags = Character.Tags.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !t.StartsWith("势力:"))
                        .ToList();
                    tags.AddRange(existingTags);
                }

                // 添加势力标签
                if (!string.IsNullOrEmpty(factionName) && factionName != "无")
                {
                    tags.Add($"势力:{factionName}");
                }

                // 设置Tags字段
                Character.Tags = string.Join(",", tags);

                // 完全清除所有导航属性和外键，避免任何EF Core跟踪问题
                Character.Faction = null;
                Character.Race = null;
                Character.FactionId = null;
                Character.RaceId = null;

                // 详细信息
                Character.CultivationLevel = GetComboBoxValue(CultivationLevelComboBox);
                Character.Notes = DescriptionTextBox.Text.Trim();
                Character.Background = BackgroundTextBox.Text.Trim();
                Character.Appearance = AppearanceTextBox.Text.Trim();
                Character.Personality = PersonalityTextBox.Text.Trim();
                Character.Abilities = AbilitiesTextBox.Text.Trim();
                Character.History = HistoryTextBox.Text.Trim();
                Character.KeyEvents = KeyEventsTextBox.Text.Trim();

                // 确保必需字段不为空
                if (string.IsNullOrEmpty(Character.Name))
                {
                    throw new ArgumentException("角色名称不能为空");
                }

                if (string.IsNullOrEmpty(Character.Type))
                {
                    Character.Type = "配角"; // 默认类型
                }

                // 设置时间戳和状态
                if (IsNewCharacter)
                {
                    Character.Id = Guid.NewGuid();
                    Character.CreatedAt = DateTime.UtcNow;
                    Character.UpdatedAt = DateTime.UtcNow;
                    Character.Status = "Active";
                    Character.Importance = 1; // 默认重要性
                    Character.IsDeleted = false;
                }
                else
                {
                    Character.UpdatedAt = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存角色数据时发生错误: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 保存按钮点击事件
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateForm())
                    return;

                SaveCharacterData();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ResetContent_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "是否将当前内容恢复为打开窗口时的初始状态？",
                "重置内容",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            RestoreSnapshot(_initialSnapshot);
        }

        /// <summary>
        /// AI 自动补全角色信息。
        /// </summary>
        private async void AutoFillWithAI_Click(object sender, RoutedEventArgs e)
        {
            if (_aiAssistantService == null)
            {
                #region debug-point E:character-autofill-service-missing
                await ReportDebugEventAsync(
                    "E",
                    "AutoFillWithAI_Click",
                    "角色自动补全时AI助手服务未初始化");
                #endregion
                MessageBox.Show("AI助手服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["characterType"] = GetComboBoxValue(TypeComboBox),
                    ["faction"] = GetComboBoxValue(FactionComboBox),
                    ["cultivationLevel"] = GetComboBoxValue(CultivationLevelComboBox),
                    ["name"] = NameTextBox.Text.Trim(),
                    ["description"] = DescriptionTextBox.Text.Trim(),
                    ["appearance"] = AppearanceTextBox.Text.Trim(),
                    ["personality"] = PersonalityTextBox.Text.Trim(),
                    ["background"] = BackgroundTextBox.Text.Trim(),
                    ["abilities"] = AbilitiesTextBox.Text.Trim(),
                    ["history"] = HistoryTextBox.Text.Trim(),
                    ["keyEvents"] = KeyEventsTextBox.Text.Trim()
                };

                #region debug-point C:character-autofill-enter
                await ReportDebugEventAsync(
                    "C",
                    "AutoFillWithAI_Click",
                    "进入角色自动补全入口",
                    new Dictionary<string, object?>
                    {
                        ["name"] = NameTextBox.Text.Trim(),
                        ["characterType"] = GetComboBoxValue(TypeComboBox),
                        ["hasDescription"] = !string.IsNullOrWhiteSpace(DescriptionTextBox.Text),
                        ["parameterKeys"] = parameters.Keys.OrderBy(key => key).ToArray()
                    });
                #endregion

                var result = await _aiAssistantService.GenerateCharacterAsync(parameters);
                if (!result.IsSuccess || result.Data == null)
                {
                    #region debug-point C:character-autofill-failed
                    await ReportDebugEventAsync(
                        "C",
                        "AutoFillWithAI_Click",
                        $"角色自动补全返回失败: {result.Message}",
                        new Dictionary<string, object?>
                        {
                            ["isSuccess"] = result.IsSuccess,
                            ["hasData"] = result.Data != null,
                            ["message"] = result.Message
                        });
                    #endregion
                    MessageBox.Show(result.Message ?? "AI自动补全失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                #region debug-point C:character-autofill-success
                await ReportDebugEventAsync(
                    "C",
                    "AutoFillWithAI_Click",
                    "角色自动补全返回成功",
                    new Dictionary<string, object?>
                    {
                        ["message"] = result.Message,
                        ["contentLength"] = result.Data?.ToString()?.Length ?? 0
                    });
                #endregion

                ApplyAiGeneratedContent(result.Data.ToString() ?? string.Empty);
                MessageBox.Show("已完成角色信息自动补全。", "AI自动补全", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                #region debug-point E:character-autofill-exception
                await ReportDebugEventAsync(
                    "E",
                    "AutoFillWithAI_Click",
                    $"角色自动补全异常: {ex.Message}",
                    new Dictionary<string, object?>
                    {
                        ["exceptionType"] = ex.GetType().FullName,
                        ["stack"] = ex.ToString()
                    });
                #endregion
                MessageBox.Show($"AI自动补全失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyAiGeneratedContent(string content)
        {
            content = AiAutoFillFormatter.Normalize(content);
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            var description = AiAutoFillFormatter.ExtractSummary(
                content,
                "简介", "描述", "角色描述", "概述", "人物简介");

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                NameTextBox.Text = ExtractCharacterName(content);
            }

            AiAutoFillFormatter.FillIfEmpty(DescriptionTextBox, description);
            AiAutoFillFormatter.FillSectionIfEmpty(AppearanceTextBox, content, "外貌", "外貌特征", "外观");
            AiAutoFillFormatter.FillSectionIfEmpty(PersonalityTextBox, content, "性格", "性格特点", "性情");
            AiAutoFillFormatter.FillSectionIfEmpty(BackgroundTextBox, content, "背景", "背景故事", "身世", "出身");
            AiAutoFillFormatter.FillSectionIfEmpty(AbilitiesTextBox, content, "能力", "特殊能力", "技能", "功法");
            AiAutoFillFormatter.FillSectionIfEmpty(HistoryTextBox, content, "经历", "人生经历", "履历");
            AiAutoFillFormatter.FillSectionIfEmpty(KeyEventsTextBox, content, "关键事件", "重要事件", "事件");
        }

        private static string ExtractCharacterName(string content)
        {
            var explicitName = AiAutoFillFormatter.ExtractSection(
                content,
                "姓名", "名字", "角色名", "名称");
            if (!string.IsNullOrWhiteSpace(explicitName))
            {
                return AiAutoFillFormatter.ExtractSingleLineValue(explicitName, "姓名", "名字", "角色名", "名称");
            }

            var firstMeaningfulLine = AiAutoFillFormatter.ExtractFirstMeaningfulLine(content);

            return string.IsNullOrWhiteSpace(firstMeaningfulLine)
                ? "AI生成角色"
                : AiAutoFillFormatter.ExtractSingleLineValue(firstMeaningfulLine, "姓名", "名字", "角色名", "名称");
        }

        private CharacterFormSnapshot CaptureSnapshot()
        {
            return new CharacterFormSnapshot
            {
                Name = NameTextBox.Text,
                Description = DescriptionTextBox.Text,
                Appearance = AppearanceTextBox.Text,
                Personality = PersonalityTextBox.Text,
                Background = BackgroundTextBox.Text,
                Abilities = AbilitiesTextBox.Text,
                History = HistoryTextBox.Text,
                KeyEvents = KeyEventsTextBox.Text,
                CharacterType = GetComboBoxValue(TypeComboBox),
                Faction = FactionComboBox.Text,
                CultivationLevel = GetComboBoxValue(CultivationLevelComboBox)
            };
        }

        private void RestoreSnapshot(CharacterFormSnapshot snapshot)
        {
            NameTextBox.Text = snapshot.Name;
            DescriptionTextBox.Text = snapshot.Description;
            AppearanceTextBox.Text = snapshot.Appearance;
            PersonalityTextBox.Text = snapshot.Personality;
            BackgroundTextBox.Text = snapshot.Background;
            AbilitiesTextBox.Text = snapshot.Abilities;
            HistoryTextBox.Text = snapshot.History;
            KeyEventsTextBox.Text = snapshot.KeyEvents;
            RestoreComboBoxValue(TypeComboBox, snapshot.CharacterType);
            RestoreComboBoxValue(FactionComboBox, snapshot.Faction);
            RestoreComboBoxValue(CultivationLevelComboBox, snapshot.CultivationLevel);
        }

        private void RestoreComboBoxValue(ComboBox comboBox, string value)
        {
            if (comboBox.Items.Cast<object>().Any(item =>
                string.Equals((item as ComboBoxItem)?.Content?.ToString() ?? item.ToString(), value, StringComparison.OrdinalIgnoreCase)))
            {
                SetComboBoxSelection(comboBox, value);
            }
            else
            {
                comboBox.Text = value;
            }
        }

        private sealed class CharacterFormSnapshot
        {
            public static CharacterFormSnapshot Empty { get; } = new();

            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public string Appearance { get; init; } = string.Empty;
            public string Personality { get; init; } = string.Empty;
            public string Background { get; init; } = string.Empty;
            public string Abilities { get; init; } = string.Empty;
            public string History { get; init; } = string.Empty;
            public string KeyEvents { get; init; } = string.Empty;
            public string CharacterType { get; init; } = string.Empty;
            public string Faction { get; init; } = string.Empty;
            public string CultivationLevel { get; init; } = string.Empty;
        }

        #region debug-point C:report-helper
        private static async Task ReportDebugEventAsync(string hypothesisId, string location, string message, Dictionary<string, object?>? data = null)
        {
            try
            {
                var debugServerUrl = "http://127.0.0.1:7777/event";
                var debugSessionId = "single-file-ai-ui";
                var envPath = FindDebugEnvPath();
                if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
                {
                    foreach (var line in File.ReadAllLines(envPath, Encoding.UTF8))
                    {
                        if (line.StartsWith("DEBUG_SERVER_URL=", StringComparison.OrdinalIgnoreCase))
                        {
                            debugServerUrl = line["DEBUG_SERVER_URL=".Length..].Trim();
                        }
                        else if (line.StartsWith("DEBUG_SESSION_ID=", StringComparison.OrdinalIgnoreCase))
                        {
                            debugSessionId = line["DEBUG_SESSION_ID=".Length..].Trim();
                        }
                    }
                }

                var payload = JsonSerializer.Serialize(new
                {
                    sessionId = debugSessionId,
                    runId = "pre-fix",
                    hypothesisId,
                    location,
                    msg = $"[DEBUG] {message}",
                    data,
                    ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });

                using var client = new System.Net.Http.HttpClient();
                using var content = new System.Net.Http.StringContent(payload, Encoding.UTF8, "application/json");
                await client.PostAsync(debugServerUrl, content);
            }
            catch
            {
                // ignore debug instrumentation failures
            }
        }

        private static string? FindDebugEnvPath()
        {
            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".dbg", "single-file-ai-ui.env"),
                Path.Combine(Directory.GetCurrentDirectory(), ".dbg", "single-file-ai-ui.env")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, ".dbg", "single-file-ai-ui.env");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return null;
        }
        #endregion
    }
}
