using ApiTestingStudio.Application.Abstractions;

namespace ApiTestingStudio.Infrastructure.Time;

/// <summary>Default <see cref="IClock"/> backed by the operating-system clock.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
