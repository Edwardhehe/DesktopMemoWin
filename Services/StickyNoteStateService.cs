using DesktopMemo.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace DesktopMemo.Services
{
    /// <summary>
    /// 负责保存和读取侧边便签内容与布局。
    /// </summary>
    public class StickyNoteStateService
    {
        private readonly string _appFolder;
        private readonly string _layoutPath;
        private readonly string _contentPath;

        public StickyNoteStateService()
        {
            _appFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "DesktopMemo");
            _layoutPath = Path.Combine(_appFolder, "sticky-note-layout.json");
            _contentPath = Path.Combine(_appFolder, "sticky-note-content.xamlpkg");
        }

        public StickyNoteLayout? LoadLayout()
        {
            try
            {
                if (!File.Exists(_layoutPath))
                {
                    return null;
                }

                var json = File.ReadAllText(_layoutPath);
                var layout = JsonSerializer.Deserialize<StickyNoteLayout>(json);
                if (NormalizeLegacyDefaultLayout(layout))
                {
                    SaveLayout(layout!);
                }
                return layout;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"读取便签布局失败: {ex.Message}");
                return null;
            }
        }

        public void SaveLayout(StickyNoteLayout layout)
        {
            try
            {
                EnsureAppFolder();
                var json = JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_layoutPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存便签布局失败: {ex.Message}");
            }
        }

        public void LoadContent(RichTextBox richTextBox)
        {
            try
            {
                if (!File.Exists(_contentPath))
                {
                    return;
                }

                using var stream = new FileStream(_contentPath, FileMode.Open, FileAccess.Read);
                var range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                range.Load(stream, DataFormats.XamlPackage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"读取便签内容失败: {ex.Message}");
            }
        }

        public void SaveContent(RichTextBox richTextBox)
        {
            try
            {
                EnsureAppFolder();
                using var stream = new FileStream(_contentPath, FileMode.Create, FileAccess.Write);
                var range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                range.Save(stream, DataFormats.XamlPackage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存便签内容失败: {ex.Message}");
            }
        }

        private void EnsureAppFolder()
        {
            if (!Directory.Exists(_appFolder))
            {
                Directory.CreateDirectory(_appFolder);
            }
        }

        private static bool NormalizeLegacyDefaultLayout(StickyNoteLayout? layout)
        {
            if (layout == null)
            {
                return false;
            }

            var legacyGap = Math.Abs(Math.Abs(layout.OffsetXRatio) - layout.WidthRatio);
            if (layout.OffsetXRatio < 0 &&
                legacyGap > 0.02 &&
                legacyGap < 0.04)
            {
                layout.OffsetXRatio = -layout.WidthRatio;
                return true;
            }

            return false;
        }
    }
}
