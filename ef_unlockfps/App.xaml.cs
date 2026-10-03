//using System.Configuration;
//using System.Data;
//using System.Windows;

//namespace ef_unlockfps
//{
//    /// <summary>
//    /// Interaction logic for App.xaml
//    /// </summary>
//    public partial class App : Application
//    {
//    }

//}






using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading; // 必须引用此命名空间

namespace ef_unlockfps
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // 定义 Mutex 变量
        private static Mutex _mutex = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 第一步：检测是否已经运行
            bool createdNew;

            // "true" 表示我们希望获取初始所有权
            // "out createdNew" 是关键：如果返回 false，说明 Mutex 已经存在（程序已运行）
            _mutex = new Mutex(true, "30launcher_WPF_EndFieldFPSunlocker", out createdNew);

            if (!createdNew)
            {
                // 如果 createdNew 为 false，说明已经有实例在运行
                MessageBox.Show("Another unlocker is already running", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);

                // 强制退出当前新启动的实例（更可靠地结束进程以防后台线程阻塞）
                Environment.Exit(0);
                return;
            }

            // 如果 createdNew 为 true，继续正常启动程序
            base.OnStartup(e);
        }
    }
}