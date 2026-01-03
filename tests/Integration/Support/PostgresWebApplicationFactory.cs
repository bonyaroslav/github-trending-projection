using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Integration.Support;

public sealed class PostgresWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public PostgresWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["POSTGRES_CONNECTION_STRING"] = _connectionString
            };

            config.AddInMemoryCollection(settings);
        });
    }
}
