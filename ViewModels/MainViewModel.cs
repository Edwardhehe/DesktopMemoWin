using DesktopMemo.Models;
using DesktopMemo.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace DesktopMemo.ViewModels
{
    /// <summary>
    /// 主视图模型。
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly DesktopMonitorService _desktopMonitorService;
        private readonly StartupService _startupService;
        private readonly DispatcherTimer _dateCheckTimer;
        private readonly DispatcherTimer _databaseRefreshTimer;
        private readonly FileSystemWatcher? _databaseWatcher;

        private readonly string _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "DesktopMemo",
            "config.txt");

        private DateTime _currentMonth;
        private bool _isDesktopVisible;
        private bool _isStartupEnabled;
        private string _backgroundColor = "#FFFFFF";
        private string _selectedViewMode = "月历";
        private string _selectedStatusFilter = "全部";
        private string _searchText = string.Empty;
        private string _listDescription = "按日期查看本月任务，适合做总览。";
        private string _emptyStateTitle = "当前没有内容";
        private string _emptyStateMessage = "可以切换视图、修改筛选，或者直接新建一条备忘录。";
        private string _listSummary = "当前为月历总览";

        public MainViewModel()
        {
            _databaseService = new DatabaseService();
            _desktopMonitorService = new DesktopMonitorService();
            _startupService = new StartupService();

            _currentMonth = DateTime.Today;
            _isStartupEnabled = _startupService.IsStartupEnabled();
            _backgroundColor = LoadBackgroundColor();

            PreviousMonthCommand = new RelayCommand(PreviousMonth);
            NextMonthCommand = new RelayCommand(NextMonth);
            AddMemoCommand = new RelayCommand<CalendarDayViewModel>(AddMemo);
            MarkCompletedCommand = new RelayCommand<MemoItem>(MarkCompleted);
            MarkUncompletedCommand = new RelayCommand<MemoItem>(MarkUncompleted);
            DeleteMemoCommand = new RelayCommand<MemoItem>(DeleteMemo);
            ShowMemoDetailCommand = new RelayCommand<MemoItem>(ShowMemoDetail);
            TogglePinCommand = new RelayCommand<MemoItem>(TogglePinned);
            SetLowPriorityCommand = new RelayCommand<MemoItem>(memo => SetPriority(memo, 0));
            SetMediumPriorityCommand = new RelayCommand<MemoItem>(memo => SetPriority(memo, 1));
            SetHighPriorityCommand = new RelayCommand<MemoItem>(memo => SetPriority(memo, 2));
            RestoreMemoCommand = new RelayCommand<MemoItem>(RestoreMemo);
            PermanentlyDeleteMemoCommand = new RelayCommand<MemoItem>(PermanentlyDeleteMemo);
            EmptyRecycleBinCommand = new RelayCommand(EmptyRecycleBin);

            _desktopMonitorService.DesktopVisibilityChanged += OnDesktopVisibilityChanged;
            _desktopMonitorService.StartMonitoring();

            _dateCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromHours(1) };
            _dateCheckTimer.Tick += OnDateCheckTimerTick;
            _dateCheckTimer.Start();

            _databaseRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            _databaseRefreshTimer.Tick += OnDatabaseRefreshTimerTick;
            _databaseWatcher = CreateDatabaseWatcher();

            LoadCalendarData();
            LoadFilteredMemos();
        }

        public ObservableCollection<CalendarDayViewModel> CalendarDays { get; } = new();
        public ObservableCollection<MemoItem> FilteredMemos { get; } = new();
        public ObservableCollection<string> ViewModes { get; } = new(new[] { "月历", "今日", "逾期", "本周", "回收站" });
        public ObservableCollection<string> StatusFilters { get; } = new(new[] { "全部", "未完成", "已完成", "置顶", "高优先级" });

        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand AddMemoCommand { get; }
        public ICommand MarkCompletedCommand { get; }
        public ICommand MarkUncompletedCommand { get; }
        public ICommand DeleteMemoCommand { get; }
        public ICommand ShowMemoDetailCommand { get; }
        public ICommand TogglePinCommand { get; }
        public ICommand SetLowPriorityCommand { get; }
        public ICommand SetMediumPriorityCommand { get; }
        public ICommand SetHighPriorityCommand { get; }
        public ICommand RestoreMemoCommand { get; }
        public ICommand PermanentlyDeleteMemoCommand { get; }
        public ICommand EmptyRecycleBinCommand { get; }

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

        public bool IsDesktopVisible
        {
            get => _isDesktopVisible;
            set
            {
                _isDesktopVisible = value;
                OnPropertyChanged();
            }
        }

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

        public string SelectedViewMode
        {
            get => _selectedViewMode;
            set
            {
                if (_selectedViewMode == value)
                {
                    return;
                }

                _selectedViewMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMonthView));
                OnPropertyChanged(nameof(IsListView));
                OnPropertyChanged(nameof(IsRecycleBinView));
                OnPropertyChanged(nameof(HasFilteredMemos));
                OnPropertyChanged(nameof(IsFilteredMemosEmpty));
                LoadFilteredMemos();
            }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (_selectedStatusFilter == value)
                {
                    return;
                }

                _selectedStatusFilter = value;
                OnPropertyChanged();
                LoadFilteredMemos();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                {
                    return;
                }

                _searchText = value;
                OnPropertyChanged();
                LoadFilteredMemos();
            }
        }

        public string ListSummary
        {
            get => _listSummary;
            private set
            {
                _listSummary = value;
                OnPropertyChanged();
            }
        }

        public bool IsMonthView => SelectedViewMode == "月历";
        public bool IsListView => !IsMonthView;
        public bool IsRecycleBinView => SelectedViewMode == "回收站";

        public string ListDescription
        {
            get => _listDescription;
            private set
            {
                _listDescription = value;
                OnPropertyChanged();
            }
        }

        public string EmptyStateTitle
        {
            get => _emptyStateTitle;
            private set
            {
                _emptyStateTitle = value;
                OnPropertyChanged();
            }
        }

        public string EmptyStateMessage
        {
            get => _emptyStateMessage;
            private set
            {
                _emptyStateMessage = value;
                OnPropertyChanged();
            }
        }

        public bool HasFilteredMemos => FilteredMemos.Count > 0;
        public bool IsFilteredMemosEmpty => !HasFilteredMemos;

        public DatabaseService GetDatabaseService()
        {
            return _databaseService;
        }

        public void StopDesktopMonitoring()
        {
            _desktopMonitorService.StopMonitoring();
            _dateCheckTimer.Stop();
            _databaseRefreshTimer.Stop();

            if (_databaseWatcher != null)
            {
                _databaseWatcher.EnableRaisingEvents = false;
                _databaseWatcher.Dispose();
            }
        }

        public void RefreshCalendar()
        {
            LoadCalendarData();
        }

        public void RefreshAllViews()
        {
            LoadCalendarData();
            LoadFilteredMemos();
            NotifyOpenChildWindows();
        }

        public void RefreshDate(DateTime date)
        {
            if (date.Year == CurrentMonth.Year && date.Month == CurrentMonth.Month)
            {
                LoadCalendarData();
            }

            LoadFilteredMemos();
        }

        public void RefreshMemoDateChange(DateTime oldDate, DateTime newDate)
        {
            if (oldDate.Year != newDate.Year || oldDate.Month != newDate.Month)
            {
                LoadCalendarData();
            }
            else
            {
                RefreshDate(newDate);
            }

            LoadFilteredMemos();
        }

        private void NotifyOpenChildWindows()
        {
            try
            {
                foreach (Window window in Application.Current.Windows)
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
                System.Diagnostics.Debug.WriteLine($"通知子窗口刷新失败: {ex.Message}");
            }
        }

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

                var watcher = new FileSystemWatcher(directory, fileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };

                watcher.Changed += OnDatabaseFileChanged;
                watcher.Created += OnDatabaseFileChanged;
                watcher.Renamed += OnDatabaseFileChanged;
                return watcher;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建数据库监听失败: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"处理数据库变更失败: {ex.Message}");
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
                CalendarDays.Clear();

                var firstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                var firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;

                for (var i = firstDayOfWeek - 1; i >= 0; i--)
                {
                    var date = firstDayOfMonth.AddDays(-i - 1);
                    CalendarDays.Add(new CalendarDayViewModel(date, false));
                }

                for (var day = 1; day <= lastDayOfMonth.Day; day++)
                {
                    var date = new DateTime(_currentMonth.Year, _currentMonth.Month, day);
                    var memos = _databaseService.GetMemosByDate(date);
                    CalendarDays.Add(new CalendarDayViewModel(date, true, memos));
                }

                var remainingDays = 42 - CalendarDays.Count;
                for (var i = 1; i <= remainingDays; i++)
                {
                    var date = lastDayOfMonth.AddDays(i);
                    CalendarDays.Add(new CalendarDayViewModel(date, false));
                }

                OnPropertyChanged(nameof(CalendarDays));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载日历数据失败: {ex.Message}");
            }
        }

        private void LoadFilteredMemos()
        {
            IEnumerable<MemoItem> memos = GetMemosForCurrentView();
            memos = ApplySearchAndStatusFilters(memos);
            memos = SortMemos(memos);

            FilteredMemos.Clear();
            foreach (var memo in memos)
            {
                FilteredMemos.Add(memo);
            }

            ListSummary = BuildListSummary();
            ListDescription = BuildListDescription();
            BuildEmptyStateContent();
            OnPropertyChanged(nameof(HasFilteredMemos));
            OnPropertyChanged(nameof(IsFilteredMemosEmpty));
        }

        private IEnumerable<MemoItem> GetMemosForCurrentView()
        {
            var today = DateTime.Today;

            return SelectedViewMode switch
            {
                "今日" => _databaseService.GetAllMemos().Where(m => m.Date.Date == today),
                "逾期" => _databaseService.GetAllMemos().Where(m => !m.IsCompleted && m.Date.Date < today),
                "本周" => GetThisWeekMemos(),
                "回收站" => _databaseService.GetRecycleBinMemos(),
                _ => Enumerable.Empty<MemoItem>()
            };
        }

        private IEnumerable<MemoItem> GetThisWeekMemos()
        {
            var today = DateTime.Today;
            var offset = today.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)today.DayOfWeek - 1;
            var weekStart = today.AddDays(-offset);
            var weekEnd = weekStart.AddDays(6);

            return _databaseService.GetAllMemos()
                .Where(m => m.Date.Date >= weekStart && m.Date.Date <= weekEnd);
        }

        private IEnumerable<MemoItem> ApplySearchAndStatusFilters(IEnumerable<MemoItem> memos)
        {
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                memos = memos.Where(m => m.Content.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            memos = SelectedStatusFilter switch
            {
                "未完成" => memos.Where(m => !m.IsCompleted),
                "已完成" => memos.Where(m => m.IsCompleted),
                "置顶" => memos.Where(m => m.IsPinned),
                "高优先级" => memos.Where(m => m.Priority >= 2),
                _ => memos
            };

            return memos;
        }

        public static IOrderedEnumerable<MemoItem> SortMemoItems(IEnumerable<MemoItem> memos)
        {
            return memos
                .OrderBy(m => m.IsCompleted)
                .ThenByDescending(m => m.IsPinned)
                .ThenByDescending(m => m.Priority)
                .ThenBy(m => m.Date)
                .ThenBy(m => m.SortOrder)
                .ThenBy(m => m.CreatedAt);
        }

        private static IEnumerable<MemoItem> SortMemos(IEnumerable<MemoItem> memos)
        {
            return SortMemoItems(memos);
        }

        private string BuildListSummary()
        {
            if (IsMonthView)
            {
                var monthMemos = _databaseService.GetAllMemos().Count(m => m.Date.Year == CurrentMonth.Year && m.Date.Month == CurrentMonth.Month);
                return $"{CurrentMonth:yyyy年M月} 共 {monthMemos} 条备忘录";
            }

            if (IsRecycleBinView)
            {
                return $"回收站中 {FilteredMemos.Count} 条";
            }

            var pending = FilteredMemos.Count(m => !m.IsCompleted);
            return $"{SelectedViewMode} {FilteredMemos.Count} 条，其中 {pending} 条未完成";
        }

        private string BuildListDescription()
        {
            if (IsMonthView)
            {
                return "按日期查看本月任务，适合做总览。";
            }

            if (IsRecycleBinView)
            {
                return "这里存放已删除内容。恢复后会回到正常列表，彻底删除后无法找回。";
            }

            return SelectedViewMode switch
            {
                "今日" => "聚焦今天的事项，适合快速处理。",
                "逾期" => "这里只显示已经过期但尚未完成的内容，建议优先清理。",
                "本周" => "按本周范围查看任务，适合做短周期安排。",
                _ => "当前列表已按置顶、完成状态和优先级排序。"
            };
        }

        private void BuildEmptyStateContent()
        {
            if (IsRecycleBinView)
            {
                EmptyStateTitle = "回收站是空的";
                EmptyStateMessage = string.IsNullOrWhiteSpace(SearchText)
                    ? "删除后的备忘录会暂存在这里，清空前都可以恢复。"
                    : "当前搜索条件下没有找到已删除备忘录，可以清空关键词再看。";
                return;
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                EmptyStateTitle = "没有匹配结果";
                EmptyStateMessage = $"没有找到包含“{SearchText.Trim()}”的备忘录，可以换个关键词试试。";
                return;
            }

            EmptyStateTitle = SelectedViewMode switch
            {
                "今日" => "今天没有待处理内容",
                "逾期" => "没有逾期事项",
                "本周" => "本周列表是空的",
                _ => "当前没有内容"
            };

            EmptyStateMessage = SelectedStatusFilter switch
            {
                "未完成" => "当前筛选为未完成，说明这个视图下暂时没有待办。",
                "已完成" => "当前筛选为已完成，说明这个视图下还没有完成记录。",
                "置顶" => "当前筛选为置顶，可以先给重要事项加上置顶。",
                "高优先级" => "当前筛选为高优先级，可以在编辑框或右键菜单里调整优先级。",
                _ => "可以直接在月历里新增备忘录，或切换到其他视图查看。"
            };
        }

        private void PreviousMonth()
        {
            CurrentMonth = CurrentMonth.AddMonths(-1);
        }

        private void NextMonth()
        {
            CurrentMonth = CurrentMonth.AddMonths(1);
        }

        private void AddMemo(CalendarDayViewModel? dayViewModel)
        {
            if (dayViewModel == null || !dayViewModel.IsCurrentMonth)
            {
                return;
            }

            try
            {
                var dialog = new Views.MemoInputDialog(dayViewModel.Date, string.Empty, Application.Current.MainWindow);
                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                var memo = new MemoItem
                {
                    Content = dialog.MemoContent,
                    Date = dialog.MemoDate,
                    IsCompleted = false,
                    CreatedAt = DateTime.Now,
                    SortOrder = dayViewModel.Memos.Count,
                    Priority = dialog.MemoPriority,
                    IsPinned = dialog.MemoIsPinned
                };

                _databaseService.AddMemo(memo);
                RefreshAllViews();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加备忘录失败: {ex.Message}");
                MessageBox.Show($"添加备忘录失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MarkCompleted(MemoItem? memo)
        {
            if (memo == null)
            {
                return;
            }

            try
            {
                _databaseService.MarkAsCompleted(memo.Id);
                memo.IsCompleted = true;
                memo.CompletedAt = DateTime.Now;
                RefreshAllViews();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"标记完成失败: {ex.Message}");
                memo.IsCompleted = false;
                memo.CompletedAt = null;
            }
        }

        private void MarkUncompleted(MemoItem? memo)
        {
            if (memo == null)
            {
                return;
            }

            memo.IsCompleted = false;
            memo.CompletedAt = null;
            _databaseService.UpdateMemo(memo);
            RefreshAllViews();
        }

        private void TogglePinned(MemoItem? memo)
        {
            if (memo == null)
            {
                return;
            }

            memo.IsPinned = !memo.IsPinned;
            _databaseService.SetPinned(memo.Id, memo.IsPinned);
            RefreshAllViews();
        }

        private void SetPriority(MemoItem? memo, int priority)
        {
            if (memo == null)
            {
                return;
            }

            memo.Priority = priority;
            _databaseService.SetPriority(memo.Id, priority);
            RefreshAllViews();
        }

        private void RestoreMemo(MemoItem? memo)
        {
            if (memo == null)
            {
                return;
            }

            _databaseService.RestoreMemo(memo.Id);
            RefreshAllViews();
        }

        private void PermanentlyDeleteMemo(MemoItem? memo)
        {
            if (memo == null)
            {
                return;
            }

            _databaseService.PermanentlyDeleteMemo(memo.Id);
            RefreshAllViews();
        }

        private void EmptyRecycleBin()
        {
            try
            {
                _databaseService.PurgeDeletedMemos();
                RefreshAllViews();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"清空回收站失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowMemoDetail(MemoItem? memo)
        {
            if (memo == null)
            {
                return;
            }

            try
            {
                var detailWindow = new Views.MemoDetailWindow(memo, EditMemoCallback, this);
                if (Application.Current.MainWindow != null)
                {
                    detailWindow.Owner = Application.Current.MainWindow;
                }

                detailWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示备忘录详情失败: {ex.Message}");
                MessageBox.Show($"显示备忘录详情失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditMemoCallback(MemoItem memo)
        {
            try
            {
                var oldDate = memo.Date;
                var dialog = new Views.MemoInputDialog(
                    memo.Date,
                    memo.Content,
                    Application.Current.MainWindow,
                    memo.Priority,
                    memo.IsPinned);

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                memo.Content = dialog.MemoContent;
                memo.Date = dialog.MemoDate;
                memo.Priority = dialog.MemoPriority;
                memo.IsPinned = dialog.MemoIsPinned;

                _databaseService.UpdateMemo(memo);

                if (oldDate.Date != memo.Date.Date)
                {
                    RefreshMemoDateChange(oldDate, memo.Date);
                }
                else
                {
                    RefreshDate(memo.Date);
                }

                RefreshAllViews();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"编辑备忘录失败: {ex.Message}");
                MessageBox.Show($"编辑备忘录失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteMemo(MemoItem? memo)
        {
            if (memo == null)
            {
                return;
            }

            try
            {
                _databaseService.DeleteMemo(memo.Id);
                RefreshAllViews();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除备忘录失败: {ex.Message}");
                MessageBox.Show($"删除备忘录失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnDesktopVisibilityChanged(object? sender, bool isVisible)
        {
            IsDesktopVisible = isVisible;
        }

        private void OnDateCheckTimerTick(object? sender, EventArgs e)
        {
            UpdateTodayStatus();
            LoadFilteredMemos();

            if (DateTime.Today.Month != _currentMonth.Month || DateTime.Today.Year != _currentMonth.Year)
            {
                CurrentMonth = DateTime.Today;
            }
        }

        private void UpdateTodayStatus()
        {
            var today = DateTime.Today;
            foreach (var day in CalendarDays)
            {
                day.IsToday = day.Date.Date == today;
            }
        }

        private void SaveBackgroundColor(string color)
        {
            try
            {
                var configDir = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                File.WriteAllText(_configPath, color);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置失败: {ex.Message}");
            }
        }

        private string LoadBackgroundColor()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    return File.ReadAllText(_configPath).Trim();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"读取配置失败: {ex.Message}");
            }

            return "#FFFFFF";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void NotifyPropertyChanged(string? propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
    }

    /// <summary>
    /// 日历单元格视图模型。
    /// </summary>
    public class CalendarDayViewModel : INotifyPropertyChanged
    {
        private DateTime _date;
        private bool _isCurrentMonth;
        private bool _isToday;
        private ObservableCollection<MemoItem> _memos;

        public CalendarDayViewModel(DateTime date, bool isCurrentMonth, List<MemoItem>? memos = null)
        {
            _date = date;
            _isCurrentMonth = isCurrentMonth;
            _isToday = date.Date == DateTime.Today;
            _memos = new ObservableCollection<MemoItem>(memos ?? new List<MemoItem>());
        }

        public DateTime Date
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChanged();
            }
        }

        public bool IsCurrentMonth
        {
            get => _isCurrentMonth;
            set
            {
                _isCurrentMonth = value;
                OnPropertyChanged();
            }
        }

        public bool IsToday
        {
            get => _isToday;
            set
            {
                _isToday = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<MemoItem> Memos
        {
            get => _memos;
            set
            {
                _memos = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void NotifyPropertyChanged(string? propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
    }

    /// <summary>
    /// 无参数命令。
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
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }

    /// <summary>
    /// 泛型命令。
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
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

        public void Execute(object? parameter) => _execute((T?)parameter);
    }
}
