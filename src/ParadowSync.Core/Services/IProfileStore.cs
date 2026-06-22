namespace ParadowSync.Core.Services;

public interface IProfileStore
{
    Task<IReadOnlyList<Models.Profile>> ListAsync(CancellationToken ct = default);
    Task<Models.Profile?> GetAsync(string id, CancellationToken ct = default);
    Task SaveAsync(Models.Profile profile, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
