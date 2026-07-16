using System.Collections.ObjectModel;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using DomainWorkflow = ApiTestingStudio.Domain.Entities.Workflow;

namespace ApiTestingStudio.UI.ViewModels.Workflow;

/// <summary>
/// Two-way translation between the domain <see cref="Workflow"/> graph the engine executes and the
/// designer's view-model graph. Lives in UI (it references UI view-model types); it uses the
/// Application port catalog through <see cref="INodeViewModelFactory"/> so the designer can only
/// produce edges the engine knows how to traverse — the domain graph is the single source of truth.
/// </summary>
public sealed class GraphMapper
{
    private readonly INodeViewModelFactory _factory;

    public GraphMapper(INodeViewModelFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>Materializes a domain workflow into node + connection view models.</summary>
    public (IReadOnlyList<NodeViewModel> Nodes, IReadOnlyList<ConnectionViewModel> Connections) ToViewModel(DomainWorkflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        var nodes = workflow.Nodes.Select(_factory.FromDomain).ToList();
        var byId = nodes.ToDictionary(n => n.Id);

        var connections = new List<ConnectionViewModel>();
        foreach (var edge in workflow.Edges)
        {
            if (!byId.TryGetValue(edge.SourceNodeId, out var source)
                || !byId.TryGetValue(edge.TargetNodeId, out var target))
            {
                continue;
            }

            var sourcePort = FindPort(source.Output, edge.SourcePort);
            var targetPort = FindPort(target.Input, edge.TargetPort);
            if (sourcePort is null || targetPort is null)
            {
                continue;
            }

            connections.Add(new ConnectionViewModel(sourcePort, targetPort, edge.Mapping));
        }

        return (nodes, connections);
    }

    /// <summary>Builds a runnable/persistable domain workflow from the current designer state.</summary>
    public static DomainWorkflow ToDomain(
        Guid id,
        Guid workspaceId,
        string name,
        string? description,
        IEnumerable<NodeViewModel> nodes,
        IEnumerable<ConnectionViewModel> connections)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(connections);

        var domainNodes = nodes.Select(n => new WorkflowNode
        {
            Id = n.Id,
            WorkflowId = id,
            Kind = n.Kind,
            Name = n.Title,
            PositionX = n.Location.X,
            PositionY = n.Location.Y,
            Width = n.Width,
            Height = n.Height,
            Color = n.Color,
            Config = NodeConfigSerializer.SerializeObject(n.Config),
        }).ToList();

        var domainEdges = connections.Select(c => new WorkflowEdge
        {
            WorkflowId = id,
            SourceNodeId = c.Source.Node.Id,
            TargetNodeId = c.Target.Node.Id,
            SourcePort = c.Source.Key,
            TargetPort = c.Target.Key,
            Mapping = c.Mapping,
        }).ToList();

        return new DomainWorkflow
        {
            Id = id,
            WorkspaceId = workspaceId,
            Name = name,
            Description = description,
            Nodes = domainNodes,
            Edges = domainEdges,
        };
    }

    private static PortViewModel? FindPort(Collection<PortViewModel> ports, string? key) =>
        ports.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase))
        ?? ports.FirstOrDefault();
}
