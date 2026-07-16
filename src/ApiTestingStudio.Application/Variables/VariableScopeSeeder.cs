using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Environments;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Application.Workflows;

namespace ApiTestingStudio.Application.Variables;

/// <summary>
/// Default <see cref="IVariableScopeSeeder"/>: reads the workspace's variables plus the active
/// environment's variables and applies them to a context in precedence order.
/// </summary>
public sealed class VariableScopeSeeder : IVariableScopeSeeder
{
    private readonly IVariableRepository _variables;
    private readonly IEnvironmentService _environments;
    private readonly ISecretProtector _protector;
    private readonly IWorkspaceSession _session;

    public VariableScopeSeeder(
        IVariableRepository variables,
        IEnvironmentService environments,
        ISecretProtector protector,
        IWorkspaceSession session)
    {
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(environments);
        ArgumentNullException.ThrowIfNull(protector);
        ArgumentNullException.ThrowIfNull(session);
        _variables = variables;
        _environments = environments;
        _protector = protector;
        _session = session;
    }

    public async Task SeedAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_session.Current is not { } workspace)
        {
            return;
        }

        var all = await _variables.GetByWorkspaceAsync(workspace.Id, cancellationToken).ConfigureAwait(false);
        var activeEnvironmentId = await _environments.GetActiveIdAsync(cancellationToken).ConfigureAwait(false);

        // Broadest first so narrower scopes overwrite: Global -> Workspace -> Environment (active).
        Apply(context, all.Where(v => v.Scope == VariableScope.Global));
        Apply(context, all.Where(v => v.Scope == VariableScope.Workspace));

        if (activeEnvironmentId is { } environmentId)
        {
            Apply(context, all.Where(v =>
                v.Scope == VariableScope.Environment && v.EnvironmentId == environmentId));
        }
    }

    public async Task<IWorkflowContext> BuildContextAsync(CancellationToken cancellationToken = default)
    {
        var context = new WorkflowContext();
        await SeedAsync(context, cancellationToken).ConfigureAwait(false);
        return context;
    }

    private void Apply(IWorkflowContext context, IEnumerable<Variable> variables)
    {
        foreach (var variable in variables)
        {
            context.SetVariable(variable.Key, Decode(variable));
        }
    }

    private string? Decode(Variable variable)
    {
        if (!variable.IsSecret || string.IsNullOrEmpty(variable.Value))
        {
            return variable.Value;
        }

        return _protector.Unprotect(variable.Value);
    }
}
