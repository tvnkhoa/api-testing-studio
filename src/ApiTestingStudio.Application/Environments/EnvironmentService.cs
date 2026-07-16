using System.Globalization;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Profiles;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Environments;

/// <summary>
/// Environment CRUD over <see cref="IEnvironmentRepository"/>. The active-environment id is stored
/// as a per-workspace setting via <see cref="IWorkspaceSettingRepository"/>.
/// </summary>
public sealed class EnvironmentService : IEnvironmentService
{
    /// <summary>Settings key holding the active environment id for the open workspace.</summary>
    public const string ActiveEnvironmentSettingKey = "active-environment-id";

    private readonly IEnvironmentRepository _environments;
    private readonly IWorkspaceSettingRepository _settings;
    private readonly IWorkspaceSession _session;

    public EnvironmentService(
        IEnvironmentRepository environments,
        IWorkspaceSettingRepository settings,
        IWorkspaceSession session)
    {
        ArgumentNullException.ThrowIfNull(environments);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(session);
        _environments = environments;
        _settings = settings;
        _session = session;
    }

    public async Task<Result<IReadOnlyList<EnvironmentDefinition>>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (_session.Current is not { } workspace)
        {
            return Result.Failure<IReadOnlyList<EnvironmentDefinition>>(IdentityErrors.NoWorkspaceOpen);
        }

        var environments = await _environments.GetByWorkspaceAsync(workspace.Id, cancellationToken).ConfigureAwait(false);
        return Result.Success(environments);
    }

    public async Task<Result<EnvironmentDefinition>> CreateAsync(string name, EnvironmentKind kind, CancellationToken cancellationToken = default)
    {
        if (_session.Current is not { } workspace)
        {
            return Result.Failure<EnvironmentDefinition>(IdentityErrors.NoWorkspaceOpen);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<EnvironmentDefinition>(IdentityErrors.NameRequired);
        }

        var environment = new EnvironmentDefinition
        {
            WorkspaceId = workspace.Id,
            Name = name.Trim(),
            Kind = kind,
        };

        await _environments.AddAsync(environment, cancellationToken).ConfigureAwait(false);
        return Result.Success(environment);
    }

    public async Task<Result<EnvironmentDefinition>> UpdateAsync(Guid id, string name, EnvironmentKind kind, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure<EnvironmentDefinition>(IdentityErrors.NoWorkspaceOpen);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<EnvironmentDefinition>(IdentityErrors.NameRequired);
        }

        var existing = await _environments.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return Result.Failure<EnvironmentDefinition>(IdentityErrors.EnvironmentNotFound(id));
        }

        var updated = existing with { Name = name.Trim(), Kind = kind };
        await _environments.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);
        return Result.Success(updated);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure(IdentityErrors.NoWorkspaceOpen);
        }

        var existing = await _environments.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return Result.Failure(IdentityErrors.EnvironmentNotFound(id));
        }

        await _environments.DeleteCascadeAsync(id, cancellationToken).ConfigureAwait(false);

        // Clear the active selection if it pointed at the deleted environment.
        var active = await GetActiveIdAsync(cancellationToken).ConfigureAwait(false);
        if (active == id)
        {
            await _settings.SetAsync(ActiveEnvironmentSettingKey, null, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    public async Task<Guid?> GetActiveIdAsync(CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return null;
        }

        var raw = await _settings.GetAsync(ActiveEnvironmentSettingKey, cancellationToken).ConfigureAwait(false);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public async Task<Result> SetActiveAsync(Guid? environmentId, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure(IdentityErrors.NoWorkspaceOpen);
        }

        if (environmentId is { } id)
        {
            var environment = await _environments.GetAsync(id, cancellationToken).ConfigureAwait(false);
            if (environment is null)
            {
                return Result.Failure(IdentityErrors.EnvironmentNotFound(id));
            }
        }

        var value = environmentId?.ToString(null, CultureInfo.InvariantCulture);
        await _settings.SetAsync(ActiveEnvironmentSettingKey, value, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
