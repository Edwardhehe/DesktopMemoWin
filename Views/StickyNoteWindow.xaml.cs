using DesktopMemo.Models;
using DesktopMemo.Services;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DesktopMemo.Views
{
    /// <summary>
    /// 侧边便签窗口 — 纯文本，每次编辑自动保存。
    /// </summary>
    public partial class StickyNoteWindow : Window
    {
        private readonly StickyNoteStateService _stateService = new();
        private bool _isLoadingContent;

        public StickyNoteWindow()
        {
            InitializeComponent();

            // 程序化创建 FlowDocument（避免 XAML 内联定义的序列化兼容问题）
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(0),
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#24415F")),
                LineHeight = 20
            };
            doc.Blocks.Add(new Paragraph { Margin = new Thickness(0) });
            EditorRichTextBox.Document = doc;

            Loaded += StickyNoteWindow_Loaded;
            Closing += StickyNoteWindow_Closing;
            IsVisibleChanged += StickyNoteWindow_IsVisibleChanged;
        }

        private void StickyNoteWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoadingContent = true;
            _stateService.LoadContent(EditorRichTextBox);
            _isLoadingContent = false;
        }

        private void StickyNoteWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
            {
                SaveContentNow();
            }
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

        public void SaveContentNow()
        {
            if (_isLoadingContent)
            {
                return;
            }

            _stateService.SaveContent(EditorRichTextBox);
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

            SaveContentNow();
        }

        private void StickyNoteWindow_Closing(object? sender, CancelEventArgs e)
        {
            SaveContentNow();
        }
    }
}
