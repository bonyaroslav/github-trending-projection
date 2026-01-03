using System.Globalization;
using System.Text.Json;
using Api.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<SnapshotStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new HealthResponse("ok")));
app.MapGet("/version", () => Results.Ok(new VersionResponse("dev")));

var api = app.MapGroup("/api/v1");

api.MapGet("/snapshots", (int? page, int? pageSize, SnapshotStore store) =>
{
    var resolvedPage = page ?? 1;
    var resolvedPageSize = pageSize ?? 20;

    if (resolvedPage < 1 || resolvedPageSize < 1 || resolvedPageSize > 100)
    {
        return Results.BadRequest();
    }

    var snapshots = store.List()
        .OrderByDescending(snapshot => snapshot.CapturedAtUtc)
        .ToList();

    var totalItems = snapshots.Count;
    var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)resolvedPageSize);

    var items = totalItems == 0
        ? new List<SnapshotSummaryDto>()
        : snapshots
            .Skip((resolvedPage - 1) * resolvedPageSize)
            .Take(resolvedPageSize)
            .Select(ToSummary)
            .ToList();

    var response = new PagedResponse<SnapshotSummaryDto>(items, resolvedPage, resolvedPageSize, totalItems, totalPages);

    return Results.Ok(response);
});

api.MapPost("/snapshots", (SnapshotCreateRequest request, SnapshotStore store) =>
{
    if (request.Repositories is null || request.Repositories.Count == 0)
    {
        return Results.BadRequest();
    }

    var source = string.IsNullOrWhiteSpace(request.Source) ? "manual" : request.Source;

    if (!TryResolveCapturedAt(request.CapturedAt, out var capturedAtUtc, out var capturedAt))
    {
        return Results.BadRequest();
    }

    var snapshot = new SnapshotRecord(
        Guid.NewGuid().ToString("N"),
        capturedAtUtc,
        capturedAt,
        source!,
        request.Name,
        request.Notes,
        request.Repositories);

    if (!store.TryAdd(snapshot))
    {
        return Results.Conflict();
    }

    var detail = ToDetail(snapshot);

    return Results.Created($"/api/v1/snapshots/{snapshot.Id}", detail);
});

api.MapGet("/snapshots/{snapshotId}", (string snapshotId, SnapshotStore store) =>
{
    var snapshot = store.Get(snapshotId);

    return snapshot is null ? Results.NotFound() : Results.Ok(ToDetail(snapshot));
});

api.MapPatch("/snapshots/{snapshotId}", async (string snapshotId, HttpRequest request, SnapshotStore store) =>
{
    using var document = await JsonDocument.ParseAsync(request.Body);

    if (document.RootElement.ValueKind != JsonValueKind.Object)
    {
        return Results.BadRequest();
    }

    string? name = null;
    string? notes = null;
    var hasName = false;
    var hasNotes = false;

    foreach (var property in document.RootElement.EnumerateObject())
    {
        switch (property.Name)
        {
            case "name":
                hasName = true;
                name = property.Value.ValueKind == JsonValueKind.Null ? null : property.Value.GetString();
                break;
            case "notes":
                hasNotes = true;
                notes = property.Value.ValueKind == JsonValueKind.Null ? null : property.Value.GetString();
                break;
            default:
                return Results.BadRequest();
        }
    }

    if (!hasName && !hasNotes)
    {
        return Results.BadRequest();
    }

    var updated = store.UpdateMetadata(snapshotId, hasName, name, hasNotes, notes);

    return updated is null ? Results.NotFound() : Results.Ok(ToDetail(updated));
});

api.MapDelete("/snapshots/{snapshotId}", (string snapshotId, SnapshotStore store) =>
{
    return store.Remove(snapshotId) ? Results.NoContent() : Results.NotFound();
});

app.Run();

static SnapshotSummaryDto ToSummary(SnapshotRecord snapshot)
    => new(snapshot.Id, snapshot.CapturedAt, snapshot.Source, snapshot.Name, snapshot.Repositories.Count);

static SnapshotDetailDto ToDetail(SnapshotRecord snapshot)
    => new(snapshot.Id, snapshot.CapturedAt, snapshot.Source, snapshot.Name, snapshot.Notes, snapshot.Repositories.Count);

static bool TryResolveCapturedAt(string? capturedAt, out DateTimeOffset capturedAtUtc, out string resolved)
{
    if (string.IsNullOrWhiteSpace(capturedAt))
    {
        capturedAtUtc = DateTimeOffset.UtcNow;
    }
    else if (!DateTimeOffset.TryParse(
                 capturedAt,
                 CultureInfo.InvariantCulture,
                 DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                 out capturedAtUtc))
    {
        resolved = string.Empty;
        return false;
    }

    capturedAtUtc = capturedAtUtc.ToUniversalTime();
    resolved = capturedAtUtc.ToString("yyyy-MM-ddTHH:mm:ss'Z'");
    return true;
}

public partial class Program;

internal sealed class SnapshotStore
{
    private readonly object _lock = new();
    private readonly List<SnapshotRecord> _snapshots = new();

    public bool TryAdd(SnapshotRecord snapshot)
    {
        lock (_lock)
        {
            if (_snapshots.Any(existing => existing.Source == snapshot.Source && existing.CapturedAt == snapshot.CapturedAt))
            {
                return false;
            }

            _snapshots.Add(snapshot);
            return true;
        }
    }

    public IReadOnlyList<SnapshotRecord> List()
    {
        lock (_lock)
        {
            return _snapshots.ToList();
        }
    }

    public SnapshotRecord? Get(string id)
    {
        lock (_lock)
        {
            return _snapshots.FirstOrDefault(snapshot => snapshot.Id == id);
        }
    }

    public SnapshotRecord? UpdateMetadata(string id, bool hasName, string? name, bool hasNotes, string? notes)
    {
        lock (_lock)
        {
            var snapshot = _snapshots.FirstOrDefault(item => item.Id == id);
            if (snapshot is null)
            {
                return null;
            }

            if (hasName)
            {
                snapshot.Name = name;
            }

            if (hasNotes)
            {
                snapshot.Notes = notes;
            }

            return snapshot;
        }
    }

    public bool Remove(string id)
    {
        lock (_lock)
        {
            var index = _snapshots.FindIndex(snapshot => snapshot.Id == id);
            if (index < 0)
            {
                return false;
            }

            _snapshots.RemoveAt(index);
            return true;
        }
    }
}

internal sealed class SnapshotRecord
{
    public SnapshotRecord(
        string id,
        DateTimeOffset capturedAtUtc,
        string capturedAt,
        string source,
        string? name,
        string? notes,
        IReadOnlyList<RepositorySnapshotDto> repositories)
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
    public IReadOnlyList<RepositorySnapshotDto> Repositories { get; }
}