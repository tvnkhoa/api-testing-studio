using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Dashboard;
using ApiTestingStudio.Application.Runs;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Plugin.Abstractions.Ui;
using ApiTestingStudio.UI.ViewModels.Panels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiTestingStudio.UI.ViewModels.Dashboard;

/// <summary>
/// The Dashboard document pane (Sprint 13): hosts the registered <see cref="IDashboardWidget"/>s in a
/// responsive grid and refreshes them from <see cref="IDashboardService"/>. It refreshes live off the
/// <see cref="IMetricsFeed"/> as runs complete, marshalling onto the UI thread so continuous updates
/// never stall it. One shell-hosted instance (stable <see cref="PanelViewModel.ContentId"/>).
/// </summary>
public sealed partial class DashboardViewModel : DocumentPanelViewModel
{
    public const string PanelContentId = "document.dashboard";

    private readonly IDashboardService _dashboard;
    private readonly IMetricsFeed _metrics;
    private readonly IWorkspaceSession _session;
    private readonly SynchronizationContext? _sync;

    public DashboardViewModel(
        IEnumerable<IDashboardWidget> widgets,
        IDashboardService dashboard,
        IMetricsFeed metrics,
        IWorkspaceSession session)
        : base(PanelContentId, "Dashboard")
    {
        ArgumentNullException.ThrowIfNull(widgets);
        _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _session = session ?? throw new ArgumentNullException(nameof(session));

        Widgets = widgets
            .OrderBy(w => (w as DashboardWidgetViewModel)?.Order ?? int.MaxValue)
            .ToList();

        // Captured on the UI thread (the shell is composed there) so live run notifications, which
        // arrive on background threads, can be marshalled back before touching chart state.
        _sync = SynchronizationContext.Current;
        _metrics.RunRecorded += OnRunRecorded;
    }

    /// <summary>The hosted widgets, ordered for display.</summary>
    public IReadOnlyList<IDashboardWidget> Widgets { get; }

    [ObservableProperty]
    private bool _isEmpty = true;

    /// <summary>Loads the dashboard for the open workspace.</summary>
    public Task LoadAsync(CancellationToken cancellationToken = default) => RefreshAsync(cancellationToken);

    /// <summary>Resets all widgets to the empty snapshot (workspace closed).</summary>
    public void Clear()
    {
        ApplySnapshot(DashboardSnapshot.Empty);
        IsEmpty = true;
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (!_session.IsOpen)
        {
            Clear();
            return;
        }

        var result = await _dashboard.GetSnapshotAsync(new DashboardQuery(), cancellationToken).ConfigureAwait(true);
        if (result.IsFailure)
        {
            return;
        }

        ApplySnapshot(result.Value);
        IsEmpty = result.Value.RunCount == 0;
    }

    private void ApplySnapshot(DashboardSnapshot snapshot)
    {
        foreach (var content in Widgets.OfType<IDashboardWidgetContent>())
        {
            content.Update(snapshot);
        }
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
