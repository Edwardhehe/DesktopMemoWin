using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using DesktopMemo.Views;
using DesktopMemo.ViewModels;
using FontStyle = System.Drawing.FontStyle;
using Application = System.Windows.Application;

namespace DesktopMemo.Services
{
    /// <summary>
    /// 系统托盘服务类
    /// </summary>
    public class SystemTrayService : IDisposable
    {
        private static readonly List<SystemTrayService> _instances = new List<SystemTrayService>();
        private NotifyIcon? _notifyIcon;
        private MainWindow? _mainWindow;
        private MainViewModel? _mainViewModel;
        private bool _disposed = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SystemTrayService()
        {
            lock (_instances)
            {
                _instances.Add(this);
            }
        }

        /// <summary>
        /// 清理所有实例
        /// </summary>
        public static void CleanupAll()
        {
            try
            {
                lock (_instances)
                {
                    var instancesToCleanup = _instances.ToList(); // 创建副本避免并发修改
                    
                    foreach (var instance in instancesToCleanup)
                    {
                        try
                        {
                            instance.Dispose();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"清理系统托盘实例时发生异常: {ex.Message}");
                        }
                    }
                    _instances.Clear();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理所有系统托盘实例时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化系统托盘
        /// </summary>
        /// <param name="mainWindow">主窗口</param>
        /// <param name="mainViewModel">主视图模型</param>
        public void Initialize(MainWindow mainWindow, MainViewModel mainViewModel)
        {
            _mainWindow = mainWindow;
            _mainViewModel = mainViewModel;

            // 创建托盘图标
            _notifyIcon = new NotifyIcon
            {
                Icon = CreateTrayIcon(),
                Text = "桌面备忘录",
                Visible = true
            };

            // 创建右键菜单
            var contextMenu = new ContextMenuStrip();
            
            // 显示/隐藏主窗口
            var toggleItem = new ToolStripMenuItem("显示/隐藏备忘录")
            {
                Font = new Font(contextMenu.Font, FontStyle.Bold)
            };
            toggleItem.Click += (s, e) => ToggleMainWindow();
            contextMenu.Items.Add(toggleItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // 设置菜单项
            var settingsItem = new ToolStripMenuItem("设置")
            {
                Image = CreateSettingsIcon()
            };
            settingsItem.Click += (s, e) => ShowSettings();
            contextMenu.Items.Add(settingsItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // 关于菜单项
            var aboutItem = new ToolStripMenuItem("关于");
            aboutItem.Click += (s, e) => ShowAbout();
            contextMenu.Items.Add(aboutItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // 退出菜单项
            var exitItem = new ToolStripMenuItem("退出")
            {
                Image = CreateExitIcon()
            };
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;

            // 双击托盘图标显示/隐藏主窗口
            _notifyIcon.DoubleClick += (s, e) => ToggleMainWindow();

            // 显示托盘提示
            //_notifyIcon.ShowBalloonTip(3000, "桌面备忘录", "程序已启动并在系统托盘中运行", ToolTipIcon.Info);
        }

        /// <summary>
        /// 创建托盘图标
        /// </summary>
        /// <returns>图标对象</returns>
        private Icon CreateTrayIcon()
        {
            // 创建一个简单的16x16图标
            var bitmap = new Bitmap(16, 16);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                
                // 绘制一个简单的备忘录图标
                using (var brush = new SolidBrush(Color.FromArgb(0, 122, 204)))
                {
                    graphics.FillRectangle(brush, 2, 2, 12, 12);
                }
                
                using (var pen = new Pen(Color.White, 1))
                {
                    graphics.DrawLine(pen, 4, 5, 12, 5);
                    graphics.DrawLine(pen, 4, 7, 12, 7);
                    graphics.DrawLine(pen, 4, 9, 10, 9);
                    graphics.DrawLine(pen, 4, 11, 8, 11);
                }
            }

            return Icon.FromHandle(bitmap.GetHicon());
        }

        /// <summary>
        /// 创建设置图标
        /// </summary>
        /// <returns>图像对象</returns>
        private Image CreateSettingsIcon()
        {
            var bitmap = new Bitmap(16, 16);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                using (var brush = new SolidBrush(Color.Gray))
                {
                    graphics.FillEllipse(brush, 6, 6, 4, 4);
                }
                using (var pen = new Pen(Color.Gray, 1))
                {
                    for (int i = 0; i < 8; i++)
                    {
                        double angle = i * Math.PI / 4;
                        int x1 = (int)(8 + 3 * Math.Cos(angle));
                        int y1 = (int)(8 + 3 * Math.Sin(angle));
                        int x2 = (int)(8 + 6 * Math.Cos(angle));
                        int y2 = (int)(8 + 6 * Math.Sin(angle));
                        graphics.DrawLine(pen, x1, y1, x2, y2);
                    }
                }
            }
            return bitmap;
        }

        /// <summary>
        /// 创建退出图标
        /// </summary>
        /// <returns>图像对象</returns>
        private Image CreateExitIcon()
        {
            var bitmap = new Bitmap(16, 16);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                using (var pen = new Pen(Color.Red, 2))
                {
                    graphics.DrawLine(pen, 4, 4, 12, 12);
                    graphics.DrawLine(pen, 12, 4, 4, 12);
                }
            }
            return bitmap;
        }

        /// <summary>
        /// 切换主窗口显示状态
        /// </summary>
        private void ToggleMainWindow()
        {
            if (_mainWindow == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_mainWindow.IsVisible)
                {
                    _mainWindow.Hide();
                }
                else
                {
                    _mainWindow.Show();
                    _mainWindow.WindowState = WindowState.Normal;
                    _mainWindow.Activate();
                }
            });
        }

