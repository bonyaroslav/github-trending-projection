namespace Core.Application.Snapshots;

public sealed record CaptureSnapshotOptions(
    string Source,
    string? Name,
    string? Notes);
