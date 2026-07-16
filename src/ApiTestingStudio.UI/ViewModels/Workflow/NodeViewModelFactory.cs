using System.Windows;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.UI.ViewModels.Workflow;

/// <inheritdoc />
public sealed class NodeViewModelFactory : INodeViewModelFactory
{
    public NodeViewModel Create(WorkflowNodeKind kind, Point location)
    {
        var node = new NodeViewModel(Guid.NewGuid(), kind, kind.ToString())
        {
            Location = location,
            Config = DefaultConfig(kind),
        };
        PopulatePorts(node);
        return node;
    }

    public NodeViewModel FromDomain(WorkflowNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var viewModel = new NodeViewModel(node.Id, node.Kind, node.Name)
        {
            Location = new Point(node.PositionX, node.PositionY),
            Width = node.Width,
            Height = node.Height,
            Color = node.Color,
            Config = DeserializeConfig(node.Kind, node.Config),
        };
        PopulatePorts(viewModel);
        return viewModel;
    }

    private static void PopulatePorts(NodeViewModel node)
    {
        foreach (var key in NodePortCatalog.InputPorts(node.Kind))
        {
            node.Input.Add(new PortViewModel(node, key, PortDirection.Input));
        }

        foreach (var key in NodePortCatalog.OutputPorts(node.Kind))
        {
            node.Output.Add(new PortViewModel(node, key, PortDirection.Output));
        }
    }

    /// <summary>A sensible empty config record per kind (kinds without config get null).</summary>
    private static object? DefaultConfig(WorkflowNodeKind kind) => kind switch
    {
        WorkflowNodeKind.Api => new RequestNodeConfig(),
        WorkflowNodeKind.Condition => new ConditionNodeConfig(),
        WorkflowNodeKind.Loop => new LoopNodeConfig(),
        WorkflowNodeKind.Parallel => new ParallelNodeConfig(),
        WorkflowNodeKind.Delay => new DelayNodeConfig(),
        _ => null,
    };

    /// <summary>Deserializes the stored JSON config to its typed record, falling back to the default.</summary>
    private static object? DeserializeConfig(WorkflowNodeKind kind, string? json) => kind switch
    {
        WorkflowNodeKind.Api => NodeConfigSerializer.Deserialize<RequestNodeConfig>(json) ?? new RequestNodeConfig(),
        WorkflowNodeKind.Condition => NodeConfigSerializer.Deserialize<ConditionNodeConfig>(json) ?? new ConditionNodeConfig(),
        WorkflowNodeKind.Loop => NodeConfigSerializer.Deserialize<LoopNodeConfig>(json) ?? new LoopNodeConfig(),
        WorkflowNodeKind.Parallel => NodeConfigSerializer.Deserialize<ParallelNodeConfig>(json) ?? new ParallelNodeConfig(),
        WorkflowNodeKind.Delay => NodeConfigSerializer.Deserialize<DelayNodeConfig>(json) ?? new DelayNodeConfig(),
        _ => null,
    };
}
