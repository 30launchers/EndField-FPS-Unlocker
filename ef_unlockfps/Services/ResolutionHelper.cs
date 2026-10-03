using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ef_unlockfps.Services
{
    // 建议将类名修改为具有实际意义的名称
    internal class ResolutionHelper
    {
        /// <summary>
        /// 修改《明日方舟：终末地》注册表分辨率
        /// </summary>
        /// <param name="width">目标宽度</param>
        /// <param name="height">目标高度</param>
        public static void SetEndfieldResolution(int width, int height, int gameversion)
        {
            if(gameversion == -1)
            {
                return;
            }

            // 1. 定义注册表路径
            // 注意：根据你的需求选择正确的路径，这里保留了你代码中的 Hypergryph
            //string subPath = @"Software\Hypergryph\Endfield";
            string subPath = "";
            if (gameversion == 0)
            {
                subPath = @"Software\Hypergryph\Endfield";
            }
            else if (gameversion == 1)
            {
                subPath = @"Software\Gryphline\Endfield";
            }


            try
            {
                // 2. 打开或创建注册表项
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(subPath))
                {
                    if (key == null)
                    {
                        Console.WriteLine("Error: Unable to create or open the game registry path!");
                        Console.WriteLine("Path: HKEY_CURRENT_USER\\" + subPath);
                        return;
                    }

                    //Console.WriteLine("正在修改《明日方舟：终末地》 注册表...");
                    Console.WriteLine($"Target resolution: {width} x {height}");

                    // 3. 修改键值
                    // --- 核心分辨率设置 ---
                    key.SetValue("Screenmanager Resolution Width_h182942802", width, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Resolution Height_h2627697771", height, RegistryValueKind.DWord);

                    // --- 默认分辨率设置 (防止启动重置) ---
                    key.SetValue("Screenmanager Resolution Width Default_h680557497", width, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Resolution Height Default_h1380706816", height, RegistryValueKind.DWord);

                    // --- 窗口模式大小 ---
                    key.SetValue("Screenmanager Resolution Window Width_h2524650974", width, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Resolution Window Height_h1684712807", height, RegistryValueKind.DWord);

                    // --- 游戏内部视频设置 ---
                    key.SetValue("video_resolution_width_h583690364", width, RegistryValueKind.DWord);
                    key.SetValue("video_resolution_height_h2517654917", height, RegistryValueKind.DWord);

                    // --- 强制全屏模式 ---
                    key.SetValue("Screenmanager Fullscreen mode_h3630240806", 1, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Fullscreen mode Default_h401710285", 1, RegistryValueKind.DWord);

                    // --- 关闭“使用原生分辨率” ---
                    key.SetValue("Screenmanager Resolution Use Native_h1405027254", 0, RegistryValueKind.DWord);

                    // --- 垂直同步 ---
                    key.SetValue("video_quality_vsync_v2_2_h1194426206", 0, RegistryValueKind.DWord);
                }

                //Console.WriteLine("修改成功！请启动游戏查看效果。");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        public static bool VerifySignerName(string filePathOrig, string expectedName)
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
    }
}
