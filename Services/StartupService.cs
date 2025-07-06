using Microsoft.Win32;
using System;
using System.IO;

namespace DesktopMemo.Services
{
    /// <summary>
    /// 开机启动服务类
    /// </summary>
    public class StartupService
    {
        private const string StartupKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AppName = "DesktopMemo";

        /// <summary>
        /// 设置开机启动
        /// </summary>
        /// <param name="enable">是否启用开机启动</param>
        public void SetStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
                if (key == null)
                    return;

                if (enable)
                {
                    string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
            catch (Exception ex)
            {
                // 记录错误日志
                System.Diagnostics.Debug.WriteLine($"设置开机启动失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查是否已设置开机启动
        /// </summary>
        /// <returns>是否已设置开机启动</returns>
        public bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupKey, false);
                if (key == null)
                    return false;

                var value = key.GetValue(AppName);
                return value != null;
            }
            catch
            {
                return false;
            }
        }
    }
} 