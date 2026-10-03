using ef_unlockfps.Utils;
using ef_unlockfps.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO; // 需要引用此命名空间以使用 File.Exists
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using static ef_unlockfps.Utils.Natives;

namespace ef_unlockfps.Services
{
    internal class GameInstanceService
    {
        // 加上 ? 表示它可以是 null
        private MainViewModel? _viewModel;


        // 【新增】静态属性，保存当前运行的服务实例
        public static GameInstanceService Current { get; private set; }

        // 【新增】公开的管道客户端引用，供外部检测状态
        public NamedPipeClientStream pipeClient { get; private set; }

        // 构造函数中赋值单例
        public GameInstanceService()
        {
            Current = this;
        }


        private bool _isgameprocessrunning = false;

        public void StartProcess(MainViewModel sharedViewModel,bool ismanualmode,bool hasconnected, int? processid)
        {
            _viewModel = sharedViewModel ?? new MainViewModel();

            // 在这里编写你的核心逻辑
            // 例如：启动游戏、修改内存、读写配置等
            // 如果已经在运行，直接退出，不执行后面的代码
            //if (_isRunning) return;
            //// 标记为正在运行
            //_isRunning = true;


            Task.Run(() =>
            {
                GameMainService(ismanualmode, hasconnected, processid);
            });

        }

