using System.IO;
using System.Text.Json;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Settings;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.Infrastructure.Settings;

/// <summary>
/// File-backed <see cref="IAppSettingsService"/>. Settings are stored as a JSON document under the
/// app data directory (outside any workspace database) so they persist across restarts. Access is
/// serialized with a semaphore, and a missing or corrupt store resolves to <see cref="AppSettings"/>
/// defaults rather than throwing — the same tolerant shape as <see cref="RecentWorkspacesService"/>.
/// </summary>
public sealed class AppSettingsService : IAppSettingsService, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _storeFilePath;
    private readonly ILogger<AppSettingsService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public AppSettingsService(string storeFilePath, ILogger<AppSettingsService> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeFilePath);
        ArgumentNullException.ThrowIfNull(logger);
        _storeFilePath = storeFilePath;
        _logger = logger;
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_storeFilePath))
            {
                return new AppSettings();
            }

            await using var stream = File.OpenRead(_storeFilePath);
            var settings = await JsonSerializer
                .DeserializeAsync<AppSettings>(stream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            return settings ?? new AppSettings();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "App settings store at {Path} is corrupt; using defaults.", _storeFilePath);
            return new AppSettings();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(_storeFilePath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var stream = File.Create(_storeFilePath);
            await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();
}
