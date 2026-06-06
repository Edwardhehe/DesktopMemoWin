using DesktopMemo.Models;
using DesktopMemo.Services;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DesktopMemo.Views
{
    /// <summary>
    /// 侧边便签窗口。
    /// </summary>
    public partial class StickyNoteWindow : Window
    {
        private readonly StickyNoteStateService _stateService = new();
        private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
        private bool _isLoadingContent;

        public StickyNoteWindow()
        {
            _saveTimer.Tick += SaveTimer_Tick;

            InitializeComponent();

            Loaded += StickyNoteWindow_Loaded;
            Closing += StickyNoteWindow_Closing;
        }

        private void StickyNoteWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoadingContent = true;
            _stateService.LoadContent(EditorRichTextBox);
            _isLoadingContent = false;
        }

        public StickyNoteLayout GetSavedLayout()
        {
            return _stateService.LoadLayout() ?? new StickyNoteLayout();
        }

        public void SaveRelativeLayout(Window mainWindow)
        {
            if (mainWindow.Width <= 0 || mainWindow.Height <= 0)
            {
                return;
            }

            var layout = new StickyNoteLayout
            {
                WidthRatio = Width / mainWindow.Width,
                HeightRatio = Height / mainWindow.Height,
                OffsetXRatio = (Left - mainWindow.Left) / mainWindow.Width,
                OffsetYRatio = (Top - mainWindow.Top) / mainWindow.Height
            };

            _stateService.SaveLayout(layout);
        }

        private void HeaderBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Width = Math.Max(MinWidth, Width + e.HorizontalChange);
            Height = Math.Max(MinHeight, Height + e.VerticalChange);
        }

        private void EditorRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingContent)
            {
                return;
            }

            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private void SaveTimer_Tick(object? sender, EventArgs e)
        {
            _saveTimer.Stop();
            _stateService.SaveContent(EditorRichTextBox);
        }

        private void StickyNoteWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _saveTimer.Stop();
            _stateService.SaveContent(EditorRichTextBox);
        }

        private void EditorRichTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void EditorRichTextBox_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            if (e.Data.GetData(DataFormats.FileDrop) is not string[] files)
            {
                return;
            }

            foreach (var file in files)
            {
                if (IsSupportedImage(file))
                {
                    InsertImage(file);
                }
            }
        }

        private void EditorRichTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Bitmap))
            {
                if (e.DataObject.GetData(DataFormats.Bitmap) is BitmapSource bitmapSource)
                {
                    InsertImage(bitmapSource);
                    e.CancelCommand();
                }

                return;
            }

            if (e.DataObject.GetDataPresent(DataFormats.FileDrop) &&
                e.DataObject.GetData(DataFormats.FileDrop) is string[] files)
            {
                var inserted = false;
                foreach (var file in files)
                {
                    if (!IsSupportedImage(file))
                    {
                        continue;
                    }

                    InsertImage(file);
                    inserted = true;
                }

                if (inserted)
                {
                    e.CancelCommand();
                }
            }
        }

        private void InsertImage(string filePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                InsertImage(bitmap);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"插入图片失败: {ex.Message}");
            }
        }

        private void InsertImage(BitmapSource bitmapSource)
        {
            var maxWidth = Math.Max(180, EditorRichTextBox.ActualWidth - 32);
            var image = new Image
            {
                Source = bitmapSource,
                Stretch = Stretch.Uniform,
                MaxWidth = maxWidth,
                Margin = new Thickness(0, 6, 0, 6)
            };

            var container = new InlineUIContainer(image, EditorRichTextBox.CaretPosition);
            EditorRichTextBox.CaretPosition = container.ElementEnd;
        }

        private static bool IsSupportedImage(string filePath)
        {
            var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
            return extension is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp";
        }
    }
}
