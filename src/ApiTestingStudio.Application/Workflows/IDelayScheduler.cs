namespace ApiTestingStudio.Application.Workflows;

/// <summary>
/// Abstracts the wall-clock wait a Delay node performs so tests can run without real delays. The
/// default implementation uses <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.
/// </summary>
public interface IDelayScheduler
{
    Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken = default);
}

/// <summary>Real-time <see cref="IDelayScheduler"/> backed by <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.</summary>
internal sealed class TaskDelayScheduler : IDelayScheduler
{
    public Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken = default) =>
        duration <= TimeSpan.Zero ? Task.CompletedTask : Task.Delay(duration, cancellationToken);
}
