using System;

namespace Core.Application.GitHub;

public sealed record RepositoryEnrichment(
    string RepoId,
    string Owner,
    string Name,
    string FullName,
    string? Description,
    string? Language,
    int Stars,
    int Forks,
    string Url,
    DateTimeOffset? UpdatedAt);
