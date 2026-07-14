using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Infrastructure.Security;
using ApiTestingStudio.Infrastructure.Time;
using FluentAssertions;

namespace ApiTestingStudio.Infrastructure.Tests;

public sealed class SecretProtectorTests
{
    private readonly ISecretProtector _protector = new PlaceholderSecretProtector();

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
