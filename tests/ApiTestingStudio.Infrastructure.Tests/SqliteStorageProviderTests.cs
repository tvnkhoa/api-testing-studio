using System.IO;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.Infrastructure.Tests;

public sealed class SqliteStorageProviderTests : TempDirectoryFixture
{
    private static readonly DateTimeOffset Timestamp = new(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);

    private readonly WorkspaceSession _session = new();
    private readonly SqliteStorageProvider _provider;

    public SqliteStorageProviderTests()
    {
        _provider = new SqliteStorageProvider(_session, NullLogger<SqliteStorageProvider>.Instance);
    }

    [Fact]
    public async Task Create_then_close_and_reopen_preserves_metadata()
    {
        var path = PathFor("sample.db");
        var workspace = NewWorkspace("Sample", "A description");

        var created = await _provider.CreateAsync(path, workspace);
        created.IsSuccess.Should().BeTrue();
        _provider.IsOpen.Should().BeTrue();

        await _provider.CloseAsync();
        _provider.IsOpen.Should().BeFalse();

        var opened = await _provider.OpenAsync(path);
        opened.IsSuccess.Should().BeTrue();

        var reloaded = await _provider.GetWorkspaceAsync();
        reloaded.Should().NotBeNull();
        reloaded!.Id.Should().Be(workspace.Id);
        reloaded.Name.Should().Be("Sample");
        reloaded.Description.Should().Be("A description");
        reloaded.SchemaVersion.Should().Be(Workspace.CurrentSchemaVersion);
    }

    [Fact]
    public async Task Reopening_applies_migrations_idempotently()
    {
        var path = PathFor("idempotent.db");
        await _provider.CreateAsync(path, NewWorkspace("Idem"));
        await _provider.CloseAsync();

        var first = await _provider.OpenAsync(path);
        await _provider.CloseAsync();
        var second = await _provider.OpenAsync(path);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        (await _provider.GetWorkspaceAsync()).Should().NotBeNull();
    }

    [Fact]
    public async Task Open_missing_file_returns_not_found()
    {
        var result = await _provider.OpenAsync(PathFor("nope.db"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workspace.not_found");
        _provider.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task Open_corrupt_file_returns_corrupt()
    {
        var path = PathFor("corrupt.db");
        await File.WriteAllBytesAsync(path, [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]);

        var result = await _provider.OpenAsync(path);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workspace.corrupt");
        _provider.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task Open_workspace_with_newer_schema_returns_schema_too_new()
    {
        var path = PathFor("future.db");
        await _provider.CreateAsync(path, NewWorkspace("Future"));

        var current = await _provider.GetWorkspaceAsync();
        await _provider.SaveWorkspaceAsync(current! with { SchemaVersion = Workspace.CurrentSchemaVersion + 1 });
        await _provider.CloseAsync();

        var result = await _provider.OpenAsync(path);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workspace.schema_too_new");
        _provider.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task Create_over_existing_file_returns_already_exists()
    {
        var path = PathFor("dup.db");
        await _provider.CreateAsync(path, NewWorkspace("First"));
        await _provider.CloseAsync();

        var result = await _provider.CreateAsync(path, NewWorkspace("Second"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workspace.already_exists");
    }

    [Fact]
    public async Task Delete_removes_the_file_and_closes_it()
    {
        var path = PathFor("todelete.db");
        await _provider.CreateAsync(path, NewWorkspace("Doomed"));

        var result = await _provider.DeleteAsync(path);

        result.IsSuccess.Should().BeTrue();
        File.Exists(path).Should().BeFalse();
        _provider.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_missing_file_returns_not_found()
    {
        var result = await _provider.DeleteAsync(PathFor("ghost.db"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workspace.not_found");
    }

    private static Workspace NewWorkspace(string name, string? description = null) => new()
    {
        Name = name,
        Description = description,
        SchemaVersion = Workspace.CurrentSchemaVersion,
        CreatedUtc = Timestamp,
        ModifiedUtc = Timestamp,
    };
}
