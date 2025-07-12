using DesktopMemo.Models;
using DesktopMemo.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.IO;

namespace DesktopMemo.ViewModels
{
    /// <summary>
    /// 主视图模型类
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly DesktopMonitorService _desktopMonitorService;
        private readonly StartupService _startupService;

        private DateTime _currentMonth;
        private bool _isDesktopVisible;
        private bool _isStartupEnabled;
        private string _backgroundColor = "#FFFFFF";
        // 配置文件路径
        private readonly string _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "DesktopMemo", "config.txt");

        /// <summary>
        /// 当前月份
        /// </summary>
        public DateTime CurrentMonth
        {
            get => _currentMonth;
            set
            {
                _currentMonth = value;
                OnPropertyChanged();
                LoadCalendarData();
            }
        }

        /// <summary>
        /// 桌面是否可见
        /// </summary>
        public bool IsDesktopVisible
        {
            get => _isDesktopVisible;
            set
            {
                _isDesktopVisible = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否启用开机启动
        /// </summary>
        public bool IsStartupEnabled
        {
            get => _isStartupEnabled;
            set
            {
                _isStartupEnabled = value;
                _startupService.SetStartup(value);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 背景颜色
        /// </summary>
        public string BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                SaveBackgroundColor(value);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 日历数据
        /// </summary>
        public ObservableCollection<CalendarDayViewModel> CalendarDays { get; } = new();

        /// <summary>
        /// 上个月命令
        /// </summary>
        public ICommand PreviousMonthCommand { get; }

        /// <summary>
        /// 下个月命令
        /// </summary>
        public ICommand NextMonthCommand { get; }

        /// <summary>
        /// 添加备忘录命令
        /// </summary>
        public ICommand AddMemoCommand { get; }

        /// <summary>
        /// 标记完成命令
        /// </summary>
        public ICommand MarkCompletedCommand { get; }

        /// <summary>
        /// 删除备忘录命令
        /// </summary>
        public ICommand DeleteMemoCommand { get; }

        /// <summary>
        /// 显示备忘录详情命令
        /// </summary>
        public ICommand ShowMemoDetailCommand { get; }

        /// <summary>
        /// 停止桌面监控服务
        /// </summary>
        public void StopDesktopMonitoring()
        {
            _desktopMonitorService?.StopMonitoring();
        }

        /// <summary>
        /// 刷新日历数据
        /// </summary>
        public void RefreshCalendarData()
        {
            LoadCalendarData();
        }

        /// <summary>
        /// 获取数据库服务实例
        /// </summary>
        /// <returns>数据库服务实例</returns>
        public DatabaseService GetDatabaseService()
        {
            return _databaseService;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainViewModel()
        {
            _databaseService = new DatabaseService();
            _desktopMonitorService = new DesktopMonitorService();
            _startupService = new StartupService();

            _currentMonth = DateTime.Now;
            _isStartupEnabled = _startupService.IsStartupEnabled();

            // 加载背景色
            _backgroundColor = LoadBackgroundColor();

            // 初始化命令
            PreviousMonthCommand = new RelayCommand(PreviousMonth);
            NextMonthCommand = new RelayCommand(NextMonth);
            AddMemoCommand = new RelayCommand<CalendarDayViewModel>(AddMemo);
            MarkCompletedCommand = new RelayCommand<MemoItem>(MarkCompleted);
            DeleteMemoCommand = new RelayCommand<MemoItem>(DeleteMemo);
            ShowMemoDetailCommand = new RelayCommand<MemoItem>(ShowMemoDetail);

            // 订阅桌面可见性变化事件
            _desktopMonitorService.DesktopVisibilityChanged += OnDesktopVisibilityChanged;

            // 开始监控桌面状态
            _desktopMonitorService.StartMonitoring();

            // 加载日历数据
            LoadCalendarData();
        }

        /// <summary>
        /// 加载日历数据
        /// </summary>
        private void LoadCalendarData()
        {
            try
            {
                // 暂停UI更新以提高性能
                var collection = CalendarDays;
                collection.Clear();

                var firstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                var firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;

                // 添加上个月的日期
                for (int i = firstDayOfWeek - 1; i >= 0; i--)
                {
                    var date = firstDayOfMonth.AddDays(-i - 1);
                    var dayViewModel = new CalendarDayViewModel(date, false);
                    collection.Add(dayViewModel);
                }

                // 添加当前月的日期
                for (int day = 1; day <= lastDayOfMonth.Day; day++)
                {
                    var date = new DateTime(_currentMonth.Year, _currentMonth.Month, day);
                    var memos = _databaseService.GetMemosByDate(date);
                    var dayViewModel = new CalendarDayViewModel(date, true, memos);
                    collection.Add(dayViewModel);
                }

                // 添加下个月的日期
                var remainingDays = 42 - collection.Count; // 保持6行7列的格式
                for (int i = 1; i <= remainingDays; i++)
                {
                    var date = lastDayOfMonth.AddDays(i);
                    var dayViewModel = new CalendarDayViewModel(date, false);
                    collection.Add(dayViewModel);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载日历数据时发生异常：{ex.Message}");
                // 不抛出异常，避免应用程序崩溃
            }
        }

        /// <summary>
        /// 上个月
        /// </summary>
        private void PreviousMonth()
        {
            CurrentMonth = CurrentMonth.AddMonths(-1);
        }

        /// <summary>
        /// 下个月
        /// </summary>
        private void NextMonth()
        {
            CurrentMonth = CurrentMonth.AddMonths(1);
        }

        /// <summary>
        /// 添加备忘录
        /// </summary>
        /// <param name="dayViewModel">日期视图模型</param>
        private void AddMemo(CalendarDayViewModel? dayViewModel)
        {
            if (dayViewModel == null || !dayViewModel.IsCurrentMonth) return;

            try
            {
                // 获取主窗口实例
                var mainWindow = System.Windows.Application.Current.MainWindow;
                var dialog = new Views.MemoInputDialog(dayViewModel.Date, "", mainWindow);
                if (dialog.ShowDialog() == true)
                {
                    var memo = new MemoItem
                    {
                        Content = dialog.MemoContent,
                        Date = dialog.MemoDate,
                        IsCompleted = false,
                        CreatedAt = DateTime.Now,
                        SortOrder = dayViewModel.Memos.Count
                    };

                    _databaseService.AddMemo(memo);

                    // 直接添加到对应的日期视图模型中，而不是重新加载整个日历
                    dayViewModel.Memos.Add(memo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加备忘录失败：{ex.Message}");
                System.Windows.MessageBox.Show(
                    $"添加备忘录失败：{ex.Message}\n\n请检查数据库连接是否正常。",
                    "错误",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 标记备忘录为已完成
        /// </summary>
        /// <param name="memo">备忘录</param>
        private void MarkCompleted(MemoItem? memo)
        {
            if (memo == null) return;

            try
            {
                memo.IsCompleted = true;
                memo.CompletedAt = DateTime.Now;
                _databaseService.MarkAsCompleted(memo.Id);

                // 重新排序备忘录列表，将已完成的放到未完成的后面
                ReorderMemosInDay(memo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"标记备忘录完成失败：{ex.Message}");
                // 恢复状态
                memo.IsCompleted = false;
                memo.CompletedAt = null;
            }
        }

        /// <summary>
        /// 重新排序指定日期的备忘录列表
        /// </summary>
        /// <param name="memo">备忘录</param>
        private void ReorderMemosInDay(MemoItem memo)
        {
            // 找到包含该备忘录的日期视图模型
            foreach (var day in CalendarDays)
            {
                if (day.Memos.Contains(memo))
                {
                    // 创建新的排序列表：未完成的在前，已完成的在后
                    var sortedMemos = day.Memos
                        .OrderBy(m => m.IsCompleted)  // 未完成的排在前面
                        .ThenBy(m => m.CreatedAt)     // 同状态内按创建时间排序
                        .ToList();

                    // 清空并重新添加排序后的备忘录
                    day.Memos.Clear();
                    foreach (var sortedMemo in sortedMemos)
                    {
                        day.Memos.Add(sortedMemo);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 显示备忘录详情
        /// </summary>
        /// <param name="memo">备忘录</param>
        private void ShowMemoDetail(MemoItem? memo)
        {
            if (memo == null) return;

            try
            {
                var detailWindow = new Views.MemoDetailWindow(memo, EditMemoCallback);
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    detailWindow.Owner = mainWindow;
                }
                detailWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示备忘录详情失败：{ex.Message}");
                System.Windows.MessageBox.Show(
                    $"显示备忘录详情失败：{ex.Message}",
                    "错误",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 编辑备忘录回调
        /// </summary>
        /// <param name="memo">要编辑的备忘录</param>
        private void EditMemoCallback(MemoItem memo)
        {
            try
            {
                var mainWindow = System.Windows.Application.Current.MainWindow;
                var dialog = new Views.MemoInputDialog(memo.Date, memo.Content, mainWindow);
                if (dialog.ShowDialog() == true)
                {
                    memo.Content = dialog.MemoContent;
                    memo.Date = dialog.MemoDate;

                    // 更新数据库
                    _databaseService.UpdateMemo(memo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"编辑备忘录失败：{ex.Message}");
                System.Windows.MessageBox.Show(
                    $"编辑备忘录失败：{ex.Message}",
                    "错误",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除备忘录
        /// </summary>
        /// <param name="memo">备忘录</param>
        private void DeleteMemo(MemoItem? memo)
        {
            if (memo == null) return;

            try
            {
                _databaseService.DeleteMemo(memo.Id);

                // 从所有日期视图模型中查找并删除该备忘录
                foreach (var day in CalendarDays)
                {
                    if (day.Memos.Contains(memo))
                    {
                        day.Memos.Remove(memo);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除备忘录失败：{ex.Message}");
                System.Windows.MessageBox.Show(
                    $"删除备忘录失败：{ex.Message}",
                    "错误",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 桌面可见性变化事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="isVisible">是否可见</param>
        private void OnDesktopVisibilityChanged(object? sender, bool isVisible)
        {
            IsDesktopVisible = isVisible;
        }

        /// <summary>
        /// 保存背景颜色到本地配置
        /// </summary>
        /// <param name="color">颜色字符串</param>
        private void SaveBackgroundColor(string color)
        {
            try
            {
                File.WriteAllText(_configPath, color);
            }
            catch { }
        }

        /// <summary>
        /// 读取本地配置的背景颜色
        /// </summary>
        /// <returns>颜色字符串</returns>
        private string LoadBackgroundColor()
        {
            try
            {
                if (File.Exists(_configPath))
                    return File.ReadAllText(_configPath).Trim();
            }
            catch { }
            return "#FFFFFF";
        }

        /// <summary>
        /// 属性变化事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性变化事件
        /// </summary>
        /// <param name="propertyName">属性名</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 日历日期视图模型
    /// </summary>
    public class CalendarDayViewModel : INotifyPropertyChanged
    {
        private DateTime _date;
        private bool _isCurrentMonth;
        private ObservableCollection<MemoItem> _memos;

        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否为当前月
        /// </summary>
        public bool IsCurrentMonth
        {
            get => _isCurrentMonth;
            set
            {
                _isCurrentMonth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 备忘录列表
        /// </summary>
        public ObservableCollection<MemoItem> Memos
        {
            get => _memos;
            set
            {
                _memos = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="isCurrentMonth">是否为当前月</param>
        /// <param name="memos">备忘录列表</param>
        public CalendarDayViewModel(DateTime date, bool isCurrentMonth, System.Collections.Generic.List<MemoItem>? memos = null)
        {
            _date = date;
            _isCurrentMonth = isCurrentMonth;
            _memos = new ObservableCollection<MemoItem>(memos ?? new System.Collections.Generic.List<MemoItem>());
        }

        /// <summary>
        /// 属性变化事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性变化事件
        /// </summary>
        /// <param name="propertyName">属性名</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 简单命令实现
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }

    /// <summary>
    /// 带参数的命令实现
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

        public void Execute(object? parameter) => _execute((T?)parameter);
    }
}