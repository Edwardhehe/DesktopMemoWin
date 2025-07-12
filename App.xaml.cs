using System;
using System.Windows;

namespace DesktopMemo
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 应用程序启动事件
        /// </summary>
        /// <param name="e">启动事件参数</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 设置应用程序异常处理
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        /// <summary>
        /// 应用程序域未处理异常事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">未处理异常事件参数</param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;

                // 记录异常信息到调试输出
                System.Diagnostics.Debug.WriteLine($"应用程序域发生未处理异常：{exception?.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈：{exception?.StackTrace}");
                Console.WriteLine($"应用程序域异常：{exception?.Message}");

                // 检查是否为严重异常
                if (e.IsTerminating)
                {
                    System.Diagnostics.Debug.WriteLine("应用程序即将终止");
                    Console.WriteLine("应用程序即将终止");
                }
            }
            catch (Exception ex)
            {
                // 如果异常处理过程中再次发生异常，只记录到调试输出
                System.Diagnostics.Debug.WriteLine($"异常处理过程中发生错误：{ex.Message}");
                Console.WriteLine($"异常处理失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 调度器未处理异常事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">未处理异常事件参数</param>
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // 立即标记为已处理，防止异常传播
            e.Handled = true;

            try
            {
                // 记录异常信息到调试输出
                System.Diagnostics.Debug.WriteLine($"应用程序发生异常：{e.Exception.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈：{e.Exception.StackTrace}");

                // 检查是否为栈溢出异常
                if (e.Exception is StackOverflowException)
                {
                    System.Diagnostics.Debug.WriteLine("检测到栈溢出异常，尝试恢复应用程序状态");
                    return;
                }

                // 使用简单的控制台输出而不是MessageBox，避免UI线程问题
                Console.WriteLine($"应用程序发生异常：{e.Exception.Message}");

                // 如果是严重的异常，可以考虑关闭应用程序
                if (e.Exception is OutOfMemoryException || e.Exception is StackOverflowException)
                {
                    System.Diagnostics.Debug.WriteLine("检测到严重异常，准备关闭应用程序");
                    // 延迟关闭，避免在异常处理过程中直接关闭
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            System.Windows.Application.Current.Shutdown();
                        }
                        catch
                        {
                            // 如果关闭失败，强制退出
                            Environment.Exit(1);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                // 如果异常处理过程中再次发生异常，只记录到调试输出
                System.Diagnostics.Debug.WriteLine($"异常处理过程中发生错误：{ex.Message}");
                Console.WriteLine($"异常处理失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 应用程序退出事件
        /// </summary>
        /// <param name="e">退出事件参数</param>
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // 清理事件订阅
                AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
                DispatcherUnhandledException -= App_DispatcherUnhandledException;

                // 清理系统托盘资源
                Services.SystemTrayService.CleanupAll();
            }
            catch (Exception ex)
            {
                // 记录退出时的异常，但不阻止退出
                System.Diagnostics.Debug.WriteLine($"应用程序退出时发生异常: {ex.Message}");
            }
            finally
            {
                base.OnExit(e);
            }
        }
    }
}