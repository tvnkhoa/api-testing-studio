using System.IO;
using ApiTestingStudio.Scenarios.Catalog;
using ApiTestingStudio.Scenarios.Execution;
using ApiTestingStudio.Scenarios.Providers;
using ApiTestingStudio.Scenarios.Reporting;
using FluentAssertions;
using Xunit.Abstractions;

namespace ApiTestingStudio.Scenarios;

/// <summary>
/// On-demand Executable-Scenarios runner. Skipped unless <c>ES_RUN=1</c> so a normal `dotnet test`
/// sweep does not spend minutes driving the GUI. When enabled it launches the real app per scenario,
/// drives it via FlaUI, writes a screenshot Markdown report under <c>reports/&lt;timestamp&gt;/</c>,
/// and asserts every seed scenario passed.
/// </summary>
public sealed class ScenarioTests
{
    private readonly ITestOutputHelper _output;

    public ScenarioTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task Seed_scenarios_pass_and_emit_a_report()
    {
        if (Environment.GetEnvironmentVariable("ES_RUN") != "1")
        {
            _output.WriteLine("ES_RUN != 1 — skipping the on-demand UI scenario run.");
            return;
        }

        var repoRoot = FindRepoRoot();
        var exe = FindHostExe(repoRoot);
        File.Exists(exe).Should().BeTrue($"the Host executable should be built at {exe}");

        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var reportDir = Path.Combine(repoRoot, "tests", "ApiTestingStudio.Scenarios", "reports", stamp);
        var samplePath = Path.Combine(reportDir, "SampleWorkspace.atsdb");

        var runner = new ScenarioRunner(() => new FlaUiScenarioProvider(exe, reportDir));

        var results = new List<ScenarioResult>();
        foreach (var scenario in SeedScenarios.All(samplePath))
        {
            var result = await runner.RunAsync(scenario);
            _output.WriteLine($"{scenario.Name}: {(result.Passed ? "PASS" : "FAIL")} ({result.DurationMs} ms)");
            foreach (var e in result.Expectations)
            {
                _output.WriteLine($"    [{(e.Success ? "ok" : "X")}] {e.Expectation.Kind} {e.Expectation.Target} — {e.Detail}");
            }

            results.Add(result);
        }

        var reportPath = MarkdownReporter.Write(reportDir, results);
        _output.WriteLine($"Report: {reportPath}");

        results.Should().OnlyContain(r => r.Passed, "every seed scenario should pass");
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "ApiTestingStudio.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        return dir ?? throw new InvalidOperationException("Could not locate the repository root (ApiTestingStudio.slnx).");
    }

    private static string FindHostExe(string repoRoot)
    {
        var env = Environment.GetEnvironmentVariable("ES_APP_EXE");
        if (!string.IsNullOrEmpty(env) && File.Exists(env))
        {
            return env;
        }

        var config = AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}")
            ? "Release"
            : "Debug";
        return Path.Combine(repoRoot, "src", "ApiTestingStudio.Host", "bin", config, "net10.0-windows", "ApiTestingStudio.Host.exe");
    }
}
