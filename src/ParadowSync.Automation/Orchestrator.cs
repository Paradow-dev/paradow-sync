using ParadowSync.Core.Models;
using ParadowSync.Core.Services;
using ParadowSync.Automation.Services;
using ParadowSync.Windows.Services;

namespace ParadowSync.Automation;

public sealed class Orchestrator : IOrchestrator
{
    private static readonly TimeSpan GameWindowTimeout = TimeSpan.FromSeconds(60);

    private readonly ILauncherService _launcher;
    private readonly ICharacterSelector _characterSelector;
    private readonly IWindowManager _windowManager;
    private readonly AppSettings _settings;

    private SessionState? _currentSession;

    public Orchestrator(
        ILauncherService launcher,
        ICharacterSelector characterSelector,
        IWindowManager windowManager,
        AppSettings settings)
    {
        _launcher = launcher;
        _characterSelector = characterSelector;
        _windowManager = windowManager;
        _settings = settings;
    }

    public SessionState? CurrentSession => _currentSession;

    public async Task LaunchProfileAsync(Profile profile, CancellationToken ct = default)
    {
        var slots = new List<RuntimeSlot>();
        _currentSession = new SessionState
        {
            ActiveProfileId = profile.Id,
            Slots = slots,
        };

        for (var i = 0; i < profile.Accounts.Count; i++)
        {
            var account = profile.Accounts[i];
            var runtimeSlot = new RuntimeSlot
            {
                AccountIndex = i,
                Character = account.Character,
                Class = account.Class,
                Status = SlotStatus.Launching,
            };
            slots.Add(runtimeSlot);

            await _launcher.LaunchAccountAsync(account.AccountId, ct).ConfigureAwait(false);

            runtimeSlot.Status = SlotStatus.SelectingCharacter;

            nint hwnd;
            try
            {
                hwnd = await _windowManager
                    .WaitForGameWindowAsync("Dofus", GameWindowTimeout, ct)
                    .ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                runtimeSlot.Status = SlotStatus.Error;
                continue;
            }

            runtimeSlot.Hwnd = hwnd;

            var selectTimeout = TimeSpan.FromMilliseconds(_settings.CharacterSelectTimeoutMs);
            var success = await _characterSelector
                .SelectCharacterAsync(hwnd, account.Character, selectTimeout, ct)
                .ConfigureAwait(false);

            runtimeSlot.Status = success ? SlotStatus.Ready : SlotStatus.ManualRequired;

            _windowManager.PlaceWindow(hwnd, account.Monitor, account.Slot);

            if (i < profile.Accounts.Count - 1)
                await Task.Delay(_settings.LaunchDelayMs, ct).ConfigureAwait(false);
        }
    }

    public Task StopAllAsync(CancellationToken ct = default)
    {
        _currentSession = null;
        return Task.CompletedTask;
    }

    public Task FocusSlotAsync(int index, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var session = _currentSession;
        if (session is null || index < 0 || index >= session.Slots.Count)
            return Task.CompletedTask;

        var slot = session.Slots[index];
        if (slot.Hwnd != nint.Zero)
            _windowManager.FocusWindow(slot.Hwnd);

        session.FocusedSlotIndex = index;
        return Task.CompletedTask;
    }
}
