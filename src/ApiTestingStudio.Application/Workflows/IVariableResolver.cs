namespace ApiTestingStudio.Application.Workflows;

/// <summary>
/// Resolves <c>{{token}}</c> expressions in a template against a workflow context. Supports
/// interpolation only (no operators): <c>{{vars.name}}</c> reads a variable, <c>{{Node.key}}</c>
/// reads a node output, and trailing dotted segments walk into a JSON value
/// (e.g. <c>{{Login.body.data.token}}</c>).
/// </summary>
public interface IVariableResolver
{
    /// <summary>Replaces every token in <paramref name="template"/>; unresolved tokens become empty.</summary>
    string Resolve(string? template, IWorkflowContext context);

    /// <summary>
    /// Like <see cref="Resolve(string?, IWorkflowContext)"/>, but additionally appends the raw
    /// expression of every token that could not be resolved to <paramref name="unresolvedTokens"/>
    /// (deduplicated by the caller if desired). This lets callers warn on a hollow substitution
    /// instead of failing silently — the core trust fix for workflows/requests that reference
    /// variables the active environment does not define.
    /// </summary>
    string Resolve(string? template, IWorkflowContext context, ICollection<string> unresolvedTokens);

    /// <summary>Resolves a single token expression (without the braces); returns false if unresolved.</summary>
    bool TryResolveToken(string token, IWorkflowContext context, out string? value);
}
