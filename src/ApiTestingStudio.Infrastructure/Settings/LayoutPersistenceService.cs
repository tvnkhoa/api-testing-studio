using System.IO;
using ApiTestingStudio.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.Infrastructure.Settings;

/// <summary>
/// File-backed <see cref="ILayoutPersistenceService"/>. The docking layout is stored as an opaque
/// AvalonDock XML payload in a single global per-user file under the app data directory. The payload
/// is written and read verbatim — this service never parses it. Access is semaphore-guarded and a
/// missing file resolves to "no saved layout" rather than throwing.
/// </summary>
public sealed class LayoutPersistenceService : ILayoutPersistenceService, IDisposable
{
    private readonly string _storeFilePath;
    private readonly ILogger<LayoutPersistenceService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public LayoutPersistenceService(string storeFilePath, ILogger<LayoutPersistenceService> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeFilePath);
        ArgumentNullException.ThrowIfNull(logger);
        _storeFilePath = storeFilePath;
        _logger = logger;
    }

    public async Task<string?> LoadLayoutAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_storeFilePath))
            {
                return null;
            }

            var xml = await File.ReadAllTextAsync(_storeFilePath, cancellationToken).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(xml) ? null : xml;
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to read layout store at {Path}; falling back to default layout.", _storeFilePath);
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveLayoutAsync(string layoutXml, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutXml);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(_storeFilePath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(_storeFilePath, layoutXml, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (File.Exists(_storeFilePath))
            {
                File.Delete(_storeFilePath);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to clear layout store at {Path}.", _storeFilePath);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();
}
