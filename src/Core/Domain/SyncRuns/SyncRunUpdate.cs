using System;

namespace Core.Domain.SyncRuns;

public sealed record SyncRunUpdate(
    SyncRunStatus? Status = null,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? FinishedAt = null,
    string? SnapshotId = null,
    string? Error = null,
    int? SeedsProcessed = null,
    int? ItemsInserted = null);
