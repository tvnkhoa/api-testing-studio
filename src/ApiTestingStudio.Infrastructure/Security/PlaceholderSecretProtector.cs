using System.Text;
using ApiTestingStudio.Application.Abstractions;

namespace ApiTestingStudio.Infrastructure.Security;

/// <summary>
/// TEMPORARY placeholder <see cref="ISecretProtector"/> for Phase 1 wiring only.
/// It merely Base64-encodes values — this is NOT encryption and must never ship to users.
/// The Profiles &amp; Environments sprint replaces this with DPAPI/AES-GCM protection.
/// The seam exists now so no code path is written against plaintext storage.
/// </summary>
public sealed class PlaceholderSecretProtector : ISecretProtector
{
    public string Protect(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));
    }

    public string Unprotect(string protectedValue)
    {
        ArgumentNullException.ThrowIfNull(protectedValue);
        return Encoding.UTF8.GetString(Convert.FromBase64String(protectedValue));
    }
}
