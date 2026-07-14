using ApiTestingStudio.Application.Workspaces;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class WorkspaceServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeStorageProvider _storage = new();
    private readonly FakeRecentWorkspacesService _recent = new();
    private readonly WorkspaceService _service;

    public WorkspaceServiceTests()
    {
        _service = new WorkspaceService(_storage, _recent, new FixedClock(Now));
    }

    [Fact]
    public async Task Create_with_blank_name_fails_without_touching_storage()
    {
        var result = await _service.CreateAsync("ws.db", "   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workspace.invalid_name");
        _storage.LastCreateLocation.Should().BeNull();
    }

    [Fact]
    public async Task Create_stamps_timestamps_trims_name_and_records_recent()
    {
        var result = await _service.CreateAsync("ws.db", "  My Workspace  ", "notes");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("My Workspace");
        result.Value.Description.Should().Be("notes");
        result.Value.SchemaVersion.Should().Be(Workspace.CurrentSchemaVersion);
        result.Value.CreatedUtc.Should().Be(Now);
        result.Value.ModifiedUtc.Should().Be(Now);

        _storage.LastCreatedMetadata.Should().NotBeNull();
        _recent.Touched.Should().ContainSingle();
        _recent.Touched[0].Location.Should().Be("ws.db");
        _recent.Touched[0].Name.Should().Be("My Workspace");
    }

    [Fact]
    public async Task Create_closes_the_currently_open_workspace_first()
    {
        _storage.IsOpen = true;

        await _service.CreateAsync("new.db", "New");

        _storage.CloseCount.Should().Be(1);
    }

    [Fact]
    public async Task Create_failure_propagates_error_and_skips_recent()
    {
        _storage.CreateResult = Result.Failure(WorkspaceErrors.AlreadyExists("ws.db"));

        var result = await _service.CreateAsync("ws.db", "Dup");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workspace.already_exists");
        _recent.Touched.Should().BeEmpty();
    }

    [Fact]
    public async Task Open_failure_propagates_error_and_skips_recent()
    {
        _storage.OpenResult = Result.Failure(WorkspaceErrors.NotFound("missing.db"));

        var result = await _service.OpenAsync("missing.db");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workspace.not_found");
        _recent.Touched.Should().BeEmpty();
    }

    [Fact]
    public async Task Open_success_returns_workspace_and_records_recent()
    {
        _storage.WorkspaceToReturn = new Workspace
        {
            Name = "Opened",
            SchemaVersion = Workspace.CurrentSchemaVersion,
            CreatedUtc = Now,
            ModifiedUtc = Now,
        };

        var result = await _service.OpenAsync("path.db");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Opened");
        _recent.Touched.Should().ContainSingle();
        _recent.Touched[0].Location.Should().Be("path.db");
        _recent.Touched[0].Name.Should().Be("Opened");
    }

    [Fact]
    public async Task Close_when_none_open_returns_none_open()
    {
        _storage.IsOpen = false;

        var result = await _service.CloseAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workspace.none_open");
        _storage.CloseCount.Should().Be(0);
    }

    [Fact]
    public async Task Close_when_open_succeeds()
    {
        _storage.IsOpen = true;

        var result = await _service.CloseAsync();

        result.IsSuccess.Should().BeTrue();
        _storage.CloseCount.Should().Be(1);
    }

    [Fact]
    public async Task Delete_success_removes_from_recent()
    {
        var result = await _service.DeleteAsync("gone.db");

        result.IsSuccess.Should().BeTrue();
        _recent.Removed.Should().ContainSingle().Which.Should().Be("gone.db");
    }

    [Fact]
    public async Task Delete_failure_does_not_touch_recent()
    {
        _storage.DeleteResult = Result.Failure(WorkspaceErrors.NotFound("gone.db"));

        var result = await _service.DeleteAsync("gone.db");

        result.IsFailure.Should().BeTrue();
        _recent.Removed.Should().BeEmpty();
    }
}
