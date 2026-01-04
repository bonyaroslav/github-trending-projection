using System.Collections.Generic;
using Core.Domain.Snapshots;

namespace Core.Application.Snapshots;

public interface ISnapshotStore
{
    Task<bool> TryAddAsync(Snapshot snapshot, CancellationToken cancellationToken);
    Task<IReadOnlyList<Snapshot>> ListAsync(CancellationToken cancellationToken);
    Task<Snapshot?> GetAsync(string id, CancellationToken cancellationToken);
    Task<RepositoryPage?> QueryRepositoriesAsync(string snapshotId, RepositoryQueryParameters parameters, CancellationToken cancellationToken);
    Task<Snapshot?> UpdateMetadataAsync(string id, bool hasName, string? name, bool hasNotes, string? notes, CancellationToken cancellationToken);
    Task<bool> RemoveAsync(string id, CancellationToken cancellationToken);
}
