using System;
using System.Runtime.InteropServices;
using System.Text;


namespace ef_unlockfps.Utils;

// 自定义 Win32Window 类
public class Win32Window
{
    private IntPtr _hWnd;

    public Win32Window(IntPtr hWnd)
    {
        _hWnd = hWnd;
    }

    public string ClassName
    {
        get
        {
            const int nMaxCount = 256;
            StringBuilder className = new StringBuilder(nMaxCount);
            if (GetClassName(_hWnd, className, nMaxCount) > 0)
                return className.ToString();
            return string.Empty;
        }
    }

    // 如果需要，还可以添加其他属性，如窗口标题等
    public string WindowText
    {
        get
        {
            const int nMaxCount = 256;
            StringBuilder windowText = new StringBuilder(nMaxCount);
            if (GetWindowText(_hWnd, windowText, nMaxCount) > 0)
                return windowText.ToString();
            return string.Empty;
        }
    }

    // P/Invoke 声明
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
}