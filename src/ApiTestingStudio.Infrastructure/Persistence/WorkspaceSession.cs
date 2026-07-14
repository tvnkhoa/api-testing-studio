using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// Holds the state of the single currently-open workspace. Exposes a read-only view through
/// <see cref="IWorkspaceSession"/> to the rest of the app; the connection string and mutation
/// entry points are <c>internal</c> so only the storage provider (same assembly) can drive them.
/// </summary>
public sealed class WorkspaceSession : IWorkspaceSession
{
    public bool IsOpen => ConnectionString is not null;

    public Workspace? Current { get; private set; }

    public string? Location { get; private set; }

    /// <summary>Connection string for the open workspace file, or null when none is open.</summary>
    internal string? ConnectionString { get; private set; }

    internal void Open(string location, string connectionString, Workspace current)
    {
        Location = location;
        ConnectionString = connectionString;
        Current = current;
    }

    internal void UpdateCurrent(Workspace current) => Current = current;

    internal void Close()
    {
        Location = null;
        ConnectionString = null;
        Current = null;
    }
}
