//using System;
//using System.IO.Pipes; // 引入命名管道命名空间
//using System.Threading;
//using System.Threading.Tasks;

//namespace ef_unlockfps.Services
//{
//    public class ConnectionMonitor
//    {
//        // 管道名称，必须与 C++ 服务端一致
//        private const string PipeName = "MyGameCommPipe";
//        private const string ServerName = "."; // "." 代表本地计算机

//        private CancellationTokenSource _cts;

//        /// <summary>
//        /// 静态方法：检测一次管道连接状态
//        /// </summary>
//        /// <param name="timeoutMs">连接超时时间（毫秒）</param>
//        /// <returns>是否连接成功</returns>
//        public static async Task<bool> CheckConnectionAsync(int timeoutMs = 1000)
//        {
//            NamedPipeClientStream pipeClient = null;
//            try
//            {
//                // 创建管道客户端
//                pipeClient = new NamedPipeClientStream(ServerName, PipeName, PipeDirection.Out);

//                // Connect 是同步方法，放在 Task.Run 中运行以免阻塞 UI
//                bool connected = await Task.Run(() =>
//                {
//                    try
//                    {
//                        pipeClient.Connect(timeoutMs);
//                        return true;
//                    }
//                    catch (TimeoutException)
//                    {
//                        return false; // 超时即为未连接
//                    }
//                    catch (Exception)
//                    {
//                        return false; // 其他异常也视为未连接
//                    }
//                });

//                return connected;
//            }
//            catch (Exception)
//            {
//                return false;
//            }
//            finally
//            {
//                // 统一的资源释放逻辑
//                try
//                {
//                    if (pipeClient != null)
//                    {
//                        if (pipeClient.IsConnected)
//                        {
//                            // 发送信号告知服务端我要断开了（可选，WriteByte 可以触发服务端的 ReadFile 返回）
//                            // pipeClient.WriteByte(0); 
//                        }
//                        pipeClient.Close();
//                        pipeClient.Dispose();
//                    }
//                }
//                catch { }
//            }
//        }

//        /// <summary>
//        /// 启动后台循环监测
//        /// </summary>
//        public void Start(Action<bool> onStatusChanged)
//        {
//            if (_cts != null && !_cts.IsCancellationRequested)
//            {
//                return;
//            }

//            _cts = new CancellationTokenSource();
//            var token = _cts.Token;

//            Task.Run(async () =>
//            {
//                while (!token.IsCancellationRequested)
//                {
//                    // 调用上面的静态方法进行检测
//                    bool isConnected = await CheckConnectionAsync(1000);

//                    if (!token.IsCancellationRequested)
//                    {
//                        onStatusChanged?.Invoke(isConnected);
//                    }

//                    try
//                    {
//                        // 间隔 3 秒检测一次
//                        await Task.Delay(3000, token);
//                    }
//                    catch (TaskCanceledException)
//                    {
//                        break;
//                    }
//                }
//            }, token);
//        }

//        public void Stop()
//        {
//            if (_cts != null)
//            {
//                _cts.Cancel();
//                _cts.Dispose();
//                _cts = null;
//            }
//        }
//    }
//}



using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace ef_unlockfps.Services
{
    public class ConnectionMonitor
    {
        private CancellationTokenSource _cts;

        public void Start(Action<bool> onStatusChanged)
        {
            if (_cts != null && !_cts.IsCancellationRequested) return;

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    bool isConnected = false;

                    // 【核心修改】直接读取共享实例的状态
                    // 如果服务实例存在，且管道不为空，且处于连接状态
                    if (GameInstanceService.Current != null &&
                        GameInstanceService.Current.pipeClient != null)
                    {
                        isConnected = GameInstanceService.Current.pipeClient.IsConnected;
                    }

                    if (!token.IsCancellationRequested)
                    {
                        onStatusChanged?.Invoke(isConnected);
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
            }, token);
        }

        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }
    }
}
