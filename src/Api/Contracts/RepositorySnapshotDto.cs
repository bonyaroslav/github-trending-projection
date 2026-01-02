namespace Api.Contracts;

public sealed record RepositorySnapshotDto(
    string Id,
    int Rank,
    string Name,
    string Owner,
    string FullName,
    string? Description,
    string? Language,
    int Stars,
    int Forks,
    string Url,
    string RepoUpdatedAt);
