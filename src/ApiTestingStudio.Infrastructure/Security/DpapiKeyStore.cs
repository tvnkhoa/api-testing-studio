using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using ApiTestingStudio.Application.Abstractions;

namespace ApiTestingStudio.Infrastructure.Security;

/// <summary>
/// Stores the AES master key on disk wrapped with Windows DPAPI (<see cref="ProtectedData"/>),
/// scoped to the current user. The wrapped blob lives under the app-data directory, outside any
/// workspace file. Because DPAPI is user/machine-bound, workspace files are NOT portable across
/// machines/users — this is a deliberate offline security trade-off (see ADR-0010).
/// </summary>
public sealed class DpapiKeyStore : IKeyStore
{
    private const int KeySizeBytes = 32; // AES-256
    private const string KeysFolderName = "keys";
    private const string KeyFileName = "master.key";

    // Extra entropy mixed into DPAPI so the blob is bound to this application, not just the user.
    private static readonly byte[] Entropy = "ApiTestingStudio.SecretProtector.v1"u8.ToArray();

    private readonly string _keyFilePath;
    private readonly object _gate = new();
    private byte[]? _cachedKey;

    public DpapiKeyStore(string appDataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appDataDirectory);
        _keyFilePath = Path.Combine(appDataDirectory, KeysFolderName, KeyFileName);
    }

    [SupportedOSPlatform("windows")]
    public byte[] GetOrCreateMasterKey()
    {
        lock (_gate)
        {
            if (_cachedKey is not null)
            {
                return _cachedKey;
            }

            _cachedKey = File.Exists(_keyFilePath) ? LoadKey() : CreateKey();
            return _cachedKey;
        }
    }

    [SupportedOSPlatform("windows")]
    public string GetKeyFingerprint()
    {
        // SHA-256 of the master key, truncated to 16 bytes and hex-encoded. One-way: reveals nothing
        // about the key, but stably identifies it so imports can detect a different-machine key.
        var hash = SHA256.HashData(GetOrCreateMasterKey());
        return Convert.ToHexStringLower(hash.AsSpan(0, 16));
    }

    [SupportedOSPlatform("windows")]
    private byte[] LoadKey()
    {
        var wrapped = File.ReadAllBytes(_keyFilePath);
        return ProtectedData.Unprotect(wrapped, Entropy, DataProtectionScope.CurrentUser);
    }

    [SupportedOSPlatform("windows")]
    private byte[] CreateKey()
    {
        var key = RandomNumberGenerator.GetBytes(KeySizeBytes);
        var wrapped = ProtectedData.Protect(key, Entropy, DataProtectionScope.CurrentUser);

        var directory = Path.GetDirectoryName(_keyFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(_keyFilePath, wrapped);
        return key;
    }
}
