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

    /// <summary>Resolves a single token expression (without the braces); returns false if unresolved.</summary>
    bool TryResolveToken(string token, IWorkflowContext context, out string? value);
}
