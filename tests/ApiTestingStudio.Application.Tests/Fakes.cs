using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Workspaces;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Plugin.Abstractions.Storage;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Tests;

/// <summary>Configurable in-memory <see cref="IStorageProvider"/> for exercising WorkspaceService.</summary>
internal sealed class FakeStorageProvider : IStorageProvider
{
    public string ProviderName => "fake";

    public bool IsOpen { get; set; }

    public Result CreateResult { get; set; } = Result.Success();

    public Result OpenResult { get; set; } = Result.Success();

    public Result DeleteResult { get; set; } = Result.Success();

    public Workspace? WorkspaceToReturn { get; set; }

    public string? LastCreateLocation { get; private set; }

    public Workspace? LastCreatedMetadata { get; private set; }

    public string? LastOpenLocation { get; private set; }

    public string? LastDeleteLocation { get; private set; }

    public int CloseCount { get; private set; }

    public Task<Result> CreateAsync(string location, Workspace metadata, CancellationToken cancellationToken = default)
    {
        LastCreateLocation = location;
        LastCreatedMetadata = metadata;

        if (CreateResult.IsSuccess)
        {
            IsOpen = true;
            WorkspaceToReturn = metadata;
        }

        return Task.FromResult(CreateResult);
    }

    public Task<Result> OpenAsync(string location, CancellationToken cancellationToken = default)
    {
        LastOpenLocation = location;

        if (OpenResult.IsSuccess)
        {
            IsOpen = true;
        }

        return Task.FromResult(OpenResult);
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        CloseCount++;
        IsOpen = false;
        return Task.CompletedTask;
    }

    public Task<Result> DeleteAsync(string location, CancellationToken cancellationToken = default)
    {
        LastDeleteLocation = location;
        return Task.FromResult(DeleteResult);
    }

    public Task<Workspace?> GetWorkspaceAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(WorkspaceToReturn);

    public Task SaveWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default)
    {
        WorkspaceToReturn = workspace;
        return Task.CompletedTask;
    }
}

/// <summary>Records interactions with the MRU list.</summary>
internal sealed class FakeRecentWorkspacesService : IRecentWorkspacesService
{
    public List<RecentWorkspaceEntry> Touched { get; } = [];

    public List<string> Removed { get; } = [];

    public Task<IReadOnlyList<RecentWorkspaceEntry>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RecentWorkspaceEntry>>(Touched);

    public Task AddOrTouchAsync(RecentWorkspaceEntry entry, CancellationToken cancellationToken = default)
    {
        Touched.Insert(0, entry);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string location, CancellationToken cancellationToken = default)
    {
        Removed.Add(location);
        return Task.CompletedTask;
    }
}

/// <summary>Deterministic <see cref="IClock"/>.</summary>
internal sealed class FixedClock : IClock
{
    public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;

    public DateTimeOffset UtcNow { get; }
}
