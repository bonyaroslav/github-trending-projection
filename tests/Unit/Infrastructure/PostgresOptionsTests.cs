using Infrastructure;
using Infrastructure.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Unit.Infrastructure;

public sealed class PostgresOptionsTests
{
    [Fact]
    public void AddInfrastructure_Throws_WhenConnectionStringMissing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<PostgresOptions>>().Value);
    }

    [Fact]
    public void AddInfrastructure_BindsConnectionString_WhenProvided()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["POSTGRES_CONNECTION_STRING"] = "Host=localhost;Database=snapshots;Username=postgres;Password=postgres"
            })
            .Build();

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<PostgresOptions>>().Value;

        Assert.Equal("Host=localhost;Database=snapshots;Username=postgres;Password=postgres", options.ConnectionString);
    }
}
