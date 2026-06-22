using ParadowSync.Windows.Services;

namespace ParadowSync.Windows.Tests;

public class WindowManagerTests
{
    [Fact]
    public void IsWindowValid_ZeroHandle_ReturnsFalse()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var manager = new WindowManager();
        Assert.False(manager.IsWindowValid(0));
        Assert.False(manager.IsWindowValid(nint.Zero));
    }

    [Fact]
    public void GetMonitors_OnWindows_ReturnsAtLeastOne()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var manager = new WindowManager();
        var monitors = manager.GetMonitors();

        Assert.NotEmpty(monitors);
        for (var i = 0; i < monitors.Count; i++)
        {
            var monitor = monitors[i];
            Assert.True(monitor.Width > 0);
            Assert.True(monitor.Height > 0);
            Assert.Equal(i, monitor.Index);
        }
    }
}
