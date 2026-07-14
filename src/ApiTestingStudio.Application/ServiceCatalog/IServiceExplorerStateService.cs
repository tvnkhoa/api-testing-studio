namespace ApiTestingStudio.Application.ServiceCatalog;

/// <summary>
/// Per-workspace expansion + selection state for the Service Explorer, persisted inside the open
/// workspace's database so it travels with the workspace file.
/// </summary>
public interface IServiceExplorerStateService
{
    /// <summary>Loads the saved state, or <see cref="ExplorerTreeState.Empty"/> when none/invalid.</summary>
    Task<ExplorerTreeState> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists the given expansion + selection state.</summary>
    Task SaveAsync(ExplorerTreeState state, CancellationToken cancellationToken = default);
}

/// <summary>Serializable snapshot of which nodes are expanded and which node is selected.</summary>
public sealed record ExplorerTreeState(IReadOnlyList<Guid> ExpandedIds, Guid? SelectedId)
{
    public static ExplorerTreeState Empty { get; } = new([], null);
}
