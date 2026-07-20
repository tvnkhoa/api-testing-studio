using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using ApiTestingStudio.Application.Testing;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.UI.Messaging;
using ApiTestingStudio.UI.ViewModels.Panels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Testing;

/// <summary>
/// Document pane showing the outcome of a test-case or suite run: an aggregate summary plus a
/// pass/fail tree of cases and their assertions.
/// </summary>
public sealed partial class TestResultsViewModel : DocumentPanelViewModel
{
    public const string PanelContentId = "document.testresults";

    private readonly ITestReportBuilder _reportBuilder;

    public TestResultsViewModel(ITestReportBuilder reportBuilder)
        : base(PanelContentId, "Test Results")
    {
        _reportBuilder = reportBuilder ?? throw new System.ArgumentNullException(nameof(reportBuilder));
    }

    public ObservableCollection<CaseResultViewModel> Cases { get; } = [];

    [ObservableProperty]
    private string _summary = "No test run yet.";

    /// <summary>Populates the view with the results of a completed run.</summary>
    public void Show(IReadOnlyList<TestRunResultView> results)
    {
        System.ArgumentNullException.ThrowIfNull(results);

        Cases.Clear();
        foreach (var view in results)
        {
            Cases.Add(new CaseResultViewModel(view.CaseName, view.Result));
        }

        var report = _reportBuilder.Build(results.Select(r => r.Result).ToList());
        Summary = $"{report.PassedCases}/{report.TotalCases} cases passed · " +
                  $"assertions: {report.PassedAssertions} passed, {report.FailedAssertions} failed, {report.SkippedAssertions} skipped";
    }
}

/// <summary>Display row for one case's run result, with its assertion rows as children.</summary>
public sealed class CaseResultViewModel
{
    public CaseResultViewModel(string caseName, TestRunResult result)
    {
        CaseName = caseName;
        Status = result.Status;
        Header = $"{caseName} — {result.Status} " +
                 $"({result.PassedCount} passed, {result.FailedCount} failed, {result.SkippedCount} skipped, {result.DurationMs} ms)";
        Assertions = TestResultDetails.Deserialize(result.DetailsJson)
            .Select(a => new AssertionResultRow(a))
            .ToList();
    }

    public string CaseName { get; }

    public RunStatus Status { get; }

    public string Header { get; }

    public IReadOnlyList<AssertionResultRow> Assertions { get; }
}

/// <summary>Display row for a single assertion outcome.</summary>
public sealed class AssertionResultRow
{
    public AssertionResultRow(AssertionResult result)
    {
        Outcome = result.Outcome;
        Text = string.Create(CultureInfo.InvariantCulture, $"[{result.Kind}] {result.Outcome}: {result.Message}");
    }

    public AssertionOutcome Outcome { get; }

    public string Text { get; }
}
