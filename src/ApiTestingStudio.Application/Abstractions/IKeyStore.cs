namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Port that supplies the symmetric master key used by <see cref="ISecretProtector"/> to encrypt
/// secrets at rest. The key never leaves the process in plaintext; the infrastructure adapter is
/// responsible for creating it on first use and protecting it at rest (e.g. DPAPI on Windows).
/// </summary>
public interface IKeyStore
{
    /// <summary>
    /// Returns the 32-byte (AES-256) master key, creating and persisting a protected copy on first
    /// use. Subsequent calls return the same key. Implementations must never log the key.
    /// </summary>
    byte[] GetOrCreateMasterKey();

    /// <summary>
    /// Returns a stable, NON-reversible fingerprint of the master key (never the key itself), used to
    /// detect on import whether a package's secrets were encrypted under a different key (and so
    /// cannot be decrypted here). See ADR-0012.
    /// </summary>
    string GetKeyFingerprint();
}
