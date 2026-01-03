using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Integration.Support;
using Microsoft.AspNetCore.Mvc;

namespace Integration;

[Collection("Postgres")]
public sealed class SnapshotCrudEndpointsTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private PostgresWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public SnapshotCrudEndpointsTests(PostgresFixture fixture)
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
    public async Task CreateSnapshot_ReturnsCreatedWithLocation()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/snapshots", BuildCreateRequest());

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

    [DockerFact]
    public async Task GetSnapshot_ReturnsDetail()
    {
        var created = await CreateSnapshotAsync(_client);

        var response = await _client.GetAsync($"/api/v1/snapshots/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SnapshotDetailDto>();

        Assert.NotNull(payload);
        Assert.Equal(created.Id, payload!.Id);
        Assert.Equal("My snapshot", payload.Name);
        Assert.Equal("Optional notes", payload.Notes);
    }

    [DockerFact]
    public async Task ListSnapshots_ReturnsPagedSummaries()
    {
        var created = await CreateSnapshotAsync(_client);

        var response = await _client.GetAsync("/api/v1/snapshots?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<SnapshotSummaryDto>>();

        Assert.NotNull(payload);
        Assert.Equal(1, payload!.TotalItems);
        Assert.Equal(1, payload.TotalPages);
        Assert.Contains(payload.Items, item => item.Id == created.Id);
    }

    [DockerFact]
    public async Task PatchSnapshot_UpdatesMetadata()
    {
        var created = await CreateSnapshotAsync(_client);

        var response = await _client.PatchAsJsonAsync($"/api/v1/snapshots/{created.Id}", new
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

    [DockerFact]
    public async Task DeleteSnapshot_RemovesSnapshot()
    {
        var created = await CreateSnapshotAsync(_client);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/snapshots/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/snapshots/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [DockerFact]
    public async Task CreateSnapshot_ReturnsBadRequest_WhenRepositoriesEmpty()
    {
        var request = BuildCreateRequest(repositories: new List<RepositorySnapshotDto>());

        var response = await _client.PostAsJsonAsync("/api/v1/snapshots", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [DockerFact]
    public async Task CreateSnapshot_ReturnsConflict_WhenCapturedAtAndSourceMatch()
    {
        var request = BuildCreateRequest(capturedAt: "2026-01-02T18:45:00Z", source: "manual");

        var first = await _client.PostAsJsonAsync("/api/v1/snapshots", request);
        var second = await _client.PostAsJsonAsync("/api/v1/snapshots", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [DockerFact]
    public async Task GetSnapshot_ReturnsNotFound_WhenMissing()
    {
        var response = await _client.GetAsync("/api/v1/snapshots/missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [DockerFact]
    public async Task GetSnapshot_ReturnsProblemDetails_WhenMissing()
    {
        var response = await _client.GetAsync("/api/v1/snapshots/missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(payload);
        Assert.Equal((int)HttpStatusCode.NotFound, payload!.Status);
    }

    [DockerFact]
    public async Task CreateSnapshot_ReturnsProblemDetails_WhenInvalid()
    {
        var request = BuildCreateRequest(repositories: new List<RepositorySnapshotDto>());

        var response = await _client.PostAsJsonAsync("/api/v1/snapshots", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var payload = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(payload);
        Assert.Equal((int)HttpStatusCode.BadRequest, payload!.Status);
    }

    [DockerFact]
    public async Task CreateSnapshot_ReturnsProblemDetails_WhenConflict()
    {
        var request = BuildCreateRequest(capturedAt: "2026-01-02T18:45:00Z", source: "manual");

        var first = await _client.PostAsJsonAsync("/api/v1/snapshots", request);
        var second = await _client.PostAsJsonAsync("/api/v1/snapshots", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);

        var payload = await second.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(payload);
        Assert.Equal((int)HttpStatusCode.Conflict, payload!.Status);
    }

    [DockerFact]
    public async Task CreateSnapshot_ReturnsBadRequest_WhenRankDuplicate()
    {
        var repositories = new List<RepositorySnapshotDto>
        {
            BuildRepository(rank: 1, repoId: "repo-1"),
            BuildRepository(rank: 1, repoId: "repo-2")
        };

        var response = await _client.PostAsJsonAsync("/api/v1/snapshots", BuildCreateRequest(repositories: repositories));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [DockerFact]
    public async Task CreateSnapshot_ReturnsBadRequest_WhenRepoIdDuplicate()
    {
        var repositories = new List<RepositorySnapshotDto>
        {
            BuildRepository(rank: 1, repoId: "repo-1"),
            BuildRepository(rank: 2, repoId: "repo-1")
        };

        var response = await _client.PostAsJsonAsync("/api/v1/snapshots", BuildCreateRequest(repositories: repositories));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [DockerFact]
    public async Task CreateSnapshot_ReturnsBadRequest_WhenRankHasGaps()
    {
        var repositories = new List<RepositorySnapshotDto>
        {
            BuildRepository(rank: 1, repoId: "repo-1"),
            BuildRepository(rank: 3, repoId: "repo-2")
        };

        var response = await _client.PostAsJsonAsync("/api/v1/snapshots", BuildCreateRequest(repositories: repositories));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [DockerFact]
    public async Task CreateSnapshot_ReturnsBadRequest_WhenStarsOrForksNegative()
    {
        var repositories = new List<RepositorySnapshotDto>
        {
            BuildRepository(rank: 1, repoId: "repo-1", stars: -1),
            BuildRepository(rank: 2, repoId: "repo-2", forks: -5)
        };

        var response = await _client.PostAsJsonAsync("/api/v1/snapshots", BuildCreateRequest(repositories: repositories));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [DockerFact]
    public async Task GetSnapshotRepositories_ReturnsPagedRepositories()
    {
        var created = await CreateSnapshotAsync(_client);

        var response = await _client.GetAsync($"/api/v1/snapshots/{created.Id}/repositories?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<RepositorySnapshotDto>>();

        Assert.NotNull(payload);
        Assert.Equal(1, payload!.TotalItems);
        Assert.Single(payload.Items);
        Assert.Equal("octocat/hello-world", payload.Items[0].FullName);
    }

    [DockerFact]
    public async Task GetSnapshotRepositories_FiltersByQuery()
    {
        var created = await CreateSnapshotAsync(_client, BuildRepositoryList());

        var response = await _client.GetAsync($"/api/v1/snapshots/{created.Id}/repositories?q=runtime");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<RepositorySnapshotDto>>();

        Assert.NotNull(payload);
        Assert.Single(payload!.Items);
        Assert.Equal("dotnet/runtime", payload.Items[0].FullName);
    }

    [DockerFact]
    public async Task GetSnapshotRepositories_FiltersByLanguage()
    {
        var created = await CreateSnapshotAsync(_client, BuildRepositoryList());

        var response = await _client.GetAsync($"/api/v1/snapshots/{created.Id}/repositories?language=Kotlin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<RepositorySnapshotDto>>();

        Assert.NotNull(payload);
        Assert.Single(payload!.Items);
        Assert.Equal("kotlin/kotlinx.coroutines", payload.Items[0].FullName);
    }

    [DockerFact]
    public async Task GetSnapshotRepositories_SortsByStarsDescendingByDefault()
    {
        var created = await CreateSnapshotAsync(_client, BuildRepositoryList());

        var response = await _client.GetAsync($"/api/v1/snapshots/{created.Id}/repositories?sort=stars");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<RepositorySnapshotDto>>();

        Assert.NotNull(payload);
        Assert.Equal("dotnet/runtime", payload!.Items[0].FullName);
    }

    [DockerFact]
    public async Task GetSnapshotRepositories_SortsByForksAscending()
    {
        var created = await CreateSnapshotAsync(_client, BuildRepositoryList());

        var response = await _client.GetAsync($"/api/v1/snapshots/{created.Id}/repositories?sort=forks&order=asc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<RepositorySnapshotDto>>();

        Assert.NotNull(payload);
        Assert.Equal("octocat/hello-world", payload!.Items[0].FullName);
    }

    [DockerFact]
    public async Task GetSnapshotRepository_ReturnsRepositoryById()
    {
        var created = await CreateSnapshotAsync(_client);
        var repoId = Uri.EscapeDataString("MDEwOlJlcG9zaXRvcnkxMjM0NTY3OA==");

        var response = await _client.GetAsync($"/api/v1/snapshots/{created.Id}/repositories/{repoId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<RepositorySnapshotDto>();

        Assert.NotNull(payload);
        Assert.Equal("octocat/hello-world", payload!.FullName);
        Assert.Equal(1, payload.Rank);
    }

    [DockerFact]
    public async Task GetSnapshotRepositoryByFullName_ReturnsRepository()
    {
        var created = await CreateSnapshotAsync(_client);

        var response = await _client.GetAsync($"/api/v1/snapshots/{created.Id}/repositories/by-full-name?fullName=octocat/hello-world");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<RepositorySnapshotDto>();

        Assert.NotNull(payload);
        Assert.Equal("octocat/hello-world", payload!.FullName);
    }

    [DockerFact]
    public async Task GetSnapshotRepositoryByFullName_ReturnsNotFound_WhenMissing()
    {
        var created = await CreateSnapshotAsync(_client);

        var response = await _client.GetAsync($"/api/v1/snapshots/{created.Id}/repositories/by-full-name?fullName=missing/repo");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [DockerFact]
    public async Task GetSnapshotRepositoryByFullName_ReturnsBadRequest_WhenFullNameMissing()
    {
        var created = await CreateSnapshotAsync(_client);

        var response = await _client.GetAsync($"/api/v1/snapshots/{created.Id}/repositories/by-full-name");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<SnapshotDetailDto> CreateSnapshotAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/snapshots", BuildCreateRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SnapshotDetailDto>();

        Assert.NotNull(payload);

        return payload!;
    }

    private static async Task<SnapshotDetailDto> CreateSnapshotAsync(HttpClient client, List<RepositorySnapshotDto> repositories)
    {
        var response = await client.PostAsJsonAsync("/api/v1/snapshots", BuildCreateRequest(repositories: repositories));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SnapshotDetailDto>();

        Assert.NotNull(payload);

        return payload!;
    }

    private static SnapshotCreateRequest BuildCreateRequest(
        string? capturedAt = null,
        string? source = null,
        List<RepositorySnapshotDto>? repositories = null)
    {
        return new SnapshotCreateRequest(
            source ?? "manual",
            "My snapshot",
            "Optional notes",
            capturedAt ?? "2026-01-02T18:45:00Z",
            repositories ?? new List<RepositorySnapshotDto>
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

    private static List<RepositorySnapshotDto> BuildRepositoryList()
    {
        return new List<RepositorySnapshotDto>
        {
            BuildRepository(rank: 1, repoId: "repo-1", fullName: "octocat/hello-world", language: "C#", stars: 1234, forks: 56),
            BuildRepository(rank: 2, repoId: "repo-2", fullName: "dotnet/runtime", language: "C#", stars: 25000, forks: 4100),
            BuildRepository(rank: 3, repoId: "repo-3", fullName: "kotlin/kotlinx.coroutines", language: "Kotlin", stars: 8000, forks: 900)
        };
    }

    private static RepositorySnapshotDto BuildRepository(
        int rank,
        string repoId,
        string fullName = "octocat/hello-world",
        string language = "C#",
        int stars = 1234,
        int forks = 56)
    {
        var parts = fullName.Split('/');
        var owner = parts.Length > 0 ? parts[0] : "octocat";
        var name = parts.Length > 1 ? parts[1] : "hello-world";

        return new RepositorySnapshotDto(
            repoId,
            rank,
            owner,
            name,
            fullName,
            "Example repo",
            language,
            stars,
            forks,
            $"https://github.com/{fullName}",
            "2025-12-31T12:00:00Z");
    }
}
