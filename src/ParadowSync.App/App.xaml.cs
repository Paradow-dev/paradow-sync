using Microsoft.UI.Xaml;
using ParadowSync.App.Services;
using ParadowSync.Automation;
using ParadowSync.Automation.Services;
using ParadowSync.Core.Catalog;
using ParadowSync.Core.Models;
using ParadowSync.Core.Services;
using ParadowSync.Windows.Overlay;
using ParadowSync.Windows.Services;

namespace ParadowSync.App;

public partial class App : Application
{
    private readonly AppSettings _settings = new();
    private readonly ProfileStore _profileStore = new();
    private readonly WindowManager _windowManager = new();
    private readonly FocusTracker _focusTracker = new();
    private readonly IconLoader _iconLoader = new();
    private readonly OverlayManager _overlayManager;
    private readonly ZaapLauncherService _launcher;
    private readonly UiAutomationCharacterSelector _characterSelector;
    private readonly Orchestrator _orchestrator;
    private readonly AppSessionService _session;

    private MainWindow? _mainWindow;
    private TrayService? _tray;
    private HotkeyService? _hotkeys;
    private System.Threading.Timer? _hwndValidationTimer;

    public App()
    {
        InitializeComponent();

        _overlayManager = new OverlayManager(_windowManager, _focusTracker, _iconLoader);
        _launcher = new ZaapLauncherService(_settings);
        _characterSelector = new UiAutomationCharacterSelector();
        _orchestrator = new Orchestrator(_launcher, _characterSelector, _windowManager, _settings);
        _session = new AppSessionService(_orchestrator, _overlayManager, _focusTracker, _windowManager);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow(_profileStore, _session);
        _mainWindow.Activate();

        _hotkeys = new HotkeyService();
        _hotkeys.Register();
        _hotkeys.FocusSlotPressed += OnFocusSlotPressed;
        _hotkeys.ToggleOverlayPressed += OnToggleOverlayPressed;
        _hotkeys.StopAllPressed += OnStopAllPressed;

        _tray = new TrayService(_profileStore);
        _tray.LaunchProfileRequested += OnTrayLaunchProfile;
        _tray.StopAllRequested += OnTrayStopAll;
        _tray.ToggleOverlayRequested += OnTrayToggleOverlay;
        _tray.QuitRequested += OnTrayQuit;
        _tray.ShowMainWindowRequested += OnTrayShowMainWindow;
        _ = RefreshTrayProfilesAsync();

        _hwndValidationTimer = new System.Threading.Timer(
            _ => _session.ValidateHwnds(),
            null,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5));
    }

    public async Task RefreshTrayProfilesAsync()
    {
        if (_tray is not null)
            await _tray.RefreshProfilesAsync().ConfigureAwait(false);
    }

    private void OnFocusSlotPressed(object? sender, int index) =>
        _ = _session.FocusSlotAsync(index);

    private void OnToggleOverlayPressed(object? sender, EventArgs e) =>
        _session.ToggleOverlay();

    private void OnStopAllPressed(object? sender, EventArgs e) =>
        _ = _session.StopAllAsync();

    private void OnTrayLaunchProfile(object? sender, string profileId)
    {
        if (_mainWindow is null)
            return;

        _mainWindow.DispatcherQueue.TryEnqueue(async () =>
        {
            _mainWindow.Activate();
            await _mainWindow.LaunchProfileByIdAsync(profileId);
        });
    }

    private void OnTrayStopAll(object? sender, EventArgs e) =>
        _mainWindow?.DispatcherQueue.TryEnqueue(async () => await _session.StopAllAsync());

    private void OnTrayToggleOverlay(object? sender, EventArgs e) =>
        _session.ToggleOverlay();

    private void OnTrayShowMainWindow(object? sender, EventArgs e)
    {
        if (_mainWindow is not null)
            _mainWindow.Activate();
    }

    private void OnTrayQuit(object? sender, EventArgs e)
    {
        ShutdownServices();
        Environment.Exit(0);
    }

    private void ShutdownServices()
    {
        _hwndValidationTimer?.Dispose();
        _hwndValidationTimer = null;

        if (_hotkeys is not null)
        {
            _hotkeys.FocusSlotPressed -= OnFocusSlotPressed;
            _hotkeys.ToggleOverlayPressed -= OnToggleOverlayPressed;
            _hotkeys.StopAllPressed -= OnStopAllPressed;
            _hotkeys.Dispose();
            _hotkeys = null;
        }

        _tray?.Dispose();
        _tray = null;

        _overlayManager.Dispose();
        _focusTracker.Dispose();
    }
}
