using System.Linq;
using System.Windows;
using ApiTestingStudio.Application.ServiceCatalog;
using ApiTestingStudio.UI.ViewModels.Dialogs;
using ApiTestingStudio.UI.Views.Dialogs;

namespace ApiTestingStudio.UI.Services;

/// <summary>WPF implementation of <see cref="IDialogService"/> using modal editor windows.</summary>
public sealed class DialogService : IDialogService
{
    public ServiceDraft? PromptService(string title, ServiceDraft? existing = null)
    {
        var viewModel = new ServiceEditorViewModel(title, existing);
        var dialog = new ServiceEditorDialog { DataContext = viewModel, Owner = ActiveWindow() };
        return dialog.ShowDialog() == true ? viewModel.ToDraft() : null;
    }

    public EndpointDraft? PromptEndpoint(string title, EndpointDraft? existing = null)
    {
        var viewModel = new EndpointEditorViewModel(title, existing);
        var dialog = new EndpointEditorDialog { DataContext = viewModel, Owner = ActiveWindow() };
        return dialog.ShowDialog() == true ? viewModel.ToDraft() : null;
    }

    public string? PromptName(string title, string label, string? existing = null)
    {
        var viewModel = new NamePromptViewModel(title, label, existing);
        var dialog = new NamePromptDialog { DataContext = viewModel, Owner = ActiveWindow() };
        return dialog.ShowDialog() == true ? viewModel.Result : null;
    }

    public bool Confirm(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private static Window? ActiveWindow() =>
        System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        ?? System.Windows.Application.Current?.MainWindow;
}
