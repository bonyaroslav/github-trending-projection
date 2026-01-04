using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.GitHub;

public interface IGitHubGraphQlClient
{
    Task<RepositoryEnrichment?> GetRepositoryAsync(string owner, string name, CancellationToken cancellationToken);
}
