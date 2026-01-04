using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Postgres.Migrations;

public partial class AddSyncRuns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "sync_runs",
            columns: table => new
            {
                id = table.Column<string>(type: "text", nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                requested_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                started_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                finished_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                snapshot_id = table.Column<string>(type: "text", nullable: true),
                error = table.Column<string>(type: "text", nullable: true),
                seeds_processed = table.Column<int>(type: "integer", nullable: true),
                items_inserted = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_sync_runs", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_sync_runs_requested_at_utc",
            table: "sync_runs",
            column: "requested_at_utc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "sync_runs");
    }
}
