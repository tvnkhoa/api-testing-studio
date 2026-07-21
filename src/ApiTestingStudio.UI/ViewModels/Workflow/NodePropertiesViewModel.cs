using System.Collections.ObjectModel;
using System.ComponentModel;
using ApiTestingStudio.Application.Common;
using ApiTestingStudio.Application.Testing;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.UI.Services;
using ApiTestingStudio.UI.ViewModels.Workflow.Commands;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
    private readonly IDialogService _dialog;
    private readonly IReadOnlyList<string> _assertionKinds;
    private NodeViewModel? _node;
    private (string Title, object? Config) _committed;
    private bool _loading;

    public NodePropertiesViewModel(IUndoRedoService undo, IDialogService dialog, IReadOnlyList<string> assertionKinds)
    {
        _undo = undo ?? throw new ArgumentNullException(nameof(undo));
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
        _assertionKinds = assertionKinds ?? throw new ArgumentNullException(nameof(assertionKinds));
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

    // Assertion
    [ObservableProperty]
    private string _assertionSourceNode = string.Empty;

    /// <summary>The assertions configured on the selected Assertion node (edited via the dialog).</summary>
    public ObservableCollection<AssertionRowViewModel> Assertions { get; } = [];

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
            AssertionSourceNode = string.Empty;
            Assertions.Clear();

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
                case AssertionNodeConfig a:
                    AssertionSourceNode = a.SourceNode;
                    RefreshAssertionRows(a.Assertions);
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
        // Only the scalar SourceNode flows through Apply; the assertion list is committed explicitly by
        // the Add/Edit/Remove commands, so the existing list reference is preserved here.
        WorkflowNodeKind.Assertion => (_committed.Config as AssertionNodeConfig ?? new AssertionNodeConfig())
            with { SourceNode = AssertionSourceNode },
        _ => _committed.Config,
    };

    [RelayCommand]
    private void AddAssertion()
    {
        if (_node is null)
        {
            return;
        }

        var draft = _dialog.PromptAssertion("New Assertion", _assertionKinds);
        if (draft is null)
        {
            return;
        }

        var specs = CurrentSpecs();
        specs.Add(FromDraft(draft));
        CommitAssertions(specs);
    }

    [RelayCommand]
    private void EditAssertion(AssertionRowViewModel? row)
    {
        if (_node is null || row is null)
        {
            return;
        }

        var draft = _dialog.PromptAssertion("Edit Assertion", _assertionKinds, ToDraft(row.Spec));
        if (draft is null)
        {
            return;
        }

        var specs = CurrentSpecs();
        var index = specs.FindIndex(s => ReferenceEquals(s, row.Spec));
        if (index < 0)
        {
            return;
        }

        specs[index] = FromDraft(draft);
        CommitAssertions(specs);
    }

    [RelayCommand]
    private void RemoveAssertion(AssertionRowViewModel? row)
    {
        if (_node is null || row is null)
        {
            return;
        }

        var specs = CurrentSpecs();
        specs.RemoveAll(s => ReferenceEquals(s, row.Spec));
        CommitAssertions(specs);
    }

    private List<AssertionSpec> CurrentSpecs() =>
        (_committed.Config as AssertionNodeConfig)?.Assertions.ToList() ?? [];

    private void CommitAssertions(IReadOnlyList<AssertionSpec> specs)
    {
        var current = _committed.Config as AssertionNodeConfig ?? new AssertionNodeConfig();
        var after = (Title, (object?)(current with { Assertions = specs }));

        _undo.Execute(new EditNodeCommand(_node!, _committed, after));
        _committed = after;
        RefreshAssertionRows(specs);
    }

    private void RefreshAssertionRows(IEnumerable<AssertionSpec> specs)
    {
        Assertions.Clear();
        foreach (var spec in specs)
        {
            Assertions.Add(new AssertionRowViewModel(spec));
        }
    }

    private static AssertionDraft ToDraft(AssertionSpec spec) =>
        new(spec.Kind, spec.Source, spec.Target, spec.Expression, spec.Operator, spec.Expected, spec.Enabled);

    private static AssertionSpec FromDraft(AssertionDraft draft) => new()
    {
        Kind = draft.Kind,
        Source = draft.Source,
        Target = draft.Target,
        Expression = draft.Expression,
        Operator = draft.Operator,
        Expected = draft.Expected,
        Enabled = draft.Enabled,
    };
}

/// <summary>One assertion row shown in the Assertion node's property inspector list.</summary>
public sealed class AssertionRowViewModel
{
    public AssertionRowViewModel(AssertionSpec spec)
    {
        Spec = spec ?? throw new ArgumentNullException(nameof(spec));
    }

    /// <summary>The immutable assertion this row displays; edits replace it via the dialog.</summary>
    public AssertionSpec Spec { get; }

    /// <summary>A one-line human-readable summary for the list.</summary>
    public string Summary
    {
        get
        {
            var target = string.IsNullOrWhiteSpace(Spec.Expression) ? Spec.Target : Spec.Expression;
            var comparison = string.IsNullOrWhiteSpace(Spec.Operator)
                ? Spec.Expected
                : $"{Spec.Operator} {Spec.Expected}";
            var detail = string.Join(" · ", new[] { target, comparison }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var summary = $"{Spec.Kind} · {Spec.Source}";
            return string.IsNullOrWhiteSpace(detail) ? summary : $"{summary} · {detail}";
        }
    }

    public bool Enabled => Spec.Enabled;
}
