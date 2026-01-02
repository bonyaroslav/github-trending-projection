namespace Api.Contracts;

public sealed record SnapshotUpdateRequest(
    string? Name,
    string? Notes);
