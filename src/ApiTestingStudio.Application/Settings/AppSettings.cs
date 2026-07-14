namespace ApiTestingStudio.Application.Settings;

/// <summary>
/// User-level application preferences that live outside any workspace database (in the app-settings
/// store under the app data directory) so they persist across restarts and across workspaces.
/// Immutable: callers produce a modified copy with <c>with</c> and hand it back to the store.
/// </summary>
public sealed record AppSettings
{
    /// <summary>The active shell theme. Defaults to <see cref="ThemeMode.Light"/>.</summary>
    public ThemeMode Theme { get; init; } = ThemeMode.Light;
}
