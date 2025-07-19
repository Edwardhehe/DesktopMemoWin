using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace DesktopMemo.Diagnostics
{
    /// <summary>
    /// 开机启动功能诊断工具
    /// </summary>
    public static class StartupDiagnostic
    {
        private const string StartupKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AppName = "DesktopMemo";

        /// <summary>
        /// 运行完整的开机启动诊断
        /// </summary>
        public static void RunDiagnostic()
        {
            Console.WriteLine("=== 开机启动功能诊断 ===");
            Console.WriteLine();

            // 1. 检查当前可执行文件路径
            CheckExecutablePaths();
            Console.WriteLine();

            // 2. 检查注册表状态
            CheckRegistryStatus();
            Console.WriteLine();

            // 3. 检查权限
            CheckPermissions();
            Console.WriteLine();

            // 4. 提供修复建议
            ProvideSuggestions();
        }

        /// <summary>
        /// 检查可执行文件路径
        /// </summary>
        private static void CheckExecutablePaths()
        {
            Console.WriteLine("1. 检查可执行文件路径：");

            try
            {
                // 进程路径
                var processPath = Process.GetCurrentProcess().MainModule?.FileName;
                Console.WriteLine($"   进程路径: {processPath ?? "未找到"}");
                if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath))
                {
                    Console.WriteLine("   ✓ 进程路径有效");
                }
                else
                {
                    Console.WriteLine("   ✗ 进程路径无效");
                }

                // 程序集位置
                var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                Console.WriteLine($"   程序集位置: {assemblyLocation ?? "未找到"}");
                if (!string.IsNullOrEmpty(assemblyLocation) && File.Exists(assemblyLocation))
                {
                    Console.WriteLine("   ✓ 程序集位置有效");
                }
                else
                {
                    Console.WriteLine("   ✗ 程序集位置无效");
                }

                // 应用程序域基目录
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                Console.WriteLine($"   基目录: {baseDirectory ?? "未找到"}");
                if (Directory.Exists(baseDirectory))
                {
                    Console.WriteLine("   ✓ 基目录有效");
                }
                else
                {
                    Console.WriteLine("   ✗ 基目录无效");
                }

                // 推荐的启动路径
                var recommendedPath = GetRecommendedStartupPath();
                Console.WriteLine($"   推荐启动路径: {recommendedPath ?? "无法确定"}");
                if (!string.IsNullOrEmpty(recommendedPath) && File.Exists(recommendedPath))
                {
                    Console.WriteLine("   ✓ 推荐路径有效");
                }
                else
                {
                    Console.WriteLine("   ✗ 推荐路径无效");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✗ 检查路径时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查注册表状态
        /// </summary>
        private static void CheckRegistryStatus()
        {
            Console.WriteLine("2. 检查注册表状态：");

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupKey, false);
                if (key == null)
                {
                    Console.WriteLine("   ✗ 无法打开启动项注册表键");
                    return;
                }

                Console.WriteLine("   ✓ 成功打开启动项注册表键");

                var value = key.GetValue(AppName);
                if (value != null)
                {
                    var registeredPath = value.ToString();
                    Console.WriteLine($"   当前注册路径: {registeredPath}");

                    var cleanPath = registeredPath?.Trim('"');
                    if (!string.IsNullOrEmpty(cleanPath) && File.Exists(cleanPath))
                    {
                        Console.WriteLine("   ✓ 注册路径有效");
                    }
                    else
                    {
                        Console.WriteLine("   ✗ 注册路径无效或文件不存在");
                    }
                }
                else
                {
                    Console.WriteLine("   ℹ 未设置开机启动");
                }

                // 列出所有启动项
                Console.WriteLine("   所有启动项:");
                foreach (var valueName in key.GetValueNames())
                {
                    var valueData = key.GetValue(valueName);
                    Console.WriteLine($"     {valueName}: {valueData}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✗ 检查注册表时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查权限
        /// </summary>
        private static void CheckPermissions()
        {
            Console.WriteLine("3. 检查权限：");

            try
            {
                // 尝试打开注册表键进行写入
                using var key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
                if (key != null)
                {
                    Console.WriteLine("   ✓ 具有注册表写入权限");
                }
                else
                {
                    Console.WriteLine("   ✗ 缺少注册表写入权限");
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("   ✗ 缺少注册表写入权限");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✗ 权限检查异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 提供修复建议
        /// </summary>
        private static void ProvideSuggestions()
        {
            Console.WriteLine("4. 修复建议：");

            var recommendedPath = GetRecommendedStartupPath();
            if (string.IsNullOrEmpty(recommendedPath) || !File.Exists(recommendedPath))
            {
                Console.WriteLine("   ⚠ 无法确定有效的可执行文件路径");
                Console.WriteLine("     建议：确保程序已正确构建并部署");
                return;
            }

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupKey, false);
                var currentValue = key?.GetValue(AppName);

                if (currentValue == null)
                {
                    Console.WriteLine("   💡 建议启用开机启动");
                    Console.WriteLine($"     执行：reg add \"HKCU\\{StartupKey}\" /v {AppName} /t REG_SZ /d \"\\\"{recommendedPath}\\\"\" /f");
                }
                else
                {
                    var currentPath = currentValue.ToString()?.Trim('"');
                    if (currentPath != recommendedPath)
                    {
                        Console.WriteLine("   💡 建议更新开机启动路径");
                        Console.WriteLine($"     当前路径：{currentPath}");
                        Console.WriteLine($"     推荐路径：{recommendedPath}");
                        Console.WriteLine($"     执行：reg add \"HKCU\\{StartupKey}\" /v {AppName} /t REG_SZ /d \"\\\"{recommendedPath}\\\"\" /f");
                    }
                    else
                    {
                        Console.WriteLine("   ✓ 开机启动配置正确");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✗ 生成建议时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取推荐的启动路径
        /// </summary>
        private static string? GetRecommendedStartupPath()
        {
            try
            {
                // 优先使用进程路径
                var processPath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath))
                {
                    return processPath;
                }

                // 备用方案：使用程序集位置
                var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(assemblyLocation))
                {
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

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
