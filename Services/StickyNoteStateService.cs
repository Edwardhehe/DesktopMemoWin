using DesktopMemo.Models;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Documents;

namespace DesktopMemo.Services
{
    /// <summary>
    /// 负责保存和读取侧边便签内容与布局。
    /// </summary>
    public class StickyNoteStateService
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        private readonly string _appFolder;
        private readonly string _layoutPath;
        private readonly string _contentPath;

        public StickyNoteStateService()
        {
            _appFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "DesktopMemo");
            _layoutPath = Path.Combine(_appFolder, "sticky-note-layout.json");
            _contentPath = Path.Combine(_appFolder, "sticky-note-content.txt");
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

                var text = File.ReadAllText(_contentPath, Utf8NoBom).TrimEnd('\r', '\n');
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }

                // 清空默认段落，按行重建
                richTextBox.Document.Blocks.Clear();
                var lines = text.Replace("\r\n", "\n").Split('\n');
                foreach (var line in lines)
                {
                    richTextBox.Document.Blocks.Add(new Paragraph(new Run(line)));
                }
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

                // 逐段落提取纯文本，用 \n 分隔
                var sb = new StringBuilder();
                var i = 0;
                foreach (var block in richTextBox.Document.Blocks)
                {
                    if (block is Paragraph para)
                    {
                        var range = new TextRange(para.ContentStart, para.ContentEnd);
                        if (i > 0) sb.Append('\n');
                        sb.Append(range.Text);
                        i++;
                    }
                }

                var text = sb.ToString().TrimEnd('\r', '\n');

                // 空内容不写入文件（保留已持久化内容）
                if (text.Length == 0)
                {
                    return;
                }

                File.WriteAllText(_contentPath, text, Utf8NoBom);
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
