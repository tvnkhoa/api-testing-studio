using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace ApiTestingStudio.Application.Workflows;

/// <summary>
/// Default <see cref="IWorkflowContext"/> backed by concurrent dictionaries so parallel branches can
/// read/write safely. Variable and node names are matched case-insensitively.
/// </summary>
public sealed class WorkflowContext : IWorkflowContext
{
    private readonly ConcurrentDictionary<string, string?> _variables =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _outputs =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string?> Variables => _variables;

    public void SetVariable(string name, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _variables[name] = value;
    }

    public bool TryGetVariable(string name, out string? value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            value = null;
            return false;
        }

        return _variables.TryGetValue(name, out value);
    }

    public void SetNodeOutputs(string nodeName, IReadOnlyDictionary<string, string> outputs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);
        ArgumentNullException.ThrowIfNull(outputs);
        _outputs[nodeName] = outputs;
    }

    public bool TryGetNodeOutputs(string nodeName, [MaybeNullWhen(false)] out IReadOnlyDictionary<string, string> outputs)
    {
        if (string.IsNullOrWhiteSpace(nodeName))
        {
            outputs = null;
            return false;
        }

        return _outputs.TryGetValue(nodeName, out outputs);
    }
}
