using System;
using System.Collections.Generic;
using System.Linq;
using Core.Domain.Snapshots;

namespace Api.Support;

internal static class SnapshotQuery
{
    public static bool TryResolvePagination(
        int? page,
        int? pageSize,
        out PaginationParameters parameters,
        out Dictionary<string, string[]>? errors)
    {
        var resolvedPage = page ?? 1;
        var resolvedPageSize = pageSize ?? 20;

        var errorList = new Dictionary<string, string[]>();

        if (resolvedPage < 1)
        {
            errorList["page"] = new[] { "page must be >= 1." };
        }

        if (resolvedPageSize < 1 || resolvedPageSize > 100)
        {
            errorList["pageSize"] = new[] { "pageSize must be between 1 and 100." };
        }

        if (errorList.Count > 0)
        {
            parameters = new PaginationParameters(resolvedPage, resolvedPageSize);
            errors = errorList;
            return false;
        }

        parameters = new PaginationParameters(resolvedPage, resolvedPageSize);
        errors = null;
        return true;
    }

    public static bool TryResolveSortAndOrder(
        string? sort,
        string? order,
        out SortParameters parameters,
        out Dictionary<string, string[]>? errors)
    {
        var resolvedSort = string.IsNullOrWhiteSpace(sort) ? "rank" : sort.Trim().ToLowerInvariant();
        if (resolvedSort is not ("rank" or "stars" or "forks"))
        {
            parameters = new SortParameters(resolvedSort, "asc");
            errors = new Dictionary<string, string[]>
            {
                ["sort"] = new[] { "sort must be one of: rank, stars, forks." }
            };
            return false;
        }

        if (string.IsNullOrWhiteSpace(order))
        {
            var defaultOrder = resolvedSort == "rank" ? "asc" : "desc";
            parameters = new SortParameters(resolvedSort, defaultOrder);
            errors = null;
            return true;
        }

        var resolvedOrder = order.Trim().ToLowerInvariant();
        if (resolvedOrder is not ("asc" or "desc"))
        {
            parameters = new SortParameters(resolvedSort, resolvedOrder);
            errors = new Dictionary<string, string[]>
            {
                ["order"] = new[] { "order must be one of: asc, desc." }
            };
            return false;
        }

        parameters = new SortParameters(resolvedSort, resolvedOrder);
        errors = null;
        return true;
    }

    public static bool TryResolveCapturedAt(
        string? capturedAt,
        out DateTimeOffset capturedAtUtc,
        out string normalized)
    {
        if (string.IsNullOrWhiteSpace(capturedAt))
        {
            capturedAtUtc = DateTimeOffset.UtcNow;
        }
        else if (!DateTimeOffset.TryParse(
                     capturedAt,
                     System.Globalization.CultureInfo.InvariantCulture,
                     System.Globalization.DateTimeStyles.AssumeUniversal |
                     System.Globalization.DateTimeStyles.AdjustToUniversal,
                     out capturedAtUtc))
        {
            normalized = string.Empty;
            return false;
        }

        capturedAtUtc = capturedAtUtc.ToUniversalTime();
        normalized = capturedAtUtc.ToString("yyyy-MM-ddTHH:mm:ss'Z'");
        return true;
    }

    public static IEnumerable<RepositorySnapshot> ApplyRepositoryFilters(
        IReadOnlyList<RepositorySnapshot> repositories,
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

    public static IOrderedEnumerable<RepositorySnapshot> ApplyRepositorySort(
        IEnumerable<RepositorySnapshot> repositories,
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
}

internal sealed record PaginationParameters(int Page, int PageSize);

internal sealed record SortParameters(string Sort, string Order);
