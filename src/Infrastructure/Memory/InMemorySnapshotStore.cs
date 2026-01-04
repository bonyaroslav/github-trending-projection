using Core.Application.Snapshots;
using Core.Domain.Snapshots;

namespace Infrastructure.Memory;

public sealed class InMemorySnapshotStore : ISnapshotStore
{
    private readonly object _lock = new();
    private readonly List<Snapshot> _snapshots = new();

    public bool TryAdd(Snapshot snapshot)
    {
        lock (_lock)
        {
            if (_snapshots.Any(existing => existing.Source == snapshot.Source && existing.CapturedAt == snapshot.CapturedAt))
            {
                return false;
            }

            _snapshots.Add(snapshot);
            return true;
        }
    }

    public IReadOnlyList<Snapshot> List()
    {
        lock (_lock)
        {
            return _snapshots.ToList();
        }
    }

    public Snapshot? Get(string id)
    {
        lock (_lock)
        {
            return _snapshots.FirstOrDefault(snapshot => snapshot.Id == id);
        }
    }

    public RepositoryPage? QueryRepositories(string snapshotId, RepositoryQueryParameters parameters)
    {
        Snapshot? snapshot;
        lock (_lock)
        {
            snapshot = _snapshots.FirstOrDefault(item => item.Id == snapshotId);
        }

        if (snapshot is null)
        {
            return null;
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

        return new RepositoryPage(items, totalItems);
    }

    public Snapshot? UpdateMetadata(string id, bool hasName, string? name, bool hasNotes, string? notes)
    {
        lock (_lock)
        {
            var snapshot = _snapshots.FirstOrDefault(item => item.Id == id);
            if (snapshot is null)
            {
                return null;
            }

            if (hasName)
            {
                snapshot.Name = name;
            }

            if (hasNotes)
            {
                snapshot.Notes = notes;
            }

            return snapshot;
        }
    }

    public bool Remove(string id)
    {
        lock (_lock)
        {
            var index = _snapshots.FindIndex(snapshot => snapshot.Id == id);
            if (index < 0)
            {
                return false;
            }

            _snapshots.RemoveAt(index);
            return true;
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
