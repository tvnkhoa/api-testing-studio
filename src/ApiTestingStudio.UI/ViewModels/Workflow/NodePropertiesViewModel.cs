using System.ComponentModel;
using ApiTestingStudio.Application.Common;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.UI.ViewModels.Workflow.Commands;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Workflow;

/// <summary>
/// Edits the selected node's title and typed <c>*NodeConfig</c>. Each committed change is applied
/// through the shared <see cref="IUndoRedoService"/> as an <see cref="EditNodeCommand"/>, so property
/// edits participate in undo/redo alongside graph edits. The view shows only the field group matching
/// the node's <see cref="Kind"/>.
/// </summary>
public sealed partial class NodePropertiesViewModel : ObservableObject
{
    private readonly IUndoRedoService _undo;
    private NodeViewModel? _node;
    private (string Title, object? Config) _committed;
    private bool _loading;

    public NodePropertiesViewModel(IUndoRedoService undo)
    {
        _undo = undo ?? throw new ArgumentNullException(nameof(undo));
    }

    /// <summary>Available HTTP verbs for the Request editor.</summary>
    public IReadOnlyList<HttpVerb> Methods { get; } = Enum.GetValues<HttpVerb>();

    /// <summary>Available operators for the Condition editor.</summary>
    public IReadOnlyList<ConditionOperator> Operators { get; } = Enum.GetValues<ConditionOperator>();

    [ObservableProperty]
    private bool _hasNode;

    [ObservableProperty]
    private WorkflowNodeKind _kind;

    [ObservableProperty]
    private string _title = string.Empty;

    // Request
    [ObservableProperty]
    private HttpVerb _requestMethod;

    [ObservableProperty]
    private string _requestUrl = string.Empty;

    [ObservableProperty]
    private string? _requestBody;

    // Condition
    [ObservableProperty]
    private string _conditionLeft = string.Empty;

    [ObservableProperty]
    private ConditionOperator _conditionOperator;

    [ObservableProperty]
    private string? _conditionRight;

    // Loop
    [ObservableProperty]
    private string? _loopCollectionExpression;

    [ObservableProperty]
    private int? _loopCount;

    [ObservableProperty]
    private string _loopItemVariable = "item";

    [ObservableProperty]
    private string _loopIndexVariable = "index";

    // Parallel
    [ObservableProperty]
    private int? _parallelDegree;

    // Delay
    [ObservableProperty]
    private int _delayMs;

    /// <summary>Binds the inspector to a node (or clears it when null).</summary>
    public void Load(NodeViewModel? node)
    {
        _loading = true;
        try
        {
            _node = node;
            HasNode = node is not null;
            if (node is null)
            {
                return;
            }

            Kind = node.Kind;
            Title = node.Title;

            switch (node.Config)
            {
                case RequestNodeConfig r:
                    RequestMethod = r.Method;
                    RequestUrl = r.Url;
                    RequestBody = r.Body;
                    break;
                case ConditionNodeConfig c:
                    ConditionLeft = c.Left;
                    ConditionOperator = c.Operator;
                    ConditionRight = c.Right;
                    break;
                case LoopNodeConfig l:
                    LoopCollectionExpression = l.CollectionExpression;
                    LoopCount = l.Count;
                    LoopItemVariable = l.ItemVariable;
                    LoopIndexVariable = l.IndexVariable;
                    break;
                case ParallelNodeConfig p:
                    ParallelDegree = p.MaxDegreeOfParallelism;
                    break;
                case DelayNodeConfig d:
                    DelayMs = d.DelayMs;
                    break;
                default:
                    break;
            }

            _committed = (node.Title, node.Config);
        }
        finally
        {
            _loading = false;
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // HasNode/Kind are display state, not edits; everything else is a user edit to commit.
        if (_loading || _node is null || e.PropertyName is nameof(HasNode) or nameof(Kind))
        {
            return;
        }

        Apply();
    }

    private void Apply()
    {
        if (_node is null)
        {
            return;
        }

        var after = (Title, BuildConfig());
        if (string.Equals(after.Item1, _committed.Title, StringComparison.Ordinal)
            && Equals(after.Item2, _committed.Config))
        {
            return;
        }

        _undo.Execute(new EditNodeCommand(_node, _committed, after));
        _committed = after;
    }

    private object? BuildConfig() => _node!.Kind switch
    {
        WorkflowNodeKind.Api => (_committed.Config as RequestNodeConfig ?? new RequestNodeConfig())
            with { Method = RequestMethod, Url = RequestUrl, Body = RequestBody },
        WorkflowNodeKind.Condition => (_committed.Config as ConditionNodeConfig ?? new ConditionNodeConfig())
            with { Left = ConditionLeft, Operator = ConditionOperator, Right = ConditionRight },
        WorkflowNodeKind.Loop => (_committed.Config as LoopNodeConfig ?? new LoopNodeConfig())
            with
            {
                CollectionExpression = LoopCollectionExpression,
                Count = LoopCount,
                ItemVariable = LoopItemVariable,
                IndexVariable = LoopIndexVariable,
            },
        WorkflowNodeKind.Parallel => (_committed.Config as ParallelNodeConfig ?? new ParallelNodeConfig())
            with { MaxDegreeOfParallelism = ParallelDegree },
        WorkflowNodeKind.Delay => (_committed.Config as DelayNodeConfig ?? new DelayNodeConfig())
            with { DelayMs = DelayMs },
        _ => _committed.Config,
    };
}
