namespace Api.Contracts;

public sealed record RepositorySummaryDto(
    string Id,
    string Name,
    string Owner,
    string FullName,
    string? Description,
    string? Language,
    int Stars,
    int Forks,
    string Url,
    string UpdatedAt);
