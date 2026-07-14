using System.Windows;

namespace ApiTestingStudio.UI.Views.Dialogs;

/// <summary>New/edit service dialog. Code-behind is limited to the modal result (view-only concern).</summary>
public partial class ServiceEditorDialog : Window
{
    public ServiceEditorDialog() => InitializeComponent();

    private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;
}
