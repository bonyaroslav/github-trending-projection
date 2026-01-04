using Core.Domain.Snapshots;
using Infrastructure.Postgres;
using Integration.Support;

namespace Integration;

[Collection("Postgres")]
public sealed class PostgresSnapshotStoreTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;

    public PostgresSnapshotStoreTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [DockerFact]
    public async Task TryAdd_PersistsSnapshotWithRepositories()
    {
        using var context = new SnapshotDbContext(_fixture.CreateOptions());
        var store = new PostgresSnapshotStore(context);
        var snapshot = BuildSnapshot(id: "snap-1", capturedAt: "2026-01-02T18:45:00Z");

        var added = await store.TryAddAsync(snapshot, CancellationToken.None);
        var loaded = await store.GetAsync("snap-1", CancellationToken.None);

        Assert.True(added);
        Assert.NotNull(loaded);
        Assert.Equal(snapshot.Id, loaded!.Id);
        Assert.Single(loaded.Repositories);
    }

    [DockerFact]
    public async Task TryAdd_ReturnsFalse_WhenCapturedAtAndSourceDuplicate()
    {
        using var context = new SnapshotDbContext(_fixture.CreateOptions());
        var store = new PostgresSnapshotStore(context);
        var first = BuildSnapshot(id: "snap-1", capturedAt: "2026-01-02T18:45:00Z");
        var second = BuildSnapshot(id: "snap-2", capturedAt: "2026-01-02T18:45:00Z");

        var addedFirst = await store.TryAddAsync(first, CancellationToken.None);
        var addedSecond = await store.TryAddAsync(second, CancellationToken.None);

        Assert.True(addedFirst);
        Assert.False(addedSecond);
    }

    [DockerFact]
    public async Task TryAdd_RollsBack_WhenRepositoryConstraintViolated()
    {
        using var context = new SnapshotDbContext(_fixture.CreateOptions());
        var store = new PostgresSnapshotStore(context);

        var repositories = new List<RepositorySnapshot>
        {
            BuildRepository(rank: 1, repoId: "repo-1"),
            BuildRepository(rank: 2, repoId: "repo-1")
        };

        var snapshot = new Snapshot(
            "snap-1",
            DateTimeOffset.Parse("2026-01-02T18:45:00Z"),
            "2026-01-02T18:45:00Z",
            "manual",
            "My snapshot",
            "Optional notes",
            repositories);

        var added = await store.TryAddAsync(snapshot, CancellationToken.None);
        var loaded = await store.GetAsync("snap-1", CancellationToken.None);

        Assert.False(added);
        Assert.Null(loaded);
    }

    private static Snapshot BuildSnapshot(string id, string capturedAt)
    {
        return new Snapshot(
            id,
            DateTimeOffset.Parse(capturedAt),
            capturedAt,
            "manual",
            "My snapshot",
            "Optional notes",
            new List<RepositorySnapshot>
            {
                BuildRepository(rank: 1, repoId: "repo-1")
            });
    }

    private static RepositorySnapshot BuildRepository(int rank, string repoId)
    {
        return new RepositorySnapshot(
            repoId,
            rank,
            "octocat",
            "hello-world",
            "octocat/hello-world",
            "Example repo",
            "C#",
            1234,
            56,
            "https://github.com/octocat/hello-world",
            "2025-12-31T12:00:00Z");
    }
}
