using Core.Domain.SyncRuns;

namespace Infrastructure.Postgres;

internal static class SyncRunRecordMapper
{
    public static SyncRunRecord ToRecord(SyncRun run)
    {
        return new SyncRunRecord
        {
            Id = run.Id,
            Status = run.Status,
            RequestedAt = run.RequestedAt,
            StartedAt = run.StartedAt,
            FinishedAt = run.FinishedAt,
            SnapshotId = run.SnapshotId,
            Error = run.Error,
            SeedsProcessed = run.SeedsProcessed,
            ItemsInserted = run.ItemsInserted
        };
    }

    public static SyncRun ToDomain(SyncRunRecord record)
    {
        return new SyncRun(
            record.Id,
            record.Status,
            record.RequestedAt,
            record.StartedAt,
            record.FinishedAt,
            record.SnapshotId,
            record.Error,
            record.SeedsProcessed,
            record.ItemsInserted);
    }
}
