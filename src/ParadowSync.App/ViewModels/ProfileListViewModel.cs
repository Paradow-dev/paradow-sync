using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ParadowSync.Core.Models;
using ParadowSync.Core.Services;

namespace ParadowSync.App.ViewModels;

public sealed class ProfileListViewModel : INotifyPropertyChanged
{
    private readonly IProfileStore _profileStore;
    private ProfileSummary? _selectedProfile;
    private string _statusText = "Prêt";

    public ProfileListViewModel(IProfileStore profileStore)
    {
        _profileStore = profileStore;
        Profiles = new ObservableCollection<ProfileSummary>();
    }

    public ObservableCollection<ProfileSummary> Profiles { get; }

    public ProfileSummary? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (Equals(_selectedProfile, value))
                return;

            _selectedProfile = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText == value)
                return;

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var profiles = await _profileStore.ListAsync(ct).ConfigureAwait(false);
        Profiles.Clear();

        foreach (var profile in profiles.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
            Profiles.Add(new ProfileSummary(profile.Id, profile.Name, profile.Accounts.Count));

        SelectedProfile = Profiles.FirstOrDefault();
        StatusText = $"{Profiles.Count} profil(s) chargé(s)";
    }

    public void SetStatus(string text) => StatusText = text;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record ProfileSummary(string Id, string Name, int AccountCount)
{
    public string AccountLabel => $"{AccountCount} compte(s)";
}
