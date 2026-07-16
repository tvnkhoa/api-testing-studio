using System.Windows;

namespace ApiTestingStudio.UI.Views.Dialogs;

/// <summary>New/edit profile dialog. Code-behind is limited to the modal result (view-only concern).</summary>
public partial class ProfileEditorDialog : Window
{
    public ProfileEditorDialog() => InitializeComponent();

    private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;
}
