namespace ParadowSync.Automation.Services;

public interface ILauncherService
{
    Task LaunchAccountAsync(string accountId, CancellationToken ct = default);
}
