using Core.Domain.Snapshots;
using Infrastructure.Memory;

namespace Unit.Snapshots;

public sealed class InMemorySnapshotStoreTests
{
    [Fact]
    public void TryAdd_ReturnsTrue_AndGetReturnsSnapshot()
    {
        var store = new InMemorySnapshotStore();
        var snapshot = BuildSnapshot(id: "snap-1", capturedAt: "2026-01-02T18:45:00Z");

        var added = store.TryAdd(snapshot);
        var loaded = store.Get("snap-1");

        Assert.True(added);
        Assert.NotNull(loaded);
        Assert.Equal(snapshot.Id, loaded!.Id);
    }

    [Fact]
    public void TryAdd_ReturnsFalse_WhenCapturedAtAndSourceDuplicate()
    {
        var store = new InMemorySnapshotStore();
        var first = BuildSnapshot(id: "snap-1", capturedAt: "2026-01-02T18:45:00Z");
        var second = BuildSnapshot(id: "snap-2", capturedAt: "2026-01-02T18:45:00Z");

        var addedFirst = store.TryAdd(first);
        var addedSecond = store.TryAdd(second);

        Assert.True(addedFirst);
        Assert.False(addedSecond);
    }

    [Fact]
    public void List_ReturnsAllSnapshots()
    {
        var store = new InMemorySnapshotStore();
        store.TryAdd(BuildSnapshot(id: "snap-1", capturedAt: "2026-01-02T18:45:00Z"));
        store.TryAdd(BuildSnapshot(id: "snap-2", capturedAt: "2026-01-02T18:50:00Z"));

        var list = store.List();

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void UpdateMetadata_UpdatesNameAndNotes()
    {
        var store = new InMemorySnapshotStore();
        store.TryAdd(BuildSnapshot(id: "snap-1", capturedAt: "2026-01-02T18:45:00Z"));

        var updated = store.UpdateMetadata("snap-1", true, "Updated", true, "Notes");

        Assert.NotNull(updated);
        Assert.Equal("Updated", updated!.Name);
        Assert.Equal("Notes", updated.Notes);
    }

    [Fact]
    public void Remove_ReturnsTrue_AndSnapshotIsGone()
    {
        var store = new InMemorySnapshotStore();
        store.TryAdd(BuildSnapshot(id: "snap-1", capturedAt: "2026-01-02T18:45:00Z"));

        var removed = store.Remove("snap-1");

        Assert.True(removed);
        Assert.Null(store.Get("snap-1"));
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
