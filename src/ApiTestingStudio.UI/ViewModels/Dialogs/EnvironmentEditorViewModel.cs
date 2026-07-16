using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Dialogs;

/// <summary>Backs the new/edit environment dialog (name + kind).</summary>
public sealed partial class EnvironmentEditorViewModel : ObservableObject
{
    public EnvironmentEditorViewModel(string title, EnvironmentDefinition? existing)
    {
        Title = title;
        _name = existing?.Name ?? string.Empty;
        _kind = existing?.Kind ?? EnvironmentKind.Development;
    }

    public string Title { get; }

    public IReadOnlyList<EnvironmentKind> Kinds { get; } = Enum.GetValues<EnvironmentKind>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private string _name;

    [ObservableProperty]
    private EnvironmentKind _kind;

    public bool CanConfirm => !string.IsNullOrWhiteSpace(Name);
}
