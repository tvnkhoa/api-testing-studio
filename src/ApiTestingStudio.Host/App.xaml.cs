using System.Globalization;
using System.IO;
using System.Windows;
using ApiTestingStudio.Application.DependencyInjection;
using ApiTestingStudio.Core.DependencyInjection;
using ApiTestingStudio.Core.Plugins;
using ApiTestingStudio.Host.Composition;
using ApiTestingStudio.Infrastructure.DependencyInjection;
using ApiTestingStudio.Plugin.Abstractions.Storage;
using ApiTestingStudio.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace ApiTestingStudio.Host;

/// <summary>
/// Application composition root. Builds the DI container, wires logging, infrastructure and the
/// plugin host, then resolves and shows the main window. This is the ONLY place that knows about
/// concrete infrastructure and plugin implementations.
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ApiTestingStudio");
        Directory.CreateDirectory(appDataDir);

        ConfigureSerilog(appDataDir);

        try
        {
            _host = BuildHost(appDataDir);
            await _host.StartAsync().ConfigureAwait(true);

            await InitializeStorageAsync().ConfigureAwait(true);

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start.");
            MessageBox.Show(ex.Message, "API Testing Studio — startup error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync().ConfigureAwait(true);
            _host.Dispose();
        }

        await Log.CloseAndFlushAsync().ConfigureAwait(true);
        base.OnExit(e);
    }

    private static void ConfigureSerilog(string appDataDir)
    {
        var logPath = Path.Combine(appDataDir, "logs", "log-.txt");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();
    }

    private static IHost BuildHost(string appDataDir)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        builder.Logging.ClearProviders();
        builder.Services.AddSerilog();

        var dbPath = Path.Combine(appDataDir, "workspace.db");
        var connectionString = $"Data Source={dbPath}";

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(connectionString);

        using var bootstrapLoggerFactory = new SerilogLoggerFactory(Log.Logger);
        builder.Services.AddPluginHost(PluginCatalog.GetPluginAssemblies(), bootstrapLoggerFactory);

        // UI composition
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        return builder.Build();
    }

    private async Task InitializeStorageAsync()
    {
        using var scope = _host!.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageProvider>();
        await storage.InitializeAsync().ConfigureAwait(true);

        var registry = _host.Services.GetRequiredService<IPluginRegistry>();
        Log.Information(
            "Startup complete. Storage provider '{Provider}' initialized; {Count} plugin(s) active: {Plugins}.",
            storage.ProviderName,
            registry.Plugins.Count,
            string.Join(", ", registry.Plugins.Select(p => p.Name)));
    }
}
