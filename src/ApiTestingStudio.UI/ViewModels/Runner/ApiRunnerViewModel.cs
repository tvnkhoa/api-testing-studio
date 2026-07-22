using System.Collections.ObjectModel;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.ApiRunner;
using ApiTestingStudio.Application.Profiles;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.UI.Messaging;
using ApiTestingStudio.UI.Services;
using ApiTestingStudio.UI.ViewModels.Panels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.UI.ViewModels.Runner;

/// <summary>
/// The API Runner document pane: builds and sends a request for the selected endpoint, shows the
/// response with timing, and keeps a re-runnable per-endpoint history. Subscribes to
/// <see cref="EndpointSelectedMessage"/> to load the endpoint chosen in the Service Explorer. A
/// single shared pane is reused across endpoints (stable <see cref="PanelContentId"/>).
/// </summary>
public sealed partial class ApiRunnerViewModel : DocumentPanelViewModel, IRecipient<EndpointSelectedMessage>
{
    public const string PanelContentId = "document.runner";

    private readonly IRequestExecutionService _execution;
    private readonly IRequestHistoryService _history;
    private readonly IEndpointRepository _endpoints;
    private readonly IServiceRepository _services;
    private readonly IProfileService _profiles;
    private readonly IStatusBarService _statusBar;
    private readonly ILogger<ApiRunnerViewModel> _logger;

    private Guid? _endpointId;

    public ApiRunnerViewModel(
        IRequestExecutionService execution,
        IRequestHistoryService history,
        IEndpointRepository endpoints,
        IServiceRepository services,
        IProfileService profiles,
        IMessenger messenger,
        IStatusBarService statusBar,
        ILogger<ApiRunnerViewModel> logger)
        : base(PanelContentId, "Runner")
    {
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(messenger);
        ArgumentNullException.ThrowIfNull(statusBar);
        ArgumentNullException.ThrowIfNull(logger);

        _execution = execution;
        _history = history;
        _endpoints = endpoints;
        _services = services;
        _profiles = profiles;
        _statusBar = statusBar;
        _logger = logger;

        messenger.Register(this);
    }

    public RequestBuilderViewModel Builder { get; } = new();

    public ResponseViewerViewModel Response { get; } = new();

    public ObservableCollection<RequestHistoryEntry> History { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReplayCommand))]
    private RequestHistoryEntry? _selectedHistoryEntry;

    /// <summary>
    /// Inline warning shown above the response when the last send had unresolved <c>{{variables}}</c>.
    /// Empty when the request resolved cleanly. Also written to the log so the failure is never silent.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWarning))]
    private string _warningMessage = string.Empty;

    public bool HasWarning => !string.IsNullOrEmpty(WarningMessage);

    /// <summary>Loads the endpoint selected in the Service Explorer into the builder.</summary>
    public void Receive(EndpointSelectedMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _ = LoadEndpointSafeAsync(message.EndpointId);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SendAsync(CancellationToken cancellationToken)
    {
        WarningMessage = string.Empty;
        var request = Builder.Build();
        var profileId = await _profiles.GetActiveIdAsync(cancellationToken).ConfigureAwait(true);
        var result = await _execution
            .SendAsync(_endpointId ?? Guid.Empty, request, profileId, cancellationToken)
            .ConfigureAwait(true);

        if (result.IsFailure)
        {
            _statusBar.SetMessage(result.Error.Message);
            return;
        }

        Response.Show(result.Value);
        SurfaceWarnings(result.Value.Warnings);
        _statusBar.SetMessage($"{result.Value.Response.StatusCode} · {result.Value.Timing.Total.TotalMilliseconds:0} ms");
        await RefreshHistoryAsync(cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Shows unresolved-variable warnings inline and logs them, so a hollow send is never silent.</summary>
    private void SurfaceWarnings(IReadOnlyList<string> warnings)
    {
        if (warnings.Count == 0)
        {
            return;
        }

        var tokens = string.Join(", ", warnings.Select(w => $"{{{{{w}}}}}"));
        WarningMessage = $"Unresolved variables sent as empty: {tokens}. Check the active environment.";
        _logger.LogWarning("Request sent with {Count} unresolved variable(s): {Tokens}", warnings.Count, tokens);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedHistory))]
    private async Task ReplayAsync(CancellationToken cancellationToken)
    {
        if (SelectedHistoryEntry is not { } entry)
        {
            return;
        }

        var request = await _history.GetRequestForReplayAsync(entry.Id, cancellationToken).ConfigureAwait(true);
        if (request.IsFailure)
        {
            _statusBar.SetMessage(request.Error.Message);
            return;
        }

        Builder.LoadFromRequest(request.Value);
        await SendAsync(cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task ClearHistoryAsync(CancellationToken cancellationToken)
    {
        var result = await _history.ClearHistoryAsync(_endpointId ?? Guid.Empty, cancellationToken).ConfigureAwait(true);
        if (result.IsSuccess)
        {
            History.Clear();
        }
    }

    private bool HasSelectedHistory() => SelectedHistoryEntry is not null;

    private async Task LoadEndpointSafeAsync(Guid endpointId)
    {
        try
        {
            await LoadEndpointAsync(endpointId, CancellationToken.None).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load endpoint {EndpointId} into the runner.", endpointId);
            _statusBar.SetMessage("Failed to load the selected endpoint.");
        }
    }

    private async Task LoadEndpointAsync(Guid endpointId, CancellationToken cancellationToken)
    {
        var endpoint = await _endpoints.GetAsync(endpointId, cancellationToken).ConfigureAwait(true);
        if (endpoint is null)
        {
            return;
        }

        _endpointId = endpointId;
        var service = await _services.GetAsync(endpoint.ServiceId, cancellationToken).ConfigureAwait(true);
        Builder.LoadFromEndpoint(endpoint, service?.BaseUrl);
        Title = endpoint.Name;

        await RefreshHistoryAsync(cancellationToken).ConfigureAwait(true);
    }

    private async Task RefreshHistoryAsync(CancellationToken cancellationToken)
    {
        var result = await _history.GetHistoryAsync(_endpointId ?? Guid.Empty, cancellationToken).ConfigureAwait(true);
        History.Clear();
        if (result.IsFailure)
        {
            return;
        }

        foreach (var entry in result.Value)
        {
            History.Add(entry);
        }
    }
}
