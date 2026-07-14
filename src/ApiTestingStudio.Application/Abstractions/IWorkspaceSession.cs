using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Read-only view of the currently open workspace. Exactly one workspace is open at a time; the
/// rest of the application depends on this port to know what (if anything) is open rather than on
/// the storage provider directly. The lifecycle is driven by <see cref="IWorkspaceService"/>.
/// </summary>
public interface IWorkspaceSession
{
    /// <summary>Whether a workspace is currently open.</summary>
    bool IsOpen { get; }

    /// <summary>Metadata of the open workspace, or null when none is open.</summary>
    Workspace? Current { get; }

    /// <summary>Provider-specific locator of the open workspace, or null when none is open.</summary>
    string? Location { get; }
}
