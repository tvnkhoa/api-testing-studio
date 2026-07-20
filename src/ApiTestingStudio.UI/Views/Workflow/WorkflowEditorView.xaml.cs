using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.UI.Services;
using ApiTestingStudio.UI.ViewModels.Workflow;

namespace ApiTestingStudio.UI.Views.Workflow;

/// <summary>
/// Code-behind for <see cref="WorkflowEditorView"/>. Limited to view-only drag-and-drop plumbing:
/// starting a palette drag and dropping either a node kind (from the palette) or an endpoint
/// reference (from the Service Explorer) onto the Nodify canvas at the mouse location. All
/// state/behaviour lives in <see cref="WorkflowEditorViewModel"/>.
/// </summary>
public partial class WorkflowEditorView : UserControl
{
    private Point _dragStart;
    private NodePaletteItem? _dragItem;

    public WorkflowEditorView()
    {
        InitializeComponent();
    }

    private void Palette_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(null);
        _dragItem = (e.OriginalSource as FrameworkElement)?.DataContext as NodePaletteItem;
    }

    private void Palette_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragItem is null)
        {
            return;
        }

        var diff = _dragStart - e.GetPosition(null);
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var data = new DataObject(DragFormats.NodeKind, _dragItem.Kind);
        _dragItem = null;
        DragDrop.DoDragDrop(Palette, data, DragDropEffects.Copy);
    }

    private void Editor_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is not WorkflowEditorViewModel viewModel)
        {
            return;
        }

        // Editor.MouseLocation is in graph-space coordinates, so the node lands under the cursor.
        var location = Editor.MouseLocation;

        if (e.Data.GetDataPresent(DragFormats.NodeKind)
            && e.Data.GetData(DragFormats.NodeKind) is WorkflowNodeKind kind)
        {
            viewModel.AddNodeAt(kind, location);
            return;
        }

        if (e.Data.GetDataPresent(DragFormats.EndpointRef)
            && e.Data.GetData(DragFormats.EndpointRef) is Guid endpointId)
        {
            // Fire-and-forget: the view model resolves the endpoint and reports failures itself.
            _ = viewModel.AddApiNodeFromEndpointAsync(endpointId, location);
        }
    }
}
