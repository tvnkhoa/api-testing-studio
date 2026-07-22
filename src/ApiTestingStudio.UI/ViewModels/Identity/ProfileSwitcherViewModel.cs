using System.Collections.ObjectModel;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Profiles;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.UI.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace ApiTestingStudio.UI.ViewModels.Identity;

/// <summary>
/// Toolbar combo that shows the workspace's profiles and switches the active "Run As" profile.
/// Selecting a profile persists it via <see cref="IProfileService"/> so the Runner authenticates
/// requests as that profile; a null selection means "no profile" (unauthenticated). Mirrors
/// <see cref="EnvironmentSwitcherViewModel"/> — the analogous switcher for environments.
/// </summary>
public sealed partial class ProfileSwitcherViewModel : ObservableObject
{
    private readonly IProfileService _profiles;
    private readonly IWorkspaceSession _session;

    private bool _suppress;

    public ProfileSwitcherViewModel(
        IProfileService profiles,
        IWorkspaceSession session,
        IMessenger messenger)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(messenger);

        _profiles = profiles;
        _session = session;

        // Refresh when profiles are created/renamed/deleted elsewhere (the manager panel).
        messenger.Register<ProfilesChangedMessage>(this, (_, _) => _ = LoadAsync(CancellationToken.None));
    }

    public ObservableCollection<ProfileDefinition> Profiles { get; } = [];

    [ObservableProperty]
    private bool _isWorkspaceOpen;

    [ObservableProperty]
    private ProfileDefinition? _selectedProfile;

    /// <summary>Loads the profiles and reflects the persisted active selection.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsWorkspaceOpen = _session.IsOpen;
        _suppress = true;
        try
        {
            Profiles.Clear();
            SelectedProfile = null;

            if (!_session.IsOpen)
            {
                return;
            }

            var result = await _profiles.ListAsync(cancellationToken).ConfigureAwait(true);
            if (result.IsFailure)
            {
                return;
            }

            foreach (var item in result.Value)
            {
                Profiles.Add(item);
            }

            var activeId = await _profiles.GetActiveIdAsync(cancellationToken).ConfigureAwait(true);
            SelectedProfile = activeId is { } id ? Profiles.FirstOrDefault(p => p.Id == id) : null;
        }
        finally
        {
            _suppress = false;
        }
    }

    public void Clear()
    {
        _suppress = true;
        Profiles.Clear();
        SelectedProfile = null;
        IsWorkspaceOpen = _session.IsOpen;
        _suppress = false;
    }

    partial void OnSelectedProfileChanged(ProfileDefinition? value)
    {
        if (_suppress)
        {
            return;
        }

        _ = SetActiveAsync(value?.Id);
    }

    private async Task SetActiveAsync(Guid? profileId) =>
        await _profiles.SetActiveAsync(profileId, CancellationToken.None).ConfigureAwait(true);
}
