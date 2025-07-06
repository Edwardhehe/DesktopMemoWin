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
            var exception = e.ExceptionObject as Exception;
            MessageBox.Show(
                $"应用程序发生未处理的异常：\n{exception?.Message}\n\n详细信息：\n{exception?.StackTrace}",
                "应用程序错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        /// <summary>
        /// 调度器未处理异常事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">未处理异常事件参数</param>
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"应用程序发生异常：\n{e.Exception.Message}",
                "应用程序错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            e.Handled = true;
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