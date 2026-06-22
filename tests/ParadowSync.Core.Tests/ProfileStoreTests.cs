using ParadowSync.Core.Models;
using ParadowSync.Core.Services;

namespace ParadowSync.Core.Tests;

public class ProfileStoreTests : IDisposable
{
    private readonly string _dir;
    private readonly ProfileStore _store;

    public ProfileStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "paradow-sync-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_dir);
        _store = new ProfileStore(_dir);
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public async Task SaveAndGet_roundtrips_profile()
    {
        var profile = new Profile
        {
            Id = "test-1",
            Name = "Team Test",
            Accounts =
            [
                new AccountSlot
                {
                    AccountId = "acc-1",
                    Character = "Iopette",
                    Class = "Iop",
                    Monitor = 0,
                    Slot = new WindowSlot { X = 0, Y = 0, W = 960, H = 540 }
                }
            ]
        };

        await _store.SaveAsync(profile);
        var loaded = await _store.GetAsync("test-1");

        Assert.NotNull(loaded);
        Assert.Equal("test-1", loaded.Id);
        Assert.Equal("Team Test", loaded.Name);
        Assert.Equal("Iopette", loaded.Accounts[0].Character);
    }

    [Fact]
    public async Task ListAsync_returns_all_saved_profiles()
    {
        var profile1 = new Profile
        {
            Id = "list-1",
            Name = "Profile One",
            Accounts = []
        };
        var profile2 = new Profile
        {
            Id = "list-2",
            Name = "Profile Two",
            Accounts = []
        };

        await _store.SaveAsync(profile1);
        await _store.SaveAsync(profile2);

        var profiles = await _store.ListAsync();

        Assert.Equal(2, profiles.Count);
        Assert.Contains(profiles, p => p.Id == "list-1");
        Assert.Contains(profiles, p => p.Id == "list-2");
    }

    [Fact]
    public async Task DeleteAsync_removes_profile()
    {
        var profile = new Profile
        {
            Id = "delete-me",
            Name = "To Delete",
            Accounts = []
        };

        await _store.SaveAsync(profile);
        Assert.NotNull(await _store.GetAsync("delete-me"));

        await _store.DeleteAsync("delete-me");

        Assert.Null(await _store.GetAsync("delete-me"));
    }
}
