namespace ParadowSync.Core.Models;

public sealed class AppSettings
{
    public string LauncherPath { get; init; } = @"C:\Program Files\Ankama\Zaap\zaap.exe";
    public int LaunchDelayMs { get; init; } = 3000;
    public int CharacterSelectTimeoutMs { get; init; } = 30000;
    public double OverlayOpacity { get; init; } = 0.85;
}
