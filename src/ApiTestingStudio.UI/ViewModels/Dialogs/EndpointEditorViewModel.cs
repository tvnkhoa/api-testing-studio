using ApiTestingStudio.Application.ServiceCatalog;
using ApiTestingStudio.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Dialogs;

/// <summary>Backs the new/edit endpoint dialog. Produces an <see cref="EndpointDraft"/> on confirm.</summary>
public sealed partial class EndpointEditorViewModel : ObservableObject
{
    public EndpointEditorViewModel(string title, EndpointDraft? existing)
    {
        Title = title;
        _name = existing?.Name ?? string.Empty;
        _method = existing?.Method ?? HttpVerb.Get;
        _path = existing?.Path ?? string.Empty;
        _description = existing?.Description ?? string.Empty;
    }

    public string Title { get; }

    public IReadOnlyList<HttpVerb> Methods { get; } = Enum.GetValues<HttpVerb>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private string _name;

    [ObservableProperty]
    private HttpVerb _method;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private string _path;

    [ObservableProperty]
    private string _description;

    public bool CanConfirm => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Path);

    public EndpointDraft ToDraft() =>
        new(Name.Trim(), Method, Path.Trim(), string.IsNullOrWhiteSpace(Description) ? null : Description.Trim());
}
