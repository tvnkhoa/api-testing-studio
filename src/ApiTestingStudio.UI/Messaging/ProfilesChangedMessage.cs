namespace ApiTestingStudio.UI.Messaging;

/// <summary>
/// Published when the set of profiles changes (created/renamed/deleted), so the toolbar "Run As"
/// profile switcher can refresh its list. Sent by the Profiles &amp; Environments panel.
/// </summary>
public sealed record ProfilesChangedMessage;
