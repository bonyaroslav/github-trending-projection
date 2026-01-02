namespace Api.Contracts;

public sealed record SnapshotSummaryDto(
    string Id,
    string CapturedAt,
    string Source,
    string? Name,
    int ItemCount);
