using Core.Application.Snapshots;

namespace Unit.Snapshots;

public sealed class SnapshotCreateCommandValidatorTests
{
    [Fact]
    public void Validate_ReturnsInvalid_WhenRepositoriesEmpty()
    {
        var validator = new SnapshotCreateCommandValidator();
        var command = BuildCommand(repositories: new List<RepositorySnapshotInput>());

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "repositories");
    }

    [Fact]
    public void Validate_ReturnsInvalid_WhenRankDuplicate()
    {
        var validator = new SnapshotCreateCommandValidator();
        var repositories = new List<RepositorySnapshotInput>
        {
            BuildRepository(rank: 1, repoId: "repo-1"),
            BuildRepository(rank: 1, repoId: "repo-2")
        };

        var result = validator.Validate(BuildCommand(repositories));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "repositories.rank");
    }

    [Fact]
    public void Validate_ReturnsInvalid_WhenRepoIdDuplicate()
    {
        var validator = new SnapshotCreateCommandValidator();
        var repositories = new List<RepositorySnapshotInput>
        {
            BuildRepository(rank: 1, repoId: "repo-1"),
            BuildRepository(rank: 2, repoId: "repo-1")
        };

        var result = validator.Validate(BuildCommand(repositories));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "repositories.repoId");
    }

    [Fact]
    public void Validate_ReturnsInvalid_WhenRankNotContiguous()
    {
        var validator = new SnapshotCreateCommandValidator();
        var repositories = new List<RepositorySnapshotInput>
        {
            BuildRepository(rank: 1, repoId: "repo-1"),
            BuildRepository(rank: 3, repoId: "repo-2")
        };

        var result = validator.Validate(BuildCommand(repositories));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "repositories.rank");
    }

    [Fact]
    public void Validate_ReturnsInvalid_WhenStarsOrForksNegative()
    {
        var validator = new SnapshotCreateCommandValidator();
        var repositories = new List<RepositorySnapshotInput>
        {
            BuildRepository(rank: 1, repoId: "repo-1", stars: -1),
            BuildRepository(rank: 2, repoId: "repo-2", forks: -5)
        };

        var result = validator.Validate(BuildCommand(repositories));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "repositories.stars");
        Assert.Contains(result.Errors, error => error.PropertyName == "repositories.forks");
    }

    [Fact]
    public void Validate_ReturnsValid_WhenRequestIsValid()
    {
        var validator = new SnapshotCreateCommandValidator();
        var repositories = new List<RepositorySnapshotInput>
        {
            BuildRepository(rank: 1, repoId: "repo-1"),
            BuildRepository(rank: 2, repoId: "repo-2")
        };

        var result = validator.Validate(BuildCommand(repositories));

        Assert.True(result.IsValid);
    }

    private static SnapshotCreateCommand BuildCommand(IReadOnlyList<RepositorySnapshotInput>? repositories = null)
    {
        return new SnapshotCreateCommand(
            Source: "manual",
            Name: "My snapshot",
            Notes: "Optional notes",
            CapturedAt: "2026-01-02T18:45:00Z",
            Repositories: repositories ?? new List<RepositorySnapshotInput>
            {
                BuildRepository(rank: 1, repoId: "repo-1")
            });
    }

    private static RepositorySnapshotInput BuildRepository(
        int rank,
        string repoId,
        string fullName = "octocat/hello-world",
        string language = "C#",
        int stars = 1234,
        int forks = 56)
    {
        var parts = fullName.Split('/');
        var owner = parts.Length > 0 ? parts[0] : "octocat";
        var name = parts.Length > 1 ? parts[1] : "hello-world";

        return new RepositorySnapshotInput(
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
