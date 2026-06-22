using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ParadowSync.App.ViewModels;
using ParadowSync.Core.Models;
using ParadowSync.Core.Services;

namespace ParadowSync.App;

public sealed partial class ProfileEditorPage : Page
{
    private readonly IProfileStore _profileStore;
    private readonly ProfileEditorViewModel _viewModel = new();
    private Window? _hostWindow;

    public IReadOnlyList<string> ClassNames => _viewModel.Classes;

    public ProfileEditorPage(IProfileStore profileStore)
    {
        _profileStore = profileStore;
        InitializeComponent();
        BindViewModel();
    }

    public event EventHandler? ProfileSaved;

    public Task ShowForCreateAsync(Window owner)
    {
        _viewModel.ResetForCreate();
        ApplyViewModelToUi();
        return ShowInWindowAsync(owner, "Nouveau profil");
    }

    public Task ShowForEditAsync(Window owner, Profile profile)
    {
        _viewModel.LoadFrom(profile);
        ApplyViewModelToUi();
        return ShowInWindowAsync(owner, $"Modifier — {profile.Name}");
    }

    private void BindViewModel()
    {
        DataContext = _viewModel;
        AccountsList.ItemsSource = _viewModel.Accounts;
        TeamStripPositionBox.ItemsSource = _viewModel.TeamStripPositions;
    }

    private void ApplyViewModelToUi()
    {
        NameBox.Text = _viewModel.Name;
        TeamStripEnabledBox.IsChecked = _viewModel.TeamStripEnabled;
        TeamStripPositionBox.SelectedItem = _viewModel.TeamStripPosition;
        TeamStripMonitorBox.Text = _viewModel.TeamStripMonitor.ToString();
        BadgesEnabledBox.IsChecked = _viewModel.BadgesEnabled;
        HotkeyFocusBox.Text = _viewModel.HotkeyFocus;
        HotkeyToggleOverlayBox.Text = _viewModel.HotkeyToggleOverlay;
        HotkeyStopAllBox.Text = _viewModel.HotkeyStopAll;
    }

    private void ReadUiIntoViewModel()
    {
        _viewModel.Name = NameBox.Text;
        _viewModel.TeamStripEnabled = TeamStripEnabledBox.IsChecked == true;
        _viewModel.TeamStripPosition = TeamStripPositionBox.SelectedItem as string ?? "top";
        _viewModel.TeamStripMonitor = int.TryParse(TeamStripMonitorBox.Text, out var monitor) ? monitor : 0;
        _viewModel.BadgesEnabled = BadgesEnabledBox.IsChecked == true;
        _viewModel.HotkeyFocus = HotkeyFocusBox.Text;
        _viewModel.HotkeyToggleOverlay = HotkeyToggleOverlayBox.Text;
        _viewModel.HotkeyStopAll = HotkeyStopAllBox.Text;
    }

    private Task ShowInWindowAsync(Window owner, string title)
    {
        var tcs = new TaskCompletionSource();

        _hostWindow = new Window
        {
            Title = title,
        };

        _hostWindow.Content = this;

        _hostWindow.Closed += (_, _) => tcs.TrySetResult();
        _hostWindow.Activate();

        return tcs.Task;
    }

    private void OnAddAccountClick(object sender, RoutedEventArgs e) => _viewModel.AddAccount();

    private void OnRemoveAccountClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Accounts.Count > 0)
            _viewModel.RemoveAccount(_viewModel.Accounts[^1]);
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        ReadUiIntoViewModel();

        if (string.IsNullOrWhiteSpace(_viewModel.Name))
        {
            await ShowMessageAsync("Le nom du profil est requis.");
            return;
        }

        if (_viewModel.Accounts.Count == 0)
        {
            await ShowMessageAsync("Ajoutez au moins un compte.");
            return;
        }

        if (_viewModel.Accounts.Any(a => string.IsNullOrWhiteSpace(a.AccountId) || string.IsNullOrWhiteSpace(a.Character)))
        {
            await ShowMessageAsync("Chaque compte doit avoir un Account ID et un personnage.");
            return;
        }

        var profile = _viewModel.ToProfile();
        await _profileStore.SaveAsync(profile);
        ProfileSaved?.Invoke(this, EventArgs.Empty);
        await ((App)Application.Current).RefreshTrayProfilesAsync();
        CloseHost();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => CloseHost();

    private void CloseHost()
    {
        if (_hostWindow is not null)
            _hostWindow.Close();
    }

    private async Task ShowMessageAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "paradow-sync",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot,
        };

        await dialog.ShowAsync();
    }
}
