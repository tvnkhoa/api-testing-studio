using System.Collections.ObjectModel;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Environments;
using ApiTestingStudio.Application.Profiles;
using ApiTestingStudio.Application.Variables;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.UI.Messaging;
using ApiTestingStudio.UI.Services;
using ApiTestingStudio.UI.ViewModels.Panels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace ApiTestingStudio.UI.ViewModels.Identity;

/// <summary>
/// The Profiles &amp; Environments tool panel: manages identity profiles, environments, and variables
/// for the open workspace. All work is delegated to the Application services; secret values are never
/// displayed (masked in editors, stored as ciphertext). Environment changes are broadcast so the
/// toolbar switcher can refresh.
/// </summary>
public sealed partial class ProfilesPanelViewModel : ToolPanelViewModel
{
    public const string PanelContentId = "tool.profiles";

    private readonly IProfileService _profiles;
    private readonly IEnvironmentService _environments;
    private readonly IVariableService _variables;
    private readonly IDialogService _dialog;
    private readonly IStatusBarService _statusBar;
    private readonly IWorkspaceSession _session;
    private readonly IMessenger _messenger;

    public ProfilesPanelViewModel(
        IProfileService profiles,
        IEnvironmentService environments,
        IVariableService variables,
        IDialogService dialog,
        IStatusBarService statusBar,
        IWorkspaceSession session,
        IMessenger messenger)
        : base(PanelContentId, "Profiles & Environments")
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(environments);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(dialog);
        ArgumentNullException.ThrowIfNull(statusBar);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(messenger);

