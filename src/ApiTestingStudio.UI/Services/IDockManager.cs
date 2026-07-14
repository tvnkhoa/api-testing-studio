namespace ApiTestingStudio.UI.Services;

/// <summary>
/// Bridges the live AvalonDock <c>DockingManager</c> control to the persisted layout store. The
/// shell window hands its docking manager to <see cref="Attach"/> (view wiring); everything else is
/// driven from view models so no serialization logic lives in code-behind.
/// </summary>
public interface IDockManager
{
    /// <summary>
    /// Associates the live docking manager with this service and captures the XAML-defined default
    /// arrangement so <see cref="ResetLayoutAsync"/> can restore it. Call once, after the window and
    /// its bound panes have loaded.
    /// </summary>
    /// <param name="dockingManager">The <c>AvalonDock.DockingManager</c> instance.</param>
    void Attach(object dockingManager);

    /// <summary>
    /// Restores the previously saved layout, if any. Returns <c>true</c> when a saved layout was
    /// applied, <c>false</c> when none existed (the default arrangement stays in place).
    /// </summary>
    Task<bool> LoadLayoutAsync(CancellationToken cancellationToken = default);

    /// <summary>Serializes the current layout and persists it.</summary>
    Task SaveLayoutAsync(CancellationToken cancellationToken = default);

    /// <summary>Clears the saved layout and restores the captured default arrangement.</summary>
    Task ResetLayoutAsync(CancellationToken cancellationToken = default);
}
