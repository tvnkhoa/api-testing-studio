using System.IO;
using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.Core.Plugins;

/// <summary>A candidate directory plugin: a validated manifest plus the resolved paths to load it.</summary>
/// <param name="Manifest">The parsed, validated manifest.</param>
/// <param name="PluginDirectory">The plugin's folder.</param>
/// <param name="EntryAssemblyPath">Absolute path to the plugin's entry assembly.</param>
public sealed record DirectoryPluginCandidate(PluginManifest Manifest, string PluginDirectory, string EntryAssemblyPath);

/// <summary>
/// The outcome of scanning one plugin folder: either a loadable <see cref="Candidate"/> or an
/// <see cref="Error"/> describing why the folder was rejected.
/// </summary>
public sealed record PluginScanResult(string Source, DirectoryPluginCandidate? Candidate, Error? Error)
{
    /// <summary>True when the folder produced a loadable candidate.</summary>
    public bool IsSuccess => Candidate is not null;

    /// <summary>Creates a successful scan result.</summary>
    public static PluginScanResult Ok(DirectoryPluginCandidate candidate) =>
        new(candidate.PluginDirectory, candidate, null);

    /// <summary>Creates a failed scan result for the given source folder.</summary>
    public static PluginScanResult Failed(string source, Error error) => new(source, null, error);
}

/// <summary>
/// Scans a root directory for plugins. Each immediate subfolder is expected to contain a
/// <c>manifest.json</c> and the entry assembly it names. Scanning never throws for a bad folder —
/// it reports a failed <see cref="PluginScanResult"/> so the caller can quarantine it.
/// </summary>
public sealed class PluginDirectoryScanner
{
    /// <summary>Error code when the entry assembly named by the manifest is missing.</summary>
    public const string EntryAssemblyMissingCode = "plugin.entry_assembly_missing";

    private readonly PluginManifestReader _manifestReader;
    private readonly ILogger<PluginDirectoryScanner> _logger;

    public PluginDirectoryScanner(
        PluginManifestReader? manifestReader = null,
        ILogger<PluginDirectoryScanner>? logger = null)
    {
        _manifestReader = manifestReader ?? new PluginManifestReader();
        _logger = logger ?? NullLogger<PluginDirectoryScanner>.Instance;
    }

    /// <summary>Scans every immediate subfolder of <paramref name="rootDirectory"/> for plugins.</summary>
    public IReadOnlyList<PluginScanResult> Scan(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

        if (!Directory.Exists(rootDirectory))
        {
            _logger.LogInformation("Plugin directory '{Directory}' does not exist; no directory plugins loaded.", rootDirectory);
            return [];
        }

        var results = new List<PluginScanResult>();
        foreach (var pluginDirectory in Directory.EnumerateDirectories(rootDirectory))
        {
            var folderName = Path.GetFileName(pluginDirectory);

            var manifestResult = _manifestReader.ReadFromDirectory(pluginDirectory);
            if (manifestResult.IsFailure)
            {
                _logger.LogWarning("Skipping '{Folder}': {Reason}", folderName, manifestResult.Error.Message);
                results.Add(PluginScanResult.Failed(folderName, manifestResult.Error));
                continue;
            }

            var manifest = manifestResult.Value;
            var entryAssemblyPath = Path.Combine(pluginDirectory, manifest.EntryAssembly);
            if (!File.Exists(entryAssemblyPath))
            {
                var error = new Error(
                    EntryAssemblyMissingCode,
                    $"Entry assembly '{manifest.EntryAssembly}' not found in '{folderName}'.");
                _logger.LogWarning("Skipping '{Folder}': {Reason}", folderName, error.Message);
                results.Add(PluginScanResult.Failed(folderName, error));
                continue;
            }

            _logger.LogInformation("Discovered plugin manifest '{Id}' v{Version} in '{Folder}'.",
                manifest.Id, manifest.Version, folderName);
            results.Add(PluginScanResult.Ok(new DirectoryPluginCandidate(manifest, pluginDirectory, entryAssemblyPath)));
        }

        return results;
    }
}
