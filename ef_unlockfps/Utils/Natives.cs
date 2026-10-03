using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit;

namespace ef_unlockfps.Utils
{
    internal class Natives
    {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);

        public static string GetProcessPathFromPid(uint pid, out nint processHandle)
        {
            var hProcess = OpenProcess(
                ProcessAccess.QUERY_LIMITED_INFORMATION |
                ProcessAccess.TERMINATE |
                StandardAccess.SYNCHRONIZE, false, pid);

            processHandle = hProcess;

            if (hProcess == nint.Zero)
                return string.Empty;

            StringBuilder sb = new StringBuilder(1024);
            uint bufferSize = (uint)sb.Capacity;
            if (!QueryFullProcessImageName(hProcess, 0, sb, ref bufferSize))
                return string.Empty;

            return sb.ToString();
        }

        internal static class ProcessAccess
        {
            public const uint TERMINATE = 0x0001;
            public const uint CREATE_THREAD = 0x0002;
            public const uint SET_SESSIONID = 0x0004;
            public const uint VM_OPERATION = 0x0008;
            public const uint VM_READ = 0x0010;
            public const uint VM_WRITE = 0x0020;
            public const uint DUP_HANDLE = 0x0040;
            public const uint CREATE_PROCESS = 0x0080;
            public const uint SET_QUOTA = 0x0100;
            public const uint SET_INFORMATION = 0x0200;
            public const uint QUERY_INFORMATION = 0x0400;
            public const uint SUSPEND_RESUME = 0x0800;
            public const uint QUERY_LIMITED_INFORMATION = 0x1000;
            public const uint SET_LIMITED_INFORMATION = 0x2000;
            public const uint ALL_ACCESS = 0x1FFFFF;
        }

        internal static class StandardAccess
        {
            public const uint DELETE = 0x00010000;
            public const uint READ_CONTROL = 0x00020000;
            public const uint WRITE_DAC = 0x00040000;
            public const uint WRITE_OWNER = 0x00080000;
            public const uint SYNCHRONIZE = 0x00100000;
            public const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
            public const uint STANDARD_RIGHTS_READ = READ_CONTROL;
            public const uint STANDARD_RIGHTS_WRITE = READ_CONTROL;
            public const uint STANDARD_RIGHTS_EXECUTE = READ_CONTROL;
            public const uint STANDARD_RIGHTS_ALL = 0x001F0000;
            public const uint SPECIFIC_RIGHTS_ALL = 0x0000FFFF;
        }


        // 必要的 Windows API 结构体和常量
        [StructLayout(LayoutKind.Sequential)]
        public class SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        // 创建挂起进程的标志
        public const uint CREATE_SUSPENDED = 0x00000004;

        // 引入 kernel32.dll 中的函数
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            SECURITY_ATTRIBUTES lpProcessAttributes,
            SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        //
        // 枚举和结构体
        [Flags]
        enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000
        }

        [Flags]
        enum MemoryProtection
        {
            ReadWrite = 0x04
        }

        [Flags]
        enum FreeType
        {
            Release = 0x8000
        }

        // 导入 Windows API 函数
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            FreeType dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            uint nSize,
            out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            IntPtr lpStartAddress,
            IntPtr lpParameter,
            uint dwCreationFlags,
            out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        // DLL 注入函数
        public static bool InjectDll123(IntPtr hProcess, string dllPath)
        {
            // 在目标进程中分配内存
            uint size = (uint)((dllPath.Length + 1) * Marshal.SizeOf(typeof(char)));
            IntPtr remoteMem = VirtualAllocEx(
                hProcess,
                IntPtr.Zero,
                size,
                AllocationType.Commit | AllocationType.Reserve,
                MemoryProtection.ReadWrite);

            if (remoteMem == IntPtr.Zero)
                return false;

            // 将 DLL 路径写入目标进程
            byte[] dllPathBytes = Encoding.Unicode.GetBytes(dllPath);
            if (!WriteProcessMemory(
                hProcess,
                remoteMem,
                dllPathBytes,
                (uint)dllPathBytes.Length,
                out _))
            {
                VirtualFreeEx(hProcess, remoteMem, 0, FreeType.Release);
                return false;
            }

            // 获取 LoadLibraryW 函数地址
            IntPtr kernel32 = GetModuleHandle("kernel32.dll");
            if (kernel32 == IntPtr.Zero)
                return false;

            IntPtr loadLibrary = GetProcAddress(kernel32, "LoadLibraryW");
            if (loadLibrary == IntPtr.Zero)
                return false;

            // 创建远程线程执行 DLL 注入
            IntPtr hThread = CreateRemoteThread(
                hProcess,
                IntPtr.Zero,
                0,
                loadLibrary,
                remoteMem,
                0,
                out _);

            if (hThread == IntPtr.Zero)
            {
                VirtualFreeEx(hProcess, remoteMem, 0, FreeType.Release);
                return false;
            }

            // 等待线程执行完成
            WaitForSingleObject(hThread, 0xFFFFFFFF); // INFINITE

            // 清理资源
            VirtualFreeEx(hProcess, remoteMem, 0, FreeType.Release);
            CloseHandle(hThread);

            return true;
        }

        // 定义 STILL_ACTIVE 常量
        public const uint STILL_ACTIVE = 259;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);
    }
}
