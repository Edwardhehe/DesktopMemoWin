using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopMemo.Services
{
    /// <summary>
    /// 桌面监控服务，用于检测用户是否在桌面
    /// </summary>
    public class DesktopMonitorService
    {
        /// <summary>
        /// 桌面可见性变化事件
        /// </summary>
        public event EventHandler<bool>? DesktopVisibilityChanged;

        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _monitoringTask;

        // Windows API 声明
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// 开始监控桌面状态
        /// </summary>
        public void StartMonitoring()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _monitoringTask = Task.Run(() => MonitorDesktop(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// 停止监控桌面状态
        /// </summary>
        public void StopMonitoring()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                
                // 等待任务完成，但设置超时时间
                if (_monitoringTask != null)
                {
                    _monitoringTask.Wait(TimeSpan.FromSeconds(2));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"停止桌面监控时发生异常: {ex.Message}");
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _monitoringTask = null;
            }
        }

        /// <summary>
        /// 监控桌面状态的核心方法
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task MonitorDesktop(CancellationToken cancellationToken)
        {
            bool lastDesktopVisible = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    bool isDesktopVisible = IsDesktopVisible();
                    
                    if (isDesktopVisible != lastDesktopVisible)
                    {
                        lastDesktopVisible = isDesktopVisible;
                        DesktopVisibilityChanged?.Invoke(this, isDesktopVisible);
                    }

                    await Task.Delay(300, cancellationToken); // 每300ms检查一次，提高响应速度
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // 忽略其他异常，继续监控
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        /// <summary>
        /// 检查桌面是否可见
        /// </summary>
        /// <returns>桌面是否可见</returns>
        private bool IsDesktopVisible()
        {
            try
            {
                // 获取桌面窗口和Shell窗口
                IntPtr desktopWindow = GetDesktopWindow();
                IntPtr shellWindow = GetShellWindow();

                // 获取当前活动窗口
                IntPtr foregroundWindow = GetForegroundWindow();
                
                if (foregroundWindow == IntPtr.Zero)
                    return true;

                // 如果当前活动窗口是桌面窗口或Shell窗口，则桌面可见
                if (foregroundWindow == desktopWindow || foregroundWindow == shellWindow)
                    return true;

                // 检查当前窗口是否为桌面相关窗口
                if (IsDesktopRelatedWindow(foregroundWindow))
                    return true;

                // 检查当前窗口是否最小化
                if (IsIconic(foregroundWindow))
                    return true;

                // 检查当前窗口是否为系统窗口或桌面相关进程
                GetWindowThreadProcessId(foregroundWindow, out uint processId);
                
                // 系统进程ID检查
                if (processId == 0 || processId == 4) // System Idle Process 或 System
                    return true;

                // 如果当前窗口是可见的且不是桌面相关窗口，则认为桌面被覆盖
                if (IsWindowVisible(foregroundWindow))
                {
                    // 检查窗口标题，如果为空或包含特定关键词，可能是桌面
                    var windowText = new System.Text.StringBuilder(256);
                    GetWindowText(foregroundWindow, windowText, 256);
                    string title = windowText.ToString().ToLower();
                    
                    if (string.IsNullOrEmpty(title) || 
                        title.Contains("desktop") ||
                        title.Contains("explorer") ||
                        title.Contains("program manager"))
                    {
                        return true;
                    }
                    
                    return false;
                }

                return true;
            }
            catch
            {
                // 如果出现异常，默认返回false
                return false;
            }
        }

        /// <summary>
        /// 检查窗口是否为桌面相关窗口
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>是否为桌面相关窗口</returns>
        private bool IsDesktopRelatedWindow(IntPtr hWnd)
        {
            try
            {
                var windowText = new System.Text.StringBuilder(256);
                GetWindowText(hWnd, windowText, 256);
                string title = windowText.ToString().ToLower();

                // 检查窗口标题是否包含桌面相关关键词
                if (string.IsNullOrEmpty(title) || 
                    title.Contains("desktop") ||
                    title.Contains("explorer") ||
                    title.Contains("program manager") ||
                    title.Contains("shell_traywnd") ||
                    title.Contains("shell_dll_defview"))
                {
                    return true;
                }

                // 检查窗口类名
                // 这里可以添加更多的桌面相关窗口类名检查

                return false;
            }
            catch
            {
                return false;
            }
        }


    }
} 