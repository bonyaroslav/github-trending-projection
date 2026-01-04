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
using Infrastructure.Memory;
using Tests.Shared;
using Xunit;

namespace Unit.Snapshots;

public sealed class CaptureSnapshotOrchestratorTests
{
    [Fact]
    public async Task Capture_PersistsSnapshotAndSyncRun()
    {
        var clock = new TestClock(new[]
        {
            DateTimeOffset.Parse("2026-01-03T08:00:00Z"),
            DateTimeOffset.Parse("2026-01-03T08:00:05Z"),
            DateTimeOffset.Parse("2026-01-03T08:00:10Z"),
            DateTimeOffset.Parse("2026-01-03T08:00:15Z")
        });

        var idGenerator = new TestIdGenerator(new[] { "run-1", "snap-1" });

        var trending = new FakeTrendingSeedProvider(new[]
        {
            new RepositorySeed("octocat", "hello-world", 1),
            new RepositorySeed("dotnet", "roslyn", 2)
        });
        var graphQl = new FakeGraphQlClient(new Dictionary<(string Owner, string Name), RepositoryEnrichment>
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
        });

        var snapshotStore = new InMemorySnapshotStore();
        var syncRunStore = new TestSyncRunStore();

        var orchestrator = new CaptureSnapshotOrchestrator(
            trending,
            graphQl,
            snapshotStore,
            syncRunStore,
            clock,
            idGenerator);

        var result = await orchestrator.CaptureAsync(
            new CaptureSnapshotOptions("github-trending", Name: "Phase3 capture", Notes: null),
            CancellationToken.None);

        Assert.Equal("run-1", result.SyncRunId);
        Assert.Equal("snap-1", result.SnapshotId);

        var snapshot = await snapshotStore.GetAsync("snap-1", CancellationToken.None);
        Assert.NotNull(snapshot);
        Assert.Equal(2, snapshot!.Repositories.Count);
        Assert.Equal("repo-1", snapshot.Repositories[0].RepoId);
        Assert.Equal("repo-2", snapshot.Repositories[1].RepoId);

        Assert.Single(syncRunStore.AddedRuns);
        Assert.Equal(SyncRunStatus.Queued, syncRunStore.AddedRuns[0].Status);

        Assert.Equal(2, syncRunStore.Patches.Count);
        Assert.Equal(SyncRunStatus.Running, syncRunStore.Patches[0].Status);
        Assert.Equal(SyncRunStatus.Succeeded, syncRunStore.Patches[1].Status);
        Assert.Equal("snap-1", syncRunStore.Patches[1].SnapshotId);
        Assert.Equal(2, syncRunStore.Patches[1].SeedsProcessed);
        Assert.Equal(2, syncRunStore.Patches[1].ItemsInserted);
    }

    private sealed class TestSyncRunStore : ISyncRunStore
    {
        public List<SyncRun> AddedRuns { get; } = new();
        public List<SyncRunUpdate> Patches { get; } = new();

        public Task<bool> TryAddAsync(SyncRun syncRun, CancellationToken cancellationToken)
        {
            AddedRuns.Add(syncRun);
            return Task.FromResult(true);
        }

        public Task<bool> TryPatchAsync(string id, SyncRunUpdate update, CancellationToken cancellationToken)
        {
            Patches.Add(update);
            return Task.FromResult(true);
        }

        public Task<SyncRun?> GetAsync(string id, CancellationToken cancellationToken) => Task.FromResult<SyncRun?>(null);

        public Task<IReadOnlyList<SyncRun>> ListLatestAsync(int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SyncRun>>(Array.Empty<SyncRun>());
    }

}
