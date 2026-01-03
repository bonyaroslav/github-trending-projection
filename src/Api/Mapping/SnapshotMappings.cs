using System.Collections.Generic;
using System.Linq;
using Api.Contracts;
using Core.Application.Snapshots;
using Core.Domain.Snapshots;

namespace Api.Mapping;

internal static class SnapshotMappings
{
    public static SnapshotCreateCommand ToCommand(this SnapshotCreateRequest request)
    {
        return new SnapshotCreateCommand(
            request.Source,
            request.Name,
            request.Notes,
            request.CapturedAt,
            request.Repositories.Select(ToInput).ToList());
    }

    private static RepositorySnapshotInput ToInput(RepositorySnapshotDto repository)
    {
        return new RepositorySnapshotInput(
            repository.RepoId,
            repository.Rank,
            repository.Owner,
            repository.Name,
            repository.FullName,
            repository.Description,
            repository.Language,
            repository.Stars,
            repository.Forks,
            repository.Url,
            repository.RepoUpdatedAt);
    }

    public static IReadOnlyList<RepositorySnapshot> ToDomainRepositories(this IEnumerable<RepositorySnapshotDto> repositories)
    {
        return repositories.Select(ToDomainRepository).ToList();
    }

    public static RepositorySnapshotDto ToDto(this RepositorySnapshot repository)
    {
        return new RepositorySnapshotDto(
            repository.RepoId,
            repository.Rank,
            repository.Owner,
            repository.Name,
            repository.FullName,
            repository.Description,
            repository.Language,
            repository.Stars,
            repository.Forks,
            repository.Url,
            repository.RepoUpdatedAt);
    }

    private static RepositorySnapshot ToDomainRepository(RepositorySnapshotDto repository)
    {
        return new RepositorySnapshot(
            repository.RepoId,
            repository.Rank,
            repository.Owner,
            repository.Name,
            repository.FullName,
            repository.Description,
            repository.Language,
            repository.Stars,
            repository.Forks,
            repository.Url,
            repository.RepoUpdatedAt);
    }

    public static SnapshotSummaryDto ToSummaryDto(this Snapshot snapshot)
    {
        return new SnapshotSummaryDto(
            snapshot.Id,
            snapshot.CapturedAt,
            snapshot.Source,
            snapshot.Name,
            snapshot.Repositories.Count);
    }

    public static SnapshotDetailDto ToDetailDto(this Snapshot snapshot)
    {
        return new SnapshotDetailDto(
            snapshot.Id,
            snapshot.CapturedAt,
            snapshot.Source,
            snapshot.Name,
            snapshot.Notes,
            snapshot.Repositories.Count);
    }
}
