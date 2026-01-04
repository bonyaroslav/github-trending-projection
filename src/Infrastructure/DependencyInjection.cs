using Core.Application.Snapshots;
using Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PostgresOptions>()
            .Configure(options => options.ConnectionString = configuration["POSTGRES_CONNECTION_STRING"] ?? string.Empty)
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "POSTGRES_CONNECTION_STRING is required.")
            .ValidateOnStart();

        services.AddDbContext<SnapshotDbContext>((provider, options) =>
        {
            var postgresOptions = provider.GetRequiredService<IOptions<PostgresOptions>>().Value;
            options.UseNpgsql(postgresOptions.ConnectionString);
        });
        services.AddScoped<ISnapshotStore, PostgresSnapshotStore>();
        return services;
    }
}
