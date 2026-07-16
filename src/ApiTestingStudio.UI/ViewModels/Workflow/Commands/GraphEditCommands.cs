using System.Collections.ObjectModel;
using System.Windows;
using ApiTestingStudio.Application.Common;

namespace ApiTestingStudio.UI.ViewModels.Workflow.Commands;

/// <summary>Adds a node to the graph (undo removes it, along with any connections it gained — there are none at add time).</summary>
public sealed class AddNodeCommand : IUndoableCommand
{
    private readonly ObservableCollection<NodeViewModel> _nodes;
    private readonly NodeViewModel _node;
    private readonly Action _afterChange;

    public AddNodeCommand(ObservableCollection<NodeViewModel> nodes, NodeViewModel node, Action afterChange)
    {
        _nodes = nodes;
        _node = node;
        _afterChange = afterChange;
    }

    public string Description => "Add node";

    public void Execute()
    {
        _nodes.Add(_node);
        _afterChange();
    }

    public void Undo()
    {
        _nodes.Remove(_node);
        _afterChange();
    }
}

/// <summary>Removes a node and every connection touching it; undo restores both.</summary>
public sealed class RemoveNodeCommand : IUndoableCommand
{
    private readonly ObservableCollection<NodeViewModel> _nodes;
    private readonly ObservableCollection<ConnectionViewModel> _connections;
    private readonly NodeViewModel _node;
    private readonly Action _afterChange;
    private readonly List<ConnectionViewModel> _removedConnections = [];

    public RemoveNodeCommand(
        ObservableCollection<NodeViewModel> nodes,
        ObservableCollection<ConnectionViewModel> connections,
        NodeViewModel node,
        Action afterChange)
    {
        _nodes = nodes;
        _connections = connections;
        _node = node;
        _afterChange = afterChange;
    }

    public string Description => "Delete node";

    public void Execute()
    {
        _removedConnections.Clear();
        _removedConnections.AddRange(_connections.Where(c => c.Source.Node == _node || c.Target.Node == _node));
        foreach (var connection in _removedConnections)
        {
            _connections.Remove(connection);
        }

        _nodes.Remove(_node);
        _afterChange();
    }

    public void Undo()
    {
        _nodes.Add(_node);
        foreach (var connection in _removedConnections)
        {
            _connections.Add(connection);
        }

        _afterChange();
    }
}

/// <summary>Adds a connection between two ports; undo removes it.</summary>
public sealed class AddConnectionCommand : IUndoableCommand
{
    private readonly ObservableCollection<ConnectionViewModel> _connections;
    private readonly ConnectionViewModel _connection;
    private readonly Action _afterChange;

    public AddConnectionCommand(ObservableCollection<ConnectionViewModel> connections, ConnectionViewModel connection, Action afterChange)
    {
        _connections = connections;
        _connection = connection;
        _afterChange = afterChange;
    }

    public string Description => "Connect";

    public void Execute()
    {
        _connections.Add(_connection);
        _afterChange();
    }

    public void Undo()
    {
        _connections.Remove(_connection);
        _afterChange();
    }
}

/// <summary>Removes one or more connections (e.g. disconnecting a connector); undo restores them.</summary>
public sealed class RemoveConnectionsCommand : IUndoableCommand
{
    private readonly ObservableCollection<ConnectionViewModel> _connections;
    private readonly IReadOnlyList<ConnectionViewModel> _removed;
    private readonly Action _afterChange;

    public RemoveConnectionsCommand(ObservableCollection<ConnectionViewModel> connections, IReadOnlyList<ConnectionViewModel> removed, Action afterChange)
    {
        _connections = connections;
        _removed = removed;
        _afterChange = afterChange;
    }

    public string Description => "Disconnect";

    public void Execute()
    {
        foreach (var connection in _removed)
        {
            _connections.Remove(connection);
        }

        _afterChange();
    }

    public void Undo()
    {
        foreach (var connection in _removed)
        {
            _connections.Add(connection);
        }

        _afterChange();
    }
}

/// <summary>Moves one or more nodes to new locations; undo restores the originals. Locations are
/// already applied by the Nodify two-way binding when this is recorded, so <see cref="Execute"/> is
/// idempotent (it re-applies the new locations, which matters only on redo).</summary>
public sealed class MoveNodesCommand : IUndoableCommand
{
    private readonly IReadOnlyList<(NodeViewModel Node, Point From, Point To)> _moves;

    public MoveNodesCommand(IReadOnlyList<(NodeViewModel Node, Point From, Point To)> moves)
    {
        _moves = moves;
    }

    public string Description => "Move node";

    public void Execute()
    {
        foreach (var move in _moves)
        {
            move.Node.Location = move.To;
        }
    }

    public void Undo()
    {
        foreach (var move in _moves)
        {
            move.Node.Location = move.From;
        }
    }
}

/// <summary>Applies an edit to a node's title and/or typed config; undo restores the previous snapshot.</summary>
public sealed class EditNodeCommand : IUndoableCommand
{
    private readonly NodeViewModel _node;
    private readonly (string Title, object? Config) _before;
    private readonly (string Title, object? Config) _after;

    public EditNodeCommand(NodeViewModel node, (string Title, object? Config) before, (string Title, object? Config) after)
    {
        _node = node;
        _before = before;
        _after = after;
    }

    public string Description => "Edit node";

    public void Execute()
    {
        _node.Title = _after.Title;
        _node.Config = _after.Config;
    }

    public void Undo()
    {
        _node.Title = _before.Title;
        _node.Config = _before.Config;
    }
}
