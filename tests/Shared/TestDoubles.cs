using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Common;
using Core.Application.GitHub;
using Core.Application.Sources;

namespace Tests.Shared;

public sealed class FakeTrendingSeedProvider : ITrendingSeedProvider
{
    private readonly IReadOnlyList<RepositorySeed> _seeds;

    public FakeTrendingSeedProvider(IEnumerable<RepositorySeed>? seeds = null)
    {
        _seeds = seeds is IReadOnlyList<RepositorySeed> list
            ? list
            : seeds is null
                ? Array.Empty<RepositorySeed>()
                : new List<RepositorySeed>(seeds);
    }

    public Task<IReadOnlyList<RepositorySeed>> GetSeedsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_seeds);
}

public sealed class FakeGraphQlClient : IGitHubGraphQlClient
{
    private readonly Dictionary<(string Owner, string Name), RepositoryEnrichment> _responses;

    public FakeGraphQlClient(Dictionary<(string Owner, string Name), RepositoryEnrichment>? responses = null)
    {
        _responses = responses is null ? new() : new(responses);
    }

    public Task<RepositoryEnrichment?> GetRepositoryAsync(string owner, string name, CancellationToken cancellationToken) =>
        Task.FromResult(_responses.TryGetValue((owner, name), out var enrichment) ? enrichment : null);
}

public sealed class TestClock : IClock
{
    private readonly Queue<DateTimeOffset> _values;

    public TestClock(IEnumerable<DateTimeOffset>? values = null)
    {
        _values = values is null ? new Queue<DateTimeOffset>() : new Queue<DateTimeOffset>(values);
    }

    public DateTimeOffset UtcNow =>
        _values.Count > 0 ? _values.Dequeue() : DateTimeOffset.UtcNow;
}

public sealed class TestIdGenerator : IIdGenerator
{
    private readonly Queue<string> _values;

    public TestIdGenerator(params string[] values)
    {
        _values = new Queue<string>(values);
    }

    public string NewId() =>
        _values.Count > 0 ? _values.Dequeue() : Guid.NewGuid().ToString("D");
}
