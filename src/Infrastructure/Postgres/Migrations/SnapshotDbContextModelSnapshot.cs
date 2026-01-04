using Core.Domain.SyncRuns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Infrastructure.Postgres.Migrations;

[DbContext(typeof(SnapshotDbContext))]
public partial class SnapshotDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.1");
        modelBuilder.HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.Entity("Infrastructure.Postgres.SnapshotRecord", b =>
        {
            b.Property<string>("Id")
                .HasColumnType("text")
                .HasColumnName("id");

            b.Property<DateTimeOffset>("CapturedAtUtc")
                .HasColumnType("timestamptz")
                .HasColumnName("captured_at_utc");

            b.Property<string>("Name")
                .HasColumnType("text")
                .HasColumnName("name");

            b.Property<string>("Notes")
                .HasColumnType("text")
                .HasColumnName("notes");

            b.Property<string>("Source")
                .IsRequired()
                .HasColumnType("text")
                .HasColumnName("source");

            b.HasKey("Id");

            b.HasIndex("Source", "CapturedAtUtc")
                .IsUnique();

            b.ToTable("snapshots");
        });

        modelBuilder.Entity("Infrastructure.Postgres.SnapshotRepositoryRecord", b =>
        {
            b.Property<string>("SnapshotId")
                .HasColumnType("text")
                .HasColumnName("snapshot_id");

            b.Property<string>("RepoId")
                .HasColumnType("text")
                .HasColumnName("repo_id");

            b.Property<string>("Description")
                .HasColumnType("text")
                .HasColumnName("description");

            b.Property<int>("Forks")
                .HasColumnType("integer")
                .HasColumnName("forks");

            b.Property<string>("FullName")
                .IsRequired()
                .HasColumnType("text")
                .HasColumnName("full_name");

            b.Property<string>("Language")
                .HasColumnType("text")
                .HasColumnName("language");

            b.Property<string>("Name")
                .IsRequired()
                .HasColumnType("text")
                .HasColumnName("name");

            b.Property<string>("Owner")
                .IsRequired()
                .HasColumnType("text")
                .HasColumnName("owner");

            b.Property<int>("Rank")
                .HasColumnType("integer")
                .HasColumnName("rank");

            b.Property<string>("RepoUpdatedAt")
                .HasColumnType("text")
                .HasColumnName("repo_updated_at");

            b.Property<int>("Stars")
                .HasColumnType("integer")
                .HasColumnName("stars");

            b.Property<string>("Url")
                .IsRequired()
                .HasColumnType("text")
                .HasColumnName("url");

            b.HasKey("SnapshotId", "RepoId");

            b.HasIndex("Forks");

            b.HasIndex("FullName");

            b.HasIndex("Language");

            b.HasIndex("SnapshotId", "Rank")
                .IsUnique();

            b.HasIndex("Stars");

            b.ToTable("snapshot_repositories");
        });

        modelBuilder.Entity("Infrastructure.Postgres.SnapshotRepositoryRecord", b =>
        {
            b.HasOne("Infrastructure.Postgres.SnapshotRecord", "Snapshot")
                .WithMany("Repositories")
                .HasForeignKey("SnapshotId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Snapshot");
        });

        modelBuilder.Entity("Infrastructure.Postgres.SnapshotRecord", b =>
        {
            b.Navigation("Repositories");
        });

        modelBuilder.Entity("Infrastructure.Postgres.SyncRunRecord", b =>
        {
            b.Property<string>("Id")
                .HasColumnType("text")
                .HasColumnName("id");

            b.Property<SyncRunStatus>("Status")
                .IsRequired()
                .HasColumnType("text")
                .HasConversion<string>()
                .HasColumnName("status");

            b.Property<DateTimeOffset>("RequestedAt")
                .HasColumnType("timestamptz")
                .HasColumnName("requested_at_utc");

            b.Property<DateTimeOffset?>("StartedAt")
                .HasColumnType("timestamptz")
                .HasColumnName("started_at_utc");

            b.Property<DateTimeOffset?>("FinishedAt")
                .HasColumnType("timestamptz")
                .HasColumnName("finished_at_utc");

            b.Property<string>("SnapshotId")
                .HasColumnType("text")
                .HasColumnName("snapshot_id");

            b.Property<string>("Error")
                .HasColumnType("text")
                .HasColumnName("error");

            b.Property<int?>("SeedsProcessed")
                .HasColumnType("integer")
                .HasColumnName("seeds_processed");

            b.Property<int?>("ItemsInserted")
                .HasColumnType("integer")
                .HasColumnName("items_inserted");

            b.Property<SyncRunFailureCode>("FailureCode")
                .HasColumnType("text")
                .HasConversion<string>()
                .HasColumnName("error_code");

            b.HasKey("Id");

            b.HasIndex("RequestedAt");

            b.ToTable("sync_runs");
        });
    }
}
