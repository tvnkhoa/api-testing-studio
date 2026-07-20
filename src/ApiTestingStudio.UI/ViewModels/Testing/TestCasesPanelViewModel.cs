using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Testing;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Plugin.Abstractions.Assertions;
using ApiTestingStudio.UI.Messaging;
using ApiTestingStudio.UI.Services;
using ApiTestingStudio.UI.ViewModels.Panels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace ApiTestingStudio.UI.ViewModels.Testing;

/// <summary>
/// The Test Cases tool panel: manages test suites, their cases (each targeting an endpoint request or
/// a workflow), and the assertions attached to each case, and runs them via the
/// <see cref="ITestSuiteExecutor"/>. Results are broadcast for the Test Results document to display.
/// </summary>
public sealed partial class TestCasesPanelViewModel : ToolPanelViewModel
{
    public const string PanelContentId = "tool.testcases";

    private readonly ITestSuiteRepository _suites;
    private readonly ITestCaseRepository _cases;
    private readonly ITestSuiteExecutor _executor;
    private readonly IServiceRepository _services;
    private readonly IEndpointRepository _endpoints;
    private readonly IWorkflowRepository _workflows;
    private readonly IReadOnlyList<string> _assertionKinds;
    private readonly IDialogService _dialog;
    private readonly IStatusBarService _statusBar;
    private readonly IWorkspaceSession _session;
    private readonly IMessenger _messenger;

    public TestCasesPanelViewModel(
        ITestSuiteRepository suites,
        ITestCaseRepository cases,
        ITestSuiteExecutor executor,
        IServiceRepository services,
        IEndpointRepository endpoints,
        IWorkflowRepository workflows,
        IEnumerable<IAssertion> assertions,
        IDialogService dialog,
        IStatusBarService statusBar,
        IWorkspaceSession session,
        IMessenger messenger)
        : base(PanelContentId, "Test Cases")
    {
        ArgumentNullException.ThrowIfNull(suites);
        ArgumentNullException.ThrowIfNull(cases);
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(workflows);
        ArgumentNullException.ThrowIfNull(assertions);
        ArgumentNullException.ThrowIfNull(dialog);
        ArgumentNullException.ThrowIfNull(statusBar);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(messenger);

        _suites = suites;
        _cases = cases;
        _executor = executor;
        _services = services;
        _endpoints = endpoints;
        _workflows = workflows;
        _assertionKinds = assertions.Select(a => a.Kind).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        _dialog = dialog;
        _statusBar = statusBar;
        _session = session;
        _messenger = messenger;
    }

    public ObservableCollection<TestSuite> Suites { get; } = [];

    public ObservableCollection<TestCaseDefinition> Cases { get; } = [];

