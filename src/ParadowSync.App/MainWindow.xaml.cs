using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ParadowSync.App.Services;
using ParadowSync.App.ViewModels;
using ParadowSync.Core.Services;

namespace ParadowSync.App;

public sealed partial class MainWindow : Window
{
    private readonly ProfileListViewModel _viewModel;
    private readonly IProfileStore _profileStore;
    private readonly AppSessionService _session;

    public MainWindow(IProfileStore profileStore, AppSessionService session)
    {
        _profileStore = profileStore;
        _session = session;
        _viewModel = new ProfileListViewModel(profileStore);

        InitializeComponent();
        ProfileListView.ItemsSource = _viewModel.Profiles;
        ProfileListView.SelectionChanged += OnSelectionChanged;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ProfileListViewModel.StatusText))
                StatusTextBlock.Text = _viewModel.StatusText;
        };

        _ = LoadProfilesAsync();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _viewModel.SelectedProfile = ProfileListView.SelectedItem as ProfileSummary;
    }

    private async Task LoadProfilesAsync()
    {
        try
        {
            await _viewModel.LoadAsync();
            if (_viewModel.SelectedProfile is not null)
                ProfileListView.SelectedItem = _viewModel.SelectedProfile;
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"Erreur chargement: {ex.Message}");
        }
    }

    private async void OnNewClick(object sender, RoutedEventArgs e)
    {
        var editor = new ProfileEditorPage(_profileStore);
        editor.ProfileSaved += async (_, _) => await LoadProfilesAsync();
        await editor.ShowForCreateAsync(this);
    }

    private async void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedProfile is null)
        {
            _viewModel.SetStatus("Sélectionnez un profil à modifier.");
            return;
        }

        var profile = await _profileStore.GetAsync(_viewModel.SelectedProfile.Id);
        if (profile is null)
        {
            _viewModel.SetStatus("Profil introuvable.");
            return;
        }

        var editor = new ProfileEditorPage(_profileStore);
        editor.ProfileSaved += async (_, _) => await LoadProfilesAsync();
        await editor.ShowForEditAsync(this, profile);
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedProfile is null)
        {
            _viewModel.SetStatus("Sélectionnez un profil à supprimer.");
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Supprimer le profil",
            Content = $"Supprimer « {_viewModel.SelectedProfile.Name} » ?",
            PrimaryButtonText = "Supprimer",
            CloseButtonText = "Annuler",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        await _profileStore.DeleteAsync(_viewModel.SelectedProfile.Id);
        await LoadProfilesAsync();
        _viewModel.SetStatus("Profil supprimé.");
        await ((App)Application.Current).RefreshTrayProfilesAsync();
    }

    private async void OnLaunchClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedProfile is null)
        {
            _viewModel.SetStatus("Sélectionnez un profil à lancer.");
            return;
        }

        var profile = await _profileStore.GetAsync(_viewModel.SelectedProfile.Id);
        if (profile is null)
        {
            _viewModel.SetStatus("Profil introuvable.");
            return;
        }

        _viewModel.SetStatus($"Lancement de {profile.Name}...");
        LaunchButton.IsEnabled = false;

        try
        {
            await _session.LaunchProfileAsync(profile);
            _viewModel.SetStatus($"Session active: {profile.Name}");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"Erreur lancement: {ex.Message}");
        }
        finally
        {
            LaunchButton.IsEnabled = true;
        }
    }

    private async void OnStopClick(object sender, RoutedEventArgs e)
    {
        await _session.StopAllAsync();
        _viewModel.SetStatus("Session arrêtée.");
    }

    public async Task LaunchProfileByIdAsync(string profileId)
    {
        var profile = await _profileStore.GetAsync(profileId);
        if (profile is null)
        {
            _viewModel.SetStatus("Profil introuvable.");
            return;
        }

        _viewModel.SetStatus($"Lancement de {profile.Name}...");
        try
        {
            await _session.LaunchProfileAsync(profile);
            _viewModel.SetStatus($"Session active: {profile.Name}");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatus($"Erreur lancement: {ex.Message}");
        }
    }
}
