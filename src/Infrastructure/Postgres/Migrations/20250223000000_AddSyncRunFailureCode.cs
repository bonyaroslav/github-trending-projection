using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Postgres.Migrations;

public partial class AddSyncRunFailureCode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "error_code",
            table: "sync_runs",
            type: "text",
            nullable: false,
            defaultValue: "None");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "error_code", table: "sync_runs");
    }
}
