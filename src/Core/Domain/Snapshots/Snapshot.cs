using System;
using System.Collections.Generic;

namespace Core.Domain.Snapshots;

public sealed class Snapshot
{
    public Snapshot(
        string id,
        DateTimeOffset capturedAtUtc,
        string capturedAt,
        string source,
        string? name,
        string? notes,
        IReadOnlyList<RepositorySnapshot> repositories)
    {
        Id = id;
        CapturedAtUtc = capturedAtUtc;
        CapturedAt = capturedAt;
        Source = source;
        Name = name;
        Notes = notes;
        Repositories = repositories;
    }

    public string Id { get; }
    public DateTimeOffset CapturedAtUtc { get; }
    public string CapturedAt { get; }
    public string Source { get; }
    public string? Name { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<RepositorySnapshot> Repositories { get; }
}
