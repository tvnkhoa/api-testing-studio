using System.Security.Cryptography;
using System.Text;
using ApiTestingStudio.Application.Abstractions;

namespace ApiTestingStudio.Infrastructure.Security;

/// <summary>
/// Real <see cref="ISecretProtector"/>: authenticated encryption with AES-256-GCM, keyed by the
/// master key from <see cref="IKeyStore"/>. Replaces the Phase 1 Base64 placeholder. Ciphertext is
/// self-describing: <c>base64( version(1) || nonce(12) || tag(16) || ciphertext )</c>. Tampering is
/// detected on <see cref="Unprotect"/> (the GCM tag fails) and surfaces as a
/// <see cref="CryptographicException"/> rather than silently returning garbage. See ADR-0010.
/// </summary>
public sealed class AesSecretProtector : ISecretProtector
{
    private const byte FormatVersion = 1;
    private const int NonceSize = 12; // AesGcm.NonceByteSizes recommends 12
    private const int TagSize = 16;   // AesGcm.TagByteSizes max

    private readonly IKeyStore _keyStore;

    public AesSecretProtector(IKeyStore keyStore)
    {
        ArgumentNullException.ThrowIfNull(keyStore);
        _keyStore = keyStore;
    }

    public string Protect(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(_keyStore.GetOrCreateMasterKey(), TagSize))
        {
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        var output = new byte[1 + NonceSize + TagSize + cipherBytes.Length];
        output[0] = FormatVersion;
        Buffer.BlockCopy(nonce, 0, output, 1, NonceSize);
        Buffer.BlockCopy(tag, 0, output, 1 + NonceSize, TagSize);
        Buffer.BlockCopy(cipherBytes, 0, output, 1 + NonceSize + TagSize, cipherBytes.Length);

        return Convert.ToBase64String(output);
    }

    public string Unprotect(string protectedValue)
    {
        ArgumentNullException.ThrowIfNull(protectedValue);

        byte[] input;
        try
        {
            input = Convert.FromBase64String(protectedValue);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Protected value is not valid Base64.", ex);
        }

        if (input.Length < 1 + NonceSize + TagSize || input[0] != FormatVersion)
        {
            throw new CryptographicException("Protected value has an unrecognized format.");
        }

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipherLength = input.Length - 1 - NonceSize - TagSize;
        var cipherBytes = new byte[cipherLength];

        Buffer.BlockCopy(input, 1, nonce, 0, NonceSize);
        Buffer.BlockCopy(input, 1 + NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(input, 1 + NonceSize + TagSize, cipherBytes, 0, cipherLength);

        var plainBytes = new byte[cipherLength];
        using (var aes = new AesGcm(_keyStore.GetOrCreateMasterKey(), TagSize))
        {
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }
}
