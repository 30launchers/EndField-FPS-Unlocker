#include <array>
#include <vector>
#include <atomic>
#include <thread>
#include <string>

// 防止 windows.h 定义 min 和 max 宏，避免与 std::min/std::max 冲突
#define NOMINMAX
// 减少 Windows.h 包含的内容，加快编译速度
#define WIN32_LEAN_AND_MEAN

#include <windows.h>
#include <Psapi.h>

#pragma comment(lib, "Psapi.lib")

#include "PatternScanner.hpp"
// #include "MinHookManager.h"
#include "HookUtility.h"

int g_TargetFps = 60;         // 用于存储目标帧率
int g_PowerSaving = -1;       // 用于存储省电模式状态 (0=关, 1=开)
int g_ProcessPriority = -1;   // 用于存储进程优先级 (0=Realtime, 1=High, 2=Above Normal, 3=Normal, 4=Below Normal, 5=Low)
int g_UnlimitedFpsMode = -1; // 用于存储是否无限帧率模式 (0=关, 1=开)

// 加密后的字符串数据
namespace encrypted_strings {
    //EndField Fps MEM_code
    constexpr auto fps_code = XorString::encrypt("7F 00 00 ?? 00 00 00 ?? 00 00 00 00 00 ?? 00 00 00 00 00 53 79 6E 63 20 50 72 65 73 65 6E 74 20 54 68 72 65 61 64");
    constexpr auto pipe_code = XorString::encrypt("\\\\.\\pipe\\D83964AF-51C9-D415-4ED9-0D8A61201717");
}

// 辅助函数：向调试器输出格式化字符串
void DebugPrint(const char* format, ...) {
    char buffer[1024];
    va_list args;
    va_start(args, format);
    vsnprintf_s(buffer, sizeof(buffer), _TRUNCATE, format, args);
    va_end(args);
    OutputDebugStringA(buffer);
}

BOOL __declspec(noinline) OnWinError(const char* szFunction, DWORD dwError)
{
    char szMessage[256];
    wsprintfA(szMessage, "%s failed with error %d", szFunction, dwError);
    MessageBoxA(nullptr, szMessage, "Error", MB_ICONERROR);

    //if (pIPCData)
    //    pIPCData->Status = IPCStatus::Error;

    return FALSE;
}


std::atomic<bool> g_IsRunning(true);


// 管道名称定义
//const wchar_t* PIPE_NAME = L"\\\\.\\pipe\\D83964AF-51C9-D415-4ED9-0D8A61201717";
const char* PIPE_NAME = "";
const int PIPE_BUFFER_SIZE = 4096;

// ============================================================
// 【通信处理逻辑】
// ============================================================
void HandleClient(HANDLE hPipe) {
    char recvbuf[PIPE_BUFFER_SIZE];
    DWORD bytesRead = 0;

    while (g_IsRunning) {
        // 1. 读取数据 (对应 Socket 的 recv)
        // ReadFile 在管道中是阻塞的，如果客户端断开，它会返回 FALSE 或返回 0 字节
        BOOL bSuccess = ReadFile(
            hPipe,
            recvbuf,
            PIPE_BUFFER_SIZE - 1, // 留一位给 '\0'
            &bytesRead,
            NULL
        );

        if (bSuccess && bytesRead > 0) {
            // 安全处理字符串结尾
            recvbuf[bytesRead] = '\0';

            // 解析数据 (保持原样)
            int itemsMatched = sscanf_s(recvbuf, "%d,%d,%d,%d",
                &g_TargetFps, &g_PowerSaving, &g_ProcessPriority, &g_UnlimitedFpsMode);

            // (可选) 如果需要回复，可以使用 WriteFile
            // const char* reply = "OK";
            // DWORD written;
            // WriteFile(hPipe, reply, (DWORD)strlen(reply), &written, NULL);
        }
        else {
            // 客户端断开或出错
            // 对于管道，如果 ReadFile 返回 0 或 FALSE，通常意味着连接断开
            if (!bSuccess) {
                int error = GetLastError();
                // ERROR_BROKEN_PIPE (109): 客户端正常关闭
                // ERROR_NO_DATA (232): 客户端关闭了写句柄
                if (error != ERROR_BROKEN_PIPE && error != ERROR_NO_DATA)
                {
                    //DebugPrint("[DLL] Pipe read error: %d\n", error);
                }
            }
            break; // 退出循环
        }
    }

    // 2. 清理连接
    // 管道不需要像 TCP 那样复杂的 shutdown 等待流程
    FlushFileBuffers(hPipe);
    DisconnectNamedPipe(hPipe); // 断开与客户端的连接，准备下一次连接
    CloseHandle(hPipe);         // 关闭当前句柄
    //DebugPrint("[DLL] Pipe client disconnected.\n");
}

