using Core.Application.Snapshots;
using Infrastructure.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISnapshotStore, InMemorySnapshotStore>();
        return services;
    }
}
