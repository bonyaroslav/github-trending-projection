using Core.Domain.Snapshots;

namespace Infrastructure.Postgres;

internal static class SnapshotRecordMapper
{
    public static SnapshotRecord ToRecord(Snapshot snapshot)
    {
        var record = new SnapshotRecord
        {
            Id = snapshot.Id,
            Source = snapshot.Source,
            CapturedAtUtc = snapshot.CapturedAtUtc,
            Name = snapshot.Name,
            Notes = snapshot.Notes,
            Repositories = new List<SnapshotRepositoryRecord>()
        };

        foreach (var repository in snapshot.Repositories)
        {
            record.Repositories.Add(ToRecord(repository, snapshot.Id, record));
        }

        return record;
    }

    public static Snapshot ToDomain(SnapshotRecord record)
    {
        var repositories = record.Repositories
            .OrderBy(repository => repository.Rank)
            .Select(ToDomainRepository)
            .ToList();

        return new Snapshot(
            record.Id,
            record.CapturedAtUtc,
            record.CapturedAtUtc.ToUniversalTime().ToString("O"),
            record.Source,
            record.Name,
            record.Notes,
            repositories);
    }

    private static SnapshotRepositoryRecord ToRecord(RepositorySnapshot repository, string snapshotId, SnapshotRecord snapshot)
    {
        return new SnapshotRepositoryRecord
        {
            SnapshotId = snapshotId,
            Snapshot = snapshot,
            RepoId = repository.RepoId,
            Rank = repository.Rank,
            Owner = repository.Owner,
            Name = repository.Name,
            FullName = repository.FullName,
            Description = repository.Description,
            Language = repository.Language,
            Stars = repository.Stars,
            Forks = repository.Forks,
            Url = repository.Url,
            RepoUpdatedAt = repository.RepoUpdatedAt
        };
    }

    internal static RepositorySnapshot ToDomainRepository(SnapshotRepositoryRecord repository)
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
}