// ============================================================
// 【服务端主循环】
// ============================================================
DWORD WINAPI RunNetService(LPVOID lpParam)
{
    auto _pipe_code = XorString::decrypt(encrypted_strings::pipe_code.data(), encrypted_strings::pipe_code.size());
    PIPE_NAME = _pipe_code.c_str();

    // 循环处理连接
    while (g_IsRunning)
    {
        // 1. 创建命名管道实例
        // PIPE_ACCESS_DUPLEX: 双向通信 (如果只需要接收可用 PIPE_ACCESS_INBOUND)
        //HANDLE hPipe = CreateNamedPipe(
        //    PIPE_NAME,
        //    PIPE_ACCESS_DUPLEX,       // 双向
        //    PIPE_TYPE_MESSAGE |       // 消息流模式 (类似 TCP 的消息边界)
        //    PIPE_READMODE_MESSAGE |
        //    PIPE_WAIT,
        //    PIPE_UNLIMITED_INSTANCES, // 最大实例数
        //    PIPE_BUFFER_SIZE,         // 输出缓冲
        //    PIPE_BUFFER_SIZE,         // 输入缓冲
        //    0,                        // 默认超时
        //    NULL                      // 默认安全
        //);

        // 窄字符版，使用CreateNamedPipeA而不是CreateNamedPipe
        HANDLE hPipe = CreateNamedPipeA(
            PIPE_NAME,
            PIPE_ACCESS_DUPLEX,       // 双向
            PIPE_TYPE_MESSAGE |       // 消息流模式 (类似 TCP 的消息边界)
            PIPE_READMODE_MESSAGE |
            PIPE_WAIT,
            PIPE_UNLIMITED_INSTANCES, // 最大实例数
            PIPE_BUFFER_SIZE,         // 输出缓冲
            PIPE_BUFFER_SIZE,         // 输入缓冲
            0,                        // 默认超时
            NULL                      // 默认安全
        );


        if (hPipe == INVALID_HANDLE_VALUE) {
            OnWinError("CreateNamedPipe", GetLastError());
            Sleep(1000); // 出错等待一下防止死循环刷屏
            continue;
        }

        // 2. 等待客户端连接 (对应 Socket 的 accept)
        // ConnectNamedPipe 是阻塞的。
        // 为了能响应 g_IsRunning 退出，我们在创建句柄后，
        // 依靠 ConnectNamedPipe 的阻塞等待。
        // 如果需要非常及时的退出响应，可以使用重叠(IO)模式，但这里保持简单。

        BOOL bConnected = ConnectNamedPipe(hPipe, NULL);

        // 如果 ConnectNamedPipe 返回 0，检查是否是因为客户端已连接
        if (!bConnected && GetLastError() != ERROR_PIPE_CONNECTED) {
            // 连接失败，关闭句柄重试
            CloseHandle(hPipe);
            continue;
        }

        // 3. 客户端已连接，处理通信
        // 注意：这里是在主服务线程中直接处理。
        // 如果处理耗时较长，建议创建新线程传参 hPipe，否则会阻塞后续连接。
        // 但参考原代码 HandleClient 是阻塞的，这里保持一致。
        HandleClient(hPipe);

        // HandleClient 内部已经调用了 CloseHandle，这里不需要再次关闭
    }

    //DebugPrint("[DLL] Pipe service stopped.\n");
    return 0;
}



// 枚举窗口的回调数据
struct EnumWindowsData {
    DWORD processId;
    HWND foundWindow;
};

// 枚举回调函数
BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM lParam) {
    EnumWindowsData* data = (EnumWindowsData*)lParam;

    DWORD pid = 0;
    GetWindowThreadProcessId(hwnd, &pid);

    if (pid == data->processId && IsWindowVisible(hwnd)) {
        if (GetWindowTextLengthA(hwnd) > 0) {
            char className[256];
            GetClassNameA(hwnd, className, sizeof(className));

            if (strcmp(className, "UnityWndClass") == 0) {
                data->foundWindow = hwnd;
                return FALSE; // 停止枚举
            }
        }
    }
    return TRUE;
}

