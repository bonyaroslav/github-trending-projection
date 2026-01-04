using Core.Application.Snapshots;
using Core.Domain.Snapshots;
using Infrastructure.Memory;

namespace Unit.Snapshots;

public sealed class InMemorySnapshotStoreQueryTests
{
    [Fact]
    public void QueryRepositories_ReturnsNull_WhenSnapshotMissing()
    {
        var store = new InMemorySnapshotStore();

        var result = store.QueryRepositories("missing", new RepositoryQueryParameters(
            Page: 1,
            PageSize: 20,
            Sort: "rank",
            Order: "asc",
            Query: null,
            Language: null));

        Assert.Null(result);
    }

    [Fact]
    public void QueryRepositories_ReturnsPagedSortedResults()
    {
        var store = new InMemorySnapshotStore();
        store.TryAdd(BuildSnapshot());

        var result = store.QueryRepositories("snap-1", new RepositoryQueryParameters(
            Page: 1,
            PageSize: 1,
            Sort: "stars",
            Order: "desc",
            Query: null,
            Language: "C#"));

        Assert.NotNull(result);
        Assert.Equal(2, result!.TotalItems);
        Assert.Single(result.Items);
        Assert.Equal("dotnet/runtime", result.Items[0].FullName);
    }

    [Fact]
    public void QueryRepositories_FiltersByQuery()
    {
        var store = new InMemorySnapshotStore();
        store.TryAdd(BuildSnapshot());

        var result = store.QueryRepositories("snap-1", new RepositoryQueryParameters(
            Page: 1,
            PageSize: 20,
            Sort: "rank",
            Order: "asc",
            Query: "runtime",
            Language: null));

        Assert.NotNull(result);
        Assert.Single(result!.Items);
        Assert.Equal("dotnet/runtime", result.Items[0].FullName);
    }

    private static Snapshot BuildSnapshot()
    {
        return new Snapshot(
            "snap-1",
            DateTimeOffset.Parse("2026-01-02T18:45:00Z"),
            "2026-01-02T18:45:00Z",
            "manual",
            "My snapshot",
            "Optional notes",
            new List<RepositorySnapshot>
            {
                BuildRepository(rank: 1, repoId: "repo-1", fullName: "octocat/hello-world", language: "C#", stars: 1234, forks: 56),
                BuildRepository(rank: 2, repoId: "repo-2", fullName: "dotnet/runtime", language: "C#", stars: 25000, forks: 4100),
                BuildRepository(rank: 3, repoId: "repo-3", fullName: "kotlin/kotlinx.coroutines", language: "Kotlin", stars: 8000, forks: 900)
            });
    }

    private static RepositorySnapshot BuildRepository(
        int rank,
        string repoId,
        string fullName,
        string language,
        int stars,
        int forks)
    {
        var parts = fullName.Split('/');
        var owner = parts.Length > 0 ? parts[0] : "octocat";
        var name = parts.Length > 1 ? parts[1] : "hello-world";

        return new RepositorySnapshot(
            repoId,
            rank,
            owner,
            name,
            fullName,
            "Example repo",
            language,
            stars,
            forks,
            $"https://github.com/{fullName}",
            "2025-12-31T12:00:00Z");
    }
}
