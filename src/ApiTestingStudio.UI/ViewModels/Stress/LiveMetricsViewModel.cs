using ApiTestingStudio.Plugin.Abstractions.Runners;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Stress;

/// <summary>
/// Live readout of a stress run's metrics. Fed by the runner's <see cref="System.IProgress{T}"/>
/// stream on the UI thread; the view binds the observable properties (charts land in Sprint 13).
/// </summary>
public sealed partial class LiveMetricsViewModel : ObservableObject
{
    [ObservableProperty]
    private double _elapsedSeconds;

    [ObservableProperty]
    private long _completed;

    [ObservableProperty]
    private long _failed;

    [ObservableProperty]
    private double _requestsPerSecond;

    [ObservableProperty]
    private double _minMs;

    [ObservableProperty]
    private double _averageMs;

    [ObservableProperty]
    private double _maxMs;

    [ObservableProperty]
    private double _p50Ms;

    [ObservableProperty]
    private double _p95Ms;

    [ObservableProperty]
    private double _p99Ms;

    [ObservableProperty]
    private double _errorRate;

    /// <summary>Applies a snapshot to the bound properties.</summary>
    public void Update(StressMetricsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ElapsedSeconds = snapshot.Elapsed.TotalSeconds;
        Completed = snapshot.Completed;
        Failed = snapshot.Failed;
        RequestsPerSecond = snapshot.RequestsPerSecond;
        MinMs = snapshot.MinMs;
        AverageMs = snapshot.AverageMs;
        MaxMs = snapshot.MaxMs;
        P50Ms = snapshot.P50Ms;
        P95Ms = snapshot.P95Ms;
        P99Ms = snapshot.P99Ms;
        ErrorRate = snapshot.ErrorRate;
    }

    /// <summary>Clears the readout back to zeros.</summary>
    public void Reset() => Update(StressMetricsSnapshot.Empty);
}
