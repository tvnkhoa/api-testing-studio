using System.Globalization;
using System.IO;
using System.Windows;
using ApiTestingStudio.Application.DependencyInjection;
using ApiTestingStudio.Core.DependencyInjection;
using ApiTestingStudio.Core.Plugins;
using ApiTestingStudio.Host.Composition;
using ApiTestingStudio.Infrastructure.DependencyInjection;
using ApiTestingStudio.Plugin.Abstractions.Storage;
using ApiTestingStudio.UI.DependencyInjection;
using ApiTestingStudio.UI.Services;
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

            LogStartupState();

            // Apply the persisted theme before the window is shown so it never flashes the default.
            var themeManager = _host.Services.GetRequiredService<IThemeManager>();
            await themeManager.InitializeAsync().ConfigureAwait(true);

            var shellWindow = _host.Services.GetRequiredService<ShellWindow>();
            shellWindow.Show();
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

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(appDataDir);

        using var bootstrapLoggerFactory = new SerilogLoggerFactory(Log.Logger);
        var pluginsDirectory = Path.Combine(AppContext.BaseDirectory, "plugins");
        builder.Services.AddPluginHost(
            PluginCatalog.GetPluginAssemblies(),
            pluginsDirectory,
            bootstrapLoggerFactory);

        // UI composition
        builder.Services.AddUi();
        builder.Services.AddSingleton<ShellWindow>();

        return builder.Build();
    }

    private void LogStartupState()
    {
        // No workspace is opened at startup: each workspace is a self-contained file the user
        // creates or opens on demand (the shell's open/recent UI lands in Sprint 04). Migrations
        // run when a workspace is created or opened, not here.
        var storage = _host!.Services.GetRequiredService<IStorageProvider>();
        var registry = _host.Services.GetRequiredService<IPluginRegistry>();
        var loaded = registry.Plugins.Where(p => p.State == PluginLifecycleState.Loaded).ToList();
        var quarantined = registry.Plugins.Where(p => p.State == PluginLifecycleState.Quarantined).ToList();
        Log.Information(
            "Startup complete. Storage provider '{Provider}' ready (no workspace open); {Count} plugin(s) active: {Plugins}. {QuarantinedCount} quarantined: {Quarantined}.",
            storage.ProviderName,
            loaded.Count,
            string.Join(", ", loaded.Select(p => $"{p.Name} [{p.Source}]")),
            quarantined.Count,
            string.Join(", ", quarantined.Select(p => $"{p.Name} ({p.Error?.Code})")));
    }
}
