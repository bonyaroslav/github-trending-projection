namespace Infrastructure.Postgres;

public sealed class SnapshotRepositoryRecord
{
    public string SnapshotId { get; set; } = string.Empty;
    public SnapshotRecord Snapshot { get; set; } = null!;
    public string RepoId { get; set; } = string.Empty;
    public int Rank { get; set; }
    public string Owner { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Language { get; set; }
    public int Stars { get; set; }
    public int Forks { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? RepoUpdatedAt { get; set; }
}
