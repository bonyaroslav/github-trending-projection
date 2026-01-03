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

    public bool TryAdd(Snapshot snapshot)
    {
        using var transaction = _dbContext.Database.BeginTransaction();

        try
        {
            var record = SnapshotRecordMapper.ToRecord(snapshot);
            _dbContext.Snapshots.Add(record);
            _dbContext.SaveChanges();
            transaction.Commit();
            return true;
        }
        catch (DbUpdateException)
        {
            transaction.Rollback();
            return false;
        }
    }

    public IReadOnlyList<Snapshot> List()
    {
        return _dbContext.Snapshots
            .AsNoTracking()
            .Include(snapshot => snapshot.Repositories)
            .ToList()
            .Select(SnapshotRecordMapper.ToDomain)
            .ToList();
    }

    public Snapshot? Get(string id)
    {
        var record = _dbContext.Snapshots
            .AsNoTracking()
            .Include(snapshot => snapshot.Repositories)
            .FirstOrDefault(snapshot => snapshot.Id == id);

        return record is null ? null : SnapshotRecordMapper.ToDomain(record);
    }

    public Snapshot? UpdateMetadata(string id, bool hasName, string? name, bool hasNotes, string? notes)
    {
        var record = _dbContext.Snapshots
            .Include(snapshot => snapshot.Repositories)
            .FirstOrDefault(snapshot => snapshot.Id == id);

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

        _dbContext.SaveChanges();

        return SnapshotRecordMapper.ToDomain(record);
    }

    public bool Remove(string id)
    {
        var record = _dbContext.Snapshots.FirstOrDefault(snapshot => snapshot.Id == id);
        if (record is null)
        {
            return false;
        }

        _dbContext.Snapshots.Remove(record);
        _dbContext.SaveChanges();
        return true;
    }
}
