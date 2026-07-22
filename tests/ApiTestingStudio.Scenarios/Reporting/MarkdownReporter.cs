using System.IO;
using System.Text;
using ApiTestingStudio.Scenarios.Execution;

namespace ApiTestingStudio.Scenarios.Reporting;

/// <summary>
/// Emits a single Markdown report for a scenario run: a summary table (scenario → pass/fail →
/// duration) followed by per-scenario steps, expectation results, and embedded screenshots. This is
/// the artifact Sprint 15 could not produce (real screenshots) and the reason the ES harness exists.
/// </summary>
public static class MarkdownReporter
{
    public static string Write(string reportDir, IReadOnlyList<ScenarioResult> results)
    {
        Directory.CreateDirectory(reportDir);
        var sb = new StringBuilder();

        sb.AppendLine("# Executable Scenarios — Run Report");
        sb.AppendLine();
        var passed = results.Count(r => r.Passed);
        sb.AppendLine($"**{passed}/{results.Count} scenarios passed.**");
        sb.AppendLine();
        sb.AppendLine("| Scenario | Result | Duration |");
        sb.AppendLine("|---|:--:|--:|");
        foreach (var r in results)
        {
            sb.AppendLine($"| {r.Name} | {(r.Passed ? "✅ PASS" : "❌ FAIL")} | {r.DurationMs} ms |");
        }

        sb.AppendLine();
        foreach (var r in results)
        {
            sb.AppendLine($"## {r.Name} — {(r.Passed ? "✅ PASS" : "❌ FAIL")}");
            sb.AppendLine();
            sb.AppendLine($"_{r.Goal}_");
            sb.AppendLine();
            if (r.Error is not null)
            {
                sb.AppendLine($"> **Error:** {r.Error}");
                sb.AppendLine();
            }

            sb.AppendLine("**Steps**");
            sb.AppendLine();
            foreach (var s in r.Steps)
            {
                sb.AppendLine($"- {(s.Success ? "✅" : "❌")} {s.Detail}");
            }

            sb.AppendLine();
            sb.AppendLine("**Expectations**");
            sb.AppendLine();
            foreach (var e in r.Expectations)
            {
                sb.AppendLine($"- {(e.Success ? "✅" : "❌")} {e.Expectation.Kind} `{e.Expectation.Target}` — {e.Detail}");
            }

            sb.AppendLine();
            sb.AppendLine("**Screenshots**");
            sb.AppendLine();
            foreach (var shot in r.Screenshots)
            {
                var rel = Path.GetFileName(shot);
                sb.AppendLine($"![{rel}]({rel})");
                sb.AppendLine();
            }
        }

        var path = Path.Combine(reportDir, "report.md");
        File.WriteAllText(path, sb.ToString());
        return path;
    }
}
