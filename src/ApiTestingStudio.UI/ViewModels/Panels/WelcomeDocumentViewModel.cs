using ApiTestingStudio.UI.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace ApiTestingStudio.UI.ViewModels.Panels;

/// <summary>
/// The first-run document shown in the centre pane when the shell opens. Explains the product and
/// offers three call-to-action buttons — Open sample, Import, and Add service — each raised as a
/// <see cref="WelcomeActionMessage"/> the shell routes to the matching flow. Import/Add-service are
/// gated on an open workspace; Open sample always works and is the primary first-run entry point.
/// </summary>
public sealed partial class WelcomeDocumentViewModel : DocumentPanelViewModel
{
    public const string PanelContentId = "document.welcome";

    private readonly IMessenger _messenger;

    public WelcomeDocumentViewModel(IMessenger messenger)
        : base(PanelContentId, "Welcome")
    {
        ArgumentNullException.ThrowIfNull(messenger);
        _messenger = messenger;
    }

    /// <summary>Headline shown on the welcome document.</summary>
    public string Headline { get; } = "API Testing Studio";

    /// <summary>Sub-text shown on the welcome document.</summary>
    public string Message { get; } =
        "A workflow-first, 100% offline API testing workspace. Send requests, chain them into visual " +
        "workflows, test as a role, and stress-test — all local, no cloud. Open the sample workspace " +
        "below to explore, or import an existing API to get started.";

    /// <summary>True when a workspace is open, enabling the Import / Add service actions.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddServiceCommand))]
    private bool _isWorkspaceOpen;

    [RelayCommand]
    private void OpenSample() => _messenger.Send(new WelcomeActionMessage(WelcomeAction.OpenSample));

    [RelayCommand(CanExecute = nameof(IsWorkspaceOpen))]
    private void Import() => _messenger.Send(new WelcomeActionMessage(WelcomeAction.Import));

    [RelayCommand(CanExecute = nameof(IsWorkspaceOpen))]
    private void AddService() => _messenger.Send(new WelcomeActionMessage(WelcomeAction.AddService));
}
