using Api.Support;
using Core.Domain.Snapshots;

namespace Unit.Api.Support;

public sealed class SnapshotQueryTests
{
    [Fact]
    public void TryResolvePagination_ReturnsDefaults_WhenNull()
    {
        var success = SnapshotQuery.TryResolvePagination(null, null, out var parameters, out var errors);

        Assert.True(success);
        Assert.Null(errors);
        Assert.Equal(1, parameters.Page);
        Assert.Equal(20, parameters.PageSize);
    }

    [Fact]
    public void TryResolvePagination_ReturnsErrors_WhenOutOfRange()
    {
        var success = SnapshotQuery.TryResolvePagination(0, 101, out var _, out var errors);

        Assert.False(success);
        Assert.NotNull(errors);
        Assert.True(errors!.ContainsKey("page"));
        Assert.True(errors.ContainsKey("pageSize"));
    }

    [Fact]
    public void TryResolveSortAndOrder_DefaultsToRankAsc()
    {
        var success = SnapshotQuery.TryResolveSortAndOrder(null, null, out var parameters, out var errors);

        Assert.True(success);
        Assert.Null(errors);
        Assert.Equal("rank", parameters.Sort);
        Assert.Equal("asc", parameters.Order);
    }

    [Fact]
    public void TryResolveSortAndOrder_DefaultsToStarsDesc()
    {
        var success = SnapshotQuery.TryResolveSortAndOrder("stars", null, out var parameters, out var errors);

        Assert.True(success);
        Assert.Null(errors);
        Assert.Equal("stars", parameters.Sort);
        Assert.Equal("desc", parameters.Order);
    }

    [Fact]
    public void TryResolveSortAndOrder_ReturnsError_WhenSortInvalid()
    {
        var success = SnapshotQuery.TryResolveSortAndOrder("unknown", "asc", out var _, out var errors);

        Assert.False(success);
        Assert.NotNull(errors);
        Assert.True(errors!.ContainsKey("sort"));
    }

    [Fact]
    public void TryResolveSortAndOrder_ReturnsError_WhenOrderInvalid()
    {
        var success = SnapshotQuery.TryResolveSortAndOrder("stars", "up", out var _, out var errors);

        Assert.False(success);
        Assert.NotNull(errors);
        Assert.True(errors!.ContainsKey("order"));
    }

    [Fact]
    public void TryResolveCapturedAt_ReturnsNormalizedUtc()
    {
        var success = SnapshotQuery.TryResolveCapturedAt(
            "2026-01-02T18:45:00Z",
            out var capturedAtUtc,
            out var normalized);

        Assert.True(success);
        Assert.Equal("2026-01-02T18:45:00Z", normalized);
        Assert.Equal(DateTimeOffset.Parse("2026-01-02T18:45:00Z"), capturedAtUtc);
    }

    [Fact]
    public void TryResolveCapturedAt_ReturnsFalse_WhenInvalid()
    {
        var success = SnapshotQuery.TryResolveCapturedAt("not-a-date", out var _, out var normalized);

        Assert.False(success);
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void ApplyRepositoryFilters_FiltersByQuery()
    {
        var repositories = BuildRepositoryList();

        var result = SnapshotQuery.ApplyRepositoryFilters(repositories, "runtime", null).ToList();

        Assert.Single(result);
        Assert.Equal("dotnet/runtime", result[0].FullName);
    }

    [Fact]
    public void ApplyRepositoryFilters_FiltersByLanguage()
    {
        var repositories = BuildRepositoryList();

        var result = SnapshotQuery.ApplyRepositoryFilters(repositories, null, "Kotlin").ToList();

        Assert.Single(result);
        Assert.Equal("kotlin/kotlinx.coroutines", result[0].FullName);
    }

    [Fact]
    public void ApplyRepositorySort_SortsByStarsDescending()
    {
        var repositories = BuildRepositoryList();

        var result = SnapshotQuery.ApplyRepositorySort(repositories, "stars", "desc").ToList();

        Assert.Equal("dotnet/runtime", result[0].FullName);
    }

    [Fact]
    public void ApplyRepositorySort_SortsByForksAscending()
    {
        var repositories = BuildRepositoryList();

        var result = SnapshotQuery.ApplyRepositorySort(repositories, "forks", "asc").ToList();

        Assert.Equal("octocat/hello-world", result[0].FullName);
    }

    private static List<RepositorySnapshot> BuildRepositoryList()
    {
        return new List<RepositorySnapshot>
        {
            BuildRepository(rank: 1, repoId: "repo-1", fullName: "octocat/hello-world", language: "C#", stars: 1234, forks: 56),
            BuildRepository(rank: 2, repoId: "repo-2", fullName: "dotnet/runtime", language: "C#", stars: 25000, forks: 4100),
            BuildRepository(rank: 3, repoId: "repo-3", fullName: "kotlin/kotlinx.coroutines", language: "Kotlin", stars: 8000, forks: 900)
        };
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
