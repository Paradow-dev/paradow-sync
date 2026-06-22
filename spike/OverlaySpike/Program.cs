using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace OverlaySpike;

internal static class Program
{
    private const int GwlExstyle = -20;

    private const int HotkeyId = 1;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;

    private const uint WmHotkey = 0x0312;
    private const uint WmNchittest = 0x0084;

    private const int Htcaption = 2;

    private const uint WsExLayered = 0x00080000;
    private const uint WsExTransparent = 0x00000020;
    private const uint WsExNoactivate = 0x08000000;
    private const uint WsExTopmost = 0x00000008;

    private const uint WsPopup = 0x80000000;
    private const int SwShow = 5;

    private const uint EventSystemForeground = 0x0003;
    private const uint WineventOutofcontext = 0x0000;

    private const byte LwaAlpha = 0x00000002;

    private static IntPtr _overlayHwnd;
    private static IntPtr _hookHandle;
    private static WinEventDelegate? _hookDelegate;
    private static WndProcDelegate? _wndProcDelegate;
    private static bool _clickThroughEnabled;
    private static readonly Stopwatch Clock = Stopwatch.StartNew();

    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        PrintInstructions();

        _hookDelegate = OnForegroundChanged;
        _hookHandle = SetWinEventHook(
            EventSystemForeground,
            EventSystemForeground,
            IntPtr.Zero,
            _hookDelegate,
            0,
            0,
            WineventOutofcontext);

        if (_hookHandle == IntPtr.Zero)
        {
            Console.WriteLine("[ERROR] SetWinEventHook failed. Win32 error: {0}", Marshal.GetLastWin32Error());
            return;
        }

        Console.WriteLine("[INFO] Focus hook installed (EVENT_SYSTEM_FOREGROUND).");
        Console.WriteLine("[INFO] Creating layered click-through overlay window...");
        Console.WriteLine();

        if (!CreateOverlayWindow())
        {
            Console.WriteLine("[ERROR] Failed to create overlay window. Win32 error: {0}", Marshal.GetLastWin32Error());
            UnhookWinEvent(_hookHandle);
            return;
        }

        Console.WriteLine("[INFO] Overlay HWND: 0x{0:X}", _overlayHwnd.ToInt64());
        Console.WriteLine("[INFO] Position the overlay over Dofus Unity and switch focus between windows.");
        Console.WriteLine("[INFO] Press Ctrl+C to exit.");
        Console.WriteLine();

        LogCurrentForeground("initial");

        RunMessageLoop();