// 检查Unity主窗口是否存在（单次检查）
bool IsUnityWindowReady() {
    EnumWindowsData data;
    data.processId = GetCurrentProcessId();
    data.foundWindow = NULL;

    EnumWindows(EnumWindowsProc, (LPARAM)&data);

    return (data.foundWindow != NULL);
}

// 循环等待Unity主窗口出现
bool WaitForUnityWindow(DWORD timeoutMs = 30000) {
    DWORD startTime = GetTickCount();

    while (GetTickCount() - startTime < timeoutMs) {
        if (IsUnityWindowReady()) {
            return true; // 找到了
        }
        Sleep(2);
    }

    return false; // 超时
}

// 安全读取 double 值，成功返回 true 并将值存入 outValue
bool SafeReadDouble(uintptr_t addr, double& outValue) {
    __try {
        outValue = *reinterpret_cast<double*>(addr);
        return true;
    }
    __except (EXCEPTION_EXECUTE_HANDLER) {
        return false;
    }
}

// 安全写入 double 值（忽略写入失败）
void SafeWriteDouble(uintptr_t addr, double value) {
    __try {
        *reinterpret_cast<double*>(addr) = value;
    }
    __except (EXCEPTION_EXECUTE_HANDLER) {
        // 写入失败时不做任何处理（可根据需要添加日志）
    }
}




// 辅助函数：查找当前进程的 Unity 主窗口句柄
HWND FindMyUnityWindow() {
    struct EnumData {
        DWORD processId;
        HWND hwnd;
    } data;

    data.processId = GetCurrentProcessId();
    data.hwnd = nullptr;

    // 枚举所有窗口，找到属于当前进程且类名为 UnityWndClass 的窗口
    EnumWindows([](HWND hwnd, LPARAM lParam) -> BOOL {
        EnumData* pData = reinterpret_cast<EnumData*>(lParam);
        DWORD windowProcessId = 0;
        GetWindowThreadProcessId(hwnd, &windowProcessId);

        if (windowProcessId == pData->processId && IsWindowVisible(hwnd)) {
            char className[256] = { 0 };
            GetClassNameA(hwnd, className, sizeof(className));
            // Unity 主窗口的类名通常是 UnityWndClass
            if (strcmp(className, "UnityWndClass") == 0) {
                pData->hwnd = hwnd;
                return FALSE; // 找到了，停止枚举
            }
        }
        return TRUE; // 继续枚举
        }, reinterpret_cast<LPARAM>(&data));

    return data.hwnd;
}


// 线程函数：循环向指定地址写入 300.0
void WriteThreadProc(uintptr_t addr)
{
    //DebugPrint("[DLL] Write thread started for address 0x%p\n", reinterpret_cast<void*>(addr));

    HANDLE hProcess = GetCurrentProcess();
    // 用于记录上一次实际写入的优先级索引，用于判断是否需要更新
    // 初始化为 -1，确保程序启动时会执行一次设置
    static int s_LastPriorityIndex = -1;

    while (true)
    {
        // --- 1. 进程优先级处理逻辑 (优先处理) ---

        // 计算“当前应该设置的优先级索引”
        // 默认为用户配置的全局优先级
        int currentTargetPriority = g_ProcessPriority;

        // 检查是否处于“省电模式”下的“失焦”状态
        bool shouldThrottlePriority = false;
        if (g_PowerSaving == 1)
        {
            static HWND hGameWnd = nullptr;
            // 动态获取窗口句柄
            if (!hGameWnd || !IsWindow(hGameWnd)) {
                hGameWnd = FindMyUnityWindow();
            }

            if (hGameWnd) {
                // 如果窗口存在且不在前台（失去焦点）
                if (GetForegroundWindow() != hGameWnd) {
                    shouldThrottlePriority = true;
                }
            }
        }

        // 如果满足降频条件，强制覆盖优先级索引为 5 (IDLE_PRIORITY_CLASS)
        if (shouldThrottlePriority) {
            currentTargetPriority = 5;
        }

        // 只有当配置有效时才尝试设置
        if (currentTargetPriority >= 0)
        {
            // 核心判断：如果当前目标优先级 与 上一次写入的优先级 不同，才执行写入
            if (currentTargetPriority != s_LastPriorityIndex)
            {
                DWORD dwPriorityClass = NORMAL_PRIORITY_CLASS; // 默认值

                // 索引映射到 Windows API 宏
                switch (currentTargetPriority)
                {
                case 0: dwPriorityClass = REALTIME_PRIORITY_CLASS; break;
                case 1: dwPriorityClass = HIGH_PRIORITY_CLASS; break;
                case 2: dwPriorityClass = ABOVE_NORMAL_PRIORITY_CLASS; break;
                case 3: dwPriorityClass = NORMAL_PRIORITY_CLASS; break;
                case 4: dwPriorityClass = BELOW_NORMAL_PRIORITY_CLASS; break;
                case 5: dwPriorityClass = IDLE_PRIORITY_CLASS; break;
                }

                // 写入优先级
                if (SetPriorityClass(hProcess, dwPriorityClass))
                {
                    s_LastPriorityIndex = currentTargetPriority;
                    //DebugPrint("[DLL] Process priority changed to index: %d (Throttled: %s)\n",currentTargetPriority, shouldThrottlePriority ? "Yes" : "No");
                }
            }
        }

        // --- 2. 帧率写入逻辑 ---

        int _fps = g_TargetFps;

        if (g_UnlimitedFpsMode == 1)
        {
            _fps = 3157;
        }

        // 复用上面的 shouldThrottlePriority 判断结果
        // 如果之前判定需要降频优先级，这里同时也把帧率限制为 15
        if (shouldThrottlePriority)
        {
            _fps = 15;
        }

        SafeWriteDouble(addr, _fps);

        Sleep(10);
    }
}


