using Core.Domain.Snapshots;
using Infrastructure.Memory;

namespace Unit.Snapshots;

public sealed class InMemorySnapshotStoreTests
{
    [Fact]
    public async Task TryAdd_ReturnsTrue_AndGetReturnsSnapshot()
    {
        var store = new InMemorySnapshotStore();
        var snapshot = BuildSnapshot(id: "snap-1", capturedAt: "2026-01-02T18:45:00Z");

        var added = await store.TryAddAsync(snapshot, CancellationToken.None);
        var loaded = await store.GetAsync("snap-1", CancellationToken.None);

        Assert.True(added);
        Assert.NotNull(loaded);
        Assert.Equal(snapshot.Id, loaded!.Id);
    }

    [Fact]
    public async Task TryAdd_ReturnsFalse_WhenCapturedAtAndSourceDuplicate()
    {
        var store = new InMemorySnapshotStore();
        var first = BuildSnapshot(id: "snap-1", capturedAt: "2026-01-02T18:45:00Z");
        var second = BuildSnapshot(id: "snap-2", capturedAt: "2026-01-02T18:45:00Z");

        var addedFirst = await store.TryAddAsync(first, CancellationToken.None);
        var addedSecond = await store.TryAddAsync(second, CancellationToken.None);

        Assert.True(addedFirst);
        Assert.False(addedSecond);
    }

    [Fact]
    public async Task List_ReturnsAllSnapshots()
    {
        var store = new InMemorySnapshotStore();
        await store.TryAddAsync(BuildSnapshot(id: "snap-1", capturedAt: "2026-01-02T18:45:00Z"), CancellationToken.None);
        await store.TryAddAsync(BuildSnapshot(id: "snap-2", capturedAt: "2026-01-02T18:50:00Z"), CancellationToken.None);

        var list = await store.ListAsync(CancellationToken.None);

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task UpdateMetadata_UpdatesNameAndNotes()
    {
        var store = new InMemorySnapshotStore();
        await store.TryAddAsync(BuildSnapshot(id: "snap-1", capturedAt: "2026-01-02T18:45:00Z"), CancellationToken.None);

        var updated = await store.UpdateMetadataAsync("snap-1", true, "Updated", true, "Notes", CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("Updated", updated!.Name);
        Assert.Equal("Notes", updated.Notes);
    }

    [Fact]
    public async Task Remove_ReturnsTrue_AndSnapshotIsGone()
    {
        var store = new InMemorySnapshotStore();
        await store.TryAddAsync(BuildSnapshot(id: "snap-1", capturedAt: "2026-01-02T18:45:00Z"), CancellationToken.None);

        var removed = await store.RemoveAsync("snap-1", CancellationToken.None);

        Assert.True(removed);
        var snapshot = await store.GetAsync("snap-1", CancellationToken.None);
        Assert.Null(snapshot);
    }

    private static Snapshot BuildSnapshot(string id, string capturedAt)
    {
        return new Snapshot(
            id,
            DateTimeOffset.Parse(capturedAt),
            capturedAt,
            "manual",
            "My snapshot",
            "Optional notes",
            new List<RepositorySnapshot>
            {
                new(
                    "repo-1",
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
