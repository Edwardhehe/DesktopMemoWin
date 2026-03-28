using DesktopMemo.Services;
using DesktopMemo.ViewModels;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace DesktopMemo.Views
{
    /// <summary>
    /// SettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        private readonly MainViewModel _viewModel;
        private readonly DatabaseService _databaseService;
        private bool _startupEnabled;
        private string _selectedBackgroundColor = "#FFFFFF";

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

        /// <summary>
        /// 构造函数
        /// </summary>
        public SettingsWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            _databaseService = viewModel.GetDatabaseService();
            StartupEnabled = viewModel.IsStartupEnabled;
            SelectedBackgroundColor = viewModel.BackgroundColor;
            DataContext = this;
        }

        /// <summary>
        /// 背景颜色按钮点击事件
        /// </summary>
        private void BackgroundColorButton_Click(object sender, RoutedEventArgs e)
        {
            var colors = new[] { "#FFFFFF", "#F0F0F0", "#E8F5E8", "#E6F3FF", "#FFF8E1", "#F3E5F5" };
            var currentIndex = Array.IndexOf(colors, SelectedBackgroundColor);
            var nextIndex = (currentIndex + 1) % colors.Length;
            SelectedBackgroundColor = colors[nextIndex];
        }

        /// <summary>
        /// 导出数据按钮点击事件
        /// </summary>
        private void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "导出备忘录数据",
                Filter = "SQLite数据库文件 (*.db)|*.db|所有文件 (*.*)|*.*",
                DefaultExt = "db",
                FileName = $"DesktopMemo_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
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
        }

        /// <summary>
        /// 导入数据按钮点击事件
        /// </summary>
        private void ImportDataButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "导入数据将覆盖当前所有备忘录数据，是否继续？",
                "确认导入",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "导入备忘录数据",
                    Filter = "SQLite数据库文件 (*.db)|*.db|所有文件 (*.*)|*.*",
                    DefaultExt = "db"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        _databaseService.ImportDatabase(openFileDialog.FileName);
                        _databaseService.ReloadDatabase();
                        _viewModel.RefreshCalendar();
                        NotifyAllWindowsRefresh();

                        MessageBox.Show("数据导入成功，界面已刷新。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"数据导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// 清空数据按钮点击事件
        /// </summary>
        private void ClearDataButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "清空数据将删除所有备忘录，此操作不可恢复，是否继续？",
                "确认清空",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var dbPath = _databaseService.GetDatabasePath();
                    if (File.Exists(dbPath))
                    {
                        File.Delete(dbPath);
                    }

                    _databaseService.ReloadDatabase();
                    _viewModel.RefreshCalendar();
                    NotifyAllWindowsRefresh();

                    MessageBox.Show("数据清空成功，界面已刷新。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"数据清空失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.IsStartupEnabled = StartupEnabled;
            _viewModel.BackgroundColor = SelectedBackgroundColor;
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void NotifyAllWindowsRefresh()
        {
            try
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is DailyTasksWindow dailyTasksWindow)
                    {
                        dailyTasksWindow.RefreshData();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"通知窗口刷新时发生异常：{ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