// 消息框线程函数
static DWORD WINAPI MessageBoxThreadGeneric(LPVOID lpParameter) {
    // 安全检查：确保传入了字符串
    if (lpParameter == nullptr) return 1;

    // 将参数转为 const char*
    const char* message = static_cast<const char*>(lpParameter);

    MessageBoxA(nullptr, message, "Error", MB_ICONWARNING);
    return 0;
}



void RunLogic()
{
    // 直接等待Unity窗口（阻塞）
    if (WaitForUnityWindow(60000))
    {
        //DebugPrint("[DLL] Found Unity window, proceeding with hook installation.\n");
        Sleep(50);
    }

    // 扫描特征码
    //std::vector<uintptr_t> setFieldOfViewAddrs = PatternScanner::MultipleScan("7F 00 00 ?? 00 00 00 ?? 00 00 00 00 00 ?? 00 00 00 00 00 53 79 6E 63 20 50 72 65 73 65 6E 74 20 54 68 72 65 61 64");
    auto _fps_code = XorString::decrypt(encrypted_strings::fps_code.data(), encrypted_strings::fps_code.size());
    std::vector<uintptr_t> setFieldOfViewAddrs = PatternScanner::MultipleScan(_fps_code.c_str());


    if (setFieldOfViewAddrs.empty()) {
        //DebugPrint("[DLL] Failed to find FOV pattern\n");
        CreateThread(nullptr, 0, MessageBoxThreadGeneric, (LPVOID)"FPS pattern failed!", 0, nullptr);
        return;
    }

    uintptr_t firstAddr = setFieldOfViewAddrs[0];
    //DebugPrint("[DLL] First address: 0x%p\n", reinterpret_cast<void*>(firstAddr));

    // --- 第一步：确定搜索起点的边界 ---

    const int searchRange = 300;
    uintptr_t startSearch = (firstAddr > searchRange) ? (firstAddr - searchRange) : 0;

    // 定义边界特征码: 00 00 00 00 00 00 ?? ?? 40
    BYTE patternBytes[] = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40 };
    bool patternMask[] = { true, true, true, true, true, false, false, true };
    size_t patternSize = sizeof(patternBytes);
    uintptr_t limitAddr = 0;

    // 扫描边界特征
    for (uintptr_t addr = startSearch; addr <= firstAddr - patternSize; ++addr) {
        bool found = true;
        for (size_t i = 0; i < patternSize; ++i) {
            if (patternMask[i]) {
                if (*reinterpret_cast<BYTE*>(addr + i) != patternBytes[i]) {
                    found = false;
                    break;
                }
            }
        }
        if (found) {
            limitAddr = addr;
            break; // 找到第一个作为边界即可
        }
    }

    if (limitAddr != 0) {
        //DebugPrint("[DLL] Found limit pattern at 0x%p. Search will start after this address.\n", reinterpret_cast<void*>(limitAddr));
    }
    else {
        //DebugPrint("[DLL] Limit pattern not found. Search will start from default offset.\n");
        limitAddr = startSearch;
    }

    // --- 第二步：扫描所有目标特征并存储地址 ---

    std::vector<uintptr_t> candidateAddresses;
    uintptr_t currentStart = (limitAddr) ? (limitAddr + 1) : startSearch;

    // 扫描范围内所有匹配的地址
    for (uintptr_t addr = currentStart; addr <= firstAddr - patternSize; ++addr) {
        bool found = true;
        for (size_t i = 0; i < patternSize; ++i) {
            if (patternMask[i]) {
                if (*reinterpret_cast<BYTE*>(addr + i) != patternBytes[i]) {
                    found = false;
                    break;
                }
            }
        }

        if (found) {
            candidateAddresses.push_back(addr);
        }
    }

    if (candidateAddresses.empty()) {
        //DebugPrint("[DLL] No target patterns found in range.\n");
        CreateThread(nullptr, 0, MessageBoxThreadGeneric, (LPVOID)"FPS pattern move failed!", 0, nullptr);
        return;
    }

    //DebugPrint("[DLL] Found %zu candidate addresses. Starting verification loop...\n", candidateAddresses.size());

    // --- 第三步：循环尝试写入和验证 (最多尝试到第5个) ---

    // 限制最多尝试5次 (索引 0 到 4，对应 foundCount 1 到 5)
    size_t maxAttempts = 5;
    if (candidateAddresses.size() < maxAttempts) {
        maxAttempts = candidateAddresses.size();
    }

    bool success = false;
    uintptr_t targetAddr = 0;

    for (size_t i = 0; i < maxAttempts; ++i) {
        targetAddr = candidateAddresses[i];
        int foundCount = static_cast<int>(i + 1); // 用于日志显示的 foundCount (1~5)

        //DebugPrint("[DLL] Attempt #%d: Testing address 0x%p\n", foundCount, reinterpret_cast<void*>(targetAddr));

        // 1. 写入 double 值 315
        *reinterpret_cast<double*>(targetAddr) = 315.0;
        //DebugPrint("[DLL] Wrote 315.0 to 0x%p\n", reinterpret_cast<void*>(targetAddr));

        // 2. 等待 2秒
        Sleep(2000);

        // 3. 读取并验证
        double currentValue = *reinterpret_cast<double*>(targetAddr);
        //DebugPrint("[DLL] Read back value: %f\n", currentValue);

        // 4. 判断是否成功
        if (currentValue == 315.0) {
            DebugPrint("[DLL] Verification FPS addr successful at address 0x%p (Attempt #%d)\n", reinterpret_cast<void*>(targetAddr), foundCount);
            success = true;
            break; // 验证成功，退出尝试循环
        }
        else {
            //DebugPrint("[DLL] Verification failed (Value mismatch). Trying next address...\n");
        }
    }

    if (!success) {
        //DebugPrint("[DLL] All %zu attempts failed. Value did not stick.\n", maxAttempts);
        CreateThread(nullptr, 0, MessageBoxThreadGeneric, (LPVOID)"FPS pattern move all failed!", 0, nullptr);
        return;
    }

    // 启动独立线程循环写入 (保持原有逻辑)
    std::thread(WriteThreadProc, targetAddr).detach();
    //DebugPrint("[DLL] Write thread detached for address 0x%p\n", reinterpret_cast<void*>(targetAddr));

    //MessageBox(NULL, TEXT("Inject fps ok!"), TEXT("Notification"), MB_ICONINFORMATION | MB_OK);
}




