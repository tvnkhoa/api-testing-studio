using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Dialogs;

/// <summary>Backs a single-field name prompt (create/rename folder, rename service).</summary>
public sealed partial class NamePromptViewModel : ObservableObject
{
    public NamePromptViewModel(string title, string label, string? existing)
    {
        Title = title;
        Label = label;
        _name = existing ?? string.Empty;
    }

    public string Title { get; }

    public string Label { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private string _name;

    public bool CanConfirm => !string.IsNullOrWhiteSpace(Name);

    public string Result => Name.Trim();
}
