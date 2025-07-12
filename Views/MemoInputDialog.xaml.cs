using System;
using System.Windows;

namespace DesktopMemo.Views
{
    /// <summary>
    /// 备忘录输入对话框
    /// </summary>
    public partial class MemoInputDialog : Window
    {
        /// <summary>
        /// 备忘录内容
        /// </summary>
        public string MemoContent { get; private set; } = string.Empty;

        /// <summary>
        /// 备忘录日期
        /// </summary>
        public DateTime MemoDate { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="date">备忘录日期</param>
        /// <param name="existingContent">现有内容（用于编辑）</param>
        /// <param name="owner">父窗口</param>
        public MemoInputDialog(DateTime date, string existingContent = "", Window? owner = null)
        {
            InitializeComponent();

            MemoDate = date;
            DateTextBlock.Text = $"日期：{date:yyyy年MM月dd日}";

            // 如果有现有内容，设置为编辑模式
            if (!string.IsNullOrEmpty(existingContent))
            {
                ContentTextBox.Text = existingContent;
                Title = "编辑备忘录";
            }

            // 设置父窗口并调整位置
            if (owner != null)
            {
                this.Owner = owner;
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                
                // 计算对话框位置，确保在主窗口范围内
                var ownerLeft = owner.Left;
                var ownerTop = owner.Top;
                var ownerWidth = owner.Width;
                var ownerHeight = owner.Height;
                
                // 对话框位置：主窗口中心偏右下方
                this.Left = ownerLeft + (ownerWidth - this.Width) / 2 + 50;
                this.Top = ownerTop + (ownerHeight - this.Height) / 2 + 50;
                
                // 确保对话框不会超出屏幕边界
                var screenWidth = SystemParameters.WorkArea.Width;
                var screenHeight = SystemParameters.WorkArea.Height;
                
                if (this.Left + this.Width > screenWidth)
                {
                    this.Left = screenWidth - this.Width - 10;
                }
                
                if (this.Top + this.Height > screenHeight)
                {
                    this.Top = screenHeight - this.Height - 10;
                }
            }

            // 设置焦点到文本框
            Loaded += (s, e) => ContentTextBox.Focus();
        }

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var content = ContentTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("请输入备忘录内容", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                ContentTextBox.Focus();
                return;
            }

            MemoContent = content;
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
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
                DialogResult = false;
                Close();
            }
            else if (e.Key == System.Windows.Input.Key.Enter && e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                OkButton_Click(this, new RoutedEventArgs());
            }

            base.OnKeyDown(e);
        }
    }
}