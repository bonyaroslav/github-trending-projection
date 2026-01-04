using Core.Application.Snapshots;
using Core.Domain.Snapshots;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Postgres;

public sealed class PostgresSnapshotStore : ISnapshotStore
{
    private readonly SnapshotDbContext _dbContext;

    public PostgresSnapshotStore(SnapshotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> TryAddAsync(Snapshot snapshot, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var record = SnapshotRecordMapper.ToRecord(snapshot);
            _dbContext.Snapshots.Add(record);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            if (DbUpdateExceptionClassifier.IsUniqueViolation(exception))
            {
                return false;
            }

            throw;
        }
    }

    public async Task<IReadOnlyList<Snapshot>> ListAsync(CancellationToken cancellationToken)
    {
        var records = await _dbContext.Snapshots
            .AsNoTracking()
            .Include(snapshot => snapshot.Repositories)
            .ToListAsync(cancellationToken);

        return records
            .Select(SnapshotRecordMapper.ToDomain)
            .ToList();
    }

    public async Task<Snapshot?> GetAsync(string id, CancellationToken cancellationToken)
    {
        var record = await _dbContext.Snapshots
            .AsNoTracking()
            .Include(snapshot => snapshot.Repositories)
            .FirstOrDefaultAsync(snapshot => snapshot.Id == id, cancellationToken);

        return record is null ? null : SnapshotRecordMapper.ToDomain(record);
    }

    public async Task<RepositoryPage?> QueryRepositoriesAsync(
        string snapshotId,
        RepositoryQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var snapshotExists = await _dbContext.Snapshots
            .AsNoTracking()
            .AnyAsync(snapshot => snapshot.Id == snapshotId, cancellationToken);

        if (!snapshotExists)
        {
            return null;
        }

        IQueryable<SnapshotRepositoryRecord> query = _dbContext.SnapshotRepositories
            .AsNoTracking()
            .Where(repository => repository.SnapshotId == snapshotId);

        if (!string.IsNullOrWhiteSpace(parameters.Query))
        {
            var search = parameters.Query.Trim();
            query = query.Where(repository =>
                EF.Functions.ILike(repository.FullName, $"%{search}%") ||
                (repository.Description != null && EF.Functions.ILike(repository.Description, $"%{search}%")));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Language))
        {
            var language = parameters.Language.Trim();
            query = query.Where(repository =>
                repository.Language != null &&
                EF.Functions.ILike(repository.Language, language));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        query = ApplySort(query, parameters.Sort, parameters.Order);

        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        var mapped = items
            .Select(SnapshotRecordMapper.ToDomainRepository)
            .ToList();

        return new RepositoryPage(mapped, totalItems);
    }

    public async Task<Snapshot?> UpdateMetadataAsync(
        string id,
        bool hasName,
        string? name,
        bool hasNotes,
        string? notes,
        CancellationToken cancellationToken)
    {
        var record = await _dbContext.Snapshots
            .Include(snapshot => snapshot.Repositories)
            .FirstOrDefaultAsync(snapshot => snapshot.Id == id, cancellationToken);

        if (record is null)
        {
            return null;
        }

        if (hasName)
        {
            record.Name = name;
        }

        if (hasNotes)
        {
            record.Notes = notes;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return SnapshotRecordMapper.ToDomain(record);
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken cancellationToken)
    {
        var record = await _dbContext.Snapshots
            .FirstOrDefaultAsync(snapshot => snapshot.Id == id, cancellationToken);
        if (record is null)
        {
            return false;
        }

        _dbContext.Snapshots.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static IQueryable<SnapshotRepositoryRecord> ApplySort(
        IQueryable<SnapshotRepositoryRecord> repositories,
        string sort,
        string order)
    {
        return (sort, order) switch
        {
            ("stars", "asc") => repositories.OrderBy(repository => repository.Stars)
                .ThenBy(repository => repository.Rank),
            ("stars", "desc") => repositories.OrderByDescending(repository => repository.Stars)
                .ThenBy(repository => repository.Rank),
            ("forks", "asc") => repositories.OrderBy(repository => repository.Forks)
                .ThenBy(repository => repository.Rank),
            ("forks", "desc") => repositories.OrderByDescending(repository => repository.Forks)
                .ThenBy(repository => repository.Rank),
            ("rank", "desc") => repositories.OrderByDescending(repository => repository.Rank),
            _ => repositories.OrderBy(repository => repository.Rank)
        };
    }
}
