namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Port for encrypting/decrypting profile secrets (tokens, passwords, API keys) before they
/// touch storage. Secrets must ALWAYS be protected at rest — this seam guarantees no code path
/// persists plaintext. The production AES/DPAPI implementation lands in the Profiles sprint.
/// </summary>
public interface ISecretProtector
{
    /// <summary>Encrypts plaintext into an opaque, storable ciphertext string.</summary>
    string Protect(string plaintext);

    /// <summary>Reverses <see cref="Protect"/>.</summary>
    string Unprotect(string protectedValue);
}
