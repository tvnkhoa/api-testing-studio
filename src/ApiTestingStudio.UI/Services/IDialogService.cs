using ApiTestingStudio.Application.ServiceCatalog;

namespace ApiTestingStudio.UI.Services;

/// <summary>
/// Shows modal editor dialogs for the Service Explorer and returns the captured result (null when
/// the user cancels). Abstracts WPF windows behind an interface so view models stay testable.
/// </summary>
public interface IDialogService
{
    /// <summary>Prompts for service fields; returns the draft, or null if cancelled.</summary>
    ServiceDraft? PromptService(string title, ServiceDraft? existing = null);

    /// <summary>Prompts for endpoint fields; returns the draft, or null if cancelled.</summary>
    EndpointDraft? PromptEndpoint(string title, EndpointDraft? existing = null);

    /// <summary>Prompts for a single name (create/rename folder, rename service); null if cancelled.</summary>
    string? PromptName(string title, string label, string? existing = null);

    /// <summary>Shows a yes/no confirmation; returns true when the user confirms.</summary>
    bool Confirm(string title, string message);

    /// <summary>Shows the modal import wizard; returns true when an import was committed.</summary>
    bool ShowImportWizard();
}
