namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Persists the docking-shell layout as an opaque serialized string (AvalonDock XML) outside any
/// workspace database. The layout is a single global per-user arrangement for this sprint;
/// per-workspace layouts are deferred (see <c>UI/DockLayout.md</c>). The service never interprets
/// the payload — the UI serializes/deserializes the live <c>DockingManager</c> against it.
/// </summary>
public interface ILayoutPersistenceService
{
    /// <summary>Returns the saved layout payload, or <c>null</c> when none has been stored.</summary>
    Task<string?> LoadLayoutAsync(CancellationToken cancellationToken = default);

    /// <summary>Stores <paramref name="layoutXml"/>, replacing any previous layout.</summary>
    Task SaveLayoutAsync(string layoutXml, CancellationToken cancellationToken = default);

    /// <summary>Removes any saved layout so the shell falls back to its default arrangement.</summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
