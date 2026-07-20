using System.Collections.Generic;
using System.Linq;
using ApiTestingStudio.Application.Testing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Dialogs;

/// <summary>Backs the new/edit test-case dialog (name, description, and target endpoint/workflow).</summary>
public sealed partial class TestCaseEditorViewModel : ObservableObject
{
    public TestCaseEditorViewModel(string title, IReadOnlyList<TestCaseTargetOption> targets, TestCaseDraft? existing)
    {
        Title = title;
        Targets = targets;
        _name = existing?.Name ?? string.Empty;
        _description = existing?.Description ?? string.Empty;
        var firstTarget = targets.Count > 0 ? targets[0] : null;
        _selectedTarget = existing is null
            ? firstTarget
            : targets.FirstOrDefault(t => t.EndpointId == existing.EndpointId && t.WorkflowId == existing.WorkflowId)
              ?? firstTarget;
    }

    public string Title { get; }

    public IReadOnlyList<TestCaseTargetOption> Targets { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private string _name;

    [ObservableProperty]
    private string _description;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private TestCaseTargetOption? _selectedTarget;

    public bool CanConfirm => !string.IsNullOrWhiteSpace(Name) && SelectedTarget is not null;

    public TestCaseDraft ToDraft() => new(
        Name.Trim(),
        string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
        SelectedTarget?.EndpointId,
        SelectedTarget?.WorkflowId);
}
