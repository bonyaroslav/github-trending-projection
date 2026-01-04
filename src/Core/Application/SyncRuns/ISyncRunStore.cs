using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Domain.SyncRuns;

namespace Core.Application.SyncRuns;

public interface ISyncRunStore
{
    Task<bool> TryAddAsync(SyncRun syncRun, CancellationToken cancellationToken);
    Task<bool> TryPatchAsync(string id, SyncRunUpdate update, CancellationToken cancellationToken);
    Task<SyncRun?> GetAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<SyncRun>> ListLatestAsync(int limit, CancellationToken cancellationToken);
}
