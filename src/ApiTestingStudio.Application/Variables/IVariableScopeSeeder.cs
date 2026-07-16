using ApiTestingStudio.Application.Workflows;

namespace ApiTestingStudio.Application.Variables;

/// <summary>
/// Loads persisted variables for the open workspace and the active environment and seeds them into
/// a workflow/execution context, honoring scope precedence (Global → Workspace → Environment, each
/// narrower scope overriding the broader). Narrower runtime scopes (Workflow / Local / node outputs)
/// are set later on the same context and therefore win. Secret values are decrypted at seed time.
/// </summary>
public interface IVariableScopeSeeder
{
    /// <summary>Seeds Global/Workspace/Environment variables into <paramref name="context"/>.</summary>
    Task SeedAsync(IWorkflowContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a fresh context seeded with the workspace + active-environment variables. Convenient
    /// for the API Runner, which has no pre-existing workflow context.
    /// </summary>
    Task<IWorkflowContext> BuildContextAsync(CancellationToken cancellationToken = default);
}
