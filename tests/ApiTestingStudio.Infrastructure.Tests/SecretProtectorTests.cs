using System.Security.Cryptography;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Infrastructure.Security;
using ApiTestingStudio.Infrastructure.Time;
using FluentAssertions;

namespace ApiTestingStudio.Infrastructure.Tests;

public sealed class SecretProtectorTests
{
    // Fixed key so tests are deterministic and never touch DPAPI / the file system.
    private sealed class FixedKeyStore : IKeyStore
    {
        private readonly byte[] _key = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

        public byte[] GetOrCreateMasterKey() => _key;

        public string GetKeyFingerprint() => "test-fingerprint";
    }

    private readonly ISecretProtector _protector = new AesSecretProtector(new FixedKeyStore());

    [Fact]
    public void Protect_then_unprotect_round_trips_the_value()
    {
        const string secret = "s3cr3t-token";

        var protectedValue = _protector.Protect(secret);
        var recovered = _protector.Unprotect(protectedValue);

        recovered.Should().Be(secret);
    }

    [Fact]
    public void Protect_does_not_return_plaintext()
    {
        const string secret = "another-secret";

        var protectedValue = _protector.Protect(secret);

        protectedValue.Should().NotBe(secret);
        Convert.FromBase64String(protectedValue).Should().NotEqual(
            System.Text.Encoding.UTF8.GetBytes(secret));
    }

    [Fact]
    public void Protect_uses_a_fresh_nonce_so_ciphertexts_differ()
    {
        const string secret = "repeatable";

        var first = _protector.Protect(secret);
        var second = _protector.Protect(secret);

        first.Should().NotBe(second);
    }

    [Fact]
    public void Unprotect_rejects_tampered_ciphertext()
    {
        var protectedValue = _protector.Protect("do-not-tamper");
        var bytes = Convert.FromBase64String(protectedValue);
        bytes[^1] ^= 0xFF; // flip a bit in the ciphertext
        var tampered = Convert.ToBase64String(bytes);

        var act = () => _protector.Unprotect(tampered);

        act.Should().Throw<CryptographicException>();
    }
}

public sealed class SystemClockTests
{
    [Fact]
    public void UtcNow_is_in_utc()
    {
        var clock = new SystemClock();

        clock.UtcNow.Offset.Should().Be(TimeSpan.Zero);
    }
}
