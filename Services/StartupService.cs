using Microsoft.Win32;
using System;
using System.Diagnostics;
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
        /// 获取当前可执行文件的完整路径
        /// </summary>
        /// <returns>可执行文件路径</returns>
        private string GetExecutablePath()
        {
            try
            {
                // 优先使用进程路径（适用于单文件发布）
                var processPath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath))
                {
                    return processPath;
                }

                // 备用方案：使用程序集位置
                var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(assemblyLocation))
                {
                    // 如果是.dll文件，尝试找到对应的.exe文件
                    if (assemblyLocation.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        var exePath = Path.ChangeExtension(assemblyLocation, ".exe");
                        if (File.Exists(exePath))
                        {
                            return exePath;
                        }
                    }
                    return assemblyLocation;
                }

                // 最后的备用方案：使用应用程序域基目录
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var exeName = Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location) + ".exe";
                var fallbackPath = Path.Combine(baseDirectory, exeName);
                if (File.Exists(fallbackPath))
                {
                    return fallbackPath;
                }

                return assemblyLocation; // 返回原始路径作为最后的备用
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取可执行文件路径失败: {ex.Message}");
                return System.Reflection.Assembly.GetExecutingAssembly().Location;
            }
        }

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
                {
                    System.Diagnostics.Debug.WriteLine("无法打开注册表启动项键");
                    return;
                }

                if (enable)
                {
                    string exePath = GetExecutablePath();
                    System.Diagnostics.Debug.WriteLine($"设置开机启动路径: {exePath}");

                    // 确保路径用引号包围，以处理包含空格的路径
                    key.SetValue(AppName, $"\"{exePath}\"");
                    System.Diagnostics.Debug.WriteLine("开机启动设置成功");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                    System.Diagnostics.Debug.WriteLine("开机启动已禁用");
                }
            }
            catch (Exception ex)
            {
                // 记录详细的错误信息
                System.Diagnostics.Debug.WriteLine($"设置开机启动失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
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
                {
                    System.Diagnostics.Debug.WriteLine("无法打开注册表启动项键进行读取");
                    return false;
                }

                var value = key.GetValue(AppName);
                if (value != null)
                {
                    var registeredPath = value.ToString();
                    System.Diagnostics.Debug.WriteLine($"注册表中的启动路径: {registeredPath}");

                    // 验证注册的路径是否仍然有效
                    var cleanPath = registeredPath?.Trim('"');
                    if (!string.IsNullOrEmpty(cleanPath) && File.Exists(cleanPath))
                    {
                        System.Diagnostics.Debug.WriteLine("开机启动已启用且路径有效");
                        return true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("开机启动路径无效，可能需要重新设置");
                        // 路径无效时，清理无效的注册表项
                        try
                        {
                            key.DeleteValue(AppName, false);
                            System.Diagnostics.Debug.WriteLine("已清理无效的开机启动项");
                        }
                        catch (Exception cleanupEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"清理无效启动项失败: {cleanupEx.Message}");
                        }
                        return false;
                    }
                }

                System.Diagnostics.Debug.WriteLine("开机启动未设置");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检查开机启动状态失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取当前注册的启动路径
        /// </summary>
        /// <returns>注册的启动路径，如果未设置则返回null</returns>
        public string? GetRegisteredStartupPath()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupKey, false);
                var value = key?.GetValue(AppName);
                return value?.ToString()?.Trim('"');
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取注册启动路径失败: {ex.Message}");
                return null;
            }
        }
    }
}