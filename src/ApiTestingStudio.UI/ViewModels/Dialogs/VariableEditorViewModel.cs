using ApiTestingStudio.Application.Variables;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Dialogs;

/// <summary>
/// Backs the new/edit variable dialog. When editing a secret variable, leaving the value blank keeps
/// the stored ciphertext. The environment picker is only relevant for the Environment scope.
/// </summary>
public sealed partial class VariableEditorViewModel : ObservableObject
{
    public VariableEditorViewModel(
        string title,
        Variable? existing,
        IReadOnlyList<EnvironmentDefinition> environments)
    {
        Title = title;
        Environments = environments;
        _key = existing?.Key ?? string.Empty;
        _scope = existing?.Scope ?? VariableScope.Workspace;
        _isSecret = existing?.IsSecret ?? false;
        // Never surface the stored ciphertext; secret values start blank on edit.
        _value = existing is { IsSecret: false } ? existing.Value ?? string.Empty : string.Empty;
        _selectedEnvironment = existing?.EnvironmentId is { } id
            ? environments.FirstOrDefault(e => e.Id == id)
            : (environments.Count > 0 ? environments[0] : null);
    }

    public string Title { get; }

    public IReadOnlyList<EnvironmentDefinition> Environments { get; }

    public IReadOnlyList<VariableScope> Scopes { get; } = Enum.GetValues<VariableScope>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private string _key;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEnvironmentScope))]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private VariableScope _scope;

    [ObservableProperty]
    private string _value;

    [ObservableProperty]
    private bool _isSecret;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private EnvironmentDefinition? _selectedEnvironment;

    public bool IsEnvironmentScope => Scope == VariableScope.Environment;

    public bool CanConfirm =>
        !string.IsNullOrWhiteSpace(Key) && (!IsEnvironmentScope || SelectedEnvironment is not null);

    public VariableDraft ToDraft() => new()
    {
        Key = Key.Trim(),
        Scope = Scope,
        EnvironmentId = IsEnvironmentScope ? SelectedEnvironment?.Id : null,
        Value = string.IsNullOrEmpty(Value) ? null : Value,
        IsSecret = IsSecret,
    };
}
