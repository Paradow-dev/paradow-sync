using ParadowSync.Windows.Native;

namespace ParadowSync.Windows.Services;

public sealed class FocusTracker : IFocusTracker
{
    private nint _hook;
    private Win32.WinEventDelegate? _callback;
    private bool _started;

    public event EventHandler<nint>? ForegroundChanged;

    public void Start()
    {
        if (_started)
            return;

        _callback = OnForegroundChanged;
        _hook = Win32.SetWinEventHook(
            Win32.EVENT_SYSTEM_FOREGROUND,
            Win32.EVENT_SYSTEM_FOREGROUND,
            nint.Zero,
            _callback,
            0,
            0,
            Win32.WINEVENT_OUTOFCONTEXT);

        if (_hook == nint.Zero)
            throw new InvalidOperationException("Failed to set foreground WinEvent hook.");

        _started = true;
    }

    public void Stop()
    {
        if (!_started)
            return;

        if (_hook != nint.Zero)
        {
            Win32.UnhookWinEvent(_hook);
            _hook = nint.Zero;
        }

        _callback = null;
        _started = false;
    }

    public void Dispose() => Stop();

    private void OnForegroundChanged(
        nint hWinEventHook,
        uint eventType,
        nint hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        ForegroundChanged?.Invoke(this, hwnd);
    }
}
