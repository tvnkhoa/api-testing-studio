using System.Collections.ObjectModel;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Environments;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.UI.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace ApiTestingStudio.UI.ViewModels.Identity;

/// <summary>
/// Toolbar combo that shows the workspace's environments and switches the active one. Selecting an
/// environment persists it via <see cref="IEnvironmentService"/> and broadcasts the change so other
/// surfaces (e.g. the status bar) refresh. A null selection means "no active environment".
/// </summary>
public sealed partial class EnvironmentSwitcherViewModel : ObservableObject
{
    private readonly IEnvironmentService _environments;
    private readonly IWorkspaceSession _session;
    private readonly IMessenger _messenger;

    private bool _suppress;

    public EnvironmentSwitcherViewModel(
        IEnvironmentService environments,
        IWorkspaceSession session,
        IMessenger messenger)
    {
        ArgumentNullException.ThrowIfNull(environments);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(messenger);

        _environments = environments;
        _session = session;
        _messenger = messenger;

        // Refresh when environments are created/renamed/deleted elsewhere (the manager panel).
        _messenger.Register<EnvironmentsChangedMessage>(this, (_, _) => _ = LoadAsync(CancellationToken.None));
    }

    public ObservableCollection<EnvironmentDefinition> Environments { get; } = [];

    [ObservableProperty]
    private bool _isWorkspaceOpen;

    [ObservableProperty]
    private EnvironmentDefinition? _selectedEnvironment;

    /// <summary>Loads the environments and reflects the persisted active selection.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsWorkspaceOpen = _session.IsOpen;
        _suppress = true;
        try
        {
            Environments.Clear();
            SelectedEnvironment = null;

            if (!_session.IsOpen)
            {
                return;
            }

            var result = await _environments.ListAsync(cancellationToken).ConfigureAwait(true);
            if (result.IsFailure)
            {
                return;
            }

            foreach (var item in result.Value)
            {
                Environments.Add(item);
            }

            var activeId = await _environments.GetActiveIdAsync(cancellationToken).ConfigureAwait(true);
            SelectedEnvironment = activeId is { } id ? Environments.FirstOrDefault(e => e.Id == id) : null;
        }
        finally
        {
            _suppress = false;
        }
    }

    public void Clear()
    {
        _suppress = true;
        Environments.Clear();
        SelectedEnvironment = null;
        IsWorkspaceOpen = _session.IsOpen;
        _suppress = false;
    }

    partial void OnSelectedEnvironmentChanged(EnvironmentDefinition? value)
    {
        if (_suppress)
        {
            return;
        }

        _ = SetActiveAsync(value?.Id);
    }

    private async Task SetActiveAsync(Guid? environmentId) =>
        await _environments.SetActiveAsync(environmentId, CancellationToken.None).ConfigureAwait(true);
}
