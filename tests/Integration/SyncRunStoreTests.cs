using System;
using System.Threading;
using Core.Domain.SyncRuns;
using Infrastructure.Postgres;
using Integration.Support;

namespace Integration;

[Collection("Postgres")]
public sealed class SyncRunStoreTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;

    public SyncRunStoreTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [DockerFact]
    public async Task TryAdd_PersistsRun()
    {
        using var context = new SnapshotDbContext(_fixture.CreateOptions());
        var store = new PostgresSyncRunStore(context);
        var run = BuildRun("run-1");

        var added = await store.TryAddAsync(run, CancellationToken.None);
        var loaded = await store.GetAsync(run.Id, CancellationToken.None);

        Assert.True(added);
        Assert.NotNull(loaded);
        Assert.Equal(run.Id, loaded!.Id);
        Assert.Equal(run.RequestedAt, loaded.RequestedAt);
        Assert.Equal(run.Status, loaded.Status);
    }

    [DockerFact]
    public async Task ListLatest_ReturnsRunsDescending()
    {
        using var context = new SnapshotDbContext(_fixture.CreateOptions());
        var store = new PostgresSyncRunStore(context);
        var first = BuildRun("run-1", requestedAt: DateTimeOffset.Parse("2026-01-03T08:00:00Z"));
        var second = BuildRun("run-2", requestedAt: DateTimeOffset.Parse("2026-01-03T09:00:00Z"));

        await store.TryAddAsync(first, CancellationToken.None);
        await store.TryAddAsync(second, CancellationToken.None);

        var latest = await store.ListLatestAsync(limit: 10, CancellationToken.None);

        Assert.Equal(2, latest.Count);
        Assert.Equal(second.Id, latest[0].Id);
        Assert.Equal(first.Id, latest[1].Id);
    }

    [DockerFact]
    public async Task TryPatch_UpdatesStatusAndCounts()
    {
        using var context = new SnapshotDbContext(_fixture.CreateOptions());
        var store = new PostgresSyncRunStore(context);
        var run = BuildRun("run-3");

        await store.TryAddAsync(run, CancellationToken.None);

        var startedAt = run.RequestedAt.AddSeconds(1);
        var runningUpdate = new SyncRunUpdate(
            Status: SyncRunStatus.Running,
            StartedAt: startedAt,
            SeedsProcessed: 5);

        var runningPatched = await store.TryPatchAsync(run.Id, runningUpdate, CancellationToken.None);

        Assert.True(runningPatched);
        var runningRun = await store.GetAsync(run.Id, CancellationToken.None);
        Assert.Equal(SyncRunStatus.Running, runningRun!.Status);
        Assert.Equal(startedAt, runningRun.StartedAt);
        Assert.Equal(5, runningRun.SeedsProcessed);

        var finishedAt = startedAt.AddMinutes(1);
        var finishedUpdate = new SyncRunUpdate(
            Status: SyncRunStatus.Succeeded,
            FinishedAt: finishedAt,
            SnapshotId: "snap-1",
            ItemsInserted: 50);

        var finishedPatched = await store.TryPatchAsync(run.Id, finishedUpdate, CancellationToken.None);

        Assert.True(finishedPatched);
        var finishedRun = await store.GetAsync(run.Id, CancellationToken.None);
        Assert.Equal(SyncRunStatus.Succeeded, finishedRun!.Status);
        Assert.Equal(finishedAt, finishedRun.FinishedAt);
        Assert.Equal("snap-1", finishedRun.SnapshotId);
        Assert.Equal(50, finishedRun.ItemsInserted);
    }

    private static SyncRun BuildRun(string id, DateTimeOffset? requestedAt = null)
    {
        return new SyncRun(
            id,
            SyncRunStatus.Queued,
            requestedAt ?? DateTimeOffset.Parse("2026-01-03T00:00:00Z"),
            null,
            null,
            null,
            null,
            0,
            0);
    }
}
