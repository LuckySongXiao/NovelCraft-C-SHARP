using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.WPF.Localization;
using NovelManagement.WPF.Services.Copilot;

namespace NovelManagement.WPF.Views.Copilot
{
    /// <summary>
    /// 章节关联选择弹窗：书籍 → 分卷 → 章节 三级联动，产出 <see cref="ChapterReferral"/>。
    /// </summary>
    public partial class ChapterRefPickerDialog : Window
    {
        private readonly ProjectService? _projectService;
        private readonly VolumeService? _volumeService;
        private readonly ChapterService? _chapterService;

        /// <summary>确认后的关联引用（取消为 null）。</summary>
        public ChapterReferral? Result { get; private set; }

        public ChapterRefPickerDialog()
        {
            InitializeComponent();
            _projectService = App.ServiceProvider?.GetService<ProjectService>();
            _volumeService = App.ServiceProvider?.GetService<VolumeService>();
            _chapterService = App.ServiceProvider?.GetService<ChapterService>();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_projectService == null)
            {
                return;
            }

            try
            {
                var projects = (await _projectService.GetAllProjectsAsync()).Where(p => !p.IsDeleted).ToList();
                ProjectComboBox.ItemsSource = projects;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, LocalizationManager.TF("CRP.LoadBooksFailed", "加载书籍列表失败：{0}", ex.Message), LocalizationManager.T("CRP.Title", "关联章节"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VolumeComboBox.ItemsSource = null;
            ChapterComboBox.ItemsSource = null;
            ConfirmButton.IsEnabled = false;

            if (_volumeService == null || ProjectComboBox.SelectedItem is not Project project)
            {
                return;
            }

            try
            {
                var volumes = (await _volumeService.GetVolumeListAsync(project.Id)).OrderBy(v => v.Order).ToList();
                VolumeComboBox.ItemsSource = volumes
                    .Select(v => new ItemDisplay(v.Id, LocalizationManager.TF("CRP.VolumeDisplay", "第{0}卷 {1}", v.Order, v.Title), v))
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, LocalizationManager.TF("CRP.LoadVolumesFailed", "加载分卷失败：{0}", ex.Message), LocalizationManager.T("CRP.Title", "关联章节"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void VolumeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChapterComboBox.ItemsSource = null;
            ConfirmButton.IsEnabled = false;

            if (_chapterService == null || VolumeComboBox.SelectedItem is not ItemDisplay volume)
            {
                return;
            }

            try
            {
                var chapters = (await _chapterService.GetChapterListAsync(volume.Id)).OrderBy(c => c.Order).ToList();
                ChapterComboBox.ItemsSource = chapters
                    .Select(c => new ItemDisplay(c.Id, LocalizationManager.TF("CRP.ChapterDisplay", "第{0}章 《{1}》", c.Order, c.Title), c))
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, LocalizationManager.TF("CRP.LoadChaptersFailed", "加载章节失败：{0}", ex.Message), LocalizationManager.T("CRP.Title", "关联章节"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ChapterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConfirmButton.IsEnabled = ChapterComboBox.SelectedItem is ItemDisplay;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectComboBox.SelectedItem is not Project project ||
                VolumeComboBox.SelectedItem is not ItemDisplay volume ||
                ChapterComboBox.SelectedItem is not ItemDisplay chapter ||
                chapter.Entity is not Chapter chapterEntity)
            {
                return;
            }

            var volumeEntity = (Volume)volume.Entity;
            Result = new ChapterReferral
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                VolumeId = volume.Id,
                VolumeTitle = volumeEntity.Title,
                VolumeOrder = volumeEntity.Order,
                ChapterId = chapter.Id,
                ChapterTitle = chapterEntity.Title,
                ChapterOrder = chapterEntity.Order,
                Summary = chapterEntity.Summary
            };
            DialogResult = true;
        }

        private sealed record ItemDisplay(Guid Id, string Text, object Entity);
    }
}
