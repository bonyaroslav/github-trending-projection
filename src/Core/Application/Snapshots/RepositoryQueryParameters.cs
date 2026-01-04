using Core.Domain.Snapshots;

namespace Core.Application.Snapshots;

public sealed record RepositoryQueryParameters(
    int Page,
    int PageSize,
    string Sort,
    string Order,
    string? Query,
    string? Language);

public sealed record RepositoryPage(IReadOnlyList<RepositorySnapshot> Items, int TotalItems);
