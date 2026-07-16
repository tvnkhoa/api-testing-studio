namespace ApiTestingStudio.UI.Messaging;

/// <summary>
/// Published when the set of environments changes (created/renamed/deleted) or the active selection
/// changes, so the toolbar environment switcher and status bar can refresh. Sent by the Profiles &amp;
/// Environments panel and the switcher itself.
/// </summary>
public sealed record EnvironmentsChangedMessage;
