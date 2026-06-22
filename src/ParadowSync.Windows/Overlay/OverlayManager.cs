using System.Runtime.CompilerServices;
using ParadowSync.Core.Catalog;
using ParadowSync.Core.Models;
using ParadowSync.Windows.Services;

[assembly: InternalsVisibleTo("ParadowSync.Windows.Tests")]

namespace ParadowSync.Windows.Overlay;

public sealed class OverlayManager : IOverlayManager
{
    private readonly IFocusTracker _focusTracker;
    private readonly bool _ownsFocusTracker;
    private readonly IWindowManager _windowManager;
    private readonly IconLoader _iconLoader;

    private TeamStripWindow? _teamStrip;
    private readonly Dictionary<int, WindowBadge> _badgesBySlot = new();
    private readonly Dictionary<nint, int> _hwndToSlot = new();
    private readonly Dictionary<int, RuntimeSlot> _slotsByIndex = new();
    private IReadOnlyList<RuntimeSlot> _orderedReadySlots = [];
    private bool _visible = true;
    private bool _shown;

    internal int ActiveSlotIndex { get; private set; } = -1;

    public event EventHandler<int>? SlotClicked;

    public OverlayManager(IWindowManager windowManager, IFocusTracker? focusTracker = null, IconLoader? iconLoader = null)
    {
        _windowManager = windowManager;
        _iconLoader = iconLoader ?? new IconLoader();
        if (focusTracker is not null)
        {
            _focusTracker = focusTracker;
            _ownsFocusTracker = false;
        }
        else
        {
            _focusTracker = new FocusTracker();
            _ownsFocusTracker = true;
        }
    }

    public void Show(SessionState session, Profile profile)
    {
        Hide();

        _shown = true;
        _visible = true;

        _orderedReadySlots = session.Slots
            .Where(slot => slot.Status == SlotStatus.Ready)
            .OrderBy(slot => slot.AccountIndex)
            .ToList();

        foreach (var slot in _orderedReadySlots)
        {
            _slotsByIndex[slot.AccountIndex] = slot;
            if (slot.Hwnd != nint.Zero)
                _hwndToSlot[slot.Hwnd] = slot.AccountIndex;
        }

        if (!OperatingSystem.IsWindows())
            return;

        if (profile.Overlay.TeamStrip.Enabled && _orderedReadySlots.Count > 0)
            CreateTeamStrip(profile);

        if (profile.Overlay.WindowBadges.Enabled)
        {
            foreach (var slot in _orderedReadySlots)
            {
                var badge = new WindowBadge(slot.Hwnd, slot.Character, slot.Class, _iconLoader);
                badge.UpdatePosition(slot.Hwnd);
                badge.SetActive(slot.AccountIndex == ActiveSlotIndex);
                badge.Show();
                badge.SetVisible(_visible);
                _badgesBySlot[slot.AccountIndex] = badge;
            }
        }

        _focusTracker.ForegroundChanged += OnForegroundChanged;
        if (_ownsFocusTracker)
            _focusTracker.Start();
    }

    public void UpdateFocusedSlot(int slotIndex)
    {
        ActiveSlotIndex = slotIndex;

        if (!OperatingSystem.IsWindows() || !_shown)
            return;

        _teamStrip?.SetActiveIndex(FindStripIndex(slotIndex));

        foreach (var (accountIndex, badge) in _badgesBySlot)
        {
            if (!_slotsByIndex.TryGetValue(accountIndex, out var slot))
                continue;

            badge.UpdatePosition(slot.Hwnd);
            badge.SetActive(accountIndex == slotIndex);
            badge.SetVisible(_visible);
        }
    }

    public void Hide()
    {
        _focusTracker.ForegroundChanged -= OnForegroundChanged;
        if (_ownsFocusTracker)
            _focusTracker.Stop();

        DestroyWindows();
        _hwndToSlot.Clear();
        _slotsByIndex.Clear();
        _orderedReadySlots = [];
        _shown = false;
        ActiveSlotIndex = -1;
    }

    public void SetVisible(bool visible)
    {
        _visible = visible;

        if (!OperatingSystem.IsWindows() || !_shown)
            return;

        if (_teamStrip is not null)
            _teamStrip.Visible = visible;

        foreach (var badge in _badgesBySlot.Values)
            badge.SetVisible(visible);
    }

    public void Dispose()
    {
        Hide();
        if (_ownsFocusTracker)
            _focusTracker.Dispose();
    }

    private void CreateTeamStrip(Profile profile)
    {
        _teamStrip = new TeamStripWindow(_iconLoader);
        _teamStrip.IconClicked += OnStripIconClicked;

        var stripSlots = _orderedReadySlots
            .Select(slot => new StripSlot(slot.AccountIndex, slot.Class, slot.Character))
            .ToList();
        _teamStrip.SetSlots(stripSlots);
        _teamStrip.SetActiveIndex(FindStripIndex(ActiveSlotIndex));

        var monitors = _windowManager.GetMonitors();
        var monitorIndex = profile.Overlay.TeamStrip.Monitor;
        if (monitorIndex < 0 || monitorIndex >= monitors.Count)
            monitorIndex = 0;

        _teamStrip.PositionOnMonitor(monitors[monitorIndex], profile.Overlay.TeamStrip.Position);
        _teamStrip.Show();
    }

    private void OnForegroundChanged(object? sender, nint hwnd)
    {
        if (_hwndToSlot.TryGetValue(hwnd, out var slotIndex))
            UpdateFocusedSlot(slotIndex);
    }

    private void OnStripIconClicked(object? sender, int slotIndex) =>
        SlotClicked?.Invoke(this, slotIndex);

    private int FindStripIndex(int slotIndex)
    {
        if (slotIndex < 0)
            return -1;

        for (var i = 0; i < _orderedReadySlots.Count; i++)
        {
            if (_orderedReadySlots[i].AccountIndex == slotIndex)
                return i;
        }

        return -1;
    }

    private void DestroyWindows()
    {
        if (_teamStrip is not null)
        {
            _teamStrip.IconClicked -= OnStripIconClicked;
            _teamStrip.Close();
            _teamStrip.Dispose();
            _teamStrip = null;
        }

        foreach (var badge in _badgesBySlot.Values)
            badge.Dispose();
        _badgesBySlot.Clear();
    }
}
