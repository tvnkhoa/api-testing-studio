using System.Collections.ObjectModel;
using System.Globalization;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Runs;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.UI.Services;
using ApiTestingStudio.UI.ViewModels.Panels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiTestingStudio.UI.ViewModels.Panels;

/// <summary>A run in the timeline list (immutable projection of a <see cref="Run"/> header).</summary>
public sealed record RunSummary(Guid Id, RunSource Source, string Display, RunStatus Status, string When, string Duration);

/// <summary>A node in the run-step drill-down tree (built from the flat <see cref="RunStep"/> rows).</summary>
public sealed class RunStepNode
{
    public required string Name { get; init; }

    public required string Kind { get; init; }

    public RunStatus Status { get; init; }

    public string Detail { get; init; } = string.Empty;

    public IReadOnlyList<RunStepNode> Children { get; init; } = [];
}

/// <summary>
/// The Execution Timeline document (Sprint 13): lists recorded runs and, on selection, reconstructs the
/// run's step tree (Loop/Parallel nesting via <c>ParentStepId</c>) for drill-down. A selected run can be
/// replayed through <see cref="IRunReplayService"/>. Refreshes live off <see cref="IMetricsFeed"/>.
/// </summary>
public sealed partial class TimelineViewModel : DocumentPanelViewModel
{
    public const string PanelContentId = "document.timeline";

    private readonly IRunStore _runStore;
    private readonly IRunReplayService _replay;
    private readonly IWorkspaceSession _session;
    private readonly IStatusBarService _statusBar;
    private readonly IMetricsFeed _metrics;
    private readonly SynchronizationContext? _sync;

    public TimelineViewModel(
        IRunStore runStore,
        IRunReplayService replay,
        IWorkspaceSession session,
        IStatusBarService statusBar,
        IMetricsFeed metrics)
        : base(PanelContentId, "Timeline")
    {
        _runStore = runStore ?? throw new ArgumentNullException(nameof(runStore));
        _replay = replay ?? throw new ArgumentNullException(nameof(replay));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _statusBar = statusBar ?? throw new ArgumentNullException(nameof(statusBar));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));

        _sync = SynchronizationContext.Current;
        _metrics.RunRecorded += OnRunRecorded;
    }

    /// <summary>Recorded runs, most recent first.</summary>
    public ObservableCollection<RunSummary> Runs { get; } = [];

    /// <summary>Root step nodes of the selected run.</summary>
    public ObservableCollection<RunStepNode> Steps { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReplayCommand))]
    private RunSummary? _selectedRun;

    [ObservableProperty]
    private bool _isEmpty = true;

    /// <summary>Loads the run list for the open workspace.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await RefreshAsync(cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Clears the view when the workspace closes.</summary>
    public void Clear()
    {
        Runs.Clear();
        Steps.Clear();
        SelectedRun = null;
        IsEmpty = true;
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (_session.Current is not { } workspace)
        {
            Clear();
            return;
        }

        var previousId = SelectedRun?.Id;
        var runs = await _runStore.ListAsync(workspace.Id, cancellationToken).ConfigureAwait(true);

        Runs.Clear();
        foreach (var run in runs)
        {
            Runs.Add(ToSummary(run));
        }

        IsEmpty = Runs.Count == 0;
        SelectedRun = Runs.FirstOrDefault(r => r.Id == previousId) ?? Runs.FirstOrDefault();
    }

    private bool CanReplay() => SelectedRun is not null;

    [RelayCommand(CanExecute = nameof(CanReplay))]
    private async Task ReplayAsync(CancellationToken cancellationToken)
    {
        if (SelectedRun is not { } run)
        {
            return;
        }

        var result = await _replay.ReplayAsync(run.Id, cancellationToken).ConfigureAwait(true);
        _statusBar.SetMessage(result.IsSuccess ? result.Value.Summary : result.Error.Message);
    }

    partial void OnSelectedRunChanged(RunSummary? value) => _ = LoadStepsAsync(value?.Id);

    private async Task LoadStepsAsync(Guid? runId)
    {
        Steps.Clear();
        if (runId is not { } id)
        {
            return;
        }

        var steps = await _runStore.GetStepsAsync(id, CancellationToken.None).ConfigureAwait(true);
        foreach (var node in BuildTree(steps, parentStepId: null))
        {
            Steps.Add(node);
        }
    }

    private static List<RunStepNode> BuildTree(IReadOnlyList<RunStep> steps, Guid? parentStepId)
    {
        return steps
            .Where(s => s.ParentStepId == parentStepId)
            .OrderBy(s => s.Order)
            .Select(s => new RunStepNode
            {
                Name = s.Name,
                Kind = s.Kind,
                Status = s.Status,
                Detail = BuildDetail(s),
                Children = BuildTree(steps, s.Id),
            })
            .ToList();
    }

    private static string BuildDetail(RunStep step)
    {
        var parts = new List<string> { step.Status.ToString() };
        if (step.StatusCode is { } code)
        {
            parts.Add(code.ToString(CultureInfo.InvariantCulture));
        }

        parts.Add($"{step.DurationMs} ms");
        if (step.Iteration is { } iteration)
        {
            parts.Add($"iter {iteration}");
        }

        if (!string.IsNullOrEmpty(step.Error))
        {
            parts.Add(step.Error);
        }

        return string.Join(" · ", parts);
    }

    private static RunSummary ToSummary(Run run)
    {
        var when = (run.CompletedUtc ?? run.StartedUtc).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
        var display = string.IsNullOrEmpty(run.TargetName) ? "(unnamed)" : run.TargetName;
        return new RunSummary(run.Id, run.Source, display, run.Status, when, $"{run.DurationMs} ms");
    }

    private void OnRunRecorded(object? sender, Run run)
    {
        if (_sync is null)
        {
            _ = RefreshAsync(CancellationToken.None);
            return;
        }

        _sync.Post(_ => _ = RefreshAsync(CancellationToken.None), null);
    }
}
