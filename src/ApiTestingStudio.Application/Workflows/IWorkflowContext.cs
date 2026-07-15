using System.Diagnostics.CodeAnalysis;

namespace ApiTestingStudio.Application.Workflows;

/// <summary>
/// Mutable, thread-safe state shared across the nodes of one workflow run: free-form variables and
/// per-node output bags. Thread-safety matters because a Parallel node runs branches concurrently;
/// branches write outputs under their own distinct node names, so they never collide.
/// </summary>
public interface IWorkflowContext
{
    /// <summary>All variables currently set, keyed case-insensitively.</summary>
    IReadOnlyDictionary<string, string?> Variables { get; }

    /// <summary>Sets (or overwrites) a variable.</summary>
    void SetVariable(string name, string? value);

    /// <summary>Reads a variable; returns false when it is not set.</summary>
    bool TryGetVariable(string name, out string? value);

    /// <summary>Publishes the outputs of a node, keyed by the node's name.</summary>
    void SetNodeOutputs(string nodeName, IReadOnlyDictionary<string, string> outputs);

    /// <summary>Reads a node's published outputs; returns false when the node has not run.</summary>
    bool TryGetNodeOutputs(string nodeName, [MaybeNullWhen(false)] out IReadOnlyDictionary<string, string> outputs);
}
