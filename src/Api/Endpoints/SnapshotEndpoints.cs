using System.Text.Json;
using Api.Contracts;
using Api.Mapping;
using Api.Support;
using Core.Application.Snapshots;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Api.Endpoints;

internal static class SnapshotEndpoints
{
    public static RouteGroupBuilder MapSnapshotEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/snapshots", ListSnapshots);
        group.MapPost("/snapshots", CreateSnapshot);
        group.MapGet("/snapshots/{snapshotId}", GetSnapshot);
        group.MapPatch("/snapshots/{snapshotId}", PatchSnapshot);
        group.MapDelete("/snapshots/{snapshotId}", DeleteSnapshot);
        group.MapGet("/snapshots/{snapshotId}/repositories", ListSnapshotRepositories);
        group.MapGet("/snapshots/{snapshotId}/repositories/{repoId}", GetSnapshotRepository);
        group.MapGet("/snapshots/{snapshotId}/repositories/by-full-name", GetSnapshotRepositoryByFullName);
        return group;
    }

    private static IResult ListSnapshots(int? page, int? pageSize, ISnapshotStore store)
    {
        if (!SnapshotQuery.TryResolvePagination(page, pageSize, out var parameters, out var errors))
        {
            return Results.ValidationProblem(errors!);
        }

        var snapshots = store.List()
            .OrderByDescending(snapshot => snapshot.CapturedAtUtc)
            .ToList();

        var totalItems = snapshots.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)parameters.PageSize);

        var items = totalItems == 0
            ? new List<SnapshotSummaryDto>()
            : snapshots
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(snapshot => snapshot.ToSummaryDto())
                .ToList();

        var response = new PagedResponse<SnapshotSummaryDto>(
            items,
            parameters.Page,
            parameters.PageSize,
            totalItems,
            totalPages);

        return Results.Ok(response);
    }

    private static IResult CreateSnapshot(
        SnapshotCreateRequest request,
        ISnapshotStore store,
        IValidator<SnapshotCreateCommand> validator)
    {
        var validationResult = validator.Validate(request.ToCommand());

        if (!validationResult.IsValid)
        {
            return ValidationProblemFactory.FromValidationResult(validationResult);
        }

        var source = string.IsNullOrWhiteSpace(request.Source) ? "manual" : request.Source;

        if (!SnapshotQuery.TryResolveCapturedAt(request.CapturedAt, out var capturedAtUtc, out var capturedAt))
        {
            return ApiErrors.ValidationProblem("capturedAt", "capturedAt must be a valid UTC ISO 8601 timestamp.");
        }

        var snapshot = new Core.Domain.Snapshots.Snapshot(
            Guid.NewGuid().ToString("N"),
            capturedAtUtc,
            capturedAt,
            source!,
            request.Name,
            request.Notes,
            request.Repositories.ToDomainRepositories());

        if (!store.TryAdd(snapshot))
        {
            return ApiErrors.Conflict("A snapshot with the same source and capturedAt already exists.");
        }

        var detail = snapshot.ToDetailDto();

        return Results.Created($"/api/v1/snapshots/{snapshot.Id}", detail);
    }

    private static IResult GetSnapshot(string snapshotId, ISnapshotStore store)
    {
        var snapshot = store.Get(snapshotId);

        return snapshot is null
            ? ApiErrors.NotFound("Snapshot not found.")
            : Results.Ok(snapshot.ToDetailDto());
    }

    private static async Task<IResult> PatchSnapshot(string snapshotId, HttpRequest request, ISnapshotStore store)
    {
        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(request.Body);
        }
        catch (JsonException)
        {
            return ApiErrors.ValidationProblem("body", "Request body must be valid JSON.");
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return ApiErrors.ValidationProblem("body", "Request body must be a JSON object.");
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
                        return ApiErrors.ValidationProblem(property.Name, "Unknown field.");
                }
            }

            if (!hasName && !hasNotes)
            {
                return ApiErrors.ValidationProblem("body", "At least one field must be provided.");
            }

            var updated = store.UpdateMetadata(snapshotId, hasName, name, hasNotes, notes);

            return updated is null
                ? ApiErrors.NotFound("Snapshot not found.")
                : Results.Ok(updated.ToDetailDto());
        }
    }

    private static IResult DeleteSnapshot(string snapshotId, ISnapshotStore store)
    {
        return store.Remove(snapshotId)
            ? Results.NoContent()
            : ApiErrors.NotFound("Snapshot not found.");
    }

    private static IResult ListSnapshotRepositories(
        string snapshotId,
        int? page,
        int? pageSize,
        string? q,
        string? language,
        string? sort,
        string? order,
        ISnapshotStore store)
    {
        if (!SnapshotQuery.TryResolvePagination(page, pageSize, out var parameters, out var paginationErrors))
        {
            return Results.ValidationProblem(paginationErrors!);
        }

        if (!SnapshotQuery.TryResolveSortAndOrder(sort, order, out var sortParameters, out var sortErrors))
        {
            return Results.ValidationProblem(sortErrors!);
        }

        var snapshot = store.Get(snapshotId);
        if (snapshot is null)
        {
            return ApiErrors.NotFound("Snapshot not found.");
        }

        var repositories = SnapshotQuery.ApplyRepositoryFilters(snapshot.Repositories, q, language);
        var ordered = SnapshotQuery.ApplyRepositorySort(repositories, sortParameters.Sort, sortParameters.Order);
        var orderedList = ordered.ToList();

        var totalItems = orderedList.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)parameters.PageSize);

        var items = totalItems == 0
            ? new List<RepositorySnapshotDto>()
            : orderedList
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(repository => repository.ToDto())
                .ToList();

        var response = new PagedResponse<RepositorySnapshotDto>(
            items,
            parameters.Page,
            parameters.PageSize,
            totalItems,
            totalPages);

        return Results.Ok(response);
    }

    private static IResult GetSnapshotRepository(string snapshotId, string repoId, ISnapshotStore store)
    {
        var snapshot = store.Get(snapshotId);
        if (snapshot is null)
        {
            return ApiErrors.NotFound("Snapshot not found.");
        }

        var repository = snapshot.Repositories.FirstOrDefault(item => item.RepoId == repoId);

        return repository is null
            ? ApiErrors.NotFound("Repository not found in snapshot.")
            : Results.Ok(repository.ToDto());
    }

    private static IResult GetSnapshotRepositoryByFullName(string snapshotId, string? fullName, ISnapshotStore store)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return ApiErrors.ValidationProblem("fullName", "fullName is required.");
        }

        var snapshot = store.Get(snapshotId);
        if (snapshot is null)
        {
            return ApiErrors.NotFound("Snapshot not found.");
        }

        var repository = snapshot.Repositories.FirstOrDefault(item =>
            string.Equals(item.FullName, fullName, StringComparison.OrdinalIgnoreCase));

        return repository is null
            ? ApiErrors.NotFound("Repository not found in snapshot.")
            : Results.Ok(repository.ToDto());
    }
}
