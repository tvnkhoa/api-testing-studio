using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTestingStudio.UI.ViewModels.Runner;

/// <summary>
/// An editable name/value row used by the request builder for headers and query params.
/// <see cref="Enabled"/> keeps a row present while excluding it from the outgoing request.
/// </summary>
public sealed partial class KeyValueRowViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _enabled = true;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;
}