        /// <summary>
        /// 显示设置窗口
        /// </summary>
        private void ShowSettings()
        {
            if (_mainWindow == null || _mainViewModel == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var settingsWindow = new SettingsWindow(_mainViewModel);
                settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                settingsWindow.ShowDialog();
            });
        }

        /// <summary>
        /// 显示关于对话框
        /// </summary>
        private void ShowAbout()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show(
                    "桌面备忘录 v1.0.0\n\n" +
                    "一个简单实用的桌面备忘录工具\n" +
                    "支持日历视图和系统托盘\n\n" +
                    "技术栈：.NET 8 WPF + SQLite\n\n" +
                    "使用说明：\n" +
                    "• 双击托盘图标显示/隐藏主窗口\n" +
                    "• 右键托盘图标打开菜单\n" +
                    "• 点击日期格子中的+号添加备忘录",
                    "关于桌面备忘录",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
        }

        /// <summary>
        /// 退出应用程序
        /// </summary>
        private void ExitApplication()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var result = System.Windows.MessageBox.Show(
                        "确定要退出桌面备忘录吗？",
                        "确认退出",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // 先清理资源，再退出应用程序
                        CleanupBeforeExit();
                        Application.Current.Shutdown();
                    }
                });
            }
            catch (Exception ex)
            {
                // 如果出现异常，强制退出
                System.Diagnostics.Debug.WriteLine($"退出应用程序时发生异常: {ex.Message}");
                try
                {
                    Application.Current.Shutdown();
                }
                catch
                {
                    // 最后的强制退出
                    Environment.Exit(0);
                }
            }
        }

        /// <summary>
        /// 退出前清理资源
        /// </summary>
        private void CleanupBeforeExit()
        {
            try
            {
                // 停止桌面监控服务
                if (_mainViewModel != null)
                {
                    _mainViewModel.StopDesktopMonitoring();
                }

                // 隐藏系统托盘图标
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                }

                // 清理所有系统托盘实例
                CleanupAll();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理资源时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 隐藏到托盘
        /// </summary>
        public void HideToTray()
        {
            if (_mainWindow != null)
            {
                _mainWindow.Hide();
            }
        }

        /// <summary>
        /// 显示托盘气泡提示
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="text">内容</param>
        /// <param name="icon">图标类型</param>
        public void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _notifyIcon?.ShowBalloonTip(3000, title, text, icon);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的具体实现
        /// </summary>
        /// <param name="disposing">是否正在释放</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    lock (_instances)
                    {
                        _instances.Remove(this);
                    }
                    
                    if (_notifyIcon != null)
                    {
                        _notifyIcon.Visible = false;
                        _notifyIcon.Dispose();
                        _notifyIcon = null;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"释放系统托盘资源时发生异常: {ex.Message}");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~SystemTrayService()
        {
            Dispose(false);
        }
    }
} 