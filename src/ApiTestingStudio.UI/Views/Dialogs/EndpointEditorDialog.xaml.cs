using System.Windows;

namespace ApiTestingStudio.UI.Views.Dialogs;

/// <summary>New/edit endpoint dialog. Code-behind is limited to the modal result (view-only concern).</summary>
public partial class EndpointEditorDialog : Window
{
    public EndpointEditorDialog() => InitializeComponent();

    private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;
}