        private void GameMainService(bool manualmode,bool hasconnected, int? processid)
        {
            Console.WriteLine("GameInstanceService is running...");
            
            if (_viewModel == null)
            {
                return;
            }


            // 写入游戏分辨率
            try
            {
                int gameversion = -1;
                string targetCompany = "Shanghai Hypergryph Network Technology Co., Ltd.";
                string targetCompanygl = "GRYPH FRONTIER PTE. LTD.";

                string? gmpathtemp = _viewModel.GamePath;
                string gmpath = gmpathtemp.Trim('"');

                if (ResolutionHelper.VerifySignerName(gmpath, targetCompany))
                {
                    // CN Version
                    gameversion = 0;
                }
                else if (ResolutionHelper.VerifySignerName(gmpath, targetCompanygl))
                {
                    // Global Version
                    gameversion = 1;
                }

                int customresolutioncode = _viewModel.CustomResolutionCfg;
                int width = 1920;
                int height = 1080;

                //if (customresolutioncode != 0)
                //{
                //    if (customresolutioncode == 1) 
                //    {
                //        width = 3840;
                //        height = 2160;
                //    }
                //    if (customresolutioncode == 2)
                //    {
                //        width = 2560;
                //        height = 1440;
                //    }
                //}

                if (customresolutioncode != 0)
                {
                    switch (customresolutioncode)
                    {
                        case 1:
                            width = 3840; height = 2160;
                            break;
                        case 2:
                            width = 2560; height = 1440;
                            break;
                        case 3:
                            width = 1920; height = 1440;
                            break;
                        case 4:
                            width = 1920; height = 1200;
                            break;
                        case 5:
                            width = 1920; height = 1080;
                            break;
                        case 6:
                            width = 1680; height = 1050;
                            break;
                        case 7:
                            width = 1600; height = 1200;
                            break;
                        case 8:
                            width = 1600; height = 1024;
                            break;
                        case 9:
                            width = 1600; height = 900;
                            break;
                        case 10:
                            width = 1440; height = 1080;
                            break;
                        case 11:
                            width = 1440; height = 900;
                            break;
                        case 12:
                            width = 1366; height = 768;
                            break;
                        case 13:
                            width = 1360; height = 768;
                            break;
                        case 14:
                            width = 1280; height = 1440;
                            break;
                        case 15:
                            width = 1280; height = 1024;
                            break;
                        case 16:
                            width = 1280; height = 960;
                            break;
                        case 17:
                            width = 1280; height = 800;
                            break;
                        case 18:
                            width = 1280; height = 768;
                            break;
                        case 19:
                            width = 1176; height = 664;
                            break;
                        case 20:
                            width = 1152; height = 864;
                            break;
                        case 21:
                            width = 1024; height = 768;
                            break;
                        case 22:
                            width = 800; height = 600;
                            break;
                        case 23:
                            width = 720; height = 576;
                            break;
                        case 24:
                            width = 720; height = 480;
                            break;
                        case 25:
                            width = 640; height = 480;
                            break;
                    }

                    // 调用刚才创建的类中的方法
                    ResolutionHelper.SetEndfieldResolution(width, height, gameversion);
                }
            }
            catch (Exception ex)
            {

            }







            // 初始化参数
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

            // 标记：是否由 CreateProcess 创建并拥有句柄（只有为 true 时我们才 CloseHandle）
            bool ownsProcessHandle = false;

            // 如果是已连接状态，查找 endfield.exe 进程并填充 pi
            if (hasconnected)
            {
                try
                {
                    // 修改 2: 通过进程名查找进程，相当于指定 pi 为 endfield.exe
                    Process[] processes = Process.GetProcessesByName("endfield");

                    if (processes.Length > 0)
                    {
                        // 获取第一个找到的进程
                        Process targetProcess = processes[0];

                        // 填充 PROCESS_INFORMATION 结构体
                        // pi 已经在上面初始化过了，这里只是重新赋值
                        pi.hProcess = targetProcess.Handle;
                        pi.dwProcessId = (uint)targetProcess.Id;
                        pi.hThread = IntPtr.Zero; // 附加模式下无法获取主线程句柄
                        pi.dwThreadId = 0;

                        ownsProcessHandle = false; // 非我们创建的句柄，不负责释放

                        Console.WriteLine($"HasConnected mode: Attached to endfield.exe (PID: {pi.dwProcessId})");
                    }
                    else
                    {
                        // 如果找不到进程，弹出提示并退出
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            System.Windows.MessageBox.Show(
                                "hasconnected is true, but endfield.exe process not found.",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        });
                        return;
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show(
                            $"Failed to find endfield.exe: {ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    });
                    return;
                }

                // 跳过启动和注入逻辑
                goto GameMainInstanceService;
            }


            // DLL 路径
            string appPath22 = AppDomain.CurrentDomain.BaseDirectory;
            string dllPath22 = Path.Combine(appPath22, @"ulk_ef_tools\ef_ulk_tol.dll");

            if (!manualmode)
            {

                string targetApp = string.Empty;
                string gamelaunchParam = string.Empty;
                try
                {
                    targetApp = _viewModel.GamePath;
                    gamelaunchParam = _viewModel.CustomLaunchParamCfg;

                    if(_viewModel.UseDx11 == true)
                    {
                        if (!string.IsNullOrEmpty(gamelaunchParam))
                        {
                            gamelaunchParam += " ";
                        }

                        gamelaunchParam += "-force-d3d11";
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show(
                            "Error: "+ex, // 内容
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    });
                }

                // 1. 处理路径中的空格：如果路径包含空格，必须用双引号括起来
                string quotedTargetApp = targetApp;
                if (!quotedTargetApp.StartsWith("\"") && !quotedTargetApp.EndsWith("\""))
                {
                    quotedTargetApp = "\"" + targetApp + "\"";
                }

                // 2. 构造完整的命令行字符串
                // 格式应为: "C:\Path\To\Game.exe" -param1 -param2
                string commandLine = quotedTargetApp + " " + gamelaunchParam;

                //// 初始化参数
                //STARTUPINFO si = new STARTUPINFO();
                //si.cb = Marshal.SizeOf(si);
                //PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

                // 调用 CreateProcess
                // 注意：CREATE_SUSPENDED 标志是关键
                bool success = CreateProcess(
                    null,                   // 应用程序名 (null 则从命令行读取)
                    //targetApp,              // 命令行参数
                    commandLine,            // 命令行参数
                    null,                   // 进程安全属性
                    null,                   // 线程安全属性
                    false,                  // 不继承句柄
                    CREATE_SUSPENDED,       // 关键：创建标志 - 挂起状态
                    IntPtr.Zero,            // 环境变量 (使用父进程的)
                    null,                   // 当前目录 (使用父进程的)
                    ref si,                 // 启动信息
                    out pi                  // 进程信息 (输出)
                );

                if (!success)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show(
                            "Unable to create game process.Error Code: " + Marshal.GetLastWin32Error(), // 内容
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    });
                    return;
                }

                ownsProcessHandle = false; // 非我们创建的句柄，不负责释放

                int suspendCount = ResumeThread(pi.hThread);

                if (suspendCount == 1)
                {
                    Console.WriteLine("The thread has been successfully resumed, and the process has started running.");
                    _isgameprocessrunning = true;
                }
                else
                {
                    Console.WriteLine($"Failed to resume the thread or the status is abnormal (Return value: {suspendCount}).");
                }
            }


