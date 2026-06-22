namespace ParadowSync.Automation.Services;

public interface ICharacterSelector
{
    Task<bool> SelectCharacterAsync(nint gameHwnd, string characterName, TimeSpan timeout, CancellationToken ct);
}
