using System;
using System.Collections.Generic;
using ApiTestingStudio.Application.Testing;
using ApiTestingStudio.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Dialogs;

/// <summary>
/// Backs the new/edit assertion dialog. The <see cref="Kinds"/> list is supplied from the loaded
/// assertion plugins so every available assertion type appears here.
/// </summary>
public sealed partial class AssertionEditorViewModel : ObservableObject
{
    public AssertionEditorViewModel(string title, IReadOnlyList<string> kinds, AssertionDraft? existing)
    {
        Title = title;
        Kinds = kinds;
        _selectedKind = existing?.Kind ?? (kinds.Count > 0 ? kinds[0] : string.Empty);
        _source = existing?.Source ?? AssertionSource.Body;
        _target = existing?.Target ?? string.Empty;
        _expression = existing?.Expression ?? string.Empty;
        _operator = existing?.Operator ?? "equals";
        _expected = existing?.Expected ?? string.Empty;
        _enabled = existing?.Enabled ?? true;
    }

    public string Title { get; }

    public IReadOnlyList<string> Kinds { get; }

    public IReadOnlyList<AssertionSource> Sources { get; } = Enum.GetValues<AssertionSource>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private string _selectedKind;

    [ObservableProperty]
    private AssertionSource _source;

    [ObservableProperty]
    private string _target;

    [ObservableProperty]
    private string _expression;

    [ObservableProperty]
    private string _operator;

    [ObservableProperty]
    private string _expected;

    [ObservableProperty]
    private bool _enabled;

    public bool CanConfirm => !string.IsNullOrWhiteSpace(SelectedKind);

    public AssertionDraft ToDraft() => new(
        SelectedKind,
        Source,
        string.IsNullOrWhiteSpace(Target) ? null : Target.Trim(),
        string.IsNullOrWhiteSpace(Expression) ? null : Expression.Trim(),
        string.IsNullOrWhiteSpace(Operator) ? null : Operator.Trim(),
        Expected ?? string.Empty,
        Enabled);
}
