using System.Collections.ObjectModel;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Stress;
using ApiTestingStudio.Core.Plugins;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Runners;
using ApiTestingStudio.UI.Services;
using ApiTestingStudio.UI.ViewModels.Panels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.UI.ViewModels.Stress;

/// <summary>
/// The Stress Runner document pane: configures a load run (mode, virtual users, iterations/duration,
/// target) and drives it through the <see cref="IStressOrchestrator"/>, showing live metrics streamed
/// from the runner. The runner is a plugin — its presence is reported from the
/// <see cref="IPluginRegistry"/>, and Run is disabled when none is loaded. One shell-hosted instance
/// (stable <see cref="PanelViewModel.ContentId"/>).
/// </summary>
public sealed partial class StressRunnerViewModel : DocumentPanelViewModel
{
    public const string PanelContentId = "document.stress";

    private readonly IStressOrchestrator _orchestrator;
    private readonly IServiceRepository _services;
    private readonly IEndpointRepository _endpoints;
    private readonly IWorkflowRepository _workflows;
    private readonly IWorkspaceSession _session;
    private readonly IStatusBarService _statusBar;
    private readonly IPluginRegistry _plugins;
    private readonly ILogger<StressRunnerViewModel> _logger;

    public StressRunnerViewModel(
        IStressOrchestrator orchestrator,
        IServiceRepository services,
        IEndpointRepository endpoints,
        IWorkflowRepository workflows,
        IWorkspaceSession session,
        IStatusBarService statusBar,
        IPluginRegistry plugins,
        ILogger<StressRunnerViewModel> logger)
        : base(PanelContentId, "Stress Runner")
    {
        ArgumentNullException.ThrowIfNull(orchestrator);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(workflows);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(statusBar);
        ArgumentNullException.ThrowIfNull(plugins);
        ArgumentNullException.ThrowIfNull(logger);

        _orchestrator = orchestrator;
        _services = services;
        _endpoints = endpoints;
        _workflows = workflows;
        _session = session;
        _statusBar = statusBar;
        _plugins = plugins;
        _logger = logger;
    }

    /// <summary>The available execution modes bound to the mode selector.</summary>
    public IReadOnlyList<StressMode> Modes { get; } = Enum.GetValues<StressMode>();

    /// <summary>The selectable endpoint/workflow targets for the open workspace.</summary>
    public ObservableCollection<StressTargetOption> Targets { get; } = [];

    /// <summary>Live metrics readout fed by the runner's progress stream.</summary>
    public LiveMetricsViewModel LiveMetrics { get; } = new();

    [ObservableProperty]
    private StressMode _selectedMode = StressMode.Sequential;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private StressTargetOption? _selectedTarget;

    [ObservableProperty]
    private int _virtualUsers = 4;

    [ObservableProperty]
    private int _iterations = 100;

    /// <summary>Duration limit in seconds for concurrent runs; zero means iteration-bounded.</summary>
    [ObservableProperty]
    private int _durationSeconds;

    [ObservableProperty]
    private int _warmupIterations;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private string _runnerStatus = "No stress runner plugin loaded.";

    [ObservableProperty]
    private string _lastRunSummary = string.Empty;

    /// <summary>Loads the runner status and target list for the open workspace.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        UpdateRunnerStatus();
        await ReloadTargetsAsync(cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Clears state when the workspace closes.</summary>
    public void Clear()
    {
        Targets.Clear();
        SelectedTarget = null;
        LiveMetrics.Reset();
        LastRunSummary = string.Empty;
    }

    private bool HasRunner => _plugins.GetByCapability(PluginCapability.StressRunner).Count > 0;

    private void UpdateRunnerStatus()
    {
        var runners = _plugins.GetByCapability(PluginCapability.StressRunner);
        RunnerStatus = runners.Count == 0
            ? "No stress runner plugin loaded."
            : $"Runner: {runners[0].Name} v{runners[0].Version}";
        RunCommand.NotifyCanExecuteChanged();
    }

    private async Task ReloadTargetsAsync(CancellationToken cancellationToken)
    {
        Targets.Clear();
        SelectedTarget = null;
        if (_session.Current is not { } workspace)
        {
            return;
        }

        foreach (var service in await _services.GetByWorkspaceAsync(workspace.Id, cancellationToken).ConfigureAwait(true))
        {
            foreach (var endpoint in await _endpoints.GetByServiceAsync(service.Id, cancellationToken).ConfigureAwait(true))
            {
                Targets.Add(new StressTargetOption(
                    $"{service.Name}: {endpoint.Method} {endpoint.Path}",
                    StressTargetKind.Endpoint,
                    endpoint.Id,
                    null));
            }
        }

        foreach (var workflow in await _workflows.ListAsync(workspace.Id, cancellationToken).ConfigureAwait(true))
        {
            Targets.Add(new StressTargetOption(
                $"Workflow: {workflow.Name}",
                StressTargetKind.Workflow,
                null,
                workflow.Id));
        }

        SelectedTarget = Targets.Count > 0 ? Targets[0] : null;
    }

    [RelayCommand(CanExecute = nameof(CanRun), IncludeCancelCommand = true)]
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        if (SelectedTarget is not { } target)
        {
            return;
        }

        LiveMetrics.Reset();
        LastRunSummary = string.Empty;

        var request = new StressRunRequest
        {
            Config = new StressRunConfig
            {
                Mode = SelectedMode,
                VirtualUsers = Math.Max(1, VirtualUsers),
                Iterations = Math.Max(1, Iterations),
                Duration = DurationSeconds > 0 ? TimeSpan.FromSeconds(DurationSeconds) : null,
                WarmupIterations = Math.Max(0, WarmupIterations),
            },
            TargetKind = target.Kind,
            EndpointId = target.EndpointId,
            WorkflowId = target.WorkflowId,
            TargetName = target.Label,
        };

        var progress = new Progress<StressMetricsSnapshot>(LiveMetrics.Update);

        IsRunning = true;
        try
        {
            var result = await _orchestrator.RunAsync(request, progress, cancellationToken).ConfigureAwait(true);
            if (result.IsFailure)
            {
                LastRunSummary = result.Error.Message;
                _statusBar.SetMessage(result.Error.Message);
                return;
            }

            var run = result.Value;
            LastRunSummary =
                $"{(run.Cancelled ? "Cancelled" : "Completed")} · {run.RequestCount} requests · " +
                $"{run.RequestsPerSecond:F1} rps · P95 {run.P95Ms:F0} ms · errors {run.ErrorRate:P1}";
            _statusBar.SetMessage($"Stress run {(run.Cancelled ? "cancelled" : "completed")} for '{run.TargetName}'.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stress run failed.");
            _statusBar.SetMessage("The stress run failed.");
        }
        finally
        {
            IsRunning = false;
        }
    }

    private bool CanRun() => !IsRunning && SelectedTarget is not null && HasRunner;
}
