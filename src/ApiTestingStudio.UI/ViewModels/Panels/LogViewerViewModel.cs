using System.Collections.ObjectModel;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.UI.ViewModels.Panels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiTestingStudio.UI.ViewModels.Panels;

/// <summary>One row in the Log Viewer grid (immutable projection of a persisted log event).</summary>
public sealed record LogRow(DateTimeOffset TimestampUtc, string Level, string Source, string Message, string? Exception);

/// <summary>
/// The Logs tool pane (Sprint 13): reads persisted application (Serilog) events from
/// <see cref="ILogEventStore"/> and filters them by level, source, and free text. It is a
/// read-only observability surface; the events are written by the buffered DB log sink. One
/// shell-hosted instance (stable <see cref="PanelViewModel.ContentId"/>).
/// </summary>
public sealed partial class LogViewerViewModel : ToolPanelViewModel
{
    public const string PanelContentId = "tool.logs";

    /// <summary>Serilog levels from most to least severe; the min-level filter includes everything at or above the selection.</summary>
    private static readonly string[] LevelSeverityOrder = ["Verbose", "Debug", "Information", "Warning", "Error", "Fatal"];

    private readonly ILogEventStore _store;
    private readonly IWorkspaceSession _session;
    private bool _suspendAutoRefresh;

    public LogViewerViewModel(ILogEventStore store, IWorkspaceSession session)
        : base(PanelContentId, "Logs")
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>The events currently shown, most recent first.</summary>
    public ObservableCollection<LogRow> Events { get; } = [];

    /// <summary>Minimum-level options offered in the filter ("All" plus each level).</summary>
    public IReadOnlyList<string> LevelOptions { get; } = ["All", .. LevelSeverityOrder];

    /// <summary>Source options, refreshed from the store; the first entry is "All".</summary>
    public ObservableCollection<string> SourceOptions { get; } = ["All"];

    [ObservableProperty]
    private string _selectedLevel = "All";

    [ObservableProperty]
    private string _selectedSource = "All";

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isEmpty = true;

    /// <summary>Loads sources and events for the open workspace.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await RefreshSourcesAsync(cancellationToken).ConfigureAwait(true);
        await RefreshAsync(cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Clears the view when the workspace closes.</summary>
    public void Clear()
    {
        _suspendAutoRefresh = true;
        try
        {
            Events.Clear();
            SourceOptions.Clear();
            SourceOptions.Add("All");
            SelectedSource = "All";
            SelectedLevel = "All";
            SearchText = string.Empty;
            IsEmpty = true;
        }
        finally
        {
            _suspendAutoRefresh = false;
        }
    }

    // Filter changes re-query immediately (data is small and paged); guarded so the resets in Clear()
    // and RefreshAsync's own no-workspace path do not re-enter.
    partial void OnSelectedLevelChanged(string value) => TriggerAutoRefresh();

    partial void OnSelectedSourceChanged(string value) => TriggerAutoRefresh();

    partial void OnSearchTextChanged(string value) => TriggerAutoRefresh();

    private void TriggerAutoRefresh()
    {
        if (!_suspendAutoRefresh)
        {
            RefreshCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (_session.Current is not { } workspace)
        {
            Events.Clear();
            IsEmpty = true;
            return;
        }

        var query = new LogEventQuery
        {
            Levels = BuildLevelFilter(),
            Source = string.Equals(SelectedSource, "All", StringComparison.Ordinal) ? null : SelectedSource,
            SearchText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
        };

        var events = await _store.QueryAsync(workspace.Id, query, cancellationToken).ConfigureAwait(true);

        Events.Clear();
        foreach (var e in events)
        {
            Events.Add(new LogRow(e.TimestampUtc, e.Level, e.Source, e.Message, e.Exception));
        }

        IsEmpty = Events.Count == 0;
    }

    private async Task RefreshSourcesAsync(CancellationToken cancellationToken)
    {
        if (_session.Current is not { } workspace)
        {
            return;
        }

        var sources = await _store.GetSourcesAsync(workspace.Id, cancellationToken).ConfigureAwait(true);

        SourceOptions.Clear();
        SourceOptions.Add("All");
        foreach (var source in sources.Where(s => !string.IsNullOrEmpty(s)))
        {
            SourceOptions.Add(source);
        }

        if (!SourceOptions.Contains(SelectedSource))
        {
            SelectedSource = "All";
        }
    }

    /// <summary>Translates the min-level selection into the explicit set of level names at or above it.</summary>
    private string[]? BuildLevelFilter()
    {
        if (string.Equals(SelectedLevel, "All", StringComparison.Ordinal))
        {
            return null;
        }

        var index = Array.IndexOf(LevelSeverityOrder, SelectedLevel);
        return index < 0 ? null : LevelSeverityOrder[index..];
    }
}
