using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Postgres.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "snapshots",
            columns: table => new
            {
                id = table.Column<string>(type: "text", nullable: false),
                source = table.Column<string>(type: "text", nullable: false),
                captured_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                name = table.Column<string>(type: "text", nullable: true),
                notes = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_snapshots", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "snapshot_repositories",
            columns: table => new
            {
                snapshot_id = table.Column<string>(type: "text", nullable: false),
                repo_id = table.Column<string>(type: "text", nullable: false),
                rank = table.Column<int>(type: "integer", nullable: false),
                owner = table.Column<string>(type: "text", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                full_name = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                language = table.Column<string>(type: "text", nullable: true),
                stars = table.Column<int>(type: "integer", nullable: false),
                forks = table.Column<int>(type: "integer", nullable: false),
                url = table.Column<string>(type: "text", nullable: false),
                repo_updated_at = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_snapshot_repositories", x => new { x.snapshot_id, x.repo_id });
                table.ForeignKey(
                    name: "fk_snapshot_repositories_snapshots_snapshot_id",
                    column: x => x.snapshot_id,
                    principalTable: "snapshots",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_snapshots_source_captured_at_utc",
            table: "snapshots",
            columns: new[] { "source", "captured_at_utc" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_snapshot_repositories_snapshot_id_rank",
            table: "snapshot_repositories",
            columns: new[] { "snapshot_id", "rank" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_snapshot_repositories_full_name",
            table: "snapshot_repositories",
            column: "full_name");

        migrationBuilder.CreateIndex(
            name: "ix_snapshot_repositories_language",
            table: "snapshot_repositories",
            column: "language");

        migrationBuilder.CreateIndex(
            name: "ix_snapshot_repositories_stars",
            table: "snapshot_repositories",
            column: "stars");

        migrationBuilder.CreateIndex(
            name: "ix_snapshot_repositories_forks",
            table: "snapshot_repositories",
            column: "forks");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "snapshot_repositories");
        migrationBuilder.DropTable(name: "snapshots");
    }
}
