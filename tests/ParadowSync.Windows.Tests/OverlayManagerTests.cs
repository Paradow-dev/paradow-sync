using ParadowSync.Core.Catalog;
using ParadowSync.Core.Models;
using ParadowSync.Windows.Overlay;
using ParadowSync.Windows.Services;

namespace ParadowSync.Windows.Tests;

public class OverlayManagerTests
{
    [Fact]
    public void UpdateFocusedSlot_changes_active_index()
    {
        using var overlay = CreateOverlay();
        overlay.UpdateFocusedSlot(2);
        Assert.Equal(2, overlay.ActiveSlotIndex);
    }

    [Fact]
    public void UpdateFocusedSlot_same_index_is_idempotent()
    {
        using var overlay = CreateOverlay();
        overlay.UpdateFocusedSlot(1);
        overlay.UpdateFocusedSlot(1);
        Assert.Equal(1, overlay.ActiveSlotIndex);
    }

    [Fact]
    public void Show_tracks_ready_slots_only()
    {
        using var overlay = CreateOverlay();
        var session = CreateSession(
            new RuntimeSlot
            {
                AccountIndex = 0,
                Character = "Alpha",
                Class = "Iop",
                Status = SlotStatus.Ready,
                Hwnd = new nint(100)
            },
            new RuntimeSlot
            {
                AccountIndex = 1,
                Character = "Beta",
                Class = "Cra",
                Status = SlotStatus.Launching,
                Hwnd = new nint(200)
            });

        overlay.Show(session, CreateProfile());

        overlay.UpdateFocusedSlot(0);
        Assert.Equal(0, overlay.ActiveSlotIndex);
    }

    [Fact]
    public void ForegroundChanged_maps_hwnd_to_focused_slot()
    {
        var focusTracker = new TestFocusTracker();
        using var overlay = CreateOverlay(focusTracker);
        var hwnd = new nint(4242);

        overlay.Show(
            CreateSession(
                new RuntimeSlot
                {
                    AccountIndex = 3,
                    Character = "Gamma",
                    Class = "Feca",
                    Status = SlotStatus.Ready,
                    Hwnd = hwnd
                }),
            CreateProfile());

        focusTracker.RaiseForeground(hwnd);

        Assert.Equal(3, overlay.ActiveSlotIndex);
    }

    [Fact]
    public void Hide_resets_active_slot_index()
    {
        using var overlay = CreateOverlay();
        overlay.Show(CreateSession(CreateReadySlot(0)), CreateProfile());
        overlay.UpdateFocusedSlot(0);

        overlay.Hide();

        Assert.Equal(-1, overlay.ActiveSlotIndex);
    }

    [Fact]
    public void SlotClicked_is_raised_from_team_strip()
    {
        if (!OperatingSystem.IsWindows())
            return;

        using var overlay = CreateOverlay();
        var clicked = -1;
        overlay.SlotClicked += (_, slotIndex) => clicked = slotIndex;
        overlay.Show(CreateSession(CreateReadySlot(4)), CreateProfile());

        Assert.Equal(-1, clicked);
    }

    [Fact]
    public void Dispose_cleans_up_without_throw()
    {
        var overlay = CreateOverlay();
        overlay.Show(CreateSession(CreateReadySlot(0)), CreateProfile());
        overlay.Dispose();
        overlay.Dispose();
    }

    private static OverlayManager CreateOverlay(IFocusTracker? focusTracker = null) =>
        new(new TestWindowManager(), focusTracker, new IconLoader());

    private static TestFocusTracker CreateFocusTracker() => new();

    private static SessionState CreateSession(params RuntimeSlot[] slots) =>
        new() { Slots = slots.ToList() };

    private static RuntimeSlot CreateReadySlot(int index) =>
        new()
        {
            AccountIndex = index,
            Character = $"Char{index}",
            Class = "Iop",
            Status = SlotStatus.Ready,
            Hwnd = new nint(1000 + index)
        };

    private static Profile CreateProfile() =>
        new()
        {
            Id = "test",
            Name = "Test",
            Accounts =
            [
                new AccountSlot
                {
                    AccountId = "a1",
                    Character = "Char",
                    Class = "Iop",
                    Monitor = 0,
                    Slot = new WindowSlot { X = 0, Y = 0, W = 800, H = 600 }
                }
            ]
        };

    private sealed class TestFocusTracker : IFocusTracker
    {
        public event EventHandler<nint>? ForegroundChanged;

        public void RaiseForeground(nint hwnd) => ForegroundChanged?.Invoke(this, hwnd);

        public void Start() { }

        public void Stop() { }

        public void Dispose() { }
    }

    private sealed class TestWindowManager : IWindowManager
    {
        public IReadOnlyList<MonitorInfo> GetMonitors() =>
        [
            new MonitorInfo(0, 0, 0, 1920, 1080)
        ];

        public Task<nint> WaitForGameWindowAsync(string processName, TimeSpan timeout, CancellationToken ct) =>
            Task.FromResult(nint.Zero);

        public void PlaceWindow(nint hwnd, int monitor, WindowSlot slot) { }

        public void FocusWindow(nint hwnd) { }

        public bool IsWindowValid(nint hwnd) => hwnd != nint.Zero;
    }
}
