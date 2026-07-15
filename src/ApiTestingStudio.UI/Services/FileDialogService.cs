using Microsoft.Win32;

namespace ApiTestingStudio.UI.Services;

/// <summary>
/// WPF implementation of <see cref="IFileDialogService"/> using the common file dialogs. Kept thin:
/// it only surfaces a path; the workspace lifecycle itself is driven through <c>IWorkspaceService</c>.
/// </summary>
public sealed class FileDialogService : IFileDialogService
{
    private const string WorkspaceExtension = ".atsdb";
    private const string WorkspaceFilter = "API Testing Studio Workspace (*.atsdb)|*.atsdb|All files (*.*)|*.*";

    public string? PromptOpenWorkspace()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open Workspace",
            Filter = WorkspaceFilter,
            DefaultExt = WorkspaceExtension,
            CheckFileExists = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PromptOpenFile(string title, string filter)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            CheckFileExists = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PromptCreateWorkspace()
    {
        var dialog = new SaveFileDialog
        {
            Title = "New Workspace",
            Filter = WorkspaceFilter,
            DefaultExt = WorkspaceExtension,
            AddExtension = true,
            OverwritePrompt = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
