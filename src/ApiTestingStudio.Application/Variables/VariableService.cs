using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Profiles;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Variables;

/// <summary>
/// Variable CRUD over <see cref="IVariableRepository"/>. Encrypts the value of secret variables via
/// <see cref="ISecretProtector"/>. Workspace scope comes from <see cref="IWorkspaceSession"/>.
/// </summary>
public sealed class VariableService : IVariableService
{
    private readonly IVariableRepository _variables;
    private readonly ISecretProtector _protector;
    private readonly IWorkspaceSession _session;

    public VariableService(
        IVariableRepository variables,
        ISecretProtector protector,
        IWorkspaceSession session)
    {
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(protector);
        ArgumentNullException.ThrowIfNull(session);
        _variables = variables;
        _protector = protector;
        _session = session;
    }

    public async Task<Result<IReadOnlyList<Variable>>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (_session.Current is not { } workspace)
        {
            return Result.Failure<IReadOnlyList<Variable>>(IdentityErrors.NoWorkspaceOpen);
        }

        var variables = await _variables.GetByWorkspaceAsync(workspace.Id, cancellationToken).ConfigureAwait(false);
        return Result.Success(variables);
    }

    public async Task<Result<Variable>> CreateAsync(VariableDraft draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (_session.Current is not { } workspace)
        {
            return Result.Failure<Variable>(IdentityErrors.NoWorkspaceOpen);
        }

        var validation = Validate(draft);
        if (validation.IsFailure)
        {
            return Result.Failure<Variable>(validation.Error);
        }

        var variable = new Variable
        {
            WorkspaceId = workspace.Id,
            Scope = draft.Scope,
            EnvironmentId = draft.Scope == VariableScope.Environment ? draft.EnvironmentId : null,
            Key = draft.Key.Trim(),
            Value = Encode(draft.Value, draft.IsSecret),
            IsSecret = draft.IsSecret,
        };

        await _variables.AddAsync(variable, cancellationToken).ConfigureAwait(false);
        return Result.Success(variable);
    }

    public async Task<Result<Variable>> UpdateAsync(Guid id, VariableDraft draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (!_session.IsOpen)
        {
            return Result.Failure<Variable>(IdentityErrors.NoWorkspaceOpen);
        }

        var validation = Validate(draft);
        if (validation.IsFailure)
        {
            return Result.Failure<Variable>(validation.Error);
        }

        var existing = await _variables.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return Result.Failure<Variable>(IdentityErrors.VariableNotFound(id));
        }

        // A null value on a secret variable keeps the stored ciphertext.
        var value = draft.IsSecret && draft.Value is null
            ? existing.Value
            : Encode(draft.Value, draft.IsSecret);

        var updated = existing with
        {
            Scope = draft.Scope,
            EnvironmentId = draft.Scope == VariableScope.Environment ? draft.EnvironmentId : null,
            Key = draft.Key.Trim(),
            Value = value,
            IsSecret = draft.IsSecret,
        };

        await _variables.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);
        return Result.Success(updated);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_session.IsOpen)
        {
            return Result.Failure(IdentityErrors.NoWorkspaceOpen);
        }

        var existing = await _variables.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return Result.Failure(IdentityErrors.VariableNotFound(id));
        }

        await _variables.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static Result Validate(VariableDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.Key))
        {
            return Result.Failure(IdentityErrors.KeyRequired);
        }

        return draft.Scope == VariableScope.Environment && draft.EnvironmentId is null
            ? Result.Failure(IdentityErrors.EnvironmentRequired)
            : Result.Success();
    }

    private string? Encode(string? value, bool isSecret)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return isSecret ? _protector.Protect(value) : value;
    }
}
