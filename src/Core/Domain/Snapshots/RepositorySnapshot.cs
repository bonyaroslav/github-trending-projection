namespace Core.Domain.Snapshots;

public sealed class RepositorySnapshot
{
    public RepositorySnapshot(
        string repoId,
        int rank,
        string owner,
        string name,
        string fullName,
        string? description,
        string? language,
        int stars,
        int forks,
        string url,
        string? repoUpdatedAt)
    {
        RepoId = repoId;
        Rank = rank;
        Owner = owner;
        Name = name;
        FullName = fullName;
        Description = description;
        Language = language;
        Stars = stars;
        Forks = forks;
        Url = url;
        RepoUpdatedAt = repoUpdatedAt;
    }

    public string RepoId { get; }
    public int Rank { get; }
    public string Owner { get; }
    public string Name { get; }
    public string FullName { get; }
    public string? Description { get; }
    public string? Language { get; }
    public int Stars { get; }
    public int Forks { get; }
    public string Url { get; }
    public string? RepoUpdatedAt { get; }
}
