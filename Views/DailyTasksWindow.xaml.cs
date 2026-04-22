using DesktopMemo.Models;
using DesktopMemo.Services;
using DesktopMemo.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DesktopMemo.Views
{
    /// <summary>
    /// 当日任务窗口。
    /// </summary>
    public partial class DailyTasksWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly MainViewModel _mainViewModel;
        private readonly DateTime _selectedDate;
        private readonly ObservableCollection<MemoItem> _dailyMemos = new();

        public ICommand DeleteMemoCommand { get; }
        public ICommand MarkCompletedCommand { get; }
        public ICommand MarkUncompletedCommand { get; }
        public ICommand TogglePinCommand { get; }
        public ICommand SetLowPriorityCommand { get; }
        public ICommand SetMediumPriorityCommand { get; }
        public ICommand SetHighPriorityCommand { get; }

        public DailyTasksWindow(DateTime selectedDate, MainViewModel mainViewModel)
        {
            InitializeComponent();

            _selectedDate = selectedDate;
            _mainViewModel = mainViewModel;
            _databaseService = mainViewModel.GetDatabaseService();

            DeleteMemoCommand = new RelayCommand<MemoItem>(DeleteMemo);
            MarkCompletedCommand = new RelayCommand<MemoItem>(MarkCompleted);
            MarkUncompletedCommand = new RelayCommand<MemoItem>(MarkUncompleted);
            TogglePinCommand = new RelayCommand<MemoItem>(TogglePinned);
            SetLowPriorityCommand = new RelayCommand<MemoItem>(memo => SetPriority(memo, 0));
            SetMediumPriorityCommand = new RelayCommand<MemoItem>(memo => SetPriority(memo, 1));
            SetHighPriorityCommand = new RelayCommand<MemoItem>(memo => SetPriority(memo, 2));

            SetWindowPosition();
            TitleTextBlock.Text = $"当日任务 {_selectedDate:yyyy年M月d日}";
            DataContext = this;
            TasksItemsControl.ItemsSource = _dailyMemos;

            LoadDailyTasks();
            UpdateStatus();
        }

        private void SetWindowPosition()
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow == null)
            {
                return;
            }

            var popupLeft = mainWindow.Left + (mainWindow.Width - Width) / 2;
            var popupTop = mainWindow.Top + (mainWindow.Height - Height) / 2;

            var screenWidth = SystemParameters.WorkArea.Width;
            var screenHeight = SystemParameters.WorkArea.Height;

            if (popupLeft + Width > screenWidth)
            {
                popupLeft = screenWidth - Width - 10;
            }

            if (popupTop + Height > screenHeight)
            {
                popupTop = screenHeight - Height - 10;
            }

            Left = Math.Max(10, popupLeft);
            Top = Math.Max(10, popupTop);
        }

        private void LoadDailyTasks()
        {
            try
            {
                UnsubscribeFromMemoChanges();
                _dailyMemos.Clear();

                var memos = MainViewModel.SortMemoItems(_databaseService.GetMemosByDate(_selectedDate));

                foreach (var memo in memos)
                {
                    _dailyMemos.Add(memo);
                }

                SubscribeToMemoChanges();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载任务失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatus()
        {
            var totalCount = _dailyMemos.Count;
            var completedCount = _dailyMemos.Count(m => m.IsCompleted);
            var pendingCount = totalCount - completedCount;
            StatusTextBlock.Text = $"共 {totalCount} 条，未完成 {pendingCount} 条，已完成 {completedCount} 条";
        }

        private void AddNewTask()
        {
            try
            {
                var dialog = new MemoInputDialog(_selectedDate, string.Empty, this)
                {
                    Topmost = true
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                var content = dialog.MemoContent?.Trim();
                if (string.IsNullOrEmpty(content))
                {
                    MessageBox.Show("请输入任务内容", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var newMemo = new MemoItem
                {
                    Content = content,
                    Date = dialog.MemoDate,
                    CreatedAt = DateTime.Now,
                    IsCompleted = false,
                    Priority = dialog.MemoPriority,
                    IsPinned = dialog.MemoIsPinned
                };

                _databaseService.AddMemo(newMemo);
                LoadDailyTasks();
                _mainViewModel.RefreshAllViews();
                NotifySiblingDailyWindows();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加任务失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewTask();
        }

        private void TaskItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border || border.Tag is not MemoItem memo)
            {
                return;
            }

            if (e.ClickCount != 2)
            {
                return;
            }

            try
            {
                var detailWindow = new MemoDetailWindow(memo, EditMemoCallback, _mainViewModel)
                {
                    Owner = this,
                    Topmost = true
                };
                detailWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开详情失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditMemoCallback(MemoItem memo)
        {
            try
            {
                var dialog = new MemoInputDialog(memo.Date, memo.Content, this, memo.Priority, memo.IsPinned)
                {
                    Topmost = true
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                memo.Content = dialog.MemoContent;
                memo.Date = dialog.MemoDate;
                memo.Priority = dialog.MemoPriority;
                memo.IsPinned = dialog.MemoIsPinned;

                _databaseService.UpdateMemo(memo);
                _mainViewModel.RefreshAllViews();
                LoadDailyTasks();
                NotifySiblingDailyWindows();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"编辑任务失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteMemo(MemoItem? memo)
        {
            if (memo == null)
            {
                return;
            }

            _databaseService.DeleteMemo(memo.Id);
            _dailyMemos.Remove(memo);
            _mainViewModel.RefreshAllViews();
            UpdateStatus();
            NotifySiblingDailyWindows();
        }

        private void MarkCompleted(MemoItem? memo)
        {
            if (memo == null)
            {
                return;
            }

            memo.IsCompleted = true;
            memo.CompletedAt = DateTime.Now;
            _databaseService.MarkAsCompleted(memo.Id);
            ReorderDailyMemos();
            UpdateStatus();
            _mainViewModel.RefreshAllViews();
            NotifySiblingDailyWindows();
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
            ReorderDailyMemos();
            UpdateStatus();
            _mainViewModel.RefreshAllViews();
            NotifySiblingDailyWindows();
        }

        private void TogglePinned(MemoItem? memo)
        {
            if (memo == null)
            {
                return;
            }

            memo.IsPinned = !memo.IsPinned;
            _databaseService.SetPinned(memo.Id, memo.IsPinned);
            ReorderDailyMemos();
            _mainViewModel.RefreshAllViews();
            NotifySiblingDailyWindows();
        }

        private void SetPriority(MemoItem? memo, int priority)
        {
            if (memo == null)
            {
                return;
            }

            memo.Priority = priority;
            _databaseService.SetPriority(memo.Id, priority);
            ReorderDailyMemos();
            _mainViewModel.RefreshAllViews();
            NotifySiblingDailyWindows();
        }

        private void ReorderDailyMemos()
        {
            var sortedMemos = MainViewModel.SortMemoItems(_dailyMemos).ToList();

            _dailyMemos.Clear();
            for (var index = 0; index < sortedMemos.Count; index++)
            {
                var memo = sortedMemos[index];
                memo.SortOrder = index;
                _dailyMemos.Add(memo);
            }

            _databaseService.UpdateMemoSortOrders(sortedMemos);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SubscribeToMemoChanges()
        {
            foreach (var memo in _dailyMemos)
            {
                memo.PropertyChanged += OnMemoPropertyChanged;
            }
        }

        private void UnsubscribeFromMemoChanges()
        {
            foreach (var memo in _dailyMemos)
            {
                memo.PropertyChanged -= OnMemoPropertyChanged;
            }
        }

        private void OnMemoPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MemoItem.IsCompleted))
            {
                _mainViewModel.RefreshAllViews();
                UpdateStatus();
            }
        }

        public void RefreshData()
        {
            LoadDailyTasks();
        }

        private void NotifySiblingDailyWindows()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is DailyTasksWindow dailyTasksWindow && !ReferenceEquals(dailyTasksWindow, this))
                {
                    dailyTasksWindow.RefreshData();
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            UnsubscribeFromMemoChanges();
            _dailyMemos.Clear();
        }
    }
}
