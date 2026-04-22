using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DesktopMemo.Views
{
    /// <summary>
    /// 备忘录输入对话框。
    /// </summary>
    public partial class MemoInputDialog : Window
    {
        public string MemoContent { get; private set; } = string.Empty;

        public FlowDocument MemoRichContent { get; private set; } = new();

        public DateTime MemoDate { get; private set; }

        public int MemoPriority { get; private set; }

        public bool MemoIsPinned { get; private set; }

        public MemoInputDialog(
            DateTime date,
            string existingContent = "",
            Window? owner = null,
            int priority = 0,
            bool isPinned = false)
        {
            InitializeComponent();

            MemoDate = date;
            DatePicker.SelectedDate = date;
            DatePicker.DisplayDate = date;
            PriorityComboBox.SelectedIndex = Math.Clamp(priority, 0, 2);
            PinnedCheckBox.IsChecked = isPinned;

            if (!string.IsNullOrWhiteSpace(existingContent))
            {
                ContentParagraph.Inlines.Add(new Run(existingContent));
                Title = "编辑备忘录";
                TitleTextBlock.Text = "编辑备忘录";
            }

            if (owner != null)
            {
                Owner = owner;
                WindowStartupLocation = WindowStartupLocation.Manual;

                Left = owner.Left + (owner.Width - Width) / 2 + 50;
                Top = owner.Top + (owner.Height - Height) / 2 + 50;

                var screenWidth = SystemParameters.WorkArea.Width;
                var screenHeight = SystemParameters.WorkArea.Height;

                if (Left + Width > screenWidth)
                {
                    Left = screenWidth - Width - 10;
                }

                if (Top + Height > screenHeight)
                {
                    Top = screenHeight - Height - 10;
                }
            }

            Loaded += (_, _) => ContentRichTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (DatePicker.SelectedDate.HasValue)
            {
                MemoDate = DatePicker.SelectedDate.Value;
            }

            MemoRichContent = ContentRichTextBox.Document;
            MemoPriority = PriorityComboBox.SelectedIndex;
            MemoIsPinned = PinnedCheckBox.IsChecked == true;

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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
            else if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                OkButton_Click(this, new RoutedEventArgs());
            }

            base.OnKeyDown(e);
        }

        private void ContentRichTextBox_Paste(object sender, DataObjectEventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage();
                if (image != null)
                {
                    var bitmapImage = new BitmapImage();
                    using var memoryStream = new MemoryStream();
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(memoryStream);
                    memoryStream.Position = 0;

                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.EndInit();

                    var imageInline = new InlineUIContainer(new Image
                    {
                        Source = bitmapImage,
                        MaxWidth = 200,
                        MaxHeight = 200,
                        Margin = new Thickness(5)
                    });

                    var caretPosition = ContentRichTextBox.CaretPosition;
                    caretPosition?.Paragraph?.Inlines.Add(imageInline);
                    e.Handled = true;
                }
            }
            else if (Clipboard.ContainsText())
            {
                ContentRichTextBox.Paste();
                e.Handled = true;
            }
        }

        private void ContentRichTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.V)
            {
                HandlePaste();
                e.Handled = true;
            }
        }

        private void HandlePaste()
        {
            if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage();
                if (image == null)
                {
                    return;
                }

                var bitmapImage = new BitmapImage();
                using var memoryStream = new MemoryStream();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(memoryStream);
                memoryStream.Position = 0;

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();

                var imageInline = new InlineUIContainer(new Image
                {
                    Source = bitmapImage,
                    MaxWidth = 200,
                    MaxHeight = 200,
                    Margin = new Thickness(5)
                });

                var caretPosition = ContentRichTextBox.CaretPosition;
                caretPosition?.Paragraph?.Inlines.Add(imageInline);
                return;
            }

            if (Clipboard.ContainsText())
            {
                ContentRichTextBox.Paste();
            }
        }
    }
}
