using System.Windows;

namespace ApiTestingStudio.UI.Views.Dialogs;

/// <summary>Modal import wizard. Code-behind is limited to the terminal close (view-only concern).</summary>
public partial class ImportWizardDialog : Window
{
    public ImportWizardDialog() => InitializeComponent();

    private void OnClose(object sender, RoutedEventArgs e) => DialogResult = true;
}
