using System.IO;

namespace ApiTestingStudio.Application.Packaging;

/// <summary>
/// Resolves the sidecar attachments folder for a workspace file. Attachment blobs live under
/// <c>&lt;workspaceFileName&gt;.attachments/</c> next to the <c>.atsdb</c> (see the <c>Attachment</c>
/// entity and ADR-0012). Pure path arithmetic — no filesystem access.
/// </summary>
public static class WorkspaceAttachmentPaths
{
    /// <summary>Returns the sidecar attachments directory path for a workspace file location.</summary>
    public static string ForWorkspace(string workspaceLocation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceLocation);
        var directory = Path.GetDirectoryName(Path.GetFullPath(workspaceLocation)) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(workspaceLocation);
        return Path.Combine(directory, name + ".attachments");
    }
}
