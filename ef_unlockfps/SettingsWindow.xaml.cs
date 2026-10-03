using ef_unlockfps.Services;
using ef_unlockfps.Utils;
using ef_unlockfps.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ef_unlockfps
{
    /// <summary>
    /// SettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private MainViewModel _viewModel;

        private CancellationTokenSource? _cts;

        //private CancellationTokenSource? _ctsconnect;

        // 新增：定义监测器实例
        private ConnectionMonitor _connectionMonitor;

        // 无参构造器保留给设计器（仍然可用，但会创建独立 VM）
        public SettingsWindow() : this(new MainViewModel())
        {
        }

        // 新的构造器：接受共享的 MainViewModel，避免多个实例冲突   
        //public SettingsWindow()
        public SettingsWindow(MainViewModel sharedViewModel)
        {
            InitializeComponent();
            //_viewModel = new MainViewModel();
            //DataContext = _viewModel;
            _viewModel = sharedViewModel ?? new MainViewModel();
            DataContext = _viewModel;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //// 在窗口加载时检查连接状态
            //bool isConnected = CheckConnection();
            //UpdateConnectionStatus(isConnected);

            //Console.WriteLine("SettingsWindow loaded");

            // 程序化启动动画，并设置为可控（isControllable = true）
            try
            {
                var storyboard = (Storyboard)FindResource("SearchingAnimation");
                storyboard.Begin(SearchStatusText, true); // 可控，便于后续 Stop
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Failed to begin searching animation: {ex.Message}");
            }

            // 启动搜索游戏窗口
            StartSearchWindow();

            //// 启动连接状态监测
            //StartCheckConnect();

            // 初始化监测器
            _connectionMonitor = new ConnectionMonitor();
            // 【修改】启动连接状态监测
            _connectionMonitor.Start((isConnected) =>
            {
                UpdateConnectionStatus(isConnected);
            });
        }

        private void Window_OnUnloaded(object? sender, RoutedEventArgs e)
        {
            if (_cts is { } cts)
            {
                cts.Cancel();
                cts.Dispose();
                _cts = null;
            }

            //// 取消连接状态监测
            //if (_ctsconnect is { } ctsnet)
            //{
            //    ctsnet.Cancel();
            //    ctsnet.Dispose();
            //    _ctsconnect = null;
            //}

            // 【修改】停止连接监测
            _connectionMonitor.Stop();


            // 窗口卸载时确保停止动画
            try
            {
                var storyboard = (Storyboard)TryFindResource("SearchingAnimation");
                storyboard?.Stop(SearchStatusText);
            }
            catch { }
        }

        private void StartSearchWindow()
        {
            // 如果已经在搜索或未取消，则不重复启动
            if (_cts != null && !_cts.IsCancellationRequested) return;

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Task.Run(async () =>
            {
                bool found = false;

                while (!token.IsCancellationRequested)
                {
                    if (await FindWindowAsync())
                    {
                        found = true;
                        break;
                    }

                    try
                    {
                        await Task.Delay(1000, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }

                if (found && !token.IsCancellationRequested)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {

                        // 停止动画（现在由代码启动，Stop 将生效）
                        try
                        {
                            var storyboard = (Storyboard)FindResource("SearchingAnimation");
                            storyboard.Stop(SearchStatusText);
                        }
                        catch { }

                        // 更新 UI
                        SearchStatusText.Text = "Found Game!";
                        SearchStatusText.Foreground = Brushes.Green;
                        SearchStatusText.FontWeight = FontWeights.Medium;

                    });
                }
            }, token);
        }

        private async ValueTask<bool> FindWindowAsync()
        {
            IntPtr windowHandle = IntPtr.Zero;
            IntPtr processHandle = IntPtr.Zero;
            string processPath = string.Empty;

            Natives.EnumWindows((hWnd, lParam) =>
            {
                var win32Window = new Win32Window(hWnd);
                if (win32Window.ClassName != "UnityWndClass") return true;

                windowHandle = hWnd;
                var err = Natives.GetWindowThreadProcessId(hWnd, out var pid);
                if (err == 0) return true;

                processPath = Natives.GetProcessPathFromPid(pid, out processHandle);
                return false;
            }, IntPtr.Zero);

            if (windowHandle == IntPtr.Zero)
                return false;

            if (string.IsNullOrEmpty(processPath))
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        "Failed to find process path.\nPlease use \"Browse\" instead.",
                        "Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);


                    // 停止动画（现在由代码启动，Stop 将生效）
                    try
                    {
                        var storyboard = (Storyboard)FindResource("SearchingAnimation");
                        storyboard.Stop(SearchStatusText);
                    }
                    catch { }

                    // 更新 UI
                    SearchStatusText.Text = "Error";
                    SearchStatusText.Foreground = Brushes.Red;
                    SearchStatusText.FontWeight = FontWeights.Medium;

                });

                // 停止后续重复搜索
                _cts?.Cancel();
                return false;
            }

            // 获取文件名并判断游戏类型
            string fileName = Path.GetFileName(processPath).ToLowerInvariant();

            if (fileName == "endfield.exe")
            {
                string gpath = Path.GetFullPath(processPath);
                Console.WriteLine($"Game Path: {gpath}");

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    gamepathbox.Text = gpath;
                });
            }
            else if (fileName == "yuanshen1.exe" || fileName == "genshinimpact1.exe")
            {
                // 保留现有逻辑
            }
            else
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        "Unknown Game File.\nPlease use \"Browse\" instead.",
                        "Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    // 停止动画（现在由代码启动，Stop 将生效）
                    try
                    {
                        var storyboard = (Storyboard)FindResource("SearchingAnimation");
                        storyboard.Stop(SearchStatusText);
                    }
                    catch { }

                    // 更新 UI
                    SearchStatusText.Text = "Unknown Game File";
                    SearchStatusText.Foreground = Brushes.Red;
                    SearchStatusText.FontWeight = FontWeights.Medium;

                });

                // 停止后续重复搜索
                _cts?.Cancel();
                return false;
            }

            return true;
        }


        // 在你的 Window 类中添加这个方法
        public void UpdateConnectionStatus(bool isConnected)
        {
            // 确保在 UI 线程上执行（如果是从其他线程调用）
            Dispatcher.Invoke(() =>
            {
                if (isConnected)
                {
                    StatusRun.Text = "已连接";
                    StatusRun.Foreground = Brushes.Green;
                }
                else
                {
                    StatusRun.Text = "未连接";
                    StatusRun.Foreground = Brushes.Red;
                }
            });
        }

        private void Browse_Button_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                //Filter = "Text documents (.txt)|*.txt|All files (*.*)|*.*"
                Filter = "Executable files (*.exe)|*.exe|EndField.exe|EndField.exe",
                FilterIndex = 2
            };
            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                // 设置Path_EXE的Text属性为选择的文件路径
                gamepathbox.Text = openFileDialog.FileName;
            }
            else
            {

            }
        }

        private bool VerifySignerName(string filePathOrig, string expectedName)
        {
            try
            {
                // 【关键修改】去除首尾可能存在的双引号
                // 例如将 "\"C:\\Path\\To\\File.exe\"" 转换为 "C:\\Path\\To\\File.exe"
                string filePath = filePathOrig.Trim('"');

                // 检查文件是否存在（这是个好习惯，避免抛出异常影响性能）
                if (!System.IO.File.Exists(filePath))
                {
                    return false;
                }

                // 从已签名的文件中提取证书（.NET Framework 可用）
                X509Certificate cert = X509Certificate.CreateFromSignedFile(filePath);
                X509Certificate2 cert2 = new X509Certificate2(cert);

                // 证书的 Subject 格式通常为 "CN=公司名, O=组织, ..."
                // 这里简单判断是否包含目标字符串（可根据实际格式调整）
                return cert2.Subject.Contains(expectedName);
            }
            catch
            {
                // 文件不存在、未签名或读取失败
                return false;
            }
        }

        private void Apply_Button_Click(object sender, RoutedEventArgs e)
        {
            string gmpathorig = gamepathbox.Text;
            string gmpath = gmpathorig.Trim('"');

            string targetCompany = "Shanghai Hypergryph Network Technology Co., Ltd.";
            string targetCompanygl = "GRYPH FRONTIER PTE. LTD.";
            // 定义目标文件名
            string targetFileName = "EndField.exe";

            bool _complete = false;

            try
            {
                _viewModel.CustomLaunchParamCfg = Param_EXE.Text;
                _viewModel.CustomResolutionCfg = Reso_COMBOBOX.SelectedIndex;
            }
            catch (Exception ex)
            {
                // 建议在此处记录日志或处理异常，空的 catch 块会掩盖错误
            }

            // 1. 验证文件名是否正确
            // 使用 StringComparer.OrdinalIgnoreCase 忽略大小写 (例如 endfield.exe 也是合法的)
            bool isFileNameValid = string.Equals(System.IO.Path.GetFileName(gmpath), targetFileName, StringComparison.OrdinalIgnoreCase);

            if (isFileNameValid)
            {
                // 2. 文件名正确，继续验证签名
                if (VerifySignerName(gmpath, targetCompany))
                {
                    _viewModel.GamePath = gmpath;
                    _complete = true;
                }
                else if (VerifySignerName(gmpath, targetCompanygl))
                {
                    _viewModel.GamePath = gmpath;
                    _complete = true;
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "Verification failed! Incorrect file",
                        "Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            else
            {
                System.Windows.MessageBox.Show(
                    "This is not the correct game EXE file",
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            if (_complete)
            {
                this.Close();
            }
        }
    }
}