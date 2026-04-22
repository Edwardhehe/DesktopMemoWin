using DesktopMemo.Services;
using DesktopMemo.ViewModels;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace DesktopMemo.Views
{
    /// <summary>
    /// 设置窗口。
    /// </summary>
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        private readonly MainViewModel _viewModel;
        private readonly DatabaseService _databaseService;
        private bool _startupEnabled;
        private string _selectedBackgroundColor = "#FFFFFF";
        private int _recycleBinCount;

        public bool StartupEnabled
        {
            get => _startupEnabled;
            set
            {
                _startupEnabled = value;
                OnPropertyChanged();
            }
        }

        public string SelectedBackgroundColor
        {
            get => _selectedBackgroundColor;
            set
            {
                _selectedBackgroundColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BackgroundPreviewBrush));
            }
        }

        public int RecycleBinCount
        {
            get => _recycleBinCount;
            set
            {
                _recycleBinCount = value;
                OnPropertyChanged();
            }
        }

        public Brush BackgroundPreviewBrush
        {
            get
            {
                try
                {
                    return (Brush)new BrushConverter().ConvertFromString(SelectedBackgroundColor);
                }
                catch
                {
                    return Brushes.White;
                }
            }
        }

        public SettingsWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            _databaseService = viewModel.GetDatabaseService();
            StartupEnabled = viewModel.IsStartupEnabled;
            SelectedBackgroundColor = viewModel.BackgroundColor;
            RecycleBinCount = _databaseService.GetRecycleBinMemos().Count;
            DataContext = this;
        }

        private void BackgroundColorButton_Click(object sender, RoutedEventArgs e)
        {
            var colors = new[] { "#FFFFFF", "#F0F0F0", "#E8F5E8", "#E6F3FF", "#FFF8E1", "#F3E5F5" };
            var currentIndex = Array.IndexOf(colors, SelectedBackgroundColor);
            var nextIndex = (currentIndex + 1) % colors.Length;
            SelectedBackgroundColor = colors[nextIndex];
        }

        private void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "导出备忘录数据",
                Filter = "SQLite 数据库文件 (*.db)|*.db|所有文件 (*.*)|*.*",
                DefaultExt = "db",
                FileName = $"DesktopMemo_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
            };

            if (saveFileDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _databaseService.ExportDatabase(saveFileDialog.FileName);
                MessageBox.Show("数据导出成功。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"数据导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportDataButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "导入数据会覆盖当前备忘录，是否继续？",
                "确认导入",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                Title = "导入备忘录数据",
                Filter = "SQLite 数据库文件 (*.db)|*.db|所有文件 (*.*)|*.*",
                DefaultExt = "db"
            };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _databaseService.ImportDatabase(openFileDialog.FileName);
                _databaseService.ReloadDatabase();
                _viewModel.RefreshAllViews();
                NotifyAllWindowsRefresh();
                RecycleBinCount = _databaseService.GetRecycleBinMemos().Count;
                MessageBox.Show("数据导入成功，界面已刷新。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"数据导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearDataButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "清空数据将删除所有备忘录且不可恢复，是否继续？",
                "确认清空",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                _databaseService.ClearAllMemos();
                _viewModel.RefreshAllViews();
                NotifyAllWindowsRefresh();
                RecycleBinCount = 0;
                MessageBox.Show("数据已清空。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"数据清空失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EmptyRecycleBinButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "将彻底删除回收站中的所有备忘录，是否继续？",
                "确认清空回收站",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                _databaseService.PurgeDeletedMemos();
                _viewModel.RefreshAllViews();
                NotifyAllWindowsRefresh();
                RecycleBinCount = 0;
                MessageBox.Show("回收站已清空。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"清空回收站失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.IsStartupEnabled = StartupEnabled;
            _viewModel.BackgroundColor = SelectedBackgroundColor;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void NotifyAllWindowsRefresh()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is DailyTasksWindow dailyTasksWindow)
                {
                    dailyTasksWindow.RefreshData();
                }
                else if (window is MemoDetailWindow memoDetailWindow)
                {
                    memoDetailWindow.RefreshData();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
