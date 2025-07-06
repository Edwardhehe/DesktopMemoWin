using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DesktopMemo.Models;
using DesktopMemo.Services;

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
        /// 停止桌面监控服务
        /// </summary>
        public void StopDesktopMonitoring()
        {
            _desktopMonitorService?.StopMonitoring();
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

            // 初始化命令
            PreviousMonthCommand = new RelayCommand(PreviousMonth);
            NextMonthCommand = new RelayCommand(NextMonth);
            AddMemoCommand = new RelayCommand<CalendarDayViewModel>(AddMemo);
            MarkCompletedCommand = new RelayCommand<MemoItem>(MarkCompleted);
            DeleteMemoCommand = new RelayCommand<MemoItem>(DeleteMemo);

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
            CalendarDays.Clear();

            var firstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            var firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;

            // 添加上个月的日期
            for (int i = firstDayOfWeek - 1; i >= 0; i--)
            {
                var date = firstDayOfMonth.AddDays(-i - 1);
                var dayViewModel = new CalendarDayViewModel(date, false);
                CalendarDays.Add(dayViewModel);
            }

            // 添加当前月的日期
            for (int day = 1; day <= lastDayOfMonth.Day; day++)
            {
                var date = new DateTime(_currentMonth.Year, _currentMonth.Month, day);
                var memos = _databaseService.GetMemosByDate(date);
                var dayViewModel = new CalendarDayViewModel(date, true, memos);
                CalendarDays.Add(dayViewModel);
            }

            // 添加下个月的日期
            var remainingDays = 42 - CalendarDays.Count; // 保持6行7列的格式
            for (int i = 1; i <= remainingDays; i++)
            {
                var date = lastDayOfMonth.AddDays(i);
                var dayViewModel = new CalendarDayViewModel(date, false);
                CalendarDays.Add(dayViewModel);
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

            var dialog = new Views.MemoInputDialog(dayViewModel.Date);
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
                
                // 重新加载当前月份的数据以刷新界面
                LoadCalendarData();
            }
        }

        /// <summary>
        /// 标记备忘录为已完成
        /// </summary>
        /// <param name="memo">备忘录</param>
        private void MarkCompleted(MemoItem? memo)
        {
            if (memo == null) return;

            memo.IsCompleted = true;
            memo.CompletedAt = DateTime.Now;
            _databaseService.MarkAsCompleted(memo.Id);

            // 重新加载日历数据以更新排序
            LoadCalendarData();
        }

        /// <summary>
        /// 删除备忘录
        /// </summary>
        /// <param name="memo">备忘录</param>
        private void DeleteMemo(MemoItem? memo)
        {
            if (memo == null) return;

            _databaseService.DeleteMemo(memo.Id);
            LoadCalendarData();
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