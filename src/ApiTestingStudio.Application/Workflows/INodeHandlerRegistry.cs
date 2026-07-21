using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Workflows;

/// <summary>
/// Resolves the <see cref="INodeHandler"/> for a node kind. Missing kinds come back as a typed
/// <see cref="Result"/> failure so the engine can record a node error instead of throwing.
/// </summary>
public interface INodeHandlerRegistry
{
    /// <summary>
    /// The node kinds that have a registered handler and can therefore be executed. Callers (e.g. the
    /// designer palette) should offer only these kinds so a user cannot place a node the engine has no
    /// handler for.
    /// </summary>
    IReadOnlyCollection<WorkflowNodeKind> SupportedKinds { get; }

    Result<INodeHandler> Resolve(WorkflowNodeKind kind);
}
