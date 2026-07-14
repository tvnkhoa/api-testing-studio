using ApiTestingStudio.Application.ServiceCatalog;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Dialogs;

/// <summary>Backs the new/edit service dialog. Produces a <see cref="ServiceDraft"/> on confirm.</summary>
public sealed partial class ServiceEditorViewModel : ObservableObject
{
    public ServiceEditorViewModel(string title, ServiceDraft? existing)
    {
        Title = title;
        _name = existing?.Name ?? string.Empty;
        _baseUrl = existing?.BaseUrl ?? string.Empty;
        _description = existing?.Description ?? string.Empty;
    }

    public string Title { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private string _name;

    [ObservableProperty]
    private string _baseUrl;

    [ObservableProperty]
    private string _description;

    public bool CanConfirm => !string.IsNullOrWhiteSpace(Name);

    public ServiceDraft ToDraft() =>
        new(
            Name.Trim(),
            string.IsNullOrWhiteSpace(BaseUrl) ? null : BaseUrl.Trim(),
            string.IsNullOrWhiteSpace(Description) ? null : Description.Trim());
}
