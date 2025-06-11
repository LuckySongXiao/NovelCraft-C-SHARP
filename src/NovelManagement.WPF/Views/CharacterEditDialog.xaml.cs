using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NovelManagement.Core.Entities;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// CharacterEditDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CharacterEditDialog : Window
    {
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
            IsNewCharacter = true;
            Character = new Character();
            Title = "新建角色";
            InitializeForm();
        }

        /// <summary>
        /// 构造函数 - 编辑现有角色
        /// </summary>
        /// <param name="character">要编辑的角色</param>
        public CharacterEditDialog(Character character)
        {
            InitializeComponent();
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
                Tags = character.Tags
            };

            // 保存原始势力和种族信息用于界面显示
            _originalFactionName = character.Faction?.Name ?? "";
            _originalRaceName = character.Race?.Name ?? "";
            Title = $"编辑角色 - {character.Name}";
            InitializeForm();
            LoadCharacterData();
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
                DescriptionTextBox.Text = Character.Background;
                AppearanceTextBox.Text = Character.Appearance;
                PersonalityTextBox.Text = Character.Personality;
                BackgroundTextBox.Text = Character.Background;
                AbilitiesTextBox.Text = Character.Abilities;

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
                Character.Name = NameTextBox.Text.Trim();
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
                Character.Background = BackgroundTextBox.Text.Trim();
                Character.Appearance = AppearanceTextBox.Text.Trim();
                Character.Personality = PersonalityTextBox.Text.Trim();
                Character.Abilities = AbilitiesTextBox.Text.Trim();

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
    }
}
