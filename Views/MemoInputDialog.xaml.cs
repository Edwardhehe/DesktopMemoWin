using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;

namespace DesktopMemo.Views
{
    /// <summary>
    /// 备忘录输入对话框
    /// </summary>
    public partial class MemoInputDialog : Window
    {
        /// <summary>
        /// 备忘录内容（纯文本）
        /// </summary>
        public string MemoContent { get; private set; } = string.Empty;

        /// <summary>
        /// 备忘录内容（富文本）
        /// </summary>
        public FlowDocument MemoRichContent { get; private set; }

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
            DatePicker.SelectedDate = date;
            DatePicker.DisplayDate = date;

            // 如果有现有内容，设置为编辑模式
            if (!string.IsNullOrEmpty(existingContent))
            {
                ContentParagraph.Inlines.Add(new Run(existingContent));
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

            // 设置焦点到富文本框
            Loaded += (s, e) => ContentRichTextBox.Focus();
        }

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取选中的日期
            if (DatePicker.SelectedDate.HasValue)
            {
                MemoDate = DatePicker.SelectedDate.Value;
            }

            // 获取富文本内容
            MemoRichContent = ContentRichTextBox.Document;

            // 提取纯文本内容用于兼容性
            var textRange = new TextRange(MemoRichContent.ContentStart, MemoRichContent.ContentEnd);
            var content = textRange.Text.Trim();

            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("请输入备忘录内容", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                ContentRichTextBox.Focus();
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

        /// <summary>
        /// 处理粘贴事件
        /// </summary>
        private void ContentRichTextBox_Paste(object sender, DataObjectEventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                // 处理图片粘贴
                var image = Clipboard.GetImage();
                if (image != null)
                {
                    var bitmapImage = new BitmapImage();
                    using (var memoryStream = new MemoryStream())
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(image));
                        encoder.Save(memoryStream);
                        memoryStream.Position = 0;

                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = memoryStream;
                        bitmapImage.EndInit();
                    }

                    var imageInline = new InlineUIContainer(new System.Windows.Controls.Image
                    {
                        Source = bitmapImage,
                        MaxWidth = 200,
                        MaxHeight = 200,
                        Margin = new Thickness(5)
                    });

                    // 在当前光标位置插入图片
                    var caretPosition = ContentRichTextBox.CaretPosition;
                    if (caretPosition != null)
                    {
                        caretPosition.Paragraph?.Inlines.Add(imageInline);
                    }

                    e.Handled = true;
                }
            }
            else if (Clipboard.ContainsText())
            {
                // 处理文本粘贴
                var text = Clipboard.GetText();
                if (!string.IsNullOrEmpty(text))
                {
                    ContentRichTextBox.Paste();
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// 处理键盘快捷键
        /// </summary>
        private void ContentRichTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.V:
                        // Ctrl+V 粘贴
                        HandlePaste();
                        e.Handled = true;
                        break;
                }
            }
        }

        /// <summary>
        /// 处理粘贴操作
        /// </summary>
        private void HandlePaste()
        {
            if (Clipboard.ContainsImage())
            {
                // 处理图片粘贴
                var image = Clipboard.GetImage();
                if (image != null)
                {
                    var bitmapImage = new BitmapImage();
                    using (var memoryStream = new MemoryStream())
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(image));
                        encoder.Save(memoryStream);
                        memoryStream.Position = 0;

                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = memoryStream;
                        bitmapImage.EndInit();
                    }

                    var imageInline = new InlineUIContainer(new System.Windows.Controls.Image
                    {
                        Source = bitmapImage,
                        MaxWidth = 200,
                        MaxHeight = 200,
                        Margin = new Thickness(5)
                    });

                    // 在当前光标位置插入图片
                    var caretPosition = ContentRichTextBox.CaretPosition;
                    if (caretPosition != null)
                    {
                        caretPosition.Paragraph?.Inlines.Add(imageInline);
                    }
                }
            }
            else if (Clipboard.ContainsText())
            {
                // 处理文本粘贴
                var text = Clipboard.GetText();
                if (!string.IsNullOrEmpty(text))
                {
                    ContentRichTextBox.Paste();
                }
            }
        }
    }
}