        UnhookWinEvent(_hookHandle);
    }

    private static void PrintInstructions()
    {
        Console.WriteLine("=== paradow-sync OverlaySpike (Wave 0) ===");
        Console.WriteLine();
        Console.WriteLine("Run on Windows only (net8.0-windows).");
        Console.WriteLine();
        Console.WriteLine("1. Start Dofus Unity (windowed, then borderless/fullscreen).");
        Console.WriteLine("2. A small semi-transparent cyan rectangle appears (top-left).");
        Console.WriteLine("3. Drag the overlay over the game client (starts draggable).");
        Console.WriteLine("4. Press Ctrl+Shift+T to toggle click-through (WS_EX_TRANSPARENT).");
        Console.WriteLine("5. Click inside the game — clicks must pass through when click-through is ON.");
        Console.WriteLine("6. Alt+Tab or click other windows — watch [FOCUS] logs with timestamps.");
        Console.WriteLine("7. Measure latency between perceived focus change and log line (target: < 50 ms).");
        Console.WriteLine("8. Record results in docs/spike/2025-06-22-overlay-findings.md");
        Console.WriteLine();
    }

    private static bool CreateOverlayWindow()
    {
        var className = "ParadowSyncOverlaySpike";

        _wndProcDelegate = WindowProc;
        var wc = new Wndclassex
        {
            CbSize = (uint)Marshal.SizeOf<Wndclassex>(),
            LpfnWndProc = _wndProcDelegate,
            HInstance = GetModuleHandle(null),
            LpszClassName = className
        };

        if (RegisterClassEx(ref wc) == 0 && Marshal.GetLastWin32Error() != 0x00000582) // ERROR_CLASS_ALREADY_EXISTS
        {
            return false;
        }

        const int width = 320;
        const int height = 48;

        // Layered + noactivate from start; TRANSPARENT toggled via Ctrl+Shift+T so the window stays draggable.
        _overlayHwnd = CreateWindowEx(
            WsExLayered | WsExNoactivate | WsExTopmost,
            className,
            "OverlaySpike — drag, then Ctrl+Shift+T",
            WsPopup,
            40,
            40,
            width,
            height,
            IntPtr.Zero,
            IntPtr.Zero,
            wc.HInstance,
            IntPtr.Zero);

        if (_overlayHwnd == IntPtr.Zero)
        {
            return false;
        }

        // ~70% opacity cyan tint — visible but unobtrusive for manual testing.
        if (!SetLayeredWindowAttributes(_overlayHwnd, 0, 180, LwaAlpha))
        {
            return false;
        }

        if (!RegisterHotKey(_overlayHwnd, HotkeyId, ModControl | ModShift, 0x54)) // 'T'
        {
            return false;
        }

        ShowWindow(_overlayHwnd, SwShow);
        UpdateWindow(_overlayHwnd);
        Console.WriteLine("[INFO] Click-through OFF — drag overlay, then Ctrl+Shift+T to enable WS_EX_TRANSPARENT.");
        return true;
    }

    private static IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            ToggleClickThrough(hWnd);
            return IntPtr.Zero;
        }

        if (msg == WmNchittest && !_clickThroughEnabled)
        {
            return (IntPtr)Htcaption;
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private static void ToggleClickThrough(IntPtr hWnd)
    {
        var exStyle = (uint)GetWindowLongPtr(hWnd, GwlExstyle);
        if (_clickThroughEnabled)
        {
            exStyle &= ~WsExTransparent;
            _clickThroughEnabled = false;
            Console.WriteLine("[INFO] Click-through OFF — overlay is draggable again.");
        }
        else
        {
            exStyle |= WsExTransparent;
            _clickThroughEnabled = true;
            Console.WriteLine("[INFO] Click-through ON — WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE active.");
        }

        _ = SetWindowLongPtr(hWnd, GwlExstyle, (IntPtr)exStyle);
        SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, 0x0027); // SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED
    }

    private static void RunMessageLoop()
    {
        while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }
    }

    private static void OnForegroundChanged(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var elapsedMs = Clock.Elapsed.TotalMilliseconds;
        var title = GetWindowTitle(hwnd);
        var className = GetWindowClassName(hwnd);
        GetWindowThreadProcessId(hwnd, out var pid);

        Console.WriteLine(
            "[FOCUS] t={0,10:F1}ms  hwnd=0x{1:X}  pid={2,6}  class=\"{3}\"  title=\"{4}\"",
            elapsedMs,
            hwnd.ToInt64(),
            pid,
            className,
            title);
    }

    private static void LogCurrentForeground(string label)
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            Console.WriteLine("[FOCUS] ({0}) no foreground window", label);
            return;
        }

        var title = GetWindowTitle(hwnd);
        Console.WriteLine("[FOCUS] ({0}) hwnd=0x{1:X} title=\"{2}\"", label, hwnd.ToInt64(), title);
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        var length = GetWindowTextLength(hwnd);
        if (length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        _ = GetWindowText(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string GetWindowClassName(IntPtr hwnd)
    {
        var builder = new StringBuilder(256);
        _ = GetClassName(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Wndclassex
    {
        public uint CbSize;
        public uint Style;
        public IntPtr LpfnWndProc;
        public int CbClsExtra;
        public int CbWndExtra;
        public IntPtr HInstance;
        public IntPtr HIcon;
        public IntPtr HCursor;
        public IntPtr HbrBackground;
        public string? LpszMenuName;
        public string LpszClassName;
        public IntPtr HIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public IntPtr Hwnd;
        public uint Message;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int PtX;
        public int PtY;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassEx(ref Wndclassex lpwcx);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle,
        string lpClassName,
        string lpWindowName,
        uint dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, byte dwFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UpdateWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out Msg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
