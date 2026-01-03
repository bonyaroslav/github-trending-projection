using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Integration;

public sealed class SnapshotCrudEndpointsTests
{
    [Fact]
    public async Task CreateSnapshot_ReturnsCreatedWithLocation()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/snapshots", BuildCreateRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var payload = await response.Content.ReadFromJsonAsync<SnapshotDetailDto>();

        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Id));
        Assert.Equal("manual", payload.Source);
        Assert.Equal("My snapshot", payload.Name);
        Assert.Equal("Optional notes", payload.Notes);
        Assert.Equal(1, payload.ItemCount);
        Assert.EndsWith($"/api/v1/snapshots/{payload.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task GetSnapshot_ReturnsDetail()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var created = await CreateSnapshotAsync(client);

        var response = await client.GetAsync($"/api/v1/snapshots/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SnapshotDetailDto>();

        Assert.NotNull(payload);
        Assert.Equal(created.Id, payload!.Id);
        Assert.Equal("My snapshot", payload.Name);
        Assert.Equal("Optional notes", payload.Notes);
    }

    [Fact]
    public async Task ListSnapshots_ReturnsPagedSummaries()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var created = await CreateSnapshotAsync(client);

        var response = await client.GetAsync("/api/v1/snapshots?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<SnapshotSummaryDto>>();

        Assert.NotNull(payload);
        Assert.Equal(1, payload!.TotalItems);
        Assert.Equal(1, payload.TotalPages);
        Assert.Contains(payload.Items, item => item.Id == created.Id);
    }

    [Fact]
    public async Task PatchSnapshot_UpdatesMetadata()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var created = await CreateSnapshotAsync(client);

        var response = await client.PatchAsJsonAsync($"/api/v1/snapshots/{created.Id}", new
        {
            name = "Renamed snapshot",
            notes = "Updated notes"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SnapshotDetailDto>();

        Assert.NotNull(payload);
        Assert.Equal("Renamed snapshot", payload!.Name);
        Assert.Equal("Updated notes", payload.Notes);
    }

    [Fact]
    public async Task DeleteSnapshot_RemovesSnapshot()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var created = await CreateSnapshotAsync(client);

        var deleteResponse = await client.DeleteAsync($"/api/v1/snapshots/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/v1/snapshots/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private static async Task<SnapshotDetailDto> CreateSnapshotAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/snapshots", BuildCreateRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SnapshotDetailDto>();

        Assert.NotNull(payload);

        return payload!;
    }

    private static SnapshotCreateRequest BuildCreateRequest()
    {
        return new SnapshotCreateRequest(
            "manual",
            "My snapshot",
            "Optional notes",
            "2026-01-02T18:45:00Z",
            new List<RepositorySnapshotDto>
            {
                new(
                    "MDEwOlJlcG9zaXRvcnkxMjM0NTY3OA==",
                    1,
                    "octocat",
                    "hello-world",
                    "octocat/hello-world",
                    "Example repo",
                    "C#",
                    1234,
                    56,
                    "https://github.com/octocat/hello-world",
                    "2025-12-31T12:00:00Z")
            });
    }
}