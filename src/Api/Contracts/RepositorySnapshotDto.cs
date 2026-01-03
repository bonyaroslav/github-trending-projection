namespace Api.Contracts;

public sealed record RepositorySnapshotDto(
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