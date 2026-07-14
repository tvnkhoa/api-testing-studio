using System.Windows;
using ApiTestingStudio.UI.ViewModels;

namespace ApiTestingStudio.Host;

/// <summary>
/// The application shell window. Hosts the AvalonDock docking manager (empty in Phase 1) and
/// binds to the injected <see cref="MainViewModel"/>. All behaviour lives in the view model;
/// code-behind is limited to view-only concerns.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => Close();
}
