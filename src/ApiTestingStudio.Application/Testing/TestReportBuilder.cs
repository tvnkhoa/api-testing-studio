using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.Testing;

/// <summary>Default <see cref="ITestReportBuilder"/>: sums case- and assertion-level tallies.</summary>
public sealed class TestReportBuilder : ITestReportBuilder
{
    public TestSuiteReport Build(IReadOnlyList<TestRunResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var passedCases = results.Count(r => r.Status == RunStatus.Passed);
        var failedCases = results.Count(r => r.Status == RunStatus.Failed);

        return new TestSuiteReport
        {
            TotalCases = results.Count,
            PassedCases = passedCases,
            FailedCases = failedCases,
            OtherCases = results.Count - passedCases - failedCases,
            TotalAssertions = results.Sum(r => r.PassedCount + r.FailedCount + r.SkippedCount),
            PassedAssertions = results.Sum(r => r.PassedCount),
            FailedAssertions = results.Sum(r => r.FailedCount),
            SkippedAssertions = results.Sum(r => r.SkippedCount),
        };
    }
}
