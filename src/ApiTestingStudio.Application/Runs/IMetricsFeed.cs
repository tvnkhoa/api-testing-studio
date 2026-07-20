using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Runs;

/// <summary>
/// A lightweight in-process broadcast of completed runs (Sprint 13). The run recorder publishes each
/// saved <see cref="Run"/>; the Dashboard subscribes and refreshes its aggregates live. Subscribers are
/// responsible for marshalling onto their own UI thread — publishing never blocks on them.
/// </summary>
public interface IMetricsFeed
{
    /// <summary>Raised after a run has been recorded.</summary>
    event EventHandler<Run>? RunRecorded;

    /// <summary>Notifies subscribers that a run was recorded.</summary>
    void Publish(Run run);
}

/// <summary>Default <see cref="IMetricsFeed"/>: a synchronous, exception-isolated event broadcaster.</summary>
public sealed class MetricsFeed : IMetricsFeed
{
    public event EventHandler<Run>? RunRecorded;

    public void Publish(Run run)
    {
        ArgumentNullException.ThrowIfNull(run);
        RunRecorded?.Invoke(this, run);
    }
}
