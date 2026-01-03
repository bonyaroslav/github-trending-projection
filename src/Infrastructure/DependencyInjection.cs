using Core.Application.Snapshots;
using Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["POSTGRES_CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("POSTGRES_CONNECTION_STRING is required.");
        }

        services.AddDbContext<SnapshotDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ISnapshotStore, PostgresSnapshotStore>();
        return services;
    }
}
