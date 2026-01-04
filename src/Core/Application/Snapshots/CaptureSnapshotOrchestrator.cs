using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Common;
using Core.Application.GitHub;
using Core.Application.Sources;
using Core.Application.SyncRuns;
using Core.Domain.Snapshots;
using Core.Domain.SyncRuns;

namespace Core.Application.Snapshots;

public sealed class CaptureSnapshotOrchestrator
{
    private readonly ITrendingSeedProvider _trendingSeedProvider;
    private readonly IGitHubGraphQlClient _graphQlClient;
    private readonly ISnapshotStore _snapshotStore;
    private readonly ISyncRunStore _syncRunStore;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;

    public CaptureSnapshotOrchestrator(
        ITrendingSeedProvider trendingSeedProvider,
        IGitHubGraphQlClient graphQlClient,
        ISnapshotStore snapshotStore,
        ISyncRunStore syncRunStore,
        IClock clock,
        IIdGenerator idGenerator)
    {
        _trendingSeedProvider = trendingSeedProvider;
        _graphQlClient = graphQlClient;
        _snapshotStore = snapshotStore;
        _syncRunStore = syncRunStore;
        _clock = clock;
        _idGenerator = idGenerator;
    }

    public async Task<CaptureResult> CaptureAsync(CaptureSnapshotOptions options, CancellationToken cancellationToken)
    {
        var runId = _idGenerator.NewId();
        var requestedAt = _clock.UtcNow;
        var syncRun = new SyncRun(runId, SyncRunStatus.Queued, requestedAt, null, null, null, null, 0, 0);
        await _syncRunStore.TryAddAsync(syncRun, cancellationToken);

        var startedAt = _clock.UtcNow;
        var runningUpdate = new SyncRunUpdate(Status: SyncRunStatus.Running, StartedAt: startedAt);
        await _syncRunStore.TryPatchAsync(runId, runningUpdate, cancellationToken);

        try
        {
            var seeds = await _trendingSeedProvider.GetSeedsAsync(cancellationToken);
            var repositories = await EnrichRepositoriesAsync(seeds, cancellationToken);

            var capturedAtUtc = _clock.UtcNow;
            var snapshotId = _idGenerator.NewId();
            var snapshot = new Snapshot(
                snapshotId,
                capturedAtUtc,
                capturedAtUtc.ToUniversalTime().ToString("O"),
                options.Source,
                options.Name,
                options.Notes,
                repositories);

            var added = await _snapshotStore.TryAddAsync(snapshot, cancellationToken);
            if (!added)
            {
                throw new SnapshotConflictException(options.Source, snapshot.CapturedAt);
            }

            var finishedAt = _clock.UtcNow;
            var succeededUpdate = new SyncRunUpdate(
                Status: SyncRunStatus.Succeeded,
                FinishedAt: finishedAt,
                SnapshotId: snapshotId,
                SeedsProcessed: seeds.Count,
                ItemsInserted: repositories.Count);
            await _syncRunStore.TryPatchAsync(runId, succeededUpdate, cancellationToken);

            return new CaptureResult(runId, snapshotId);
        }
        catch (Exception exception)
        {
            var failureCode = exception switch
            {
                SnapshotConflictException => SyncRunFailureCode.SnapshotConflict,
                _ => SyncRunFailureCode.Unknown
            };

            var failedUpdate = new SyncRunUpdate(
                Status: SyncRunStatus.Failed,
                FinishedAt: _clock.UtcNow,
                Error: exception.Message,
                FailureCode: failureCode);
            await _syncRunStore.TryPatchAsync(runId, failedUpdate, cancellationToken);
            throw;
        }
    }

    private async Task<List<RepositorySnapshot>> EnrichRepositoriesAsync(
        IReadOnlyList<RepositorySeed> seeds,
        CancellationToken cancellationToken)
    {
        var repositories = new List<RepositorySnapshot>();

        foreach (var seed in seeds.OrderBy(seed => seed.Rank))
        {
            var enrichment = await _graphQlClient.GetRepositoryAsync(seed.Owner, seed.Name, cancellationToken);
            if (enrichment is null)
            {
                continue;
            }

            repositories.Add(new RepositorySnapshot(
                enrichment.RepoId,
                seed.Rank,
                enrichment.Owner,
                enrichment.Name,
                enrichment.FullName,
                enrichment.Description,
                enrichment.Language,
                enrichment.Stars,
                enrichment.Forks,
                enrichment.Url,
                enrichment.UpdatedAt?.ToString("O")));
        }

        return repositories;
    }
}
