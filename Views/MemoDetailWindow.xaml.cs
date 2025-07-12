using DesktopMemo.Models;
using System;
using System.Windows;

namespace DesktopMemo.Views
{
    /// <summary>
    /// 备忘录详情窗口
    /// </summary>
    public partial class MemoDetailWindow : Window
    {
        private MemoItem _memo;
        private Action<MemoItem> _editCallback;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="memo">备忘录对象</param>
        /// <param name="editCallback">编辑回调函数</param>
        public MemoDetailWindow(MemoItem memo, Action<MemoItem> editCallback = null)
        {
            InitializeComponent();

            _memo = memo;
            _editCallback = editCallback;

            LoadMemoDetails();
        }

        /// <summary>
        /// 加载备忘录详情
        /// </summary>
        private void LoadMemoDetails()
        {
            if (_memo == null) return;

            // 设置内容
            ContentTextBlock.Text = _memo.Content;

            // 设置日期信息
            CreatedDateTextBlock.Text = _memo.CreatedAt.ToString("yyyy年MM月dd日 HH:mm:ss");
            MemoDateTextBlock.Text = _memo.Date.ToString("yyyy年MM月dd日");

            // 设置状态信息
            if (_memo.IsCompleted)
            {
                StatusTextBlock.Text = "已完成";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;

                if (_memo.CompletedAt.HasValue)
                {
                    CompletedDateTextBlock.Text = $"完成时间：{_memo.CompletedAt.Value:yyyy年MM月dd日 HH:mm:ss}";
                    CompletedDateTextBlock.Visibility = Visibility.Visible;
                }
            }
            else
            {
                StatusTextBlock.Text = "未完成";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
                CompletedDateTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 编辑按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_editCallback != null)
                {
                    _editCallback(_memo);
                }
                else
                {
                    // 如果没有提供编辑回调，打开编辑对话框
                    var dialog = new MemoInputDialog(_memo.Date, _memo.Content, this.Owner);
                    if (dialog.ShowDialog() == true)
                    {
                        _memo.Content = dialog.MemoContent;
                        _memo.Date = dialog.MemoDate;

                        // 重新加载详情
                        LoadMemoDetails();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"编辑备忘录时发生异常：{ex.Message}");
                MessageBox.Show($"编辑备忘录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 窗口键盘事件处理
        /// </summary>
        /// <param name="e">键盘事件参数</param>
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