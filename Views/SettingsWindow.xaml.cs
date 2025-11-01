using DesktopMemo.Services;
using DesktopMemo.ViewModels;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace DesktopMemo.Views
{
    /// <summary>
    /// SettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private MainViewModel _viewModel;
        private DatabaseService _databaseService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="viewModel">主视图模型</param>
        public SettingsWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            // 使用主视图模型中的数据库服务实例，确保数据一致性
            _databaseService = viewModel.GetDatabaseService();
            DataContext = _viewModel;
        }

        /// <summary>
        /// 背景颜色按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void BackgroundColorButton_Click(object sender, RoutedEventArgs e)
        {
            // 使用简单的颜色选择方式
            var colors = new[] { "#FFFFFF", "#F0F0F0", "#E8F5E8", "#E6F3FF", "#FFF8E1", "#F3E5F5" };
            var currentIndex = Array.IndexOf(colors, _viewModel.BackgroundColor);
            var nextIndex = (currentIndex + 1) % colors.Length;
            _viewModel.BackgroundColor = colors[nextIndex];
        }

        /// <summary>
        /// 导出数据按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
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
                    MessageBox.Show("数据导出成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
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
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
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
                        // 导入数据库
                        _databaseService.ImportDatabase(openFileDialog.FileName);

                        // 重新加载数据库连接
                        _databaseService.ReloadDatabase();

                        // 刷新主界面数据
                        _viewModel.RefreshCalendarData();

                        // 通知所有打开的窗口刷新数据
                        NotifyAllWindowsRefresh();

                        MessageBox.Show("数据导入成功！界面已刷新。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
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
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
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
                    // 删除数据库文件
                    var dbPath = _databaseService.GetDatabasePath();
                    if (File.Exists(dbPath))
                    {
                        File.Delete(dbPath);
                    }

                    // 重新初始化数据库连接
                    _databaseService.ReloadDatabase();

                    // 刷新主界面数据
                    _viewModel.RefreshCalendarData();

                    // 通知所有打开的窗口刷新数据
                    NotifyAllWindowsRefresh();

                    MessageBox.Show("数据清空成功！界面已刷新。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
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
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 通知所有打开的窗口刷新数据
        /// </summary>
        private void NotifyAllWindowsRefresh()
        {
            try
            {
                // 遍历所有打开的窗口
                foreach (Window window in System.Windows.Application.Current.Windows)
                {
                    if (window is DailyTasksWindow dailyTasksWindow)
                    {
                        // 通知DailyTasksWindow刷新
                        dailyTasksWindow.RefreshData();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"通知窗口刷新时发生异常：{ex.Message}");
            }
        }
    }
}