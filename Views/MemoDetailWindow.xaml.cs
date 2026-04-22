using DesktopMemo.Models;
using DesktopMemo.ViewModels;
using System;
using System.Windows;
using System.Windows.Media;

namespace DesktopMemo.Views
{
    /// <summary>
    /// 备忘录详情窗口。
    /// </summary>
    public partial class MemoDetailWindow : Window
    {
        private MemoItem _memo;
        private readonly Action<MemoItem>? _editCallback;
        private readonly MainViewModel? _mainViewModel;

        public MemoDetailWindow(MemoItem memo, Action<MemoItem>? editCallback = null, MainViewModel? mainViewModel = null)
        {
            InitializeComponent();

            _memo = memo;
            _editCallback = editCallback;
            _mainViewModel = mainViewModel;

            LoadMemoDetails();
        }

        private void LoadMemoDetails()
        {
            ContentTextBlock.Text = _memo.Content;
            CreatedDateTextBlock.Text = _memo.CreatedAt.ToString("yyyy年MM月dd日 HH:mm:ss");
            MemoDateTextBlock.Text = _memo.Date.ToString("yyyy年MM月dd日");
            PriorityTextBlock.Text = _memo.PriorityText;
            PinnedTextBlock.Text = _memo.IsPinned ? "是" : "否";

            if (_memo.IsCompleted)
            {
                StatusTextBlock.Text = "已完成";
                StatusTextBlock.Foreground = Brushes.Green;
                CompletedDateTextBlock.Text = _memo.CompletedAt.HasValue
                    ? $"完成时间：{_memo.CompletedAt.Value:yyyy年MM月dd日 HH:mm:ss}"
                    : "完成时间：未知";
                CompletedDateTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                StatusTextBlock.Text = "未完成";
                StatusTextBlock.Foreground = Brushes.DarkOrange;
                CompletedDateTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        public void RefreshData()
        {
            if (_mainViewModel == null || _memo.Id <= 0)
            {
                LoadMemoDetails();
                return;
            }

            try
            {
                var latestMemo = _mainViewModel.GetDatabaseService().GetMemoById(_memo.Id);
                if (latestMemo == null || latestMemo.IsDeleted)
                {
                    Close();
                    return;
                }

                _memo = latestMemo;
                LoadMemoDetails();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新详情失败: {ex.Message}");
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_editCallback != null)
                {
                    _editCallback(_memo);
                    RefreshData();
                    return;
                }

                if (_mainViewModel == null)
                {
                    return;
                }

                var oldDate = _memo.Date;
                var dialog = new MemoInputDialog(_memo.Date, _memo.Content, Owner, _memo.Priority, _memo.IsPinned);
                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                _memo.Content = dialog.MemoContent;
                _memo.Date = dialog.MemoDate;
                _memo.Priority = dialog.MemoPriority;
                _memo.IsPinned = dialog.MemoIsPinned;

                _mainViewModel.GetDatabaseService().UpdateMemo(_memo);
                _mainViewModel.RefreshMemoDateChange(oldDate, _memo.Date);
                _mainViewModel.RefreshAllViews();
                RefreshData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"编辑备忘录失败: {ex.Message}");
                MessageBox.Show($"编辑备忘录失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Close();
            }
            else if (e.Key == System.Windows.Input.Key.E && e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                EditButton_Click(this, new RoutedEventArgs());
            }

            base.OnKeyDown(e);
        }
    }
}
