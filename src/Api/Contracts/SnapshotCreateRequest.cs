using System.Collections.Generic;

namespace Api.Contracts;

public sealed record SnapshotCreateRequest(
    string? Source,
    string? Name,
    string? Notes,
    string? CapturedAt,
    IReadOnlyList<RepositorySnapshotDto> Repositories);