// original version without the new search logic, just for reference:
//void RunLogic()
//{
//    // 直接等待Unity窗口（阻塞）
//    if (WaitForUnityWindow(60000))
//    {
//        DebugPrint("[DLL] Found Unity window, proceeding with hook installation.\n");
//        Sleep(50); // 等待以确保稳定
//    }
//
//    // 扫描特征码
//    std::vector<uintptr_t> setFieldOfViewAddrs = PatternScanner::MultipleScan("7F 00 00 ?? 00 00 00 ?? 00 00 00 00 00 ?? 00 00 00 00 00 53 79 6E 63 20 50 72 65 73 65 6E 74 20 54 68 72 65 61 64");
//
//    if (setFieldOfViewAddrs.empty()) {
//        DebugPrint("[DLL] Failed to find FOV pattern\n");
//        return;
//    }
//
//    DebugPrint("[DLL] Found %zu FOV addresses:\n", setFieldOfViewAddrs.size());
//    for (size_t i = 0; i < setFieldOfViewAddrs.size(); ++i) {
//        DebugPrint("[DLL]   [%zu] 0x%p\n", i, reinterpret_cast<void*>(setFieldOfViewAddrs[i]));
//    }
//
//    // 使用第一个地址作为搜索起点
//    uintptr_t firstAddr = setFieldOfViewAddrs[0];
//    DebugPrint("[DLL] First address: 0x%p\n", reinterpret_cast<void*>(firstAddr));
//
//    // 向低地址方向搜索 160 字节范围
//    const int searchRange = 160;
//    uintptr_t startSearch = (firstAddr > searchRange) ? (firstAddr - searchRange) : 0;
//    uintptr_t targetAddr = 0;
//
//    // 超时设置：30秒
//    DWORD startTime = GetTickCount();
//    const DWORD timeoutMs = 30000;
//
//    DebugPrint("[DLL] Searching repeatedly from 0x%p to 0x%p for up to 30 seconds.\n",
//        reinterpret_cast<void*>(startSearch),
//        reinterpret_cast<void*>(firstAddr));
//
//    while (targetAddr == 0) {
//        // 执行一次范围搜索
//        for (uintptr_t addr = startSearch; addr <= firstAddr; ++addr) {
//            double value;
//            if (SafeReadDouble(addr, value)) {
//                // 检查是否为 30.0, 60.0 或 120.0（考虑浮点精度）
//                if (fabs(value - 30.0) < 0.0001 ||
//                    fabs(value - 60.0) < 0.0001 ||
//                    fabs(value - 120.0) < 0.0001) {
//                    targetAddr = addr;
//                    DebugPrint("[DLL] Found target double value %f at 0x%p\n",
//                        value, reinterpret_cast<void*>(targetAddr));
//                    break;
//                }
//            }
//        }
//
//        if (targetAddr != 0) {
//            break; // 找到后退出循环
//        }
//
//        // 检查是否超时
//        if (GetTickCount() - startTime > timeoutMs) {
//            DebugPrint("[DLL] Timeout reached (30s). No target double value found.\n");
//            return;
//        }
//
//        // 短暂休眠，避免CPU占用过高
//        Sleep(10);
//    }
//
//    // 启动独立线程循环写入 300.0
//    std::thread(WriteThreadProc, targetAddr).detach();
//    DebugPrint("[DLL] Write thread detached, continuing main logic.\n");
//}







