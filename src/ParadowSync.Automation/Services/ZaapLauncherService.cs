using System.Diagnostics;
using ParadowSync.Core.Models;

namespace ParadowSync.Automation.Services;

public sealed class ZaapLauncherService : ILauncherService
{
    // Assumed Zaap CLI (spike TBD): zaap.exe --game dofus-unity --account <accountId>
    private const string DefaultArgumentTemplate = "--game dofus-unity --account {accountId}";

    private readonly string _launcherPath;
    private readonly string _argumentTemplate;

    public ZaapLauncherService(AppSettings settings)
        : this(settings.LauncherPath)
    {
    }

    public ZaapLauncherService(string launcherPath, string? argumentTemplate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(launcherPath);
        _launcherPath = launcherPath;
        _argumentTemplate = argumentTemplate ?? DefaultArgumentTemplate;
    }

    public Task LaunchAccountAsync(string accountId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ct.ThrowIfCancellationRequested();

        var arguments = _argumentTemplate.Replace("{accountId}", accountId, StringComparison.Ordinal);
        var startInfo = new ProcessStartInfo
        {
            FileName = _launcherPath,
            Arguments = arguments,
            UseShellExecute = false,
        };

        Process.Start(startInfo);
        return Task.CompletedTask;
    }
}