            // 如果是手动模式且 processid 不为空，则附加到现有进程
            if (manualmode && processid.HasValue)
            {
                try
                {
                    // 1. 根据 PID 获取进程对象
                    Process runningProcess = Process.GetProcessById(processid.Value);

                    // 2. 填充 PROCESS_INFORMATION 结构体
                    pi = new PROCESS_INFORMATION(); // 重置结构体

                    // 进程句柄：使用 Process 对象的 Handle
                    pi.hProcess = runningProcess.Handle;

                    // 进程ID
                    pi.dwProcessId = (uint)runningProcess.Id;

                    // 线程句柄：附加模式下无法获取主线程句柄，必须置零
                    pi.hThread = IntPtr.Zero;

                    // 线程ID：附加模式下不确定，置0即可
                    pi.dwThreadId = 0;

                    ownsProcessHandle = false; // 非我们创建的句柄，不负责释放

                    Console.WriteLine($"Attached to existing process. PID: {pi.dwProcessId}");
                    _isgameprocessrunning = true; // 更新状态标记
                }
                catch (ArgumentException)
                {
                    // PID 不存在时 GetProcessById 会抛出此异常
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show(
                            $"Process with PID {processid.Value} not found.",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    });
                    return;
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show(
                            $"Failed to attach to process: {ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    });
                    return;
                }
            }

            // 注入 DLL
            if (!InjectDll123(pi.hProcess, dllPath22))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        "Unable to inject DLL.Error Code: " + Marshal.GetLastWin32Error(), // 内容
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
                return;
            }


      GameMainInstanceService:

            if(hasconnected)
            {
                Console.WriteLine("Already connected to game process, skipping launch and injection.");
            }

            MainWindow.CheckGameInstanceStatus(true);

            Thread.Sleep(315); // 等待 DLL 加载并启动服务端

            MainWindow.CheckGameInstanceStatus(false);




            // 1. 在外部定义变量
            //NamedPipeClientStream pipeClient = null;
            pipeClient = null; // 重置属性


            //try
            //{
            //    //// 创建 TCP 客户端
            //    //client = new TcpClient();

            //    //// 尝试连接 (这里假设 DLL 已经加载并启动了服务端)
            //    //// 在实际使用中，可能需要加一点延时或重试机制
            //    //client.Connect(IPAddress.Parse(LocalHost), Port);

            //    client = new TcpClient();
            //    // 【建议添加】强制关闭选项，防止服务端 CLOSE_WAIT
            //    client.LingerState = new LingerOption(true, 0);

            //    client.Connect(IPAddress.Parse(LocalHost), Port);


            //    Console.WriteLine("Connected to C++ DLL!");

            //    // 获取网络流
            //    stream = client.GetStream();


            //    //// 接收返回数据
            //    //data = new byte[256];
            //    //int bytes = stream.Read(data, 0, data.Length);
            //    //string responseData = Encoding.UTF8.GetString(data, 0, bytes);
            //    //Console.WriteLine($"Received: {responseData}");

            //    //// 关闭连接
            //    //stream.Close();
            //    //client.Close();
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Error: {ex.Message}");
            //    Console.WriteLine("Make sure the C++ DLL is loaded and the server thread is running.");
            //}



            int? lastFps = null;
            bool? lastPowerSaving = null;
            int? lastPriority = null;
            bool? lastUmlimitFpsMode = null;

            // 定义退出码变量
            uint exitCode = 0;

