using ApiTestingStudio.Application.Settings;

namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Reads and writes user-level <see cref="AppSettings"/> (theme, etc.). The store lives outside any
/// workspace database so preferences survive restarts and are independent of the open workspace.
/// A missing or corrupt store resolves to default settings rather than throwing.
/// </summary>
public interface IAppSettingsService
{
    /// <summary>Loads the current settings, or defaults when no store exists yet.</summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists <paramref name="settings"/>, replacing any previous values.</summary>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
