using System.Runtime.InteropServices;
using ParadowSync.App.Native;
using ParadowSync.Core.Models;
using ParadowSync.Core.Services;

namespace ParadowSync.App.Services;

public sealed class TrayService : IDisposable
{
    private const uint WM_USER = 0x0400;
    private const uint WM_TRAYICON = WM_USER + 1;
    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_MODIFY = 0x00000001;
    private const uint NIM_DELETE = 0x00000002;
    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;
    private const uint MF_STRING = 0x00000000;
    private const uint MF_SEPARATOR = 0x00000800;
    private const uint TPM_RIGHTBUTTON = 0x0002;
    private const uint TPM_RETURNCMD = 0x0100;
    private const uint WM_COMMAND = 0x0111;
    private const uint WM_DESTROY = 0x0002;
    private const uint IDM_LAUNCH_BASE = 1000;
    private const uint IDM_STOP_ALL = 2001;
    private const uint IDM_TOGGLE_OVERLAY = 2002;
    private const uint IDM_SHOW_WINDOW = 2003;
    private const uint IDM_QUIT = 2004;

    private readonly IProfileStore _profileStore;
    private readonly HotkeyNative.WndProcDelegate _wndProc;
    private nint _hwnd;
    private nint _menu;
    private bool _added;
    private bool _classRegistered;
    private IReadOnlyList<Profile> _profiles = [];

    public event EventHandler<string>? LaunchProfileRequested;
    public event EventHandler? StopAllRequested;
    public event EventHandler? ToggleOverlayRequested;
    public event EventHandler? QuitRequested;
    public event EventHandler? ShowMainWindowRequested;

    public TrayService(IProfileStore profileStore)
    {
        _profileStore = profileStore;
        _wndProc = WindowProc;
        EnsureWindow();
        _menu = CreatePopupMenu();
    }

    public async Task RefreshProfilesAsync(CancellationToken ct = default)
    {
        _profiles = await _profileStore.ListAsync(ct).ConfigureAwait(false);
        RebuildMenu();
    }

    private void RebuildMenu()
    {
        if (_menu == nint.Zero)
            return;

        while (GetMenuItemCount(_menu) > 0)
            RemoveMenu(_menu, 0, MF_STRING);

        var ordered = _profiles.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var id = IDM_LAUNCH_BASE;
        foreach (var profile in ordered)
        {
            AppendMenu(_menu, MF_STRING, id, profile.Name);
            id++;
        }

        AppendMenu(_menu, MF_SEPARATOR, 0, null);
        AppendMenu(_menu, MF_STRING, IDM_SHOW_WINDOW, "Ouvrir la fenêtre");
        AppendMenu(_menu, MF_STRING, IDM_STOP_ALL, "Arrêter tout");
        AppendMenu(_menu, MF_STRING, IDM_TOGGLE_OVERLAY, "Basculer l'overlay");
        AppendMenu(_menu, MF_SEPARATOR, 0, null);
        AppendMenu(_menu, MF_STRING, IDM_QUIT, "Quitter");
    }

    private void EnsureWindow()
    {
        if (_hwnd != nint.Zero)
            return;

        var hInstance = HotkeyNative.GetModuleHandleW(null);
        if (!_classRegistered)
        {
            var wndClass = new HotkeyNative.WNDCLASS
            {
                lpfnWndProc = _wndProc,
                hInstance = hInstance,
                lpszClassName = "ParadowSyncTrayWindow",
            };
            HotkeyNative.RegisterClassW(ref wndClass);
            _classRegistered = true;
        }

        _hwnd = HotkeyNative.CreateWindowExW(
            0,
            "ParadowSyncTrayWindow",
            "ParadowSyncTray",
            HotkeyNative.WS_OVERLAPPED,
            0,
            0,
            0,
            0,
            nint.Zero,
            nint.Zero,
            hInstance,
            nint.Zero);

        if (!_added)
        {
            var data = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = 1,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAYICON,
                hIcon = LoadIcon(nint.Zero, IDI_APPLICATION),
                szTip = "paradow-sync",
            };
            Shell_NotifyIcon(NIM_ADD, ref data);
            _added = true;
        }
    }

    private nint WindowProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == WM_TRAYICON)
        {
            var low = (uint)lParam;
            if (low == WM_LBUTTONDBLCLK)
            {
                ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);
                return nint.Zero;
            }

            if (low == WM_RBUTTONUP)
            {
                var point = new POINT();
                GetCursorPos(out point);
                SetForegroundWindow(_hwnd);
                var cmd = TrackPopupMenu(_menu, TPM_RIGHTBUTTON | TPM_RETURNCMD, point.X, point.Y, 0, _hwnd, nint.Zero);
                if (cmd != 0)
                    PostMessage(_hwnd, WM_COMMAND, (nint)cmd, nint.Zero);
                return nint.Zero;
            }
        }

        if (msg == WM_COMMAND)
        {
            var id = (uint)wParam.ToInt32();
            if (id >= IDM_LAUNCH_BASE && id < IDM_LAUNCH_BASE + 1000)
            {
                var index = (int)(id - IDM_LAUNCH_BASE);
                var ordered = _profiles.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();
                if (index >= 0 && index < ordered.Count)
                    LaunchProfileRequested?.Invoke(this, ordered[index].Id);
            }
            else if (id == IDM_STOP_ALL)
                StopAllRequested?.Invoke(this, EventArgs.Empty);
            else if (id == IDM_TOGGLE_OVERLAY)
                ToggleOverlayRequested?.Invoke(this, EventArgs.Empty);
            else if (id == IDM_SHOW_WINDOW)
                ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);
            else if (id == IDM_QUIT)
                QuitRequested?.Invoke(this, EventArgs.Empty);

            return nint.Zero;
        }

        if (msg == WM_DESTROY)
            return nint.Zero;

        return HotkeyNative.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_added)
        {
            var data = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = 1,
            };
            Shell_NotifyIcon(NIM_DELETE, ref data);
            _added = false;
        }

        if (_menu != nint.Zero)
        {
            DestroyMenu(_menu);
            _menu = nint.Zero;
        }

        if (_hwnd != nint.Zero)
        {
            HotkeyNative.DestroyWindow(_hwnd);
            _hwnd = nint.Zero;
        }
    }

    private const uint WM_LBUTTONDBLCLK = 0x0203;
    private const uint WM_RBUTTONUP = 0x0205;
    private const nint IDI_APPLICATION = 32512;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll")]
    private static extern nint CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(nint hMenu, uint uFlags, uint uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll")]
    private static extern int GetMenuItemCount(nint hMenu);

    [DllImport("user32.dll")]
    private static extern bool RemoveMenu(nint hMenu, uint uPosition, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(nint hMenu);

    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenu(nint hMenu, uint uFlags, int x, int y, int nReserved, nint hWnd, nint prcRect);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern nint LoadIcon(nint hInstance, nint lpIconName);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public nint hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public nint hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
}
