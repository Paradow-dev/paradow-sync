namespace ParadowSync.Core.Models;

public sealed class Profile
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required List<AccountSlot> Accounts { get; init; }
    public OverlayConfig Overlay { get; init; } = new();
    public Dictionary<string, string> Hotkeys { get; init; } = new();
}

public sealed class OverlayConfig
{
    public TeamStripConfig TeamStrip { get; init; } = new();
    public WindowBadgeConfig WindowBadges { get; init; } = new();
}

public sealed class TeamStripConfig
{
    public bool Enabled { get; init; } = true;
    public string Position { get; init; } = "top";
    public int Monitor { get; init; }
}

public sealed class WindowBadgeConfig
{
    public bool Enabled { get; init; } = true;
}
