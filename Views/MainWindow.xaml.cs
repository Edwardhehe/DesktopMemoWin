using DesktopMemo.Services;
using DesktopMemo.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DesktopMemo.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑。
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private SystemTrayService _systemTrayService;
        private StickyNoteWindow? _stickyNoteWindow;
        private bool _isUpdatingStickyNotePosition;
        private readonly DispatcherTimer _stickyNoteLayoutTimer = new() { Interval = TimeSpan.FromMilliseconds(120) };

        /// <summary>
        /// 获取系统托盘服务。
        /// </summary>
        public SystemTrayService SystemTrayService => _systemTrayService;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            ApplyBackgroundColor(_viewModel.BackgroundColor);

            _systemTrayService = new SystemTrayService();
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            LocationChanged += MainWindow_LocationChanged;
            SizeChanged += MainWindow_SizeChanged;
            _stickyNoteLayoutTimer.Tick += StickyNoteLayoutTimer_Tick;
        }

        /// <summary>
        /// 处理视图模型属性变化。
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsDesktopVisible))
            {
                if (_viewModel.IsDesktopVisible)
                {
                    Show();
                    _stickyNoteWindow?.Show();
                    SetWindowPos(
                        new System.Windows.Interop.WindowInteropHelper(this).Handle,
                        new IntPtr(-1),
                        0,
                        0,
                        0,
                        0,
                        0x0001 | 0x0002 | 0x0004);
                }
                else
                {
                    _stickyNoteWindow?.Hide();
                    Hide();
                }
            }
            else if (e.PropertyName == nameof(MainViewModel.BackgroundColor))
            {
                ApplyBackgroundColor(_viewModel.BackgroundColor);
            }
        }

        private void ApplyBackgroundColor(string colorValue)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorValue);
                MainWindowSurface.Background = new SolidColorBrush(Color.FromArgb(0xAA, color.R, color.G, color.B));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用背景颜色失败：{ex.Message}");
                MainWindowSurface.Background = new SolidColorBrush(Color.FromArgb(0xAA, 0xFF, 0xFF, 0xFF));
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _systemTrayService.Initialize(this, _viewModel);
            ApplyResponsiveWindowLayout();
            Show();
            EnsureStickyNoteWindow();
            ApplyStickyNoteLayout();

            if (_stickyNoteWindow != null)
            {
                _stickyNoteWindow.Show();
            }
        }

        private void ApplyResponsiveWindowLayout()
        {
            var workArea = SystemParameters.WorkArea;
            var targetWidth = Math.Min(780, Math.Max(560, workArea.Width * 0.62));
            var targetHeight = Math.Min(Math.Max(620, workArea.Height * 0.76), workArea.Height - 2);

            Width = targetWidth;
            Height = targetHeight;
            MaxWidth = Math.Max(560, workArea.Width - 8);
            MaxHeight = Math.Max(620, workArea.Height - 2);
            Left = Math.Max(0, workArea.Right - Width);
            Top = Math.Max(0, workArea.Top);
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            try
            {
                SaveStickyNoteLayout();
                _stickyNoteWindow?.Close();
                _viewModel?.StopDesktopMonitoring();
                _systemTrayService?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"窗口关闭时发生异常：{ex.Message}");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            HideWindowGroupToTray();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_viewModel)
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
        }

        private void DateRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is CalendarDayViewModel dayViewModel)
            {
                try
                {
                    var dailyTasksWindow = new DailyTasksWindow(dayViewModel.Date, _viewModel);
                    dailyTasksWindow.Show();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"打开当日任务窗口时发生异常：{ex.Message}");
                    MessageBox.Show($"打开当日任务失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MemoTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && sender is FrameworkElement element && element.Tag is Models.MemoItem memo)
            {
                try
                {
                    var detailWindow = new MemoDetailWindow(memo, EditMemoCallback, _viewModel)
                    {
                        Owner = this
                    };
                    detailWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"打开备忘录详情窗口时发生异常：{ex.Message}");
                    MessageBox.Show($"打开备忘录详情失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditMemoCallback(Models.MemoItem memo)
        {
            try
            {
                var oldDate = memo.Date;
                var dialog = new MemoInputDialog(memo.Date, memo.Content, this, memo.Priority, memo.IsPinned);
                if (dialog.ShowDialog() == true)
                {
                    memo.Content = dialog.MemoContent;
                    memo.Date = dialog.MemoDate;
                    memo.Priority = dialog.MemoPriority;
                    memo.IsPinned = dialog.MemoIsPinned;

                    _viewModel?.GetDatabaseService()?.UpdateMemo(memo);

                    if (_viewModel != null)
                    {
                        _viewModel.RefreshAllViews();
                        NotifyAllWindowsRefresh();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"编辑备忘录时发生异常：{ex.Message}");
                MessageBox.Show($"编辑备忘录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    else if (window is MemoDetailWindow memoDetailWindow)
                    {
                        memoDetailWindow.RefreshData();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"通知窗口刷新时发生异常：{ex.Message}");
            }
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            ScheduleStickyNoteLayoutUpdate();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScheduleStickyNoteLayoutUpdate();
        }

        private void StickyNoteLayoutTimer_Tick(object? sender, EventArgs e)
        {
            _stickyNoteLayoutTimer.Stop();
            ApplyStickyNoteLayout();
        }

        private void ScheduleStickyNoteLayoutUpdate()
        {
            if (_stickyNoteWindow == null)
            {
                return;
            }

            _stickyNoteLayoutTimer.Stop();
            _stickyNoteLayoutTimer.Start();
        }

        private void EnsureStickyNoteWindow()
        {
            if (_stickyNoteWindow != null)
            {
                return;
            }

            _stickyNoteWindow = new StickyNoteWindow
            {
                Owner = this
            };

            _stickyNoteWindow.LocationChanged += StickyNoteWindow_LayoutChanged;
            _stickyNoteWindow.SizeChanged += StickyNoteWindow_LayoutChanged;
            _stickyNoteWindow.Closing += StickyNoteWindow_Closing;
        }

        private void StickyNoteWindow_Closing(object? sender, CancelEventArgs e)
        {
            SaveStickyNoteLayout();
        }

        private void StickyNoteWindow_LayoutChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingStickyNotePosition)
            {
                return;
            }

            SaveStickyNoteLayout();
        }

        private void ApplyStickyNoteLayout()
        {
            if (_stickyNoteWindow == null)
            {
                return;
            }

            _isUpdatingStickyNotePosition = true;
            try
            {
                var workArea = SystemParameters.WorkArea;
                var layout = _stickyNoteWindow.GetSavedLayout();

                var noteWidth = Math.Clamp(Width * layout.WidthRatio, 240, Math.Max(240, workArea.Width * 0.55));
                var noteHeight = Math.Clamp(Height * layout.HeightRatio, 220, Math.Max(220, workArea.Height * 0.85));
                var noteLeft = Left + (Width * layout.OffsetXRatio);
                var noteTop = Top + (Height * layout.OffsetYRatio);

                noteLeft = Math.Max(workArea.Left, Math.Min(noteLeft, workArea.Right - noteWidth));
                noteTop = Math.Max(workArea.Top, Math.Min(noteTop, workArea.Bottom - noteHeight));

                _stickyNoteWindow.Width = noteWidth;
                _stickyNoteWindow.Height = noteHeight;
                _stickyNoteWindow.Left = noteLeft;
                _stickyNoteWindow.Top = noteTop;

                if (IsVisible)
                {
                    _stickyNoteWindow.Show();
                }
            }
            finally
            {
                _isUpdatingStickyNotePosition = false;
            }
        }

        private void SaveStickyNoteLayout()
        {
            if (_stickyNoteWindow == null)
            {
                return;
            }

            _stickyNoteWindow.SaveRelativeLayout(this);
        }

        public void HideWindowGroupToTray()
        {
            SaveStickyNoteLayout();
            _stickyNoteWindow?.SaveContentNow();
            _stickyNoteWindow?.Hide();
            Hide();
        }

        public void RestoreWindowGroupFromTray()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();

            EnsureStickyNoteWindow();
            ApplyStickyNoteLayout();
            _stickyNoteWindow?.Show();
            _stickyNoteWindow?.Activate();
        }
    }
}
