using ParadowSync.Core.Models;

namespace ParadowSync.Core.Services;

public interface IOrchestrator
{
    SessionState? CurrentSession { get; }
    Task LaunchProfileAsync(Models.Profile profile, CancellationToken ct = default);
    Task StopAllAsync(CancellationToken ct = default);
    Task FocusSlotAsync(int index, CancellationToken ct = default);
}
