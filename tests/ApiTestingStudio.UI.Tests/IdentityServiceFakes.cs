using ApiTestingStudio.Application.Environments;
using ApiTestingStudio.Application.Profiles;
using ApiTestingStudio.Application.Variables;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.UI.Tests;

/// <summary>Empty <see cref="IProfileService"/> for shell/panel tests.</summary>
internal sealed class FakeProfileService : IProfileService
{
    public Task<Result<IReadOnlyList<ProfileDefinition>>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success<IReadOnlyList<ProfileDefinition>>([]));

    public Task<Result<ProfileDefinition>> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Failure<ProfileDefinition>(IdentityErrors.ProfileNotFound(id)));

    public Task<Result<ProfileDefinition>> CreateAsync(ProfileDraft draft, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success(new ProfileDefinition { Name = draft.Name }));

    public Task<Result<ProfileDefinition>> UpdateAsync(Guid id, ProfileDraft draft, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success(new ProfileDefinition { Id = id, Name = draft.Name }));

    public Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());

    public Task<Guid?> GetActiveIdAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<Guid?>(null);

    public Task<Result> SetActiveAsync(Guid? profileId, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());
}

/// <summary>Empty <see cref="IEnvironmentService"/> for shell/panel tests.</summary>
internal sealed class FakeEnvironmentService : IEnvironmentService
{
    public Task<Result<IReadOnlyList<EnvironmentDefinition>>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success<IReadOnlyList<EnvironmentDefinition>>([]));

    public Task<Result<EnvironmentDefinition>> CreateAsync(string name, EnvironmentKind kind, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success(new EnvironmentDefinition { Name = name, Kind = kind }));

    public Task<Result<EnvironmentDefinition>> UpdateAsync(Guid id, string name, EnvironmentKind kind, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success(new EnvironmentDefinition { Id = id, Name = name, Kind = kind }));

    public Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());

    public Task<Guid?> GetActiveIdAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<Guid?>(null);

    public Task<Result> SetActiveAsync(Guid? environmentId, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());
}

/// <summary>Empty <see cref="IVariableService"/> for shell/panel tests.</summary>
internal sealed class FakeVariableService : IVariableService
{
    public Task<Result<IReadOnlyList<Variable>>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success<IReadOnlyList<Variable>>([]));

    public Task<Result<Variable>> CreateAsync(VariableDraft draft, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success(new Variable { Key = draft.Key }));

    public Task<Result<Variable>> UpdateAsync(Guid id, VariableDraft draft, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success(new Variable { Id = id, Key = draft.Key }));

    public Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());
}
