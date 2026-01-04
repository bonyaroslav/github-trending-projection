using Api.Endpoints;
using Core.Application.Snapshots;
using FluentValidation;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IValidator<SnapshotCreateCommand>, SnapshotCreateCommandValidator>();

var app = builder.Build();

app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        var problem = Results.Problem(
            title: "Internal Server Error",
            detail: "An unexpected error occurred.",
            statusCode: StatusCodes.Status500InternalServerError,
            type: "https://httpstatuses.com/500");

        await problem.ExecuteAsync(context);
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthEndpoints();

app.MapGroup("/api/v1").MapSnapshotEndpoints();

app.Run();

public partial class Program;