        _profiles = profiles;
        _environments = environments;
        _variables = variables;
        _dialog = dialog;
        _statusBar = statusBar;
        _session = session;
        _messenger = messenger;
    }

    public ObservableCollection<ProfileDefinition> Profiles { get; } = [];

    public ObservableCollection<EnvironmentDefinition> Environments { get; } = [];

    public ObservableCollection<Variable> Variables { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteProfileCommand))]
    private ProfileDefinition? _selectedProfile;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditEnvironmentCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteEnvironmentCommand))]
    private EnvironmentDefinition? _selectedEnvironment;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditVariableCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteVariableCommand))]
    private Variable? _selectedVariable;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NewProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(NewEnvironmentCommand))]
    [NotifyCanExecuteChangedFor(nameof(NewVariableCommand))]
    private bool _isWorkspaceOpen;

    /// <summary>Loads (or reloads) all three lists for the open workspace.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsWorkspaceOpen = _session.IsOpen;
        if (!_session.IsOpen)
        {
            Clear();
            return;
        }

        await ReloadProfilesAsync(cancellationToken).ConfigureAwait(true);
        await ReloadEnvironmentsAsync(cancellationToken).ConfigureAwait(true);
        await ReloadVariablesAsync(cancellationToken).ConfigureAwait(true);
    }

    public void Clear()
    {
        Profiles.Clear();
        Environments.Clear();
        Variables.Clear();
        SelectedProfile = null;
        SelectedEnvironment = null;
        SelectedVariable = null;
        IsWorkspaceOpen = _session.IsOpen;
    }

    // ---- Profiles -----------------------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(IsWorkspaceOpen))]
    private async Task NewProfileAsync(CancellationToken cancellationToken)
    {
        var draft = _dialog.PromptProfile("New Profile", existing: null);
        if (draft is null)
        {
            return;
        }

        await ReportAsync(_profiles.CreateAsync(draft, cancellationToken)).ConfigureAwait(true);
        await ReloadProfilesAsync(cancellationToken).ConfigureAwait(true);
        _messenger.Send(new ProfilesChangedMessage());
    }

    [RelayCommand(CanExecute = nameof(HasProfile))]
    private async Task EditProfileAsync(CancellationToken cancellationToken)
    {
        if (SelectedProfile is not { } profile)
        {
            return;
        }

        var draft = _dialog.PromptProfile("Edit Profile", profile);
        if (draft is null)
        {
            return;
        }

        await ReportAsync(_profiles.UpdateAsync(profile.Id, draft, cancellationToken)).ConfigureAwait(true);
        await ReloadProfilesAsync(cancellationToken).ConfigureAwait(true);
        _messenger.Send(new ProfilesChangedMessage());
    }

    [RelayCommand(CanExecute = nameof(HasProfile))]
    private async Task DeleteProfileAsync(CancellationToken cancellationToken)
    {
        if (SelectedProfile is not { } profile)
        {
            return;
        }

        if (!_dialog.Confirm("Delete", $"Delete profile '{profile.Name}'?"))
        {
            return;
        }

        await ReportAsync(_profiles.DeleteAsync(profile.Id, cancellationToken)).ConfigureAwait(true);
        await ReloadProfilesAsync(cancellationToken).ConfigureAwait(true);
        _messenger.Send(new ProfilesChangedMessage());
    }

    // ---- Environments -------------------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(IsWorkspaceOpen))]
    private async Task NewEnvironmentAsync(CancellationToken cancellationToken)
    {
        var result = _dialog.PromptEnvironment("New Environment", existing: null);
        if (result is not { } value)
        {
            return;
        }

        await ReportAsync(_environments.CreateAsync(value.Name, value.Kind, cancellationToken)).ConfigureAwait(true);
        await ReloadEnvironmentsAsync(cancellationToken).ConfigureAwait(true);
        _messenger.Send(new EnvironmentsChangedMessage());
    }

    [RelayCommand(CanExecute = nameof(HasEnvironment))]
    private async Task EditEnvironmentAsync(CancellationToken cancellationToken)
    {
        if (SelectedEnvironment is not { } environment)
        {
            return;
        }

        var result = _dialog.PromptEnvironment("Edit Environment", environment);
        if (result is not { } value)
        {
            return;
        }

        await ReportAsync(_environments.UpdateAsync(environment.Id, value.Name, value.Kind, cancellationToken)).ConfigureAwait(true);
        await ReloadEnvironmentsAsync(cancellationToken).ConfigureAwait(true);
        _messenger.Send(new EnvironmentsChangedMessage());
    }

    [RelayCommand(CanExecute = nameof(HasEnvironment))]
    private async Task DeleteEnvironmentAsync(CancellationToken cancellationToken)
    {
        if (SelectedEnvironment is not { } environment)
        {
            return;
        }

        if (!_dialog.Confirm("Delete", $"Delete environment '{environment.Name}' and its variables?"))
        {
            return;
        }

        await ReportAsync(_environments.DeleteAsync(environment.Id, cancellationToken)).ConfigureAwait(true);
        await ReloadEnvironmentsAsync(cancellationToken).ConfigureAwait(true);
        await ReloadVariablesAsync(cancellationToken).ConfigureAwait(true);
        _messenger.Send(new EnvironmentsChangedMessage());
    }

    // ---- Variables ----------------------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(IsWorkspaceOpen))]
    private async Task NewVariableAsync(CancellationToken cancellationToken)
    {
        var draft = _dialog.PromptVariable("New Variable", existing: null, Environments);
        if (draft is null)
        {
            return;
        }

        await ReportAsync(_variables.CreateAsync(draft, cancellationToken)).ConfigureAwait(true);
        await ReloadVariablesAsync(cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(HasVariable))]
    private async Task EditVariableAsync(CancellationToken cancellationToken)
    {
        if (SelectedVariable is not { } variable)
        {
            return;
        }

        var draft = _dialog.PromptVariable("Edit Variable", variable, Environments);
        if (draft is null)
        {
            return;
        }

        await ReportAsync(_variables.UpdateAsync(variable.Id, draft, cancellationToken)).ConfigureAwait(true);
        await ReloadVariablesAsync(cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(HasVariable))]
    private async Task DeleteVariableAsync(CancellationToken cancellationToken)
    {
        if (SelectedVariable is not { } variable)
        {
            return;
        }

        if (!_dialog.Confirm("Delete", $"Delete variable '{variable.Key}'?"))
        {
            return;
        }

        await ReportAsync(_variables.DeleteAsync(variable.Id, cancellationToken)).ConfigureAwait(true);
        await ReloadVariablesAsync(cancellationToken).ConfigureAwait(true);
    }

    // ---- Helpers ------------------------------------------------------------------------------

    private async Task ReloadProfilesAsync(CancellationToken cancellationToken)
    {
        var result = await _profiles.ListAsync(cancellationToken).ConfigureAwait(true);
        Profiles.Clear();
        SelectedProfile = null;
        if (result.IsSuccess)
        {
            foreach (var item in result.Value)
            {
                Profiles.Add(item);
            }
        }
    }

    private async Task ReloadEnvironmentsAsync(CancellationToken cancellationToken)
    {
        var result = await _environments.ListAsync(cancellationToken).ConfigureAwait(true);
        Environments.Clear();
        SelectedEnvironment = null;
        if (result.IsSuccess)
        {
            foreach (var item in result.Value)
            {
                Environments.Add(item);
            }
        }
    }

    private async Task ReloadVariablesAsync(CancellationToken cancellationToken)
    {
        var result = await _variables.ListAsync(cancellationToken).ConfigureAwait(true);
        Variables.Clear();
        SelectedVariable = null;
        if (result.IsSuccess)
        {
            foreach (var item in result.Value)
            {
                Variables.Add(item);
            }
        }
    }

    private async Task ReportAsync(Task<Shared.Results.Result> operation)
    {
        var result = await operation.ConfigureAwait(true);
        if (result.IsFailure)
        {
            _statusBar.SetMessage(result.Error.Message);
        }
    }

    private async Task ReportAsync<T>(Task<Shared.Results.Result<T>> operation)
    {
        var result = await operation.ConfigureAwait(true);
        if (result.IsFailure)
        {
            _statusBar.SetMessage(result.Error.Message);
        }
    }

    private bool HasProfile() => SelectedProfile is not null;

    private bool HasEnvironment() => SelectedEnvironment is not null;

    private bool HasVariable() => SelectedVariable is not null;
}
