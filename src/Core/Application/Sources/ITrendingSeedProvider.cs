using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Sources;

public interface ITrendingSeedProvider
{
    Task<IReadOnlyList<RepositorySeed>> GetSeedsAsync(CancellationToken cancellationToken);
}
