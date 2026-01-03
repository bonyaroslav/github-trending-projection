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
    if (!TryResolvePagination(page, pageSize, out var resolvedPage, out var resolvedPageSize, out var error))
    {
        return error;
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
    if (!TryValidateCreateRequest(request, out var validationErrors))
    {
        return Results.ValidationProblem(validationErrors);
    }

    var source = string.IsNullOrWhiteSpace(request.Source) ? "manual" : request.Source;

    if (!TryResolveCapturedAt(request.CapturedAt, out var capturedAtUtc, out var capturedAt))
    {
        return ValidationProblem("capturedAt", "capturedAt must be a valid UTC ISO 8601 timestamp.");
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
        return ConflictProblem("A snapshot with the same source and capturedAt already exists.");
    }

    var detail = ToDetail(snapshot);

    return Results.Created($"/api/v1/snapshots/{snapshot.Id}", detail);
});

api.MapGet("/snapshots/{snapshotId}", (string snapshotId, SnapshotStore store) =>
{
    var snapshot = store.Get(snapshotId);

    return snapshot is null
        ? NotFoundProblem("Snapshot not found.")
        : Results.Ok(ToDetail(snapshot));
});

api.MapPatch("/snapshots/{snapshotId}", async (string snapshotId, HttpRequest request, SnapshotStore store) =>
{
    JsonDocument document;
    try
    {
        document = await JsonDocument.ParseAsync(request.Body);
    }
    catch (JsonException)
    {
        return ValidationProblem("body", "Request body must be valid JSON.");
    }

    using (document)
    {
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return ValidationProblem("body", "Request body must be a JSON object.");
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
                    return ValidationProblem(property.Name, "Unknown field.");
            }
        }

        if (!hasName && !hasNotes)
        {
            return ValidationProblem("body", "At least one field must be provided.");
        }

        var updated = store.UpdateMetadata(snapshotId, hasName, name, hasNotes, notes);

        return updated is null
            ? NotFoundProblem("Snapshot not found.")
            : Results.Ok(ToDetail(updated));
    }
});

api.MapDelete("/snapshots/{snapshotId}", (string snapshotId, SnapshotStore store) =>
{
    return store.Remove(snapshotId)
        ? Results.NoContent()
        : NotFoundProblem("Snapshot not found.");
});

api.MapGet("/snapshots/{snapshotId}/repositories", (
    string snapshotId,
    int? page,
    int? pageSize,
    string? q,
    string? language,
    string? sort,
    string? order,
    SnapshotStore store) =>
{
    if (!TryResolvePagination(page, pageSize, out var resolvedPage, out var resolvedPageSize, out var paginationError))
    {
        return paginationError;
    }

    if (!TryResolveSortAndOrder(sort, order, out var resolvedSort, out var resolvedOrder, out var sortError))
    {
        return sortError;
    }

    var snapshot = store.Get(snapshotId);
    if (snapshot is null)
    {
        return NotFoundProblem("Snapshot not found.");
    }

    var repositories = ApplyRepositoryFilters(snapshot.Repositories, q, language);
    var ordered = ApplyRepositorySort(repositories, resolvedSort, resolvedOrder);
    var orderedList = ordered.ToList();

    var totalItems = orderedList.Count;
    var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)resolvedPageSize);

    var items = totalItems == 0
        ? new List<RepositorySnapshotDto>()
        : orderedList
            .Skip((resolvedPage - 1) * resolvedPageSize)
            .Take(resolvedPageSize)
            .ToList();

    var response = new PagedResponse<RepositorySnapshotDto>(items, resolvedPage, resolvedPageSize, totalItems, totalPages);

    return Results.Ok(response);
});

api.MapGet("/snapshots/{snapshotId}/repositories/{repoId}", (string snapshotId, string repoId, SnapshotStore store) =>
{
    var snapshot = store.Get(snapshotId);
    if (snapshot is null)
    {
        return NotFoundProblem("Snapshot not found.");
    }

    var repository = snapshot.Repositories.FirstOrDefault(item => item.RepoId == repoId);

    return repository is null
        ? NotFoundProblem("Repository not found in snapshot.")
        : Results.Ok(repository);
});

