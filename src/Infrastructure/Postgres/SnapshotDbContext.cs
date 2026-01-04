using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Postgres;

public sealed class SnapshotDbContext : DbContext
{
    public SnapshotDbContext(DbContextOptions<SnapshotDbContext> options)
        : base(options)
    {
    }

    public DbSet<SnapshotRecord> Snapshots => Set<SnapshotRecord>();
    public DbSet<SnapshotRepositoryRecord> SnapshotRepositories => Set<SnapshotRepositoryRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SnapshotRecord>(builder =>
        {
            builder.ToTable("snapshots");
            builder.HasKey(snapshot => snapshot.Id);
            builder.Property(snapshot => snapshot.Id).HasColumnName("id");
            builder.Property(snapshot => snapshot.Source).HasColumnName("source").IsRequired();
            builder.Property(snapshot => snapshot.CapturedAtUtc)
                .HasColumnName("captured_at_utc")
                .HasColumnType("timestamptz")
                .IsRequired();
            builder.Property(snapshot => snapshot.Name).HasColumnName("name");
            builder.Property(snapshot => snapshot.Notes).HasColumnName("notes");
            builder.HasIndex(snapshot => new { snapshot.Source, snapshot.CapturedAtUtc }).IsUnique();

            builder.HasMany(snapshot => snapshot.Repositories)
                .WithOne(repository => repository.Snapshot)
                .HasForeignKey(repository => repository.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SnapshotRepositoryRecord>(builder =>
        {
            builder.ToTable("snapshot_repositories");
            builder.HasKey(repository => new { repository.SnapshotId, repository.RepoId });
            builder.Property(repository => repository.SnapshotId).HasColumnName("snapshot_id");
            builder.Property(repository => repository.RepoId).HasColumnName("repo_id");
            builder.Property(repository => repository.Rank).HasColumnName("rank");
            builder.Property(repository => repository.Owner).HasColumnName("owner").IsRequired();
            builder.Property(repository => repository.Name).HasColumnName("name").IsRequired();
            builder.Property(repository => repository.FullName).HasColumnName("full_name").IsRequired();
            builder.Property(repository => repository.Description).HasColumnName("description");
            builder.Property(repository => repository.Language).HasColumnName("language");
            builder.Property(repository => repository.Stars).HasColumnName("stars");
            builder.Property(repository => repository.Forks).HasColumnName("forks");
            builder.Property(repository => repository.Url).HasColumnName("url").IsRequired();
            builder.Property(repository => repository.RepoUpdatedAt).HasColumnName("repo_updated_at");
            builder.HasIndex(repository => new { repository.SnapshotId, repository.Rank }).IsUnique();
            builder.HasIndex(repository => repository.FullName);
            builder.HasIndex(repository => repository.Language);
            builder.HasIndex(repository => repository.Stars);
            builder.HasIndex(repository => repository.Forks);
        });
    }
}
