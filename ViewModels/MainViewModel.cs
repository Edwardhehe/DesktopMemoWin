using DesktopMemo.Models;
using DesktopMemo.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.IO;
using System.Windows.Threading;

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
        private readonly DispatcherTimer _dateCheckTimer;
        private readonly DispatcherTimer _databaseRefreshTimer;
        private readonly FileSystemWatcher? _databaseWatcher;

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
        /// 标记未完成命令
        /// </summary>
        public ICommand MarkUncompletedCommand { get; }

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
            _dateCheckTimer?.Stop();
            _databaseRefreshTimer?.Stop();

            if (_databaseWatcher != null)
            {
                _databaseWatcher.EnableRaisingEvents = false;
                _databaseWatcher.Dispose();
            }
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
        /// 刷新日历数据
        /// </summary>
        public void RefreshCalendar()
        {
            LoadCalendarData();
        }

        public void RefreshAllViews()
        {
            LoadCalendarData();
            NotifyOpenChildWindows();
        }

        private void NotifyOpenChildWindows()
        {
            try
            {
                foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
                {
                    if (window is Views.DailyTasksWindow dailyTasksWindow)
                    {
                        dailyTasksWindow.RefreshData();
                    }
                    else if (window is Views.MemoDetailWindow memoDetailWindow)
                    {
                        memoDetailWindow.RefreshData();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"通知当日任务窗口刷新失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 刷新指定日期的日历数据
        /// </summary>
        /// <param name="date">要刷新的日期</param>
        public void RefreshDate(DateTime date)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"刷新日期：{date:yyyy-MM-dd}");

                // 找到指定日期的日历项
                var dayViewModel = CalendarDays.FirstOrDefault(d => d.Date.Date == date.Date);
                if (dayViewModel != null)
                {
                    System.Diagnostics.Debug.WriteLine($"找到日期视图模型，当前有 {dayViewModel.Memos.Count} 个备忘录");

                    // 重新加载该日期的备忘录
                    var memos = _databaseService.GetMemosByDate(date);
                    System.Diagnostics.Debug.WriteLine($"从数据库获取到 {memos.Count} 个备忘录");

                    // 优化集合操作：减少UI更新次数
                    dayViewModel.Memos.Clear();

                    // 批量添加新备忘录
                    foreach (var memo in memos)
                    {
                        dayViewModel.Memos.Add(memo);
                    }

                    // 通知UI更新（只需要一次通知）
                    dayViewModel.NotifyPropertyChanged(nameof(dayViewModel.Memos));

                    System.Diagnostics.Debug.WriteLine($"刷新完成，现在有 {dayViewModel.Memos.Count} 个备忘录");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"未找到日期 {date:yyyy-MM-dd} 的视图模型");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新指定日期时发生异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 刷新备忘录日期变更（原日期和新日期）
        /// </summary>
        /// <param name="oldDate">原日期</param>
        /// <param name="newDate">新日期</param>
        public void RefreshMemoDateChange(DateTime oldDate, DateTime newDate)
        {
            System.Diagnostics.Debug.WriteLine($"刷新备忘录日期变更：从 {oldDate:yyyy-MM-dd} 到 {newDate:yyyy-MM-dd}");

            // 刷新原日期和新日期的日历显示
            RefreshDate(oldDate);
            RefreshDate(newDate);

            // 如果新旧日期不在同一个月，刷新整个日历
            if (oldDate.Year != newDate.Year || oldDate.Month != newDate.Month)
            {
                System.Diagnostics.Debug.WriteLine($"日期跨月变更，刷新整个日历");
                // 检查是否需要切换月份
                if (newDate.Year == CurrentMonth.Year && newDate.Month == CurrentMonth.Month)
                {
                    // 新日期在当前月，不需要切换月份
                    LoadCalendarData();
                }
                else if (oldDate.Year == CurrentMonth.Year && oldDate.Month == CurrentMonth.Month)
                {
                    // 原日期在当前月，新日期不在当前月，刷新即可
                    LoadCalendarData();
                }
                else
                {
                    // 都不在当前月，也刷新一下确保数据一致性
                    LoadCalendarData();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"日期在同一月内变更");
            }
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
            MarkUncompletedCommand = new RelayCommand<MemoItem>(MarkUncompleted);
            DeleteMemoCommand = new RelayCommand<MemoItem>(DeleteMemo);

            ShowMemoDetailCommand = new RelayCommand<MemoItem>(ShowMemoDetail);

            // 订阅桌面可见性变化事件
            _desktopMonitorService.DesktopVisibilityChanged += OnDesktopVisibilityChanged;

            // 开始监控桌面状态
            _desktopMonitorService.StartMonitoring();

            // 初始化日期检查定时器
            _dateCheckTimer = new DispatcherTimer();
            _dateCheckTimer.Interval = TimeSpan.FromHours(1); // 每小时检查一次
            _dateCheckTimer.Tick += OnDateCheckTimerTick;
            _dateCheckTimer.Start();

            _databaseRefreshTimer = new DispatcherTimer();
            _databaseRefreshTimer.Interval = TimeSpan.FromMilliseconds(600);
            _databaseRefreshTimer.Tick += OnDatabaseRefreshTimerTick;

            _databaseWatcher = CreateDatabaseWatcher();

            // 加载日历数据
            LoadCalendarData();
        }

        /// <summary>
        /// 加载日历数据
        /// </summary>
        private FileSystemWatcher? CreateDatabaseWatcher()
        {
            try
            {
                var databasePath = _databaseService.GetDatabasePath();
                var directory = Path.GetDirectoryName(databasePath);
                var fileName = Path.GetFileName(databasePath);

                if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
                {
                    return null;
                }

                var watcher = new FileSystemWatcher(directory, fileName);
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime;
                watcher.IncludeSubdirectories = false;
                watcher.Changed += OnDatabaseFileChanged;
                watcher.Created += OnDatabaseFileChanged;
                watcher.Renamed += OnDatabaseFileChanged;
                watcher.EnableRaisingEvents = true;
                return watcher;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建数据库文件监听失败: {ex.Message}");
                return null;
            }
        }

        private void OnDatabaseFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                _databaseRefreshTimer.Stop();
                _databaseRefreshTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理数据库变更事件失败: {ex.Message}");
            }
        }

        private void OnDatabaseRefreshTimerTick(object? sender, EventArgs e)
        {
            _databaseRefreshTimer.Stop();
            RefreshAllViews();
        }

        private void LoadCalendarData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("开始重新加载日历数据");

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

                // 强制通知UI更新
                OnPropertyChanged(nameof(CalendarDays));

                System.Diagnostics.Debug.WriteLine($"日历数据重新加载完成，共 {collection.Count} 个日期格子");
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

                    if (memo.Date.Date == dayViewModel.Date.Date)
                    {
                        dayViewModel.Memos.Add(memo);
                        ReorderMemosInDay(memo);
                    }
                    else
                    {
                        RefreshDate(dayViewModel.Date);
                        RefreshDate(memo.Date);
                    }

                    NotifyOpenChildWindows();
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
                NotifyOpenChildWindows();
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
        /// 标记为未完成
        /// </summary>
        private void MarkUncompleted(MemoItem? memo)
        {
            if (memo == null) return;
            memo.IsCompleted = false;
            memo.CompletedAt = null;
            _databaseService.UpdateMemo(memo);
            // 重新排序，未完成的排前面
            ReorderMemosInDay(memo);
            // 通知UI刷新
            OnPropertyChanged(nameof(CalendarDays));
            NotifyOpenChildWindows();
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
                    for (int index = 0; index < sortedMemos.Count; index++)
                    {
                        var sortedMemo = sortedMemos[index];
                        sortedMemo.SortOrder = index;
                        day.Memos.Add(sortedMemo);
                    }

                    _databaseService.UpdateMemoSortOrders(sortedMemos);
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
                var detailWindow = new Views.MemoDetailWindow(memo, EditMemoCallback, this);
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
                var oldDate = memo.Date; // 保存原日期
                var mainWindow = System.Windows.Application.Current.MainWindow;
                var dialog = new Views.MemoInputDialog(memo.Date, memo.Content, mainWindow);
                if (dialog.ShowDialog() == true)
                {
                    var newDate = dialog.MemoDate;
                    memo.Content = dialog.MemoContent;
                    memo.Date = newDate;

                    // 更新数据库
                    _databaseService.UpdateMemo(memo);

                    // 刷新UI：检查日期是否改变
                    if (oldDate != newDate)
                    {
                        // 日期改变，需要刷新原日期和新日期
                        RefreshMemoDateChange(oldDate, newDate);
                    }
                    else
                    {
                        // 日期未变，只需刷新当前日期
                        RefreshDate(oldDate);
                    }

                    NotifyOpenChildWindows();
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

                NotifyOpenChildWindows();
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
        /// 日期检查定时器Tick事件处理
        /// </summary>
        private void OnDateCheckTimerTick(object? sender, EventArgs e)
        {
            // 检查是否需要更新今天的日期状态
            UpdateTodayStatus();
            
            // 如果月份发生变化，更新当前月份
            if (DateTime.Now.Month != _currentMonth.Month)
            {
                CurrentMonth = DateTime.Now;
            }
        }

        /// <summary>
        /// 更新所有日期格子的今天状态
        /// </summary>
        private void UpdateTodayStatus()
        {
            var today = DateTime.Today;
            foreach (var day in CalendarDays)
            {
                var wasToday = day.IsToday;
                day.IsToday = day.Date.Date == today;
                
                // 如果今天状态发生变化，触发属性变化通知
                if (wasToday != day.IsToday)
                {
                    // 通过重新设置属性来触发通知
                    var temp = day.IsToday;
                    day.IsToday = false;
                    day.IsToday = temp;
                }
            }
        }

        /// <summary>
        /// 保存背景颜色到本地配置
        /// </summary>
        /// <param name="color">颜色字符串</param>
        private void SaveBackgroundColor(string color)
        {
            try
            {
                // 确保配置目录存在
                var configDir = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                File.WriteAllText(_configPath, color);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置文件失败: {ex.Message}");
            }
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"读取配置文件失败: {ex.Message}");
            }
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

        /// <summary>
        /// 公开的属性变更通知方法（供外部调用）
        /// </summary>
        /// <param name="propertyName">属性名</param>
        public void NotifyPropertyChanged(string? propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
    }

    /// <summary>
    /// 日历日期视图模型
    /// </summary>
    public class CalendarDayViewModel : INotifyPropertyChanged
    {
        private DateTime _date;
        private bool _isCurrentMonth;
        private bool _isToday;
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
        /// 是否为今天
        /// </summary>
        public bool IsToday
        {
            get => _isToday;
            set
            {
                _isToday = value;
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
            _isToday = date.Date == DateTime.Today;
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

        /// <summary>
        /// 公开的属性变更通知方法（供外部调用）
        /// </summary>
        /// <param name="propertyName">属性名</param>
        public void NotifyPropertyChanged(string? propertyName = null)
        {
            OnPropertyChanged(propertyName);
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
