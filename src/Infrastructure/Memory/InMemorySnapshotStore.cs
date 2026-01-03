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
}