    public ObservableCollection<AssertionDefinition> Assertions { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteSuiteCommand))]
    [NotifyCanExecuteChangedFor(nameof(NewCaseCommand))]
    [NotifyCanExecuteChangedFor(nameof(RunSuiteCommand))]
    private TestSuite? _selectedSuite;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCaseCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCaseCommand))]
    [NotifyCanExecuteChangedFor(nameof(RunCaseCommand))]
    [NotifyCanExecuteChangedFor(nameof(NewAssertionCommand))]
    private TestCaseDefinition? _selectedCase;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditAssertionCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteAssertionCommand))]
    private AssertionDefinition? _selectedAssertion;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NewSuiteCommand))]
    private bool _isWorkspaceOpen;

    /// <summary>Loads (or reloads) the suite list for the open workspace.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsWorkspaceOpen = _session.IsOpen;
        if (!_session.IsOpen)
        {
            Clear();
            return;
        }

        await ReloadSuitesAsync(cancellationToken).ConfigureAwait(true);
    }

    public void Clear()
    {
        Suites.Clear();
        Cases.Clear();
        Assertions.Clear();
        SelectedSuite = null;
        SelectedCase = null;
        SelectedAssertion = null;
        IsWorkspaceOpen = _session.IsOpen;
    }

    partial void OnSelectedSuiteChanged(TestSuite? value) => _ = ReloadCasesAsync(CancellationToken.None);

    partial void OnSelectedCaseChanged(TestCaseDefinition? value) => _ = ReloadAssertionsAsync(CancellationToken.None);

    // ---- Suites -------------------------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(IsWorkspaceOpen))]
    private async Task NewSuiteAsync(CancellationToken cancellationToken)
    {
        if (_session.Current is not { } workspace)
        {
            return;
        }

        var name = _dialog.PromptName("New Test Suite", "Name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await _suites.AddAsync(new TestSuite { WorkspaceId = workspace.Id, Name = name.Trim(), SortOrder = Suites.Count }, cancellationToken).ConfigureAwait(true);
        await ReloadSuitesAsync(cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(HasSuite))]
    private async Task DeleteSuiteAsync(CancellationToken cancellationToken)
    {
        if (SelectedSuite is not { } suite || !_dialog.Confirm("Delete", $"Delete suite '{suite.Name}'? Its cases become ungrouped."))
        {
            return;
        }

        await _suites.DeleteAsync(suite.Id, cancellationToken).ConfigureAwait(true);
        await ReloadSuitesAsync(cancellationToken).ConfigureAwait(true);
    }

    // ---- Cases --------------------------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(HasSuite))]
    private async Task NewCaseAsync(CancellationToken cancellationToken)
    {
        if (_session.Current is not { } workspace || SelectedSuite is not { } suite)
        {
            return;
        }

        var targets = await BuildTargetsAsync(cancellationToken).ConfigureAwait(true);
        if (targets.Count == 0)
        {
            _statusBar.SetMessage("Add an endpoint or workflow before creating a test case.");
            return;
        }

        var draft = _dialog.PromptTestCase("New Test Case", targets);
        if (draft is null)
        {
            return;
        }

        var definition = new TestCaseDefinition
        {
            WorkspaceId = workspace.Id,
            TestSuiteId = suite.Id,
            EndpointId = draft.EndpointId,
            WorkflowId = draft.WorkflowId,
            Name = draft.Name,
            Description = draft.Description,
            SortOrder = Cases.Count,
        };
        await _cases.SaveAsync(new TestCase { Definition = definition }, cancellationToken).ConfigureAwait(true);
        await ReloadCasesAsync(cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(HasCase))]
    private async Task EditCaseAsync(CancellationToken cancellationToken)
    {
        if (SelectedCase is not { } selected)
        {
            return;
        }

        var current = await _cases.GetAsync(selected.Id, cancellationToken).ConfigureAwait(true);
        if (current is null)
        {
            return;
        }

        var targets = await BuildTargetsAsync(cancellationToken).ConfigureAwait(true);
        var existing = new TestCaseDraft(selected.Name, selected.Description, selected.EndpointId, selected.WorkflowId);
        var draft = _dialog.PromptTestCase("Edit Test Case", targets, existing);
        if (draft is null)
        {
            return;
        }

        var updated = current.Definition with
        {
            Name = draft.Name,
            Description = draft.Description,
            EndpointId = draft.EndpointId,
            WorkflowId = draft.WorkflowId,
        };
        await _cases.SaveAsync(current with { Definition = updated }, cancellationToken).ConfigureAwait(true);
        await ReloadCasesAsync(cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(HasCase))]
    private async Task DeleteCaseAsync(CancellationToken cancellationToken)
    {
        if (SelectedCase is not { } selected || !_dialog.Confirm("Delete", $"Delete test case '{selected.Name}'?"))
        {
            return;
        }

        await _cases.DeleteAsync(selected.Id, cancellationToken).ConfigureAwait(true);
        await ReloadCasesAsync(cancellationToken).ConfigureAwait(true);
    }

    // ---- Assertions ---------------------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(HasCase))]
    private async Task NewAssertionAsync(CancellationToken cancellationToken)
    {
        if (SelectedCase is not { } selected)
        {
            return;
        }

        var draft = _dialog.PromptAssertion("New Assertion", _assertionKinds);
        if (draft is null)
        {
            return;
        }

        var current = await _cases.GetAsync(selected.Id, cancellationToken).ConfigureAwait(true);
        if (current is null)
        {
            return;
        }

        var assertion = ToDefinition(draft, selected.Id, current.Assertions.Count);
        var updated = current with { Assertions = [.. current.Assertions, assertion] };
        await _cases.SaveAsync(updated, cancellationToken).ConfigureAwait(true);
        await ReloadAssertionsAsync(cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(HasAssertion))]
    private async Task EditAssertionAsync(CancellationToken cancellationToken)
    {
        if (SelectedCase is not { } selectedCase || SelectedAssertion is not { } selected)
        {
            return;
        }

        var existing = new AssertionDraft(selected.Kind, selected.Source, selected.Target, selected.Expression, selected.Operator, selected.Expected, selected.Enabled);
        var draft = _dialog.PromptAssertion("Edit Assertion", _assertionKinds, existing);
        if (draft is null)
        {
            return;
        }

        var current = await _cases.GetAsync(selectedCase.Id, cancellationToken).ConfigureAwait(true);
        if (current is null)
        {
            return;
        }

        var replaced = current.Assertions
            .Select(a => a.Id == selected.Id
                ? a with { Kind = draft.Kind, Source = draft.Source, Target = draft.Target, Expression = draft.Expression, Operator = draft.Operator, Expected = draft.Expected, Enabled = draft.Enabled }
                : a)
            .ToList();
        await _cases.SaveAsync(current with { Assertions = replaced }, cancellationToken).ConfigureAwait(true);
        await ReloadAssertionsAsync(cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(HasAssertion))]
    private async Task DeleteAssertionAsync(CancellationToken cancellationToken)
    {
        if (SelectedCase is not { } selectedCase || SelectedAssertion is not { } selected)
        {
            return;
        }

        var current = await _cases.GetAsync(selectedCase.Id, cancellationToken).ConfigureAwait(true);
        if (current is null)
        {
            return;
        }

        var remaining = current.Assertions.Where(a => a.Id != selected.Id).ToList();
        await _cases.SaveAsync(current with { Assertions = remaining }, cancellationToken).ConfigureAwait(true);
        await ReloadAssertionsAsync(cancellationToken).ConfigureAwait(true);
    }

    // ---- Run ----------------------------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(HasCase))]
    private async Task RunCaseAsync(CancellationToken cancellationToken)
    {
        if (SelectedCase is not { } selected)
        {
            return;
        }

        var result = await _executor.RunCaseAsync(selected.Id, progress: null, cancellationToken).ConfigureAwait(true);
        if (result.IsFailure)
        {
            _statusBar.SetMessage(result.Error.Message);
            return;
        }

        _messenger.Send(new ShowTestResultsMessage(
            $"Case: {selected.Name}",
            [new TestRunResultView(selected.Name, result.Value)]));
        _statusBar.SetMessage($"Ran '{selected.Name}': {result.Value.Status}.");
    }

    [RelayCommand(CanExecute = nameof(HasSuite))]
    private async Task RunSuiteAsync(CancellationToken cancellationToken)
    {
        if (SelectedSuite is not { } suite)
        {
            return;
        }

        var result = await _executor.RunSuiteAsync(suite.Id, progress: null, cancellationToken).ConfigureAwait(true);
        if (result.IsFailure)
        {
            _statusBar.SetMessage(result.Error.Message);
            return;
        }

        var names = Cases.ToDictionary(c => c.Id, c => c.Name);
        var views = result.Value
            .Select(r => new TestRunResultView(names.TryGetValue(r.TestCaseId, out var name) ? name : r.TestCaseId.ToString(), r))
            .ToList();
        _messenger.Send(new ShowTestResultsMessage($"Suite: {suite.Name}", views));

        var passed = result.Value.Count(r => r.Status == Domain.Enums.RunStatus.Passed);
        _statusBar.SetMessage($"Ran suite '{suite.Name}': {passed}/{result.Value.Count} cases passed.");
    }

    // ---- Helpers ------------------------------------------------------------------------------

    private async Task ReloadSuitesAsync(CancellationToken cancellationToken)
    {
        Suites.Clear();
        SelectedSuite = null;
        if (_session.Current is not { } workspace)
        {
            return;
        }

        foreach (var suite in await _suites.ListAsync(workspace.Id, cancellationToken).ConfigureAwait(true))
        {
            Suites.Add(suite);
        }
    }

    private async Task ReloadCasesAsync(CancellationToken cancellationToken)
    {
        Cases.Clear();
        SelectedCase = null;
        if (SelectedSuite is not { } suite)
        {
            return;
        }

        foreach (var testCase in await _cases.ListBySuiteAsync(suite.Id, cancellationToken).ConfigureAwait(true))
        {
            Cases.Add(testCase);
        }
    }

    private async Task ReloadAssertionsAsync(CancellationToken cancellationToken)
    {
        Assertions.Clear();
        SelectedAssertion = null;
        if (SelectedCase is not { } selected)
        {
            return;
        }

        var current = await _cases.GetAsync(selected.Id, cancellationToken).ConfigureAwait(true);
        if (current is null)
        {
            return;
        }

        foreach (var assertion in current.Assertions)
        {
            Assertions.Add(assertion);
        }
    }

    private async Task<IReadOnlyList<TestCaseTargetOption>> BuildTargetsAsync(CancellationToken cancellationToken)
    {
        var targets = new List<TestCaseTargetOption>();
        if (_session.Current is not { } workspace)
        {
            return targets;
        }

        foreach (var service in await _services.GetByWorkspaceAsync(workspace.Id, cancellationToken).ConfigureAwait(true))
        {
            foreach (var endpoint in await _endpoints.GetByServiceAsync(service.Id, cancellationToken).ConfigureAwait(true))
            {
                targets.Add(new TestCaseTargetOption($"{service.Name}: {endpoint.Method} {endpoint.Path}", endpoint.Id, null));
            }
        }

        foreach (var workflow in await _workflows.ListAsync(workspace.Id, cancellationToken).ConfigureAwait(true))
        {
            targets.Add(new TestCaseTargetOption($"Workflow: {workflow.Name}", null, workflow.Id));
        }

        return targets;
    }

    private static AssertionDefinition ToDefinition(AssertionDraft draft, Guid testCaseId, int sortOrder) => new()
    {
        TestCaseId = testCaseId,
        Kind = draft.Kind,
        Source = draft.Source,
        Target = draft.Target,
        Expression = draft.Expression,
        Operator = draft.Operator,
        Expected = draft.Expected,
        Enabled = draft.Enabled,
        SortOrder = sortOrder,
    };

    private bool HasSuite() => SelectedSuite is not null;

    private bool HasCase() => SelectedCase is not null;

    private bool HasAssertion() => SelectedAssertion is not null;
}
