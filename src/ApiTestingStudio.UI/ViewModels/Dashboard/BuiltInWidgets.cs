using System.Globalization;
using ApiTestingStudio.Application.Dashboard;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace ApiTestingStudio.UI.ViewModels.Dashboard;

/// <summary>Shared axis-label paint so charts read on both light and dark themes.</summary>
internal static class ChartPaints
{
    public static SolidColorPaint AxisLabel { get; } = new(new SKColor(0x88, 0x88, 0x88));
}

/// <summary>Scalar KPIs: total runs, success rate, average duration, failures.</summary>
public sealed partial class OverviewWidgetViewModel : DashboardWidgetViewModel
{
    public OverviewWidgetViewModel()
        : base("builtin.overview", "Overview", 0)
    {
    }

    [ObservableProperty]
    private int _runCount;

    [ObservableProperty]
    private string _successRate = "—";

    [ObservableProperty]
    private string _averageDuration = "—";

    [ObservableProperty]
    private int _failedCount;

    public override void Update(DashboardSnapshot snapshot)
    {
        RunCount = snapshot.RunCount;
        FailedCount = snapshot.FailedCount;
        SuccessRate = snapshot.RunCount == 0 ? "—" : snapshot.SuccessRate.ToString("P0", CultureInfo.CurrentCulture);
        AverageDuration = snapshot.RunCount == 0 ? "—" : $"{snapshot.AverageDurationMs:F0} ms";
    }
}

/// <summary>Pass / fail / cancelled distribution as a donut.</summary>
public sealed partial class SuccessRateWidgetViewModel : DashboardWidgetViewModel
{
    public SuccessRateWidgetViewModel()
        : base("builtin.success-rate", "Success Rate", 1)
    {
    }

    [ObservableProperty]
    private ISeries[] _series = [];

    [ObservableProperty]
    private string _caption = "No runs yet.";

    public override void Update(DashboardSnapshot snapshot)
    {
        Series = snapshot.StatusDistribution
            .Select(b => (ISeries)new PieSeries<double>
            {
                Values = [b.Count],
                Name = b.Label,
            })
            .ToArray();

        Caption = snapshot.RunCount == 0
            ? "No runs yet."
            : $"{snapshot.SuccessRate:P0} pass · {snapshot.PassedCount}/{snapshot.RunCount}";
    }
}

/// <summary>Latency of each run over time (line).</summary>
public sealed partial class LatencyWidgetViewModel : DashboardWidgetViewModel
{
    public LatencyWidgetViewModel()
        : base("builtin.latency", "Latency Over Time", 2)
    {
    }

    [ObservableProperty]
    private ISeries[] _series = [];

    [ObservableProperty]
    private Axis[] _xAxes = [];

    [ObservableProperty]
    private Axis[] _yAxes = [];

    public override void Update(DashboardSnapshot snapshot)
    {
        Series =
        [
            new LineSeries<double>
            {
                Values = snapshot.Timeline.Select(p => p.DurationMs).ToArray(),
                Name = "Duration (ms)",
                GeometrySize = 6,
            },
        ];

        XAxes =
        [
            new Axis
            {
                Labels = snapshot.Timeline.Select(p => p.TimestampUtc.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture)).ToArray(),
                LabelsRotation = 45,
                LabelsPaint = ChartPaints.AxisLabel,
            },
        ];

        YAxes =
        [
            new Axis { Name = "ms", LabelsPaint = ChartPaints.AxisLabel, NamePaint = ChartPaints.AxisLabel },
        ];
    }
}

/// <summary>Slowest targets by average duration (horizontal bars).</summary>
public sealed partial class SlowestTargetsWidgetViewModel : DashboardWidgetViewModel
{
    public SlowestTargetsWidgetViewModel()
        : base("builtin.slowest", "Slowest Targets", 3)
    {
    }

    [ObservableProperty]
    private ISeries[] _series = [];

    [ObservableProperty]
    private Axis[] _yAxes = [];

    public override void Update(DashboardSnapshot snapshot)
    {
        Series =
        [
            new RowSeries<double>
            {
                Values = snapshot.SlowestTargets.Select(t => t.AverageMs).ToArray(),
                Name = "Avg ms",
            },
        ];

        YAxes =
        [
            new Axis
            {
                Labels = snapshot.SlowestTargets.Select(t => t.Name).ToArray(),
                LabelsPaint = ChartPaints.AxisLabel,
            },
        ];
    }
}

/// <summary>Most-called targets by run count (horizontal bars).</summary>
public sealed partial class MostCalledTargetsWidgetViewModel : DashboardWidgetViewModel
{
    public MostCalledTargetsWidgetViewModel()
        : base("builtin.most-called", "Most-Called Targets", 4)
    {
    }

    [ObservableProperty]
    private ISeries[] _series = [];

    [ObservableProperty]
    private Axis[] _yAxes = [];

    public override void Update(DashboardSnapshot snapshot)
    {
        Series =
        [
            new RowSeries<double>
            {
                Values = snapshot.MostCalledTargets.Select(t => (double)t.Count).ToArray(),
                Name = "Runs",
            },
        ];

        YAxes =
        [
            new Axis
            {
                Labels = snapshot.MostCalledTargets.Select(t => t.Name).ToArray(),
                LabelsPaint = ChartPaints.AxisLabel,
            },
        ];
    }
}
