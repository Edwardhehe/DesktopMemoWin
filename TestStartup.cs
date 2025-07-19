using System;
using DesktopMemo.Services;
using DesktopMemo.Diagnostics;

namespace DesktopMemo.Testing
{
    /// <summary>
    /// 开机启动功能测试程序
    /// </summary>
    public class TestStartup
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("桌面备忘录 - 开机启动功能测试");
            Console.WriteLine("================================");
            Console.WriteLine();

            var startupService = new StartupService();

            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    case "enable":
                        TestEnableStartup(startupService);
                        break;
                    case "disable":
                        TestDisableStartup(startupService);
                        break;
                    case "status":
                        TestCheckStatus(startupService);
                        break;
                    case "diagnostic":
                        StartupDiagnostic.RunDiagnostic();
                        break;
                    default:
                        ShowUsage();
                        break;
                }
            }
            else
            {
                RunInteractiveTest(startupService);
            }

            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        private static void RunInteractiveTest(StartupService startupService)
        {
            while (true)
            {
                Console.WriteLine("请选择操作：");
                Console.WriteLine("1. 检查开机启动状态");
                Console.WriteLine("2. 启用开机启动");
                Console.WriteLine("3. 禁用开机启动");
                Console.WriteLine("4. 运行诊断");
                Console.WriteLine("5. 退出");
                Console.Write("请输入选择 (1-5): ");

                var choice = Console.ReadLine();
                Console.WriteLine();

                switch (choice)
                {
                    case "1":
                        TestCheckStatus(startupService);
                        break;
                    case "2":
                        TestEnableStartup(startupService);
                        break;
                    case "3":
                        TestDisableStartup(startupService);
                        break;
                    case "4":
                        StartupDiagnostic.RunDiagnostic();
                        break;
                    case "5":
                        return;
                    default:
                        Console.WriteLine("无效选择，请重试。");
                        break;
                }

                Console.WriteLine();
                Console.WriteLine("按任意键继续...");
                Console.ReadKey();
                Console.Clear();
            }
        }

        private static void TestCheckStatus(StartupService startupService)
        {
            Console.WriteLine("=== 检查开机启动状态 ===");
            
            try
            {
                bool isEnabled = startupService.IsStartupEnabled();
                Console.WriteLine($"开机启动状态: {(isEnabled ? "已启用" : "未启用")}");

                if (isEnabled)
                {
                    var registeredPath = startupService.GetRegisteredStartupPath();
                    Console.WriteLine($"注册路径: {registeredPath ?? "未知"}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查状态失败: {ex.Message}");
            }
        }

        private static void TestEnableStartup(StartupService startupService)
        {
            Console.WriteLine("=== 启用开机启动 ===");
            
            try
            {
                Console.WriteLine("正在启用开机启动...");
                startupService.SetStartup(true);
                
                // 验证设置是否成功
                bool isEnabled = startupService.IsStartupEnabled();
                if (isEnabled)
                {
                    Console.WriteLine("✓ 开机启动已成功启用");
                    var registeredPath = startupService.GetRegisteredStartupPath();
                    Console.WriteLine($"注册路径: {registeredPath}");
                }
                else
                {
                    Console.WriteLine("✗ 开机启动启用失败");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启用开机启动失败: {ex.Message}");
            }
        }

        private static void TestDisableStartup(StartupService startupService)
        {
            Console.WriteLine("=== 禁用开机启动 ===");
            
            try
            {
                Console.WriteLine("正在禁用开机启动...");
                startupService.SetStartup(false);
                
                // 验证设置是否成功
                bool isEnabled = startupService.IsStartupEnabled();
                if (!isEnabled)
                {
                    Console.WriteLine("✓ 开机启动已成功禁用");
                }
                else
                {
                    Console.WriteLine("✗ 开机启动禁用失败");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"禁用开机启动失败: {ex.Message}");
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("用法:");
            Console.WriteLine("  TestStartup.exe                - 运行交互式测试");
            Console.WriteLine("  TestStartup.exe enable         - 启用开机启动");
            Console.WriteLine("  TestStartup.exe disable        - 禁用开机启动");
            Console.WriteLine("  TestStartup.exe status         - 检查状态");
            Console.WriteLine("  TestStartup.exe diagnostic     - 运行诊断");
        }
    }
}
