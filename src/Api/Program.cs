using Api.Endpoints;
using Core.Application.Snapshots;
using FluentValidation;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IValidator<SnapshotCreateCommand>, SnapshotCreateCommandValidator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthEndpoints();

app.MapGroup("/api/v1").MapSnapshotEndpoints();

app.Run();

public partial class Program;
