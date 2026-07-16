using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.UI.ViewModels.Workflow;

namespace ApiTestingStudio.UI.Views.Workflow;

/// <summary>
/// Code-behind for <see cref="WorkflowEditorView"/>. Limited to view-only drag-and-drop plumbing:
/// starting a palette drag and dropping a node kind onto the Nodify canvas at the mouse location.
/// All state/behaviour lives in <see cref="WorkflowEditorViewModel"/>.
/// </summary>
public partial class WorkflowEditorView : UserControl
{
    private const string NodeKindFormat = "ApiTestingStudio.NodeKind";

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

        var data = new DataObject(NodeKindFormat, _dragItem.Kind);
        _dragItem = null;
        DragDrop.DoDragDrop(Palette, data, DragDropEffects.Copy);
    }

    private void Editor_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is not WorkflowEditorViewModel viewModel
            || !e.Data.GetDataPresent(NodeKindFormat)
            || e.Data.GetData(NodeKindFormat) is not WorkflowNodeKind kind)
        {
            return;
        }

        // Editor.MouseLocation is in graph-space coordinates, so the node lands under the cursor.
        viewModel.AddNodeAt(kind, Editor.MouseLocation);
    }
}
