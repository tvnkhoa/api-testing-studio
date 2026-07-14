using ApiTestingStudio.Plugin.Abstractions;
using ApiTestingStudio.Plugin.Abstractions.Ui;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.Sample.HelloWorld;

/// <summary>
/// A minimal directory-loaded sample plugin used to exercise the dynamic loading pipeline
/// (Sprint 03). It contributes a tool window and implements the optional lifecycle hooks so the
/// host can drive it through Initialize/Start/Stop.
/// </summary>
public sealed class HelloWorldPluginModule : IPluginModule, IPluginLifecycle
{
    public string Name => "Sample.HelloWorld";

    public Version Version => new(1, 0, 0);

    public void ConfigureServices(IServiceCollection services)
        => services.AddSingleton<IToolWindow, HelloWorldToolWindow>();

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>The sample tool window contributed by <see cref="HelloWorldPluginModule"/>.</summary>
public sealed class HelloWorldToolWindow : IToolWindow
{
    public string ToolWindowId => "sample.helloworld";

    public string Title => "Hello World";
}

/// <summary>
/// A deliberately faulty module whose <see cref="ConfigureServices"/> throws. It exists only so the
/// plugin-host tests can verify a throwing plugin is quarantined without crashing the host. A
/// manifest selects it via its <c>entryType</c>.
/// </summary>
public sealed class ThrowingPluginModule : IPluginModule
{
    public string Name => "Sample.Throwing";

    public Version Version => new(1, 0, 0);

    public void ConfigureServices(IServiceCollection services)
        => throw new InvalidOperationException("Intentional failure from a faulty sample plugin.");
}
