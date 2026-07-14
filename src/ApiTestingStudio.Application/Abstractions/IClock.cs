namespace ApiTestingStudio.Application.Abstractions;

/// <summary>
/// Abstraction over the system clock so time-dependent logic stays testable. Never call
/// <c>DateTimeOffset.UtcNow</c> directly in application/domain code — depend on this port.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
