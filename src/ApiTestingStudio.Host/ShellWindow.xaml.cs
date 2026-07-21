using System.ComponentModel;
using System.Windows;
using ApiTestingStudio.UI.Services;
using ApiTestingStudio.UI.ViewModels;
using Microsoft.Extensions.Logging;

namespace ApiTestingStudio.Host;

/// <summary>
/// The application shell window. Hosts the AvalonDock docking manager and binds to the injected
/// <see cref="ShellViewModel"/>. Code-behind is limited to view wiring: handing the live
/// <c>DockingManager</c> to <see cref="IDockManager"/>, restoring the saved layout on load, and
/// persisting it on close. All behaviour lives in the view models.
/// </summary>
public partial class ShellWindow : Window
{
    private readonly ShellViewModel _viewModel;
    private readonly IDockManager _dockManager;
    private readonly ILogger<ShellWindow> _logger;
    private bool _layoutSaved;

    public ShellWindow(ShellViewModel viewModel, IDockManager dockManager, ILogger<ShellWindow> logger)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(dockManager);
        ArgumentNullException.ThrowIfNull(logger);

        _viewModel = viewModel;
        _dockManager = dockManager;
        _logger = logger;

        InitializeComponent();
        DataContext = viewModel;

        _viewModel.CloseRequested += OnCloseRequested;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // The docking manager has now materialised its default layout from the bound sources.
        _dockManager.Attach(DockManager);

        try
        {
            await _dockManager.LoadLayoutAsync().ConfigureAwait(true);
            await _viewModel.InitializeAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialise the shell layout/state.");
        }
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        // Persist the layout before the window actually closes. Cancel the first close, save, then
        // close for real — an async save cannot complete inside a synchronous close.
        if (!_layoutSaved)
        {
            e.Cancel = true;
            _layoutSaved = true;

            try
            {
                await _dockManager.SaveLayoutAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save the shell layout on close.");
            }

            // Re-close on a fresh dispatcher cycle. Calling Close() directly here re-enters the
            // still-unwinding close pipeline and throws ("Cannot ... while a Window is closing").
            _ = Dispatcher.BeginInvoke(new Action(Close));
            return;
        }

        base.OnClosing(e);
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close();
}
