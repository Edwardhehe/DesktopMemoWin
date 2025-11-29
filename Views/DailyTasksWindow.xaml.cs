using DesktopMemo.Models;
using DesktopMemo.Services;
using DesktopMemo.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DesktopMemo.Views
{
    /// <summary>
    /// 当天待办事项窗口
    /// </summary>
    public partial class DailyTasksWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly MainViewModel _mainViewModel;
        private DateTime _selectedDate;
        private ObservableCollection<MemoItem> _dailyMemos;

        /// <summary>
        /// 删除备忘录命令
        /// </summary>
        public ICommand DeleteMemoCommand { get; }

        /// <summary>
        /// 标记完成命令
        /// </summary>
        public ICommand MarkCompletedCommand { get; }

        /// <summary>
        /// 标记未完成命令
        /// </summary>
        public ICommand MarkUncompletedCommand { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="selectedDate">选中的日期</param>
        /// <param name="mainViewModel">主视图模型</param>
        public DailyTasksWindow(DateTime selectedDate, MainViewModel mainViewModel)
        {
            InitializeComponent();
            
            _selectedDate = selectedDate;
            _mainViewModel = mainViewModel;
            _databaseService = mainViewModel.GetDatabaseService();
            _dailyMemos = new ObservableCollection<MemoItem>();
            
            // 初始化命令
            DeleteMemoCommand = new RelayCommand<MemoItem>(DeleteMemo);
            MarkCompletedCommand = new RelayCommand<MemoItem>(MarkCompleted);
            MarkUncompletedCommand = new RelayCommand<MemoItem>(MarkUncompleted);
            
            // 设置窗口位置（在主界面范围内）
            SetWindowPosition();
            
            // 设置标题
            TitleTextBlock.Text = $"📅 {selectedDate:yyyy年MM月dd日} 待办事项";
            
            // 设置DataContext以便命令绑定
            this.DataContext = this;
            
            // 绑定数据源
            TasksItemsControl.ItemsSource = _dailyMemos;
            
            // 加载当天待办事项
            LoadDailyTasks();
            
            // 更新状态
            UpdateStatus();
            
            // 订阅备忘录状态变化事件
            SubscribeToMemoChanges();
        }

        /// <summary>
        /// 设置窗口位置
        /// </summary>
        private void SetWindowPosition()
        {
            // 获取主窗口位置和大小
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                var mainLeft = mainWindow.Left;
                var mainTop = mainWindow.Top;
                var mainWidth = mainWindow.Width;
                var mainHeight = mainWindow.Height;
                
                // 计算弹出窗口位置（居中显示）
                var popupLeft = mainLeft + (mainWidth - this.Width) / 2;
                var popupTop = mainTop + (mainHeight - this.Height) / 2;
                
                // 确保窗口不超出屏幕边界
                var screenWidth = SystemParameters.WorkArea.Width;
                var screenHeight = SystemParameters.WorkArea.Height;
                
                if (popupLeft + this.Width > screenWidth)
                    popupLeft = screenWidth - this.Width - 10;
                if (popupTop + this.Height > screenHeight)
                    popupTop = screenHeight - this.Height - 10;
                if (popupLeft < 0) popupLeft = 10;
                if (popupTop < 0) popupTop = 10;
                
                this.Left = popupLeft;
                this.Top = popupTop;
            }
        }

        /// <summary>
        /// 加载当天待办事项
        /// </summary>
        private void LoadDailyTasks()
        {
            try
            {
                _dailyMemos.Clear();
                
                // 从数据库获取当天所有备忘录
                var memos = _databaseService.GetMemosByDate(_selectedDate);
                
                // 按完成状态排序：未完成的在前，已完成的在后
                var sortedMemos = memos.OrderBy(m => m.IsCompleted).ThenBy(m => m.CreatedAt);
                
                foreach (var memo in sortedMemos)
                {
                    _dailyMemos.Add(memo);
                }
                
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载待办事项失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新状态显示
        /// </summary>
        private void UpdateStatus()
        {
            var totalCount = _dailyMemos.Count;
            var completedCount = _dailyMemos.Count(m => m.IsCompleted);
            var pendingCount = totalCount - completedCount;
            
            StatusTextBlock.Text = $"共 {totalCount} 个待办事项（{pendingCount} 个待完成，{completedCount} 个已完成）";
        }

        /// <summary>
        /// 添加新待办事项
        /// </summary>
        private void AddNewTask()
        {
            try
            {
                // 使用与主界面相同的添加备忘录窗口
                var dialog = new MemoInputDialog(_selectedDate, "", this);
                dialog.Topmost = true; // 设置为最上层
                if (dialog.ShowDialog() == true)
                {
                    var content = dialog.MemoContent?.Trim();
                    if (string.IsNullOrEmpty(content))
                    {
                        MessageBox.Show("请输入待办事项内容", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // 创建新的备忘录
                    var newMemo = new MemoItem
                    {
                        Content = content,
                        Date = dialog.MemoDate,
                        CreatedAt = DateTime.Now,
                        IsCompleted = false
                    };

                    // 保存到数据库
                    _databaseService.AddMemo(newMemo);

                    // 重新加载当天待办事项
                    LoadDailyTasks();

                    // 通知主界面刷新
                    _mainViewModel.RefreshCalendar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加待办事项失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 添加按钮点击事件
        /// </summary>
        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewTask();
        }



        /// <summary>
        /// 待办事项点击事件
        /// </summary>
        private void TaskItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is MemoItem memo)
            {
                // 判断是否为双击
                if (e.ClickCount == 2)
                {
                    try
                    {
                        var detailWindow = new MemoDetailWindow(memo, EditMemoCallback, _mainViewModel);
                        detailWindow.Owner = this;
                        detailWindow.Topmost = true; // 设置为最上层
                        detailWindow.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"打开备忘录详情窗口时发生异常：{ex.Message}");
                        MessageBox.Show($"打开备忘录详情失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    e.Handled = true;
                }
                else
                {
                    e.Handled = false; // 允许事件继续传播
                }
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
                var dialog = new MemoInputDialog(memo.Date, memo.Content, this);
                dialog.Topmost = true; // 设置为最上层
                if (dialog.ShowDialog() == true)
                {
                    memo.Content = dialog.MemoContent;
                    memo.Date = dialog.MemoDate;

                    // 更新数据库
                    _databaseService.UpdateMemo(memo);

                    // 强制刷新主界面和当天任务
                    _mainViewModel.RefreshCalendar();

                    // 重新加载当天待办事项
                    LoadDailyTasks();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"编辑备忘录时发生异常：{ex.Message}");
                MessageBox.Show($"编辑备忘录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除备忘录
        /// </summary>
        /// <param name="memo">要删除的备忘录</param>
        private void DeleteMemo(MemoItem? memo)
        {
            if (memo == null) return;

            try
            {
                // 从数据库删除
                _databaseService.DeleteMemo(memo.Id);

                // 从当日待办集合中删除
                if (_dailyMemos.Contains(memo))
                {
                    _dailyMemos.Remove(memo);
                }

                // 从主界面日历数据中删除
                foreach (var day in _mainViewModel.CalendarDays)
                {
                    if (day.Memos.Contains(memo))
                    {
                        day.Memos.Remove(memo);
                        break;
                    }
                }

                // 更新状态
                UpdateStatus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除备忘录失败：{ex.Message}");
                MessageBox.Show($"删除备忘录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 标记备忘录为完成
        /// </summary>
        /// <param name="memo">要标记的备忘录</param>
        private void MarkCompleted(MemoItem? memo)
        {
            if (memo == null) return;

            try
            {
                memo.IsCompleted = true;
                memo.CompletedAt = DateTime.Now;
                _databaseService.MarkAsCompleted(memo.Id);

                // 重新排序当日待办事项
                ReorderDailyMemos();
                
                // 更新状态
                UpdateStatus();
                
                // 通知主界面刷新
                _mainViewModel.RefreshCalendar();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"标记完成失败：{ex.Message}");
                MessageBox.Show($"标记完成失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 标记备忘录为未完成
        /// </summary>
        /// <param name="memo">要标记的备忘录</param>
        private void MarkUncompleted(MemoItem? memo)
        {
            if (memo == null) return;

            try
            {
                memo.IsCompleted = false;
                _databaseService.UpdateMemo(memo);
                
                // 重新排序当日待办事项
                ReorderDailyMemos();
                
                // 更新状态
                UpdateStatus();
                
                // 通知主界面刷新
                _mainViewModel.RefreshCalendar();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"标记未完成失败：{ex.Message}");
                MessageBox.Show($"标记未完成失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 重新排序当日待办事项（未完成的在前，已完成的在后）
        /// </summary>
        private void ReorderDailyMemos()
        {
            var sortedMemos = _dailyMemos.OrderBy(m => m.IsCompleted).ThenBy(m => m.CreatedAt).ToList();
            _dailyMemos.Clear();
            foreach (var memo in sortedMemos)
            {
                _dailyMemos.Add(memo);
            }
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 订阅备忘录状态变化事件
        /// </summary>
        private void SubscribeToMemoChanges()
        {
            // 为每个备忘录订阅PropertyChanged事件
            foreach (var memo in _dailyMemos)
            {
                memo.PropertyChanged += OnMemoPropertyChanged;
            }
        }

        /// <summary>
        /// 备忘录属性变化事件处理
        /// </summary>
        private void OnMemoPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MemoItem.IsCompleted))
            {
                // 当完成状态变化时，刷新主界面
                _mainViewModel.RefreshCalendar();
                UpdateStatus();
            }
        }

        /// <summary>
        /// 刷新窗口数据（公共方法供外部调用）
        /// </summary>
        public void RefreshData()
        {
            try
            {
                // 重新加载当天任务
                LoadDailyTasks();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新DailyTasksWindow数据时发生异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // 取消订阅事件
            foreach (var memo in _dailyMemos)
            {
                memo.PropertyChanged -= OnMemoPropertyChanged;
            }

            // 清理资源
            _dailyMemos?.Clear();
        }
    }

  } 