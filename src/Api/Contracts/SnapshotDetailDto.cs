namespace Api.Contracts;

public sealed record SnapshotDetailDto(
    string Id,
    string CapturedAt,
    string Source,
    string? Name,
    int ItemCount);
