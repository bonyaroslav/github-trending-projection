using System.Net;
using System.Net.Http.Json;
using Integration.Support;

namespace Integration;

[Collection("Postgres")]
public sealed class HealthVersionEndpointsTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private PostgresWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public HealthVersionEndpointsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _factory = new PostgresWebApplicationFactory(_fixture.ConnectionString);
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [DockerFact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [DockerFact]
    public async Task GetReady_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [DockerFact]
    public async Task GetVersion_ReturnsVersionPayload()
    {
        var response = await _client.GetAsync("/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<VersionResponse>();

        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Version));
    }

    private sealed record VersionResponse(string Version);
}
