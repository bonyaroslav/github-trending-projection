using Api.Contracts;
using Infrastructure.Postgres;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Api.Endpoints;

internal static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new HealthResponse("ok")));
        app.MapGet("/health/ready", async (SnapshotDbContext dbContext, CancellationToken cancellationToken) =>
        {
            try
            {
                var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
                return canConnect
                    ? Results.Ok(new HealthResponse("ready"))
                    : Results.Problem(
                        title: "Service Unavailable",
                        detail: "Database connectivity check failed.",
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        type: "https://httpstatuses.com/503");
            }
            catch
            {
                return Results.Problem(
                    title: "Service Unavailable",
                    detail: "Database connectivity check failed.",
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    type: "https://httpstatuses.com/503");
            }
        });
        app.MapGet("/version", () => Results.Ok(new VersionResponse("dev")));
        return app;
    }
}
