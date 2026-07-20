using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Testing;

/// <summary>Aggregate summary of a set of <see cref="TestRunResult"/>s (a suite or ad-hoc run).</summary>
public sealed record TestSuiteReport
{
    public int TotalCases { get; init; }

    public int PassedCases { get; init; }

    public int FailedCases { get; init; }

    /// <summary>Cases that neither passed nor failed cleanly (cancelled / no assertions run).</summary>
    public int OtherCases { get; init; }

    public int TotalAssertions { get; init; }

    public int PassedAssertions { get; init; }

    public int FailedAssertions { get; init; }

    public int SkippedAssertions { get; init; }

    /// <summary>Whether every case passed.</summary>
    public bool AllPassed => TotalCases > 0 && PassedCases == TotalCases;
}

/// <summary>Builds an aggregate <see cref="TestSuiteReport"/> from per-case run results.</summary>
public interface ITestReportBuilder
{
    TestSuiteReport Build(IReadOnlyList<TestRunResult> results);
}
