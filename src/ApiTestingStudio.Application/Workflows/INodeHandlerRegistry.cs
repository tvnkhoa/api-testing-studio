using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Workflows;

/// <summary>
/// Resolves the <see cref="INodeHandler"/> for a node kind. Missing kinds come back as a typed
/// <see cref="Result"/> failure so the engine can record a node error instead of throwing.
/// </summary>
public interface INodeHandlerRegistry
{
    Result<INodeHandler> Resolve(WorkflowNodeKind kind);
}
