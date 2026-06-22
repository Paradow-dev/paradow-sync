using System.Diagnostics;
using System.Runtime.InteropServices;
using ParadowSync.Core.Models;
using ParadowSync.Windows.Native;

namespace ParadowSync.Windows.Services;

public sealed class WindowManager : IWindowManager
{
    private readonly List<nint> _monitorHandles = [];

    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        _monitorHandles.Clear();
        var monitors = new List<MonitorInfo>();

        Win32.EnumDisplayMonitors(
            nint.Zero,
            nint.Zero,
            (nint hMonitor, nint _, ref Win32.RECT _, nint _) =>
            {
                var index = _monitorHandles.Count;
                _monitorHandles.Add(hMonitor);

                var info = new Win32.MONITORINFO { cbSize = Marshal.SizeOf<Win32.MONITORINFO>() };
                if (Win32.GetMonitorInfo(hMonitor, ref info))
                {
                    monitors.Add(new MonitorInfo(
                        index,
                        info.rcMonitor.Left,
                        info.rcMonitor.Top,
                        info.rcMonitor.Width,
                        info.rcMonitor.Height));
                }

                return true;
            },
            nint.Zero);

        return monitors;
    }

    public async Task<nint> WaitForGameWindowAsync(string processName, TimeSpan timeout, CancellationToken ct)
    {
        var normalizedName = NormalizeProcessName(processName);
        var knownWindows = GetTopLevelWindowsForProcess(normalizedName);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            var hwnd = FindNewTopLevelWindow(normalizedName, knownWindows);
            if (hwnd != nint.Zero)
                return hwnd;

            await Task.Delay(100, ct).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"No new top-level window found for process '{processName}' within {timeout}.");
    }

    public void PlaceWindow(nint hwnd, int monitor, WindowSlot slot)
    {
        EnsureMonitorsLoaded();

        if (monitor < 0 || monitor >= _monitorHandles.Count)
            throw new ArgumentOutOfRangeException(nameof(monitor), monitor, "Monitor index is out of range.");

        var info = new Win32.MONITORINFO { cbSize = Marshal.SizeOf<Win32.MONITORINFO>() };
        if (!Win32.GetMonitorInfo(_monitorHandles[monitor], ref info))
            throw new InvalidOperationException($"Failed to get monitor info for index {monitor}.");

        var x = info.rcWork.Left + slot.X;
        var y = info.rcWork.Top + slot.Y;

        Win32.SetWindowPos(
            hwnd,
            Win32.HWND_TOP,
            x,
            y,
            slot.W,
            slot.H,
            Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE | Win32.SWP_SHOWWINDOW);
    }

    public void FocusWindow(nint hwnd) => Win32.SetForegroundWindow(hwnd);

    public bool IsWindowValid(nint hwnd) => hwnd != nint.Zero && Win32.IsWindow(hwnd);

    private void EnsureMonitorsLoaded()
    {
        if (_monitorHandles.Count == 0)
            GetMonitors();
    }

    private static string NormalizeProcessName(string processName)
    {
        var name = processName.Trim();
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            name = name[..^4];
        return name;
    }

    private static HashSet<nint> GetTopLevelWindowsForProcess(string processName)
    {
        var windows = new HashSet<nint>();
        var processIds = GetProcessIds(processName);

        Win32.EnumWindows((hwnd, _) =>
        {
            if (!IsTopLevelVisibleWindow(hwnd))
                return true;

            Win32.GetWindowThreadProcessId(hwnd, out var pid);
            if (processIds.Contains(pid))
                windows.Add(hwnd);

            return true;
        }, nint.Zero);

        return windows;
    }

    private static nint FindNewTopLevelWindow(string processName, HashSet<nint> knownWindows)
    {
        nint found = nint.Zero;
        var processIds = GetProcessIds(processName);

        Win32.EnumWindows((hwnd, _) =>
        {
            if (!IsTopLevelVisibleWindow(hwnd) || knownWindows.Contains(hwnd))
                return true;

            Win32.GetWindowThreadProcessId(hwnd, out var pid);
            if (!processIds.Contains(pid))
                return true;

            found = hwnd;
            return false;
        }, nint.Zero);

        return found;
    }

    private static HashSet<uint> GetProcessIds(string processName)
    {
        var ids = new HashSet<uint>();
        foreach (var process in Process.GetProcessesByName(processName))
        {
            using (process)
            {
                ids.Add((uint)process.Id);
            }
        }

        return ids;
    }

    private static bool IsTopLevelVisibleWindow(nint hwnd) =>
        Win32.IsWindow(hwnd)
        && Win32.IsWindowVisible(hwnd)
        && Win32.GetParent(hwnd) == nint.Zero;
}
