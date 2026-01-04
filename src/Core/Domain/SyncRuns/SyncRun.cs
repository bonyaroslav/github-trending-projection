using System;

namespace Core.Domain.SyncRuns;

public sealed record SyncRun(
    string Id,
    SyncRunStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string? SnapshotId,
    string? Error,
    int? SeedsProcessed,
    int? ItemsInserted);
