using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ParadowSync.Core.Catalog;
using ParadowSync.Core.Models;

namespace ParadowSync.App.ViewModels;

public sealed class ProfileEditorViewModel : INotifyPropertyChanged
{
    private string _name = "Nouveau profil";
    private bool _teamStripEnabled = true;
    private string _teamStripPosition = "top";
    private int _teamStripMonitor;
    private bool _badgesEnabled = true;
    private string _hotkeyFocus = "Ctrl+1..8";
    private string _hotkeyToggleOverlay = "Ctrl+Shift+O";
    private string _hotkeyStopAll = "Ctrl+Shift+Q";

    public ProfileEditorViewModel()
    {
        Classes = new ObservableCollection<string>(ClassCatalog.All.Select(c => c.Name).OrderBy(n => n));
        Accounts = new ObservableCollection<AccountSlotEditModel>();
        TeamStripPositions = ["top", "bottom"];
    }

    public string? ProfileId { get; private set; }

    public ObservableCollection<string> Classes { get; }
    public ObservableCollection<AccountSlotEditModel> Accounts { get; }
    public IReadOnlyList<string> TeamStripPositions { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
                return;

            _name = value;
            OnPropertyChanged();
        }
    }

    public bool TeamStripEnabled
    {
        get => _teamStripEnabled;
        set
        {
            if (_teamStripEnabled == value)
                return;

            _teamStripEnabled = value;
            OnPropertyChanged();
        }
    }

    public string TeamStripPosition
    {
        get => _teamStripPosition;
        set
        {
            if (_teamStripPosition == value)
                return;

            _teamStripPosition = value;
            OnPropertyChanged();
        }
    }

    public int TeamStripMonitor
    {
        get => _teamStripMonitor;
        set
        {
            if (_teamStripMonitor == value)
                return;

            _teamStripMonitor = value;
            OnPropertyChanged();
        }
    }

    public bool BadgesEnabled
    {
        get => _badgesEnabled;
        set
        {
            if (_badgesEnabled == value)
                return;

            _badgesEnabled = value;
            OnPropertyChanged();
        }
    }

    public string HotkeyFocus
    {
        get => _hotkeyFocus;
        set
        {
            if (_hotkeyFocus == value)
                return;

            _hotkeyFocus = value;
            OnPropertyChanged();
        }
    }

    public string HotkeyToggleOverlay
    {
        get => _hotkeyToggleOverlay;
        set
        {
            if (_hotkeyToggleOverlay == value)
                return;

            _hotkeyToggleOverlay = value;
            OnPropertyChanged();
        }
    }

    public string HotkeyStopAll
    {
        get => _hotkeyStopAll;
        set
        {
            if (_hotkeyStopAll == value)
                return;

            _hotkeyStopAll = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void LoadFrom(Profile profile)
    {
        ProfileId = profile.Id;
        Name = profile.Name;
        TeamStripEnabled = profile.Overlay.TeamStrip.Enabled;
        TeamStripPosition = profile.Overlay.TeamStrip.Position;
        TeamStripMonitor = profile.Overlay.TeamStrip.Monitor;
        BadgesEnabled = profile.Overlay.WindowBadges.Enabled;

        HotkeyFocus = profile.Hotkeys.GetValueOrDefault("focus", "Ctrl+1..8");
        HotkeyToggleOverlay = profile.Hotkeys.GetValueOrDefault("toggleOverlay", "Ctrl+Shift+O");
        HotkeyStopAll = profile.Hotkeys.GetValueOrDefault("stopAll", "Ctrl+Shift+Q");

        Accounts.Clear();
        foreach (var account in profile.Accounts)
        {
            Accounts.Add(new AccountSlotEditModel
            {
                AccountId = account.AccountId,
                Character = account.Character,
                Class = account.Class,
                Monitor = account.Monitor,
                X = account.Slot.X,
                Y = account.Slot.Y,
                W = account.Slot.W,
                H = account.Slot.H,
            });
        }
    }

    public void ResetForCreate()
    {
        ProfileId = null;
        Name = "Nouveau profil";
        TeamStripEnabled = true;
        TeamStripPosition = "top";
        TeamStripMonitor = 0;
        BadgesEnabled = true;
        HotkeyFocus = "Ctrl+1..8";
        HotkeyToggleOverlay = "Ctrl+Shift+O";
        HotkeyStopAll = "Ctrl+Shift+Q";
        Accounts.Clear();
        Accounts.Add(new AccountSlotEditModel());
    }

    public Profile ToProfile()
    {
        var id = ProfileId ?? Guid.NewGuid().ToString("N");
        return new Profile
        {
            Id = id,
            Name = Name.Trim(),
            Accounts = Accounts
                .Select(a => new AccountSlot
                {
                    AccountId = a.AccountId.Trim(),
                    Character = a.Character.Trim(),
                    Class = a.Class,
                    Monitor = a.Monitor,
                    Slot = new WindowSlot
                    {
                        X = a.X,
                        Y = a.Y,
                        W = a.W,
                        H = a.H,
                    },
                })
                .ToList(),
            Overlay = new OverlayConfig
            {
                TeamStrip = new TeamStripConfig
                {
                    Enabled = TeamStripEnabled,
                    Position = TeamStripPosition,
                    Monitor = TeamStripMonitor,
                },
                WindowBadges = new WindowBadgeConfig
                {
                    Enabled = BadgesEnabled,
                },
            },
            Hotkeys = new Dictionary<string, string>
            {
                ["focus"] = HotkeyFocus,
                ["toggleOverlay"] = HotkeyToggleOverlay,
                ["stopAll"] = HotkeyStopAll,
            },
        };
    }

    public void AddAccount() => Accounts.Add(new AccountSlotEditModel());

    public void RemoveAccount(AccountSlotEditModel account)
    {
        if (Accounts.Contains(account))
            Accounts.Remove(account);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class AccountSlotEditModel : INotifyPropertyChanged
{
    private string _accountId = "";
    private string _character = "";
    private string _class = "Iop";
    private int _monitor;
    private int _x;
    private int _y = 100;
    private int _w = 960;
    private int _h = 540;

    public string AccountId
    {
        get => _accountId;
        set
        {
            if (_accountId == value)
                return;

            _accountId = value;
            OnPropertyChanged();
        }
    }

    public string Character
    {
        get => _character;
        set
        {
            if (_character == value)
                return;

            _character = value;
            OnPropertyChanged();
        }
    }

    public string Class
    {
        get => _class;
        set
        {
            if (_class == value)
                return;

            _class = value;
            OnPropertyChanged();
        }
    }

    public int Monitor
    {
        get => _monitor;
        set
        {
            if (_monitor == value)
                return;

            _monitor = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MonitorText));
        }
    }

    public int X
    {
        get => _x;
        set
        {
            if (_x == value)
                return;

            _x = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(XText));
        }
    }

    public int Y
    {
        get => _y;
        set
        {
            if (_y == value)
                return;

            _y = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(YText));
        }
    }

    public int W
    {
        get => _w;
        set
        {
            if (_w == value)
                return;

            _w = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(WText));
        }
    }

    public int H
    {
        get => _h;
        set
        {
            if (_h == value)
                return;

            _h = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HText));
        }
    }

    public string MonitorText
    {
        get => Monitor.ToString();
        set
        {
            if (int.TryParse(value, out var parsed))
                Monitor = parsed;
            else
                OnPropertyChanged();
        }
    }

    public string XText
    {
        get => X.ToString();
        set
        {
            if (int.TryParse(value, out var parsed))
                X = parsed;
            else
                OnPropertyChanged();
        }
    }

    public string YText
    {
        get => Y.ToString();
        set
        {
            if (int.TryParse(value, out var parsed))
                Y = parsed;
            else
                OnPropertyChanged();
        }
    }

    public string WText
    {
        get => W.ToString();
        set
        {
            if (int.TryParse(value, out var parsed))
                W = parsed;
            else
                OnPropertyChanged();
        }
    }

    public string HText
    {
        get => H.ToString();
        set
        {
            if (int.TryParse(value, out var parsed))
                H = parsed;
            else
                OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
