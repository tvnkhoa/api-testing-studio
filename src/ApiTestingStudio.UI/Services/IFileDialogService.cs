namespace ApiTestingStudio.UI.Services;

/// <summary>
/// Abstracts the open/save file dialogs the shell uses to pick a workspace file, so workspace
/// commands stay testable without a live WPF dialog. A workspace is a single self-contained SQLite
/// file with the <c>.atsdb</c> extension.
/// </summary>
public interface IFileDialogService
{
    /// <summary>Prompts for an existing workspace file to open. Returns its path, or null if cancelled.</summary>
    string? PromptOpenWorkspace();

    /// <summary>Prompts for a location to create a new workspace file. Returns its path, or null if cancelled.</summary>
    string? PromptCreateWorkspace();
}
