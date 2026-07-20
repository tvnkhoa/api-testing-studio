using System.Windows;

namespace ApiTestingStudio.UI.Views.Dialogs;

/// <summary>New/edit assertion dialog. Code-behind is limited to the modal result.</summary>
public partial class AssertionEditorDialog : Window
{
    public AssertionEditorDialog() => InitializeComponent();

    private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;
}
