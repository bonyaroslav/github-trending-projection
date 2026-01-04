using System;
using Core.Domain.SyncRuns;

namespace Infrastructure.Postgres;

public sealed class SyncRunRecord
{
    public string Id { get; set; } = string.Empty;
    public SyncRunStatus Status { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string? SnapshotId { get; set; }
    public string? Error { get; set; }
    public int? SeedsProcessed { get; set; }
    public int? ItemsInserted { get; set; }
}
