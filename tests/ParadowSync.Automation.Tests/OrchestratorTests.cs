using ParadowSync.Automation;
using ParadowSync.Automation.Services;
using ParadowSync.Core.Models;
using ParadowSync.Windows.Services;

namespace ParadowSync.Automation.Tests;

public class OrchestratorTests
{
    [Fact]
    public async Task LaunchProfileAsync_selectorSucceeds_setsSlotReady()
    {
        var launcher = new FakeLauncher();
        var selector = new FakeCharacterSelector { ShouldSucceed = true };
        var windowManager = new FakeWindowManager();
        var settings = new AppSettings { LaunchDelayMs = 0 };
        var orchestrator = new Orchestrator(launcher, selector, windowManager, settings);

        var profile = CreateProfile("selector-success");

        await orchestrator.LaunchProfileAsync(profile);

        Assert.NotNull(orchestrator.CurrentSession);
        Assert.Equal("selector-success", orchestrator.CurrentSession.ActiveProfileId);
        Assert.Equal(SlotStatus.Ready, orchestrator.CurrentSession.Slots[0].Status);
        Assert.Equal(new nint(100), orchestrator.CurrentSession.Slots[0].Hwnd);
        Assert.Equal(["acc-1"], launcher.LaunchedAccounts);
        Assert.Single(windowManager.PlacedWindows);
    }

    [Fact]
    public async Task LaunchProfileAsync_selectorFails_setsManualRequired()
    {
        var launcher = new FakeLauncher();
        var selector = new FakeCharacterSelector { ShouldSucceed = false };
        var windowManager = new FakeWindowManager();
        var settings = new AppSettings { LaunchDelayMs = 0 };
        var orchestrator = new Orchestrator(launcher, selector, windowManager, settings);

        var profile = CreateProfile("selector-fail");

        await orchestrator.LaunchProfileAsync(profile);

        Assert.NotNull(orchestrator.CurrentSession);
        Assert.Equal(SlotStatus.ManualRequired, orchestrator.CurrentSession.Slots[0].Status);
        Assert.Single(windowManager.PlacedWindows);
    }

    [Fact]
    public async Task FocusSlotAsync_callsFocusWindow()
    {
        var launcher = new FakeLauncher();
        var selector = new FakeCharacterSelector { ShouldSucceed = true };
        var windowManager = new FakeWindowManager();
        var settings = new AppSettings { LaunchDelayMs = 0 };
        var orchestrator = new Orchestrator(launcher, selector, windowManager, settings);

        await orchestrator.LaunchProfileAsync(CreateProfile("focus-test"));
        await orchestrator.FocusSlotAsync(0);

        Assert.Equal([new nint(100)], windowManager.FocusedWindows);
        Assert.Equal(0, orchestrator.CurrentSession?.FocusedSlotIndex);
    }

    [Fact]
    public async Task StopAllAsync_clearsCurrentSession()
    {
        var orchestrator = new Orchestrator(
            new FakeLauncher(),
            new FakeCharacterSelector(),
            new FakeWindowManager(),
            new AppSettings { LaunchDelayMs = 0 });

        await orchestrator.LaunchProfileAsync(CreateProfile("stop-test"));
        await orchestrator.StopAllAsync();

        Assert.Null(orchestrator.CurrentSession);
    }

    private static Profile CreateProfile(string id) => new()
    {
        Id = id,
        Name = "Test Profile",
        Accounts =
        [
            new AccountSlot
            {
                AccountId = "acc-1",
                Character = "Iopette",
                Class = "Iop",
                Monitor = 0,
                Slot = new WindowSlot { X = 0, Y = 0, W = 960, H = 540 },
            },
        ],
    };

    private sealed class FakeLauncher : ILauncherService
    {
        public List<string> LaunchedAccounts { get; } = [];

        public Task LaunchAccountAsync(string accountId, CancellationToken ct = default)
        {
            LaunchedAccounts.Add(accountId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCharacterSelector : ICharacterSelector
    {
        public bool ShouldSucceed { get; set; } = true;

        public Task<bool> SelectCharacterAsync(
            nint gameHwnd,
            string characterName,
            TimeSpan timeout,
            CancellationToken ct) =>
            Task.FromResult(ShouldSucceed);
    }

    private sealed class FakeWindowManager : IWindowManager
    {
        private nint _nextHwnd = 100;

        public List<(nint Hwnd, int Monitor, WindowSlot Slot)> PlacedWindows { get; } = [];
        public List<nint> FocusedWindows { get; } = [];

        public IReadOnlyList<MonitorInfo> GetMonitors() =>
            [new MonitorInfo(0, 0, 0, 1920, 1080)];

        public Task<nint> WaitForGameWindowAsync(string processName, TimeSpan timeout, CancellationToken ct) =>
            Task.FromResult(_nextHwnd++);

        public void PlaceWindow(nint hwnd, int monitor, WindowSlot slot) =>
            PlacedWindows.Add((hwnd, monitor, slot));

        public void FocusWindow(nint hwnd) => FocusedWindows.Add(hwnd);

        public bool IsWindowValid(nint hwnd) => hwnd != nint.Zero;
    }
}
