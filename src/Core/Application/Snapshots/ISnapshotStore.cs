using System.Collections.Generic;
using Core.Domain.Snapshots;

namespace Core.Application.Snapshots;

public interface ISnapshotStore
{
    bool TryAdd(Snapshot snapshot);
    IReadOnlyList<Snapshot> List();
    Snapshot? Get(string id);
    RepositoryPage? QueryRepositories(string snapshotId, RepositoryQueryParameters parameters);
    Snapshot? UpdateMetadata(string id, bool hasName, string? name, bool hasNotes, string? notes);
    bool Remove(string id);
}
