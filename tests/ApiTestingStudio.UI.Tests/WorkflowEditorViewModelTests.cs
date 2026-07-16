using System.Windows;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Common;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.UI.ViewModels.Workflow;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ApiTestingStudio.UI.Tests;

public sealed class WorkflowEditorViewModelTests
{
    private readonly Guid _workflowId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly InMemoryWorkflowRepository _repository = new();
    private readonly RecordingEngine _engine = new();
    private readonly FakeStatusBarService _status = new();

    private WorkflowEditorViewModel CreateEditor()
    {
        _repository.Seed(new Workflow { Id = _workflowId, WorkspaceId = _workspaceId, Name = "W", Nodes = [], Edges = [] });
        var factory = new NodeViewModelFactory();
        return new WorkflowEditorViewModel(
            _workflowId,
            "W",
            _repository,
            _engine,
            new UndoRedoService(),
            new ConnectorValidator(),
            factory,
            new GraphMapper(factory),
            _status,
            NullLogger<WorkflowEditorViewModel>.Instance);
    }

    [Fact]
    public async Task AddNode_then_undo_then_redo()
    {
        var editor = CreateEditor();
        await editor.LoadAsync();

        editor.AddNodeAt(WorkflowNodeKind.Api, new Point(10, 10));
        editor.Nodes.Should().ContainSingle();

        editor.UndoCommand.Execute(null);
        editor.Nodes.Should().BeEmpty();

        editor.RedoCommand.Execute(null);
        editor.Nodes.Should().ContainSingle();
    }

    [Fact]
    public void Connect_valid_output_to_input_adds_a_connection()
    {
        var editor = CreateEditor();
        editor.AddNodeAt(WorkflowNodeKind.Api, new Point(0, 0));
        editor.AddNodeAt(WorkflowNodeKind.Condition, new Point(200, 0));
        var source = editor.Nodes[0].Output[0];
        var target = editor.Nodes[1].Input[0];

        editor.ConnectCommand.Execute(Tuple.Create<object, object>(source, target));

        editor.Connections.Should().ContainSingle();
        editor.Connections[0].Source.Should().Be(source);
        editor.Connections[0].Target.Should().Be(target);
    }

    [Fact]
    public void Connect_self_is_rejected_and_reported()
    {
        var editor = CreateEditor();
        editor.AddNodeAt(WorkflowNodeKind.Api, new Point(0, 0));
        var node = editor.Nodes[0];

        editor.ConnectCommand.Execute(Tuple.Create<object, object>(node.Output[0], node.Input[0]));

        editor.Connections.Should().BeEmpty();
        _status.Message.Should().Be(WorkflowErrors.ConnectSelf.Message);
    }

    [Fact]
    public async Task Save_persists_the_mapped_graph()
    {
        var editor = CreateEditor();
        await editor.LoadAsync();
        editor.AddNodeAt(WorkflowNodeKind.Delay, new Point(5, 5));

        await editor.SaveCommand.ExecuteAsync(null);

        var saved = await _repository.GetAsync(_workflowId);
        saved!.Nodes.Should().ContainSingle(n => n.Kind == WorkflowNodeKind.Delay);
    }

    [Fact]
    public async Task Run_executes_the_mapped_graph_and_applies_live_status()
    {
        // Progress<T> marshals reports to the captured SynchronizationContext (the UI thread in the
        // app). Install an inline context so the reports apply deterministically in this test.
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new InlineSynchronizationContext());
        try
        {
            var editor = CreateEditor();
            await editor.LoadAsync();
            editor.AddNodeAt(WorkflowNodeKind.Delay, new Point(5, 5));

            await editor.RunCommand.ExecuteAsync(null);

            _engine.LastWorkflow!.Nodes.Should().ContainSingle();
            editor.Nodes[0].Status.Should().Be(RunStatus.Passed);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    /// <summary>Runs posted callbacks inline so <see cref="Progress{T}"/> reports apply synchronously.</summary>
    private sealed class InlineSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => d(state);

        public override void Send(SendOrPostCallback d, object? state) => d(state);
    }

    /// <summary>Captures the workflow it was asked to run and streams Running→Passed per node.</summary>
    private sealed class RecordingEngine : IWorkflowEngine
    {
        public Workflow? LastWorkflow { get; private set; }

        public Task<WorkflowRunResult> RunAsync(
            Workflow workflow,
            WorkflowRunOptions? options = null,
            IWorkflowContext? context = null,
            IProgress<NodeRunResult>? progress = null,
            CancellationToken cancellationToken = default)
        {
            LastWorkflow = workflow;
            foreach (var node in workflow.Nodes)
            {
                progress?.Report(new NodeRunResult { NodeId = node.Id, NodeName = node.Name, Kind = node.Kind, Status = RunStatus.Running });
                progress?.Report(new NodeRunResult { NodeId = node.Id, NodeName = node.Name, Kind = node.Kind, Status = RunStatus.Passed });
            }

            return Task.FromResult(new WorkflowRunResult { WorkflowId = workflow.Id, Status = RunStatus.Passed, Nodes = [] });
        }
    }

    private sealed class InMemoryWorkflowRepository : IWorkflowRepository
    {
        private readonly Dictionary<Guid, Workflow> _store = [];

        public void Seed(Workflow workflow) => _store[workflow.Id] = workflow;

        public Task<Workflow?> GetAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.TryGetValue(id, out var workflow) ? workflow : null);

        public Task<IReadOnlyList<WorkflowDefinition>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<WorkflowDefinition>>([]);

        public Task SaveAsync(Workflow workflow, CancellationToken cancellationToken = default)
        {
            _store[workflow.Id] = workflow;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _store.Remove(id);
            return Task.CompletedTask;
        }
    }
}
