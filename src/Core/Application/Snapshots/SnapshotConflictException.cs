using System;

namespace Core.Application.Snapshots;

public sealed class SnapshotConflictException : Exception
{
    public SnapshotConflictException(string snapshotSource, string capturedAt)
        : base($"Snapshot conflict for source '{snapshotSource}' at '{capturedAt}'.")
    {
        SnapshotSource = snapshotSource;
        CapturedAt = capturedAt;
    }

    public string SnapshotSource { get; }
    public string CapturedAt { get; }
}
