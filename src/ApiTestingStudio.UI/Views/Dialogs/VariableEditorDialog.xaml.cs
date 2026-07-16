using System.Windows;

namespace ApiTestingStudio.UI.Views.Dialogs;

/// <summary>New/edit variable dialog. Code-behind is limited to the modal result.</summary>
public partial class VariableEditorDialog : Window
{
    public VariableEditorDialog() => InitializeComponent();

    private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;
}
