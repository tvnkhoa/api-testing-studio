using ApiTestingStudio.Application.Profiles;
using ApiTestingStudio.Application.ServiceCatalog;
using ApiTestingStudio.Application.Testing;
using ApiTestingStudio.Application.Variables;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.UI.ViewModels.Dialogs;

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

    /// <summary>Prompts for profile fields (secrets masked); returns the draft, or null if cancelled.</summary>
    ProfileDraft? PromptProfile(string title, ProfileDefinition? existing = null);

    /// <summary>Prompts for environment name + kind; returns the values, or null if cancelled.</summary>
    (string Name, EnvironmentKind Kind)? PromptEnvironment(string title, EnvironmentDefinition? existing = null);

    /// <summary>Prompts for variable fields; returns the draft, or null if cancelled.</summary>
    VariableDraft? PromptVariable(string title, Variable? existing, IReadOnlyList<EnvironmentDefinition> environments);

    /// <summary>Prompts for test-case fields (name, description, target); returns the draft, or null if cancelled.</summary>
    TestCaseDraft? PromptTestCase(string title, IReadOnlyList<TestCaseTargetOption> targets, TestCaseDraft? existing = null);

    /// <summary>Prompts for assertion fields; <paramref name="kinds"/> come from the loaded assertion plugins. Null if cancelled.</summary>
    AssertionDraft? PromptAssertion(string title, IReadOnlyList<string> kinds, AssertionDraft? existing = null);

    /// <summary>Shows a yes/no confirmation; returns true when the user confirms.</summary>
    bool Confirm(string title, string message);

    /// <summary>Shows an informational message with an OK button.</summary>
    void ShowMessage(string title, string message);

    /// <summary>Shows the modal import wizard; returns true when an import was committed.</summary>
    bool ShowImportWizard();

    /// <summary>Shows the modal backup settings + restore dialog.</summary>
    void ShowBackupSettings(BackupSettingsViewModel viewModel);
}