BOOL APIENTRY DllMain(HINSTANCE hInstance, DWORD fdwReason, LPVOID lpReserved)
{
    if (hInstance)
        DisableThreadLibraryCalls(hInstance);

    // 检查是否是目标进程 260202
    HMODULE hYuanShen = GetModuleHandleA("YuanShen.exe");
    HMODULE hGenshinImpact = GetModuleHandleA("GenshinImpact.exe");
    HMODULE hStarRail = GetModuleHandleA("Endfield.exe");

    // 如果不是目标进程，直接返回TRUE（DLL加载成功但不初始化）
    if (!hYuanShen && !hGenshinImpact && !hStarRail) {
        return TRUE;
    }

    if (fdwReason == DLL_PROCESS_ATTACH)
    {
        //const auto hThread = CreateThread(nullptr, 0, (LPTHREAD_START_ROUTINE)RunLogic, nullptr, 0, nullptr);
        //if (!hThread)
        //    return OnWinError("CreateThread", GetLastError());

        //CloseHandle(hThread);


        // 260202 启动逻辑线程更改
        LPTHREAD_START_ROUTINE startRoutine = nullptr;

        // 判断当前是哪个进程
        if (hYuanShen || hGenshinImpact) {
            // ys或genshin进程，执行ys逻辑
            //startRoutine = (LPTHREAD_START_ROUTINE)RunLogicGenshin;
        }
        else if (hStarRail) {
            // sr进程，执行sr逻辑
            startRoutine = (LPTHREAD_START_ROUTINE)RunLogic;
        }

        if (startRoutine) {
            const auto hThread = CreateThread(nullptr, 0, startRoutine, nullptr, 0, nullptr);
            if (!hThread)
                return OnWinError("CreateThread", GetLastError());

            CloseHandle(hThread);

            // 启动网络服务线程
            const auto hThreadNet = CreateThread(nullptr, 0, RunNetService, nullptr, 0, nullptr);
            if (!hThreadNet)
            {
                return OnWinError("CreateThreadNet", GetLastError());
            }
            CloseHandle(hThreadNet);
        }
    }
    else if (fdwReason == DLL_PROCESS_DETACH)
    {
        // 禁用所有钩子
        //MinHookManager::DisableAllHooks();
    }

    return TRUE;

}