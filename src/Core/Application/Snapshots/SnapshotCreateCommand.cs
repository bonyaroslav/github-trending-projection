using System.Collections.Generic;

namespace Core.Application.Snapshots;

public sealed record SnapshotCreateCommand(
    string? Source,
    string? Name,
    string? Notes,
    string? CapturedAt,
    IReadOnlyList<RepositorySnapshotInput> Repositories);

public sealed record RepositorySnapshotInput(
    string RepoId,
    int Rank,
    string Owner,
    string Name,
    string FullName,
    string? Description,
    string? Language,
    int Stars,
    int Forks,
    string Url,
    string? RepoUpdatedAt);