            while (true)
            {
                int getconfigpriority = -1;

                if (_viewModel.ProcessPriority == "Realtime")
                {
                    getconfigpriority = 0;
                }
                if (_viewModel.ProcessPriority == "High")
                {
                    getconfigpriority = 1;
                }
                if (_viewModel.ProcessPriority == "Above Normal")
                {
                    getconfigpriority = 2;
                }
                if (_viewModel.ProcessPriority == "Normal")
                {
                    getconfigpriority = 3;
                }
                if (_viewModel.ProcessPriority == "Below Normal")
                {
                    getconfigpriority = 4;
                }
                if (_viewModel.ProcessPriority == "Low")
                {
                    getconfigpriority = 5;
                }

                // 2. 调用 API 获取进程退出码
                if (GetExitCodeProcess(pi.hProcess, out exitCode))
                {
                    // 3. 判断退出码是否为 STILL_ACTIVE
                    if (exitCode != STILL_ACTIVE)
                    {
                        // 进程已退出
                        Console.WriteLine($"Game process has exited, exit code: {exitCode}");
                        break;
                    }
                }
                else
                {
                    // 获取状态失败（可能句柄无效或已关闭）
                    Console.WriteLine("Unable to get process status, exiting monitoring.");
                    break;
                }


                // ============================================================
                // 【核心修改：管道连接与重连逻辑】
                // ============================================================
                try
                {
                    // 如果管道为空、已断开或未连接，尝试连接/重连
                    if (pipeClient == null || !pipeClient.IsConnected)
                    {
                        // 清理旧的管道对象
                        if (pipeClient != null)
                        {
                            pipeClient.Dispose();
                            pipeClient = null;
                        }

                        // 创建新的管道客户端
                        // "." 代表本机，"MyGameCommPipe" 必须与 C++ 定义一致
                        pipeClient = new NamedPipeClientStream(".", "D83964AF-51C9-D415-4ED9-0D8A61201717", PipeDirection.Out);

                        // 尝试连接，设置 1 秒超时
                        // 注意：如果 C++ 服务端刚启动，这里可能需要一点时间，如果连不上就下次循环重试
                        pipeClient.Connect(1000);

                        Console.WriteLine("Connected to Game!");
                    }

                    // ============================================================
                    // 【核心修改：数据发送逻辑】
                    // ============================================================
                    if (pipeClient.IsConnected)
                    {
                        // 读取当前值
                        int currentFps = _viewModel.Fps;
                        bool currentPowerSaving = _viewModel.PowerSaving;
                        int currentPriority = getconfigpriority;
                        bool currentUmlimitFpsMode = _viewModel.UnlimitedFps;

                        // 判断是否需要发送
                        bool needSend = !lastFps.HasValue ||
                                        currentFps != lastFps.Value ||
                                        currentPowerSaving != lastPowerSaving ||
                                        currentPriority != lastPriority.Value ||
                                        currentUmlimitFpsMode != lastUmlimitFpsMode.Value;

                        if (needSend)
                        {
                            // 构造消息 (格式: Fps,PowerSaving,Priority,UmlimitFpsMode)
                            string message = $"{currentFps},{(currentPowerSaving ? "1" : "0")},{currentPriority},{(currentUmlimitFpsMode ? "1" : "0")}";

                            byte[] data = Encoding.UTF8.GetBytes(message);

                            // 直接写入 pipeClient (它也是 Stream)
                            pipeClient.Write(data, 0, data.Length);

                            // 可选：立即刷新，确保数据发出去
                            // pipeClient.Flush(); 

                            // 更新缓存
                            lastFps = currentFps;
                            lastPowerSaving = currentPowerSaving;
                            lastPriority = currentPriority;
                            lastUmlimitFpsMode = currentUmlimitFpsMode;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 捕获异常（连接失败或写入失败）
                    Console.WriteLine($"Pipe Error: {ex.Message}");

                    // 发生错误时，强制销毁当前管道对象，下次循环会尝试重建
                    if (pipeClient != null)
                    {
                        pipeClient.Dispose();
                        pipeClient = null;
                    }
                }


                // 降低 CPU 占用，避免死循环空转
                Thread.Sleep(300);
            }



            // 循环结束后清理资源
            if (pipeClient != null)
            {
                pipeClient.Dispose();
            }



            // 仅在我们“拥有”句柄时关闭它们（避免关闭 Process.Handle）
            try
            {
                if (ownsProcessHandle)
                {
                    if (pi.hProcess != IntPtr.Zero) CloseHandle(pi.hProcess);
                    if (pi.hThread != IntPtr.Zero) CloseHandle(pi.hThread);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occurred while closing the handle: {ex.Message}");
            }

            Console.WriteLine("GameInstanceService stopped.");









            // 5. 根据配置决定是否自动关闭主程序
            if (_viewModel.AutoCloseCfg == true)
            {
                //Environment.Exit(0);
                MainWindow.needdisplayState(0, 1);
            }
            else
            {
                MainWindow.needdisplayState(1, 0);
            }


            MainWindow.exitedstate(1);
            Console.WriteLine("GameInstanceService has completed its check.");

        }
    }
}
