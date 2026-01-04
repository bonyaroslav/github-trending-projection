using Infrastructure.Postgres;
using Integration.Support;
using Microsoft.EntityFrameworkCore;

namespace Integration;

[Collection("Postgres")]
public sealed class MigrationsTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;

    public MigrationsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [DockerFact]
    public async Task Migrate_AppliesInitialMigration()
    {
        await using var context = new SnapshotDbContext(_fixture.CreateOptions());
        await context.Database.MigrateAsync();

        var applied = await context.Database.GetAppliedMigrationsAsync();

        Assert.NotEmpty(applied);
    }
}