api.MapGet("/snapshots/{snapshotId}/repositories/by-full-name", (string snapshotId, string? fullName, SnapshotStore store) =>
{
    if (string.IsNullOrWhiteSpace(fullName))
    {
        return ValidationProblem("fullName", "fullName is required.");
    }

    var snapshot = store.Get(snapshotId);
    if (snapshot is null)
    {
        return NotFoundProblem("Snapshot not found.");
    }

    var repository = snapshot.Repositories.FirstOrDefault(item =>
        string.Equals(item.FullName, fullName, StringComparison.OrdinalIgnoreCase));

    return repository is null
        ? NotFoundProblem("Repository not found in snapshot.")
        : Results.Ok(repository);
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

static bool TryResolvePagination(
    int? page,
    int? pageSize,
    out int resolvedPage,
    out int resolvedPageSize,
    out IResult? error)
{
    resolvedPage = page ?? 1;
    resolvedPageSize = pageSize ?? 20;

    var errors = new Dictionary<string, string[]>();

    if (resolvedPage < 1)
    {
        errors["page"] = new[] { "page must be >= 1." };
    }

    if (resolvedPageSize < 1 || resolvedPageSize > 100)
    {
        errors["pageSize"] = new[] { "pageSize must be between 1 and 100." };
    }

    if (errors.Count > 0)
    {
        error = Results.ValidationProblem(errors);
        return false;
    }

    error = null;
    return true;
}

static bool TryResolveSortAndOrder(
    string? sort,
    string? order,
    out string resolvedSort,
    out string resolvedOrder,
    out IResult? error)
{
    resolvedSort = string.IsNullOrWhiteSpace(sort) ? "rank" : sort.Trim().ToLowerInvariant();
    if (resolvedSort is not ("rank" or "stars" or "forks"))
    {
        error = ValidationProblem("sort", "sort must be one of: rank, stars, forks.");
        resolvedOrder = "asc";
        return false;
    }

    if (string.IsNullOrWhiteSpace(order))
    {
        resolvedOrder = resolvedSort == "rank" ? "asc" : "desc";
        error = null;
        return true;
    }

    resolvedOrder = order.Trim().ToLowerInvariant();
    if (resolvedOrder is not ("asc" or "desc"))
    {
        error = ValidationProblem("order", "order must be one of: asc, desc.");
        return false;
    }

    error = null;
    return true;
}

static IEnumerable<RepositorySnapshotDto> ApplyRepositoryFilters(
    IReadOnlyList<RepositorySnapshotDto> repositories,
    string? q,
    string? language)
{
    var filtered = repositories.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(q))
    {
        var query = q.Trim();
        filtered = filtered.Where(repository =>
            repository.FullName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            (repository.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));
    }

    if (!string.IsNullOrWhiteSpace(language))
    {
        var normalizedLanguage = language.Trim();
        filtered = filtered.Where(repository =>
            repository.Language is not null &&
            string.Equals(repository.Language, normalizedLanguage, StringComparison.OrdinalIgnoreCase));
    }

    return filtered;
}

static IOrderedEnumerable<RepositorySnapshotDto> ApplyRepositorySort(
    IEnumerable<RepositorySnapshotDto> repositories,
    string sort,
    string order)
{
    return (sort, order) switch
    {
        ("stars", "asc") => repositories.OrderBy(repository => repository.Stars).ThenBy(repository => repository.Rank),
        ("stars", "desc") => repositories.OrderByDescending(repository => repository.Stars).ThenBy(repository => repository.Rank),
        ("forks", "asc") => repositories.OrderBy(repository => repository.Forks).ThenBy(repository => repository.Rank),
        ("forks", "desc") => repositories.OrderByDescending(repository => repository.Forks).ThenBy(repository => repository.Rank),
        ("rank", "desc") => repositories.OrderByDescending(repository => repository.Rank),
        _ => repositories.OrderBy(repository => repository.Rank)
    };
}

static bool TryValidateCreateRequest(
    SnapshotCreateRequest request,
    out Dictionary<string, string[]> errors)
{
    var errorList = new Dictionary<string, List<string>>();

    if (request.Repositories is null || request.Repositories.Count == 0)
    {
        AddError(errorList, "repositories", "At least one repository is required.");
        errors = FlattenErrors(errorList);
        return false;
    }

    var rankSet = new HashSet<int>();
    var repoIdSet = new HashSet<string>(StringComparer.Ordinal);
    var maxRank = 0;

    foreach (var repository in request.Repositories)
    {
        if (repository.Rank < 1)
        {
            AddError(errorList, "repositories.rank", "Rank must be >= 1.");
        }

        if (!rankSet.Add(repository.Rank))
        {
            AddError(errorList, "repositories.rank", "Rank must be unique within the snapshot.");
        }

        if (!repoIdSet.Add(repository.RepoId))
        {
            AddError(errorList, "repositories.repoId", "repoId must be unique within the snapshot.");
        }

        if (repository.Stars < 0)
        {
            AddError(errorList, "repositories.stars", "Stars must be >= 0.");
        }

        if (repository.Forks < 0)
        {
            AddError(errorList, "repositories.forks", "Forks must be >= 0.");
        }

        maxRank = Math.Max(maxRank, repository.Rank);
    }

    if (rankSet.Count == request.Repositories.Count && maxRank != request.Repositories.Count)
    {
        AddError(errorList, "repositories.rank", "Rank values must start at 1 and be contiguous.");
    }

    errors = FlattenErrors(errorList);
    return errors.Count == 0;
}

static void AddError(IDictionary<string, List<string>> errors, string key, string message)
{
    if (!errors.TryGetValue(key, out var list))
    {
        list = new List<string>();
        errors[key] = list;
    }

    if (!list.Contains(message))
    {
        list.Add(message);
    }
}

static Dictionary<string, string[]> FlattenErrors(IDictionary<string, List<string>> errors)
{
    return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
}

static IResult ValidationProblem(string field, string message)
{
    return Results.ValidationProblem(new Dictionary<string, string[]>
    {
        [field] = new[] { message }
    });
}

static IResult NotFoundProblem(string detail)
{
    return Results.Problem(
        title: "Not Found",
        detail: detail,
        statusCode: StatusCodes.Status404NotFound,
        type: "https://httpstatuses.com/404");
}

static IResult ConflictProblem(string detail)
{
    return Results.Problem(
        title: "Conflict",
        detail: detail,
        statusCode: StatusCodes.Status409Conflict,
        type: "https://httpstatuses.com/409");
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
