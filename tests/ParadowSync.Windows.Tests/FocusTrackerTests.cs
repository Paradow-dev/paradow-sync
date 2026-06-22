using ParadowSync.Windows.Services;

namespace ParadowSync.Windows.Tests;

public class FocusTrackerTests
{
    [Fact]
    public void StartStop_DoesNotThrow()
    {
        if (!OperatingSystem.IsWindows())
            return;

        using var tracker = new FocusTracker();
        tracker.Start();
        tracker.Stop();
    }

    [Fact]
    public void Dispose_UnhooksWithoutThrow()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var tracker = new FocusTracker();
        tracker.Start();
        tracker.Dispose();
        tracker.Dispose();
    }
}
