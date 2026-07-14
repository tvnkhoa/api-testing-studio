using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Plugin.Abstractions.Storage;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Workspaces;

/// <summary>
/// Default <see cref="IWorkspaceService"/>. Orchestrates the workspace lifecycle over the
/// storage provider, stamps timestamps via <see cref="IClock"/>, and keeps the recent-workspaces
/// list up to date. Holds no persistence types itself — all storage work goes through the provider.
/// </summary>
public sealed class WorkspaceService : IWorkspaceService
{
    private readonly IStorageProvider _storage;
    private readonly IRecentWorkspacesService _recent;
    private readonly IClock _clock;

    public WorkspaceService(IStorageProvider storage, IRecentWorkspacesService recent, IClock clock)
    {
        _storage = storage;
        _recent = recent;
        _clock = clock;
    }

    public async Task<Result<Workspace>> CreateAsync(
        string location,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return Result.Failure<Workspace>(WorkspaceErrors.NotFound(location ?? string.Empty));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Workspace>(WorkspaceErrors.InvalidName);
        }

        await CloseIfOpenAsync(cancellationToken).ConfigureAwait(false);

        var now = _clock.UtcNow;
        var workspace = new Workspace
        {
            Name = name.Trim(),
            Description = description,
            SchemaVersion = Workspace.CurrentSchemaVersion,
            CreatedUtc = now,
            ModifiedUtc = now,
        };

        var created = await _storage.CreateAsync(location, workspace, cancellationToken).ConfigureAwait(false);
        if (created.IsFailure)
        {
            return Result.Failure<Workspace>(created.Error);
        }

        await TouchRecentAsync(location, workspace.Name, now, cancellationToken).ConfigureAwait(false);
        return Result.Success(workspace);
    }

    public async Task<Result<Workspace>> OpenAsync(string location, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return Result.Failure<Workspace>(WorkspaceErrors.NotFound(location ?? string.Empty));
        }

        await CloseIfOpenAsync(cancellationToken).ConfigureAwait(false);

        var opened = await _storage.OpenAsync(location, cancellationToken).ConfigureAwait(false);
        if (opened.IsFailure)
        {
            return Result.Failure<Workspace>(opened.Error);
        }

        var workspace = await _storage.GetWorkspaceAsync(cancellationToken).ConfigureAwait(false);
        if (workspace is null)
        {
            await _storage.CloseAsync(cancellationToken).ConfigureAwait(false);
            return Result.Failure<Workspace>(WorkspaceErrors.Corrupt(location));
        }

        await TouchRecentAsync(location, workspace.Name, _clock.UtcNow, cancellationToken).ConfigureAwait(false);
        return Result.Success(workspace);
    }

    public async Task<Result> CloseAsync(CancellationToken cancellationToken = default)
    {
        if (!_storage.IsOpen)
        {
            return Result.Failure(WorkspaceErrors.NoneOpen);
        }

        await _storage.CloseAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string location, CancellationToken cancellationToken = default)
    {
        var deleted = await _storage.DeleteAsync(location, cancellationToken).ConfigureAwait(false);
        if (deleted.IsFailure)
        {
            return deleted;
        }

        await _recent.RemoveAsync(location, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task CloseIfOpenAsync(CancellationToken cancellationToken)
    {
        if (_storage.IsOpen)
        {
            await _storage.CloseAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task TouchRecentAsync(string location, string name, DateTimeOffset when, CancellationToken cancellationToken)
    {
        var entry = new RecentWorkspaceEntry
        {
            Location = location,
            Name = name,
            LastOpenedUtc = when,
        };

        await _recent.AddOrTouchAsync(entry, cancellationToken).ConfigureAwait(false);
    }
}
