using ApiTestingStudio.Application.Settings;

namespace ApiTestingStudio.UI.Services;

/// <summary>
/// Applies and persists the shell theme. Applying swaps the Material Design base theme on the live
/// application resources; the choice is stored via <c>IAppSettingsService</c> so it survives
/// restarts. Implementations touch WPF resources and therefore live in the UI layer.
/// </summary>
public interface IThemeManager
{
    /// <summary>The theme currently applied.</summary>
    ThemeMode Current { get; }

    /// <summary>Loads the persisted theme and applies it. Call once at startup.</summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Flips between light and dark, applying and persisting the result.</summary>
    Task ToggleAsync(CancellationToken cancellationToken = default);

    /// <summary>Applies <paramref name="mode"/> and persists it.</summary>
    Task SetAsync(ThemeMode mode, CancellationToken cancellationToken = default);
}
