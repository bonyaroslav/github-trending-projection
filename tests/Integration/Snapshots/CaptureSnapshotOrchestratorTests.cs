using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Common;
using Core.Application.GitHub;
using Core.Application.Snapshots;
using Core.Application.Sources;
using Core.Application.SyncRuns;
using Core.Domain.SyncRuns;
using Infrastructure.Postgres;
using Integration.Support;
using Tests.Shared;
using Xunit;

namespace Integration.Snapshots;

[Collection("Postgres")]
public sealed class CaptureSnapshotOrchestratorTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;

    public CaptureSnapshotOrchestratorTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync() => await _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [DockerFact]
    public async Task Capture_InsertsSnapshotAndRecordsSyncRun()
    {
        using var snapshotContext = new SnapshotDbContext(_fixture.CreateOptions());
        using var syncRunContext = new SnapshotDbContext(_fixture.CreateOptions());

        var snapshotStore = new PostgresSnapshotStore(snapshotContext);
        var syncRunStore = new PostgresSyncRunStore(syncRunContext);

        var orchestrator = BuildOrchestrator(snapshotStore, syncRunStore, new TestClock(new[]
        {
            DateTimeOffset.Parse("2026-01-03T08:00:00Z"),
            DateTimeOffset.Parse("2026-01-03T08:00:05Z"),
            DateTimeOffset.Parse("2026-01-03T08:00:10Z"),
            DateTimeOffset.Parse("2026-01-03T08:00:15Z")
        }), new TestIdGenerator("run-1", "snap-1"));

        var result = await orchestrator.CaptureAsync(
            new CaptureSnapshotOptions("github-trending", "Phase3 capture", null),
            CancellationToken.None);

        var snapshot = await snapshotStore.GetAsync(result.SnapshotId, CancellationToken.None);
        Assert.NotNull(snapshot);
        Assert.Equal(2, snapshot!.Repositories.Count);
        Assert.Equal("repo-1", snapshot.Repositories[0].RepoId);

        var syncRun = await syncRunStore.GetAsync(result.SyncRunId, CancellationToken.None);
        Assert.NotNull(syncRun);
        Assert.Equal(SyncRunStatus.Succeeded, syncRun!.Status);
        Assert.Equal(2, syncRun.SeedsProcessed);
        Assert.Equal(2, syncRun.ItemsInserted);
        Assert.Equal(SyncRunFailureCode.None, syncRun.FailureCode);
    }

    [DockerFact]
    public async Task Capture_ConflictsSnapshotAndFailsRun()
    {
        using var snapshotContext = new SnapshotDbContext(_fixture.CreateOptions());
        using var syncRunContext = new SnapshotDbContext(_fixture.CreateOptions());

        var snapshotStore = new PostgresSnapshotStore(snapshotContext);
        var syncRunStore = new PostgresSyncRunStore(syncRunContext);

        var baseClock = new TestClock(new[]
        {
            DateTimeOffset.Parse("2026-01-03T08:00:00Z"),
            DateTimeOffset.Parse("2026-01-03T08:00:05Z"),
            DateTimeOffset.Parse("2026-01-03T08:00:10Z"),
            DateTimeOffset.Parse("2026-01-03T08:00:15Z")
        });

        var captureClock = new TestClock(new[]
        {
            DateTimeOffset.Parse("2026-01-03T09:00:00Z"),
            DateTimeOffset.Parse("2026-01-03T09:00:05Z"),
            DateTimeOffset.Parse("2026-01-03T09:00:10Z"),
            DateTimeOffset.Parse("2026-01-03T09:00:15Z")
        });

        var firstOrchestrator = BuildOrchestrator(snapshotStore, syncRunStore, baseClock, new TestIdGenerator("run-1", "snap-1"));
        await firstOrchestrator.CaptureAsync(
            new CaptureSnapshotOptions("github-trending", "Initial capture", null),
            CancellationToken.None);

        var secondOrchestrator = BuildOrchestrator(snapshotStore, syncRunStore, captureClock, new TestIdGenerator("run-2", "snap-2"));

        await Assert.ThrowsAsync<SnapshotConflictException>(() => secondOrchestrator.CaptureAsync(
            new CaptureSnapshotOptions("github-trending", "Conflict capture", null),
            CancellationToken.None));

        var failedRun = await syncRunStore.GetAsync("run-2", CancellationToken.None);
        Assert.NotNull(failedRun);
        Assert.Equal(SyncRunStatus.Failed, failedRun!.Status);
        Assert.Equal(SyncRunFailureCode.SnapshotConflict, failedRun.FailureCode);
        Assert.Contains("Snapshot conflict", failedRun.Error);

        var snapshots = await snapshotStore.ListAsync(CancellationToken.None);
        Assert.Single(snapshots);
    }

    private static CaptureSnapshotOrchestrator BuildOrchestrator(
        ISnapshotStore snapshotStore,
        ISyncRunStore syncRunStore,
        IClock clock,
        IIdGenerator generator)
    {
        return new CaptureSnapshotOrchestrator(
            new FakeTrendingSeedProvider(CreateDefaultSeeds()),
            new FakeGraphQlClient(CreateEnrichments()),
            snapshotStore,
            syncRunStore,
            clock,
            generator);
    }

    private static IReadOnlyList<RepositorySeed> CreateDefaultSeeds() => new[]
    {
        new RepositorySeed("octocat", "hello-world", 1),
        new RepositorySeed("dotnet", "roslyn", 2)
    };

    private static Dictionary<(string Owner, string Name), RepositoryEnrichment> CreateEnrichments() =>
        new()
        {
            [("octocat", "hello-world")] = new RepositoryEnrichment(
                "repo-1",
                "octocat",
                "hello-world",
                "octocat/hello-world",
                "Example repo",
                "C#",
                1_000,
                42,
                "https://github.com/octocat/hello-world",
                DateTimeOffset.Parse("2025-12-31T12:00:00Z")),
            [("dotnet", "roslyn")] = new RepositoryEnrichment(
                "repo-2",
                "dotnet",
                "roslyn",
                "dotnet/roslyn",
                "Compiler repo",
                "C#",
                5_000,
                120,
                "https://github.com/dotnet/roslyn",
                DateTimeOffset.Parse("2026-01-01T08:00:00Z"))
        };
}
