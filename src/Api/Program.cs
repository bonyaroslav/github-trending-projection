var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.StatusCode(StatusCodes.Status501NotImplemented));
app.MapGet("/version", () => Results.StatusCode(StatusCodes.Status501NotImplemented));
app.MapGet("/repositories", () => Results.StatusCode(StatusCodes.Status501NotImplemented));
app.MapGet("/repositories/{id}", (string id) => Results.StatusCode(StatusCodes.Status501NotImplemented));

app.Run();
