using System.Windows;

namespace ApiTestingStudio.UI.Views.Dialogs;

/// <summary>New/edit test-case dialog. Code-behind is limited to the modal result.</summary>
public partial class TestCaseEditorDialog : Window
{
    public TestCaseEditorDialog() => InitializeComponent();

    private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;
}
