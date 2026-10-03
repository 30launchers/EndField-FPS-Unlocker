using ef_unlockfps.Services;
using ef_unlockfps.ViewModels;
using System.Diagnostics;
using System.IO; // 需要引用此命名空间以使用 File.Exists
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace ef_unlockfps
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        private NotifyIcon notifyIcon;

        // 新增：定义监测器实例
        private ConnectionMonitor _connectionMonitor;

        // 1. 定义 CTS 对象
        private CancellationTokenSource _ctsmainloop;

        private bool _isautolaunch = false;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // 初始化 NotifyIcon
            InitializeNotifyIcon();

            // 根据 AutoStart 设置决定是否启动时最小化到托盘
            if (_viewModel.AutoStart)
            {
                _isautolaunch = true;
                this.Hide(); // 隐藏窗口并显示托盘图标
                notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                notifyIcon.BalloonTipTitle = "EndField FPS Unlocker";
                notifyIcon.BalloonTipText = "Minimized to tray";
                notifyIcon.ShowBalloonTip(3000);
                // 直接调用启动逻辑
                start_but_Click(null, null);
            }

            Task.Run(() =>
            {
                //test1();
                //MainLoop();
            });

            // 2. 初始化 CTS
            _ctsmainloop = new CancellationTokenSource();
            // 3. 将 Token 传递给 Task 和 MainLoop
            Task.Run(() => MainLoop(_ctsmainloop.Token), _ctsmainloop.Token);

        }

        // 4. 方法签名接收 CancellationToken
        private void MainLoop(CancellationToken token)
        {
            try
            {
                // 5. 使用 IsCancellationRequested 判断，或使用 ThrowIfCancellationRequested
                while (!token.IsCancellationRequested)
                {
                    if (_needdisplaymainwindow == 1)
                    {
                        // 显示窗口的操作必须在 UI 线程上执行
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            // 检查窗口是否已经可见
                            // 如果窗口已经显示，则不执行任何操作
                            if (this.IsVisible)
                                return;

                            // 显式计算并设置窗口位置为屏幕中心（在恢复显示前设置）
                            // 确保使用 Manual 否则 Left/Top 可能被忽略
                            this.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;

                            // 使用 ActualWidth/ActualHeight（若为 0 则回退到 Width/Height）
                            double w = (this.ActualWidth > 0) ? this.ActualWidth : this.Width;
                            double h = (this.ActualHeight > 0) ? this.ActualHeight : this.Height;

                            // 使用工作区而非整个屏幕，避免遮挡任务栏
                            var wa = SystemParameters.WorkArea;
                            double left = wa.Left + (wa.Width - w) / 2;
                            double top = wa.Top + (wa.Height - h) / 2;

                            // 防护：如果计算结果是 NaN 或 Infinity，则不设置
                            if (!double.IsNaN(left) && !double.IsInfinity(left))
                                this.Left = left;
                            if (!double.IsNaN(top) && !double.IsInfinity(top))
                                this.Top = top;

                            // 显示窗口并恢复状态，激活窗口
                            this.Show();
                            this.WindowState = WindowState.Normal;
                            // 激活窗口以确保获取焦点并置于最前
                            try
                            {
                                this.Activate();
                            }
                            catch { }

                        });

                        // 清除图标残留
                        if(_viewModel.AutoCloseCfg == true)
                        {
                            CleanupNotifyIcon();
                            Environment.Exit(0);
                        }


                        _needdisplaymainwindow = 0; // 重置标志
                    }

                    if (_needexit == 1)
                    {
                        // 清除图标残留
                        if (_viewModel.AutoCloseCfg == true)
                        {
                            CleanupNotifyIcon();
                            Environment.Exit(0);
                        }
                    }

                    // 推荐：如果循环体执行很快，加一点 Delay 防止 CPU 占用过高
                    Task.Delay(300, token).Wait();
                }
            }
            catch (OperationCanceledException)
            {
                // 任务被取消是正常行为，通常不需要处理，或者记录日志
                //System.Diagnostics.Debug.WriteLine("MainLoop 已停止");
            }
        }

        // 控制是否需要显示主窗口的标志，初始值为0（不需要显示）
        private static int _needdisplaymainwindow = 0;

        private static int _needexit= 0;

        // 
        public static void needdisplayState(int needdisplaymainwindow, int needexit)
        {
            if (needdisplaymainwindow == 1)
            {
                _needdisplaymainwindow = 1;
            }
            if (needexit == 1)
            {
                _needexit = 1;
            }
        }

        private static int _alreadyexited = 0;

        // 
        public static void exitedstate(int needdisplaymainwindow)
        {
            _alreadyexited = 1;
        }

        private void sli_main_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int fps_minvalue = 20;
            if (e.NewValue < fps_minvalue)
            {
                ((Slider)sender).Value = fps_minvalue;
                return;
            }

            // 同步FPS值到notifyIcon图标的提示文本
            if (notifyIcon != null)
            {
                notifyIcon.Text = "EF unlocker (FPS: " + e.NewValue + ")";
            }
        }

        private void test1()
        {
            while (true)
            {
                if (_viewModel != null)
                {
                    int fps = _viewModel.Fps;
                    int sliderValue = _viewModel.SliderValue;

                    // 使用 fps 值
                    Console.WriteLine($"当前 FPS: {fps}, Slider: {sliderValue}");
                }

                Thread.Sleep(300);
            }
        }

        private void Setiings_Item_Click(object sender, RoutedEventArgs e)
        {
            //SettingsWindow windowab = new SettingsWindow();
            // 传入共享的 viewmodel，避免创建多个独立的配置实例
            SettingsWindow windowab = new SettingsWindow(_viewModel);

            // 设置模态窗口的Owner属性为主窗口
            windowab.Owner = this;
            // 保存主窗口的正常透明度
            double normal = this.Opacity;

            var parentTop = this.Top;
            var parentLeft = this.Left;
            var parentWidth = this.ActualWidth;
            var parentHeight = this.ActualHeight;
            double top = parentTop + (parentHeight - windowab.Height) / 2;
            double left = parentLeft + (parentWidth - windowab.Width) / 2;
            windowab.Top = top;
            windowab.Left = left;

            // 降低主窗口的透明度
            this.Opacity = 0.4;

            //windowab.UpdateTextParam(exeparam);
            //windowab.UpdateGameSelect(exelauselect);
            windowab.ShowDialog();

            // 模态窗口关闭后，恢复主窗口的正常透明度
            this.Opacity = normal;
        }

        private void About_Item_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow windowab = new AboutWindow();

            // 设置模态窗口的Owner属性为主窗口
            windowab.Owner = this;
            // 保存主窗口的正常透明度
            double normal = this.Opacity;

            var parentTop = this.Top;
            var parentLeft = this.Left;
            var parentWidth = this.ActualWidth;
            var parentHeight = this.ActualHeight;
            double top = parentTop + (parentHeight - windowab.Height) / 2;
            double left = parentLeft + (parentWidth - windowab.Width) / 2;
            windowab.Top = top;
            windowab.Left = left;

            // 降低主窗口的透明度
            this.Opacity = 0.4;
            windowab.ShowDialog();
            // 模态窗口关闭后，恢复主窗口的正常透明度
            this.Opacity = normal;
        }

        private void Exit_Item_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Close();
            }
            catch { }
        }

        // 在窗口加载时初始化 NotifyIcon
        private void InitializeNotifyIcon()
        {
            notifyIcon = new NotifyIcon();
            //notifyIcon.Text = "unlocker" + Tb_main.Text;
            //notifyIcon.Icon = new System.Drawing.Icon("Resources/icon.ico");
            //notifyIcon.Icon = new System.Drawing.Icon("icon.ico");
            notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
            notifyIcon.Visible = true;

            // Context menu
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Quit");
            exitMenuItem.Click += ExitMenuItem_Click;
            contextMenu.Items.Add(exitMenuItem);
            notifyIcon.ContextMenuStrip = contextMenu;

            // Handle the DoubleClick event to show the window when the user double clicks the NotifyIcon
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            // Hide the notify icon and close the application
            notifyIcon.Visible = false;
            CleanupNotifyIcon();
            System.Windows.Application.Current.Shutdown();
        }

        // Method to clean up the NotifyIcon
        private void CleanupNotifyIcon()
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Icon = null; // Set the icon to null to release the resource
                notifyIcon.Dispose(); // Call dispose to clean up the icon
                notifyIcon = null; // Set the variable to null to remove the reference
            }
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            if (_isautolaunch == true)
            {
                // 显式计算并设置窗口位置为屏幕中心（在恢复显示前设置）
                // 确保使用 Manual 否则 Left/Top 可能被忽略
                this.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;

                // 使用 ActualWidth/ActualHeight（若为 0 则回退到 Width/Height）
                double w = (this.ActualWidth > 0) ? this.ActualWidth : this.Width;
                double h = (this.ActualHeight > 0) ? this.ActualHeight : this.Height;

                // 使用工作区而非整个屏幕，避免遮挡任务栏
                var wa = SystemParameters.WorkArea;
                double left = wa.Left + (wa.Width - w) / 2;
                double top = wa.Top + (wa.Height - h) / 2;

                // 防护：如果计算结果是 NaN 或 Infinity，则不设置
                if (!double.IsNaN(left) && !double.IsInfinity(left))
                    this.Left = left;
                if (!double.IsNaN(top) && !double.IsInfinity(top))
                    this.Top = top;

                _isautolaunch = false;
            }

            // Show the window
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.Hide(); // Hide the window and show the tray icon
                notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                notifyIcon.BalloonTipTitle = "EndField FPS Unlocker";
                notifyIcon.BalloonTipText = "Minimized to tray";
                notifyIcon.ShowBalloonTip(3000);
            }
            base.OnStateChanged(e);
        }

        private GameInstanceService _service = new GameInstanceService();

        private bool _ismanualmode = false;

        private void start_but_Click(object sender, RoutedEventArgs e)
        {
            _ismanualmode = false;

            bool _ischeckok = CheckSomething();

            if (_service != null)
            {
                if (_ischeckok)
                {
                    //_needdisplaymainwindow = 0; // 重置标志
                    _alreadyexited = 0;

                    Thread.Sleep(200);
                    // 如果游戏没有运行，最小化窗口并启动服务
                    this.WindowState = WindowState.Minimized;

                    _service.StartProcess(_viewModel, _ismanualmode, false, null);
                }
            }
        }

        private bool CheckSomething()
        {
            // 检查是否存在名为 EndField 的进程
            var processes = System.Diagnostics.Process.GetProcessesByName("EndField");

            if (processes.Length > 0)
            {
                //释放进程资源
                foreach (var p in processes)
                {
                    p.Dispose();
                }

                // 如果发现进程，在UI线程弹出提示框
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        "Detected EndField is running!", // Content
                        "Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                });

                return false;
            }

            string targetApp = string.Empty;
            try
            {
                targetApp = _viewModel.GamePath;
            }
            catch (Exception ex)
            {

            }

            // 合并检查：路径为空/空白 或 文件不存在
            if (string.IsNullOrWhiteSpace(targetApp) || !File.Exists(targetApp))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    string message = string.IsNullOrWhiteSpace(targetApp)
                        ? "The application path cannot be empty."          // Prompt when the path is invalid
                        : "The specified application file does not exist."; // Prompt when the file does not exist

                    System.Windows.MessageBox.Show(
                        message,
                        "Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                });
                return false; // 终止执行
            }

            return true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = LoadGenshinToolsDLL("ef_unlock_plugin.dll", "ef_ulk_tol.dll");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"加载DLL失败: {ex}");
                //string message = ex.Message;
                //System.Windows.MessageBox.Show(message, "Error:", MessageBoxButton.OK, MessageBoxImage.Error);
            }


             //ConnnectOKQuickLaunch();
            // 初始化监测器
            _connectionMonitor = new ConnectionMonitor();
            // 启动连接状态监测
            _connectionMonitor.Start((isConnected) =>
            {
                UpdateConnectionStatus(isConnected);
            });

            Task.Run(() =>
            {
                ConnnectOKQuickLaunch();
            });
        }


        private bool _ishasconnected = true;


        private async void ConnnectOKQuickLaunch()
        {
            _ishasconnected = false;

            // UI 更新放在前面
            Dispatcher.Invoke(() =>
            {
                start_but.Content = "Checking...";
                start_but.IsEnabled = false;
            });

            bool _checkconnectedresult = false;

            try
            {
                // 【第一步】先检查 GameInstanceService 是否已经在运行
                // 如果已经在运行，说明连接已建立，无需重复检测，直接返回
                if (GameInstanceService.Current != null && GameInstanceService.Current.pipeClient != null && GameInstanceService.Current.pipeClient.IsConnected)
                {
                    Console.WriteLine("QuickLaunch: Service is already running.");
                    return; // 按钮状态由 ConnectionMonitor 维持，这里直接退出
                }

                // 【第二步】如果服务没运行，尝试连接管道来检测 DLL 是否已注入
                // 这里使用 Task.Run 包装同步的 Connect 方法
                await Task.Run(() =>
                {
                    // 使用 using 确保试探完成后立即销毁连接，释放资源
                    // 这里的连接会在 using 结束后自动 Close/Dispose，不会长期占用
                    using (var testPipe = new NamedPipeClientStream(".", "D83964AF-51C9-D415-4ED9-0D8A61201717", PipeDirection.Out))
                    {
                        try
                        {
                            // 尝试连接，超时设短一点，比如 500ms
                            testPipe.Connect(500);

                            // 如果连上了，说明 DLL 已经注入且服务端在运行
                            if (testPipe.IsConnected)
                            {
                                _checkconnectedresult = true;
                                Console.WriteLine("QuickLaunch: Detected existing pipe service.");
                            }
                        }
                        catch (TimeoutException)
                        {
                            // 超时说明管道不存在（游戏未启动或DLL未注入）
                            _checkconnectedresult = false;
                        }
                        catch (Exception ex)
                        {
                            // 其他错误
                            _checkconnectedresult = false;
                            Console.WriteLine($"QuickLaunch check error: {ex.Message}");
                        }
                    }
                    // 离开 using 块后，testPipe 立即关闭。
                    // C++ 服务端会收到 disconnect 信号，但这正是我们想要的：
                    // 告诉服务端“我探测到了你”，然后断开，把路让给后续的正式连接。
                });

                // 【第三步】根据检测结果决定行为
                if (_checkconnectedresult)
                {
                    Console.WriteLine("QuickLaunch: Connecting to existing game service...");
                    _ismanualmode = false;

                    if (_alreadyexited == 0)
                    {
                        // 传入 true，告诉 GameInstanceService 直接去连管道，不要启动游戏
                        _service.StartProcess(_viewModel, _ismanualmode, true, null);
                    }
                }
                else
                {
                    // 检测失败（游戏没开 或 DLL未注入），恢复按钮让用户手动启动
                    Console.WriteLine("QuickLaunch: No service detected. Ready to start.");
                    //Dispatcher.Invoke(() =>
                    //{
                    //    start_but.Content = "Start Game";
                    //    start_but.IsEnabled = true;
                    //});
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("QuickLaunch unexpected error: " + ex.Message);
                //Dispatcher.Invoke(() =>
                //{
                //    start_but.Content = "Start Game";
                //    start_but.IsEnabled = true;
                //});
            }
            finally
            {
                _ishasconnected = true;
            }
        }


        //// 建议改为 async Task（虽然事件调用可以是 void，但内部逻辑要小心）
        //private async void ConnnectOKQuickLaunch()
        //{
        //    _ishasconnected = false;

        //    // UI 更新放在前面
        //    Dispatcher.Invoke(() =>
        //    {
        //        start_but.Content = "Checking...";
        //        start_but.IsEnabled = false;
        //    });

        //    bool _checkconnectedresult = false;

        //    try
        //    {
        //        // 【修正】使用 Task.Run 包装同步的 Connect 方法，并正确 await
        //        // 这里的逻辑是：启动一个新任务去连接，我们等待这个任务完成
        //        await Task.Run(() =>
        //        {
        //            using (var testPipe = new NamedPipeClientStream(".", "MyGameCommPipe", PipeDirection.Out))
        //            {
        //                // Connect(500) 是同步阻塞的。
        //                // 如果 500ms 内没连上，它会抛出 TimeoutException。
        //                // 我们就在这个 Task 里面捕获它，不让它崩到主线程。
        //                try
        //                {
        //                    testPipe.Connect(500);
        //                    _checkconnectedresult = testPipe.IsConnected;
        //                }
        //                catch (TimeoutException)
        //                {
        //                    // 捕获超时，不需要处理，_checkconnectedresult 保持 false
        //                }
        //                catch (Exception ex)
        //                {
        //                    // 捕获其他可能的管道错误
        //                    Console.WriteLine("Pipe check inner error: " + ex.Message);
        //                }
        //            }
        //        });

        //        // 代码能走到这里，说明 Task.Run 结束了，结果在 _checkconnectedresult 里
        //        if (_checkconnectedresult)
        //        {
        //            Console.WriteLine("Pipe check success: DLL service is running.");
        //        }
        //        else
        //        {
        //            Console.WriteLine("Pipe check failed: Service not running.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // 捕获 Task.Run 本身的意外错误
        //        Console.WriteLine("QuickLaunch check error: " + ex.Message);
        //    }

        //    // 后续逻辑保持不变
        //    try
        //    {
        //        if (_checkconnectedresult)
        //        {
        //            _ismanualmode = false;
        //            if (_alreadyexited == 0)
        //            {
        //                // 检测成功，直接跳转到服务逻辑
        //                _service.StartProcess(_viewModel, _ismanualmode, true, null);
        //            }
        //        }
        //        else
        //        {
        //            // 检测失败，恢复按钮
        //            Dispatcher.Invoke(() =>
        //            {
        //                start_but.Content = "Start Game";
        //                start_but.IsEnabled = true;
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Service start error: " + ex.Message);
        //    }
        //    finally
        //    {
        //        _ishasconnected = true;
        //    }
        //}




        private string LoadGenshinToolsDLL(string resourceFileName, string outputFileName)
        {
            // 构建资源完整名称和输出文件路径
            string resourceName = $"ef_unlockfps.Resources.{resourceFileName}";
            string filePath = Path.Combine(AppContext.BaseDirectory, "ulk_ef_tools", outputFileName);

            //// 检查文件是否已存在
            //if (File.Exists(filePath))
            //    return filePath;

            var assembly = Assembly.GetExecutingAssembly();

            // 验证资源是否存在（调试时可启用打印）
            // Console.WriteLine("可用的嵌入资源:");
            // foreach (var name in assembly.GetManifestResourceNames())
            // {
            //     Console.WriteLine(name);
            // }

            if (!assembly.GetManifestResourceNames().Contains(resourceName))
                throw new Exception($"资源 '{resourceName}' 未找到");

            // 确保输出目录存在
            string outputDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // 从嵌入资源提取并保存文件
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new Exception($"无法加载资源 '{resourceName}'");

                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
                catch (Exception ex)
                {
                    // 截断异常信息以避免过长
                    string message = ex.Message;
                    const int maxLength = 500;
                    if (message.Length > maxLength)
                    {
                        message = message.Substring(0, maxLength) + "...";
                    }
                    throw new Exception($"DLL load error: {message}");
                }
            }

            return filePath;
        }

        private void Window_UnLoaded(object sender, RoutedEventArgs e)
        {
            // 发送取消信号
            if (_ctsmainloop != null)
            {
                _ctsmainloop.Cancel();      // 通知 Task 结束
                _ctsmainloop.Dispose();     // 释放非托管资源
                _ctsmainloop = null;
            }

            //// 停止监测
            //_connectionMonitor.Stop();

            //// 清除图标残留
            //CleanupNotifyIcon();
        }


        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            CleanupNotifyIcon();
        }

        private static bool _checkGameInstanceStatus = false;
        public static void CheckGameInstanceStatus(bool state)
        {
            _checkGameInstanceStatus = state;
            //Console.WriteLine(state);
        }

        // 在你的 Window 类中添加这个方法
        public void UpdateConnectionStatus(bool isConnected)
        {
            // 确保在 UI 线程上执行（如果是从其他线程调用）
            Dispatcher.Invoke(() =>
            {
                if (!_ishasconnected)
                    return;
                //Console.WriteLine("555");
                if (_checkGameInstanceStatus)
                    return;
                //Console.WriteLine("55589"+_checkGameInstanceStatus);

                if (isConnected)
                {
                    start_but.Content = "Connected";
                    start_but.IsEnabled = false;
                }
                else
                {
                    // 最稳妥的修改：
                    start_but.Content = "Start Game";
                    start_but.IsEnabled = true;
                }
            });
        }

        private bool _isusingmanualmode = false; // 添加一个标志来跟踪是否正在使用手动模式
        private ManualWindow advancelaunchWindow; // 声明Advancelaunch窗口的引用
        private CancellationTokenSource _cts; // 声明取消令牌源
        private void ManualMode_Item_Click(object sender, RoutedEventArgs e)
        {
            const string processNametest = "EndField";
            try
            {
                var processes = Process.GetProcessesByName(processNametest);
                if (processes.Length > 0)
                {
                    Console.WriteLine($"{processNametest} game is already running");
                    System.Windows.MessageBox.Show("Game is already running", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            catch (OperationCanceledException)
            {

            }

            // 替换局部变量，使用类成员变量
            advancelaunchWindow = new ManualWindow();
            // 设置模态窗口的Owner属性为主窗口
            advancelaunchWindow.Owner = this;
            advancelaunchWindow.Topmost = true;
            // 保存主窗口的正常透明度
            double normal = this.Opacity;

            var parentTop = this.Top;
            var parentLeft = this.Left;
            var parentWidth = this.ActualWidth;
            var parentHeight = this.ActualHeight;
            double top = parentTop + (parentHeight - advancelaunchWindow.Height) / 2;
            double left = parentLeft + (parentWidth - advancelaunchWindow.Width) / 2;
            advancelaunchWindow.Top = top;
            advancelaunchWindow.Left = left;

            _cts = new CancellationTokenSource(); // 初始化取消令牌源
            // 启动监控任务时传递取消令牌
            Task.Run(() => MonitorProcess(_cts.Token));
            advancelaunchWindow.Closed += (s, args) =>
            {
                // 窗口关闭时触发取消操作
                //Console.WriteLine("Monitoring stopped.123");
                _cts.Cancel();
            };


            // 降低主窗口的透明度
            this.Opacity = 0.4;
            advancelaunchWindow.ShowDialog();
            // 模态窗口关闭后，恢复主窗口的正常透明度
            this.Opacity = normal;

        }


        private async Task MonitorProcess(CancellationToken token)
        {
            const string processNametest = "EndField";

            _isusingmanualmode = true; // 设置标志表示正在使用手动模式

            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested(); // 检查是否被取消

                    var processes = Process.GetProcessesByName(processNametest);
                    if (processes.Length > 0)
                    {
                        Console.WriteLine($"{processNametest} is running");
                        var process = processes[0];  // 这里声明 process 变量



                        // 检查主模块是否已加载
                        if (process.MainModule != null && !string.IsNullOrEmpty(process.MainModule.FileName))
                        {
                            // 检查进程是否响应
                            if (!process.HasExited && process.Responding)
                            {
                                Console.WriteLine($"Game '{processNametest}' is fully running.");
                                Console.WriteLine($"Process ID: {process.Id}");
                                Console.WriteLine($"Process File: {process.MainModule.FileName}");

                                // 开始游戏后执行解锁逻辑
                                Task.Run(() => Unlockstart_Manual(process.Id));

                                // 在 UI 线程异步执行关闭操作
                                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                                {
                                    //start_but.IsEnabled = false;
                                    //start_but.Content = "Manual Mo...";

                                    if (advancelaunchWindow != null)
                                    {
                                        await Task.Delay(1531);
                                        advancelaunchWindow.UpdateTipMessage("Game detected!"); // 调用窗口的公共方法
                                                                                                // 异步等待 1 秒（不阻塞 UI）
                                        await Task.Delay(1531);

                                        // 关闭前增加安全性检查
                                        if (advancelaunchWindow.IsVisible)
                                        {
                                            advancelaunchWindow.Close();
                                        }
                                        advancelaunchWindow = null;
                                    }
                                });
                                break;

                                //break;
                            }
                        }
                    }
                    else
                    {
                        //Console.WriteLine($"{processNametest} NOT launch");
                    }

                    // 使用可取消的异步延迟
                    await Task.Delay(150, token);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Monitoring stopped.");
                _isusingmanualmode = false; // 监控停止时重置标志
            }
        }

        private void Unlockstart_Manual(int pid)
        {
            _ismanualmode = true;
            //_needdisplaymainwindow = 0; // 重置标志
            _alreadyexited = 0;
            _service.StartProcess(_viewModel, _ismanualmode, false, pid);
        }
    }
}