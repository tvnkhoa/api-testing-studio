using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Variables;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Tests;

/// <summary>
/// Reversible in-memory <see cref="ISecretProtector"/> for tests: prefixes plaintext so ciphertext
/// is observably different yet round-trippable, without touching real crypto/DPAPI.
/// </summary>
internal sealed class FakeSecretProtector : ISecretProtector
{
    private const string Prefix = "enc:";

    public string Protect(string plaintext) => Prefix + plaintext;

    public string Unprotect(string protectedValue) =>
        protectedValue.StartsWith(Prefix, StringComparison.Ordinal)
            ? protectedValue[Prefix.Length..]
            : protectedValue;
}

/// <summary>In-memory <see cref="IProfileRepository"/>.</summary>
internal sealed class InMemoryProfileRepository : IProfileRepository
{
    public List<ProfileDefinition> Items { get; } = [];

    public Task<IReadOnlyList<ProfileDefinition>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ProfileDefinition>>(Items.Where(p => p.WorkspaceId == workspaceId).ToList());

    public Task<ProfileDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.FirstOrDefault(p => p.Id == id));

    public Task AddAsync(ProfileDefinition profile, CancellationToken cancellationToken = default)
    {
        Items.Add(profile);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ProfileDefinition profile, CancellationToken cancellationToken = default)
    {
        Items.RemoveAll(p => p.Id == profile.Id);
        Items.Add(profile);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Items.RemoveAll(p => p.Id == id);
        return Task.CompletedTask;
    }
}

/// <summary>In-memory <see cref="IEnvironmentRepository"/>.</summary>
internal sealed class InMemoryEnvironmentRepository : IEnvironmentRepository
{
    public List<EnvironmentDefinition> Items { get; } = [];

    public Task<IReadOnlyList<EnvironmentDefinition>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<EnvironmentDefinition>>(Items.Where(e => e.WorkspaceId == workspaceId).ToList());

    public Task<EnvironmentDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.FirstOrDefault(e => e.Id == id));

    public Task AddAsync(EnvironmentDefinition environment, CancellationToken cancellationToken = default)
    {
        Items.Add(environment);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(EnvironmentDefinition environment, CancellationToken cancellationToken = default)
    {
        Items.RemoveAll(e => e.Id == environment.Id);
        Items.Add(environment);
        return Task.CompletedTask;
    }

    public Task DeleteCascadeAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        Items.RemoveAll(e => e.Id == environmentId);
        return Task.CompletedTask;
    }
}

/// <summary>In-memory <see cref="IVariableRepository"/>.</summary>
internal sealed class InMemoryVariableRepository : IVariableRepository
{
    public List<Variable> Items { get; } = [];

    public Task<IReadOnlyList<Variable>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Variable>>(Items.Where(v => v.WorkspaceId == workspaceId).ToList());

    public Task<IReadOnlyList<Variable>> GetByScopeAsync(Guid workspaceId, VariableScope scope, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Variable>>(Items.Where(v => v.WorkspaceId == workspaceId && v.Scope == scope).ToList());

    public Task<IReadOnlyList<Variable>> GetByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Variable>>(Items.Where(v => v.EnvironmentId == environmentId).ToList());

    public Task<Variable?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.FirstOrDefault(v => v.Id == id));

    public Task AddAsync(Variable variable, CancellationToken cancellationToken = default)
    {
        Items.Add(variable);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Variable variable, CancellationToken cancellationToken = default)
    {
        Items.RemoveAll(v => v.Id == variable.Id);
        Items.Add(variable);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Items.RemoveAll(v => v.Id == id);
        return Task.CompletedTask;
    }
}

/// <summary>No-op <see cref="IVariableScopeSeeder"/> yielding an empty (or supplied) context.</summary>
internal sealed class FakeVariableScopeSeeder : IVariableScopeSeeder
{
    public Task SeedAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IWorkflowContext> BuildContextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IWorkflowContext>(new WorkflowContext());
}
