using ParadowSync.Core.Models;

namespace ParadowSync.Windows.Services;

public interface IWindowManager
{
    IReadOnlyList<MonitorInfo> GetMonitors();
    Task<nint> WaitForGameWindowAsync(string processName, TimeSpan timeout, CancellationToken ct);
    void PlaceWindow(nint hwnd, int monitor, WindowSlot slot);
    void FocusWindow(nint hwnd);
    bool IsWindowValid(nint hwnd);
}

public sealed record MonitorInfo(int Index, int X, int Y, int Width, int Height);
