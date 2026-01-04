using Core.Application.Snapshots;
using Core.Domain.Snapshots;

namespace Infrastructure.Memory;

public sealed class InMemorySnapshotStore : ISnapshotStore
{
    private readonly object _lock = new();
    private readonly List<Snapshot> _snapshots = new();

    public Task<bool> TryAddAsync(Snapshot snapshot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (_snapshots.Any(existing => existing.Source == snapshot.Source && existing.CapturedAt == snapshot.CapturedAt))
            {
                return Task.FromResult(false);
            }

            _snapshots.Add(snapshot);
            return Task.FromResult(true);
        }
    }

    public Task<IReadOnlyList<Snapshot>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<Snapshot>>(_snapshots.ToList());
        }
    }

    public Task<Snapshot?> GetAsync(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(_snapshots.FirstOrDefault(snapshot => snapshot.Id == id));
        }
    }

    public Task<RepositoryPage?> QueryRepositoriesAsync(
        string snapshotId,
        RepositoryQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Snapshot? snapshot;
        lock (_lock)
        {
            snapshot = _snapshots.FirstOrDefault(item => item.Id == snapshotId);
        }

        if (snapshot is null)
        {
            return Task.FromResult<RepositoryPage?>(null);
        }

        var filtered = snapshot.Repositories.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(parameters.Query))
        {
            var query = parameters.Query.Trim();
            filtered = filtered.Where(repository =>
                repository.FullName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (repository.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Language))
        {
            var language = parameters.Language.Trim();
            filtered = filtered.Where(repository =>
                repository.Language is not null &&
                string.Equals(repository.Language, language, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = ApplySort(filtered, parameters.Sort, parameters.Order);
        var totalItems = ordered.Count();

        var items = ordered
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToList();

        return Task.FromResult<RepositoryPage?>(new RepositoryPage(items, totalItems));
    }

    public Task<Snapshot?> UpdateMetadataAsync(
        string id,
        bool hasName,
        string? name,
        bool hasNotes,
        string? notes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var snapshot = _snapshots.FirstOrDefault(item => item.Id == id);
            if (snapshot is null)
            {
                return Task.FromResult<Snapshot?>(null);
            }

            if (hasName)
            {
                snapshot.Name = name;
            }

            if (hasNotes)
            {
                snapshot.Notes = notes;
            }

            return Task.FromResult<Snapshot?>(snapshot);
        }
    }

    public Task<bool> RemoveAsync(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var index = _snapshots.FindIndex(snapshot => snapshot.Id == id);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            _snapshots.RemoveAt(index);
            return Task.FromResult(true);
        }
    }

    private static IOrderedEnumerable<RepositorySnapshot> ApplySort(
        IEnumerable<RepositorySnapshot> repositories,
        string sort,
        string order)
    {
        return (sort, order) switch
        {
            ("stars", "asc") => repositories.OrderBy(repository => repository.Stars).ThenBy(repository => repository.Rank),
            ("stars", "desc") => repositories.OrderByDescending(repository => repository.Stars).ThenBy(repository => repository.Rank),
            ("forks", "asc") => repositories.OrderBy(repository => repository.Forks).ThenBy(repository => repository.Rank),
            ("forks", "desc") => repositories.OrderByDescending(repository => repository.Forks).ThenBy(repository => repository.Rank),
            ("rank", "desc") => repositories.OrderByDescending(repository => repository.Rank),
            _ => repositories.OrderBy(repository => repository.Rank)
        };
    }
}
