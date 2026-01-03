using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Integration.Support;

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly TestcontainersContainer _container;

    public PostgresFixture()
    {
        _container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage("postgres:16-alpine")
            .WithEnvironment("POSTGRES_USER", "postgres")
            .WithEnvironment("POSTGRES_PASSWORD", "postgres")
            .WithEnvironment("POSTGRES_DB", "snapshots")
            .WithPortBinding(5432, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
    }

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        if (!DockerTestsEnabled)
        {
            return;
        }

        try
        {
            await _container.StartAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Docker is required for Postgres integration tests.", ex);
        }

        var port = _container.GetMappedPublicPort(5432);
        ConnectionString = $"Host=localhost;Port={port};Database=snapshots;Username=postgres;Password=postgres;Include Error Detail=true";
        await EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (!DockerTestsEnabled)
        {
            return;
        }

        await _container.DisposeAsync();
    }

    public DbContextOptions<SnapshotDbContext> CreateOptions()
    {
        if (!DockerTestsEnabled || string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException("Docker integration tests are disabled. Set RUN_DOCKER_TESTS=1 to enable.");
        }

        return new DbContextOptionsBuilder<SnapshotDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
    }

    public async Task ResetAsync()
    {
        if (!DockerTestsEnabled)
        {
            return;
        }

        await using var context = new SnapshotDbContext(CreateOptions());
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"snapshot_repositories\", \"snapshots\";");
    }

    private async Task EnsureCreatedAsync()
    {
        await using var context = new SnapshotDbContext(CreateOptions());
        await context.Database.EnsureCreatedAsync();
    }

    private static bool DockerTestsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable("RUN_DOCKER_TESTS"), "1", StringComparison.OrdinalIgnoreCase);
}
