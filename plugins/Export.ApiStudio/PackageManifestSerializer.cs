using System.Text.Json;
using ApiTestingStudio.Plugin.Abstractions.Storage;

namespace ApiTestingStudio.Export.ApiStudio;

/// <summary>
/// Reads and writes the package <c>manifest.json</c> using System.Text.Json with camelCase naming
/// and indentation (the manifest is small and human-inspectable). Pure and offline.
/// </summary>
internal static class PackageManifestSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static byte[] Serialize(PackageManifest manifest)
        => JsonSerializer.SerializeToUtf8Bytes(manifest, Options);

    public static PackageManifest Deserialize(Stream json)
        => JsonSerializer.Deserialize<PackageManifest>(json, Options)
           ?? throw new InvalidDataException("Package manifest is empty or malformed.");
}
