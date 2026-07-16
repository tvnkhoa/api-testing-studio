using System.Windows;

namespace ApiTestingStudio.UI.Views.Dialogs;

/// <summary>New/edit environment dialog. Code-behind is limited to the modal result.</summary>
public partial class EnvironmentEditorDialog : Window
{
    public EnvironmentEditorDialog() => InitializeComponent();

    private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;
}
