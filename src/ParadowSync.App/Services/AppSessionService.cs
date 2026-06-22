using ParadowSync.Core.Models;
using ParadowSync.Core.Services;
using ParadowSync.Windows.Overlay;
using ParadowSync.Windows.Services;

namespace ParadowSync.App.Services;

public sealed class AppSessionService
{
    private readonly IOrchestrator _orchestrator;
    private readonly IOverlayManager _overlayManager;
    private readonly IFocusTracker _focusTracker;
    private readonly IWindowManager _windowManager;

    private Profile? _activeProfile;
    private bool _overlayVisible = true;

    public AppSessionService(
        IOrchestrator orchestrator,
        IOverlayManager overlayManager,
        IFocusTracker focusTracker,
        IWindowManager windowManager)
    {
        _orchestrator = orchestrator;
        _overlayManager = overlayManager;
        _focusTracker = focusTracker;
        _windowManager = windowManager;
    }

    public Profile? ActiveProfile => _activeProfile;
    public bool IsSessionActive => _orchestrator.CurrentSession is not null;
    public bool OverlayVisible => _overlayVisible;

    public event EventHandler? SessionChanged;

    public async Task LaunchProfileAsync(Profile profile, CancellationToken ct = default)
    {
        await StopAllAsync(ct).ConfigureAwait(false);
        await _orchestrator.LaunchProfileAsync(profile, ct).ConfigureAwait(false);

        var session = _orchestrator.CurrentSession;
        if (session is null)
            return;

        _activeProfile = profile;
        _overlayVisible = true;
        _overlayManager.SlotClicked += OnOverlaySlotClicked;
        _overlayManager.Show(session, profile);
        _focusTracker.Start();

        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task StopAllAsync(CancellationToken ct = default)
    {
        _overlayManager.SlotClicked -= OnOverlaySlotClicked;
        _overlayManager.Hide();
        _focusTracker.Stop();
        await _orchestrator.StopAllAsync(ct).ConfigureAwait(false);
        _activeProfile = null;
        _overlayVisible = true;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ToggleOverlay()
    {
        if (!IsSessionActive)
            return;

        _overlayVisible = !_overlayVisible;
        _overlayManager.SetVisible(_overlayVisible);
    }

    public async Task FocusSlotAsync(int index, CancellationToken ct = default)
    {
        await _orchestrator.FocusSlotAsync(index, ct).ConfigureAwait(false);

        var session = _orchestrator.CurrentSession;
        if (session?.FocusedSlotIndex is int focused)
            _overlayManager.UpdateFocusedSlot(focused);
    }

    public void ValidateHwnds()
    {
        var session = _orchestrator.CurrentSession;
        if (session is null)
            return;

        var changed = false;
        foreach (var slot in session.Slots)
        {
            if (slot.Hwnd == nint.Zero || slot.Status != SlotStatus.Ready)
                continue;

            if (_windowManager.IsWindowValid(slot.Hwnd))
                continue;

            slot.Status = SlotStatus.Error;
            changed = true;
        }

        if (changed)
            SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnOverlaySlotClicked(object? sender, int slotIndex) =>
        _ = FocusSlotAsync(slotIndex);
}
