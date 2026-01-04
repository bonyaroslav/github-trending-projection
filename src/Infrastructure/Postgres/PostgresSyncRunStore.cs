using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.SyncRuns;
using Core.Domain.SyncRuns;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Postgres;

public sealed class PostgresSyncRunStore : ISyncRunStore
{
    private readonly SnapshotDbContext _dbContext;

    public PostgresSyncRunStore(SnapshotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> TryAddAsync(SyncRun syncRun, CancellationToken cancellationToken)
    {
        var record = SyncRunRecordMapper.ToRecord(syncRun);
        _dbContext.SyncRuns.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryPatchAsync(string id, SyncRunUpdate update, CancellationToken cancellationToken)
    {
        var record = await _dbContext.SyncRuns.FirstOrDefaultAsync(run => run.Id == id, cancellationToken);
        if (record is null)
        {
            return false;
        }

        if (update.Status.HasValue)
        {
            record.Status = update.Status.Value;
        }

        if (update.StartedAt.HasValue)
        {
            record.StartedAt = update.StartedAt;
        }

        if (update.FinishedAt.HasValue)
        {
            record.FinishedAt = update.FinishedAt;
        }

        if (update.SnapshotId is not null)
        {
            record.SnapshotId = update.SnapshotId;
        }

        if (update.Error is not null)
        {
            record.Error = update.Error;
        }

        if (update.SeedsProcessed.HasValue)
        {
            record.SeedsProcessed = update.SeedsProcessed;
        }

        if (update.ItemsInserted.HasValue)
        {
            record.ItemsInserted = update.ItemsInserted;
        }

        if (update.FailureCode.HasValue)
        {
            record.FailureCode = update.FailureCode.Value;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<SyncRun?> GetAsync(string id, CancellationToken cancellationToken)
    {
        var record = await _dbContext.SyncRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(run => run.Id == id, cancellationToken);

        return record is null ? null : SyncRunRecordMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<SyncRun>> ListLatestAsync(int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return Array.Empty<SyncRun>();
        }

        var records = await _dbContext.SyncRuns
            .AsNoTracking()
            .OrderByDescending(run => run.RequestedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return records.Select(SyncRunRecordMapper.ToDomain).ToList();
    }
}
