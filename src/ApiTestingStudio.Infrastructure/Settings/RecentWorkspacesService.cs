using System.IO;
using System.Text.Json;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Workspaces;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.Infrastructure.Settings;

/// <summary>
/// File-backed <see cref="IRecentWorkspacesService"/>. The MRU list is stored as a JSON document
/// outside any workspace database (under the app data directory) so it persists across app
/// restarts and is independent of which workspace is open. Access is serialized with a semaphore
/// because several UI actions may touch the list concurrently.
/// </summary>
public sealed class RecentWorkspacesService : IRecentWorkspacesService, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _storeFilePath;
    private readonly ILogger<RecentWorkspacesService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public RecentWorkspacesService(string storeFilePath, ILogger<RecentWorkspacesService> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeFilePath);
        _storeFilePath = storeFilePath;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RecentWorkspaceEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await LoadAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddOrTouchAsync(RecentWorkspaceEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var entries = await LoadAsync(cancellationToken).ConfigureAwait(false);

            var merged = new List<RecentWorkspaceEntry>(entries.Count + 1) { entry };
            merged.AddRange(entries.Where(e => !SameLocation(e.Location, entry.Location)));

            var trimmed = merged
                .OrderByDescending(e => e.LastOpenedUtc)
                .Take(IRecentWorkspacesService.Capacity)
                .ToList();

            await SaveAsync(trimmed, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveAsync(string location, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var entries = await LoadAsync(cancellationToken).ConfigureAwait(false);
            var remaining = entries.Where(e => !SameLocation(e.Location, location)).ToList();

            if (remaining.Count != entries.Count)
            {
                await SaveAsync(remaining, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();

    private async Task<IReadOnlyList<RecentWorkspaceEntry>> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storeFilePath))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(_storeFilePath);
            var entries = await JsonSerializer
                .DeserializeAsync<List<RecentWorkspaceEntry>>(stream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            return entries is null
                ? []
                : entries.OrderByDescending(e => e.LastOpenedUtc).ToList();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Recent-workspaces store at {Path} is corrupt; ignoring it.", _storeFilePath);
            return [];
        }
    }

    private async Task SaveAsync(IReadOnlyList<RecentWorkspaceEntry> entries, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(_storeFilePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_storeFilePath);
        await JsonSerializer.SerializeAsync(stream, entries, SerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    private static bool SameLocation(string first, string second)
        => string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
}
