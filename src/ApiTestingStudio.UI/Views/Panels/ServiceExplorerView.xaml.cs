using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ApiTestingStudio.UI.Services;
using ApiTestingStudio.UI.ViewModels.Explorer;

namespace ApiTestingStudio.UI.Views.Panels;

/// <summary>
/// Service Explorer tree view. Code-behind is limited to view-only concerns: selecting the node
/// under the cursor on right-click so the shared context menu acts on it, and starting a drag of an
/// endpoint (carrying its id) so it can be dropped onto the Workflow Designer canvas.
/// </summary>
public partial class ServiceExplorerView : UserControl
{
    private Point _dragStart;
    private EndpointNodeViewModel? _dragEndpoint;

    public ServiceExplorerView() => InitializeComponent();

    private void OnItemPreviewRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeViewItem item)
        {
            item.IsSelected = true;
            e.Handled = true;
        }
    }

    private void OnTreePreviewLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Only endpoints are draggable; other nodes leave _dragEndpoint null so no drag begins.
        _dragStart = e.GetPosition(null);
        _dragEndpoint = (e.OriginalSource as FrameworkElement)?.DataContext as EndpointNodeViewModel;
    }

    private void OnTreePreviewLeftButtonUp(object sender, MouseButtonEventArgs e) => _dragEndpoint = null;

    private void OnTreePreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragEndpoint is null)
        {
            return;
        }

        var diff = _dragStart - e.GetPosition(null);
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var data = new DataObject(DragFormats.EndpointRef, _dragEndpoint.Id);
        _dragEndpoint = null;
        DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Copy);
    }
}
