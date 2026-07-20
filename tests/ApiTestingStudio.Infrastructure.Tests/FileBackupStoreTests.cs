using System.IO;
using ApiTestingStudio.Infrastructure.Persistence;
using FluentAssertions;

namespace ApiTestingStudio.Infrastructure.Tests;

/// <summary>
/// Verifies the <see cref="FileBackupStore"/> allocates timestamped archives, lists them newest-first,
/// enumerates backed-up workspaces, and prunes to a retention count. See ADR-0012.
/// </summary>
public sealed class FileBackupStoreTests : TempDirectoryFixture
{
    private static readonly DateTimeOffset Base = new(2026, 7, 20, 5, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Allocate_creates_distinct_paths_per_second()
    {
        var store = new FileBackupStore(TempDir);
        var workspace = Guid.NewGuid();

        var a = store.AllocateBackupPath(workspace, Base);
        File.WriteAllText(a, "x");
        var b = store.AllocateBackupPath(workspace, Base); // same second → suffixed

        a.Should().NotBe(b);
        Path.GetExtension(a).Should().Be(".apistudio");
    }

    [Fact]
    public void List_returns_archives_newest_first_and_reports_workspace()
    {
        var store = new FileBackupStore(TempDir);
        var workspace = Guid.NewGuid();

        for (var i = 0; i < 3; i++)
        {
            File.WriteAllText(store.AllocateBackupPath(workspace, Base.AddMinutes(i)), "x");
        }

        var files = store.ListBackupFiles(workspace);
        files.Should().HaveCount(3);
        var names = files.Select(Path.GetFileName).ToList();
        names.Should().Equal(names.OrderByDescending(n => n, StringComparer.Ordinal));
        store.ListBackedUpWorkspaces().Should().Contain(workspace);
    }

    [Fact]
    public void Prune_keeps_only_the_newest_archives()
    {
        var store = new FileBackupStore(TempDir);
        var workspace = Guid.NewGuid();

        var newest = string.Empty;
        for (var i = 0; i < 5; i++)
        {
            newest = store.AllocateBackupPath(workspace, Base.AddMinutes(i));
            File.WriteAllText(newest, "x");
        }

        store.Prune(workspace, retain: 2);

        var remaining = store.ListBackupFiles(workspace);
        remaining.Should().HaveCount(2);
        remaining.Should().Contain(newest);
    }

    [Fact]
    public void List_of_unknown_workspace_is_empty()
    {
        var store = new FileBackupStore(TempDir);
        store.ListBackupFiles(Guid.NewGuid()).Should().BeEmpty();
    }
}
