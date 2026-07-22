namespace ApiTestingStudio.UI.Messaging;

/// <summary>The call-to-action a user invoked on the Welcome screen.</summary>
public enum WelcomeAction
{
    /// <summary>Open the import wizard (requires an open workspace).</summary>
    Import,

    /// <summary>Create a new service in the catalog (requires an open workspace).</summary>
    AddService,

    /// <summary>Build and open the bundled sample workspace.</summary>
    OpenSample,
}

/// <summary>
/// Published when a Welcome-screen call-to-action is clicked. The shell handles it by routing to the
/// matching flow (import wizard, add-service, or build-and-open the sample workspace), keeping the
/// Welcome view model decoupled from those services.
/// </summary>
public sealed record WelcomeActionMessage(WelcomeAction Action);
