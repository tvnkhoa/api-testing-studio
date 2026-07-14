using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ApiTestingStudio.UI.Views.Panels;

/// <summary>
/// Service Explorer tree view. Code-behind is limited to a view-only concern: selecting the node
/// under the cursor on right-click so the shared context menu acts on it.
/// </summary>
public partial class ServiceExplorerView : UserControl
{
    public ServiceExplorerView() => InitializeComponent();

    private void OnItemPreviewRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeViewItem item)
        {
            item.IsSelected = true;
            e.Handled = true;
        }
    }
}
