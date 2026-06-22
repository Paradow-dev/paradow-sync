using ParadowSync.App.Native;

namespace ParadowSync.App.Services;

public sealed class HotkeyService : IDisposable
{
    private const string WindowClassName = "ParadowSyncHotkeyWindow";

    private readonly HotkeyNative.WndProcDelegate _wndProc;
    private nint _hwnd;
    private bool _registered;
    private bool _classRegistered;

    public event EventHandler<int>? FocusSlotPressed;
    public event EventHandler? ToggleOverlayPressed;
    public event EventHandler? StopAllPressed;

    public HotkeyService()
    {
        _wndProc = WindowProc;
        EnsureWindow();
    }

    public void Register()
    {
        if (_registered)
            return;

        EnsureWindow();

        for (var i = 0; i < 8; i++)
        {
            var vk = (uint)('1' + i);
            HotkeyNative.RegisterHotKey(
                _hwnd,
                HotkeyNative.IdFocusSlotBase + i,
                HotkeyNative.MOD_CONTROL,
                vk);
        }

        HotkeyNative.RegisterHotKey(
            _hwnd,
            HotkeyNative.IdToggleOverlay,
            HotkeyNative.MOD_CONTROL | HotkeyNative.MOD_SHIFT,
            'O');

        HotkeyNative.RegisterHotKey(
            _hwnd,
            HotkeyNative.IdStopAll,
            HotkeyNative.MOD_CONTROL | HotkeyNative.MOD_SHIFT,
            'Q');

        _registered = true;
    }

    public void Unregister()
    {
        if (!_registered || _hwnd == nint.Zero)
            return;

        for (var i = 0; i < 8; i++)
            HotkeyNative.UnregisterHotKey(_hwnd, HotkeyNative.IdFocusSlotBase + i);

        HotkeyNative.UnregisterHotKey(_hwnd, HotkeyNative.IdToggleOverlay);
        HotkeyNative.UnregisterHotKey(_hwnd, HotkeyNative.IdStopAll);
        _registered = false;
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
                lpszClassName = WindowClassName,
            };
            HotkeyNative.RegisterClassW(ref wndClass);
            _classRegistered = true;
        }

        _hwnd = HotkeyNative.CreateWindowExW(
            0,
            WindowClassName,
            "ParadowSyncHotkeys",
            HotkeyNative.WS_OVERLAPPED,
            0,
            0,
            0,
            0,
            nint.Zero,
            nint.Zero,
            hInstance,
            nint.Zero);
    }

    private nint WindowProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == HotkeyNative.WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (id >= HotkeyNative.IdFocusSlotBase && id < HotkeyNative.IdFocusSlotBase + 8)
            {
                FocusSlotPressed?.Invoke(this, id - HotkeyNative.IdFocusSlotBase);
                return nint.Zero;
            }

            if (id == HotkeyNative.IdToggleOverlay)
            {
                ToggleOverlayPressed?.Invoke(this, EventArgs.Empty);
                return nint.Zero;
            }

            if (id == HotkeyNative.IdStopAll)
            {
                StopAllPressed?.Invoke(this, EventArgs.Empty);
                return nint.Zero;
            }
        }

        return HotkeyNative.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        Unregister();
        if (_hwnd != nint.Zero)
        {
            HotkeyNative.DestroyWindow(_hwnd);
            _hwnd = nint.Zero;
        }
    }
}
