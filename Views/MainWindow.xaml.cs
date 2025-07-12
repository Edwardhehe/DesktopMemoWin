using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using DesktopMemo.Services;
using DesktopMemo.ViewModels;

namespace DesktopMemo.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private SystemTrayService _systemTrayService;

        /// <summary>
        /// 获取系统托盘服务
        /// </summary>
        public SystemTrayService SystemTrayService => _systemTrayService;

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            
            // 初始化系统托盘服务
            _systemTrayService = new SystemTrayService();
            
            // 订阅桌面可见性变化事件
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            
            // 窗口加载完成后初始化托盘
            Loaded += MainWindow_Loaded;
            
            // 处理窗口关闭事件
            Closing += MainWindow_Closing;
        }

        /// <summary>
        /// 视图模型属性变化事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">属性变化事件参数</param>
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsDesktopVisible))
            {
                // 根据桌面可见性控制窗口显示
                if (_viewModel.IsDesktopVisible)
                {
                    Show();
                    // 设置窗口为最顶层，但不使用Topmost属性
                    SetWindowPos(new System.Windows.Interop.WindowInteropHelper(this).Handle, 
                                new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0004);
                }
                else
                {
                    Hide();
                }
            }
        }

        /// <summary>
        /// 设置窗口位置的Windows API
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="hWndInsertAfter">插入位置</param>
        /// <param name="X">X坐标</param>
        /// <param name="Y">Y坐标</param>
        /// <param name="cx">宽度</param>
        /// <param name="cy">高度</param>
        /// <param name="uFlags">标志</param>
        /// <returns>是否成功</returns>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        /// <summary>
        /// 主窗口加载完成事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始化系统托盘
            _systemTrayService.Initialize(this, _viewModel);

            // 强制显示窗口并设置位置
            this.Left = SystemParameters.WorkArea.Width - this.Width;
            this.Top = 0;
            this.Show();
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">取消事件参数</param>
        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            try
            {
                // 停止桌面监控服务
                _viewModel?.StopDesktopMonitoring();
                
                // 清理系统托盘资源
                _systemTrayService?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"窗口关闭时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 关闭按钮点击事件（最小化到托盘）
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _systemTrayService.HideToTray();
        }

        /// <summary>
        /// 设置按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_viewModel);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        /// <summary>
        /// 备忘录项目鼠标左键点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标按钮事件参数</param>
        private void MemoItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is Models.MemoItem memo)
            {
                // 可以在这里添加编辑备忘录的功能
                // 例如打开编辑对话框
            }
        }

        /// <summary>
        /// 备忘录文本鼠标左键点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标按钮事件参数</param>
        private void MemoTextBlock_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 判断是否为双击
            if (e.ClickCount == 2 && sender is FrameworkElement element && element.Tag is Models.MemoItem memo)
            {
                try
                {
                    var detailWindow = new Views.MemoDetailWindow(memo, EditMemoCallback);
                    detailWindow.Owner = this;
                    detailWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"打开备忘录详情窗口时发生异常：{ex.Message}");
                    MessageBox.Show($"打开备忘录详情失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 编辑备忘录回调
        /// </summary>
        /// <param name="memo">要编辑的备忘录</param>
        private void EditMemoCallback(Models.MemoItem memo)
        {
            try
            {
                var dialog = new Views.MemoInputDialog(memo.Date, memo.Content);
                if (dialog.ShowDialog() == true)
                {
                    memo.Content = dialog.MemoContent;
                    memo.Date = dialog.MemoDate;
                    
                    // 更新数据库
                    _viewModel?.GetDatabaseService()?.UpdateMemo(memo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"编辑备忘录时发生异常：{ex.Message}");
                MessageBox.Show($"编辑备忘录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 窗口加载完成事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 窗口加载完成后的初始化操作
        }
    }
} 