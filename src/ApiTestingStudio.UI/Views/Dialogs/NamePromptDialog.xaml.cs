using System.Windows;

namespace ApiTestingStudio.UI.Views.Dialogs;

/// <summary>Single-field name prompt. Code-behind is limited to the modal result (view-only concern).</summary>
public partial class NamePromptDialog : Window
{
    public NamePromptDialog() => InitializeComponent();

    private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;
}
