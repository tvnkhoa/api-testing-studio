using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Workflows;

/// <summary>
/// A proposed connection between two node ports, evaluated by <see cref="IConnectorValidator"/> before
/// the designer commits it to the graph.
/// </summary>
public sealed record ConnectionRequest
{
    public required Guid SourceNodeId { get; init; }

    public required WorkflowNodeKind SourceKind { get; init; }

    public required string SourcePort { get; init; }

    public required Guid TargetNodeId { get; init; }

    public required WorkflowNodeKind TargetKind { get; init; }

    public required string TargetPort { get; init; }

    /// <summary>The edges already present in the graph, used to reject duplicates.</summary>
    public IReadOnlyList<WorkflowEdge> ExistingEdges { get; init; } = [];
}

/// <summary>
/// Pure rule check deciding whether a proposed edge is legal: rejects self-connections, unknown
/// ports (which also catches reversed/incompatible directions, since a source must be an output port
/// and a target an input port), and exact duplicates. UI-independent and unit-tested.
/// </summary>
public interface IConnectorValidator
{
    Result Validate(ConnectionRequest request);
}
