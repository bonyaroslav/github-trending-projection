namespace Infrastructure.Postgres;

public sealed class SnapshotRecord
{
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset CapturedAtUtc { get; set; }
    public string? Name { get; set; }
    public string? Notes { get; set; }
    public List<SnapshotRepositoryRecord> Repositories { get; set; } = new();
}
